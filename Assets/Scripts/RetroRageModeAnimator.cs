using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Swaps the character animator to a rage override controller for a short duration.
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/Abilities/Retro Rage Mode Animator")]
    public class RetroRageModeAnimator : CharacterAbility, MMEventListener<MMDamageTakenEvent>
    {
        [Header("Input")]
        [Tooltip("If true, pressing RageKey will trigger rage mode.")]
        public bool ReadInput = true;
        [Tooltip("The keyboard key used to trigger rage mode.")]
        public KeyCode RageKey = KeyCode.C;

        [Header("Animator Controllers")]
        [Tooltip("The normal animator controller. If empty, this is captured from the character animator on start.")]
        public RuntimeAnimatorController NormalAnimatorController;
        [Tooltip("The animator override controller used while rage mode is active.")]
        public RuntimeAnimatorController RageAnimatorController;

        [Header("Animation States")]
        [Tooltip("Animator state to play when rage mode starts. This state must exist in the rage controller's base animator.")]
        public string AbilityAppearStateName = "AbilityAppear";
        [Tooltip("Animator state to play before rage mode ends. This state must exist in the rage controller's base animator.")]
        public string AbilityDisappearStateName = "AbilityDisappear";

        [Header("Timing")]
        [Tooltip("How long rage mode lasts from the moment it is triggered.")]
        public float RageDuration = 15f;
        [Tooltip("Optional clip reference used only to know how long to wait after playing AbilityAppear.")]
        public AnimationClip AbilityAppearClip;
        [Tooltip("Optional clip reference used only to know how long to wait after playing AbilityDisappear.")]
        public AnimationClip AbilityDisappearClip;
        [Tooltip("Fallback wait time if AbilityAppearClip is not assigned.")]
        public float AbilityAppearFallbackDuration = 0.25f;
        [Tooltip("Fallback wait time if AbilityDisappearClip is not assigned.")]
        public float AbilityDisappearFallbackDuration = 0.25f;
        [Tooltip("If true, the appear animation time is included in RageDuration.")]
        public bool CountAppearTimeInRageDuration = true;
        [Tooltip("If true, horizontal movement is paused while AbilityAppear plays.")]
        public bool FreezeMovementDuringAbilityAppear = true;

        [Header("Rage Meter")]
        [Tooltip("If true, rage mode starts only when the rage meter is full.")]
        public bool RageRequiresFullMeter = true;
        [Tooltip("The UI Slider used for the rage meter. If empty, Slider_RageMode is found automatically.")]
        public Slider RageMeterSlider;
        public string RageMeterSliderName = "Slider_RageMode";
        [Tooltip("Rage gained for one enemy kill. 2 means 5 kills fills a 10 point meter.")]
        public float RageGainPerEnemyKill = 2f;
        public float RageMeterMaxValue = 10f;
        [Tooltip("Forces rage mode to last this many seconds when started by the meter.")]
        public float RageMeterDuration = 15f;
        [Tooltip("If true, enemy kills during active rage do not refill the next meter.")]
        public bool IgnoreKillsDuringRage = true;
        [Tooltip("How often alive enemy health components are scanned for death events.")]
        public float EnemyDeathScanInterval = 0.5f;

        [Header("Rage Buffs")]
        [Tooltip("Movement speed multiplier applied while rage mode is active.")]
        public float RageMovementSpeedMultiplier = 1.5f;
        [Tooltip("Weapon timing multiplier applied while rage mode is active. Lower values make sword combos faster.")]
        public float RageWeaponTimingMultiplier = 0.65f;
        [Tooltip("Melee damage multiplier applied while rage mode is active.")]
        public float RageMeleeDamageMultiplier = 1.5f;
        [Tooltip("If true, all melee weapons in the current combo are buffed. If false, only the current weapon is buffed.")]
        public bool BuffAllComboWeapons = true;

        [Header("Rage Visuals")]
        [Tooltip("If true, the character sprite is tinted while rage mode is active.")]
        public bool TintCharacterDuringRage = false;
        [Tooltip("If true, blocks the old blue character sprite tint even if old prefab values still have tint enabled.")]
        public bool DisableCharacterSpriteTint = true;
        [Tooltip("Sprite tint applied while rage mode is active.")]
        public Color RageSpriteColor = new Color(0.25f, 0.65f, 1f, 1f);
        [Tooltip("If true, walking feedback particles are tinted to match the current character sprite color while rage mode is active.")]
        public bool MatchWalkFeedbackToSpriteColor = true;
        [Tooltip("Particle systems played while rage mode is active. Assign child aura/blue particle systems here from the player prefab.")]
        public ParticleSystem[] RageModeParticles;
        [Tooltip("If true, rage mode particles are tinted with RageParticleColor when rage starts.")]
        public bool TintRageModeParticles = true;
        [Tooltip("Particle tint applied to RageModeParticles while rage mode is active.")]
        public Color RageParticleColor = new Color(0.25f, 0.75f, 1f, 1f);
        [Tooltip("If true, rage mode particles are cleared when rage ends instead of only stopping emission.")]
        public bool ClearRageModeParticlesOnStop = true;
        [Tooltip("If true, hides the normal sword slash helper sprite while rage mode is active.")]
        public bool HideNormalSlashSpriteDuringRage = true;
        public string NormalSlashSpriteName = "RetroSwordSlash";

        [Header("Bolt Drop")]
        [Tooltip("If true, a bolt is spawned after AbilityAppear finishes.")]
        public bool DropBoltAfterAbilityAppear = false;
        [Tooltip("Optional prefab to spawn for the bolt. If empty, a temporary runtime object is created.")]
        public GameObject BoltPrefab;
        [Tooltip("Optional animation clip to play on the bolt object.")]
        public AnimationClip BoltClip;
        [Tooltip("How far above the enemy center the bolt visual spawns. X is always locked to the enemy center.")]
        public float BoltDropYOffset = 2f;
        [Tooltip("Extra Y offset applied to the enemy center before adding BoltDropYOffset.")]
        public float BoltTargetCenterYOffset = 0f;
        [Tooltip("How long the bolt object remains if no clip length can be found.")]
        public float BoltFallbackLifetime = 0.5f;
        [Tooltip("If true, the bolt is resized after its sprite appears so its height is based on the character height.")]
        public bool ScaleBoltToCharacterHeight = true;
        [Tooltip("Bolt height compared to the character height. 2 means twice as tall as the character.")]
        public float BoltCharacterHeightMultiplier = 2f;
        [Tooltip("Fallback transform scale if the bolt or character renderer bounds can't be read.")]
        public float BoltFallbackScale = 2f;
        [Tooltip("Manual X/Y multiplier applied after the bolt is scaled. Use this to stretch or squash the bolt from the Inspector.")]
        public Vector2 BoltManualScaleMultiplier = Vector2.one;
        [Tooltip("If true, draws a selected gizmo preview for the bolt search radius, visual size, and damage area.")]
        public bool ShowBoltGizmos = true;
        public Color BoltSearchGizmoColor = new Color(0.25f, 0.65f, 1f, 0.3f);
        public Color BoltVisualGizmoColor = new Color(0.65f, 0.9f, 1f, 0.9f);
        public Color BoltDamageGizmoColor = new Color(1f, 0.2f, 0.1f, 0.8f);
        public Color BoltGroundGizmoColor = new Color(0.2f, 1f, 0.35f, 0.9f);
        [Tooltip("If true, the bolt shifts vertically after scaling so its bottom touches the ground and never goes below it.")]
        public bool ClampBoltBottomToGround = true;
        [Tooltip("Layers used to detect ground under the bolt.")]
        public LayerMask BoltGroundLayerMask = LayerManager.ObstaclesLayerMask;
        [Tooltip("How far above the bolt position the ground raycast starts.")]
        public float BoltGroundRaycastStartHeight = 6f;
        [Tooltip("How far below the raycast start point to search for ground.")]
        public float BoltGroundRaycastDistance = 16f;
        [Tooltip("Extra vertical spacing from the ground. Keep 0 to touch the ground exactly.")]
        public float BoltGroundYOffset = 0f;
        [Tooltip("How far from the character to search for an enemy target.")]
        public float BoltTargetSearchRadius = 12f;
        [Tooltip("Layers used to find enemies for the bolt target.")]
        public LayerMask BoltTargetLayerMask = LayerManager.EnemiesLayerMask;
        [Tooltip("If true, AI Characters with Health can be targeted even if their collider layer is not in BoltTargetLayerMask.")]
        public bool BoltCanTargetAnyAICharacter = true;
        [Tooltip("If true, the bolt creates a damage hitbox at the enemy center.")]
        public bool BoltDealsDamage = false;
        public float BoltDamage = 10f;
        public Vector2 BoltDamageAreaSize = new Vector2(1f, 1.5f);
        public float BoltDamageActiveDuration = 0.15f;
        public float BoltDamageInvincibilityDuration = 0.1f;

        public bool RageModeActive { get; protected set; }

        protected Coroutine _rageCoroutine;
        protected PlayableGraph _clipGraph;
        protected PlayableGraph _boltGraph;
        protected bool _storedHorizontalMovementReadInput;
        protected bool _storedHorizontalMovementPermitted;
        protected bool _horizontalMovementFrozen;
        protected float _storedAbilityMovementSpeedMultiplier = 1f;
        protected bool _rageBuffsApplied;
        protected CharacterHandleWeapon _characterHandleWeapon;
        protected readonly List<WeaponBuffSnapshot> _weaponBuffSnapshots = new List<WeaponBuffSnapshot>();
        protected readonly List<SpriteColorSnapshot> _spriteColorSnapshots = new List<SpriteColorSnapshot>();
        protected readonly List<ParticleColorSnapshot> _walkFeedbackColorSnapshots = new List<ParticleColorSnapshot>();
        protected readonly List<ParticlePlaybackSnapshot> _rageParticlePlaybackSnapshots = new List<ParticlePlaybackSnapshot>();
        protected readonly List<RendererEnabledSnapshot> _rendererEnabledSnapshots = new List<RendererEnabledSnapshot>();
        protected readonly HashSet<Health> _trackedEnemyHealths = new HashSet<Health>();
        protected bool _rageVisualsApplied;
        protected float _rageMeterValue;
        protected float _nextEnemyScanTime;

        protected struct WeaponBuffSnapshot
        {
            public Weapon Weapon;
            public float DelayBeforeUse;
            public float TimeBetweenUses;
            public MeleeWeapon MeleeWeapon;
            public int DamageCaused;
            public float InitialDelay;
            public float ActiveDuration;
        }

        protected struct SpriteColorSnapshot
        {
            public SpriteRenderer Renderer;
            public Color Color;
        }

        protected struct ParticleColorSnapshot
        {
            public ParticleSystem ParticleSystem;
            public ParticleSystem.MinMaxGradient StartColor;
        }

        protected struct ParticlePlaybackSnapshot
        {
            public ParticleSystem ParticleSystem;
            public bool WasPlaying;
        }

        protected struct RendererEnabledSnapshot
        {
            public Renderer Renderer;
            public bool Enabled;
        }

        public override string HelpBoxText()
        {
            return "Press the configured key to swap this character to a rage animator override, play appear/disappear states, and return to the normal controller after the configured duration.";
        }

        protected override void Initialization()
        {
            base.Initialization();

            if ((_animator == null) && (_character != null) && (_character.CharacterModel != null))
            {
                _animator = _character.CharacterModel.GetComponentInChildren<Animator>();
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if ((_animator != null) && (NormalAnimatorController == null))
            {
                NormalAnimatorController = _animator.runtimeAnimatorController;
            }

            _characterHandleWeapon = _character?.FindAbility<CharacterHandleWeapon>();
            RageDuration = Mathf.Max(0f, RageMeterDuration);
            InitializeRageMeter();
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            ProcessRageMeterTracking();

            if (!ReadInput || !AbilityAuthorized || (_rageCoroutine != null))
            {
                return;
            }

            if (Input.GetKeyDown(RageKey))
            {
                TriggerRageMode();
            }
        }

        public virtual void TriggerRageMode()
        {
            if ((_rageCoroutine != null) || (_animator == null) || (RageAnimatorController == null))
            {
                return;
            }

            if (RageRequiresFullMeter && (_rageMeterValue < RageMeterMaxValue))
            {
                return;
            }

            if (NormalAnimatorController == null)
            {
                NormalAnimatorController = _animator.runtimeAnimatorController;
            }

            RageDuration = Mathf.Max(0f, RageMeterDuration);
            SetRageMeterValue(RageMeterMaxValue);
            _rageCoroutine = StartCoroutine(RageModeCoroutine());
        }

        protected virtual IEnumerator RageModeCoroutine()
        {
            RageModeActive = true;
            PlayAbilityStartFeedbacks();

            float appearDuration = GetClipDuration(AbilityAppearClip, AbilityAppearFallbackDuration);
            yield return PlayClipOrAnimatorState(AbilityAppearClip, AbilityAppearStateName, appearDuration, FreezeMovementDuringAbilityAppear);

            DropBoltOnCurrentEnemy();

            _animator.runtimeAnimatorController = RageAnimatorController;
            ApplyRageBuffs();
            ApplyRageVisuals();

            float activeDuration = Mathf.Max(0f, RageDuration);
            if (CountAppearTimeInRageDuration)
            {
                activeDuration = Mathf.Max(0f, activeDuration - appearDuration);
            }

            if (activeDuration > 0f)
            {
                yield return new WaitForSeconds(activeDuration);
            }

            float disappearDuration = GetClipDuration(AbilityDisappearClip, AbilityDisappearFallbackDuration);
            yield return PlayClipOrAnimatorState(AbilityDisappearClip, AbilityDisappearStateName, disappearDuration, false);

            RestoreRageBuffs();
            RestoreRageVisuals();
            RestoreNormalAnimatorController();
            SyncNormalAnimatorToCurrentMovement();
            PlayAbilityStopFeedbacks();

            RageModeActive = false;
            _rageCoroutine = null;
            ResetRageMeter();
        }

        protected virtual void InitializeRageMeter()
        {
            RageMeterMaxValue = Mathf.Max(0.01f, RageMeterMaxValue);
            BindRageMeterSlider();
            ResetRageMeter();
        }

        protected virtual void BindRageMeterSlider()
        {
            if (RageMeterSlider != null)
            {
                ConfigureRageMeterSlider();
                return;
            }

            if (string.IsNullOrEmpty(RageMeterSliderName))
            {
                return;
            }

            GameObject sliderObject = GameObject.Find(RageMeterSliderName);
            if (sliderObject != null)
            {
                RageMeterSlider = sliderObject.GetComponent<Slider>();
            }

            ConfigureRageMeterSlider();
        }

        protected virtual void ConfigureRageMeterSlider()
        {
            if (RageMeterSlider == null)
            {
                return;
            }

            RageMeterSlider.minValue = 0f;
            RageMeterSlider.maxValue = RageMeterMaxValue;
            RageMeterSlider.wholeNumbers = false;
            RageMeterSlider.interactable = false;
            RageMeterSlider.value = Mathf.Clamp(_rageMeterValue, 0f, RageMeterMaxValue);
        }

        protected virtual void ProcessRageMeterTracking()
        {
            if (Time.unscaledTime < _nextEnemyScanTime)
            {
                return;
            }

            _nextEnemyScanTime = Time.unscaledTime + Mathf.Max(0.05f, EnemyDeathScanInterval);
            if (RageMeterSlider == null)
            {
                BindRageMeterSlider();
            }
        }

        protected virtual void ScanEnemyHealths()
        {
            Health[] healths = FindObjectsByType<Health>(FindObjectsSortMode.None);
            for (int i = 0; i < healths.Length; i++)
            {
                Health health = healths[i];
                if (!IsRageMeterEnemy(health) || _trackedEnemyHealths.Contains(health))
                {
                    continue;
                }

                _trackedEnemyHealths.Add(health);
                health.OnDeath += HandleTrackedEnemyDeath;
            }
        }

        protected virtual bool IsRageMeterEnemy(Health health)
        {
            if ((health == null) || (health.CurrentHealth <= 0f) || IsSelfHealth(health))
            {
                return false;
            }

            Character character = health.GetComponent<Character>();
            if (character == null)
            {
                character = health.GetComponentInParent<Character>();
            }

            return (character != null) && (character.CharacterType == Character.CharacterTypes.AI);
        }

        protected virtual void HandleTrackedEnemyDeath()
        {
            if (IgnoreKillsDuringRage && RageModeActive)
            {
                return;
            }

            AddRageMeterValue(RageGainPerEnemyKill);
        }

        public virtual void OnMMEvent(MMDamageTakenEvent damageTakenEvent)
        {
            if (damageTakenEvent.CurrentHealth > 0f)
            {
                return;
            }

            if (damageTakenEvent.PreviousHealth <= 0f)
            {
                return;
            }

            if (damageTakenEvent.AffectedCharacter == null
                || damageTakenEvent.AffectedCharacter == _character
                || damageTakenEvent.AffectedCharacter.CharacterType == Character.CharacterTypes.Player)
            {
                return;
            }

            if (!WasDamageCausedByThisCharacter(damageTakenEvent.Instigator))
            {
                return;
            }

            AddRageMeterValue(RageGainPerEnemyKill);
        }

        protected virtual bool WasDamageCausedByThisCharacter(GameObject instigator)
        {
            if ((_character == null) || (instigator == null))
            {
                return false;
            }

            Transform characterTransform = _character.transform;
            Transform instigatorTransform = instigator.transform;

            return instigatorTransform == characterTransform
                   || instigatorTransform.IsChildOf(characterTransform)
                   || characterTransform.IsChildOf(instigatorTransform);
        }

        protected virtual void AddRageMeterValue(float amount)
        {
            if ((_rageCoroutine != null) || RageModeActive)
            {
                return;
            }

            SetRageMeterValue(_rageMeterValue + amount);
            if (_rageMeterValue >= RageMeterMaxValue)
            {
                TriggerRageMode();
            }
        }

        protected virtual void SetRageMeterValue(float value)
        {
            RageMeterMaxValue = Mathf.Max(0.01f, RageMeterMaxValue);
            _rageMeterValue = Mathf.Clamp(value, 0f, RageMeterMaxValue);

            if (RageMeterSlider == null)
            {
                BindRageMeterSlider();
            }

            if (RageMeterSlider != null)
            {
                RageMeterSlider.maxValue = RageMeterMaxValue;
                RageMeterSlider.value = _rageMeterValue;
            }
        }

        protected virtual void ResetRageMeter()
        {
            SetRageMeterValue(0f);
        }

        protected virtual void UnregisterEnemyHealths()
        {
            foreach (Health health in _trackedEnemyHealths)
            {
                if (health != null)
                {
                    health.OnDeath -= HandleTrackedEnemyDeath;
                }
            }

            _trackedEnemyHealths.Clear();
        }

        protected virtual void ApplyRageBuffs()
        {
            if (_rageBuffsApplied)
            {
                return;
            }

            if (_characterHorizontalMovement != null)
            {
                _storedAbilityMovementSpeedMultiplier = _characterHorizontalMovement.AbilityMovementSpeedMultiplier;
                _characterHorizontalMovement.AbilityMovementSpeedMultiplier *= Mathf.Max(0f, RageMovementSpeedMultiplier);
            }

            _weaponBuffSnapshots.Clear();
            Weapon[] weapons = GetWeaponsToBuff();
            float timingMultiplier = Mathf.Max(0.01f, RageWeaponTimingMultiplier);
            float damageMultiplier = Mathf.Max(0f, RageMeleeDamageMultiplier);

            for (int i = 0; i < weapons.Length; i++)
            {
                Weapon weapon = weapons[i];
                if (weapon == null)
                {
                    continue;
                }

                MeleeWeapon meleeWeapon = weapon as MeleeWeapon;
                WeaponBuffSnapshot snapshot = new WeaponBuffSnapshot
                {
                    Weapon = weapon,
                    DelayBeforeUse = weapon.DelayBeforeUse,
                    TimeBetweenUses = weapon.TimeBetweenUses,
                    MeleeWeapon = meleeWeapon,
                    DamageCaused = (meleeWeapon != null) ? meleeWeapon.DamageCaused : 0,
                    InitialDelay = (meleeWeapon != null) ? meleeWeapon.InitialDelay : 0f,
                    ActiveDuration = (meleeWeapon != null) ? meleeWeapon.ActiveDuration : 0f
                };
                _weaponBuffSnapshots.Add(snapshot);

                weapon.DelayBeforeUse *= timingMultiplier;
                weapon.TimeBetweenUses *= timingMultiplier;

                if (meleeWeapon != null)
                {
                    meleeWeapon.InitialDelay *= timingMultiplier;
                    meleeWeapon.ActiveDuration *= timingMultiplier;
                    meleeWeapon.DamageCaused = Mathf.RoundToInt(meleeWeapon.DamageCaused * damageMultiplier);
                    UpdateMeleeDamageOnTouch(meleeWeapon);
                }
            }

            _rageBuffsApplied = true;
        }

        protected virtual void RestoreRageBuffs()
        {
            if (!_rageBuffsApplied)
            {
                return;
            }

            if (_characterHorizontalMovement != null)
            {
                _characterHorizontalMovement.AbilityMovementSpeedMultiplier = _storedAbilityMovementSpeedMultiplier;
            }

            for (int i = 0; i < _weaponBuffSnapshots.Count; i++)
            {
                WeaponBuffSnapshot snapshot = _weaponBuffSnapshots[i];
                if (snapshot.Weapon == null)
                {
                    continue;
                }

                snapshot.Weapon.DelayBeforeUse = snapshot.DelayBeforeUse;
                snapshot.Weapon.TimeBetweenUses = snapshot.TimeBetweenUses;

                if (snapshot.MeleeWeapon != null)
                {
                    snapshot.MeleeWeapon.DamageCaused = snapshot.DamageCaused;
                    snapshot.MeleeWeapon.InitialDelay = snapshot.InitialDelay;
                    snapshot.MeleeWeapon.ActiveDuration = snapshot.ActiveDuration;
                    UpdateMeleeDamageOnTouch(snapshot.MeleeWeapon);
                }
            }

            _weaponBuffSnapshots.Clear();
            _rageBuffsApplied = false;
        }

        protected virtual void ApplyRageVisuals()
        {
            if (_rageVisualsApplied)
            {
                return;
            }

            if (TintCharacterDuringRage && !DisableCharacterSpriteTint)
            {
                SpriteRenderer[] characterRenderers = GetCharacterSpriteRenderers();
                for (int i = 0; i < characterRenderers.Length; i++)
                {
                    SpriteRenderer renderer = characterRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    _spriteColorSnapshots.Add(new SpriteColorSnapshot { Renderer = renderer, Color = renderer.color });
                    renderer.color = RageSpriteColor;
                }
            }

            if (MatchWalkFeedbackToSpriteColor)
            {
                TintWalkFeedbackParticles(GetCurrentCharacterSpriteColor());
            }

            PlayRageModeParticles();
            HideNormalSlashSprite();

            _rageVisualsApplied = true;
        }

        protected virtual void RestoreRageVisuals()
        {
            if (!_rageVisualsApplied)
            {
                return;
            }

            for (int i = 0; i < _spriteColorSnapshots.Count; i++)
            {
                SpriteColorSnapshot snapshot = _spriteColorSnapshots[i];
                if (snapshot.Renderer != null)
                {
                    snapshot.Renderer.color = snapshot.Color;
                }
            }

            for (int i = 0; i < _walkFeedbackColorSnapshots.Count; i++)
            {
                ParticleColorSnapshot snapshot = _walkFeedbackColorSnapshots[i];
                if (snapshot.ParticleSystem != null)
                {
                    ParticleSystem.MainModule main = snapshot.ParticleSystem.main;
                    main.startColor = snapshot.StartColor;
                }
            }

            _spriteColorSnapshots.Clear();
            _walkFeedbackColorSnapshots.Clear();
            StopRageModeParticles();
            RestoreHiddenRenderers();
            _rageVisualsApplied = false;
        }

        protected virtual void PlayRageModeParticles()
        {
            if (RageModeParticles == null)
            {
                return;
            }

            _rageParticlePlaybackSnapshots.Clear();
            for (int i = 0; i < RageModeParticles.Length; i++)
            {
                ParticleSystem particleSystem = RageModeParticles[i];
                if (particleSystem == null)
                {
                    continue;
                }

                _rageParticlePlaybackSnapshots.Add(new ParticlePlaybackSnapshot { ParticleSystem = particleSystem, WasPlaying = particleSystem.isPlaying });

                if (TintRageModeParticles)
                {
                    StoreAndTintParticleSystem(particleSystem, RageParticleColor);
                }

                particleSystem.Play(true);
            }
        }

        protected virtual void StopRageModeParticles()
        {
            ParticleSystemStopBehavior stopBehavior = ClearRageModeParticlesOnStop
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting;

            for (int i = 0; i < _rageParticlePlaybackSnapshots.Count; i++)
            {
                ParticlePlaybackSnapshot snapshot = _rageParticlePlaybackSnapshots[i];
                if ((snapshot.ParticleSystem == null) || snapshot.WasPlaying)
                {
                    continue;
                }

                snapshot.ParticleSystem.Stop(true, stopBehavior);
            }

            _rageParticlePlaybackSnapshots.Clear();
        }

        protected virtual void HideNormalSlashSprite()
        {
            if (!HideNormalSlashSpriteDuringRage || string.IsNullOrEmpty(NormalSlashSpriteName) || (_character == null) || (_character.CharacterModel == null))
            {
                return;
            }

            Transform slashTransform = _character.CharacterModel.transform.Find(NormalSlashSpriteName);
            if (slashTransform == null)
            {
                return;
            }

            Renderer[] renderers = slashTransform.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                {
                    continue;
                }

                _rendererEnabledSnapshots.Add(new RendererEnabledSnapshot { Renderer = renderers[i], Enabled = renderers[i].enabled });
                renderers[i].enabled = false;
            }
        }

        protected virtual void RestoreHiddenRenderers()
        {
            for (int i = 0; i < _rendererEnabledSnapshots.Count; i++)
            {
                RendererEnabledSnapshot snapshot = _rendererEnabledSnapshots[i];
                if (snapshot.Renderer != null)
                {
                    snapshot.Renderer.enabled = snapshot.Enabled;
                }
            }

            _rendererEnabledSnapshots.Clear();
        }

        protected virtual SpriteRenderer[] GetCharacterSpriteRenderers()
        {
            if ((_character != null) && (_character.CharacterModel != null))
            {
                return _character.CharacterModel.GetComponentsInChildren<SpriteRenderer>(true);
            }

            return GetComponentsInChildren<SpriteRenderer>(true);
        }

        protected virtual Color GetCurrentCharacterSpriteColor()
        {
            SpriteRenderer[] characterRenderers = GetCharacterSpriteRenderers();
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                if (characterRenderers[i] != null)
                {
                    return characterRenderers[i].color;
                }
            }

            return RageSpriteColor;
        }

        protected virtual void TintWalkFeedbackParticles(Color color)
        {
            if ((_characterHorizontalMovement == null) || (_characterHorizontalMovement.AbilityStartFeedbacks == null))
            {
                return;
            }

            MMFeedbacks walkFeedbacks = _characterHorizontalMovement.AbilityStartFeedbacks;
            ParticleSystem[] childParticles = walkFeedbacks.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < childParticles.Length; i++)
            {
                StoreAndTintParticleSystem(childParticles[i], color);
            }

            TintParticleSystemsReferencedByFeedbacks(walkFeedbacks, color);
        }

        protected virtual void TintParticleSystemsReferencedByFeedbacks(MMFeedbacks feedbacks, Color color)
        {
            for (int i = 0; i < feedbacks.Feedbacks.Count; i++)
            {
                TintParticleSystemsReferencedByObject(feedbacks.Feedbacks[i], color);
            }

            MMF_Player mmfPlayer = feedbacks as MMF_Player;
            if ((mmfPlayer == null) || (mmfPlayer.FeedbacksList == null))
            {
                return;
            }

            for (int i = 0; i < mmfPlayer.FeedbacksList.Count; i++)
            {
                TintParticleSystemsReferencedByObject(mmfPlayer.FeedbacksList[i], color);
            }
        }

        protected virtual void TintParticleSystemsReferencedByObject(object target, Color color)
        {
            if (target == null)
            {
                return;
            }

            FieldInfo[] fields = target.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                object value = fields[i].GetValue(target);
                ParticleSystem particleSystem = value as ParticleSystem;
                if (particleSystem != null)
                {
                    StoreAndTintParticleSystem(particleSystem, color);
                    continue;
                }

                IEnumerable<ParticleSystem> particleSystems = value as IEnumerable<ParticleSystem>;
                if (particleSystems == null)
                {
                    continue;
                }

                foreach (ParticleSystem listedParticleSystem in particleSystems)
                {
                    StoreAndTintParticleSystem(listedParticleSystem, color);
                }
            }
        }

        protected virtual void StoreAndTintParticleSystem(ParticleSystem particleSystem, Color color)
        {
            if (particleSystem == null)
            {
                return;
            }

            for (int i = 0; i < _walkFeedbackColorSnapshots.Count; i++)
            {
                if (_walkFeedbackColorSnapshots[i].ParticleSystem == particleSystem)
                {
                    TintParticleSystem(particleSystem, color);
                    return;
                }
            }

            ParticleSystem.MainModule main = particleSystem.main;
            _walkFeedbackColorSnapshots.Add(new ParticleColorSnapshot { ParticleSystem = particleSystem, StartColor = main.startColor });
            TintParticleSystem(particleSystem, color);
        }

        protected virtual void TintParticleSystem(ParticleSystem particleSystem, Color color)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            Color particleColor = color;
            particleColor.a = main.startColor.color.a;
            main.startColor = particleColor;
        }

        protected virtual Weapon[] GetWeaponsToBuff()
        {
            if ((_characterHandleWeapon == null) || (_characterHandleWeapon.CurrentWeapon == null))
            {
                return new Weapon[0];
            }

            ComboWeapon comboWeapon = _characterHandleWeapon.CurrentWeapon.GetComponent<ComboWeapon>();
            if (BuffAllComboWeapons && (comboWeapon != null) && (comboWeapon.Weapons != null) && (comboWeapon.Weapons.Length > 0))
            {
                return comboWeapon.Weapons;
            }

            return new[] { _characterHandleWeapon.CurrentWeapon };
        }

        protected virtual void UpdateMeleeDamageOnTouch(MeleeWeapon meleeWeapon)
        {
            DamageOnTouch damageOnTouch = meleeWeapon.GetComponentInChildren<DamageOnTouch>(true);
            if (damageOnTouch == null)
            {
                return;
            }

            damageOnTouch.MinDamageCaused = meleeWeapon.DamageCaused;
            damageOnTouch.MaxDamageCaused = meleeWeapon.DamageCaused;
        }

        protected virtual IEnumerator PlayClipOrAnimatorState(AnimationClip clip, string stateName, float duration, bool freezeMovement)
        {
            if (freezeMovement)
            {
                FreezeHorizontalMovement();
            }

            if (clip != null)
            {
                PlayAnimationClip(clip);
            }
            else
            {
                PlayAnimatorState(stateName);
            }

            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    if (freezeMovement && (_controller != null))
                    {
                        _controller.SetHorizontalForce(0f);
                    }

                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            StopClipGraph();

            if (freezeMovement)
            {
                RestoreHorizontalMovement();
            }
        }

        protected virtual void PlayAnimationClip(AnimationClip clip)
        {
            StopClipGraph();

            if ((_animator == null) || (clip == null))
            {
                return;
            }

            _clipGraph = PlayableGraph.Create("RetroRageModeAnimatorClip");
            _clipGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_clipGraph, clip);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_clipGraph, "Animation", _animator);
            output.SetSourcePlayable(clipPlayable);

            _clipGraph.Play();
        }

        protected virtual void StopClipGraph()
        {
            if (_clipGraph.IsValid())
            {
                _clipGraph.Destroy();
            }
        }

        protected virtual void FreezeHorizontalMovement()
        {
            if ((_characterHorizontalMovement == null) || _horizontalMovementFrozen)
            {
                return;
            }

            RetroMovementLockRegistry.Acquire(_characterHorizontalMovement);
            _horizontalMovementFrozen = true;

            _characterHorizontalMovement.ReadInput = false;
            _characterHorizontalMovement.AbilityPermitted = false;
            _characterHorizontalMovement.SetHorizontalMove(0f);

            if (_controller != null)
            {
                _controller.SetHorizontalForce(0f);
            }
        }

        protected virtual void RestoreHorizontalMovement()
        {
            if ((_characterHorizontalMovement == null) || !_horizontalMovementFrozen)
            {
                return;
            }

            RetroMovementLockRegistry.Release(_characterHorizontalMovement);
            _horizontalMovementFrozen = false;
        }

        protected virtual void RestoreNormalAnimatorController()
        {
            if ((_animator != null) && (NormalAnimatorController != null))
            {
                _animator.runtimeAnimatorController = NormalAnimatorController;
            }
        }

        protected virtual void SyncNormalAnimatorToCurrentMovement()
        {
            if (_animator == null)
            {
                return;
            }

            bool grounded = (_controller != null) && _controller.State.IsGrounded;
            Vector2 speed = (_controller != null) ? _controller.Speed : Vector2.zero;
            CharacterStates.MovementStates movementState = (_movement != null) ? _movement.CurrentState : CharacterStates.MovementStates.Idle;

            SetAnimatorBoolIfExists("Idle", movementState == CharacterStates.MovementStates.Idle);
            SetAnimatorBoolIfExists("Walking", movementState == CharacterStates.MovementStates.Walking);
            SetAnimatorBoolIfExists("Running", movementState == CharacterStates.MovementStates.Running);
            SetAnimatorBoolIfExists("Dashing", movementState == CharacterStates.MovementStates.Dashing);
            SetAnimatorBoolIfExists("Grounded", grounded);
            SetAnimatorBoolIfExists("Airborne", !grounded);
            SetAnimatorFloatIfExists("xSpeed", speed.x);
            SetAnimatorFloatIfExists("ySpeed", speed.y);
            SetAnimatorFloatIfExists("Speed", Mathf.Abs(speed.x));

            string stateName = GetCurrentMovementAnimatorStateName(movementState);
            if (!string.IsNullOrEmpty(stateName))
            {
                _animator.Play(stateName, 0, 0f);
            }

            _animator.Update(0f);
        }

        protected virtual string GetCurrentMovementAnimatorStateName(CharacterStates.MovementStates movementState)
        {
            switch (movementState)
            {
                case CharacterStates.MovementStates.Running:
                    return AnimatorHasState("Runb") ? "Runb" : (AnimatorHasState("Run") ? "Run" : null);
                case CharacterStates.MovementStates.Walking:
                    return AnimatorHasState("Runb") ? "Runb" : (AnimatorHasState("Walk") ? "Walk" : null);
                case CharacterStates.MovementStates.Dashing:
                    return AnimatorHasState("Dash") ? "Dash" : null;
                case CharacterStates.MovementStates.Jumping:
                case CharacterStates.MovementStates.DoubleJumping:
                    return AnimatorHasState("Jumpb") ? "Jumpb" : (AnimatorHasState("Jump") ? "Jump" : null);
                case CharacterStates.MovementStates.Falling:
                    return AnimatorHasState("Fallb") ? "Fallb" : (AnimatorHasState("Fall") ? "Fall" : null);
            }

            return null;
        }

        protected virtual bool AnimatorHasState(string stateName)
        {
            return (_animator != null) && _animator.HasState(0, Animator.StringToHash(stateName));
        }

        protected virtual void SetAnimatorBoolIfExists(string parameterName, bool value)
        {
            if (AnimatorHasParameter(parameterName, AnimatorControllerParameterType.Bool))
            {
                _animator.SetBool(parameterName, value);
            }
        }

        protected virtual void SetAnimatorFloatIfExists(string parameterName, float value)
        {
            if (AnimatorHasParameter(parameterName, AnimatorControllerParameterType.Float))
            {
                _animator.SetFloat(parameterName, value);
            }
        }

        protected virtual bool AnimatorHasParameter(string parameterName, AnimatorControllerParameterType parameterType)
        {
            if ((_animator == null) || string.IsNullOrEmpty(parameterName))
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if ((parameters[i].type == parameterType) && (parameters[i].name == parameterName))
                {
                    return true;
                }
            }

            return false;
        }

        protected virtual float GetClipDuration(AnimationClip clip, float fallbackDuration)
        {
            if (clip != null)
            {
                return Mathf.Max(0f, clip.length);
            }

            return Mathf.Max(0f, fallbackDuration);
        }

        protected virtual void DropBoltOnCurrentEnemy()
        {
            if (!DropBoltAfterAbilityAppear)
            {
                return;
            }

            Health target = FindBoltTarget();
            if (target == null)
            {
                return;
            }

            Vector3 targetCenter = GetTargetCenter(target);
            Vector3 boltPosition = new Vector3(targetCenter.x, targetCenter.y + BoltTargetCenterYOffset + BoltDropYOffset, targetCenter.z);
            GameObject boltRoot = new GameObject("AbilityBolt");
            boltRoot.transform.position = boltPosition;

            GameObject boltVisual = (BoltPrefab != null) ? Instantiate(BoltPrefab, boltRoot.transform) : new GameObject("AbilityBoltVisual");
            boltVisual.transform.SetParent(boltRoot.transform);
            boltVisual.transform.localPosition = Vector3.zero;
            boltVisual.transform.localRotation = Quaternion.identity;
            boltVisual.SetActive(true);

            AnimationClip boltClip = GetBoltClip();
            if (boltClip != null)
            {
                PlayBoltClip(boltRoot, boltVisual, boltClip, targetCenter);
            }
            else
            {
                ScaleBoltVisual(boltRoot, boltVisual, targetCenter);
            }

            if (BoltDealsDamage)
            {
                CreateBoltDamageHitbox(targetCenter);
            }

            float lifetime = (boltClip != null) ? boltClip.length : BoltFallbackLifetime;
            Destroy(boltRoot, Mathf.Max(0.01f, lifetime));
        }

        protected virtual Health FindBoltTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, BoltTargetSearchRadius, BoltCanTargetAnyAICharacter ? Physics2D.AllLayers : BoltTargetLayerMask.value);
            Health closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hits.Length; i++)
            {
                Health health = hits[i].GetComponentInParent<Health>();
                if ((health == null) || IsSelfHealth(health) || !CanBoltTarget(hits[i], health))
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude((Vector2)GetTargetCenter(health) - (Vector2)transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = health;
                }
            }

            return closest;
        }

        protected virtual bool IsSelfHealth(Health health)
        {
            return (_character != null) && ((health.gameObject == _character.gameObject) || health.transform.IsChildOf(_character.transform));
        }

        protected virtual bool CanBoltTarget(Collider2D targetCollider, Health targetHealth)
        {
            bool layerMatches = ((BoltTargetLayerMask.value & (1 << targetCollider.gameObject.layer)) != 0)
                                || ((BoltTargetLayerMask.value & (1 << targetHealth.gameObject.layer)) != 0);
            if (layerMatches)
            {
                return true;
            }

            if (!BoltCanTargetAnyAICharacter)
            {
                return false;
            }

            Character character = targetHealth.GetComponent<Character>();
            if (character == null)
            {
                character = targetHealth.GetComponentInParent<Character>();
            }

            return (character != null) && (character.CharacterType == Character.CharacterTypes.AI);
        }

        protected virtual Vector3 GetTargetCenter(Health target)
        {
            Collider2D targetCollider = target.GetComponentInChildren<Collider2D>();
            if (targetCollider != null)
            {
                return targetCollider.bounds.center;
            }

            return target.transform.position;
        }

        protected virtual void PlayBoltClip(GameObject boltRoot, GameObject boltVisual, AnimationClip boltClip, Vector3 targetCenter)
        {
            StopBoltGraph();

            Animator boltAnimator = boltVisual.GetComponentInChildren<Animator>();
            if (boltAnimator == null)
            {
                boltAnimator = boltVisual.AddComponent<Animator>();
            }

            SpriteRenderer boltRenderer = boltVisual.GetComponentInChildren<SpriteRenderer>();
            if (boltRenderer == null)
            {
                boltRenderer = boltVisual.AddComponent<SpriteRenderer>();
            }

            CopyCharacterSorting(boltRenderer);

            _boltGraph = PlayableGraph.Create("RetroRageModeAnimatorBolt");
            _boltGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_boltGraph, boltClip);
            clipPlayable.SetDuration(boltClip.length);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_boltGraph, "BoltAnimation", boltAnimator);
            output.SetSourcePlayable(clipPlayable);

            _boltGraph.Play();
            StartCoroutine(ScaleBoltVisualNextFrame(boltRoot, boltVisual, targetCenter));
            StartCoroutine(StopBoltGraphAfter(boltClip.length));
        }

        protected virtual IEnumerator ScaleBoltVisualNextFrame(GameObject boltRoot, GameObject boltVisual, Vector3 targetCenter)
        {
            yield return null;
            ScaleBoltVisual(boltRoot, boltVisual, targetCenter);
        }

        protected virtual void ScaleBoltVisual(GameObject boltRoot, GameObject boltVisual, Vector3 targetCenter)
        {
            if ((boltRoot == null) || (boltVisual == null))
            {
                return;
            }

            if (!ScaleBoltToCharacterHeight)
            {
                ApplyBoltManualScale(boltRoot, Vector3.one * Mathf.Max(0.01f, BoltFallbackScale));
                ClampBoltRootToGround(boltRoot, boltVisual, targetCenter);
                return;
            }

            float characterHeight = GetCharacterVisualHeight();
            float boltHeight = GetObjectVisualHeight(boltVisual);
            if ((characterHeight <= 0f) || (boltHeight <= 0f))
            {
                ApplyBoltManualScale(boltRoot, Vector3.one * Mathf.Max(0.01f, BoltFallbackScale));
                ClampBoltRootToGround(boltRoot, boltVisual, targetCenter);
                return;
            }

            float targetHeight = characterHeight * Mathf.Max(0.01f, BoltCharacterHeightMultiplier);
            float scaleMultiplier = targetHeight / boltHeight;
            ApplyBoltManualScale(boltRoot, boltRoot.transform.localScale * scaleMultiplier);
            ClampBoltRootToGround(boltRoot, boltVisual, targetCenter);
        }

        protected virtual void ApplyBoltManualScale(GameObject bolt, Vector3 baseScale)
        {
            if (bolt == null)
            {
                return;
            }

            bolt.transform.localScale = new Vector3(
                baseScale.x * Mathf.Max(0.01f, BoltManualScaleMultiplier.x),
                baseScale.y * Mathf.Max(0.01f, BoltManualScaleMultiplier.y),
                baseScale.z);
        }

        protected virtual void ClampBoltRootToGround(GameObject boltRoot, GameObject boltVisual, Vector3 targetCenter)
        {
            if (!ClampBoltBottomToGround || (boltRoot == null) || (boltVisual == null))
            {
                return;
            }

            Bounds visualBounds;
            if (!TryGetObjectVisualBounds(boltVisual, out visualBounds))
            {
                return;
            }

            Vector3 raycastOrigin = targetCenter + Vector3.up * Mathf.Max(0f, BoltGroundRaycastStartHeight);
            RaycastHit2D groundHit = Physics2D.Raycast(raycastOrigin, Vector2.down, Mathf.Max(0.01f, BoltGroundRaycastDistance), BoltGroundLayerMask);
            if (groundHit.collider == null)
            {
                return;
            }

            float targetBottomY = groundHit.point.y + BoltGroundYOffset;
            float yCorrection = targetBottomY - visualBounds.min.y;
            boltRoot.transform.position += new Vector3(0f, yCorrection, 0f);
        }

        protected virtual Vector2 GetBoltGizmoVisualSize()
        {
            float characterHeight = ScaleBoltToCharacterHeight ? GetCharacterVisualHeight() : 0f;
            float baseHeight = characterHeight > 0f ? characterHeight * Mathf.Max(0.01f, BoltCharacterHeightMultiplier) : Mathf.Max(0.01f, BoltFallbackScale);
            return new Vector2(
                baseHeight * Mathf.Max(0.01f, BoltManualScaleMultiplier.x),
                baseHeight * Mathf.Max(0.01f, BoltManualScaleMultiplier.y));
        }

        protected virtual float GetCharacterVisualHeight()
        {
            GameObject characterVisual = ((_character != null) && (_character.CharacterModel != null)) ? _character.CharacterModel.gameObject : gameObject;
            return GetObjectVisualHeight(characterVisual);
        }

        protected virtual float GetObjectVisualHeight(GameObject source)
        {
            if (source == null)
            {
                return 0f;
            }

            SpriteRenderer[] renderers = source.GetComponentsInChildren<SpriteRenderer>();
            if ((renderers == null) || (renderers.Length == 0))
            {
                return 0f;
            }

            Bounds bounds = renderers[0].bounds;
            bool foundRenderer = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if ((renderers[i] == null) || !renderers[i].enabled || (renderers[i].sprite == null))
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds = renderers[i].bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return foundRenderer ? bounds.size.y : 0f;
        }

        protected virtual bool TryGetObjectVisualBounds(GameObject source, out Bounds bounds)
        {
            bounds = new Bounds();
            if (source == null)
            {
                return false;
            }

            SpriteRenderer[] renderers = source.GetComponentsInChildren<SpriteRenderer>();
            if ((renderers == null) || (renderers.Length == 0))
            {
                return false;
            }

            bool foundRenderer = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if ((renderers[i] == null) || !renderers[i].enabled || (renderers[i].sprite == null))
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds = renderers[i].bounds;
                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return foundRenderer;
        }

        protected virtual IEnumerator StopBoltGraphAfter(float delay)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, delay));
            StopBoltGraph();
        }

        protected virtual void StopBoltGraph()
        {
            if (_boltGraph.IsValid())
            {
                _boltGraph.Destroy();
            }
        }

        protected virtual void CreateBoltDamageHitbox(Vector3 targetCenter)
        {
            GameObject damageArea = new GameObject("AbilityBoltDamage");
            damageArea.transform.position = targetCenter;

            RetroSkillDamageHitbox damageHitbox = damageArea.AddComponent<RetroSkillDamageHitbox>();
            damageHitbox.Initialize(
                (_character != null) ? _character.gameObject : gameObject,
                BoltTargetLayerMask,
                BoltDamage,
                BoltDamageInvincibilityDuration,
                BoltDamageAreaSize,
                Vector2.zero,
                BoltDamageActiveDuration,
                BoltCanTargetAnyAICharacter,
                false,
                1,
                0f);

            Destroy(damageArea, Mathf.Max(0.01f, BoltDamageActiveDuration));
        }

        protected virtual AnimationClip GetBoltClip()
        {
#if UNITY_EDITOR
            if (BoltClip == null)
            {
                BoltClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animation5/bolt.anim");
            }
#endif
            return BoltClip;
        }

        protected virtual void CopyCharacterSorting(SpriteRenderer boltRenderer)
        {
            if (boltRenderer == null)
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

            boltRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            boltRenderer.sortingOrder = Mathf.Max(sourceRenderer.sortingOrder + 3, 100);
            boltRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            DrawBoltGizmos();
        }

        protected virtual void OnDrawGizmos()
        {
            DrawBoltGizmos();
        }

        protected virtual void DrawBoltGizmos()
        {
            if (!ShowBoltGizmos)
            {
                return;
            }

            Color previousColor = Gizmos.color;

            Gizmos.color = BoltSearchGizmoColor;
            Gizmos.DrawWireSphere(transform.position, BoltTargetSearchRadius);

            Vector3 previewTargetCenter = transform.position + new Vector3(0f, BoltTargetCenterYOffset, 0f);
            Vector3 previewBoltCenter = previewTargetCenter + new Vector3(0f, BoltDropYOffset, 0f);
            Vector2 visualSize = GetBoltGizmoVisualSize();

            Gizmos.color = BoltVisualGizmoColor;
            Gizmos.DrawWireCube(previewBoltCenter, new Vector3(visualSize.x, visualSize.y, 0.01f));
            Gizmos.DrawLine(previewTargetCenter, previewBoltCenter);

            if (ClampBoltBottomToGround)
            {
                Vector3 raycastStart = previewTargetCenter + Vector3.up * Mathf.Max(0f, BoltGroundRaycastStartHeight);
                Vector3 raycastEnd = raycastStart + Vector3.down * Mathf.Max(0.01f, BoltGroundRaycastDistance);
                Gizmos.color = BoltGroundGizmoColor;
                Gizmos.DrawLine(raycastStart, raycastEnd);
                Gizmos.DrawWireCube(previewBoltCenter + Vector3.down * (visualSize.y * 0.5f), new Vector3(visualSize.x, 0.05f, 0.01f));
            }

            Gizmos.color = BoltDamageGizmoColor;
            Gizmos.DrawWireCube(previewTargetCenter, new Vector3(BoltDamageAreaSize.x, BoltDamageAreaSize.y, 0.01f));

            Gizmos.color = previousColor;
        }

        protected virtual bool PlayAnimatorState(string stateName)
        {
            if ((_animator == null) || string.IsNullOrEmpty(stateName))
            {
                return false;
            }

            for (int i = 0; i < _animator.layerCount; i++)
            {
                string layerName = _animator.GetLayerName(i);
                string fullStateName = stateName.Contains(".") ? stateName : layerName + "." + stateName;
                int fullPathHash = Animator.StringToHash(fullStateName);

                if (_animator.HasState(i, fullPathHash))
                {
                    _animator.Play(fullPathHash, i, 0f);
                    return true;
                }

                int stateHash = Animator.StringToHash(stateName);
                if (_animator.HasState(i, stateHash))
                {
                    _animator.Play(stateHash, i, 0f);
                    return true;
                }
            }

            Debug.LogWarning("RetroRageModeAnimator couldn't find animator state '" + stateName + "' on " + _animator.name + ".", this);
            return false;
        }

        public override void ResetAbility()
        {
            base.ResetAbility();

            if (_rageCoroutine != null)
            {
                StopCoroutine(_rageCoroutine);
                _rageCoroutine = null;
            }

            StopClipGraph();
            StopBoltGraph();
            RestoreHorizontalMovement();
            RestoreRageBuffs();
            RestoreRageVisuals();
            RestoreNormalAnimatorController();
            RageModeActive = false;
            ResetRageMeter();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            this.MMEventStartListening<MMDamageTakenEvent>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            this.MMEventStopListening<MMDamageTakenEvent>();

            if (_rageCoroutine != null)
            {
                StopCoroutine(_rageCoroutine);
                _rageCoroutine = null;
            }

            StopClipGraph();
            StopBoltGraph();
            RestoreHorizontalMovement();
            RestoreRageBuffs();
            RestoreRageVisuals();
            RestoreNormalAnimatorController();
            UnregisterEnemyHealths();
            RageModeActive = false;
        }
    }
}
