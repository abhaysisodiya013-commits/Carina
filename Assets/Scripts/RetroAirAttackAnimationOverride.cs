using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Plays a direct airborne attack clip when Corgi's weapon attacks off the ground.
    /// </summary>
    [AddComponentMenu("Corgi Engine/Character/Abilities/Retro Air Attack Animation Override")]
    public class RetroAirAttackAnimationOverride : CharacterAbility
    {
        [Header("Clips")]
        [Tooltip("Air attack clip used outside rage mode.")]
        public AnimationClip NormalAirAttackClip;
        [Tooltip("Air attack clip used while rage mode is active.")]
        public AnimationClip RageAirAttackClip;
        [Tooltip("Stops horizontal ground movement while the current weapon is attacking.")]
        public bool StopGroundMovementWhileAttacking = true;
        [Tooltip("Turns off normal weapon attack animator parameters while an airborne attack clip is playing.")]
        public bool SuppressNormalAttackAnimationInAir = true;
        [Tooltip("Creates a short airborne melee damage window using the current weapon's existing damage settings.")]
        public bool EnsureAirAttackDamage = true;

        protected CharacterHandleWeapon _characterHandleWeapon;
        protected RetroRageModeAnimator _rageModeAnimator;
        protected Weapon.WeaponStates _lastWeaponState = Weapon.WeaponStates.WeaponIdle;
        protected PlayableGraph _airAttackGraph;
        protected GameObject _airAttackDamageArea;
        protected float _airAttackEndsAt;
        protected bool _groundAttackMovementLocked;
        protected bool _storedMovementForbidden;

        protected override void Initialization()
        {
            base.Initialization();

            _characterHandleWeapon = _character?.FindAbility<CharacterHandleWeapon>();
            _rageModeAnimator = _character?.FindAbility<RetroRageModeAnimator>();

            if ((_animator == null) && (_character != null) && (_character.CharacterModel != null))
            {
                _animator = _character.CharacterModel.GetComponentInChildren<Animator>();
            }
        }

        public override void ProcessAbility()
        {
            base.ProcessAbility();

            if ((_characterHandleWeapon == null) || (_characterHandleWeapon.CurrentWeapon == null))
            {
                StopAirAttackClip();
                RestoreGroundAttackMovement();
                _lastWeaponState = Weapon.WeaponStates.WeaponIdle;
                return;
            }

            Weapon weapon = _characterHandleWeapon.CurrentWeapon;
            Weapon.WeaponStates currentState = weapon.WeaponState.CurrentState;
            bool isAttackState = IsAttackState(currentState);

            if (ShouldPlayAirAttack(currentState, isAttackState))
            {
                PlayAirAttackClip(weapon);
            }

            HandleGroundAttackMovementLock(isAttackState);
            HandleAirAttackAnimationSuppression(weapon);

            if (_airAttackGraph.IsValid() && (Time.time >= _airAttackEndsAt))
            {
                StopAirAttackClip();
            }

            _lastWeaponState = currentState;
        }

        protected virtual void LateUpdate()
        {
            if ((_characterHandleWeapon == null) || (_characterHandleWeapon.CurrentWeapon == null))
            {
                return;
            }

            Weapon weapon = _characterHandleWeapon.CurrentWeapon;
            Weapon.WeaponStates currentState = weapon.WeaponState.CurrentState;
            bool isAttackState = IsAttackState(currentState);

            if (ShouldPlayAirAttack(currentState, isAttackState))
            {
                PlayAirAttackClip(weapon);
            }

            HandleGroundAttackMovementLock(isAttackState);
            HandleAirAttackAnimationSuppression(weapon);
            _lastWeaponState = currentState;
        }

        protected virtual bool ShouldPlayAirAttack(Weapon.WeaponStates currentState, bool isAttackState)
        {
            return isAttackState
                   && !IsAttackState(_lastWeaponState)
                   && _controller != null
                   && !_controller.State.IsGrounded;
        }

        protected virtual bool IsAttackState(Weapon.WeaponStates state)
        {
            return state == Weapon.WeaponStates.WeaponStart
                   || state == Weapon.WeaponStates.WeaponDelayBeforeUse
                   || state == Weapon.WeaponStates.WeaponUse
                   || state == Weapon.WeaponStates.WeaponDelayBetweenUses;
        }

        protected virtual void HandleGroundAttackMovementLock(bool isAttackState)
        {
            if (!StopGroundMovementWhileAttacking || (_characterHorizontalMovement == null) || (_controller == null))
            {
                return;
            }

            if (isAttackState && _controller.State.IsGrounded)
            {
                FreezeGroundAttackMovement();
                return;
            }

            RestoreGroundAttackMovement();
        }

        protected virtual void FreezeGroundAttackMovement()
        {
            if (!_groundAttackMovementLocked)
            {
                RetroMovementLockRegistry.Acquire(_characterHorizontalMovement);
                _groundAttackMovementLocked = true;
            }

            _characterHorizontalMovement.SetHorizontalMove(0f);
            _characterHorizontalMovement.MovementForbidden = true;
            _controller.SetHorizontalForce(0f);
        }

        protected virtual void RestoreGroundAttackMovement()
        {
            if ((_characterHorizontalMovement == null) || !_groundAttackMovementLocked)
            {
                return;
            }

            RetroMovementLockRegistry.Release(_characterHorizontalMovement);
            _groundAttackMovementLocked = false;
        }

        protected virtual void PlayAirAttackClip(Weapon weapon)
        {
            AnimationClip clip = GetAirAttackClip();
            if ((_animator == null) || (clip == null))
            {
                return;
            }

            StopAirAttackClip();

            _airAttackGraph = PlayableGraph.Create("RetroAirAttackAnimationOverride");
            _airAttackGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(_airAttackGraph, clip);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_airAttackGraph, "AirAttack", _animator);
            output.SetSourcePlayable(clipPlayable);

            _airAttackEndsAt = Time.time + Mathf.Max(0.01f, clip.length);
            _airAttackGraph.Play();

            EnsureAirAttackDamageArea(weapon, clip.length);
        }

        protected virtual void EnsureAirAttackDamageArea(Weapon weapon, float clipLength)
        {
            if (!EnsureAirAttackDamage || (weapon == null))
            {
                return;
            }

            MeleeWeapon meleeWeapon = weapon as MeleeWeapon;
            if (meleeWeapon == null)
            {
                meleeWeapon = weapon.GetComponent<MeleeWeapon>();
            }
            if (meleeWeapon == null)
            {
                return;
            }

            DestroyAirAttackDamageArea();

            _airAttackDamageArea = new GameObject(weapon.name + "AirAttackDamageArea");
            _airAttackDamageArea.transform.SetParent(weapon.transform);
            _airAttackDamageArea.transform.localPosition = Vector3.zero;
            _airAttackDamageArea.transform.localRotation = Quaternion.identity;
            _airAttackDamageArea.transform.localScale = Vector3.one;

            Collider2D damageCollider = null;
            if (meleeWeapon.DamageAreaShape == MeleeWeapon.MeleeDamageAreaShapes.Circle)
            {
                CircleCollider2D circleCollider = _airAttackDamageArea.AddComponent<CircleCollider2D>();
                circleCollider.offset = meleeWeapon.AreaOffset;
                circleCollider.radius = meleeWeapon.AreaSize.x / 2f;
                damageCollider = circleCollider;
            }
            else
            {
                BoxCollider2D boxCollider = _airAttackDamageArea.AddComponent<BoxCollider2D>();
                boxCollider.offset = meleeWeapon.AreaOffset;
                boxCollider.size = meleeWeapon.AreaSize;
                damageCollider = boxCollider;
            }

            damageCollider.isTrigger = true;

            Rigidbody2D rigidBody = _airAttackDamageArea.AddComponent<Rigidbody2D>();
            rigidBody.bodyType = RigidbodyType2D.Kinematic;

            DamageOnTouch damageOnTouch = _airAttackDamageArea.AddComponent<DamageOnTouch>();
            damageOnTouch.Owner = (_character != null) ? _character.gameObject : weapon.gameObject;
            damageOnTouch.TargetLayerMask = meleeWeapon.TargetLayerMask;
            damageOnTouch.MinDamageCaused = meleeWeapon.DamageCaused;
            damageOnTouch.MaxDamageCaused = meleeWeapon.DamageCaused;
            damageOnTouch.InvincibilityDuration = meleeWeapon.InvincibilityDuration;
            damageOnTouch.DamageCausedKnockbackType = meleeWeapon.Knockback;
            damageOnTouch.DamageCausedKnockbackForce = meleeWeapon.KnockbackForce;

            Destroy(_airAttackDamageArea, Mathf.Max(0.05f, clipLength));
        }

        protected virtual void HandleAirAttackAnimationSuppression(Weapon weapon)
        {
            if (!SuppressNormalAttackAnimationInAir || !_airAttackGraph.IsValid() || (weapon == null) || (_animator == null))
            {
                return;
            }

            SetAnimatorParameterOff(weapon.StartAnimationParameter);
            SetAnimatorParameterOff(weapon.DelayBeforeUseAnimationParameter);
            SetAnimatorParameterOff(weapon.SingleUseAnimationParameter);
            SetAnimatorParameterOff(weapon.UseAnimationParameter);
            SetAnimatorParameterOff(weapon.DelayBetweenUsesAnimationParameter);
            SetAnimatorParameterOff(weapon.StopAnimationParameter);
            SetAnimatorParameterOff("ComboInProgress");
        }

        protected virtual void SetAnimatorParameterOff(string parameterName)
        {
            if (string.IsNullOrEmpty(parameterName) || (_animator == null))
            {
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name != parameterName)
                {
                    continue;
                }

                if (parameters[i].type == AnimatorControllerParameterType.Bool)
                {
                    _animator.SetBool(parameterName, false);
                }
                else if (parameters[i].type == AnimatorControllerParameterType.Trigger)
                {
                    _animator.ResetTrigger(parameterName);
                }
                return;
            }
        }

        protected virtual AnimationClip GetAirAttackClip()
        {
            if ((_rageModeAnimator != null) && _rageModeAnimator.RageModeActive && (RageAirAttackClip != null))
            {
                return RageAirAttackClip;
            }

            return NormalAirAttackClip;
        }

        protected virtual void StopAirAttackClip()
        {
            if (_airAttackGraph.IsValid())
            {
                _airAttackGraph.Destroy();
            }

            DestroyAirAttackDamageArea();
            SyncAnimatorToCurrentMovement();
        }

        protected virtual void SyncAnimatorToCurrentMovement()
        {
            if ((_animator == null) || (_character == null))
            {
                return;
            }

            bool grounded = (_controller != null) && _controller.State.IsGrounded;
            CharacterStates.MovementStates movementState = (_movement != null)
                ? _movement.CurrentState
                : CharacterStates.MovementStates.Idle;

            SetAnimatorParameter("Jumping", movementState == CharacterStates.MovementStates.Jumping);
            SetAnimatorParameter("DoubleJumping", movementState == CharacterStates.MovementStates.DoubleJumping);
            SetAnimatorParameter("Grounded", grounded);
            SetAnimatorParameter("Airborne", !grounded);

            _animator.Update(0f);
        }

        protected virtual void SetAnimatorParameter(string parameterName, bool value)
        {
            if (string.IsNullOrEmpty(parameterName) || (_animator == null))
            {
                return;
            }

            AnimatorControllerParameter[] parameters = _animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if ((parameters[i].type == AnimatorControllerParameterType.Bool) && (parameters[i].name == parameterName))
                {
                    _animator.SetBool(parameterName, value);
                    return;
                }
            }
        }

        protected virtual void DestroyAirAttackDamageArea()
        {
            if (_airAttackDamageArea != null)
            {
                Destroy(_airAttackDamageArea);
                _airAttackDamageArea = null;
            }
        }

        public override void ResetAbility()
        {
            base.ResetAbility();
            StopAirAttackClip();
            RestoreGroundAttackMovement();
            _lastWeaponState = Weapon.WeaponStates.WeaponIdle;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            StopAirAttackClip();
            RestoreGroundAttackMovement();
        }
    }
}
