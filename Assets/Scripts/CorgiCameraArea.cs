using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Corgi Engine/Camera/Corgi Camera Area")]
    public class CorgiCameraArea : MonoBehaviour
    {
        [Header("Bounds")]
        [Tooltip("Collider used by the Cinemachine Confiner for this area. If empty, this object's Collider2D will be used.")]
        public Collider2D AreaBounds;

        [Tooltip("The Cinemachine confiner on the existing virtual camera. If empty, the first one found in the scene will be used.")]
        public CinemachineConfiner CameraConfiner;

        [Tooltip("Also broadcasts Corgi's camera confiner event for existing camera listeners.")]
        public bool BroadcastCameraEvent = true;

        [Tooltip("If true, this area changes the active Cinemachine camera bounds. Keep this off when using LevelManager bounds or CameraRoomBounds.")]
        public bool ApplyBoundsToCamera = false;

        [Header("Background")]
        [Tooltip("Objects to enable when the player enters this area.")]
        public List<GameObject> BackgroundsToEnable = new List<GameObject>();

        [Tooltip("Objects to disable when the player enters this area.")]
        public List<GameObject> BackgroundsToDisable = new List<GameObject>();

        [Header("Transition")]
        [Tooltip("If true, entering the area fades to black, applies bounds/backgrounds, then fades back.")]
        public bool UseFadeTransition = true;

        [Tooltip("Fade-to-black duration.")]
        public float FadeToBlackDuration = 0.12f;

        [Tooltip("Small pause while black before fading back.")]
        public float HoldBlackDuration = 0.03f;

        [Tooltip("Fade-from-black duration.")]
        public float FadeFromBlackDuration = 0.12f;

        [Tooltip("Fader ID to target. Keep 0 for the default Corgi fader.")]
        public int FaderID = 0;

        [Tooltip("Fade tween used for area transitions.")]
        public MMTweenType FadeTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic);

        [Tooltip("Use unscaled time for the area fade.")]
        public bool IgnoreTimeScale = true;

        [Header("Startup")]
        [Tooltip("If the player starts inside this area, apply its bounds/backgrounds without a fade.")]
        public bool ApplyOnStartIfPlayerInside = true;

        protected Collider2D _triggerCollider;
        protected bool _transitioning;
        protected static CorgiCameraArea _currentArea;

        protected virtual void Awake()
        {
            _triggerCollider = GetComponent<Collider2D>();
            if (AreaBounds == null)
            {
                AreaBounds = _triggerCollider;
            }

            if ((_triggerCollider != null) && !_triggerCollider.isTrigger)
            {
                Debug.LogWarning($"{nameof(CorgiCameraArea)} on {name} needs its Collider2D set to Is Trigger.", this);
            }
        }

        protected virtual IEnumerator Start()
        {
            if (!ApplyOnStartIfPlayerInside)
            {
                yield break;
            }

            yield return null;

            Character player = GetFirstPlayer();
            if ((player != null) && (AreaBounds != null) && AreaBounds.bounds.Contains(player.transform.position))
            {
                ApplyArea();
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D collider)
        {
            if (_transitioning || (_currentArea == this))
            {
                return;
            }

            Character character = collider.gameObject.MMGetComponentNoAlloc<Character>();
            if ((character == null) || (character.CharacterType != Character.CharacterTypes.Player))
            {
                return;
            }

            StartCoroutine(TransitionToArea(character.transform.position));
        }

        protected virtual IEnumerator TransitionToArea(Vector3 fadePosition)
        {
            _transitioning = true;

            if (UseFadeTransition)
            {
                MMFadeInEvent.Trigger(FadeToBlackDuration, FadeTween, FaderID, IgnoreTimeScale, fadePosition);
                yield return WaitForTransition(FadeToBlackDuration);
            }

            ApplyArea();

            if (UseFadeTransition && (HoldBlackDuration > 0f))
            {
                yield return WaitForTransition(HoldBlackDuration);
            }

            if (UseFadeTransition)
            {
                MMFadeOutEvent.Trigger(FadeFromBlackDuration, FadeTween, FaderID, IgnoreTimeScale, fadePosition);
                yield return WaitForTransition(FadeFromBlackDuration);
            }

            _transitioning = false;
        }

        protected virtual void ApplyArea()
        {
            _currentArea = this;
            ApplyBackgrounds();
            ApplyCameraBounds();
        }

        protected virtual void ApplyBackgrounds()
        {
            foreach (GameObject background in BackgroundsToDisable)
            {
                if (background != null)
                {
                    background.SetActive(false);
                }
            }

            foreach (GameObject background in BackgroundsToEnable)
            {
                if (background != null)
                {
                    background.SetActive(true);
                }
            }
        }

        protected virtual void ApplyCameraBounds()
        {
            if (!ApplyBoundsToCamera)
            {
                return;
            }

            if (AreaBounds == null)
            {
                return;
            }

            ResolveCameraConfiner();

            if (CameraConfiner != null)
            {
                CameraConfiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
                CameraConfiner.m_BoundingShape2D = AreaBounds;
                CameraConfiner.m_ConfineScreenEdges = true;
                CameraConfiner.InvalidatePathCache();
            }

            if (BroadcastCameraEvent)
            {
                MMCameraEvent.Trigger(MMCameraEventTypes.SetConfiner, null, null, AreaBounds);
            }
        }

        protected virtual void ResolveCameraConfiner()
        {
            if (CameraConfiner != null)
            {
                return;
            }

            CameraConfiner = FindFirstObjectByType<CinemachineConfiner>();
        }

        protected virtual Character GetFirstPlayer()
        {
            if (!LevelManager.HasInstance || (LevelManager.Instance.Players == null) || (LevelManager.Instance.Players.Count == 0))
            {
                return null;
            }

            return LevelManager.Instance.Players[0];
        }

        protected virtual IEnumerator WaitForTransition(float duration)
        {
            if (duration <= 0f)
            {
                yield break;
            }

            if (IgnoreTimeScale)
            {
                yield return new WaitForSecondsRealtime(duration);
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }
        }

        public static void RefreshAreaForPlayer(Character player)
        {
            if (player == null)
            {
                return;
            }

            CorgiCameraArea[] areas = FindObjectsByType<CorgiCameraArea>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            CorgiCameraArea bestArea = null;
            float bestAreaSize = float.MaxValue;
            Vector3 playerPosition = player.transform.position;

            for (int i = 0; i < areas.Length; i++)
            {
                CorgiCameraArea area = areas[i];
                if (area == null || area.AreaBounds == null || area.gameObject.scene != player.gameObject.scene)
                {
                    continue;
                }

                Bounds bounds = area.AreaBounds.bounds;
                if (!bounds.Contains(playerPosition))
                {
                    continue;
                }

                float areaSize = bounds.size.x * bounds.size.y;
                if (areaSize < bestAreaSize)
                {
                    bestAreaSize = areaSize;
                    bestArea = area;
                }
            }

            if (bestArea != null)
            {
                bestArea.ApplyArea();
            }
        }
    }
}
