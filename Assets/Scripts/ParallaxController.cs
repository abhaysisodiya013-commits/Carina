using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    public class ParallaxLayerOverride : MonoBehaviour
    {
        [Tooltip("When enabled, this single background layer can recycle/loop normally.")]
        public bool Loop = true;

        [Tooltip("When enabled, this single background follows the camera and does not drift backward.")]
        public bool StayWithCamera;

        [Tooltip("When enabled, this single background does not move, parallax, or loop at runtime.")]
        public bool NoParallaxStayPut;

        [Tooltip("Stops this background layer from recycling/looping.")]
        [HideInInspector]
        public bool IgnoreLooping;

        [Tooltip("Makes this background follow the camera exactly, with no backward/parallax drift.")]
        [HideInInspector]
        public bool MoveWithCameraOnly;
    }

    [AddComponentMenu("Corgi Engine/Camera/Parallax Controller")]
    public class ParallaxController : MonoBehaviour
    {
        private class BackgroundLayer
        {
            public readonly List<Transform> Tiles = new List<Transform>();
            public float TileWidth;
            public float TileSpacing;
            public float LayerFactor;
            public float VerticalLayerFactor;
            public int HeadIndex;
            public Transform Reference;
            public int LayerIndex;
            public string LayerName;
            public ParallaxLayerOverride OverrideSettings;
        }

        [Range(0f, 1f)]
        public float parallaxSpeed = 0.22f;

        [Range(0f, 1f)]
        public float verticalParallaxSpeed = 0.08f;

        [Header("Camera Movement")]
        [Tooltip("When enabled, background layers only move with the camera at different speeds. When disabled, the old looping/recycling system is used.")]
        [SerializeField] private bool moveWithCameraParallax = true;
        [Range(0f, 1f)]
        [Tooltip("Kept for existing Inspector data. Depth-based parallax now uses Parallax Speed as the strength control.")]
#pragma warning disable 0414
        [SerializeField] private float cameraFollowBoost = 0.15f;
#pragma warning restore 0414
        [Tooltip("When enabled, backgrounds move opposite the camera/player direction for a stronger passing-by parallax feel.")]
        [SerializeField] private bool moveBackwardOnScreen = true;
        [Tooltip("Keeps each parallax layer inside the camera view so it can move without disappearing at screen edges.")]
#pragma warning disable 0414
        [SerializeField] private bool keepLayersInsideCameraView = true;
#pragma warning restore 0414
        [SerializeField] private float viewEdgePadding = 0.05f;
        [Tooltip("Specific layer/object names that should keep recycling while parallax is enabled so they never disappear.")]
        [SerializeField] private string[] protectedObjectNames = { "Mountain" };

        [Header("Looping")]
        [SerializeField] private bool autoArrangeTiles = true;
        [SerializeField] private bool preserveSceneLayoutOnStart = true;

        [Header("Layer Speeds")]
        [SerializeField] private float farLayerFactor = 0.02f;
        [SerializeField] private float nearLayerFactor = 0.28f;
        [SerializeField] private float farVerticalLayerFactor = 0.01f;
        [SerializeField] private float nearVerticalLayerFactor = 0.12f;
        [SerializeField] private bool separateByHierarchyOrder = true;
        [Range(0f, 1f)]
        [SerializeField] private float hierarchyWeight = 1f;
        [SerializeField] private float recyclePadding = 0.5f;
        [SerializeField] private int maxRuntimeTilesPerLayer = 5;
        [SerializeField] private float minimumCameraDelta = 0.0001f;

        private readonly List<BackgroundLayer> layers = new List<BackgroundLayer>();
        private Transform cam;
        private Camera camComponent;
        private Vector3 previousCameraPosition;
        private bool initialized;
        private bool warnedAboutTiles;

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
            if (camComponent != null && renderingCamera != camComponent)
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

            if (!initialized || cam == null)
            {
                return;
            }

            Vector3 cameraDelta = cam.position - previousCameraPosition;
            if ((Mathf.Abs(cameraDelta.x) < minimumCameraDelta) && (Mathf.Abs(cameraDelta.y) < minimumCameraDelta))
            {
                return;
            }

            float cameraX = cam.position.x;
            float halfViewWidth = GetCameraViewWidth() * 0.5f;

            for (int i = 0; i < layers.Count; i++)
            {
                BackgroundLayer layer = layers[i];
                if (layer.Tiles.Count == 0)
                {
                    continue;
                }

                if (moveWithCameraParallax)
                {
                    if (ShouldStayPut(layer))
                    {
                        continue;
                    }

                    Vector3 shift;
                    if (ShouldFollowCameraOnly(layer))
                    {
                        shift = new Vector3(cameraDelta.x, cameraDelta.y, 0f);
                    }
                    else
                    {
                        float horizontalFollow = Mathf.Clamp01(layer.LayerFactor * parallaxSpeed);
                        float verticalFollow = verticalParallaxSpeed <= 0f
                            ? 0f
                            : Mathf.Clamp01(layer.VerticalLayerFactor * verticalParallaxSpeed);
                        float horizontalDirection = moveBackwardOnScreen ? -1f : 1f;
                        shift = new Vector3(
                            cameraDelta.x * horizontalFollow * horizontalDirection,
                            cameraDelta.y * verticalFollow,
                            0f);
                    }

                    for (int j = 0; j < layer.Tiles.Count; j++)
                    {
                        Transform tile = layer.Tiles[j];
                        if (tile != null)
                        {
                            tile.position += shift;
                        }
                    }

                    if (!ShouldIgnoreLooping(layer) && layer.Tiles.Count >= 2)
                    {
                        RecycleLayer(layer, cameraX, halfViewWidth);
                    }
                }

                if (!moveWithCameraParallax && !ShouldStayPut(layer) && !ShouldIgnoreLooping(layer) && layer.Tiles.Count >= 2)
                {
                    RecycleLayer(layer, cameraX, halfViewWidth);
                }
            }

            previousCameraPosition = cam.position;
        }

        private void Initialize()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            camComponent = mainCamera;
            cam = mainCamera.transform;
            previousCameraPosition = cam.position;
            layers.Clear();

            float nearestDistance = float.PositiveInfinity;
            float farthestDistance = 0f;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform layerRoot = transform.GetChild(i);
                DisableNestedParallax(layerRoot);

                Transform reference = FindReferenceTile(layerRoot);
                if (reference == null)
                {
                    continue;
                }

                float distanceFromCamera = Mathf.Abs(reference.position.z - cam.position.z);
                nearestDistance = Mathf.Min(nearestDistance, distanceFromCamera);
                farthestDistance = Mathf.Max(farthestDistance, distanceFromCamera);
            }

            if (float.IsPositiveInfinity(nearestDistance))
            {
                nearestDistance = 0f;
                farthestDistance = 1f;
            }

            bool foundLoopableLayer = false;
            List<string> validLayerGroups = GetValidLayerGroups();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform layerRoot = transform.GetChild(i);
                int layerGroupIndex = GetLayerGroupIndex(validLayerGroups, layerRoot.name);
                BackgroundLayer layer = BuildLayer(layerRoot, nearestDistance, farthestDistance, layerGroupIndex, validLayerGroups.Count);
                if (layer == null)
                {
                    continue;
                }

                if (layer.Tiles.Count >= 2)
                {
                    foundLoopableLayer = true;
                }

                layers.Add(layer);
            }

            if (!foundLoopableLayer && !warnedAboutTiles)
            {
                Debug.LogWarning($"{nameof(ParallaxController)} on {name} needs each layer in the current setup to have at least 2 tiles placed side by side.", this);
                warnedAboutTiles = true;
            }

            initialized = layers.Count > 0;
        }

        private static void DisableNestedParallax(Transform layerRoot)
        {
            Parallax[] nestedParallaxComponents = layerRoot.GetComponentsInChildren<Parallax>(true);
            for (int i = 0; i < nestedParallaxComponents.Length; i++)
            {
                Parallax nestedParallax = nestedParallaxComponents[i];
                if (nestedParallax != null && nestedParallax.enabled)
                {
                    nestedParallax.enabled = false;
                }
            }
        }

        private BackgroundLayer BuildLayer(Transform layerRoot, float nearestDistance, float farthestDistance, int layerIndex, int totalLayerCount)
        {
            List<Transform> foundTiles = new List<Transform>();

            Renderer[] renderers = layerRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rendererComponent = renderers[i];
                if (IsValidParallaxRenderer(rendererComponent, layerRoot))
                {
                    foundTiles.Add(rendererComponent.transform);
                }
            }

            Renderer rootRenderer = layerRoot.GetComponent<Renderer>();
            if (foundTiles.Count == 0 && IsValidParallaxRenderer(rootRenderer, null))
            {
                foundTiles.Add(layerRoot);
            }

            if (foundTiles.Count == 0)
            {
                return null;
            }

            foundTiles.Sort(CompareByWorldX);

            float tileWidth = GetTileWidth(foundTiles[0]);
            if (tileWidth <= 0f)
            {
                return null;
            }

            float tileSpacing = GetTileSpacing(foundTiles, tileWidth);

            if (autoArrangeTiles && !preserveSceneLayoutOnStart)
            {
                AlignTiles(foundTiles, tileWidth);
                tileSpacing = tileWidth;
            }

            BackgroundLayer layer = new BackgroundLayer();
            layer.Reference = foundTiles[0];
            layer.TileWidth = tileWidth;
            layer.TileSpacing = tileSpacing;
            layer.HeadIndex = 0;
            layer.LayerIndex = layerIndex;
            layer.LayerName = GetLayerGroupName(layerRoot.name);
            layer.OverrideSettings = layerRoot.GetComponent<ParallaxLayerOverride>();

            float layerDistance = Mathf.Abs(layer.Reference.position.z - cam.position.z);
            bool hasDepthRange = Mathf.Abs(farthestDistance - nearestDistance) > 0.01f;
            float normalizedDepth = hasDepthRange
                ? 1f - Mathf.InverseLerp(nearestDistance, farthestDistance, layerDistance)
                : 0f;
            float normalizedOrder = totalLayerCount > 1 ? (float)layerIndex / (totalLayerCount - 1) : 0f;
            float hierarchySeparation = separateByHierarchyOrder ? Mathf.Lerp(0f, normalizedOrder, hierarchyWeight) : 0f;
            float separation = hasDepthRange ? normalizedDepth : hierarchySeparation;

            layer.LayerFactor = Mathf.Lerp(farLayerFactor, nearLayerFactor, separation);
            layer.VerticalLayerFactor = Mathf.Lerp(farVerticalLayerFactor, nearVerticalLayerFactor, separation);
            layer.Tiles.AddRange(foundTiles);

            if (!ShouldStayPut(layer) && !ShouldIgnoreLooping(layer) && layer.Tiles.Count >= 1)
            {
                EnsureEnoughTiles(layer);
            }

            return layer;
        }

        private static bool ShouldIgnoreLooping(BackgroundLayer layer)
        {
            return layer.OverrideSettings != null && (layer.OverrideSettings.NoParallaxStayPut || !layer.OverrideSettings.Loop || layer.OverrideSettings.IgnoreLooping);
        }

        private static bool ShouldFollowCameraOnly(BackgroundLayer layer)
        {
            return layer.OverrideSettings != null && (layer.OverrideSettings.StayWithCamera || layer.OverrideSettings.MoveWithCameraOnly);
        }

        private static bool ShouldStayPut(BackgroundLayer layer)
        {
            return layer.OverrideSettings != null && layer.OverrideSettings.NoParallaxStayPut;
        }

        private void KeepLayerInsideCameraView(BackgroundLayer layer, float cameraX, float halfViewWidth)
        {
            if (layer.Tiles.Count == 0)
            {
                return;
            }

            float layerMinX;
            float layerMaxX;
            if (!TryGetLayerBounds(layer, out layerMinX, out layerMaxX))
            {
                return;
            }

            float viewMinX = cameraX - halfViewWidth + viewEdgePadding;
            float viewMaxX = cameraX + halfViewWidth - viewEdgePadding;
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

            for (int i = 0; i < layer.Tiles.Count; i++)
            {
                Transform tile = layer.Tiles[i];
                if (tile != null)
                {
                    tile.position += new Vector3(offsetX, 0f, 0f);
                }
            }
        }

        private void KeepProtectedObjectsInsideCameraView(BackgroundLayer layer, float cameraX, float halfViewWidth)
        {
            if (protectedObjectNames == null || protectedObjectNames.Length == 0)
            {
                return;
            }

            float viewMinX = cameraX - halfViewWidth + viewEdgePadding;
            float viewMaxX = cameraX + halfViewWidth - viewEdgePadding;

            for (int i = 0; i < layer.Tiles.Count; i++)
            {
                Transform tile = layer.Tiles[i];
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
            string groupName = GetLayerGroupName(objectName);
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

        private static bool TryGetLayerBounds(BackgroundLayer layer, out float minX, out float maxX)
        {
            minX = float.PositiveInfinity;
            maxX = float.NegativeInfinity;

            for (int i = 0; i < layer.Tiles.Count; i++)
            {
                Transform tile = layer.Tiles[i];
                if (tile == null)
                {
                    continue;
                }

                minX = Mathf.Min(minX, GetLeftEdge(tile));
                maxX = Mathf.Max(maxX, GetRightEdge(tile));
            }

            return maxX > minX;
        }

        private void EnsureEnoughTiles(BackgroundLayer layer)
        {
            float cameraWidth = GetCameraViewWidth();
            if (cameraWidth <= 0f || layer.Tiles.Count < 1)
            {
                return;
            }

            float requiredCoverage = cameraWidth + (layer.TileWidth * 1.5f) + (recyclePadding * 2f);
            int safetyLimit = Mathf.Max(2, maxRuntimeTilesPerLayer);

            while (GetLayerCoverage(layer) < requiredCoverage && layer.Tiles.Count < safetyLimit)
            {
                Transform rightMost = layer.Tiles[layer.Tiles.Count - 1];
                if (rightMost == null)
                {
                    return;
                }

                Transform clone = Instantiate(rightMost, rightMost.parent);
                clone.name = $"{rightMost.name} Loop";
                MoveTileLeftEdgeTo(clone, GetRightEdge(rightMost));
                layer.Tiles.Add(clone);
            }
        }

        private List<string> GetValidLayerGroups()
        {
            List<string> layerGroups = new List<string>();

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform layerRoot = transform.GetChild(i);
                if (FindReferenceTile(layerRoot) == null)
                {
                    continue;
                }

                string groupName = GetLayerGroupName(layerRoot.name);
                if (!layerGroups.Contains(groupName))
                {
                    layerGroups.Add(groupName);
                }
            }

            if (layerGroups.Count == 0)
            {
                layerGroups.Add(string.Empty);
            }

            return layerGroups;
        }

        private static int GetLayerGroupIndex(List<string> layerGroups, string layerName)
        {
            string groupName = GetLayerGroupName(layerName);
            int index = layerGroups.IndexOf(groupName);
            return Mathf.Max(0, index);
        }

        private static string GetLayerGroupName(string layerName)
        {
            int suffixStart = layerName.LastIndexOf(" (");
            if (!layerName.EndsWith(")") || suffixStart < 0)
            {
                return layerName;
            }

            string suffix = layerName.Substring(suffixStart + 2, layerName.Length - suffixStart - 3);
            for (int i = 0; i < suffix.Length; i++)
            {
                if (!char.IsDigit(suffix[i]))
                {
                    return layerName;
                }
            }

            return layerName.Substring(0, suffixStart);
        }

        private static void AlignTiles(List<Transform> foundTiles, float tileWidth)
        {
            for (int i = 0; i < foundTiles.Count; i++)
            {
                Transform tile = foundTiles[i];
                if (i == 0)
                {
                    continue;
                }

                MoveTileLeftEdgeTo(tile, GetRightEdge(foundTiles[i - 1]));
            }
        }

        private Transform FindReferenceTile(Transform layerRoot)
        {
            Renderer[] renderers = layerRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rendererComponent = renderers[i];
                if (IsValidParallaxRenderer(rendererComponent, layerRoot))
                {
                    return rendererComponent.transform;
                }
            }

            Renderer rootRenderer = layerRoot.GetComponent<Renderer>();
            if (IsValidParallaxRenderer(rootRenderer, null))
            {
                return layerRoot;
            }

            return null;
        }

        private void RecycleLayer(BackgroundLayer layer, float cameraX, float halfViewWidth)
        {
            float leftBound = cameraX - halfViewWidth - recyclePadding;
            float rightBound = cameraX + halfViewWidth + recyclePadding;
            int tileCount = layer.Tiles.Count;

            if (tileCount < 2)
            {
                return;
            }

            // Bounded loop only. No unbounded while loop.
            for (int safety = 0; safety < tileCount; safety++)
            {
                Transform leftMost = layer.Tiles[layer.HeadIndex];
                Transform rightMost = layer.Tiles[(layer.HeadIndex + tileCount - 1) % tileCount];

                if (leftMost == null || rightMost == null)
                {
                    return;
                }

                bool moved = false;

                if (GetRightEdge(leftMost) < leftBound)
                {
                    MoveTileLeftEdgeTo(leftMost, GetRightEdge(rightMost));
                    layer.HeadIndex = (layer.HeadIndex + 1) % tileCount;
                    moved = true;
                }
                else if (GetLeftEdge(rightMost) > rightBound)
                {
                    MoveTileRightEdgeTo(rightMost, GetLeftEdge(leftMost));
                    layer.HeadIndex = (layer.HeadIndex - 1 + tileCount) % tileCount;
                    moved = true;
                }

                if (!moved)
                {
                    break;
                }
            }
        }

        private float GetCameraViewWidth()
        {
            if (cam == null)
            {
                return 0f;
            }

            if (camComponent == null)
            {
                return 0f;
            }

            if (camComponent.orthographic)
            {
                return camComponent.orthographicSize * 2f * camComponent.aspect;
            }

            float distance = Mathf.Abs(camComponent.transform.position.z - transform.position.z);
            Vector3 left = camComponent.ViewportToWorldPoint(new Vector3(0f, 0.5f, distance));
            Vector3 right = camComponent.ViewportToWorldPoint(new Vector3(1f, 0.5f, distance));
            return Mathf.Abs(right.x - left.x);
        }

        private static float GetTileWidth(Transform tile)
        {
            Renderer rendererComponent = tile.GetComponent<Renderer>();
            return rendererComponent != null ? rendererComponent.bounds.size.x : 0f;
        }

        private static bool IsValidParallaxRenderer(Renderer rendererComponent, Transform ignoredRoot)
        {
            if (rendererComponent == null)
            {
                return false;
            }

            if (ignoredRoot != null && rendererComponent.transform == ignoredRoot)
            {
                return false;
            }

            return rendererComponent.GetComponentInParent<Character>() == null;
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

        private static float GetLayerCoverage(BackgroundLayer layer)
        {
            if (layer.Tiles.Count == 0)
            {
                return 0f;
            }

            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            for (int i = 0; i < layer.Tiles.Count; i++)
            {
                Transform tile = layer.Tiles[i];
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
}
