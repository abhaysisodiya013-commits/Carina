using System.Collections;
using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using MoreMountains.InventoryEngine;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Corgi Engine/Level Bounds/Runtime Level Bounds Override")]
[DefaultExecutionOrder(-1000)]
public class RuntimeLevelBoundsOverride : MonoBehaviour, MMEventListener<CorgiEngineEvent>
{
    protected const int DefaultResponsiveFrameRate = 60;
    protected const float DefaultResponsiveFixedDeltaTime = 1f / 60f;
    protected const float DefaultResponsiveMaximumDeltaTime = 0.066f;

    [Header("Horizontal Bounds Override")]
    [Tooltip("Applies the override in Awake so camera and character bounds use the expanded values immediately.")]
    public bool applyOnAwake = true;

    [Tooltip("Applies the override again in Start in case another script resets the level bounds during initialization.")]
    public bool applyOnStart = true;

    [Tooltip("Reapplies startup fixes after Corgi's LevelManager has finished spawning/registering players and GUI/input.")]
    public bool applyAfterLevelManagerStart = true;

    [Tooltip("How many frames to wait before applying the delayed startup fix. 2 is safe for Corgi scene startup order.")]
    [Min(1)]
    public int delayedStartupFixFrames = 2;

    [Tooltip("If enabled, the left side of the level bounds will be expanded to at least minimumLeftX.")]
    public bool expandLeft = true;

    [Tooltip("If enabled, the right side of the level bounds will be expanded to at least maximumRightX.")]
    public bool expandRight = true;

    [Tooltip("World-space X position to use as the minimum left bound.")]
    public float minimumLeftX = -10000f;

    [Tooltip("World-space X position to use as the maximum right bound.")]
    public float maximumRightX = 10000f;

    [Tooltip("If enabled, the bottom side of the level bounds will be expanded to at least minimumBottomY.")]
    public bool expandDown = true;

    [Tooltip("If enabled, the top side of the level bounds will be expanded to at least maximumTopY.")]
    public bool expandUp = true;

    [Tooltip("World-space Y position to use as the minimum bottom bound.")]
    public float minimumBottomY = -10000f;

    [Tooltip("World-space Y position to use as the maximum top bound.")]
    public float maximumTopY = 10000f;

    [Header("Character Clamp Override")]
    [Tooltip("If enabled, spawned player characters will have their left/right CharacterLevelBounds clamps disabled at runtime.")]
    public bool disableCharacterHorizontalConstraints = true;

    [Tooltip("Keeps reapplying the override in LateUpdate so newly spawned players and late scene setup can't restore the old bounds.")]
    public bool keepReapplyingAtRuntime = false;

    [Header("Startup State Fixes")]
    [Tooltip("Disables runtime debug ray drawing so AI detection/patrol rays don't show in the Game view when Gizmos are on.")]
    public bool disableDebugRays = true;

    [Tooltip("Forces scene item pickers back to their visible/active state at startup.")]
    public bool reviveSceneItemPickersOnStart = true;

    [Tooltip("Activates the scene Props root. Pickers and props placed under an inactive Props root are collectible but invisible.")]
    public bool activatePropsRootOnStart = true;

    [Tooltip("Name of the scene props root to activate at startup.")]
    public string scenePropsRootName = "Props";

    [Tooltip("Also fixes scene props whose parent/object names include these strings, even if they were dropped outside the Props root.")]
    public string[] scenePropNameMarkers =
    {
        "PF Dungeon Props",
        "Dungeon Props"
    };

    [Tooltip("Adds missing trigger/item-picker components to visible weapon pickup sprites that were accidentally placed as decoration.")]
    public bool repairDecorativeWeaponPickersOnStart = true;

    [Tooltip("Trigger radius to use when repairing a visible weapon pickup sprite that has no collider.")]
    public float repairedWeaponPickerRadius = 0.6f;

    [Tooltip("Forces pickup sprites to draw above same-layer tilemaps so the item can be seen before it is collected.")]
    public bool forceSceneItemPickerVisualsInFront = true;

    [Tooltip("Minimum sorting order applied to item picker renderers when forcing them visible.")]
    public int itemPickerVisualSortingOrder = 1000;

    [Tooltip("Keeps item picker visuals visible during play in case another scene system hides their renderers after startup.")]
    public bool keepSceneItemPickerVisualsVisibleAtRuntime = false;

    [Tooltip("How often to refresh item picker visuals during play.")]
    [Min(0.05f)]
    public float itemPickerVisualRefreshInterval = 0.2f;

    [Tooltip("Forces regular scene props under the Props root to render above depth-writing background planes.")]
    public bool forceScenePropsVisibleOnStart = true;

    [Tooltip("Keeps scene props visible during play if another scene system changes their renderer state.")]
    public bool keepScenePropsVisibleAtRuntime = false;

    [Tooltip("Minimum sorting order applied to regular prop renderers under the Props root.")]
    public int scenePropVisualSortingOrder = 1000;

    [Tooltip("Moves regular scene prop renderers to a stable world Z so they don't spawn behind opaque background planes.")]
    public bool normalizeScenePropDepth = true;

    [Tooltip("World Z used for regular scene prop renderers when normalizing prop depth.")]
    public float scenePropWorldZ = 0f;

    [Tooltip("Disables depth writing on background mesh materials so they can't hide sprites while leaving colliders active.")]
    public bool disableBackgroundDepthWriteOnStart = true;

    [Tooltip("Refreshes player ability input managers after spawn so abilities like dash don't miss startup input binding.")]
    public bool refreshAbilityInputManagersAfterSpawn = true;

    [Tooltip("Clears stale mobile/keyboard input during level start so the player doesn't dash once immediately after spawning.")]
    public bool resetPlayerInputOnSpawn = true;

    [Tooltip("How many startup frames to keep clearing stale input after a level starts or the player respawns.")]
    [Min(1)]
    public int spawnInputResetFrames = 20;

    [Tooltip("If enabled, cancels an accidental Dashing state during the spawn input reset window.")]
    public bool cancelAccidentalDashOnSpawn = true;

    [Tooltip("Disables accidental Corgi GravityZone triggers in this scene. This fixes large invisible areas where player/enemies float and jump direction becomes wrong.")]
    public bool disableSceneGravityZonesOnStart = true;

    [Tooltip("Keeps enemy DamageOnTouch hitboxes disabled after the enemy's Health reaches 0, so invisible dead enemies cannot hurt the player.")]
    public bool disableDeadEnemyDamageZonesAtRuntime = true;

    [Tooltip("How often to check cached enemy DamageOnTouch zones during play. This avoids full scene scans every frame.")]
    [Min(0.05f)]
    public float deadEnemyDamageZoneCheckInterval = 0.25f;

    [Tooltip("Caps active enemy machine gun bullet speed so blob shots are readable and shieldable.")]
    public bool capEnemyMachineGunProjectileSpeed = true;

    [Tooltip("Maximum runtime speed for enemy RetroMachineGunBullet projectiles. Corgi Projectile movement divides this by 10.")]
    public float enemyMachineGunProjectileMaxSpeed = 120f;

    [Tooltip("How often to cap active enemy machine gun bullet speed.")]
    [Min(0.01f)]
    public float enemyProjectileSpeedCheckInterval = 0.25f;

    [Tooltip("Repairs rare grounded/falling state desyncs where the player is grounded but jump charges don't reset.")]
    public bool repairGroundedJumpStateAtRuntime = true;

    [Tooltip("How often to repair grounded jump state during play.")]
    [Min(0.05f)]
    public float groundedJumpStateRepairInterval = 0.1f;

    [Header("Starting Weapons")]
    [Tooltip("At startup, gives the player only the Retro combo sword, machine gun and shotgun, then equips the combo sword in hand.")]
    public bool grantRetroWeaponsOnStart = false;

    [Tooltip("Clears the player's main/hotbar/weapon inventories before adding the three starting weapons. This keeps the initial inventory clean.")]
    public bool clearPlayerInventoriesBeforeGrantingWeapons = true;

    [Tooltip("Equips RetroComboSword as the player's starting weapon in hand.")]
    public bool equipRetroSwordOnStart = true;
    [Tooltip("Stops horizontal ground movement while the retro starting weapons attack.")]
    public bool stopGroundMovementWhileAttacking = true;
    [Tooltip("Automatically adds the airborne attack animation override to the player.")]
    public bool installAirAttackAnimationOverride = true;
    [Tooltip("Air attack clip used outside rage mode.")]
    public AnimationClip normalAirAttackClip;
    [Tooltip("Air attack clip used while rage mode is active.")]
    public AnimationClip rageAirAttackClip;

    [Tooltip("Resources path for the starting combo sword inventory item.")]
    public string retroSwordResourcePath = "Items/RetroComboSword";

    [Tooltip("Resources path for the starting machine gun inventory item.")]
    public string retroMachineGunResourcePath = "Items/RetroMachineGun";

    [Tooltip("Resources path for the starting shotgun inventory item.")]
    public string retroShotgunResourcePath = "Items/RetroShotgun";

    [Tooltip("Keeps the three starting weapons visible and switchable after Corgi inventory load/restart UI events.")]
    public bool keepRetroWeaponsSyncedAtRuntime = false;

