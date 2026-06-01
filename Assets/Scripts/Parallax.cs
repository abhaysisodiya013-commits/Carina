using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    public Transform cameraTarget;

    [Range(0f, 1f)]
    public float speed = 0.18f;

    [Range(0f, 1f)]
    public float verticalSpeed = 0.05f;

    [Header("Camera Movement")]
    [Tooltip("When enabled, this background only moves with the camera. When disabled, the old looping/recycling system is used.")]
    [SerializeField] private bool moveWithCameraParallax = true;
    [Range(0f, 1f)]
    [Tooltip("Kept for existing Inspector data. Camera parallax now uses Speed directly.")]
#pragma warning disable 0414
    [SerializeField] private float cameraFollowBoost = 0.15f;
#pragma warning restore 0414
    [Tooltip("When enabled, this background moves opposite the camera/player direction for a stronger passing-by parallax feel.")]
    [SerializeField] private bool moveBackwardOnScreen = true;
    [Tooltip("Keeps this parallax background inside the camera view so it can move without disappearing at screen edges.")]
#pragma warning disable 0414
    [SerializeField] private bool keepInsideCameraView = true;
#pragma warning restore 0414
    [SerializeField] private float viewEdgePadding = 0.05f;
    [Tooltip("Specific object names that should keep recycling while parallax is enabled so they never disappear.")]
    [SerializeField] private string[] protectedObjectNames = { "Mountain" };

    [Header("Looping")]
    [SerializeField] private Transform tilesRoot;
    [SerializeField] private bool autoArrangeTiles = true;
    [SerializeField] private bool preserveSceneLayoutOnStart = true;
    [SerializeField] private float recyclePadding = 0.5f;
    [SerializeField] private int maxRuntimeTiles = 5;
    [SerializeField] private float minimumCameraDelta = 0.0001f;

    private readonly List<Transform> tiles = new List<Transform>();
    private Vector3 lastCameraPosition;
    private float tileWidth;
    private float tileSpacing;
    private int headIndex;
    private bool initialized;
    private bool warnedAboutTiles;
    private Camera cameraComponent;
    private MoreMountains.CorgiEngine.ParallaxLayerOverride overrideSettings;

    private void OnEnable()
    {
        Camera.onPreCull += HandleCameraPreCull;
    }

    private void OnDisable()
    {
        Camera.onPreCull -= HandleCameraPreCull;
    }

    private void Start()
    {
        Initialize();
    }

    private void HandleCameraPreCull(Camera renderingCamera)
    {
        if (cameraComponent != null && renderingCamera != cameraComponent)
        {
            return;
        }

        UpdateParallax();
    }

    private void UpdateParallax()
    {
        if (!initialized)
        {
            Initialize();
        }

        if (!initialized || cameraTarget == null || tiles.Count == 0)
        {
            return;
        }

        Vector3 cameraDelta = cameraTarget.position - lastCameraPosition;
        if ((Mathf.Abs(cameraDelta.x) < minimumCameraDelta) && (Mathf.Abs(cameraDelta.y) < minimumCameraDelta))
        {
            return;
        }

        if (moveWithCameraParallax)
        {
            if (ShouldStayPut())
            {
                lastCameraPosition = cameraTarget.position;
                return;
            }

            Vector3 shift;
            if (ShouldFollowCameraOnly())
            {
                shift = new Vector3(cameraDelta.x, cameraDelta.y, 0f);
            }
            else
            {
                float horizontalFollow = Mathf.Clamp01(speed);
                float verticalFollow = Mathf.Clamp01(verticalSpeed);
                float horizontalDirection = moveBackwardOnScreen ? -1f : 1f;
                shift = new Vector3(cameraDelta.x * horizontalFollow * horizontalDirection, cameraDelta.y * verticalFollow, 0f);
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                Transform tile = tiles[i];
                tile.position += shift;
            }

            if (!ShouldIgnoreLooping() && tiles.Count >= 2)
            {
                RecycleTiles();
            }
        }

        if (!moveWithCameraParallax && !ShouldStayPut() && !ShouldIgnoreLooping() && tiles.Count >= 2)
        {
            RecycleTiles();
        }
        lastCameraPosition = cameraTarget.position;
    }

    private void Initialize()
    {
        if (cameraTarget == null && Camera.main != null)
        {
            cameraComponent = Camera.main;
            cameraTarget = cameraComponent.transform;
        }
        else if (cameraComponent == null && cameraTarget != null)
        {
            cameraComponent = cameraTarget.GetComponent<Camera>();
        }

        overrideSettings = GetComponent<MoreMountains.CorgiEngine.ParallaxLayerOverride>();
        Transform root = tilesRoot != null ? tilesRoot : transform;

        tiles.Clear();

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererComponent = renderers[i];
            if (IsValidParallaxRenderer(rendererComponent))
            {
                tiles.Add(rendererComponent.transform);
            }
        }

        Renderer rootRenderer = root.GetComponent<Renderer>();
        if (tiles.Count == 0 && IsValidParallaxRenderer(rootRenderer))
        {
            tiles.Add(root);
        }

        if (tiles.Count < 2 && !moveWithCameraParallax)
        {
            if (!warnedAboutTiles)
            {
                Debug.LogWarning($"{nameof(Parallax)} on {name} needs at least 2 tiles in the current setup to recycle endlessly.", this);
                warnedAboutTiles = true;
            }

            initialized = false;
            return;
        }

        tiles.Sort(CompareByWorldX);

        tileWidth = GetTileWidth(tiles[0]);
        if (tileWidth <= 0f)
        {
            Debug.LogWarning($"{nameof(Parallax)} on {name} detected a zero-width tile.", this);
            initialized = false;
            return;
        }

        tileSpacing = GetTileSpacing(tiles, tileWidth);

        if (autoArrangeTiles && !preserveSceneLayoutOnStart)
        {
            AlignTiles();
            tileSpacing = tileWidth;
        }

        headIndex = 0;
        lastCameraPosition = cameraTarget != null ? cameraTarget.position : Vector3.zero;

        if (!ShouldStayPut() && !ShouldIgnoreLooping() && tiles.Count >= 1)
        {
            EnsureEnoughTiles();
        }
        initialized = true;
    }

    private bool ShouldIgnoreLooping()
    {
        return overrideSettings != null && (overrideSettings.NoParallaxStayPut || !overrideSettings.Loop || overrideSettings.IgnoreLooping);
    }

    private bool ShouldFollowCameraOnly()
    {
        return overrideSettings != null && (overrideSettings.StayWithCamera || overrideSettings.MoveWithCameraOnly);
    }

    private bool ShouldStayPut()
    {
        return overrideSettings != null && overrideSettings.NoParallaxStayPut;
    }

    private void KeepInsideCameraView()
    {
        if (cameraTarget == null || tiles.Count == 0)
        {
            return;
        }

        float layerMinX;
        float layerMaxX;
        if (!TryGetLayerBounds(out layerMinX, out layerMaxX))
        {
            return;
        }

        float halfViewWidth = GetCameraViewWidth() * 0.5f;
        float viewMinX = cameraTarget.position.x - halfViewWidth + viewEdgePadding;
        float viewMaxX = cameraTarget.position.x + halfViewWidth - viewEdgePadding;
        float layerWidth = layerMaxX - layerMinX;
        float viewWidth = viewMaxX - viewMinX;
        float offsetX = 0f;

        if (layerWidth <= viewWidth)
        {
            return;
        }

        if (layerMinX > viewMinX)
        {
            offsetX = viewMinX - layerMinX;
        }
        else if (layerMaxX < viewMaxX)
        {
            offsetX = viewMaxX - layerMaxX;
        }

        if (Mathf.Abs(offsetX) < minimumCameraDelta)
        {
            return;
        }

        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            if (tile != null)
            {
                tile.position += new Vector3(offsetX, 0f, 0f);
            }
        }
    }

    private void KeepProtectedObjectsInsideCameraView()
    {
        if (protectedObjectNames == null || protectedObjectNames.Length == 0)
        {
            return;
        }

        float halfViewWidth = GetCameraViewWidth() * 0.5f;
        float viewMinX = cameraTarget.position.x - halfViewWidth + viewEdgePadding;
        float viewMaxX = cameraTarget.position.x + halfViewWidth - viewEdgePadding;

        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            if (tile == null || !IsProtectedName(tile.name))
            {
                continue;
            }

            float tileMinX = GetLeftEdge(tile);
            float tileMaxX = GetRightEdge(tile);
            float tileWidth = tileMaxX - tileMinX;
            float viewWidth = viewMaxX - viewMinX;
            float offsetX = 0f;

            if (tileWidth <= viewWidth)
            {
                if (tileMinX < viewMinX)
                {
                    offsetX = viewMinX - tileMinX;
                }
                else if (tileMaxX > viewMaxX)
                {
                    offsetX = viewMaxX - tileMaxX;
                }
            }
            else if (tileMinX > viewMinX)
            {
                offsetX = viewMinX - tileMinX;
            }
            else if (tileMaxX < viewMaxX)
            {
                offsetX = viewMaxX - tileMaxX;
            }

            if (Mathf.Abs(offsetX) >= minimumCameraDelta)
            {
                tile.position += new Vector3(offsetX, 0f, 0f);
            }
        }
    }

    private bool IsProtectedName(string objectName)
    {
        string groupName = GetObjectGroupName(objectName);
        for (int i = 0; i < protectedObjectNames.Length; i++)
        {
            string protectedName = protectedObjectNames[i];
            if (!string.IsNullOrEmpty(protectedName) && string.Equals(groupName, protectedName, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetObjectGroupName(string objectName)
    {
        int suffixStart = objectName.LastIndexOf(" (");
        if (!objectName.EndsWith(")") || suffixStart < 0)
        {
            return objectName;
        }

        string suffix = objectName.Substring(suffixStart + 2, objectName.Length - suffixStart - 3);
        for (int i = 0; i < suffix.Length; i++)
        {
            if (!char.IsDigit(suffix[i]))
            {
                return objectName;
            }
        }

        return objectName.Substring(0, suffixStart);
    }

    private bool TryGetLayerBounds(out float minX, out float maxX)
    {
        minX = float.PositiveInfinity;
        maxX = float.NegativeInfinity;

        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            if (tile == null)
            {
                continue;
            }

            minX = Mathf.Min(minX, GetLeftEdge(tile));
            maxX = Mathf.Max(maxX, GetRightEdge(tile));
        }

        return maxX > minX;
    }

    private void EnsureEnoughTiles()
    {
        float cameraWidth = GetCameraViewWidth();
        if (cameraWidth <= 0f || tiles.Count < 1)
        {
            return;
        }

        float requiredCoverage = cameraWidth + (tileWidth * 1.5f) + (recyclePadding * 2f);
        int safetyLimit = Mathf.Max(2, maxRuntimeTiles);

        while (GetLayerCoverage() < requiredCoverage && tiles.Count < safetyLimit)
        {
            Transform rightMost = tiles[tiles.Count - 1];
            if (rightMost == null)
            {
                return;
            }

            Transform clone = Instantiate(rightMost, rightMost.parent);
            clone.name = $"{rightMost.name} Loop";
            MoveTileLeftEdgeTo(clone, GetRightEdge(rightMost));
            tiles.Add(clone);
        }
    }

    private void AlignTiles()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            if (i == 0)
            {
                continue;
            }

            MoveTileLeftEdgeTo(tile, GetRightEdge(tiles[i - 1]));
        }
    }

    private void RecycleTiles()
    {
        float halfViewWidth = GetCameraViewWidth() * 0.5f;
        float leftBound = cameraTarget.position.x - halfViewWidth - recyclePadding;
        float rightBound = cameraTarget.position.x + halfViewWidth + recyclePadding;
        int tileCount = tiles.Count;

        for (int safety = 0; safety < tileCount; safety++)
        {
            Transform leftMost = tiles[headIndex];
            Transform rightMost = tiles[(headIndex + tileCount - 1) % tileCount];

            if (leftMost == null || rightMost == null)
            {
                return;
            }

            bool recycled = false;

            if (GetRightEdge(leftMost) < leftBound)
            {
                MoveTileLeftEdgeTo(leftMost, GetRightEdge(rightMost));
                headIndex = (headIndex + 1) % tileCount;
                recycled = true;
                continue;
            }

            if (GetLeftEdge(rightMost) > rightBound)
            {
                MoveTileRightEdgeTo(rightMost, GetLeftEdge(leftMost));
                headIndex = (headIndex - 1 + tileCount) % tileCount;
                recycled = true;
            }

            if (!recycled)
            {
                break;
            }
        }
    }

    private float GetCameraViewWidth()
    {
        if (cameraComponent == null)
        {
            return tileWidth * 2f;
        }

        if (cameraComponent.orthographic)
        {
            return cameraComponent.orthographicSize * 2f * cameraComponent.aspect;
        }

        float distance = Mathf.Abs(cameraComponent.transform.position.z - tiles[headIndex].position.z);
        Vector3 left = cameraComponent.ViewportToWorldPoint(new Vector3(0f, 0.5f, distance));
        Vector3 right = cameraComponent.ViewportToWorldPoint(new Vector3(1f, 0.5f, distance));
        return Mathf.Abs(right.x - left.x);
    }

    private static float GetTileWidth(Transform tile)
    {
        Renderer rendererComponent = tile.GetComponent<Renderer>();
        return rendererComponent != null ? rendererComponent.bounds.size.x : 0f;
    }

    private static bool IsValidParallaxRenderer(Renderer rendererComponent)
    {
        if (rendererComponent == null)
        {
            return false;
        }

        return rendererComponent.GetComponentInParent<MoreMountains.CorgiEngine.Character>() == null;
    }

    private static float GetTileSpacing(List<Transform> sortedTiles, float fallbackWidth)
    {
        if (sortedTiles.Count < 2)
        {
            return fallbackWidth;
        }

        float spacing = 0f;
        for (int i = 1; i < sortedTiles.Count; i++)
        {
            spacing += Mathf.Abs(sortedTiles[i].position.x - sortedTiles[i - 1].position.x);
        }

        spacing /= sortedTiles.Count - 1;
        return spacing > 0f ? spacing : fallbackWidth;
    }

    private float GetLayerCoverage()
    {
        if (tiles.Count == 0)
        {
            return 0f;
        }

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        for (int i = 0; i < tiles.Count; i++)
        {
            Transform tile = tiles[i];
            if (tile == null)
            {
                continue;
            }

            minX = Mathf.Min(minX, GetLeftEdge(tile));
            maxX = Mathf.Max(maxX, GetRightEdge(tile));
        }

        return maxX > minX ? maxX - minX : 0f;
    }

    private static float GetLeftEdge(Transform tile)
    {
        Renderer rendererComponent = tile.GetComponent<Renderer>();
        return rendererComponent != null ? rendererComponent.bounds.min.x : tile.position.x;
    }

    private static float GetRightEdge(Transform tile)
    {
        Renderer rendererComponent = tile.GetComponent<Renderer>();
        return rendererComponent != null ? rendererComponent.bounds.max.x : tile.position.x;
    }

    private static void MoveTileLeftEdgeTo(Transform tile, float targetLeftEdge)
    {
        float offset = targetLeftEdge - GetLeftEdge(tile);
        tile.position += new Vector3(offset, 0f, 0f);
    }

    private static void MoveTileRightEdgeTo(Transform tile, float targetRightEdge)
    {
        float offset = targetRightEdge - GetRightEdge(tile);
        tile.position += new Vector3(offset, 0f, 0f);
    }

    private static int CompareByWorldX(Transform a, Transform b)
    {
        return a.position.x.CompareTo(b.position.x);
    }
}
