using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Cinemachine;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// A class that handles camera follow for Cinemachine powered cameras
    /// </summary>
    public class CinemachineCameraController : MonoBehaviour, MMEventListener<MMCameraEvent>, MMEventListener<CorgiEngineEvent>
    {
        public enum PerspectiveZoomMethods { FieldOfView, FramingTransposerDistance }

        public bool FollowsPlayer { get; set; }

        [Header("Settings")]
        [Tooltip("if this is true, this camera will follow a player")]
        public bool FollowsAPlayer = true;

        [Tooltip("whether this camera should be confined by the bounds determined in the LevelManager or not")]
        public bool ConfineCameraToLevelBounds = true;

        [Tooltip("How high (or low) from the Player the camera should move when looking up/down")]
        public float ManualUpDownLookDistance = 3;

        [Tooltip("Deprecated. Camera framing is now controlled from the Cinemachine Virtual Camera inspector.")]
        public bool CenterCameraTargetOnScreen = false;

        [MMVector("Min", "Max")]
        [Tooltip("the min and max speed to consider for this character (when dealing with the zoom)")]
        public Vector2 CharacterSpeed = new Vector2(0f, 16f);

        [MMReadOnly]
        [Tooltip("the target character this camera follows")]
        public Character TargetCharacter;

        [MMReadOnly]
        [Tooltip("the controller bound to the character this camera follows")]
        public CorgiController TargetController;

        [Space(10)]
        [Header("Orthographic Zoom")]
        [MMInformation("Determine here the min and max zoom, and the zoom speed. By default the engine will zoom out when your character is going at full speed, and zoom in when you slow down (or stop).", MoreMountains.Tools.MMInformationAttribute.InformationType.Info, false)]
        [Tooltip("Whether this camera should zoom in or out as the character moves")]
        public bool UseOrthographicZoom = false;

        [MMCondition("UseOrthographicZoom", true)]
        [MMVector("Min", "Max")]
        [Tooltip("the minimum & maximum orthographic camera zoom")]
        public Vector2 OrthographicZoom = new Vector2(5f, 9f);

        [MMCondition("UseOrthographicZoom", true)]
        [Tooltip("the initial zoom value when using an orthographic zoom")]
        public float InitialOrthographicZoom = 5f;

        [MMCondition("UseOrthographicZoom", true)]
        [Tooltip("the speed at which the orthographic camera zooms")]
        public float OrthographicZoomSpeed = 0.4f;

        [Space(10)]
        [Header("Perspective Zoom")]
        [MMInformation("Determine here the min and max zoom, and the zoom speed when the camera is in perspective mode. You can pick two zoom methods, either playing with the field of view or the transposer's distance.", MoreMountains.Tools.MMInformationAttribute.InformationType.Info, false)]
        [Tooltip("if this is true, perspective zoom will be processed every frame")]
        public bool UsePerspectiveZoom = false;

        [MMCondition("UsePerspectiveZoom", true)]
        [Tooltip("the zoom method for this camera")]
        public PerspectiveZoomMethods PerspectiveZoomMethod = PerspectiveZoomMethods.FramingTransposerDistance;

        [MMCondition("UsePerspectiveZoom", true)]
        [MMVector("Min", "Max")]
        [Tooltip("the min and max perspective camera zooms")]
        public Vector2 PerspectiveZoom = new Vector2(10f, 15f);

        [MMCondition("UsePerspectiveZoom", true)]
        [Tooltip("the initial zoom to apply to the camera when in perspective mode")]
        public float InitialPerspectiveZoom = 5f;

        [MMCondition("UsePerspectiveZoom", true)]
        [Tooltip("the speed at which the perspective camera zooms")]
        public float PerspectiveZoomSpeed = 0.4f;

        [Space(10)]
        [Header("Respawn")]
        [Tooltip("if this is true, the camera will teleport to the player's location on respawn, otherwise it'll move there at its regular speed")]
        public bool InstantRepositionCameraOnRespawn = false;

        [Header("Debug")]
        [MMInspectorButton("StartFollowing")]
        public bool StartFollowingBtn;

        [MMInspectorButton("StopFollowing")]
        public bool StopFollowingBtn;

        protected CinemachineVirtualCamera _virtualCamera;
        protected CinemachineConfiner _confiner;
        protected CinemachineFramingTransposer _framingTransposer;

        protected float _currentZoom;
        protected bool _initialized = false;

        protected virtual void Awake()
        {
            Initialization();
        }

        protected virtual void Initialization()
        {
            if (_initialized)
            {
                return;
            }

            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            if (_virtualCamera == null)
            {
                Debug.LogWarning($"{nameof(CinemachineCameraController)} needs a CinemachineVirtualCamera on the same GameObject.", this);
                enabled = false;
                return;
            }

            _confiner = GetComponent<CinemachineConfiner>();
            _currentZoom = _virtualCamera.m_Lens.Orthographic ? InitialOrthographicZoom : InitialPerspectiveZoom;
            _framingTransposer = _virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            _initialized = true;
        }

        protected virtual void Start()
        {
            if (!_initialized || _virtualCamera == null)
            {
                return;
            }

            InitializeConfiner();

            if (UseOrthographicZoom)
            {
                _virtualCamera.m_Lens.OrthographicSize = InitialOrthographicZoom;
            }

            if (UsePerspectiveZoom)
            {
                SetPerspectiveZoom(InitialPerspectiveZoom);
            }
        }

        protected virtual void InitializeConfiner()
        {
            if (_confiner == null || !ConfineCameraToLevelBounds || LevelManager.Instance == null)
            {
                return;
            }

            if (_confiner.m_ConfineMode == CinemachineConfiner.Mode.Confine2D)
            {
                _confiner.m_BoundingShape2D = LevelManager.Instance.BoundsCollider2D;
            }
            else
            {
                _confiner.m_BoundingVolume = LevelManager.Instance.BoundsCollider;
            }
        }

        public virtual void Set2DConfinerBounds(Collider2D boundsCollider)
        {
            if (boundsCollider == null)
            {
                return;
            }

            if (_confiner == null)
            {
                _confiner = GetComponent<CinemachineConfiner>();
            }

            if (_confiner == null)
            {
                Debug.LogWarning($"{nameof(CinemachineCameraController)} needs a CinemachineConfiner to use room camera bounds.", this);
                return;
            }

            ConfineCameraToLevelBounds = true;
            _confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
            _confiner.m_BoundingShape2D = boundsCollider;
            _confiner.m_ConfineScreenEdges = true;
            _confiner.InvalidatePathCache();
        }

        public virtual void SetTarget(Character character)
        {
            TargetCharacter = character;
            TargetController = null;

            if (character == null)
            {
                return;
            }

            TargetController = character.gameObject.MMGetComponentNoAlloc<CorgiController>();
        }

        public virtual void StartFollowing()
        {
            Initialization();
            if (!FollowsAPlayer || _virtualCamera == null || TargetCharacter == null || TargetCharacter.CameraTarget == null)
            {
                return;
            }

            FollowsPlayer = true;
            _virtualCamera.Follow = TargetCharacter.CameraTarget.transform;
            _virtualCamera.enabled = true;
        }

        public virtual void StopFollowing()
        {
            Initialization();
            if (!FollowsAPlayer || _virtualCamera == null)
            {
                return;
            }

            FollowsPlayer = false;
            _virtualCamera.Follow = null;
            // WE MUST NOT TURN IT OFF, otherwise it falls back to the persistent Priority 0 Cinematic Camera!
            // _virtualCamera.enabled = false; 
        }

        protected virtual void LateUpdate()
        {
            if (_virtualCamera == null)
            {
                return;
            }

            HandleZoom();
        }

        protected virtual void HandleZoom()
        {
            if (_virtualCamera.m_Lens.Orthographic)
            {
                PerformOrthographicZoom();
            }
            else
            {
                PerformPerspectiveZoom();
            }
        }

        protected virtual void PerformOrthographicZoom()
        {
            if (!UseOrthographicZoom || TargetController == null)
            {
                return;
            }

            float characterSpeed = Mathf.Abs(TargetController.Speed.x);
            float currentVelocity = Mathf.Max(characterSpeed, CharacterSpeed.x);
            float targetZoom = MMMaths.Remap(currentVelocity, CharacterSpeed.x, CharacterSpeed.y, OrthographicZoom.x, OrthographicZoom.y);

            _currentZoom = Mathf.Lerp(_currentZoom, targetZoom, Time.deltaTime * OrthographicZoomSpeed);
            _virtualCamera.m_Lens.OrthographicSize = _currentZoom;
        }

        protected virtual void PerformPerspectiveZoom()
        {
            if (!UsePerspectiveZoom || TargetController == null)
            {
                return;
            }

            float characterSpeed = Mathf.Abs(TargetController.Speed.x);
            float currentVelocity = Mathf.Max(characterSpeed, CharacterSpeed.x);
            float targetZoom = MMMaths.Remap(currentVelocity, CharacterSpeed.x, CharacterSpeed.y, PerspectiveZoom.x, PerspectiveZoom.y);

            _currentZoom = Mathf.Lerp(_currentZoom, targetZoom, Time.deltaTime * PerspectiveZoomSpeed);
            SetPerspectiveZoom(_currentZoom);
        }

        protected virtual void SetPerspectiveZoom(float newZoom)
        {
            if (_virtualCamera == null)
            {
                return;
            }

            switch (PerspectiveZoomMethod)
            {
                case PerspectiveZoomMethods.FieldOfView:
                    _virtualCamera.m_Lens.FieldOfView = newZoom;
                    break;

                case PerspectiveZoomMethods.FramingTransposerDistance:
                    if (_framingTransposer != null)
                    {
                        _framingTransposer.m_CameraDistance = newZoom;
                    }
                    break;
            }
        }

        public virtual void OnMMEvent(MMCameraEvent cameraEvent)
        {
            switch (cameraEvent.EventType)
            {
                case MMCameraEventTypes.SetTargetCharacter:
                    SetTarget(cameraEvent.TargetCharacter);
                    break;

                case MMCameraEventTypes.SetConfiner:
                    if (_confiner != null && ConfineCameraToLevelBounds)
                    {
                        if (_confiner.m_ConfineMode == CinemachineConfiner.Mode.Confine2D)
                        {
                            _confiner.m_BoundingShape2D = cameraEvent.Bounds2D;
                        }
                        else
                        {
                            _confiner.m_BoundingVolume = cameraEvent.Bounds;
                        }
                    }
                    break;

                case MMCameraEventTypes.StartFollowing:
                    if (cameraEvent.TargetCharacter != null && cameraEvent.TargetCharacter != TargetCharacter)
                    {
                        return;
                    }
                    StartFollowing();
                    break;

                case MMCameraEventTypes.StopFollowing:
                    if (cameraEvent.TargetCharacter != null && cameraEvent.TargetCharacter != TargetCharacter)
                    {
                        return;
                    }
                    StopFollowing();
                    break;

                case MMCameraEventTypes.ResetPriorities:
                    if (_virtualCamera != null)
                    {
                        _virtualCamera.Priority = 0;
                    }
                    break;
            }
        }

        public virtual void TeleportCameraToTarget()
        {
            if (TargetCharacter == null)
            {
                return;
            }

            transform.position = TargetCharacter.transform.position;
        }

        public virtual void SetPriority(int priority)
        {
            if (_virtualCamera == null)
            {
                return;
            }

            _virtualCamera.Priority = priority;
        }

        public virtual void OnMMEvent(CorgiEngineEvent corgiEngineEvent)
        {
            if (corgiEngineEvent.EventType == CorgiEngineEventTypes.Respawn)
            {
                if (InstantRepositionCameraOnRespawn)
                {
                    TeleportCameraToTarget();
                }
            }

            if (corgiEngineEvent.EventType == CorgiEngineEventTypes.CharacterSwitch ||
                corgiEngineEvent.EventType == CorgiEngineEventTypes.CharacterSwap)
            {
                if (LevelManager.Instance != null &&
                    LevelManager.Instance.Players != null &&
                    LevelManager.Instance.Players.Count > 0)
                {
                    SetTarget(LevelManager.Instance.Players[0]);
                    StartFollowing();
                }
            }
        }

        protected virtual void OnEnable()
        {
            this.MMEventStartListening<MMCameraEvent>();
            this.MMEventStartListening<CorgiEngineEvent>();
        }

        protected virtual void OnDisable()
        {
            this.MMEventStopListening<MMCameraEvent>();
            this.MMEventStopListening<CorgiEngineEvent>();
        }
    }
}
