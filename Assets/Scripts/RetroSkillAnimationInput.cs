using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Plays three direct skill animation clips, with normal/rage variants.
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/Abilities/Retro Skill Animation Input")]
    public class RetroSkillAnimationInput : CharacterAbility
    {
        [Header("Input")]
        public bool ReadInput = true;
        public KeyCode SpellKey = KeyCode.V;
        public KeyCode MultiAttackKey = KeyCode.B;
        public KeyCode SplashAttackKey = KeyCode.N;
        public KeyCode ShieldKey = KeyCode.M;
        public KeyCode GroundAttackKey = KeyCode.L;
        public KeyCode SpawnJumperKey = KeyCode.K;
        public bool StopMovementWhileUsingSkills = true;

        [Header("Skill Camera Shake")]
        public bool EnableSkillCameraShake = true;
        public float SkillCameraShakeDelay = 0f;
        public float SkillCameraShakeDuration = 0.08f;
        public float SkillCameraShakeAmplitude = 0.08f;
        public float SkillCameraShakeFrequency = 8f;
        public float SkillCameraShakeAmplitudeX = 0.08f;
        public float SkillCameraShakeAmplitudeY = 0.04f;
        public float SkillCameraShakeAmplitudeZ = 0f;
        public int SkillCameraShakeChannel = 0;

        [Header("Normal Clips")]
        public AnimationClip SpellClip;
        public AnimationClip MultiAttackClip;
        public AnimationClip SplashAttackClip;
        public AnimationClip GroundAttackClip;
        public AnimationClip SpawnJumperClip;

        [Header("Rage Clips")]
        public AnimationClip RageSpellClip;
        public AnimationClip RageMultiAttackClip;
        public AnimationClip RageAreaAttackClip;
        public AnimationClip RageGroundAttackClip;

        [Header("Ground Attack")]
        public float GroundAttackAnimationDuration = 0f;

        [Header("Ground Attack VFX")]
        public AnimationClip GroundAttackVfxClip;
        public float GroundAttackVfxDuration = 0f;
        public Vector2 GroundAttackVfxOffset = new Vector2(0.75f, 0f);
        public Vector2 GroundAttackVfxVisualSize = Vector2.one;
        public float GroundAttackVfxScale = 1f;
        public int GroundAttackVfxCount = 3;
        public float GroundAttackVfxHorizontalSpacing = 0.75f;
        public float GroundAttackVfxStepDelay = 0.08f;
        public int GroundAttackVfxFramesBeforeAttackEnds = 1;
        public float GroundAttackVfxTimeOffset = 0f;
        public Color GroundAttackVfxColor = Color.white;
        public bool ShowGroundAttackVfxGizmos = true;
        public Color GroundAttackVfxGizmoColor = new Color(1f, 0.55f, 0f, 0.8f);
        public int GroundAttackVfxSortingOrderOffset = 2;
        public int GroundAttackVfxMinimumSortingOrder = 101;
        public bool FlipGroundAttackVfxWithFacing = true;
        public bool GroundAttackVfxFacesRight = true;
        public bool GroundAttackVfxFreezesEnemies = true;
        public float GroundAttackVfxFreezeDuration = 1f;
        public float GroundAttackVfxFreezeDelay = 0f;
        public Color GroundAttackVfxFreezeColor = new Color(0.55f, 0.9f, 1f, 1f);

        [Header("Spawn Jumper")]
        public float SpawnJumperAnimationDuration = 0f;
        public AnimationClip SpawnJumperVfxClip;
        public Sprite SpawnJumperVfxSprite;
        public float SpawnJumperVfxDuration = 0f;
        public Vector2 SpawnJumperVfxOffset = new Vector2(1.25f, 0f);
        public Vector2 SpawnJumperVfxVisualSize = new Vector2(1.5f, 2.5f);
        public float SpawnJumperVfxScale = 1f;
        public Color SpawnJumperVfxColor = Color.white;
        public int SpawnJumperVfxSortingOrderOffset = 2;
        public int SpawnJumperVfxMinimumSortingOrder = 101;
        public bool FlipSpawnJumperVfxWithFacing = true;
        public bool SpawnJumperVfxFacesRight = true;
        public Vector2 SpawnJumperLiftAreaSize = new Vector2(2f, 3f);
        public Vector2 SpawnJumperLiftAreaOffset = new Vector2(1.25f, 1.25f);
        public LayerMask SpawnJumperTargetLayerMask = LayerManager.EnemiesLayerMask;
        public bool SpawnJumperLiftAnyAICharacter = true;
        public float SpawnJumperLiftForce = 18f;
        public float SpawnJumperLiftDelay = 0f;
        public bool ShowSpawnJumperGizmos = true;
        public Color SpawnJumperLiftGizmoColor = new Color(0.2f, 1f, 0.45f, 0.8f);
        public Color SpawnJumperVfxGizmoColor = new Color(0.2f, 0.75f, 1f, 0.8f);

        [Header("Shield")]
        public AnimationClip ShieldClip;
        public AnimationClip RageShieldClip;
        public bool UseSeparateRageShieldClip = false;
        public AnimationClip ShieldVfxClip;
        public Sprite ShieldVfxSprite;
        public float ShieldVfxDuration = 0.5f;
        public bool UseFullShieldAnimationDuration = true;
        public bool MatchShieldAnimationDurationToShieldVfx = true;
        public bool HoldFullShieldVfxFrame = true;
        [Range(0f, 1f)]
        public float ShieldVfxFullFrameNormalizedTime = 0.5f;
        public Vector2 ShieldVfxOffset = new Vector2(0.75f, 0f);
        [FormerlySerializedAs("ShieldVfxVisualGizmoSize")]
        public Vector2 ShieldVfxVisualSize = new Vector2(1f, 1.5f);
        public Vector2 ShieldProjectileBlockSize = new Vector2(1.25f, 1.75f);
        public LayerMask ShieldProjectileLayerMask = LayerManager.ProjectilesLayerMask;
        public float ShieldVfxScale = 1f;
        public Color ShieldVfxColor = new Color(0.35f, 0.9f, 1f, 0.75f);
        public bool UseGeneratedShieldVfxFallback = false;
        public bool ShowShieldGizmos = true;
        public Color ShieldVfxGizmoColor = new Color(0.35f, 0.9f, 1f, 0.8f);
        public Color ShieldProjectileBlockGizmoColor = new Color(0f, 1f, 1f, 0.8f);
        public int ShieldVfxSortingOrderOffset = 2;
        public int ShieldVfxMinimumSortingOrder = 101;
        public bool FlipShieldVfxWithFacing = true;
        public bool ShieldVfxFacesRight = false;
        public bool ShieldCancelAttackUsesComboHit = true;
        public int ShieldCancelComboHitIndex = 2;
        public AnimationClip ShieldCancelAttackClip;
        public AnimationClip RageShieldCancelAttackClip;
        public float ShieldCancelAttackAnimationDuration = 0f;
        public bool UseRawShieldCancelAttackInput = true;
        public KeyCode[] RawShieldCancelAttackKeys = { KeyCode.Mouse0, KeyCode.LeftControl };
        public bool DropBlockedProjectiles = true;
        public float BlockedProjectileDropSpeed = 8f;
        public float BlockedProjectileGroundRaycastDistance = 6f;
        public float BlockedProjectileGroundOffset = 0.05f;
        public float BlockedProjectileFadeDelay = 0.35f;
        public float BlockedProjectileFadeDuration = 0.6f;
        public LayerMask BlockedProjectileGroundLayerMask = LayerManager.ObstaclesLayerMask;

        [Header("Skill Damage")]
        public bool EnableSkillDamage = true;
        public LayerMask SkillDamageTargetLayerMask = LayerManager.EnemiesLayerMask;
        public bool DamageAnyAICharacter = true;
        public bool DestroySpellProjectileOnEnemyHit = true;
        public float SpellDamage = 18f;
        public Vector2 SpellDamageAreaSize = new Vector2(0.5f, 0.5f);
        public Vector2 SpellDamageAreaOffset = Vector2.zero;
        public float MultiAttackDamage = 12f;
        public int MultiAttackHitCount = 3;
        public float MultiAttackHitInterval = 0.08f;
        public float MultiAttackDamageInvincibilityDuration = 0f;
        public Vector2 MultiAttackDamageAreaSize = new Vector2(2f, 1.25f);
        public Vector2 MultiAttackDamageAreaOffset = new Vector2(1f, 0f);
        public float SplashAttackDamage = 15f;
        public float UltimateDamage = 20f;
        public int SplashAttackDamageFramesBeforeEnd = 1;
        public float SplashAttackDamageTimeOffset = 0f;
        public float SplashAttackDamageDelayOverride = -1f;
        public Vector2 SplashAttackDamageAreaSize = new Vector2(3f, 1.5f);
        public Vector2 SplashAttackDamageAreaOffset = new Vector2(1.25f, 0f);
        public float SkillDamageActiveDuration = 0.15f;
        public float SkillDamageInvincibilityDuration = 0.1f;
        public Vector2 SkillDamageKnockbackForce = new Vector2(6f, 2f);
        public bool InvulnerableDuringSplashAttack = true;
        public bool BlockKnockbackDuringSplashAttack = true;
        public bool SplashInvulnerabilityUsesAnimationLength = true;
        public float SplashAttackInvulnerabilityDuration = 0.25f;

        [Header("Skill Damage Gizmos")]
        public bool ShowSkillDamageGizmos = true;
        public Color SpellDamageGizmoColor = new Color(0f, 0.8f, 1f, 0.8f);
        public Color MultiAttackDamageGizmoColor = new Color(1f, 0.8f, 0f, 0.8f);
        public Color SplashAttackDamageGizmoColor = new Color(1f, 0.15f, 0.05f, 0.8f);

        [Header("Spell Cast Projectile")]
        public Transform SpellCastPoint;
        public string SpellCastPointName = "SpellCastPoint";
        public GameObject SpellCastProjectilePrefab;
        public Sprite SpellCastProjectileSprite;
        public AnimationClip SpellCastProjectileClip;
        public bool MoveSpellCastProjectile = true;
        public float SpellCastProjectileSpeed = 8f;
        public float SpellCastProjectileDistance = 5f;
        public float SpellCastProjectileLifetime = 0.75f;
        public bool IgnoreSpellCastProjectileLifetime = false;
        public bool PlaySpellCastProjectileAnimationOnce = true;
        public bool UseExactSpellCastPointPosition = true;
        public Vector2 SpellCastProjectileSpawnOffset = Vector2.zero;
        public int SpellCastProjectileFramesBeforeSpellEnds = 4;
        public float SpellCastProjectileScale = 2f;
        public bool FlipSpellCastProjectileWithFacing = false;
        public bool SpellCastProjectileSpriteFacesRight = true;
        public int SpellCastSortingOrderOffset = 1;
        public int SpellCastMinimumSortingOrder = 100;
        public bool LogSpellCastDebug = true;

        protected RetroRageModeAnimator _rageModeAnimator;
        protected CharacterHandleWeapon _characterHandleWeapon;
        protected PlayableGraph _skillGraph;
        protected float _skillEndsAt;
        protected bool _spellCastProjectilePending;
        protected float _spellCastProjectileSpawnAt;
        protected Coroutine _splashDamageCoroutine;
        protected Coroutine _splashInvulnerabilityCoroutine;
        protected bool _skillMovementLocked;
        protected bool _storedMovementForbidden;
        protected Health _health;
        protected bool _splashProtectionActive;
        protected bool _storedSplashInvulnerable;
        protected bool _storedSplashImmuneToKnockback;
        protected PlayableGraph _shieldVfxGraph;
        protected AnimationClipPlayable _shieldVfxPlayable;
        protected GameObject _shieldVfxObject;
        protected float _shieldVfxAnimationEndsAt;
        protected float _shieldVfxEndsAt;
        protected Sprite _generatedShieldVfxSprite;
        protected bool _shieldActive;
        protected Coroutine _shieldVfxHoldCoroutine;
        protected int _lastShieldCancelAttackFrame = -1;
        protected Coroutine _groundAttackVfxCoroutine;
        protected Coroutine _spawnJumperLiftCoroutine;
        protected Coroutine _skillCameraShakeCoroutine;
        protected PlayableGraph _spawnJumperVfxGraph;
        protected GameObject _spawnJumperVfxObject;
        protected float _spawnJumperVfxEndsAt;
        protected readonly List<GameObject> _groundAttackVfxObjects = new List<GameObject>();
        protected readonly List<PlayableGraph> _groundAttackVfxGraphs = new List<PlayableGraph>();

        protected MMTouchButton _spellTouchBtn;
        protected MMTouchButton _multiAtkTouchBtn;
        protected MMTouchButton _splashAtkTouchBtn;
        protected MMTouchButton _freezeAtkTouchBtn;

        public override string HelpBoxText()
        {
            return "Press V, B, N, M, L, or K to play direct skill animation clips. Rage mode automatically uses the rage clip variants.";
        }

        public override void EarlyProcessAbility()
        {
            if (_shieldActive && HasShieldCancelAttackInputDown())
            {
                CancelShieldForWeaponAttack();
            }

            base.EarlyProcessAbility();
        }

        protected virtual void Update()
        {
            if (_shieldActive && HasShieldCancelAttackInputDown())
            {
                CancelShieldForWeaponAttack();
            }
        }

        protected override void Initialization()
        {
            base.Initialization();

            _rageModeAnimator = _character?.FindAbility<RetroRageModeAnimator>();
            _characterHandleWeapon = _character?.FindAbility<CharacterHandleWeapon>();
            _health = (_character != null) ? _character.GetComponent<Health>() : GetComponent<Health>();
            if ((_health == null) && (_character != null))
            {
                _health = _character.GetComponentInChildren<Health>();
            }
            if (_health == null)
            {
                _health = GetComponentInParent<Health>();
            }

            if ((_animator == null) && (_character != null) && (_character.CharacterModel != null))
            {
                _animator = _character.CharacterModel.GetComponentInChildren<Animator>();
            }

            MMTouchButton[] touchButtons = FindObjectsOfType<MMTouchButton>(true);
            foreach (MMTouchButton btn in touchButtons)
            {
                if (btn.gameObject.name == "SpellBtn") _spellTouchBtn = btn;
                else if (btn.gameObject.name == "MultiAtkBtn") _multiAtkTouchBtn = btn;
                else if (btn.gameObject.name == "AreaAtkBtn") _splashAtkTouchBtn = btn;
                else if (btn.gameObject.name == "FreezeAtkBtn") _freezeAtkTouchBtn = btn;
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if (_spellCastProjectilePending && (Time.time >= _spellCastProjectileSpawnAt))
            {
                _spellCastProjectilePending = false;
                SpawnSpellCastProjectile();
            }

            if (_skillGraph.IsValid() && (Time.time >= _skillEndsAt))
            {
                StopSkillClip();
            }

            if (_skillGraph.IsValid())
            {
                FreezeSkillMovement();
            }

            if (_shieldVfxGraph.IsValid() && (Time.time >= _shieldVfxAnimationEndsAt))
            {
                StopShieldVfxGraph();
            }

            if ((_shieldVfxObject != null) && (Time.time >= _shieldVfxEndsAt))
            {
                StopShieldVfx();
            }

            if (_spawnJumperVfxGraph.IsValid() && (Time.time >= _spawnJumperVfxEndsAt))
            {
                StopSpawnJumperVfx();
            }

            if (!ReadInput || !AbilityAuthorized)
            {
                return;
            }

            if (_shieldActive && WantsToUseWeapon())
            {
                if (HasShieldCancelAttackInputDown())
                {
                    CancelShieldForWeaponAttack();
                }
                else
                {
                    CancelShield();
                }
            }

            bool spellPressed = Input.GetKeyDown(SpellKey);
            bool multiPressed = Input.GetKeyDown(MultiAttackKey);
            bool splashPressed = Input.GetKeyDown(SplashAttackKey);
            bool shieldPressed = Input.GetKeyDown(ShieldKey);
            bool groundPressed = Input.GetKeyDown(GroundAttackKey);
            bool jumperPressed = Input.GetKeyDown(SpawnJumperKey);

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Gamepad.current != null)
            {
                if (UnityEngine.InputSystem.Gamepad.current.buttonWest.wasPressedThisFrame) spellPressed = true; // X button
                if (UnityEngine.InputSystem.Gamepad.current.buttonNorth.wasPressedThisFrame) multiPressed = true; // Y button
                
                // Triggers in Input System can be treated as buttons with wasPressedThisFrame
                if (UnityEngine.InputSystem.Gamepad.current.rightTrigger.wasPressedThisFrame) splashPressed = true; // RT button
                
                // Mapped Freeze to both Right Shoulder (RB) and Face Button B (Circle) to cover all bases
                if (UnityEngine.InputSystem.Gamepad.current.rightShoulder.wasPressedThisFrame) groundPressed = true; 
                if (UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame) groundPressed = true; 
            }
#endif

            // Check mobile UI buttons
            if (_spellTouchBtn != null && _spellTouchBtn.CurrentState == MMTouchButton.ButtonStates.ButtonDown) spellPressed = true;
            if (_multiAtkTouchBtn != null && _multiAtkTouchBtn.CurrentState == MMTouchButton.ButtonStates.ButtonDown) multiPressed = true;
            if (_splashAtkTouchBtn != null && _splashAtkTouchBtn.CurrentState == MMTouchButton.ButtonStates.ButtonDown) splashPressed = true;
            if (_freezeAtkTouchBtn != null && _freezeAtkTouchBtn.CurrentState == MMTouchButton.ButtonStates.ButtonDown) groundPressed = true;

            if (spellPressed)
            {
                PlaySpell();
            }
            if (multiPressed)
            {
                PlayMultiAttack();
            }
            if (splashPressed)
            {
                PlaySplashOrAreaAttack();
            }
            if (shieldPressed)
            {
                PlayShield();
            }
            if (groundPressed)
            {
                PlayGroundAttack();
            }
            if (jumperPressed)
            {
                PlaySpawnJumper();
            }
        }

        public virtual void PlaySpell()
        {
            CancelShield();
            AnimationClip spellClip = IsRageModeActive() && (RageSpellClip != null) ? RageSpellClip : SpellClip;
            PlaySkillClip(spellClip);
            PlaySkillCameraShake();
            ScheduleSpellCastProjectile(spellClip);
        }

        public virtual void PlayMultiAttack()
        {
            CancelShield();
            AnimationClip multiAttackClip = IsRageModeActive() && (RageMultiAttackClip != null) ? RageMultiAttackClip : MultiAttackClip;
            PlaySkillClip(multiAttackClip);
            PlaySkillCameraShake();
            SpawnSkillDamageArea("MultiAttackDamage", MultiAttackDamage, MultiAttackDamageAreaSize, MultiAttackDamageAreaOffset, SkillDamageActiveDuration);
        }

        public virtual void PlaySplashOrAreaAttack()
        {
            CancelShield();
            AnimationClip splashAttackClip = IsRageModeActive() && (RageAreaAttackClip != null) ? RageAreaAttackClip : SplashAttackClip;
            PlaySkillClip(splashAttackClip);
            PlaySkillCameraShake();
            StartSplashAttackInvulnerability(splashAttackClip);
            ScheduleSplashAttackDamage(splashAttackClip);
        }

        public virtual void PlayShield()
        {
            AnimationClip shieldClip = GetShieldClip();
            AnimationClip shieldVfxClip = GetShieldVfxClip();
            float shieldDuration = GetShieldDuration(shieldClip, shieldVfxClip);
            PlaySkillClip(shieldClip, MatchShieldAnimationDurationToShieldVfx ? shieldDuration : 0f);
            PlaySkillCameraShake();
            _shieldActive = true;
            PlayShieldVfx(shieldVfxClip, shieldDuration);
        }

        public virtual void PlayGroundAttack()
        {
            CancelShield();
            AnimationClip groundAttackClip = GetGroundAttackClip();
            PlaySkillClip(groundAttackClip, GroundAttackAnimationDuration);
            PlaySkillCameraShake();
            ScheduleGroundAttackVfx(groundAttackClip);
        }

        public virtual void PlaySpawnJumper()
        {
            CancelShield();
            AnimationClip spawnJumperClip = GetSpawnJumperClip();
            PlaySkillClip(spawnJumperClip, SpawnJumperAnimationDuration);
            PlaySkillCameraShake();
            PlaySpawnJumperVfx(GetSpawnJumperVfxClip());
            ScheduleSpawnJumperLift();
        }

        protected virtual void PlaySkillCameraShake()
        {
            if (!EnableSkillCameraShake || (SkillCameraShakeDuration <= 0f) || (SkillCameraShakeAmplitude <= 0f))
            {
                return;
            }

            if (_skillCameraShakeCoroutine != null)
            {
                StopCoroutine(_skillCameraShakeCoroutine);
                _skillCameraShakeCoroutine = null;
            }

            if (SkillCameraShakeDelay <= 0f)
            {
                TriggerSkillCameraShake();
                return;
            }

            _skillCameraShakeCoroutine = StartCoroutine(DelayedSkillCameraShake());
        }

        protected virtual IEnumerator DelayedSkillCameraShake()
        {
            yield return new WaitForSeconds(SkillCameraShakeDelay);
            TriggerSkillCameraShake();
            _skillCameraShakeCoroutine = null;
        }

        protected virtual void TriggerSkillCameraShake()
        {
            MMCameraShakeEvent.Trigger(
                SkillCameraShakeDuration,
                SkillCameraShakeAmplitude,
                SkillCameraShakeFrequency,
                SkillCameraShakeAmplitudeX,
                SkillCameraShakeAmplitudeY,
                SkillCameraShakeAmplitudeZ,
                false,
                SkillCameraShakeChannel);
        }

        protected virtual bool IsRageModeActive()
        {
            return (_rageModeAnimator != null) && _rageModeAnimator.RageModeActive;
        }

        protected virtual void PlaySkillClip(AnimationClip clip)
        {
            PlaySkillClip(clip, 0f);
        }

        protected virtual void PlaySkillClip(AnimationClip clip, float forcedDuration)
        {
            if ((_animator == null) || (clip == null))
            {
                return;
            }

            StopSkillClip();

            _skillGraph = PlayableGraph.Create("RetroSkillAnimationInput");
            _skillGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_skillGraph, clip);
            float duration = forcedDuration > 0f ? forcedDuration : clip.length;
            clipPlayable.SetTime(0d);
            clipPlayable.SetDuration(duration);
            clipPlayable.SetSpeed(GetClipPlaybackSpeed(clip, duration));
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_skillGraph, "SkillAnimation", _animator);
            output.SetSourcePlayable(clipPlayable);

            _skillEndsAt = Time.time + GetOneShotPlayableDuration(duration);
            _skillGraph.Play();
            FreezeSkillMovement();
        }

        protected virtual double GetClipPlaybackSpeed(AnimationClip clip, float duration)
        {
            if ((clip == null) || (clip.length <= 0f) || (duration <= 0f))
            {
                return 1d;
            }

            return clip.length / duration;
        }

        protected virtual float GetOneShotPlayableDuration(float duration)
        {
            float safeDuration = Mathf.Max(0.01f, duration);
            return Mathf.Max(0.01f, safeDuration - Mathf.Min(0.02f, safeDuration * 0.25f));
        }

        protected virtual void FreezeSkillMovement()
        {
            if (!StopMovementWhileUsingSkills || (_characterHorizontalMovement == null))
            {
                return;
            }

            if (!_skillMovementLocked)
            {
                _storedMovementForbidden = _characterHorizontalMovement.MovementForbidden;
                _skillMovementLocked = true;
            }

            _characterHorizontalMovement.SetHorizontalMove(0f);
            _characterHorizontalMovement.MovementForbidden = true;
            if (_controller != null)
            {
                _controller.SetHorizontalForce(0f);
            }
        }

        protected virtual void RestoreSkillMovement()
        {
            if ((_characterHorizontalMovement == null) || !_skillMovementLocked)
            {
                return;
            }

            _characterHorizontalMovement.MovementForbidden = _storedMovementForbidden;
            _skillMovementLocked = false;
        }

        protected virtual void ScheduleSpellCastProjectile(AnimationClip spellClip)
        {
            _spellCastProjectilePending = false;
            if (spellClip == null)
            {
                return;
            }

            _spellCastProjectilePending = true;
            float frameDuration = 1f / Mathf.Max(1f, spellClip.frameRate);
            float spawnDelay = spellClip.length - (frameDuration * Mathf.Max(0, SpellCastProjectileFramesBeforeSpellEnds));
            _spellCastProjectileSpawnAt = Time.time + Mathf.Max(0.01f, spawnDelay);
        }

        protected virtual void ScheduleSplashAttackDamage(AnimationClip splashAttackClip)
        {
            if (_splashDamageCoroutine != null)
            {
                StopCoroutine(_splashDamageCoroutine);
                _splashDamageCoroutine = null;
            }

            if (splashAttackClip == null)
            {
                return;
            }

            _splashDamageCoroutine = StartCoroutine(SplashAttackDamageCo(splashAttackClip));
        }

        protected virtual IEnumerator SplashAttackDamageCo(AnimationClip splashAttackClip)
        {
            float delay = GetSplashAttackDamageDelay(splashAttackClip);
            yield return new WaitForSeconds(delay);
            SpawnSkillDamageArea("SplashAttackDamage", SplashAttackDamage, SplashAttackDamageAreaSize, SplashAttackDamageAreaOffset, SkillDamageActiveDuration);
            _splashDamageCoroutine = null;
        }

        protected virtual float GetSplashAttackDamageDelay(AnimationClip splashAttackClip)
        {
            if (SplashAttackDamageDelayOverride >= 0f)
            {
                return SplashAttackDamageDelayOverride;
            }

            float frameDuration = 1f / Mathf.Max(1f, splashAttackClip.frameRate);
            float delay = splashAttackClip.length - (frameDuration * Mathf.Max(0, SplashAttackDamageFramesBeforeEnd)) + SplashAttackDamageTimeOffset;
            return Mathf.Max(0.01f, delay);
        }

        protected virtual void SpawnSpellCastProjectile()
        {
            Transform castPoint = GetSpellCastPoint();
            GameObject projectilePrefab = GetSpellCastProjectilePrefab();
            GameObject projectile = (projectilePrefab != null) ? Instantiate(projectilePrefab) : new GameObject("SpellCast");

            float direction = ((_character == null) || _character.IsFacingRight) ? 1f : -1f;
            Vector3 spawnOffset = UseExactSpellCastPointPosition
                ? Vector3.zero
                : new Vector3(SpellCastProjectileSpawnOffset.x * direction, SpellCastProjectileSpawnOffset.y, 0f);
            Vector3 baseScale = projectile.transform.localScale;
            projectile.transform.position = castPoint.position + spawnOffset;
            projectile.transform.rotation = castPoint.rotation;
            projectile.transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * SpellCastProjectileScale, Mathf.Abs(baseScale.y) * SpellCastProjectileScale, baseScale.z);
            projectile.SetActive(true);

            SpriteRenderer renderer = projectile.GetComponentInChildren<SpriteRenderer>();
            if (renderer == null)
            {
                Sprite projectileSprite = GetSpellCastProjectileSprite();
                if (projectileSprite == null)
                {
                    Destroy(projectile);
                    return;
                }

                renderer = projectile.AddComponent<SpriteRenderer>();
                renderer.sprite = projectileSprite;
            }

            CopyCharacterSorting(renderer);
            renderer.enabled = true;
            renderer.color = Color.white;
            if (FlipSpellCastProjectileWithFacing)
            {
                renderer.flipX = SpellCastProjectileSpriteFacesRight ? direction < 0f : direction > 0f;
            }
            else
            {
                renderer.flipX = !SpellCastProjectileSpriteFacesRight;
            }

            RetroSpellCastProjectile mover = projectile.GetComponent<RetroSpellCastProjectile>();
            if (mover == null)
            {
                mover = projectile.AddComponent<RetroSpellCastProjectile>();
            }
            ConfigureDirectDamageHitbox(projectile, SpellDamage, SpellDamageAreaSize, SpellDamageAreaOffset, 0f, 1, 0f, SkillDamageInvincibilityDuration);
            float projectileSpeed = MoveSpellCastProjectile ? SpellCastProjectileSpeed : 0f;
            float projectileDistance = MoveSpellCastProjectile ? SpellCastProjectileDistance : 0f;
            float projectileLifetime = IgnoreSpellCastProjectileLifetime ? 0f : SpellCastProjectileLifetime;
            float projectileAnimationLength = GetSpellCastProjectileAnimationLength(projectile);
            float minimumVisibleDuration = PlaySpellCastProjectileAnimationOnce ? projectileAnimationLength : 0f;
            float travelDuration = (projectileSpeed > 0f) ? Mathf.Max(0.01f, projectileDistance / projectileSpeed) : 0f;
            mover.Initialize(direction, projectileSpeed, projectileDistance, projectileLifetime, minimumVisibleDuration);
            DestroyAfterOneProjectileAnimation(projectile, Mathf.Max(projectileAnimationLength, travelDuration));

            if (LogSpellCastDebug)
            {
                Debug.Log("Spell cast projectile spawned at " + projectile.transform.position, projectile);
            }
        }

        protected virtual void DestroyAfterOneProjectileAnimation(GameObject projectile, float animationLength)
        {
            if (!PlaySpellCastProjectileAnimationOnce || (projectile == null))
            {
                return;
            }

            if (animationLength <= 0f)
            {
                return;
            }

            Destroy(projectile, animationLength);
        }

        protected virtual float GetSpellCastProjectileAnimationLength(GameObject projectile)
        {
            if (SpellCastProjectileClip != null)
            {
                return SpellCastProjectileClip.length;
            }

            Animator projectileAnimator = projectile.GetComponentInChildren<Animator>();
            if ((projectileAnimator == null) || (projectileAnimator.runtimeAnimatorController == null))
            {
                return 0f;
            }

            AnimationClip[] clips = projectileAnimator.runtimeAnimatorController.animationClips;
            if ((clips == null) || (clips.Length == 0) || (clips[0] == null))
            {
                return 0f;
            }

            return clips[0].length;
        }

        protected virtual void PlayShieldVfx(AnimationClip shieldVfxClip)
        {
            PlayShieldVfx(shieldVfxClip, GetShieldVfxDuration(shieldVfxClip));
        }

        protected virtual void PlayShieldVfx(AnimationClip shieldVfxClip, float shieldDuration)
        {
            float activeDuration = Mathf.Max(0.01f, shieldDuration);

            StopShieldVfx();

            _shieldVfxObject = new GameObject("ShieldVfx");
            _shieldVfxObject.transform.SetParent(transform);

            float direction = ((_character == null) || _character.IsFacingRight) ? 1f : -1f;
            _shieldVfxObject.transform.localPosition = new Vector3(ShieldVfxOffset.x * direction, ShieldVfxOffset.y, 0f);
            _shieldVfxObject.transform.localRotation = Quaternion.identity;
            _shieldVfxObject.transform.localScale = Vector3.one;

            GameObject shieldVisual = new GameObject("ShieldVfxVisual");
            shieldVisual.transform.SetParent(_shieldVfxObject.transform);
            shieldVisual.transform.localPosition = Vector3.zero;
            shieldVisual.transform.localRotation = Quaternion.identity;
            shieldVisual.transform.localScale = new Vector3(
                Mathf.Max(0.01f, ShieldVfxVisualSize.x) * Mathf.Max(0.01f, ShieldVfxScale),
                Mathf.Max(0.01f, ShieldVfxVisualSize.y) * Mathf.Max(0.01f, ShieldVfxScale),
                1f);

            SpriteRenderer shieldRenderer = shieldVisual.AddComponent<SpriteRenderer>();
            CopyCharacterSorting(shieldRenderer, ShieldVfxSortingOrderOffset, ShieldVfxMinimumSortingOrder);
            shieldRenderer.sprite = GetShieldVfxSprite(shieldVfxClip);
            shieldRenderer.color = ShieldVfxColor;
            if (FlipShieldVfxWithFacing)
            {
                shieldRenderer.flipX = ShieldVfxFacesRight ? direction < 0f : direction > 0f;
            }

            RetroProjectileShieldBlocker shieldBlocker = _shieldVfxObject.AddComponent<RetroProjectileShieldBlocker>();
            shieldBlocker.Initialize(
                (_character != null) ? _character.gameObject : gameObject,
                GetShieldProjectileLayerMask(),
                ShieldProjectileBlockSize,
                activeDuration,
                DropBlockedProjectiles,
                BlockedProjectileDropSpeed,
                BlockedProjectileGroundRaycastDistance,
                BlockedProjectileGroundOffset,
                BlockedProjectileFadeDelay,
                BlockedProjectileFadeDuration,
                BlockedProjectileGroundLayerMask);

            if (shieldVfxClip != null)
            {
                Animator shieldAnimator = shieldVisual.AddComponent<Animator>();
                _shieldVfxGraph = PlayableGraph.Create("RetroShieldVfx");
                _shieldVfxGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                _shieldVfxPlayable = AnimationClipPlayable.Create(_shieldVfxGraph, shieldVfxClip);
                _shieldVfxPlayable.SetTime(0d);
                _shieldVfxPlayable.SetDuration(shieldVfxClip.length);
                AnimationPlayableOutput output = AnimationPlayableOutput.Create(_shieldVfxGraph, "ShieldVfxAnimation", shieldAnimator);
                output.SetSourcePlayable(_shieldVfxPlayable);
                _shieldVfxGraph.Play();

                float vfxLength = Mathf.Max(0.01f, shieldVfxClip.length);
                float holdDuration = HoldFullShieldVfxFrame ? Mathf.Max(0f, activeDuration - vfxLength) : 0f;
                if (holdDuration > 0f)
                {
                    _shieldVfxHoldCoroutine = StartCoroutine(HoldShieldVfxFrame(vfxLength, holdDuration));
                }
            }

            _shieldVfxAnimationEndsAt = Time.time + activeDuration;
            _shieldVfxEndsAt = Time.time + activeDuration;
            Destroy(_shieldVfxObject, activeDuration);
        }

        protected virtual IEnumerator HoldShieldVfxFrame(float shieldVfxLength, float holdDuration)
        {
            float holdAt = Mathf.Clamp01(ShieldVfxFullFrameNormalizedTime) * shieldVfxLength;
            yield return new WaitForSeconds(holdAt);

            if (!_shieldVfxGraph.IsValid() || !_shieldVfxPlayable.IsValid())
            {
                _shieldVfxHoldCoroutine = null;
                yield break;
            }

            _shieldVfxPlayable.SetTime(holdAt);
            _shieldVfxPlayable.SetSpeed(0d);

            yield return new WaitForSeconds(holdDuration);

            if (_shieldVfxGraph.IsValid() && _shieldVfxPlayable.IsValid())
            {
                _shieldVfxPlayable.SetSpeed(1d);
            }

            _shieldVfxHoldCoroutine = null;
        }

        protected virtual float GetShieldDuration(AnimationClip shieldClip, AnimationClip shieldVfxClip)
        {
            float shieldVfxDuration = GetShieldVfxDuration(shieldVfxClip);
            if (!UseFullShieldAnimationDuration || shieldClip == null)
            {
                return shieldVfxDuration;
            }

            return Mathf.Max(shieldVfxDuration, shieldClip.length);
        }

        protected virtual float GetShieldVfxDuration(AnimationClip shieldVfxClip)
        {
            if (ShieldVfxDuration > 0f)
            {
                return ShieldVfxDuration;
            }

            return (shieldVfxClip != null) ? shieldVfxClip.length : 0f;
        }

        protected virtual LayerMask GetShieldProjectileLayerMask()
        {
            LayerMask projectileLayerMask = ShieldProjectileLayerMask;
            int playerProjectilesLayer = LayerMask.NameToLayer("PlayerProjectiles");
            if (playerProjectilesLayer >= 0)
            {
                projectileLayerMask |= (1 << playerProjectilesLayer);
            }

            return projectileLayerMask;
        }

        protected virtual void PlaySpawnJumperVfx(AnimationClip spawnJumperVfxClip)
        {
            StopSpawnJumperVfx();

            if (spawnJumperVfxClip == null)
            {
                return;
            }

            float direction = ((_character == null) || _character.IsFacingRight) ? 1f : -1f;
            float vfxDuration = Mathf.Max(0.01f, GetSpawnJumperVfxDuration(spawnJumperVfxClip));

            _spawnJumperVfxObject = new GameObject("SpawnJumperVfx");
            _spawnJumperVfxObject.transform.position = transform.position + new Vector3(SpawnJumperVfxOffset.x * direction, SpawnJumperVfxOffset.y, 0f);
            _spawnJumperVfxObject.transform.rotation = transform.rotation;
            _spawnJumperVfxObject.transform.localScale = Vector3.one;

            GameObject visual = new GameObject("SpawnJumperVfxVisual");
            visual.transform.SetParent(_spawnJumperVfxObject.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(
                Mathf.Max(0.01f, SpawnJumperVfxVisualSize.x) * Mathf.Max(0.01f, SpawnJumperVfxScale),
                Mathf.Max(0.01f, SpawnJumperVfxVisualSize.y) * Mathf.Max(0.01f, SpawnJumperVfxScale),
                1f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            CopyCharacterSorting(renderer, SpawnJumperVfxSortingOrderOffset, SpawnJumperVfxMinimumSortingOrder);
            renderer.sprite = GetSpawnJumperVfxSprite(spawnJumperVfxClip);
            renderer.color = SpawnJumperVfxColor;
            if (FlipSpawnJumperVfxWithFacing)
            {
                renderer.flipX = SpawnJumperVfxFacesRight ? direction < 0f : direction > 0f;
            }

            Animator animator = visual.AddComponent<Animator>();
            _spawnJumperVfxGraph = PlayableGraph.Create("RetroSpawnJumperVfx");
            _spawnJumperVfxGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_spawnJumperVfxGraph, spawnJumperVfxClip);
            clipPlayable.SetTime(0d);
            clipPlayable.SetDuration(vfxDuration);
            clipPlayable.SetSpeed(GetClipPlaybackSpeed(spawnJumperVfxClip, vfxDuration));
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_spawnJumperVfxGraph, "SpawnJumperVfxAnimation", animator);
            output.SetSourcePlayable(clipPlayable);
            _spawnJumperVfxGraph.Play();

            _spawnJumperVfxEndsAt = Time.time + vfxDuration;
            Destroy(_spawnJumperVfxObject, vfxDuration);
        }

        protected virtual float GetSpawnJumperVfxDuration(AnimationClip spawnJumperVfxClip)
        {
            if (SpawnJumperVfxDuration > 0f)
            {
                return SpawnJumperVfxDuration;
            }

            return (spawnJumperVfxClip != null) ? spawnJumperVfxClip.length : 0f;
        }

        protected virtual void ScheduleSpawnJumperLift()
        {
            if (_spawnJumperLiftCoroutine != null)
            {
                StopCoroutine(_spawnJumperLiftCoroutine);
                _spawnJumperLiftCoroutine = null;
            }

            if (SpawnJumperLiftDelay <= 0f)
            {
                ApplySpawnJumperLift();
                return;
            }

            _spawnJumperLiftCoroutine = StartCoroutine(SpawnJumperLiftCo());
        }

        protected virtual IEnumerator SpawnJumperLiftCo()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, SpawnJumperLiftDelay));
            ApplySpawnJumperLift();
            _spawnJumperLiftCoroutine = null;
        }

        protected virtual void ApplySpawnJumperLift()
        {
            if ((SpawnJumperLiftAreaSize.x <= 0f) || (SpawnJumperLiftAreaSize.y <= 0f))
            {
                return;
            }

            float direction = ((_character == null) || _character.IsFacingRight) ? 1f : -1f;
            Vector3 center = transform.position + new Vector3(SpawnJumperLiftAreaOffset.x * direction, SpawnJumperLiftAreaOffset.y, 0f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, SpawnJumperLiftAreaSize, transform.eulerAngles.z, Physics2D.AllLayers);
            List<Transform> liftedTargets = new List<Transform>();

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null)
                {
                    continue;
                }

                Health targetHealth = hits[i].GetComponentInParent<Health>();
                CorgiController targetController = hits[i].GetComponentInParent<CorgiController>();
                Rigidbody2D targetRigidbody = hits[i].GetComponentInParent<Rigidbody2D>();
                Transform targetRoot = GetSpawnJumperTargetRoot(targetHealth, targetController, targetRigidbody, hits[i]);

                if ((targetRoot == null) || liftedTargets.Contains(targetRoot) || !CanSpawnJumperLiftTarget(hits[i], targetHealth, targetController))
                {
                    continue;
                }

                LiftSpawnJumperTarget(targetController, targetRigidbody);
                liftedTargets.Add(targetRoot);
            }
        }

        protected virtual Transform GetSpawnJumperTargetRoot(Health targetHealth, CorgiController targetController, Rigidbody2D targetRigidbody, Collider2D targetCollider)
        {
            if (targetHealth != null)
            {
                return targetHealth.transform;
            }
            if (targetController != null)
            {
                return targetController.transform;
            }
            if (targetRigidbody != null)
            {
                return targetRigidbody.transform;
            }

            return (targetCollider != null) ? targetCollider.transform : null;
        }

        protected virtual bool CanSpawnJumperLiftTarget(Collider2D targetCollider, Health targetHealth, CorgiController targetController)
        {
            if (IsSpawnJumperOwner(targetHealth, targetController, targetCollider))
            {
                return false;
            }

            bool layerMatches = ((SpawnJumperTargetLayerMask.value & (1 << targetCollider.gameObject.layer)) != 0)
                                || ((targetHealth != null) && ((SpawnJumperTargetLayerMask.value & (1 << targetHealth.gameObject.layer)) != 0))
                                || ((targetController != null) && ((SpawnJumperTargetLayerMask.value & (1 << targetController.gameObject.layer)) != 0));
            if (layerMatches)
            {
                return true;
            }

            if (!SpawnJumperLiftAnyAICharacter)
            {
                return false;
            }

            Character character = (targetHealth != null) ? targetHealth.GetComponent<Character>() : null;
            if ((character == null) && (targetController != null))
            {
                character = targetController.GetComponent<Character>();
            }
            if ((character == null) && (targetCollider != null))
            {
                character = targetCollider.GetComponentInParent<Character>();
            }

            return (character != null) && (character.CharacterType == Character.CharacterTypes.AI);
        }

        protected virtual bool IsSpawnJumperOwner(Health targetHealth, CorgiController targetController, Collider2D targetCollider)
        {
            GameObject owner = (_character != null) ? _character.gameObject : gameObject;
            return ((targetHealth != null) && ((targetHealth.gameObject == owner) || targetHealth.transform.IsChildOf(owner.transform)))
                   || ((targetController != null) && ((targetController.gameObject == owner) || targetController.transform.IsChildOf(owner.transform)))
                   || ((targetCollider != null) && ((targetCollider.gameObject == owner) || targetCollider.transform.IsChildOf(owner.transform)));
        }

        protected virtual void LiftSpawnJumperTarget(CorgiController targetController, Rigidbody2D targetRigidbody)
        {
            if (targetController != null)
            {
                targetController.SetVerticalForce(Mathf.Max(0f, SpawnJumperLiftForce));
                CharacterJump targetJump = targetController.gameObject.MMGetComponentNoAlloc<Character>()?.FindAbility<CharacterJump>();
                if (targetJump != null)
                {
                    targetJump.SetCanJumpStop(false);
                }
                return;
            }

            if (targetRigidbody != null)
            {
                Vector2 velocity = targetRigidbody.linearVelocity;
                velocity.y = Mathf.Max(velocity.y, SpawnJumperLiftForce);
                targetRigidbody.linearVelocity = velocity;
            }
        }

        protected virtual void ScheduleGroundAttackVfx(AnimationClip groundAttackClip)
        {
            StopGroundAttackVfx();

            AnimationClip groundAttackVfxClip = GetGroundAttackVfxClip();
            if (groundAttackVfxClip == null)
            {
                return;
            }

            float delay = GetGroundAttackVfxStartDelay(groundAttackClip);
            _groundAttackVfxCoroutine = StartCoroutine(GroundAttackVfxSequenceCo(delay, groundAttackVfxClip));
        }

        protected virtual float GetGroundAttackVfxStartDelay(AnimationClip groundAttackClip)
        {
            if (groundAttackClip == null)
            {
                return Mathf.Max(0f, GroundAttackVfxTimeOffset);
            }

            float frameDuration = 1f / Mathf.Max(1f, groundAttackClip.frameRate);
            float delay = groundAttackClip.length - (frameDuration * Mathf.Max(0, GroundAttackVfxFramesBeforeAttackEnds)) + GroundAttackVfxTimeOffset;
            return Mathf.Max(0f, delay);
        }

        protected virtual IEnumerator GroundAttackVfxSequenceCo(float startDelay, AnimationClip groundAttackVfxClip)
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSeconds(startDelay);
            }

            int vfxCount = Mathf.Max(1, GroundAttackVfxCount);
            float vfxDuration = Mathf.Max(0.01f, GetGroundAttackVfxDuration(groundAttackVfxClip));
            float stepDelay = GroundAttackVfxStepDelay >= 0f ? GroundAttackVfxStepDelay : 0.08f;
            float direction = ((_character == null) || _character.IsFacingRight) ? 1f : -1f;

            for (int i = 0; i < vfxCount; i++)
            {
                SpawnGroundAttackVfx(groundAttackVfxClip, i, direction, vfxDuration);
                if (i < vfxCount - 1)
                {
                    yield return new WaitForSeconds(Mathf.Max(0f, stepDelay));
                }
            }

            yield return new WaitForSeconds(vfxDuration + 0.02f);
            DestroyGroundAttackVfxInstances();
            _groundAttackVfxCoroutine = null;
        }

        protected virtual void SpawnGroundAttackVfx(AnimationClip groundAttackVfxClip, int index, float direction, float duration)
        {
            GameObject groundVfxObject = new GameObject("GroundAtkVfx");
            Vector3 offset = new Vector3(
                (GroundAttackVfxOffset.x + GroundAttackVfxHorizontalSpacing * index) * direction,
                GroundAttackVfxOffset.y,
                0f);
            groundVfxObject.transform.position = transform.position + offset;
            groundVfxObject.transform.rotation = transform.rotation;
            groundVfxObject.transform.localScale = Vector3.one;

            GameObject visual = new GameObject("GroundAtkVfxVisual");
            visual.transform.SetParent(groundVfxObject.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(
                Mathf.Max(0.01f, GroundAttackVfxVisualSize.x) * Mathf.Max(0.01f, GroundAttackVfxScale),
                Mathf.Max(0.01f, GroundAttackVfxVisualSize.y) * Mathf.Max(0.01f, GroundAttackVfxScale),
                1f);

            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            CopyCharacterSorting(renderer, GroundAttackVfxSortingOrderOffset, GroundAttackVfxMinimumSortingOrder);
            renderer.sprite = GetFirstSpriteFromClip(groundAttackVfxClip);
            renderer.color = GroundAttackVfxColor;
            if (FlipGroundAttackVfxWithFacing)
            {
                renderer.flipX = GroundAttackVfxFacesRight ? direction < 0f : direction > 0f;
            }

            Animator animator = visual.AddComponent<Animator>();
            PlayableGraph graph = PlayableGraph.Create("RetroGroundAttackVfx");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(graph, groundAttackVfxClip);
            clipPlayable.SetTime(0d);
            clipPlayable.SetDuration(groundAttackVfxClip.length);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "GroundAttackVfxAnimation", animator);
            output.SetSourcePlayable(clipPlayable);
            graph.Play();

            ConfigureGroundAttackVfxFreezeHitbox(groundVfxObject, duration);
            _groundAttackVfxObjects.Add(groundVfxObject);
            _groundAttackVfxGraphs.Add(graph);
            Destroy(groundVfxObject, GetOneShotPlayableDuration(duration));
        }

        protected virtual void ConfigureGroundAttackVfxFreezeHitbox(GameObject groundVfxObject, float duration)
        {
            if (!GroundAttackVfxFreezesEnemies || (groundVfxObject == null))
            {
                return;
            }

            RetroSkillDamageHitbox damageHitbox = groundVfxObject.AddComponent<RetroSkillDamageHitbox>();
            Vector2 hitboxSize = new Vector2(
                Mathf.Max(0.01f, GroundAttackVfxVisualSize.x) * Mathf.Max(0.01f, GroundAttackVfxScale),
                Mathf.Max(0.01f, GroundAttackVfxVisualSize.y) * Mathf.Max(0.01f, GroundAttackVfxScale));

            damageHitbox.Initialize(
                (_character != null) ? _character.gameObject : gameObject,
                SkillDamageTargetLayerMask,
                0f,
                0f,
                hitboxSize,
                Vector2.zero,
                Mathf.Max(0.01f, duration),
                DamageAnyAICharacter,
                false,
                1,
                0f);
            damageHitbox.ConfigureFreezeEffect(true, GroundAttackVfxFreezeDuration, GroundAttackVfxFreezeDelay, GroundAttackVfxFreezeColor);
        }

        protected virtual float GetGroundAttackVfxDuration(AnimationClip groundAttackVfxClip)
        {
            if (GroundAttackVfxDuration > 0f)
            {
                return GroundAttackVfxDuration;
            }

            return (groundAttackVfxClip != null) ? groundAttackVfxClip.length : 0f;
        }

        protected virtual void SpawnSkillDamageArea(string damageAreaName, float damage, Vector2 areaSize, Vector2 areaOffset, float activeDuration)
        {
            if (!EnableSkillDamage)
            {
                return;
            }

            GameObject damageArea = new GameObject(damageAreaName);
            float direction = ((_character == null) || _character.IsFacingRight) ? 1f : -1f;
            Vector3 offset = new Vector3(areaOffset.x * direction, areaOffset.y, 0f);
            damageArea.transform.position = transform.position + offset;
            damageArea.transform.rotation = transform.rotation;
            int maxHitsPerTarget = (damageAreaName == "MultiAttackDamage") ? MultiAttackHitCount : 1;
            float hitInterval = (damageAreaName == "MultiAttackDamage") ? MultiAttackHitInterval : 0f;
            float invincibilityDuration = (damageAreaName == "MultiAttackDamage") ? MultiAttackDamageInvincibilityDuration : SkillDamageInvincibilityDuration;
            float finalActiveDuration = (damageAreaName == "MultiAttackDamage")
                ? Mathf.Max(activeDuration, (Mathf.Max(1, MultiAttackHitCount) - 1) * Mathf.Max(0f, MultiAttackHitInterval) + 0.03f)
                : activeDuration;
            ConfigureDirectDamageHitbox(damageArea, damage, areaSize, Vector2.zero, finalActiveDuration, maxHitsPerTarget, hitInterval, invincibilityDuration);
            Destroy(damageArea, Mathf.Max(0.01f, finalActiveDuration));
        }

        protected virtual void ConfigureDamageOnTouch(GameObject damageObject, float damage, Vector2 areaSize, Vector2 areaOffset)
        {
            if (!EnableSkillDamage || (damageObject == null))
            {
                return;
            }

            BoxCollider2D damageCollider = damageObject.GetComponent<BoxCollider2D>();
            if (damageCollider == null)
            {
                damageCollider = damageObject.AddComponent<BoxCollider2D>();
            }
            damageCollider.isTrigger = true;
            damageCollider.size = areaSize;
            damageCollider.offset = areaOffset;

            Rigidbody2D rigidBody = damageObject.GetComponent<Rigidbody2D>();
            if (rigidBody == null)
            {
                rigidBody = damageObject.AddComponent<Rigidbody2D>();
            }
            rigidBody.bodyType = RigidbodyType2D.Kinematic;
            rigidBody.gravityScale = 0f;

            DamageOnTouch damageOnTouch = damageObject.GetComponent<DamageOnTouch>();
            if (damageOnTouch == null)
            {
                damageOnTouch = damageObject.AddComponent<DamageOnTouch>();
            }

            damageOnTouch.Owner = (_character != null) ? _character.gameObject : gameObject;
            damageOnTouch.TargetLayerMask = SkillDamageTargetLayerMask;
            damageOnTouch.MinDamageCaused = damage;
            damageOnTouch.MaxDamageCaused = damage;
            damageOnTouch.InvincibilityDuration = SkillDamageInvincibilityDuration;
            damageOnTouch.DamageCausedKnockbackType = DamageOnTouch.KnockbackStyles.SetForce;
            damageOnTouch.DamageCausedKnockbackDirection = DamageOnTouch.CausedKnockbackDirections.BasedOnOwnerPosition;
            damageOnTouch.DamageCausedKnockbackForce = SkillDamageKnockbackForce;
            damageOnTouch.DamageTakenEveryTime = 0f;
            damageOnTouch.DamageTakenDamageable = 0f;
            damageOnTouch.DamageTakenNonDamageable = 0f;
        }

        protected virtual void ConfigureDirectDamageHitbox(GameObject damageObject, float damage, Vector2 areaSize, Vector2 areaOffset, float lifetime, int maxHitsPerTarget, float hitInterval, float invincibilityDuration)
        {
            if (!EnableSkillDamage || (damageObject == null))
            {
                return;
            }

            RetroSkillDamageHitbox damageHitbox = damageObject.GetComponent<RetroSkillDamageHitbox>();
            if (damageHitbox == null)
            {
                damageHitbox = damageObject.AddComponent<RetroSkillDamageHitbox>();
            }

            damageHitbox.Initialize(
                (_character != null) ? _character.gameObject : gameObject,
                SkillDamageTargetLayerMask,
                damage,
                invincibilityDuration,
                areaSize,
                areaOffset,
                lifetime,
                DamageAnyAICharacter,
                (lifetime <= 0f) && DestroySpellProjectileOnEnemyHit,
                maxHitsPerTarget,
                hitInterval);
        }

        protected virtual void StartSplashAttackInvulnerability(AnimationClip splashAttackClip)
        {
            if (!InvulnerableDuringSplashAttack || (_health == null))
            {
                return;
            }

            if (_splashInvulnerabilityCoroutine != null)
            {
                StopCoroutine(_splashInvulnerabilityCoroutine);
                _splashInvulnerabilityCoroutine = null;
            }

            float duration = SplashInvulnerabilityUsesAnimationLength && (splashAttackClip != null)
                ? Mathf.Max(SplashAttackInvulnerabilityDuration, splashAttackClip.length)
                : SplashAttackInvulnerabilityDuration;
            _splashInvulnerabilityCoroutine = StartCoroutine(SplashAttackInvulnerabilityCo(duration));
        }

        protected virtual IEnumerator SplashAttackInvulnerabilityCo(float duration)
        {
            ApplySplashProtection();
            yield return new WaitForSeconds(Mathf.Max(0.01f, duration));
            RestoreSplashProtection();
            _splashInvulnerabilityCoroutine = null;
        }

        protected virtual void ApplySplashProtection()
        {
            if (_health == null)
            {
                return;
            }

            if (!_splashProtectionActive)
            {
                _storedSplashInvulnerable = _health.Invulnerable;
                _storedSplashImmuneToKnockback = _health.ImmuneToKnockback;
                _splashProtectionActive = true;
            }

            _health.DamageDisabled();
            _health.Invulnerable = true;
            if (BlockKnockbackDuringSplashAttack)
            {
                _health.ImmuneToKnockback = true;
            }
        }

        protected virtual void RestoreSplashProtection()
        {
            if (_health == null)
            {
                return;
            }

            _health.DamageEnabled();
            if (!_splashProtectionActive)
            {
                return;
            }

            _health.Invulnerable = _storedSplashInvulnerable;
            _health.ImmuneToKnockback = _storedSplashImmuneToKnockback;
            _splashProtectionActive = false;
        }

        protected virtual GameObject GetSpellCastProjectilePrefab()
        {
#if UNITY_EDITOR
            if (SpellCastProjectilePrefab == null)
            {
                SpellCastProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SpellCastProjectile.prefab");
            }
#endif
            return SpellCastProjectilePrefab;
        }

        protected virtual Sprite GetSpellCastProjectileSprite()
        {
#if UNITY_EDITOR
            if (SpellCastProjectileSprite == null)
            {
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Spearwoman/lightning ball-Sheet.png");
                for (int i = 0; i < assets.Length; i++)
                {
                    Sprite sprite = assets[i] as Sprite;
                    if (sprite != null)
                    {
                        SpellCastProjectileSprite = sprite;
                        break;
                    }
                }
            }
#endif
            return SpellCastProjectileSprite;
        }

        protected virtual AnimationClip GetGroundAttackClip()
        {
#if UNITY_EDITOR
            if (GroundAttackClip == null)
            {
                GroundAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/GroundAtk.anim");
            }
            if (RageGroundAttackClip == null)
            {
                RageGroundAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation4/GroundAtk.anim");
            }
#endif
            if (IsRageModeActive() && (RageGroundAttackClip != null))
            {
                return RageGroundAttackClip;
            }

            return GroundAttackClip;
        }

        protected virtual AnimationClip GetSpawnJumperClip()
        {
#if UNITY_EDITOR
            if (SpawnJumperClip == null)
            {
                SpawnJumperClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/Spawnjumper.anim");
            }
#endif
            return SpawnJumperClip;
        }

        protected virtual AnimationClip GetSpawnJumperVfxClip()
        {
#if UNITY_EDITOR
            if (SpawnJumperVfxClip == null)
            {
                SpawnJumperVfxClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/SpawnJumpVfx.anim");
            }
#endif
            return SpawnJumperVfxClip;
        }

        protected virtual Sprite GetSpawnJumperVfxSprite(AnimationClip spawnJumperVfxClip)
        {
            if (SpawnJumperVfxSprite != null)
            {
                return SpawnJumperVfxSprite;
            }

            SpawnJumperVfxSprite = GetFirstSpriteFromClip(spawnJumperVfxClip);
            return SpawnJumperVfxSprite;
        }

        protected virtual AnimationClip GetGroundAttackVfxClip()
        {
#if UNITY_EDITOR
            if (GroundAttackVfxClip == null)
            {
                GroundAttackVfxClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/GroundAtkVfx.anim");
            }
#endif
            return GroundAttackVfxClip;
        }

        protected virtual AnimationClip GetShieldClip()
        {
#if UNITY_EDITOR
            if (ShieldClip == null)
            {
                ShieldClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/Shield.anim");
            }
            if (RageShieldClip == null)
            {
                RageShieldClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/Shield.anim");
            }
#endif
            if (IsRageModeActive())
            {
                return (UseSeparateRageShieldClip && HasVisibleClipBindings(RageShieldClip)) ? RageShieldClip : ShieldClip;
            }

            return ShieldClip;
        }

        protected virtual AnimationClip GetShieldVfxClip()
        {
#if UNITY_EDITOR
            if (ShieldVfxClip == null)
            {
                ShieldVfxClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/ShieldVfx.anim");
            }
#endif
            return ShieldVfxClip;
        }

        protected virtual AnimationClip GetShieldCancelAttackClip()
        {
#if UNITY_EDITOR
            if (ShieldCancelAttackClip == null)
            {
                ShieldCancelAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/AtkCombo3.anim");
            }
            if (RageShieldCancelAttackClip == null)
            {
                RageShieldCancelAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation4/AtkCombo3L.anim");
            }
#endif
            if (IsRageModeActive() && (RageShieldCancelAttackClip != null))
            {
                return RageShieldCancelAttackClip;
            }

            return ShieldCancelAttackClip;
        }

        protected virtual Sprite GetShieldVfxSprite(AnimationClip shieldVfxClip)
        {
            if (ShieldVfxSprite != null)
            {
                return ShieldVfxSprite;
            }

#if UNITY_EDITOR
            if (HasVisibleClipBindings(shieldVfxClip))
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(shieldVfxClip);
                for (int i = 0; i < bindings.Length; i++)
                {
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(shieldVfxClip, bindings[i]);
                    if ((keyframes != null) && (keyframes.Length > 0) && (keyframes[0].value is Sprite sprite))
                    {
                        ShieldVfxSprite = sprite;
                        return ShieldVfxSprite;
                    }
                }
            }
