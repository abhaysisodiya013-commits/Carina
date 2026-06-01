using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEngine;

public class RetroJumpGhostAfterImage : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private bool spawnWhileDashing = true;
    [SerializeField] private bool suppressDashGhostsWhileAttacking = true;
    [SerializeField] private bool suppressGhostsDuringRageMode = true;
    [SerializeField] private int maxGhostCount = 16;
    [SerializeField] private float spawnInterval = 0.055f;
    [SerializeField] private float minimumGhostDistance = 0.3f;
    [SerializeField] private float lifetime = 0.24f;
    [SerializeField] private bool waitForJumpPose = true;
    [SerializeField] private bool useSourceRendererColor = true;
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color shineColor = new Color(1f, 1f, 1f, 0.38f);
    [SerializeField] private int sortingOrderOffset = -1;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private float endScaleMultiplier = 1f;
    [SerializeField] private float shineLineLength = 0.32f;
    [SerializeField] private float shineLineWidth = 0.035f;
    [SerializeField] private float shineLineOffset = 0.08f;
    [SerializeField] private float minimumJumpRiseSpeed = 0.1f;
    [SerializeField] private int dashGhostCopies = 1;
    [SerializeField] private int jumpGhostCopies = 1;
    [SerializeField] private bool includeAnchorGhost = true;
    [SerializeField] private float ghostSpacing = 0.34f;
    [SerializeField] private float dashSpacingMultiplier = 1.15f;
    [SerializeField] private float jumpSpacingMultiplier = 0.85f;
    [SerializeField] private bool spawnParticles;
    [SerializeField] private int particleCount = 0;
    [SerializeField] private float particleLifetime = 0.18f;
    [SerializeField] private float particleSize = 0.035f;
    [SerializeField] private float particleRadius = 0.18f;
    [SerializeField] private Color particleColor = new Color(1f, 1f, 1f, 0.32f);

    private sealed class Ghost
    {
        public GameObject Root;
        public SpriteRenderer Body;
        public LineRenderer Shine;
        public Vector3 StartScale;
        public Color StartBodyColor;
        public Color StartShineColor;
        public float Age;
        public float Duration;
        public bool Active;
    }

    private readonly List<Ghost> _ghosts = new List<Ghost>();
    private Character _character;
    private CorgiController _controller;
    private CharacterHandleWeapon _characterHandleWeapon;
    private RetroRageModeAnimator _rageModeAnimator;
    private bool _wasTrailActive;
    private bool _trailStateInitialized;
    private float _lastSpawnTime;
    private Vector3 _lastGhostPosition;
    private Vector3 _previousSourcePosition;
    private Vector3 _lastTrailDirection = Vector3.right;
    private Material _shineMaterial;
    private bool _wasRageModeActive;

    private void Awake()
    {
        _character = GetComponent<Character>();
        _controller = GetComponent<CorgiController>();
        _characterHandleWeapon = _character?.FindAbility<CharacterHandleWeapon>();
        _rageModeAnimator = _character?.FindAbility<RetroRageModeAnimator>();

        if ((sourceRenderer == null) && (_character != null) && (_character.CharacterModel != null))
        {
            sourceRenderer = _character.CharacterModel.GetComponent<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        EnsurePool();
        _trailStateInitialized = false;
    }

    private void LateUpdate()
    {
        UpdateGhosts();

        if ((_character == null) || (sourceRenderer == null))
        {
            return;
        }

        Vector3 currentPosition = sourceRenderer.transform.position + worldOffset;

        bool rageModeActive = IsRageModeActive();
        if (rageModeActive)
        {
            DeactivateGhosts();
            _lastGhostPosition = Vector3.positiveInfinity;
            _previousSourcePosition = currentPosition;
            _wasTrailActive = false;
            _wasRageModeActive = true;
            return;
        }
        _wasRageModeActive = rageModeActive;

        if (IsWeaponAttackInProgress())
        {
            DeactivateGhosts();
            _previousSourcePosition = currentPosition;
            _wasTrailActive = false;
            _lastGhostPosition = Vector3.positiveInfinity;
            return;
        }

        bool trailActive = IsTrailActive();

        if (!_trailStateInitialized)
        {
            _wasTrailActive = trailActive;
            _previousSourcePosition = currentPosition;
            _lastGhostPosition = Vector3.positiveInfinity;
            _trailStateInitialized = true;
            return;
        }

        Vector3 movementDelta = currentPosition - _previousSourcePosition;
        if (movementDelta.sqrMagnitude > 0.0001f)
        {
            _lastTrailDirection = movementDelta.normalized;
        }

        if (trailActive && !_wasTrailActive)
        {
            _lastSpawnTime = waitForJumpPose ? Time.time : -999f;
            _lastGhostPosition = Vector3.positiveInfinity;
            _lastTrailDirection = movementDelta.sqrMagnitude > 0.0001f ? movementDelta.normalized : GetFallbackDirection();
        }

        if (trailActive && ShouldSpawn(currentPosition))
        {
            bool dashing = IsDashing();
            SpawnGhostSet(currentPosition, _lastTrailDirection, dashing);
            SpawnParticleBurst(currentPosition);
            _lastSpawnTime = Time.time;
            _lastGhostPosition = currentPosition;
        }

        _previousSourcePosition = currentPosition;
        _wasTrailActive = trailActive;
    }

    private bool ShouldSpawn(Vector3 currentPosition)
    {
        if ((Time.time - _lastSpawnTime) < spawnInterval)
        {
            return false;
        }

        float activeMinimumDistance = minimumGhostDistance * (IsDashing() ? 0.9f : 1f);
        return (_lastGhostPosition == Vector3.positiveInfinity)
               || (Vector3.Distance(_lastGhostPosition, currentPosition) >= activeMinimumDistance);
    }

    private bool IsTrailActive()
    {
        if (IsRageModeActive())
        {
            return false;
        }

        if (IsWeaponAttackInProgress())
        {
            return false;
        }

        return IsDashing() || IsJumpTrailActive();
    }

    private bool IsJumpTrailActive()
    {
        if ((_character == null) || (_character.MovementState == null))
        {
            return false;
        }

        CharacterStates.MovementStates state = _character.MovementState.CurrentState;
        bool jumpState = (state == CharacterStates.MovementStates.Jumping)
                         || (state == CharacterStates.MovementStates.DoubleJumping);

        return jumpState && ((_controller == null) || (_controller.Speed.y > minimumJumpRiseSpeed));
    }

    private bool IsDashing()
    {
        return spawnWhileDashing
               && (_character != null)
               && (_character.MovementState != null)
               && (_character.MovementState.CurrentState == CharacterStates.MovementStates.Dashing);
    }

    private bool IsRageModeActive()
    {
        if (!suppressGhostsDuringRageMode)
        {
            return false;
        }

        if ((_rageModeAnimator == null) && (_character != null))
        {
            _rageModeAnimator = _character.FindAbility<RetroRageModeAnimator>();
        }

        return (_rageModeAnimator != null) && _rageModeAnimator.RageModeActive;
    }

    private bool IsWeaponAttackInProgress()
    {
        if (!suppressDashGhostsWhileAttacking)
        {
            return false;
        }

        if ((_characterHandleWeapon == null) && (_character != null))
        {
            _characterHandleWeapon = _character.FindAbility<CharacterHandleWeapon>();
        }

        if ((_characterHandleWeapon == null) || (_characterHandleWeapon.CurrentWeapon == null))
        {
            return false;
        }

        Weapon[] weapons = _characterHandleWeapon.CurrentWeapon.GetComponents<Weapon>();
        for (int i = 0; i < weapons.Length; i++)
        {
            if ((weapons[i] != null) && (weapons[i].WeaponState != null) && IsAttackState(weapons[i].WeaponState.CurrentState))
            {
                return true;
            }
        }

        return (_characterHandleWeapon.CurrentWeapon.WeaponState != null)
               && IsAttackState(_characterHandleWeapon.CurrentWeapon.WeaponState.CurrentState);
    }

    private bool IsAttackState(Weapon.WeaponStates state)
    {
        return state == Weapon.WeaponStates.WeaponStart
               || state == Weapon.WeaponStates.WeaponDelayBeforeUse
               || state == Weapon.WeaponStates.WeaponUse
               || state == Weapon.WeaponStates.WeaponDelayBetweenUses;
    }

    private Vector3 GetFallbackDirection()
    {
        return ((_character != null) && _character.IsFacingRight) ? Vector3.right : Vector3.left;
    }

    private void EnsurePool()
    {
        int targetCount = Mathf.Max(1, maxGhostCount);
        while (_ghosts.Count < targetCount)
        {
            _ghosts.Add(CreateGhost());
        }
    }

    private Ghost CreateGhost()
    {
        GameObject root = new GameObject("PlayerAfterImage");
        root.SetActive(false);

        SpriteRenderer body = root.AddComponent<SpriteRenderer>();

        GameObject shineObject = new GameObject("AfterImageShine");
        shineObject.transform.SetParent(root.transform, false);
        LineRenderer shine = shineObject.AddComponent<LineRenderer>();
        shine.useWorldSpace = true;
        shine.positionCount = 2;
        shine.textureMode = LineTextureMode.Stretch;
        shine.numCapVertices = 0;
        shine.numCornerVertices = 0;

        return new Ghost
        {
            Root = root,
            Body = body,
            Shine = shine
        };
    }

    private Ghost GetAvailableGhost()
    {
        EnsurePool();

        for (int i = 0; i < _ghosts.Count; i++)
        {
            if (!_ghosts[i].Active)
            {
                return _ghosts[i];
            }
        }

        Ghost oldest = _ghosts[0];
        for (int i = 1; i < _ghosts.Count; i++)
        {
            if (_ghosts[i].Age > oldest.Age)
            {
                oldest = _ghosts[i];
            }
        }

        return oldest;
    }

    private void SpawnGhostSet(Vector3 anchorPosition, Vector3 trailDirection, bool dashing)
    {
        if ((sourceRenderer == null) || (sourceRenderer.sprite == null))
        {
            return;
        }

        if (dashing)
        {
            trailDirection = new Vector3(trailDirection.x, 0f, 0f);
        }

        if (trailDirection.sqrMagnitude < 0.0001f)
        {
            trailDirection = GetFallbackDirection();
        }

        trailDirection.Normalize();

        int copies = Mathf.Max(1, dashing ? dashGhostCopies : jumpGhostCopies);
        float spacingMultiplier = dashing ? dashSpacingMultiplier : jumpSpacingMultiplier;

        if (includeAnchorGhost)
        {
            SpawnGhost(anchorPosition, 1f, trailDirection);
        }

        for (int i = 0; i < copies; i++)
        {
            float spacing = ghostSpacing * spacingMultiplier * (i + 1);
            float alphaMultiplier = Mathf.Pow(0.68f, i);
            SpawnGhost(anchorPosition - (trailDirection * spacing), alphaMultiplier, trailDirection);
        }
    }

    private void SpawnGhost(Vector3 ghostPosition, float alphaMultiplier, Vector3 trailDirection)
    {
        Ghost ghost = GetAvailableGhost();
        Transform rootTransform = ghost.Root.transform;
        rootTransform.position = ghostPosition;
        rootTransform.rotation = sourceRenderer.transform.rotation;
        rootTransform.localScale = sourceRenderer.transform.lossyScale;

        ConfigureRenderer(ghost.Body, ghostColor, alphaMultiplier, sortingOrderOffset);

        ConfigureShineLine(ghost.Shine, ghostPosition, trailDirection, alphaMultiplier);

        ghost.StartScale = rootTransform.localScale;
        ghost.StartBodyColor = ghost.Body.color;
        ghost.StartShineColor = ghost.Shine.startColor;
        ghost.Age = 0f;
        ghost.Duration = Mathf.Max(0.01f, lifetime);
        ghost.Active = true;
        ghost.Root.SetActive(true);
    }

    private void ConfigureRenderer(SpriteRenderer renderer, Color color, float alphaMultiplier, int sortingOffset)
    {
        renderer.sprite = sourceRenderer.sprite;
        renderer.flipX = sourceRenderer.flipX;
        renderer.flipY = sourceRenderer.flipY;
        renderer.sharedMaterial = sourceRenderer.sharedMaterial;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder + sortingOffset;
        renderer.color = BuildGhostColor(color, alphaMultiplier);
    }

    private void ConfigureShineLine(LineRenderer line, Vector3 ghostPosition, Vector3 trailDirection, float alphaMultiplier)
    {
        if (trailDirection.sqrMagnitude < 0.0001f)
        {
            trailDirection = GetFallbackDirection();
        }

        Vector3 direction = trailDirection.normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f).normalized;
        Vector3 offset = perpendicular * shineLineOffset;
        Vector3 start = ghostPosition - (direction * 0.04f) + offset;
        Vector3 end = ghostPosition - (direction * shineLineLength) + offset;

        line.sharedMaterial = GetShineMaterial();
        line.sortingLayerID = sourceRenderer.sortingLayerID;
        line.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset + 1;
        line.widthMultiplier = shineLineWidth;
        line.SetPosition(0, start);
        line.SetPosition(1, end);

        Color lineColor = BuildGhostColor(shineColor, alphaMultiplier);
        line.startColor = lineColor;
        line.endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0f);
    }

    private Material GetShineMaterial()
    {
        if (_shineMaterial == null)
        {
            _shineMaterial = new Material(Shader.Find("Sprites/Default"));
        }

        return _shineMaterial;
    }

    private Color BuildGhostColor(Color tint, float alphaMultiplier)
    {
        Color sourceColor = useSourceRendererColor && (sourceRenderer != null) ? sourceRenderer.color : Color.white;
        return new Color(
            sourceColor.r * tint.r,
            sourceColor.g * tint.g,
            sourceColor.b * tint.b,
            tint.a * alphaMultiplier);
    }

    private void UpdateGhosts()
    {
        for (int i = 0; i < _ghosts.Count; i++)
        {
            Ghost ghost = _ghosts[i];
            if (!ghost.Active)
            {
                continue;
            }

            ghost.Age += Time.deltaTime;
            float t = Mathf.Clamp01(ghost.Age / ghost.Duration);

            Color body = ghost.StartBodyColor;
            body.a = Mathf.Lerp(ghost.StartBodyColor.a, 0f, t);
            ghost.Body.color = body;

            Color shine = ghost.StartShineColor;
            shine.a = Mathf.Lerp(ghost.StartShineColor.a, 0f, t);
            ghost.Shine.startColor = shine;
            ghost.Shine.endColor = new Color(shine.r, shine.g, shine.b, 0f);

            ghost.Root.transform.localScale = Vector3.Lerp(ghost.StartScale, ghost.StartScale * endScaleMultiplier, t);

            if (t >= 1f)
            {
                ghost.Active = false;
                ghost.Root.SetActive(false);
            }
        }
    }

    private void DeactivateGhosts()
    {
        for (int i = 0; i < _ghosts.Count; i++)
        {
            if (_ghosts[i].Root != null)
            {
                _ghosts[i].Root.SetActive(false);
            }

            _ghosts[i].Active = false;
            _ghosts[i].Age = 0f;
        }
    }

    private void SpawnParticleBurst(Vector3 position)
    {
        if (!spawnParticles || (particleCount <= 0))
        {
            return;
        }

        GameObject particles = new GameObject("PlayerAfterImageParticles");
        particles.transform.position = position;

        ParticleSystem particleSystem = particles.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0.28f;
        main.startSize = particleSize;
        main.startColor = particleColor;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = particleRadius;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder + sortingOrderOffset;

        particleSystem.Play();
        Destroy(particles, particleLifetime + 0.2f);
    }
}