    [Tooltip("How often to reassert the three starting weapons during play.")]
    [Min(0.05f)]
    public float retroWeaponSyncInterval = 0.25f;

    [Tooltip("How long after scene start the starting weapons may be reasserted if runtime syncing is enabled.")]
    [Min(0f)]
    public float retroWeaponStartupSyncDuration = 1.5f;

    [Header("Mobile Performance")]
    [Tooltip("Applies lightweight mobile runtime settings for smoother frame pacing. The static runtime fallback still applies the safe default timing if this scene component is disabled.")]
    public bool applyMobilePerformanceSettings = true;

    [Tooltip("Target frame rate for mobile/editor play. Use 300 for maximum-FPS testing. Real phone display FPS is still capped by refresh rate.")]
    public int targetFrameRate = 300;

    [Tooltip("Upper cap applied even if an old scene override still has a higher test value serialized.")]
    [Min(30)]
    public int maximumRuntimeFrameRate = DefaultResponsiveFrameRate;

    [Tooltip("Legacy field kept for old scene data. Runtime no longer overrides fixed timestep because that can make heavy builds feel like slow motion.")]
    public bool tunePhysicsForResponsiveCombat = false;

    [Tooltip("Fixed timestep used when tuning physics for responsive combat.")]
    [Min(0.005f)]
    public float responsiveFixedDeltaTime = DefaultResponsiveFixedDeltaTime;

    [Tooltip("Caps long frame catch-up bursts so one hitch does not make physics/input feel stuck.")]
    [Min(0.02f)]
    public float responsiveMaximumDeltaTime = DefaultResponsiveMaximumDeltaTime;

    [Tooltip("Disables vSync so Application.targetFrameRate controls frame pacing.")]
    public bool disableVSyncForTargetFrameRate = true;

    [Tooltip("Forces Unity's fastest quality level and then reapplies key low-cost quality settings.")]
    public bool forceFastestQualitySettings = true;

    [Tooltip("Quality level index used by maximum FPS mode. In this project 0 is Fastest.")]
    public int fastestQualityLevelIndex = 0;

    [Tooltip("Disables MSAA on active cameras. Pixel art does not need it and it costs fill-rate on mobile.")]
    public bool disableCameraMSAA = true;

    [Tooltip("Disables HDR on active cameras. This reduces bandwidth and helps mobile GPUs.")]
    public bool disableCameraHDR = true;

    [Tooltip("Disables scene post-processing volumes such as bloom, vignette and chromatic aberration.")]
    public bool disablePostProcessingVolumes = true;

    [Tooltip("Lowers mobile render resolution to reduce GPU cost. 0.7 is much faster; 1 keeps native resolution.")]
    [Range(0.5f, 1f)]
    public float mobileResolutionScale = 0.7f;

    [Tooltip("Uses animator culling so offscreen animated objects stop doing unnecessary visual work.")]
    public bool optimizeAnimatorCulling = true;

    [Tooltip("Reuses 2D collision callbacks to reduce garbage collector spikes during gameplay.")]
    public bool reusePhysics2DCollisionCallbacks = true;

    [Header("Gate Game Over")]
    [Tooltip("Adds a trigger to named gate objects so entering them opens the game over scene.")]
    public bool configureGateGameOverTriggersOnStart = true;

    [Tooltip("Scene to load when the player enters one of the configured gates.")]
    public string gateGameOverSceneName = "RetroAdventureGameOver";

    [Tooltip("Scene object names that should trigger game over when the player enters their zone.")]
    public string[] gateGameOverObjectNames =
    {
        "PF Dungeon Props - Gate 01",
        "PF Dungeon Props - Gate 01 (1)",
        "Dungeon Props - Gate 01",
        "Dungeon Props - Gate 01 (1)"
    };

    protected LevelManager _levelManager;
    protected readonly HashSet<DamageOnTouch> _runtimeDisabledDeadDamageZones = new HashSet<DamageOnTouch>();
    protected bool _retroWeaponsGranted;
    protected bool _damageZonesCached;
    protected float _lastItemPickerVisualRefreshAt = -100f;
    protected float _lastScenePropVisualRefreshAt = -100f;
    protected float _lastEnemyProjectileSpeedCheckAt = -100f;
    protected float _lastGroundedJumpRepairAt = -100f;
    protected float _lastDeadEnemyDamageZoneCheckAt = -100f;
    protected float _lastRetroWeaponSyncAt = -100f;
    protected float _retroWeaponSyncUntilTime = -1f;
    protected InventoryEngineWeapon _cachedComboSword;
    protected InventoryEngineWeapon _cachedMachineGun;
    protected InventoryEngineWeapon _cachedShotgun;
    protected readonly List<DamageOnTouch> _sceneDamageZones = new List<DamageOnTouch>();
    protected bool _cameraPerformanceApplied;
    protected bool _postProcessingPerformanceApplied;
    protected bool _animatorPerformanceApplied;
    protected bool _startupSceneRepairsStarted;
    protected bool _startupSceneRepairsCompleted;
    protected Coroutine _spawnInputResetCoroutine;

    protected virtual void OnEnable()
    {
        this.MMEventStartListening<CorgiEngineEvent>();
    }

    protected virtual void OnDisable()
    {
        this.MMEventStopListening<CorgiEngineEvent>();
    }

    protected virtual void Awake()
    {
        EnsureSaveSlotManagerExists();
        CacheLevelManager();
        ApplyMobilePerformanceSettings();

        if (applyOnAwake)
        {
            ApplyOverride();
        }

        DisableDebugRays();
        DisableCharacterHorizontalBounds();
        RepairGroundedJumpState();
        BeginSpawnInputReset();
    }

    protected virtual void Start()
    {
        EnsureSaveSlotManagerExists();

        if (applyOnStart)
        {
            ApplyOverride();
        }

        DisableDebugRays();
        DisableCharacterHorizontalBounds();
        RefreshPlayerAbilityInputManagers();
        BeginSpawnInputReset();
        EnsureAirAttackAnimationOverrides();
        RepairGroundedJumpState();
        BeginRetroWeaponStartupSync();
        GrantRetroStartingWeapons();

        if (applyAfterLevelManagerStart)
        {
            StartCoroutine(DelayedStartupFixes());
        }
        else
        {
            StartCoroutine(RunStartupSceneRepairsAsync());
        }
    }

    protected virtual void EnsureSaveSlotManagerExists()
    {
        if (CorgiCustomMechanics.SaveSlotManager.HasInstance)
        {
            return;
        }

        GameObject managerObject = new GameObject("SaveSlotManager");
        managerObject.AddComponent<CorgiCustomMechanics.SaveSlotManager>();
        DontDestroyOnLoad(managerObject);
        Debug.Log("ALTAR: RuntimeLevelBoundsOverride created missing SaveSlotManager for altar save/load.");
    }

    protected virtual void LateUpdate()
    {
        DisableDeadEnemyDamageZonesAtInterval();
        CapEnemyProjectileSpeedAtInterval();
        RefreshItemPickerVisualsAtRuntime();
        RefreshScenePropVisualsAtRuntime();
        RepairGroundedJumpStateAtInterval();
        SyncRetroStartingWeaponsAtInterval();

        if (!keepReapplyingAtRuntime)
        {
            return;
        }

        ApplyOverride();
        DisableDebugRays();
        DisableSceneGravityZones();
        DisableCharacterHorizontalBounds();
        RefreshPlayerAbilityInputManagers();
        BeginSpawnInputReset();
        EnsureAirAttackAnimationOverrides();
        GrantRetroStartingWeapons();
    }

    protected virtual IEnumerator DelayedStartupFixes()
    {
        int framesToWait = Mathf.Max(1, delayedStartupFixFrames);
        for (int i = 0; i < framesToWait; i++)
        {
            yield return null;
        }

        if (applyOnStart)
        {
            ApplyOverride();
        }

        DisableDebugRays();
        DisableCharacterHorizontalBounds();
        RefreshPlayerAbilityInputManagers();
        EnsureAirAttackAnimationOverrides();
        RepairGroundedJumpState();
        BeginRetroWeaponStartupSync();
        GrantRetroStartingWeapons();
        RepairMobileInputs();

        yield return RunStartupSceneRepairsAsync();
    }

    protected virtual IEnumerator RunStartupSceneRepairsAsync()
    {
        if (_startupSceneRepairsCompleted || _startupSceneRepairsStarted)
        {
            yield break;
        }

        _startupSceneRepairsStarted = true;

        DisableSceneGravityZones();
        yield return null;

        CacheSceneDamageZones();
        yield return null;

        OptimizeSceneCameras();
        yield return null;

        DisableScenePostProcessingVolumes();
        yield return null;

        OptimizeSceneAnimators();
        yield return null;

        ActivateScenePropsRoot();
        yield return null;

        DisableBackgroundDepthWrite();
        yield return null;

        ForceScenePropsVisible();
        yield return null;

        ConfigureGateGameOverTriggers();
        yield return null;

        ReviveSceneItemPickers();
        yield return null;

        RepairDecorativeWeaponPickers();
        yield return null;

        ForceSceneItemPickerVisuals();
        yield return null;

        RepairUninitializedEnemyHealth();
        DisableDeadEnemyDamageZones();

        _startupSceneRepairsCompleted = true;
    }

