using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Add this class on a character and it'll be able to dash just like the regular dash, and apply damage to everything its DamageOnTouch zone touches
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Damage Dash")]
	public class CharacterDamageDash : CharacterDash
	{
		[Header("Damage Dash")]
		/// the DamageOnTouch object to activate when dashing (usually placed under the Character's model, will require a Collider2D of some form, set to trigger
		[Tooltip("the DamageOnTouch object to activate when dashing (usually placed under the Character's model, will require a Collider2D of some form, set to trigger")]
		public DamageOnTouch TargetDamageOnTouch;

		[Header("Airborne Kick")]
		/// if true, the damage zone will only be active while dashing in the air
		[Tooltip("if true, the damage zone will only be active while dashing in the air")]
		public bool DamageOnlyWhenAirborne = true;
		/// if true, hitting a damageable target while airborne refreshes one dash use
		[Tooltip("if true, hitting a damageable target while airborne refreshes one dash use")]
		public bool RefreshDashOnAirborneHit = true;
		/// if true, hitting a damageable target while airborne moves the character forward to help land on the other side
		[Tooltip("if true, hitting a damageable target while airborne moves the character forward to help land on the other side")]
		public bool JumpPastTargetOnAirborneHit = true;
		/// how far forward the character is moved after an airborne dash hit
		[Tooltip("how far forward the character is moved after an airborne dash hit")]
		public float AirborneHitHorizontalMove = 1.25f;
		/// how much vertical lift is added to the side switch
		[Tooltip("how much vertical lift is added to the side switch")]
		public float AirborneHitVerticalMove = 0.2f;
		/// the force applied after an airborne dash hit
		[Tooltip("the force applied after an airborne dash hit")]
		public Vector2 AirborneHitForce = new Vector2(8f, 6f);
		/// cooldown applied after an airborne hit refresh, usually 0 so the next kick can happen immediately
		[Tooltip("cooldown applied after an airborne hit refresh, usually 0 so the next kick can happen immediately")]
		public float AirborneHitDashCooldown = 0f;
		/// the layers used to prevent the side switch from moving the character into walls
		[Tooltip("the layers used to prevent the side switch from moving the character into walls")]
		public LayerMask AirborneHitObstacleMask = LayerManager.ObstaclesLayerMask;

		protected float _lastAirborneDamageDashHitAt = -100f;
		protected const float _airborneDamageDashHitLockout = 0.05f;
        
		/// <summary>
		/// On initialization, we disable our damage on touch object
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			if (TargetDamageOnTouch != null)
			{
				if (_character != null)
				{
					TargetDamageOnTouch.Owner = _character.gameObject;
				}
				TargetDamageOnTouch.OnHitDamageable += OnDamageDashHitDamageable;
			}
			TargetDamageOnTouch?.gameObject.SetActive(false);
		}

		/// <summary>
		/// When we start to dash, we activate our damage object
		/// </summary>
		public override void InitiateDash()
		{
			base.InitiateDash();
			UpdateDamageOnTouchState();
		}

		/// <summary>
		/// Keeps the dash hitbox synced with grounded/airborne state.
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();
			UpdateDamageOnTouchState();
		}

		/// <summary>
		/// When we stop dashing, we disable our damage object
		/// </summary>
		public override void StopDash()
		{
			base.StopDash();
			TargetDamageOnTouch?.gameObject.SetActive(false);
		}

		protected virtual void UpdateDamageOnTouchState()
		{
			if (TargetDamageOnTouch == null)
			{
				return;
			}

			TargetDamageOnTouch.gameObject.SetActive(ShouldEnableDamageOnTouch());
		}

		protected virtual bool ShouldEnableDamageOnTouch()
		{
			if ((_movement == null) || (_movement.CurrentState != CharacterStates.MovementStates.Dashing))
			{
				return false;
			}

			return !DamageOnlyWhenAirborne || IsAirborneForDamageDash();
		}

		protected virtual bool IsAirborneForDamageDash()
		{
			return (_controller != null) && !_controller.State.IsGrounded && !_controller.State.IsCollidingBelow;
		}

		protected virtual void OnDamageDashHitDamageable()
		{
			if (!ShouldEnableDamageOnTouch() || (Time.time - _lastAirborneDamageDashHitAt < _airborneDamageDashHitLockout))
			{
				return;
			}

			_lastAirborneDamageDashHitAt = Time.time;

			if (JumpPastTargetOnAirborneHit)
			{
				MovePastAirborneHitTarget();
			}

			if (RefreshDashOnAirborneHit)
			{
				SetSuccessiveDashesLeft(Mathf.Max(SuccessiveDashesLeft, 1));
				_cooldownTimeStamp = Time.time + Mathf.Max(0f, AirborneHitDashCooldown);
			}
		}

		protected virtual void MovePastAirborneHitTarget()
		{
			if ((_characterTransform == null) || (_controller == null))
			{
				return;
			}

			float direction = (_currentDirection == 0f) ? (_character.IsFacingRight ? 1f : -1f) : Mathf.Sign(_currentDirection);
			float horizontalMove = Mathf.Max(0f, AirborneHitHorizontalMove);
			Vector2 moveDirection = Vector2.right * direction;
			RaycastHit2D hit = Physics2D.BoxCast(_controller.BoundsCenter, _controller.Bounds, 0f, moveDirection, horizontalMove, AirborneHitObstacleMask);
			if (hit.collider != null)
			{
				horizontalMove = Mathf.Max(0f, hit.distance - 0.05f);
			}

			_characterTransform.position += new Vector3(horizontalMove * direction, AirborneHitVerticalMove, 0f);
			_controller.SetForce(new Vector2(AirborneHitForce.x * direction, AirborneHitForce.y));
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (TargetDamageOnTouch != null)
			{
				TargetDamageOnTouch.OnHitDamageable -= OnDamageDashHitDamageable;
			}
		}
	}
}
