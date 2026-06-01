using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using UnityEngine;

[AddComponentMenu("Corgi Engine/Character/Abilities/Retro Dash After Image")]
public class RetroDashAfterImage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sourceRenderer;

    [Header("Dash After Image")]
    [SerializeField] private int afterImageCount = 4;
    [SerializeField] private float afterImageGap = 0.65f;
    [SerializeField] private float afterImageVisibility = 0.82f;
    [SerializeField] private float trailingVisibilityFalloff = 0.88f;
    [SerializeField] private float refreshInterval = 0.016f;
    [SerializeField] private float fadeOutDuration = 0.22f;
    [SerializeField] private float fadeOutDelayBetweenImages = 0.045f;
    [SerializeField] private Color afterImageTint = new Color(0.75f, 0.95f, 1f, 1f);

    [Header("Gizmos")]
    [SerializeField] private bool drawBottomMatchGizmo = true;
    [SerializeField] private bool drawGizmoOnlyWhenSelected = false;
    [SerializeField] private Color bottomMatchGizmoColor = new Color(0f, 1f, 0.35f, 0.9f);
    [SerializeField] private Color afterImageBoundsGizmoColor = new Color(0f, 0.75f, 1f, 0.35f);
    [SerializeField] private Color playerBottomGizmoColor = new Color(1f, 0.75f, 0f, 0.9f);

    private sealed class AfterImage
    {
        public GameObject Root;
        public SpriteRenderer Renderer;
        public Color DashColor;
        public float FadeAge;
        public float FadeDelay;
        public bool Fading;
    }

    private struct TrailSample
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public Sprite Sprite;
        public bool FlipX;
        public bool FlipY;
    }

    private readonly List<AfterImage> _afterImages = new List<AfterImage>();
    private readonly List<TrailSample> _trailSamples = new List<TrailSample>();
    private Character _character;
    private float _lastRefreshTime;
    private bool _wasDashing;

    private void Awake()
    {
        CacheReferences();
    }

    private void LateUpdate()
    {
        bool dashing = IsDashing();

        if (!dashing)
        {
            if (_wasDashing)
            {
                StartAfterImageFadeOut();
                _trailSamples.Clear();
            }

            UpdateFadeOut();
            _wasDashing = false;
            return;
        }

        _wasDashing = true;

        if ((sourceRenderer == null) || (sourceRenderer.sprite == null))
        {
            return;
        }

        if ((Time.time - _lastRefreshTime) < refreshInterval)
        {
            return;
        }

        RecordTrailSample();
        RefreshDashTrail();
        _lastRefreshTime = Time.time;
    }

    private void CacheReferences()
    {
        _character = GetComponent<Character>();

        if ((sourceRenderer == null) && (_character != null) && (_character.CharacterModel != null))
        {
            sourceRenderer = _character.CharacterModel.GetComponent<SpriteRenderer>();
        }

        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();
        }

    }

    private bool IsDashing()
    {
        return (_character != null)
               && (_character.MovementState != null)
               && (_character.MovementState.CurrentState == CharacterStates.MovementStates.Dashing);
    }

    private void RefreshDashTrail()
    {
        int activeCount = Mathf.Max(0, afterImageCount);
        EnsureAfterImageCount(activeCount);

        for (int i = 0; i < _afterImages.Count; i++)
        {
            if (i >= activeCount)
            {
                _afterImages[i].Root.SetActive(false);
                continue;
            }

            ConfigureAfterImage(_afterImages[i], i);
        }
    }

    private void ConfigureAfterImage(AfterImage afterImage, int index)
    {
        TrailSample sample = GetTrailSample(index + 1);
        Transform rootTransform = afterImage.Root.transform;

        rootTransform.position = sample.Position;
        rootTransform.rotation = sample.Rotation;
        rootTransform.localScale = sample.Scale;

        afterImage.Renderer.sprite = sample.Sprite;
        afterImage.Renderer.flipX = sample.FlipX;
        afterImage.Renderer.flipY = sample.FlipY;
        afterImage.Renderer.sharedMaterial = sourceRenderer.sharedMaterial;
        afterImage.Renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        afterImage.Renderer.sortingOrder = sourceRenderer.sortingOrder - 1 - index;
        afterImage.DashColor = GetAfterImageColor(Mathf.Pow(trailingVisibilityFalloff, index));
        afterImage.Renderer.color = afterImage.DashColor;
        afterImage.FadeAge = 0f;
        afterImage.Fading = false;

        afterImage.Root.SetActive(true);
    }

    private void RecordTrailSample()
    {
        TrailSample sample = new TrailSample
        {
            Position = sourceRenderer.transform.position,
            Rotation = sourceRenderer.transform.rotation,
            Scale = sourceRenderer.transform.lossyScale,
            Sprite = sourceRenderer.sprite,
            FlipX = sourceRenderer.flipX,
            FlipY = sourceRenderer.flipY
        };

        if ((_trailSamples.Count > 0) && (Vector3.Distance(_trailSamples[0].Position, sample.Position) < 0.01f))
        {
            _trailSamples[0] = sample;
        }
        else
        {
            _trailSamples.Insert(0, sample);
        }

        int maxSamples = Mathf.Max(8, afterImageCount * 8);
        while (_trailSamples.Count > maxSamples)
        {
            _trailSamples.RemoveAt(_trailSamples.Count - 1);
        }
    }

    private TrailSample GetTrailSample(int gapIndex)
    {
        float targetDistance = Mathf.Max(0.01f, afterImageGap) * gapIndex;
        float traveledDistance = 0f;

        if (_trailSamples.Count == 0)
        {
            return GetFallbackTrailSample(gapIndex);
        }

        for (int i = 1; i < _trailSamples.Count; i++)
        {
            TrailSample newer = _trailSamples[i - 1];
            TrailSample older = _trailSamples[i];
            float segmentDistance = Vector3.Distance(newer.Position, older.Position);

            if (traveledDistance + segmentDistance >= targetDistance)
            {
                float segmentT = segmentDistance > 0.0001f
                    ? (targetDistance - traveledDistance) / segmentDistance
                    : 0f;

                older.Position = Vector3.Lerp(newer.Position, older.Position, segmentT);
                older.Rotation = Quaternion.Lerp(newer.Rotation, older.Rotation, segmentT);
                older.Scale = Vector3.Lerp(newer.Scale, older.Scale, segmentT);
                return older;
            }

            traveledDistance += segmentDistance;
        }

        return GetExtrapolatedTrailSample(targetDistance, traveledDistance);
    }

    private TrailSample GetExtrapolatedTrailSample(float targetDistance, float traveledDistance)
    {
        TrailSample sample = _trailSamples[_trailSamples.Count - 1];
        Vector3 trailDirection = GetFallbackTrailDirection();

        if (_trailSamples.Count > 1)
        {
            Vector3 sampledDirection = (_trailSamples[_trailSamples.Count - 1].Position - _trailSamples[0].Position);
            if (sampledDirection.sqrMagnitude > 0.0001f)
            {
                trailDirection = sampledDirection.normalized;
            }
        }

        sample.Position += trailDirection * Mathf.Max(0f, targetDistance - traveledDistance);
        return sample;
    }

    private TrailSample GetFallbackTrailSample(int gapIndex)
    {
        return new TrailSample
        {
            Position = GetFallbackAfterImagePosition(gapIndex),
            Rotation = sourceRenderer != null ? sourceRenderer.transform.rotation : transform.rotation,
            Scale = sourceRenderer != null ? sourceRenderer.transform.lossyScale : transform.lossyScale,
            Sprite = sourceRenderer != null ? sourceRenderer.sprite : null,
            FlipX = sourceRenderer != null && sourceRenderer.flipX,
            FlipY = sourceRenderer != null && sourceRenderer.flipY
        };
    }

    private void EnsureAfterImageCount(int count)
    {
        while (_afterImages.Count < count)
        {
            _afterImages.Add(CreateAfterImage());
        }
    }

    private AfterImage CreateAfterImage()
    {
        GameObject root = new GameObject("CorgiDashAfterImage");
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        AfterImage afterImage = new AfterImage
        {
            Root = root,
            Renderer = renderer
        };
        root.SetActive(false);
        return afterImage;
    }

    private void StartAfterImageFadeOut()
    {
        for (int i = 0; i < _afterImages.Count; i++)
        {
            if (!_afterImages[i].Root.activeSelf)
            {
                continue;
            }

            _afterImages[i].FadeAge = 0f;
            _afterImages[i].FadeDelay = Mathf.Max(0f, fadeOutDelayBetweenImages) * Mathf.Max(0, (afterImageCount - 1) - i);
            _afterImages[i].Fading = true;
            _afterImages[i].DashColor = _afterImages[i].Renderer.color;
        }
    }

    private void UpdateFadeOut()
    {
        float duration = Mathf.Max(0.01f, fadeOutDuration);

        for (int i = 0; i < _afterImages.Count; i++)
        {
            AfterImage afterImage = _afterImages[i];
            if (!afterImage.Root.activeSelf || !afterImage.Fading)
            {
                continue;
            }

            afterImage.FadeAge += Time.deltaTime;
            if (afterImage.FadeAge < afterImage.FadeDelay)
            {
                continue;
            }

            float t = Mathf.Clamp01((afterImage.FadeAge - afterImage.FadeDelay) / duration);
            Color color = afterImage.DashColor;
            color.a = Mathf.Lerp(afterImage.DashColor.a, 0f, t);
            afterImage.Renderer.color = color;

            if (t >= 1f)
            {
                afterImage.Fading = false;
                afterImage.Root.SetActive(false);
            }
        }
    }

    private Color GetAfterImageColor(float alphaMultiplier)
    {
        Color sourceColor = sourceRenderer != null ? sourceRenderer.color : Color.white;
        sourceColor.r *= afterImageTint.r;
        sourceColor.g *= afterImageTint.g;
        sourceColor.b *= afterImageTint.b;
        sourceColor.a *= Mathf.Clamp01(afterImageVisibility) * alphaMultiplier;
        return sourceColor;
    }

    private Vector3 GetFallbackAfterImagePosition(int gapIndex)
    {
        if (sourceRenderer == null)
        {
            return transform.position;
        }

        Vector3 position = sourceRenderer.transform.position;
        position += GetFallbackTrailDirection() * afterImageGap * gapIndex;
        return position;
    }

    private Vector3 GetFallbackTrailDirection()
    {
        return Vector3.left * GetFacingDirection();
    }

    private float GetFacingDirection()
    {
        if (_character != null)
        {
            return _character.IsFacingRight ? 1f : -1f;
        }

        return transform.lossyScale.x >= 0f ? 1f : -1f;
    }

    private void OnDrawGizmosSelected()
    {
        DrawBottomMatchGizmo();
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmoOnlyWhenSelected)
        {
            DrawBottomMatchGizmo();
        }
    }

    private void DrawBottomMatchGizmo()
    {
        if (!drawBottomMatchGizmo)
        {
            return;
        }

        if (sourceRenderer == null)
        {
            CacheReferences();
        }

        if (sourceRenderer == null)
        {
            return;
        }

        Bounds sourceBounds = sourceRenderer.bounds;
        Vector3 size = sourceBounds.size;
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        float playerBottomY = sourceBounds.min.y;

        int gizmoCount = Mathf.Max(1, afterImageCount);
        for (int i = 0; i < gizmoCount; i++)
        {
            Vector3 center = Application.isPlaying && (_trailSamples.Count > 0)
                ? GetTrailSample(i + 1).Position
                : GetFallbackAfterImagePosition(i + 1);
            float afterImageBottomY = center.y - halfHeight;
            Vector3 bottomLeft = new Vector3(center.x - halfWidth, afterImageBottomY, center.z);
            Vector3 bottomRight = new Vector3(center.x + halfWidth, afterImageBottomY, center.z);
            Vector3 topLeft = new Vector3(center.x - halfWidth, center.y + halfHeight, center.z);
            Vector3 topRight = new Vector3(center.x + halfWidth, center.y + halfHeight, center.z);

            Gizmos.color = afterImageBoundsGizmoColor;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);

            Gizmos.color = bottomMatchGizmoColor;
            Gizmos.DrawLine(bottomLeft, bottomRight);
        }

        Gizmos.color = playerBottomGizmoColor;
        Gizmos.DrawLine(
            new Vector3(sourceBounds.min.x, playerBottomY, sourceBounds.center.z),
            new Vector3(sourceBounds.max.x, playerBottomY, sourceBounds.center.z));
    }
}