    protected virtual void RepairMobileInputs()
    {
        Transform arrowsRoot = FindMobileUIGroup("Arrows");
        if (arrowsRoot != null)
        {
            Transform left = arrowsRoot.Find("ArrowLeft");
            if (left == null) left = arrowsRoot.Find("ArrowLeft (1)");
            WireUpMMTouchButton(left, (input) => {
                if (InputManager.Instance != null) InputManager.Instance.SetHorizontalMovement(-1f);
            }, () => {
                if (InputManager.Instance != null) InputManager.Instance.SetHorizontalMovement(0f);
            }, null, true);

            Transform right = arrowsRoot.Find("ArrowRight");
            if (right == null) right = arrowsRoot.Find("ArrowRight (1)");
            WireUpMMTouchButton(right, (input) => {
                if (InputManager.Instance != null) InputManager.Instance.SetHorizontalMovement(1f);
            }, () => {
                if (InputManager.Instance != null) InputManager.Instance.SetHorizontalMovement(0f);
            }, null, true);

            Transform up = arrowsRoot.Find("ArrowUp");
            if (up == null) up = arrowsRoot.Find("ArrowUp (1)");
            WireUpMMTouchButton(up, (input) => {
                if (InputManager.Instance != null) InputManager.Instance.SetVerticalMovement(1f);
            }, () => {
                if (InputManager.Instance != null) InputManager.Instance.SetVerticalMovement(0f);
            });

            Transform down = arrowsRoot.Find("ArrowDown");
            if (down == null) down = arrowsRoot.Find("ArrowDown (1)");
            WireUpMMTouchButton(down, (input) => {
                if (InputManager.Instance != null) InputManager.Instance.SetVerticalMovement(-1f);
            }, () => {
                if (InputManager.Instance != null) InputManager.Instance.SetVerticalMovement(0f);
            });
        }

        Transform buttonsRoot = FindMobileUIGroup("Buttons");
        if (buttonsRoot != null)
        {
            // Jump
            WireUpMMTouchButton(buttonsRoot.Find("JumpBtn"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.JumpButton, state);
            });
            WireUpMMTouchButton(buttonsRoot.Find("JumpBtn (1)"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.JumpButton, state);
            });