#endif

            return UseGeneratedShieldVfxFallback ? GetGeneratedShieldVfxSprite() : null;
        }

        protected virtual Sprite GetFirstSpriteFromClip(AnimationClip clip)
        {
#if UNITY_EDITOR
            if (HasVisibleClipBindings(clip))
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
                for (int i = 0; i < bindings.Length; i++)
                {
                    ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, bindings[i]);
                    if ((keyframes != null) && (keyframes.Length > 0) && (keyframes[0].value is Sprite sprite))
                    {
                        return sprite;
                    }
                }
            }
#endif

            return null;
        }

        protected virtual bool HasVisibleClipBindings(AnimationClip clip)
        {
            return (clip != null) && !clip.empty;
        }

        protected virtual Sprite GetGeneratedShieldVfxSprite()
        {
            if (_generatedShieldVfxSprite != null)
            {
                return _generatedShieldVfxSprite;
            }

            const int width = 48;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color fill = new Color(1f, 1f, 1f, 0.32f);
            Color edge = Color.white;
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            float radiusX = width * 0.36f;
            float radiusY = height * 0.43f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalized = Mathf.Pow((x - center.x) / radiusX, 2f) + Mathf.Pow((y - center.y) / radiusY, 2f);
                    if (normalized <= 1f)
                    {
                        texture.SetPixel(x, y, normalized > 0.72f ? edge : fill);
                    }
                    else
                    {
                        texture.SetPixel(x, y, clear);
                    }
                }
            }

            texture.Apply();
            _generatedShieldVfxSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 32f);
            return _generatedShieldVfxSprite;
        }

        protected virtual Transform GetSpellCastPoint()
        {
            if (SpellCastPoint != null)
            {
                return SpellCastPoint;
            }

            if ((_character != null) && !string.IsNullOrEmpty(SpellCastPointName))
            {
                Transform found = _character.transform.Find(SpellCastPointName);
                if (found == null)
                {
                    found = FindChildRecursive(_character.transform, SpellCastPointName);
                }

                if (found != null)
                {
                    SpellCastPoint = found;
                    return SpellCastPoint;
                }
            }

            return transform;
        }

        protected virtual Transform FindChildRecursive(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        protected virtual void CopyCharacterSorting(SpriteRenderer projectileRenderer)
        {
            CopyCharacterSorting(projectileRenderer, SpellCastSortingOrderOffset, SpellCastMinimumSortingOrder);
        }

        protected virtual void CopyCharacterSorting(SpriteRenderer projectileRenderer, int sortingOrderOffset, int minimumSortingOrder)
        {
            if (projectileRenderer == null)
            {
                return;
            }

            SpriteRenderer sourceRenderer = null;
            if ((_character != null) && (_character.CharacterModel != null))
            {
                sourceRenderer = _character.CharacterModel.GetComponentInChildren<SpriteRenderer>();
            }
            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (sourceRenderer == null)
            {
                return;
            }

            projectileRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            projectileRenderer.sortingOrder = Mathf.Max(sourceRenderer.sortingOrder + sortingOrderOffset, minimumSortingOrder);
            projectileRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }

        protected virtual void StopSkillClip()
        {
            if (_skillGraph.IsValid())
            {
                _skillGraph.Destroy();
            }
            _shieldActive = false;
            RestoreSkillMovement();
        }

        protected virtual void StopShieldVfx()
        {
            StopShieldVfxGraph();

            if (_shieldVfxObject != null)
            {
                _shieldVfxObject.SetActive(false);
                Destroy(_shieldVfxObject);
                _shieldVfxObject = null;
            }
            _shieldActive = false;
        }

        protected virtual void CancelShield()
        {
            StopShieldVfx();
            StopSkillClip();
            _shieldActive = false;
        }

        protected virtual void CancelShieldForWeaponAttack()
        {
            if (_lastShieldCancelAttackFrame == Time.frameCount)
            {
                return;
            }

            _lastShieldCancelAttackFrame = Time.frameCount;
            CancelShield();
            ForceShieldCancelComboHit();

            if (_characterHandleWeapon != null)
            {
                _characterHandleWeapon.ShootStart();
            }

            PlayShieldCancelAttackAnimation();
        }

        protected virtual void PlayShieldCancelAttackAnimation()
        {
            AnimationClip attackClip = GetShieldCancelAttackClip();
            if (attackClip == null)
            {
                return;
            }

            PlaySkillClip(attackClip, ShieldCancelAttackAnimationDuration);
        }

        protected virtual void ForceShieldCancelComboHit()
        {
            if (!ShieldCancelAttackUsesComboHit || (_characterHandleWeapon == null) || (_characterHandleWeapon.CurrentWeapon == null))
            {
                return;
            }

            ComboWeapon comboWeapon = _characterHandleWeapon.CurrentWeapon.GetComponent<ComboWeapon>();
            if (comboWeapon == null)
            {
                return;
            }

            int comboIndex = Mathf.Max(0, ShieldCancelComboHitIndex);
            comboWeapon.ForceWeaponIndex(comboIndex);
        }

        protected virtual bool IsWeaponAttackInputDown()
        {
            if (_inputManager == null)
            {
                return IsRawShieldCancelAttackInputDown();
            }

            bool primaryShootDown = (_inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
                                    || (_inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed)
                                    || (_inputManager.ShootAxis == MMInput.ButtonStates.ButtonDown)
                                    || (_inputManager.ShootAxis == MMInput.ButtonStates.ButtonPressed);
            bool secondaryShootDown = (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
                                      || (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed)
                                      || (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonDown)
                                      || (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonPressed);
            return primaryShootDown || secondaryShootDown || IsRawShieldCancelAttackInputDown();
        }

        protected virtual bool HasShieldCancelAttackInputDown()
        {
            if (IsRawShieldCancelAttackInputDown())
            {
                return true;
            }

            if (_inputManager == null)
            {
                return false;
            }

            return (_inputManager.ShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
                   || (_inputManager.ShootAxis == MMInput.ButtonStates.ButtonDown)
                   || (_inputManager.SecondaryShootButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
                   || (_inputManager.SecondaryShootAxis == MMInput.ButtonStates.ButtonDown)
                   || _inputManager.ShootButton.ButtonDownRecently(0.08f)
                   || _inputManager.SecondaryShootButton.ButtonDownRecently(0.08f);
        }

        protected virtual bool IsRawShieldCancelAttackInputDown()
        {
            if (!UseRawShieldCancelAttackInput)
            {
                return false;
            }

            if (RawShieldCancelAttackKeys != null)
            {
                for (int i = 0; i < RawShieldCancelAttackKeys.Length; i++)
                {
                    if ((RawShieldCancelAttackKeys[i] != KeyCode.None) && Input.GetKeyDown(RawShieldCancelAttackKeys[i]))
                    {
                        return true;
                    }
                }
            }

            return RawButtonDown(GetPlayerButtonName("Shoot"))
                   || RawButtonDown(GetPlayerButtonName("SecondaryShoot"))
                   || RawButtonDown("Shoot")
                   || RawButtonDown("SecondaryShoot");
        }

        protected virtual string GetPlayerButtonName(string buttonName)
        {
            string playerID = (_character != null) ? _character.PlayerID : "Player1";
            return playerID + "_" + buttonName;
        }

        protected virtual bool RawButtonDown(string buttonName)
        {
            if (string.IsNullOrEmpty(buttonName))
            {
                return false;
            }

            try
            {
                return Input.GetButtonDown(buttonName);
            }
            catch (System.ArgumentException)
            {
                return false;
            }
        }

        protected virtual bool WantsToUseWeapon()
        {
            return IsWeaponAttackInputDown() || IsCurrentWeaponAttacking();
        }

        protected virtual bool IsCurrentWeaponAttacking()
        {
            if ((_characterHandleWeapon == null) || (_characterHandleWeapon.CurrentWeapon == null) || (_characterHandleWeapon.CurrentWeapon.WeaponState == null))
            {
                return false;
            }

            Weapon.WeaponStates state = _characterHandleWeapon.CurrentWeapon.WeaponState.CurrentState;
            return (state == Weapon.WeaponStates.WeaponStart)
                   || (state == Weapon.WeaponStates.WeaponDelayBeforeUse)
                   || (state == Weapon.WeaponStates.WeaponUse)
                   || (state == Weapon.WeaponStates.WeaponDelayBetweenUses);
        }

        protected virtual void StopShieldVfxGraph()
        {
            if (_shieldVfxHoldCoroutine != null)
            {
                StopCoroutine(_shieldVfxHoldCoroutine);
                _shieldVfxHoldCoroutine = null;
            }

            if (_shieldVfxGraph.IsValid())
            {
                _shieldVfxGraph.Destroy();
            }

            _shieldVfxPlayable = default(AnimationClipPlayable);
        }

        protected virtual void StopGroundAttackVfx()
        {
            if (_groundAttackVfxCoroutine != null)
            {
                StopCoroutine(_groundAttackVfxCoroutine);
                _groundAttackVfxCoroutine = null;
            }

            DestroyGroundAttackVfxInstances();
        }

        protected virtual void StopSpawnJumper()
        {
            if (_spawnJumperLiftCoroutine != null)
            {
                StopCoroutine(_spawnJumperLiftCoroutine);
                _spawnJumperLiftCoroutine = null;
            }

            StopSpawnJumperVfx();
        }

        protected virtual void StopSpawnJumperVfx()
        {
            if (_spawnJumperVfxGraph.IsValid())
            {
                _spawnJumperVfxGraph.Destroy();
            }

            if (_spawnJumperVfxObject != null)
            {
                Destroy(_spawnJumperVfxObject);
                _spawnJumperVfxObject = null;
            }
        }

        protected virtual void DestroyGroundAttackVfxInstances()
        {
            for (int i = 0; i < _groundAttackVfxGraphs.Count; i++)
            {
                if (_groundAttackVfxGraphs[i].IsValid())
                {
                    _groundAttackVfxGraphs[i].Destroy();
                }
            }
            _groundAttackVfxGraphs.Clear();

            for (int i = 0; i < _groundAttackVfxObjects.Count; i++)
            {
                if (_groundAttackVfxObjects[i] != null)
                {
                    Destroy(_groundAttackVfxObjects[i]);
                }
            }
            _groundAttackVfxObjects.Clear();
        }

        public override void ResetAbility()
        {
            base.ResetAbility();
            _spellCastProjectilePending = false;
            StopShieldVfx();
            StopGroundAttackVfx();
            StopSpawnJumper();
            StopSkillCameraShake();
            StopSplashCoroutines();
            StopSkillClip();
        }

        protected virtual void OnDisable()
        {
            if (_health != null)
            {
                RestoreSplashProtection();
            }
            _spellCastProjectilePending = false;
            StopShieldVfx();
            StopGroundAttackVfx();
            StopSpawnJumper();
            StopSkillCameraShake();
            StopSplashCoroutines();
            StopSkillClip();
        }

        protected virtual void StopSkillCameraShake()
        {
            if (_skillCameraShakeCoroutine != null)
            {
                StopCoroutine(_skillCameraShakeCoroutine);
                _skillCameraShakeCoroutine = null;
            }
        }

        protected virtual void StopSplashCoroutines()
        {
            if (_splashDamageCoroutine != null)
            {
                StopCoroutine(_splashDamageCoroutine);
                _splashDamageCoroutine = null;
            }

            if (_splashInvulnerabilityCoroutine != null)
            {
                StopCoroutine(_splashInvulnerabilityCoroutine);
                _splashInvulnerabilityCoroutine = null;
            }

            if (_health != null)
            {
                RestoreSplashProtection();
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            DrawSkillGizmos();
        }

        protected virtual void OnDrawGizmos()
        {
            DrawSkillGizmos();
        }

        protected virtual void DrawSkillGizmos()
        {
            float direction = GetGizmoFacingDirection();

            if (ShowSkillDamageGizmos)
            {
                DrawSkillDamageGizmo(GetSpellCastGizmoPosition(direction), SpellDamageAreaSize, SpellDamageGizmoColor);
                DrawSkillDamageGizmo(transform.position + new Vector3(MultiAttackDamageAreaOffset.x * direction, MultiAttackDamageAreaOffset.y, 0f), MultiAttackDamageAreaSize, MultiAttackDamageGizmoColor);
                DrawSkillDamageGizmo(transform.position + new Vector3(SplashAttackDamageAreaOffset.x * direction, SplashAttackDamageAreaOffset.y, 0f), SplashAttackDamageAreaSize, SplashAttackDamageGizmoColor);
            }

            if (ShowShieldGizmos)
            {
                Vector3 shieldCenter = transform.position + new Vector3(ShieldVfxOffset.x * direction, ShieldVfxOffset.y, 0f);
                DrawSkillDamageGizmo(shieldCenter, ShieldVfxVisualSize * Mathf.Max(0.01f, ShieldVfxScale), ShieldVfxGizmoColor);
                DrawSkillDamageGizmo(shieldCenter, ShieldProjectileBlockSize, ShieldProjectileBlockGizmoColor);
            }

            if (ShowGroundAttackVfxGizmos)
            {
                DrawGroundAttackVfxGizmos(direction);
            }

            if (ShowSpawnJumperGizmos)
            {
                DrawSpawnJumperGizmos(direction);
            }
        }

        protected virtual void DrawGroundAttackVfxGizmos(float direction)
        {
            int vfxCount = Mathf.Max(1, GroundAttackVfxCount);
            Vector2 visualSize = GroundAttackVfxVisualSize * Mathf.Max(0.01f, GroundAttackVfxScale);

            for (int i = 0; i < vfxCount; i++)
            {
                Vector3 center = transform.position + new Vector3(
                    (GroundAttackVfxOffset.x + GroundAttackVfxHorizontalSpacing * i) * direction,
                    GroundAttackVfxOffset.y,
                    0f);
                DrawSkillDamageGizmo(center, visualSize, GroundAttackVfxGizmoColor);
            }
        }

        protected virtual void DrawSpawnJumperGizmos(float direction)
        {
            Vector3 vfxCenter = transform.position + new Vector3(SpawnJumperVfxOffset.x * direction, SpawnJumperVfxOffset.y, 0f);
            Vector2 vfxSize = SpawnJumperVfxVisualSize * Mathf.Max(0.01f, SpawnJumperVfxScale);
            DrawSkillDamageGizmo(vfxCenter, vfxSize, SpawnJumperVfxGizmoColor);

            Vector3 liftCenter = transform.position + new Vector3(SpawnJumperLiftAreaOffset.x * direction, SpawnJumperLiftAreaOffset.y, 0f);
            DrawSkillDamageGizmo(liftCenter, SpawnJumperLiftAreaSize, SpawnJumperLiftGizmoColor);

#if UNITY_EDITOR
            Handles.Label(
                vfxCenter + Vector3.up * ((vfxSize.y * 0.5f) + 0.15f),
                $"Spawn Jumper VFX\nOffset {SpawnJumperVfxOffset} | Size {vfxSize}\nDuration {(SpawnJumperVfxDuration > 0f ? SpawnJumperVfxDuration : GetSpawnJumperVfxDuration(SpawnJumperVfxClip)):0.00}s");
            Handles.Label(
                liftCenter + Vector3.up * ((SpawnJumperLiftAreaSize.y * 0.5f) + 0.15f),
                $"Spawn Jumper Lift\nOffset {SpawnJumperLiftAreaOffset} | Size {SpawnJumperLiftAreaSize}\nDelay {SpawnJumperLiftDelay:0.00}s | Force {SpawnJumperLiftForce:0.00}");
#endif
        }

        protected virtual void DrawSkillDamageGizmo(Vector3 center, Vector2 size, Color color)
        {
            if ((size.x <= 0f) || (size.y <= 0f))
            {
                return;
            }

            Color previousColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, new Vector3(size.x, size.y, 0.01f));
            Gizmos.color = previousColor;
        }

        protected virtual Vector3 GetSpellCastGizmoPosition(float direction)
        {
            Transform castPoint = SpellCastPoint;
            if (castPoint == null)
            {
                castPoint = GetSpellCastPoint();
            }

            Vector3 origin = (castPoint != null) ? castPoint.position : transform.position;
            return origin + new Vector3(SpellDamageAreaOffset.x * direction, SpellDamageAreaOffset.y, 0f);
        }

        protected virtual float GetGizmoFacingDirection()
        {
            if (_character != null)
            {
                return _character.IsFacingRight ? 1f : -1f;
            }

            return (transform.lossyScale.x < 0f) ? -1f : 1f;
        }
    }

    public class RetroProjectileShieldBlocker : MonoBehaviour
    {
        protected GameObject _owner;
        protected LayerMask _projectileLayerMask;
        protected Vector2 _blockSize;
        protected float _endsAt;
        protected bool _dropBlockedProjectiles;
        protected float _blockedProjectileDropSpeed;
        protected float _blockedProjectileGroundRaycastDistance;
        protected float _blockedProjectileGroundOffset;
        protected float _blockedProjectileFadeDelay;
        protected float _blockedProjectileFadeDuration;
        protected LayerMask _blockedProjectileGroundLayerMask;

        public virtual void Initialize(
            GameObject owner,
            LayerMask projectileLayerMask,
            Vector2 blockSize,
            float duration,
            bool dropBlockedProjectiles,
            float blockedProjectileDropSpeed,
            float blockedProjectileGroundRaycastDistance,
            float blockedProjectileGroundOffset,
            float blockedProjectileFadeDelay,
            float blockedProjectileFadeDuration,
            LayerMask blockedProjectileGroundLayerMask)
        {
            _owner = owner;
            _projectileLayerMask = projectileLayerMask;
            _blockSize = blockSize;
            _endsAt = Time.time + Mathf.Max(0.01f, duration);
            _dropBlockedProjectiles = dropBlockedProjectiles;
            _blockedProjectileDropSpeed = Mathf.Max(0.01f, blockedProjectileDropSpeed);
            _blockedProjectileGroundRaycastDistance = Mathf.Max(0.01f, blockedProjectileGroundRaycastDistance);
            _blockedProjectileGroundOffset = Mathf.Max(0f, blockedProjectileGroundOffset);
            _blockedProjectileFadeDelay = Mathf.Max(0f, blockedProjectileFadeDelay);
            _blockedProjectileFadeDuration = Mathf.Max(0.01f, blockedProjectileFadeDuration);
            _blockedProjectileGroundLayerMask = blockedProjectileGroundLayerMask;
            CreateBlockerCollider();
        }

        protected virtual void Update()
        {
            if (Time.time >= _endsAt)
            {
                Destroy(gameObject);
                return;
            }

            BlockProjectiles();
        }

        protected virtual void BlockProjectiles()
        {
            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, _blockSize, transform.eulerAngles.z, _projectileLayerMask);
            for (int i = 0; i < hits.Length; i++)
            {
                Projectile projectile = hits[i].GetComponentInParent<Projectile>();
                if ((projectile == null) || IsOwnerProjectile(projectile))
                {
                    continue;
                }

                BlockProjectile(projectile);
            }
        }

        protected virtual void CreateBlockerCollider()
        {
            BoxCollider2D boxCollider = gameObject.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider2D>();
            }

            boxCollider.isTrigger = true;
            boxCollider.size = _blockSize;
            boxCollider.offset = Vector2.zero;

            Rigidbody2D rigidBody = gameObject.GetComponent<Rigidbody2D>();
            if (rigidBody == null)
            {
                rigidBody = gameObject.AddComponent<Rigidbody2D>();
            }

            rigidBody.bodyType = RigidbodyType2D.Kinematic;
            rigidBody.simulated = true;
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            TryBlockCollider(other);
        }

        protected virtual void OnTriggerStay2D(Collider2D other)
        {
            TryBlockCollider(other);
        }

        protected virtual void TryBlockCollider(Collider2D other)
        {
            if ((other == null) || (Time.time >= _endsAt) || !LayerMatches(other.gameObject.layer))
            {
                return;
            }

            Projectile projectile = other.GetComponentInParent<Projectile>();
            if ((projectile == null) || IsOwnerProjectile(projectile))
            {
                return;
            }

            BlockProjectile(projectile);
        }

        protected virtual bool LayerMatches(int layer)
        {
            return (_projectileLayerMask.value & (1 << layer)) != 0;
        }

        protected virtual void BlockProjectile(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            DamageOnTouch damageOnTouch = projectile.GetComponent<DamageOnTouch>();
            if (damageOnTouch != null)
            {
                damageOnTouch.enabled = false;
            }

            Collider2D[] colliders = projectile.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }

            if (_dropBlockedProjectiles)
            {
                RetroBlockedProjectileDrop drop = projectile.GetComponent<RetroBlockedProjectileDrop>();
                if (drop == null)
                {
                    drop = projectile.gameObject.AddComponent<RetroBlockedProjectileDrop>();
                }

                drop.Initialize(
                    projectile,
                    _blockedProjectileDropSpeed,
                    _blockedProjectileGroundRaycastDistance,
                    _blockedProjectileGroundOffset,
                    _blockedProjectileFadeDelay,
                    _blockedProjectileFadeDuration,
                    _blockedProjectileGroundLayerMask);
                return;
            }

            projectile.gameObject.SetActive(false);
        }

        protected virtual bool IsOwnerProjectile(Projectile projectile)
        {
            if ((_owner == null) || (projectile == null))
            {
                return false;
            }

            GameObject projectileOwner = projectile.GetOwner();
            return (projectileOwner == _owner) || ((projectileOwner != null) && projectileOwner.transform.IsChildOf(_owner.transform));
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, _blockSize);
        }
    }

    public class RetroBlockedProjectileDrop : MonoBehaviour
    {
        protected Projectile _projectile;
        protected SpriteRenderer[] _renderers;
        protected Color[] _initialColors;
        protected float _dropSpeed;
        protected float _targetY;
        protected float _fadeDelay;
        protected float _fadeDuration;
        protected float _fadeStartsAt;
        protected float _fadeEndsAt;
        protected bool _grounded;
        protected bool _initialized;

        public virtual void Initialize(
            Projectile projectile,
            float dropSpeed,
            float groundRaycastDistance,
            float groundOffset,
            float fadeDelay,
            float fadeDuration,
            LayerMask groundLayerMask)
        {
            _projectile = projectile;
            _dropSpeed = Mathf.Max(0.01f, dropSpeed);
            _fadeDelay = Mathf.Max(0f, fadeDelay);
            _fadeDuration = Mathf.Max(0.01f, fadeDuration);
            _renderers = GetComponentsInChildren<SpriteRenderer>();
            StoreInitialColors();

            if (_projectile != null)
            {
                _projectile.Speed = 0f;
                _projectile.enabled = false;
            }

            RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down, Mathf.Max(0.01f, groundRaycastDistance), groundLayerMask);
            _targetY = groundHit.collider != null ? groundHit.point.y + Mathf.Max(0f, groundOffset) : transform.position.y - 1f;
            _grounded = false;
            _initialized = true;
        }

        protected virtual void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (!_grounded)
            {
                Vector3 position = transform.position;
                position.y = Mathf.MoveTowards(position.y, _targetY, _dropSpeed * Time.deltaTime);
                transform.position = position;

                if (Mathf.Abs(transform.position.y - _targetY) <= 0.001f)
                {
                    _grounded = true;
                    _fadeStartsAt = Time.time + _fadeDelay;
                    _fadeEndsAt = _fadeStartsAt + _fadeDuration;
                }
                return;
            }

            if (Time.time < _fadeStartsAt)
            {
                return;
            }

            float fadeProgress = Mathf.InverseLerp(_fadeStartsAt, _fadeEndsAt, Time.time);
            SetAlpha(1f - fadeProgress);

            if (Time.time >= _fadeEndsAt)
            {
                RestoreAlpha();
                if (_projectile != null)
                {
                    _projectile.enabled = true;
                }
                _initialized = false;
                gameObject.SetActive(false);
            }
        }

        protected virtual void OnDisable()
        {
            RestoreAlpha();
            if (_projectile != null)
            {
                _projectile.enabled = true;
            }
            _initialized = false;
        }

        protected virtual void StoreInitialColors()
        {
            if (_renderers == null)
            {
                _initialColors = new Color[0];
                return;
            }

            _initialColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                _initialColors[i] = (_renderers[i] != null) ? _renderers[i].color : Color.white;
            }
        }

        protected virtual void SetAlpha(float alpha)
        {
            if ((_renderers == null) || (_initialColors == null))
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null)
                {
                    continue;
                }

                Color color = _initialColors[i];
                color.a *= Mathf.Clamp01(alpha);
                _renderers[i].color = color;
            }
        }

        protected virtual void RestoreAlpha()
        {
            if ((_renderers == null) || (_initialColors == null))
            {
                return;
            }

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].color = _initialColors[i];
                }
            }
        }
    }

}