            // Attack (Shoot)
            WireUpMMTouchButton(buttonsRoot.Find("AttackBtn"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.ShootButton, state);
            });
            WireUpMMTouchButton(buttonsRoot.Find("AttackBtn (1)"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.ShootButton, state);
            });

            // Spell (Secondary Shoot)
            WireUpMMTouchButton(buttonsRoot.Find("SpellBtn"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.SecondaryShootButton, state);
            });
            WireUpMMTouchButton(buttonsRoot.Find("SpellBtn (1)"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.SecondaryShootButton, state);
            });

            // Inventory / Interact
            WireUpMMTouchButton(buttonsRoot.Find("InventoryBtn"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.InteractButton, state);
            });
            WireUpMMTouchButton(buttonsRoot.Find("InventoryBtn (1)"), null, null, (state) => {
                if (InputManager.Instance != null)
                    ApplyInputButtonState(InputManager.Instance.InteractButton, state);
            });
        }
    }

    protected virtual void ApplyInputButtonState(MMInput.IMButton button, MMInput.ButtonStates state)
    {
        if (button == null)
        {
            return;
        }

        switch (state)
        {
            case MMInput.ButtonStates.ButtonDown:
                button.TriggerButtonDown();
                break;
            case MMInput.ButtonStates.ButtonPressed:
                button.TriggerButtonPressed();
                break;
            case MMInput.ButtonStates.ButtonUp:
                button.TriggerButtonUp();
                break;
            default:
                button.State.ChangeState(state);
                break;
        }
    }

    protected virtual Transform FindMobileUIGroup(string groupName)
    {
        Transform uiCamera = GameObject.Find("UICamera")?.transform;
        Transform found = uiCamera != null ? uiCamera.Find("Canvas/Controls/" + groupName) : null;
        if (found != null)
        {
            return found;
        }

        Transform retroUICamera = GameObject.Find("RetroUICamera")?.transform;
        found = retroUICamera != null ? retroUICamera.Find("Canvas/Controls/" + groupName) : null;
        if (found != null)
        {
            return found;
        }

        return retroUICamera != null ? retroUICamera.Find("Canvas/" + groupName) : null;
    }

    protected virtual void WireUpMMTouchButton(Transform buttonTransform, System.Action<float> onAxisSet, System.Action onAxisReset, System.Action<MMInput.ButtonStates> onStateChange = null, bool stabilizeHeldAxis = false)
    {
        if (buttonTransform == null) return;

        MoreMountains.Tools.MMTouchButton touchButton = buttonTransform.GetComponent<MoreMountains.Tools.MMTouchButton>();
        if (touchButton == null)
        {
            touchButton = buttonTransform.gameObject.AddComponent<MoreMountains.Tools.MMTouchButton>();
        }

        touchButton.MouseMode = false;

        // Ensure events are initialized if null
        if (touchButton.ButtonPressedFirstTime == null) touchButton.ButtonPressedFirstTime = new UnityEngine.Events.UnityEvent();
        if (touchButton.ButtonReleased == null) touchButton.ButtonReleased = new UnityEngine.Events.UnityEvent();
        if (touchButton.ButtonPressed == null) touchButton.ButtonPressed = new UnityEngine.Events.UnityEvent();

        // Clear existing listeners to prevent double firing if called multiple times
        touchButton.ButtonPressedFirstTime.RemoveAllListeners();
        touchButton.ButtonReleased.RemoveAllListeners();
        touchButton.ButtonPressed.RemoveAllListeners();

        touchButton.ButtonPressedFirstTime.AddListener(() => {
            if (onAxisSet != null) onAxisSet(-1f); // Pass anything, lambda captures right value
            if (onStateChange != null) onStateChange(MMInput.ButtonStates.ButtonDown);
        });

        touchButton.ButtonReleased.AddListener(() => {
            if (onAxisReset != null) onAxisReset();
            if (onStateChange != null) onStateChange(MMInput.ButtonStates.ButtonUp);
        });

        touchButton.ButtonPressed.AddListener(() => {
            if (onAxisSet != null) onAxisSet(-1f);
            if (onStateChange != null) onStateChange(MMInput.ButtonStates.ButtonPressed);
        });

        if (stabilizeHeldAxis && onAxisSet != null && onAxisReset != null)
        {
            CarinaHeldMovementArrowInput stabilizer = buttonTransform.GetComponent<CarinaHeldMovementArrowInput>();
            if (stabilizer == null)
            {
                stabilizer = buttonTransform.gameObject.AddComponent<CarinaHeldMovementArrowInput>();
            }

            stabilizer.Configure(() => onAxisSet(-1f), onAxisReset);
        }
    }

    public virtual void OnMMEvent(CorgiEngineEvent engineEvent)
    {
        if ((engineEvent.EventType == CorgiEngineEventTypes.LevelStart)
            || (engineEvent.EventType == CorgiEngineEventTypes.Respawn))
        {
            BeginSpawnInputReset();
        }
    }

    protected virtual void BeginSpawnInputReset()
    {
        if (!resetPlayerInputOnSpawn)
        {
            return;
        }

        ResetPlayerInputState();

        if (_spawnInputResetCoroutine != null)
        {
            StopCoroutine(_spawnInputResetCoroutine);
        }

        _spawnInputResetCoroutine = StartCoroutine(SpawnInputResetCoroutine());
    }

    protected virtual IEnumerator SpawnInputResetCoroutine()
    {
        int frames = Mathf.Max(1, spawnInputResetFrames);
        for (int i = 0; i < frames; i++)
        {
            ResetPlayerInputState();
            yield return null;
        }

        _spawnInputResetCoroutine = null;
    }

    protected virtual void ResetPlayerInputState()
    {
        if (InputManager.HasInstance)
        {
            InputManager inputManager = InputManager.Instance;
            inputManager.SetMovement(Vector2.zero);
            inputManager.SetHorizontalMovement(0f);
            inputManager.SetVerticalMovement(0f);
            inputManager.SetSecondaryMovement(Vector2.zero);
            inputManager.SetSecondaryHorizontalMovement(0f);
            inputManager.SetSecondaryVerticalMovement(0f);

            SetButtonOff(inputManager.JumpButton);
            SetButtonOff(inputManager.DashButton);
            SetButtonOff(inputManager.ShootButton);
            SetButtonOff(inputManager.SecondaryShootButton);
            SetButtonOff(inputManager.InteractButton);
            SetButtonOff(inputManager.RunButton);
            SetButtonOff(inputManager.RollButton);
            SetButtonOff(inputManager.ThrowButton);
            SetButtonOff(inputManager.GrabButton);
        }

        if (!cancelAccidentalDashOnSpawn)
        {
            return;
        }

        CacheLevelManager();
        if (_levelManager != null && _levelManager.Players != null && _levelManager.Players.Count > 0)
        {
            for (int i = 0; i < _levelManager.Players.Count; i++)
            {
                CancelAccidentalDash(_levelManager.Players[i]);
            }
        }
    }

    protected virtual void CancelAccidentalDash(Character character)
    {
        if (character == null
            || character.CharacterType != Character.CharacterTypes.Player
            || character.MovementState == null
            || character.MovementState.CurrentState != CharacterStates.MovementStates.Dashing)
        {
            return;
        }

        character.MovementState.ChangeState(CharacterStates.MovementStates.Idle);

        CorgiController controller = character.GetComponent<CorgiController>();
        if (controller != null)
        {
            controller.SetForce(Vector2.zero);
        }
    }

    protected virtual void SetButtonOff(MMInput.IMButton button)
    {
        if (button == null)
        {
            return;
        }

        button.State.ChangeState(MMInput.ButtonStates.Off);
    }

    [ContextMenu("Apply Override")]
    public void ApplyOverride()
    {
        CacheLevelManager();
        if (_levelManager == null)
        {
            return;
        }

        Bounds currentBounds = _levelManager.LevelBounds;
        Vector3 min = currentBounds.min;
        Vector3 max = currentBounds.max;

        if (expandLeft)
        {
            min.x = Mathf.Min(min.x, minimumLeftX);
        }

        if (expandRight)
        {
            max.x = Mathf.Max(max.x, maximumRightX);
        }

        if (expandDown)
        {
            min.y = Mathf.Min(min.y, minimumBottomY);
        }

        if (expandUp)
        {
            max.y = Mathf.Max(max.y, maximumTopY);
        }

        currentBounds.SetMinMax(min, max);

        // Use the manager method so colliders/bounds stay in sync.
        _levelManager.SetNewLevelBounds(currentBounds);
    }

    protected virtual void CacheLevelManager()
    {
        if (_levelManager != null)
        {
            return;
        }

        _levelManager = GetComponent<LevelManager>();
        if (_levelManager == null)
        {
            _levelManager = LevelManager.Instance;
        }
    }

    protected virtual T[] FindSceneComponents<T>(bool includeInactive = true) where T : Component
    {
        Scene currentScene = gameObject.scene;
        if (!currentScene.IsValid() || !currentScene.isLoaded)
        {
            return new T[0];
        }

        T[] candidates = Object.FindObjectsByType<T>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        List<T> sceneComponents = new List<T>(candidates.Length);
        for (int i = 0; i < candidates.Length; i++)
        {
            T component = candidates[i];
            if (component == null || component.gameObject.scene != currentScene)
            {
                continue;
            }

            sceneComponents.Add(component);
        }

        return sceneComponents.ToArray();
    }

    protected virtual void ApplyMobilePerformanceSettings()
    {
        if (!applyMobilePerformanceSettings)
        {
            return;
        }

        if (disableVSyncForTargetFrameRate)
        {
            QualitySettings.vSyncCount = 0;
        }

        if (forceFastestQualitySettings && QualitySettings.names != null && QualitySettings.names.Length > 0)
        {
            int qualityIndex = Mathf.Clamp(fastestQualityLevelIndex, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }

        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.pixelLightCount = 0;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.billboardsFaceCameraPosition = false;
        QualitySettings.softParticles = false;
        QualitySettings.lodBias = 0.3f;
        QualitySettings.particleRaycastBudget = 4;
        QualitySettings.resolutionScalingFixedDPIFactor = Mathf.Clamp(mobileResolutionScale, 0.5f, 1f);

        if (reusePhysics2DCollisionCallbacks)
        {
            Physics2D.reuseCollisionCallbacks = true;
        }

        ApplyResponsiveTimingSettings();
    }

    protected virtual void ApplyResponsiveTimingSettings()
    {
        if (targetFrameRate <= 0)
        {
            Application.targetFrameRate = -1;
        }
        else
        {
            Application.targetFrameRate = Mathf.Max(30, targetFrameRate);
        }
    }

    protected virtual void OptimizeSceneCameras()
    {
        if (_cameraPerformanceApplied || (!disableCameraMSAA && !disableCameraHDR))
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        Camera[] cameras = FindSceneComponents<Camera>(false);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera sceneCamera = cameras[i];
            if (sceneCamera == null || sceneCamera.gameObject.scene != currentScene)
            {
                continue;
            }

            if (disableCameraMSAA)
            {
                sceneCamera.allowMSAA = false;
            }

            if (disableCameraHDR)
            {
                sceneCamera.allowHDR = false;
            }

            sceneCamera.depthTextureMode = DepthTextureMode.None;
            sceneCamera.useOcclusionCulling = false;
            OptimizeUniversalCameraData(sceneCamera);
        }

        _cameraPerformanceApplied = true;
    }

    protected virtual void OptimizeUniversalCameraData(Camera sceneCamera)
    {
        Component[] components = sceneCamera.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null || !component.GetType().Name.Contains("UniversalAdditionalCameraData"))
            {
                continue;
            }

            SetBoolPropertyIfPresent(component, "renderPostProcessing", false);
            SetBoolPropertyIfPresent(component, "requiresDepthTexture", false);
            SetBoolPropertyIfPresent(component, "requiresColorTexture", false);
            SetBoolPropertyIfPresent(component, "renderShadows", false);
            SetEnumPropertyToZeroIfPresent(component, "antialiasing");
        }
    }

    protected virtual void SetBoolPropertyIfPresent(Component component, string propertyName, bool value)
    {
        System.Reflection.PropertyInfo propertyInfo = component.GetType().GetProperty(propertyName);
        if (propertyInfo == null || propertyInfo.PropertyType != typeof(bool) || !propertyInfo.CanWrite)
        {
            return;
        }

        propertyInfo.SetValue(component, value, null);
    }

    protected virtual void SetEnumPropertyToZeroIfPresent(Component component, string propertyName)
    {
        System.Reflection.PropertyInfo propertyInfo = component.GetType().GetProperty(propertyName);
        if (propertyInfo == null || !propertyInfo.PropertyType.IsEnum || !propertyInfo.CanWrite)
        {
            return;
        }

        object disabledValue = System.Enum.ToObject(propertyInfo.PropertyType, 0);
        propertyInfo.SetValue(component, disabledValue, null);
    }

    protected virtual void DisableScenePostProcessingVolumes()
    {
        if (_postProcessingPerformanceApplied || !disablePostProcessingVolumes)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        Volume[] volumes = FindSceneComponents<Volume>(false);
        for (int i = 0; i < volumes.Length; i++)
        {
            Volume volume = volumes[i];
            if (volume == null || volume.gameObject.scene != currentScene)
            {
                continue;
            }

            volume.enabled = false;
        }

        _postProcessingPerformanceApplied = true;
    }

    protected virtual void OptimizeSceneAnimators()
    {
        if (_animatorPerformanceApplied || !optimizeAnimatorCulling)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        Animator[] animators = FindSceneComponents<Animator>(false);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || animator.gameObject.scene != currentScene)
            {
                continue;
            }

            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        _animatorPerformanceApplied = true;
    }

    protected virtual void DisableCharacterHorizontalBounds()
    {
        if (!disableCharacterHorizontalConstraints)
        {
            return;
        }

        CacheLevelManager();
        if (_levelManager == null || _levelManager.Players == null)
        {
            return;
        }

        for (int i = 0; i < _levelManager.Players.Count; i++)
        {
            Character playerCharacter = _levelManager.Players[i];
            if (playerCharacter == null)
            {
                continue;
            }

            CharacterLevelBounds levelBounds = playerCharacter.GetComponentInChildren<CharacterLevelBounds>(true);
            if (levelBounds == null)
            {
                continue;
            }

            levelBounds.Left = CharacterLevelBounds.BoundsBehavior.Nothing;
            levelBounds.Right = CharacterLevelBounds.BoundsBehavior.Nothing;
        }
    }

    protected virtual void DisableDebugRays()
    {
        if (!disableDebugRays)
        {
            return;
        }

        MMDebug.SetDebugDrawEnabled(false);
    }

    protected virtual void DisableSceneGravityZones()
    {
        if (!disableSceneGravityZonesOnStart)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        GravityZone[] gravityZones = FindSceneComponents<GravityZone>(true);
        for (int i = 0; i < gravityZones.Length; i++)
        {
            GravityZone gravityZone = gravityZones[i];
            if (gravityZone == null || gravityZone.gameObject.scene != currentScene)
            {
                continue;
            }

            Collider2D zoneCollider = gravityZone.GetComponent<Collider2D>();
            if (zoneCollider != null)
            {
                zoneCollider.enabled = false;
            }

            gravityZone.enabled = false;
        }
    }

    protected virtual void DisableDeadEnemyDamageZones()
    {
        if (!disableDeadEnemyDamageZonesAtRuntime)
        {
            return;
        }

        if (!_damageZonesCached)
        {
            CacheSceneDamageZones();
        }

        for (int i = _sceneDamageZones.Count - 1; i >= 0; i--)
        {
            DamageOnTouch damageZone = _sceneDamageZones[i];
            if (damageZone == null)
            {
                _sceneDamageZones.RemoveAt(i);
                continue;
            }

            DisableDeadEnemyDamageZoneIfNeeded(damageZone);
        }
    }

    protected virtual void DisableDeadEnemyDamageZonesAtInterval()
    {
        if (!disableDeadEnemyDamageZonesAtRuntime)
        {
            return;
        }

        if (Time.unscaledTime - _lastDeadEnemyDamageZoneCheckAt < deadEnemyDamageZoneCheckInterval)
        {
            return;
        }

        _lastDeadEnemyDamageZoneCheckAt = Time.unscaledTime;
        DisableDeadEnemyDamageZones();
    }

    protected virtual void CacheSceneDamageZones()
    {
        Scene currentScene = gameObject.scene;
        _sceneDamageZones.Clear();
        DamageOnTouch[] damageZones = FindSceneComponents<DamageOnTouch>(true);
        for (int i = 0; i < damageZones.Length; i++)
        {
            DamageOnTouch damageZone = damageZones[i];
            if (damageZone == null || damageZone.gameObject.scene != currentScene)
            {
                continue;
            }

            _sceneDamageZones.Add(damageZone);
        }

        _damageZonesCached = true;
    }

    protected virtual void DisableDeadEnemyDamageZoneIfNeeded(DamageOnTouch damageZone)
    {
        Health ownerHealth = FindDamageZoneOwnerHealth(damageZone);
        if (ownerHealth == null)
        {
            return;
        }

        Character ownerCharacter = ownerHealth.GetComponent<Character>();
        if (ownerCharacter != null && ownerCharacter.CharacterType == Character.CharacterTypes.Player)
        {
            return;
        }

        bool ownerIsDead = IsHealthReallyDead(ownerHealth, ownerCharacter);
        if (ownerIsDead)
        {
            if (!_runtimeDisabledDeadDamageZones.Contains(damageZone))
            {
                _runtimeDisabledDeadDamageZones.Add(damageZone);
            }

            SetDamageZoneEnabled(damageZone, false);
            return;
        }

        if (_runtimeDisabledDeadDamageZones.Contains(damageZone))
        {
            SetDamageZoneEnabled(damageZone, true);
            _runtimeDisabledDeadDamageZones.Remove(damageZone);
        }
    }

    protected virtual bool IsHealthReallyDead(Health health, Character character)
    {
        if (health == null || health.CurrentHealth > 0f)
        {
            return false;
        }

        return character == null
               || character.ConditionState == null
               || character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead;
    }

    protected virtual void RepairUninitializedEnemyHealth()
    {
        Scene currentScene = gameObject.scene;
        Health[] healthComponents = FindSceneComponents<Health>(true);
        for (int i = 0; i < healthComponents.Length; i++)
        {
            Health health = healthComponents[i];
            if (health == null || health.gameObject.scene != currentScene || !health.gameObject.activeInHierarchy)
            {
                continue;
            }

            Character character = health.GetComponent<Character>();
            if (character == null || character.CharacterType == Character.CharacterTypes.Player)
            {
                continue;
            }

            if (health.CurrentHealth <= 0f
                && health.InitialHealth > 0f
                && character.ConditionState != null
                && character.ConditionState.CurrentState != CharacterStates.CharacterConditions.Dead)
            {
                health.CurrentHealth = health.InitialHealth;
            }
        }
    }

    protected virtual void RepairGroundedJumpStateAtInterval()
    {
        if (!repairGroundedJumpStateAtRuntime)
        {
            return;
        }

        if (Time.unscaledTime - _lastGroundedJumpRepairAt < groundedJumpStateRepairInterval)
        {
            return;
        }

        _lastGroundedJumpRepairAt = Time.unscaledTime;
        RepairGroundedJumpState();
    }

    protected virtual void RepairGroundedJumpState()
    {
        if (!repairGroundedJumpStateAtRuntime)
        {
            return;
        }

        CacheLevelManager();
        if (_levelManager == null || _levelManager.Players == null)
        {
            return;
        }

        for (int i = 0; i < _levelManager.Players.Count; i++)
        {
            Character playerCharacter = _levelManager.Players[i];
            if (playerCharacter == null || playerCharacter.CharacterType != Character.CharacterTypes.Player)
            {
                continue;
            }

            CorgiController controller = playerCharacter.GetComponent<CorgiController>();
            CharacterJump jump = playerCharacter.FindAbility<CharacterJump>();
            if (controller == null || jump == null || !controller.State.IsGrounded)
            {
                continue;
            }

            jump.ResetNumberOfJumps();
            jump.SetCanJumpStop(true);

            if (playerCharacter.MovementState != null
                && playerCharacter.MovementState.CurrentState == CharacterStates.MovementStates.Falling
                && controller.Speed.y <= 0.01f)
            {
                playerCharacter.MovementState.ChangeState(CharacterStates.MovementStates.Idle);
            }
        }
    }

    protected virtual Health FindDamageZoneOwnerHealth(DamageOnTouch damageZone)
    {
        if (damageZone == null)
        {
            return null;
        }

        Health health = damageZone.GetComponent<Health>();
        if (health != null)
        {
            return health;
        }

        health = damageZone.GetComponentInParent<Health>();
        if (health != null)
        {
            return health;
        }

        if (damageZone.Owner != null)
        {
            health = damageZone.Owner.GetComponent<Health>();
            if (health != null)
            {
                return health;
            }

            health = damageZone.Owner.GetComponentInParent<Health>();
            if (health != null)
            {
                return health;
            }
        }

        return null;
    }

    protected virtual void SetDamageZoneEnabled(DamageOnTouch damageZone, bool enabledState)
    {
        if (damageZone == null)
        {
            return;
        }

        Collider2D[] colliders = damageZone.GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = enabledState;
            }
        }

        damageZone.enabled = enabledState;
    }

    protected virtual void CapEnemyProjectileSpeedAtInterval()
    {
        if (!capEnemyMachineGunProjectileSpeed)
        {
            return;
        }

        float checkInterval = Mathf.Max(0.25f, enemyProjectileSpeedCheckInterval);
        if (Time.unscaledTime - _lastEnemyProjectileSpeedCheckAt < checkInterval)
        {
            return;
        }

        _lastEnemyProjectileSpeedCheckAt = Time.unscaledTime;
        CapEnemyProjectileSpeed();
    }

    protected virtual void CapEnemyProjectileSpeed()
    {
        Scene currentScene = gameObject.scene;
        Projectile[] projectiles = FindSceneComponents<Projectile>(false);
        for (int i = 0; i < projectiles.Length; i++)
        {
            Projectile projectile = projectiles[i];
            if (projectile == null
                || !projectile.gameObject.activeInHierarchy
                || projectile.gameObject.scene != currentScene
                || projectile.Speed <= enemyMachineGunProjectileMaxSpeed
                || !IsEnemyMachineGunProjectile(projectile))
            {
                continue;
            }

            projectile.Speed = enemyMachineGunProjectileMaxSpeed;
        }
    }

    protected virtual bool IsEnemyMachineGunProjectile(Projectile projectile)
    {
        if (projectile == null || !projectile.name.Contains("RetroMachineGunBullet"))
        {
            return false;
        }

        GameObject owner = projectile.GetOwner();
        if (owner == null)
        {
            return false;
        }

        Character ownerCharacter = owner.GetComponent<Character>();
        if (ownerCharacter == null)
        {
            ownerCharacter = owner.GetComponentInParent<Character>();
        }

        return ownerCharacter != null && ownerCharacter.CharacterType != Character.CharacterTypes.Player;
    }

    protected virtual void GrantRetroStartingWeapons()
    {
        if (!grantRetroWeaponsOnStart)
        {
            return;
        }

        CacheLevelManager();
        if (_levelManager == null || _levelManager.Players == null)
        {
            return;
        }

        if (!LoadRetroStartingWeapons())
        {
            return;
        }

        for (int i = 0; i < _levelManager.Players.Count; i++)
        {
            Character playerCharacter = _levelManager.Players[i];
            if (playerCharacter == null || playerCharacter.CharacterType != Character.CharacterTypes.Player)
            {
                continue;
            }

            EnsureAirAttackAnimationOverride(playerCharacter);

            CharacterInventory characterInventory = playerCharacter.GetComponentInChildren<CharacterInventory>(true);
            if (characterInventory == null || characterInventory.MainInventory == null)
            {
                continue;
            }

            EnsureInventoryHasRoom(characterInventory.MainInventory, 3);
            EnsureInventoryHasRoom(characterInventory.WeaponInventory, 1);

            bool inventoryChanged = false;
            bool mainInventoryNeedsRepair = MainInventoryNeedsStartingWeaponRepair(characterInventory.MainInventory, _cachedComboSword, _cachedMachineGun, _cachedShotgun);
            bool weaponInventoryIsEmpty = characterInventory.WeaponInventory == null
                                          || characterInventory.WeaponInventory.Content == null
                                          || characterInventory.WeaponInventory.Content.Length == 0
                                          || InventoryItem.IsNull(characterInventory.WeaponInventory.Content[0]);

            if (clearPlayerInventoriesBeforeGrantingWeapons)
            {
                inventoryChanged |= ClearInventorySlotsOutsideRange(characterInventory.MainInventory, 0, 2);
                inventoryChanged |= ClearInventoryContents(characterInventory.HotbarInventory);
            }

            if (mainInventoryNeedsRepair)
            {
                inventoryChanged |= SetWeaponAtSlot(characterInventory.MainInventory, _cachedComboSword, 0);
                inventoryChanged |= SetWeaponAtSlot(characterInventory.MainInventory, _cachedMachineGun, 1);
                inventoryChanged |= SetWeaponAtSlot(characterInventory.MainInventory, _cachedShotgun, 2);
            }

            if (equipRetroSwordOnStart && (!_retroWeaponsGranted || weaponInventoryIsEmpty))
            {
                inventoryChanged |= SetWeaponAtSlot(characterInventory.WeaponInventory, _cachedComboSword, 0);
                EquipStartingWeapon(playerCharacter, _cachedComboSword);
            }

            if (inventoryChanged || !_retroWeaponsGranted)
            {
                NotifyInventoryChanged(characterInventory.MainInventory, characterInventory.PlayerID);
                NotifyInventoryChanged(characterInventory.HotbarInventory, characterInventory.PlayerID);
                NotifyInventoryChanged(characterInventory.WeaponInventory, characterInventory.PlayerID);
            }

            _retroWeaponsGranted = true;
            return;
        }
    }

    protected virtual bool LoadRetroStartingWeapons()
    {
        if (_cachedComboSword != null && _cachedMachineGun != null && _cachedShotgun != null)
        {
            return true;
        }

        _cachedComboSword = Resources.Load<InventoryEngineWeapon>(retroSwordResourcePath);
        _cachedMachineGun = Resources.Load<InventoryEngineWeapon>(retroMachineGunResourcePath);
        _cachedShotgun = Resources.Load<InventoryEngineWeapon>(retroShotgunResourcePath);

        if (_cachedComboSword == null || _cachedMachineGun == null || _cachedShotgun == null)
        {
            return false;
        }

        PrepareStartingWeapon(_cachedComboSword);
        PrepareStartingWeapon(_cachedMachineGun);
        PrepareStartingWeapon(_cachedShotgun);
        return true;
    }

    protected virtual bool MainInventoryNeedsStartingWeaponRepair(Inventory inventory, InventoryEngineWeapon comboSword, InventoryEngineWeapon machineGun, InventoryEngineWeapon shotgun)
    {
        return !InventorySlotContains(inventory, 0, comboSword.ItemID)
               || !InventorySlotContains(inventory, 1, machineGun.ItemID)
               || !InventorySlotContains(inventory, 2, shotgun.ItemID);
    }

    protected virtual bool InventorySlotContains(Inventory inventory, int slot, string itemID)
    {
        return inventory != null
               && inventory.Content != null
               && slot >= 0
               && slot < inventory.Content.Length
               && !InventoryItem.IsNull(inventory.Content[slot])
               && inventory.Content[slot].ItemID == itemID;
    }

    protected virtual void SyncRetroStartingWeaponsAtInterval()
    {
        if (!grantRetroWeaponsOnStart || !keepRetroWeaponsSyncedAtRuntime)
        {
            return;
        }

        if (_retroWeaponsGranted && Time.unscaledTime > _retroWeaponSyncUntilTime)
        {
            return;
        }

        if (Time.unscaledTime - _lastRetroWeaponSyncAt < retroWeaponSyncInterval)
        {
            return;
        }

        _lastRetroWeaponSyncAt = Time.unscaledTime;
        GrantRetroStartingWeapons();
    }

    protected virtual void BeginRetroWeaponStartupSync()
    {
        if (!keepRetroWeaponsSyncedAtRuntime)
        {
            return;
        }

        _retroWeaponSyncUntilTime = Time.unscaledTime + retroWeaponStartupSyncDuration;
    }

    protected virtual void AddWeaponIfMissing(Inventory inventory, InventoryEngineWeapon weapon)
    {
        if (inventory == null || weapon == null)
        {
            return;
        }

        if (inventory.InventoryContains(weapon.ItemID).Count > 0)
        {
            return;
        }

        inventory.AddItem(weapon, 1);
    }

    protected virtual void EnsureInventoryHasRoom(Inventory inventory, int minimumSlots)
    {
        if (inventory == null)
        {
            return;
        }

        if (inventory.Content == null)
        {
            inventory.Content = new InventoryItem[minimumSlots];
            return;
        }

        if (inventory.Content.Length < minimumSlots)
        {
            inventory.ResizeArray(minimumSlots);
        }
    }

    protected virtual bool SetWeaponAtSlot(Inventory inventory, InventoryEngineWeapon weapon, int slot)
    {
        if (inventory == null || weapon == null || inventory.Content == null || slot >= inventory.Content.Length)
        {
            return false;
        }

        if (InventorySlotContains(inventory, slot, weapon.ItemID))
        {
            return false;
        }

        InventoryEngineWeapon weaponCopy = weapon.Copy() as InventoryEngineWeapon;
        if (weaponCopy == null)
        {
            return false;
        }

        PrepareStartingWeapon(weaponCopy);
        inventory.Content[slot] = weaponCopy;
        inventory.Content[slot].Quantity = 1;
        return true;
    }

    protected virtual void EquipStartingWeapon(Character playerCharacter, InventoryEngineWeapon weapon)
    {
        if (playerCharacter == null || weapon == null || weapon.EquippableWeapon == null)
        {
            return;
        }

        CharacterHandleWeapon[] handleWeapons = playerCharacter.GetComponentsInChildren<CharacterHandleWeapon>(true);
        for (int i = 0; i < handleWeapons.Length; i++)
        {
            CharacterHandleWeapon handleWeapon = handleWeapons[i];
            if (handleWeapon == null || handleWeapon.HandleWeaponID != weapon.HandleWeaponID)
            {
                continue;
            }

            handleWeapon.ChangeWeapon(weapon.EquippableWeapon, weapon.ItemID);
            ConfigureAttackMovementLock(handleWeapon.CurrentWeapon);
            return;
        }
    }

    protected virtual void PrepareStartingWeapon(InventoryEngineWeapon weapon)
    {
        if (weapon == null)
        {
            return;
        }

        weapon.MoveWhenEquipped = false;
        weapon.MaximumStack = 1;
        weapon.CanMoveObject = true;
        weapon.CanSwapObject = true;
        ConfigureAttackMovementLock(weapon.EquippableWeapon);
    }

    protected virtual void ConfigureAttackMovementLock(Weapon weapon)
    {
        if (weapon == null)
        {
            return;
        }

        weapon.PreventHorizontalGroundMovementWhileInUse = stopGroundMovementWhileAttacking;
    }

    protected virtual void EnsureAirAttackAnimationOverride(Character playerCharacter)
    {
        if (!installAirAttackAnimationOverride || playerCharacter == null)
        {
            return;
        }

        ResolveAirAttackClips();

        RetroAirAttackAnimationOverride airAttackOverride = playerCharacter.GetComponentInChildren<RetroAirAttackAnimationOverride>(true);
        if (airAttackOverride == null)
        {
            airAttackOverride = playerCharacter.gameObject.AddComponent<RetroAirAttackAnimationOverride>();
        }

        if (airAttackOverride.NormalAirAttackClip == null)
        {
            airAttackOverride.NormalAirAttackClip = normalAirAttackClip;
        }
        if (airAttackOverride.RageAirAttackClip == null)
        {
            airAttackOverride.RageAirAttackClip = rageAirAttackClip;
        }
    }

    protected virtual void EnsureAirAttackAnimationOverrides()
    {
        if (!installAirAttackAnimationOverride)
        {
            return;
        }

        CacheLevelManager();
        if (_levelManager == null || _levelManager.Players == null)
        {
            return;
        }

        for (int i = 0; i < _levelManager.Players.Count; i++)
        {
            Character playerCharacter = _levelManager.Players[i];
            if (playerCharacter == null || playerCharacter.CharacterType != Character.CharacterTypes.Player)
            {
                continue;
            }

            EnsureAirAttackAnimationOverride(playerCharacter);
        }
    }

    protected virtual void ResolveAirAttackClips()
    {
#if UNITY_EDITOR
        if (normalAirAttackClip == null)
        {
            normalAirAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/JumpAtk.anim");
        }
        if (rageAirAttackClip == null)
        {
            rageAirAttackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation4/JumpAtkL.anim");
        }
#endif
    }

    protected virtual void ClearInventory(Inventory inventory, string playerID)
    {
        if (inventory == null || inventory.Content == null)
        {
            return;
        }

        for (int i = 0; i < inventory.Content.Length; i++)
        {
            inventory.Content[i] = null;
        }

        RedrawInventory(inventory, playerID);
    }

    protected virtual bool ClearInventoryContents(Inventory inventory)
    {
        if (inventory == null || inventory.Content == null)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < inventory.Content.Length; i++)
        {
            if (InventoryItem.IsNull(inventory.Content[i]))
            {
                continue;
            }

            inventory.Content[i] = null;
            changed = true;
        }

        return changed;
    }

    protected virtual bool ClearInventorySlotsOutsideRange(Inventory inventory, int firstAllowedSlot, int lastAllowedSlot)
    {
        if (inventory == null || inventory.Content == null)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < inventory.Content.Length; i++)
        {
            if (i >= firstAllowedSlot && i <= lastAllowedSlot)
            {
                continue;
            }

            if (InventoryItem.IsNull(inventory.Content[i]))
            {
                continue;
            }

            inventory.Content[i] = null;
            changed = true;
        }

        return changed;
    }

    protected virtual void NotifyInventoryChanged(Inventory inventory, string playerID)
    {
        if (inventory == null)
        {
            return;
        }

        MMInventoryEvent.Trigger(MMInventoryEventType.ContentChanged, null, inventory.name, null, 0, 0, playerID);
        RedrawInventory(inventory, playerID);
    }

    protected virtual void RedrawInventory(Inventory inventory, string playerID)
    {
        if (inventory == null)
        {
            return;
        }

        MMInventoryEvent.Trigger(MMInventoryEventType.Redraw, null, inventory.name, null, 0, 0, playerID);
    }

    protected virtual void ReviveSceneItemPickers()
    {
        if (!reviveSceneItemPickersOnStart)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        RepairDecorativeWeaponPickers();
        ReviveInventoryPickableItems(currentScene);
        ReviveRegularPickableItems(currentScene);
    }

    protected virtual void ActivateScenePropsRoot()
    {
        if (!activatePropsRootOnStart || string.IsNullOrEmpty(scenePropsRootName))
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        Transform[] transforms = FindSceneComponents<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform targetTransform = transforms[i];
            if (targetTransform == null
                || targetTransform.gameObject.scene != currentScene
                || targetTransform.name != scenePropsRootName)
            {
                continue;
            }

            targetTransform.gameObject.SetActive(true);
        }
    }

    protected virtual void ConfigureGateGameOverTriggers()
    {
        if (!configureGateGameOverTriggersOnStart || gateGameOverObjectNames == null)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        Transform[] transforms = FindSceneComponents<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform targetTransform = transforms[i];
            if (targetTransform == null || targetTransform.gameObject.scene != currentScene)
            {
                continue;
            }

            if (!IsGateGameOverObjectName(targetTransform.name))
            {
                continue;
            }

            if (targetTransform.GetComponentInParent<CorgiCustomMechanics.ChapterTravelTrigger>() != null
                || targetTransform.GetComponentInChildren<CorgiCustomMechanics.ChapterTravelTrigger>(true) != null)
            {
                continue;
            }

            targetTransform.gameObject.SetActive(true);

            Collider2D triggerCollider = FindTriggerCollider(targetTransform.gameObject);
            if (triggerCollider == null)
            {
                BoxCollider2D boxCollider = targetTransform.gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = EstimateTriggerSize(targetTransform.gameObject);
                triggerCollider = boxCollider;
            }

            triggerCollider.enabled = true;

            GateGameOverTrigger gateTrigger = targetTransform.GetComponent<GateGameOverTrigger>();
            if (gateTrigger == null)
            {
                gateTrigger = targetTransform.gameObject.AddComponent<GateGameOverTrigger>();
            }

            gateTrigger.gameOverSceneName = gateGameOverSceneName;
            gateTrigger.enabled = true;
        }
    }

    protected virtual bool IsGateGameOverObjectName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        for (int i = 0; i < gateGameOverObjectNames.Length; i++)
        {
            if (objectName == gateGameOverObjectNames[i])
            {
                return true;
            }
        }

        return false;
    }

    protected virtual Collider2D FindTriggerCollider(GameObject targetObject)
    {
        Collider2D[] colliders = targetObject.GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].isTrigger)
            {
                return colliders[i];
            }
        }

        return null;
    }

    protected virtual Vector2 EstimateTriggerSize(GameObject targetObject)
    {
        Renderer renderer = targetObject.GetComponentInChildren<Renderer>(true);
        if (renderer == null)
        {
            return new Vector2(1.5f, 2.5f);
        }

        Vector3 localScale = targetObject.transform.lossyScale;
        float width = Mathf.Max(0.5f, renderer.bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(localScale.x)));
        float height = Mathf.Max(0.5f, renderer.bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(localScale.y)));
        return new Vector2(width, height);
    }

    protected virtual void RefreshItemPickerVisualsAtRuntime()
    {
        if (!keepSceneItemPickerVisualsVisibleAtRuntime)
        {
            return;
        }

        if (Time.unscaledTime - _lastItemPickerVisualRefreshAt < itemPickerVisualRefreshInterval)
        {
            return;
        }

        _lastItemPickerVisualRefreshAt = Time.unscaledTime;
        ForceSceneItemPickerVisuals();
    }

    protected virtual void RefreshScenePropVisualsAtRuntime()
    {
        if (!keepScenePropsVisibleAtRuntime)
        {
            return;
        }

        if (Time.unscaledTime - _lastScenePropVisualRefreshAt < itemPickerVisualRefreshInterval)
        {
            return;
        }

        _lastScenePropVisualRefreshAt = Time.unscaledTime;
        DisableBackgroundDepthWrite();
        ForceScenePropsVisible();
    }

    protected virtual void DisableBackgroundDepthWrite()
    {
        if (!disableBackgroundDepthWriteOnStart)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        MeshRenderer[] renderers = FindSceneComponents<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer sceneRenderer = renderers[i];
            if (sceneRenderer == null
                || sceneRenderer.gameObject.scene != currentScene
                || !IsBackgroundRenderer(sceneRenderer))
            {
                continue;
            }

            sceneRenderer.allowOcclusionWhenDynamic = false;
            sceneRenderer.sortingOrder = Mathf.Min(sceneRenderer.sortingOrder, -1000);

            Material[] sharedMaterials = sceneRenderer.sharedMaterials;
            for (int j = 0; j < sharedMaterials.Length; j++)
            {
                Material material = sharedMaterials[j];
                if (material != null && material.HasProperty("_ZWrite"))
                {
                    material.SetInt("_ZWrite", 0);
                }
            }
        }
    }

    protected virtual bool IsBackgroundRenderer(Renderer sceneRenderer)
    {
        if (!(sceneRenderer is MeshRenderer))
        {
            return false;
        }

        if (HasParentNamed(sceneRenderer.transform, "RetroMountainsBackground"))
        {
            return true;
        }

        Material sharedMaterial = sceneRenderer.sharedMaterial;
        return sharedMaterial != null
               && sharedMaterial.name.ToLowerInvariant().Contains("background");
    }

    protected virtual void ForceScenePropsVisible()
    {
        if (!forceScenePropsVisibleOnStart)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        Transform[] transforms = FindSceneComponents<Transform>(true);
        HashSet<Transform> processedRoots = new HashSet<Transform>();
        if (!string.IsNullOrEmpty(scenePropsRootName))
        {
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform rootTransform = transforms[i];
                if (rootTransform == null
                    || rootTransform.gameObject.scene != currentScene
                    || rootTransform.name != scenePropsRootName)
                {
                    continue;
                }

                ForceScenePropRootVisible(rootTransform, processedRoots);
            }
        }

        if (scenePropNameMarkers == null || scenePropNameMarkers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform rootTransform = transforms[i];
            if (rootTransform == null
                || rootTransform.gameObject.scene != currentScene
                || !NameMatchesAnyMarker(rootTransform.name, scenePropNameMarkers))
            {
                continue;
            }

            ForceScenePropRootVisible(rootTransform, processedRoots);
        }
    }

    protected virtual void ForceScenePropRootVisible(Transform rootTransform, HashSet<Transform> processedRoots)
    {
        if (rootTransform == null || processedRoots.Contains(rootTransform))
        {
            return;
        }

        processedRoots.Add(rootTransform);
        ActivateParentChain(rootTransform);
        rootTransform.gameObject.SetActive(true);

        Renderer[] renderers = rootTransform.GetComponentsInChildren<Renderer>(true);
        for (int j = 0; j < renderers.Length; j++)
        {
            Renderer propRenderer = renderers[j];
            if (!IsRegularScenePropRenderer(propRenderer))
            {
                continue;
            }

            ForceRendererVisible(propRenderer, scenePropVisualSortingOrder, normalizeScenePropDepth);
        }
    }

    protected virtual bool IsRegularScenePropRenderer(Renderer propRenderer)
    {
        return propRenderer != null
               && !(propRenderer is TilemapRenderer)
               && !IsBackgroundRenderer(propRenderer)
               && propRenderer.GetComponentInParent<Character>(true) == null
               && propRenderer.GetComponentInParent<InventoryPickableItem>(true) == null
               && propRenderer.GetComponentInParent<PickableItem>(true) == null;
    }

    protected virtual bool HasAnyParentNameMarker(Transform targetTransform, string[] nameMarkers)
    {
        if (nameMarkers == null)
        {
            return false;
        }

        Transform currentTransform = targetTransform;
        while (currentTransform != null)
        {
            for (int i = 0; i < nameMarkers.Length; i++)
            {
                string marker = nameMarkers[i];
                if (!string.IsNullOrEmpty(marker) && currentTransform.name.Contains(marker))
                {
                    return true;
                }
            }

            currentTransform = currentTransform.parent;
        }

        return false;
    }

    protected virtual bool NameMatchesAnyMarker(string objectName, string[] nameMarkers)
    {
        if (string.IsNullOrEmpty(objectName) || nameMarkers == null)
        {
            return false;
        }

        for (int i = 0; i < nameMarkers.Length; i++)
        {
            string marker = nameMarkers[i];
            if (!string.IsNullOrEmpty(marker) && objectName.Contains(marker))
            {
                return true;
            }
        }

        return false;
    }

    protected virtual void ForceSceneItemPickerVisuals()
    {
        Scene currentScene = gameObject.scene;

        InventoryPickableItem[] inventoryPickers = FindSceneComponents<InventoryPickableItem>(true);
        for (int i = 0; i < inventoryPickers.Length; i++)
        {
            InventoryPickableItem picker = inventoryPickers[i];
            if (picker == null
                || picker.gameObject.scene != currentScene
                || !picker.gameObject.activeSelf
                || picker.RemainingQuantity <= 0)
            {
                continue;
            }

            ForcePickerVisibleAndCollectible(picker.gameObject, true);
            picker.enabled = true;
        }

        PickableItem[] regularPickers = FindSceneComponents<PickableItem>(true);
        for (int i = 0; i < regularPickers.Length; i++)
        {
            PickableItem picker = regularPickers[i];
            if (picker == null
                || picker.gameObject.scene != currentScene
                || !picker.gameObject.activeSelf)
            {
                continue;
            }

            ForcePickerVisibleAndCollectible(picker.gameObject, true);
            picker.enabled = true;
        }
    }

    protected virtual void ReviveInventoryPickableItems(Scene currentScene)
    {
        InventoryPickableItem[] pickers = FindSceneComponents<InventoryPickableItem>(true);
        for (int i = 0; i < pickers.Length; i++)
        {
            InventoryPickableItem picker = pickers[i];
            if (picker == null || picker.gameObject.scene != currentScene)
            {
                continue;
            }

            AutoRespawn autoRespawn = picker.GetComponent<AutoRespawn>();
            if (autoRespawn != null)
            {
                autoRespawn.Revive();
            }

            ForcePickerVisibleAndCollectible(picker.gameObject, true);
            picker.ResetQuantity();
            picker.enabled = true;
        }
    }

    protected virtual void ReviveRegularPickableItems(Scene currentScene)
    {
        PickableItem[] pickers = FindSceneComponents<PickableItem>(true);
        for (int i = 0; i < pickers.Length; i++)
        {
            PickableItem picker = pickers[i];
            if (picker == null || picker.gameObject.scene != currentScene)
            {
                continue;
            }

            AutoRespawn autoRespawn = picker.GetComponent<AutoRespawn>();
            if (autoRespawn != null)
            {
                autoRespawn.Revive();
            }

            ForcePickerVisibleAndCollectible(picker.gameObject, true);
            picker.enabled = true;
        }
    }

    protected virtual void ForcePickerVisibleAndCollectible(GameObject pickerObject, bool forceVisualsInFront = false)
    {
        if (pickerObject == null)
        {
            return;
        }

        ActivateParentChain(pickerObject.transform);
        pickerObject.SetActive(true);

        Renderer[] renderers = pickerObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer pickerRenderer = renderers[i];
            if (pickerRenderer != null)
            {
                int sortingOrder = forceSceneItemPickerVisualsInFront && forceVisualsInFront
                    ? itemPickerVisualSortingOrder
                    : pickerRenderer.sortingOrder;
                ForceRendererVisible(pickerRenderer, sortingOrder, normalizeScenePropDepth);
            }
        }

        Collider2D[] colliders = pickerObject.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D pickerCollider = colliders[i];
            if (pickerCollider != null)
            {
                ActivateParentChain(pickerCollider.transform);
                pickerCollider.gameObject.SetActive(true);
                pickerCollider.enabled = true;

                if (pickerCollider.GetComponentInParent<InventoryPickableItem>() != null
                    || pickerCollider.GetComponentInParent<PickableItem>() != null)
                {
                    pickerCollider.isTrigger = true;
                }
            }
        }
    }

    protected virtual void ForceRendererVisible(Renderer targetRenderer, int minimumSortingOrder, bool normalizeDepth)
    {
        if (targetRenderer == null)
        {
            return;
        }

        ActivateParentChain(targetRenderer.transform);
        targetRenderer.gameObject.SetActive(true);
        targetRenderer.enabled = true;
        targetRenderer.allowOcclusionWhenDynamic = false;
        targetRenderer.sortingOrder = Mathf.Max(targetRenderer.sortingOrder, minimumSortingOrder);

        SpriteRenderer spriteRenderer = targetRenderer as SpriteRenderer;
        if (spriteRenderer != null)
        {
            spriteRenderer.maskInteraction = SpriteMaskInteraction.None;

            if (spriteRenderer.color.a < 1f)
            {
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }
        }

        if (normalizeDepth)
        {
            Vector3 position = targetRenderer.transform.position;
            position.z = scenePropWorldZ;
            targetRenderer.transform.position = position;
        }
    }

    protected virtual bool HasParentNamed(Transform targetTransform, string parentName)
    {
        Transform currentTransform = targetTransform;
        while (currentTransform != null)
        {
            if (currentTransform.name == parentName)
            {
                return true;
            }

            currentTransform = currentTransform.parent;
        }

        return false;
    }

    protected virtual void ActivateParentChain(Transform targetTransform)
    {
        Transform currentTransform = targetTransform;
        while (currentTransform != null)
        {
            if (!currentTransform.gameObject.activeSelf)
            {
                currentTransform.gameObject.SetActive(true);
            }

            currentTransform = currentTransform.parent;
        }
    }

    protected virtual void RepairDecorativeWeaponPickers()
    {
        if (!repairDecorativeWeaponPickersOnStart)
        {
            return;
        }

        Scene currentScene = gameObject.scene;
        SpriteRenderer[] spriteRenderers = FindSceneComponents<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null || spriteRenderer.gameObject.scene != currentScene)
            {
                continue;
            }

            GameObject pickerObject = spriteRenderer.gameObject;
            if (!LooksLikeWeaponPicker(pickerObject.name))
            {
                continue;
            }

            InventoryItem item = ResolveWeaponPickerItem(pickerObject.name);
            if (item == null)
            {
                continue;
            }

            InventoryPickableItem picker = pickerObject.GetComponent<InventoryPickableItem>();
            if (picker == null)
            {
                picker = pickerObject.AddComponent<InventoryPickableItem>();
            }

            picker.Item = item;
            picker.Quantity = Mathf.Max(1, picker.Quantity);
            picker.ResetQuantity();
            picker.PickableIfInventoryIsFull = false;
            picker.DisableObjectWhenDepleted = true;
            picker.enabled = true;

            Collider2D collider = pickerObject.GetComponent<Collider2D>();
            if (collider == null)
            {
                CircleCollider2D circleCollider = pickerObject.AddComponent<CircleCollider2D>();
                circleCollider.radius = Mathf.Max(0.05f, repairedWeaponPickerRadius);
                collider = circleCollider;
            }

            collider.isTrigger = true;
            collider.enabled = true;
            spriteRenderer.enabled = true;
            ForcePickerVisibleAndCollectible(pickerObject, true);
        }
    }

    protected virtual bool LooksLikeWeaponPicker(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        return objectName.Contains("RetroShotgunPicker")
               || objectName.Contains("RetroMachineGunPicker")
               || objectName.Contains("RetroSwordPicker")
               || objectName.Contains("RetroComboSwordPicker");
    }

    protected virtual InventoryItem ResolveWeaponPickerItem(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return null;
        }

        if (objectName.Contains("RetroShotgunPicker"))
        {
            return Resources.Load<InventoryItem>("Items/RetroShotgun");
        }

        if (objectName.Contains("RetroMachineGunPicker"))
        {
            return Resources.Load<InventoryItem>("Items/RetroMachineGun");
        }

        if (objectName.Contains("RetroComboSwordPicker"))
        {
            return Resources.Load<InventoryItem>("Items/RetroComboSword");
        }

        if (objectName.Contains("RetroSwordPicker"))
        {
            return Resources.Load<InventoryItem>("Items/RetroSword");
        }

        return null;
    }

    protected virtual void RefreshPlayerAbilityInputManagers()
    {
        if (!refreshAbilityInputManagersAfterSpawn)
        {
            return;
        }

        CacheLevelManager();
        if (_levelManager == null || _levelManager.Players == null)
        {
            return;
        }

        for (int i = 0; i < _levelManager.Players.Count; i++)
        {
            Character playerCharacter = _levelManager.Players[i];
            if (playerCharacter == null || playerCharacter.LinkedInputManager == null)
            {
                continue;
            }

            CharacterAbility[] abilities = playerCharacter.GetComponentsInChildren<CharacterAbility>(true);
            for (int j = 0; j < abilities.Length; j++)
            {
                if (abilities[j] == null)
                {
                    continue;
                }

                abilities[j].SetInputManager(playerCharacter.LinkedInputManager);
            }
        }
    }
}

//[DisallowMultipleComponent]
//public class GateGameOverTrigger : MonoBehaviour
//{
//    public string gameOverSceneName = "RetroAdventureGameOver";

//    private void OnTriggerEnter2D(Collider2D other)
//    {
//        Character character = other.GetComponentInParent<Character>();
//        if (character == null || character.CharacterType != Character.CharacterTypes.Player)
//        {
//            return;
//        }

//        if (!string.IsNullOrEmpty(gameOverSceneName))
//        {
//            SceneManager.LoadScene(gameOverSceneName);
//        }
//    }
//}
