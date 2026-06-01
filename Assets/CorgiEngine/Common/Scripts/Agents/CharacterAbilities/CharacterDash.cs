using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MoreMountains.CorgiEngine
{	
	/// <summary>
	/// Add this class to a character and it'll be able to perform a horizontal dash
	/// Animator parameters : Dashing
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Dash")] 
	public class CharacterDash : CharacterAbility
	{		
		/// This method is only used to display a helpbox text at the beginning of the ability's inspector
		public override string HelpBoxText() { return "This component allows your character to dash. Here you can define the distance the dash should cover, " +
		                                              "how much force to apply during the dash (which impacts its duration), whether forces should be reset on dash exit (otherwise inertia will apply)." +
		                                              "Then you can define how to pick the dash's direction, whether or not the character should be automatically flipped to match the dash's direction, and " +
		                                              "whether or not you want to correct the trajectory to prevent grounded characters to not dash if the input was slightly wrong." +
		                                              "And finally you can tweak the cooldown between the end of a dash and the start of the next one."; }

		[Header("Dash")]

		/// the distance this dash should cover
		[Tooltip("the distance this dash should cover")]
		public float DashDistance = 3f;
		/// the force of the dash
		[Tooltip("the force of the dash")]
		public float DashForce = 40f;
		/// if this is true, forces will be reset on dash exit (killing inertia)
		[Tooltip("if this is true, forces will be reset on dash exit (killing inertia)")]
		public bool ResetForcesOnExit = false;
		/// if this is true, position will be forced on exit to match an exact distance
		[Tooltip("if this is true, position will be forced on exit to match an exact distance")]
		public bool ForceExactDistance = false;

		[Header("Direction")]

		/// the dash's aim properties
		[Tooltip("the dash's aim properties")]
		public MMAim Aim;
		/// the minimum amount of input required to apply a direction to the dash
		[Tooltip("the minimum amount of input required to apply a direction to the dash")]
		public float MinimumInputThreshold = 0.1f;
		/// if this is true, the character will flip when dashing and facing the dash's opposite direction
		[Tooltip("if this is true, the character will flip when dashing and facing the dash's opposite direction")]
		public bool FlipCharacterIfNeeded = true;
		/// if this is true, will prevent the character from dashing into the ground when already grounded
		[Tooltip("if this is true, will prevent the character from dashing into the ground when already grounded")]
		public bool AutoCorrectTrajectory = true;

		public enum SuccessiveDashResetMethods { Grounded, Time }

		[Header("Cooldown")]
		/// the duration of the cooldown between 2 dashes (in seconds)
		[Tooltip("the duration of the cooldown between 2 dashes (in seconds)")]
		public float DashCooldown = 1f;
		/// how long a pressed dash input should be remembered if dash is not ready yet
		[Tooltip("how long a pressed dash input should be remembered if dash is not ready yet")]
		public float DashInputBufferDuration = 0.2f;

		[Header("Uses")]
		/// whether or not dashes can be performed infinitely
		[Tooltip("whether or not dashes can be performed infinitely")]
		public bool LimitedDashes = false;
		/// the amount of successive dashes a character can perform, only if dashes are not infinite
		[Tooltip("the amount of successive dashes a character can perform, only if dashes are not infinite")]
		[MMCondition("LimitedDashes", true)]
		public int SuccessiveDashAmount = 1;
		/// the amount of dashes left (runtime value only), only if dashes are not infinite
		[Tooltip("the amount of dashes left (runtime value only), only if dashes are not infinite")]
		[MMCondition("LimitedDashes", true)]
		[MMReadOnly]
		public int SuccessiveDashesLeft = 1;
		/// the method used to reset the number of dashes left, only if dashes are not infinite
		[Tooltip("the method used to reset the number of dashes left, only if dashes are not infinite")]
		[MMCondition("LimitedDashes", true)]
		public SuccessiveDashResetMethods SuccessiveDashResetMethod = SuccessiveDashResetMethods.Grounded;
		/// when in time reset mode, the duration, in seconds, after which the amount of dashes left gets reset, only if dashes are not infinite
		[Tooltip("when in time reset mode, the duration, in seconds, after which the amount of dashes left gets reset, only if dashes are not infinite")]
		[MMEnumCondition("SuccessiveDashResetMethod", (int)SuccessiveDashResetMethods.Time)]
		public float SuccessiveDashResetDuration = 2f;

		[Header("Damage")] 
		/// if this is true, this character won't receive any damage while a dash is in progress
		[Tooltip("if this is true, this character won't receive any damage while a dash is in progress")]
		public bool InvincibleWhileDashing = false; 

		[Header("Slide Movement Swap")]
		/// if this is true, dash keeps its input/state/animation but uses the more stable roll-style movement driver
		[Tooltip("if this is true, dash keeps its input/state/animation but uses the more stable roll-style movement driver")]
		public bool UseRollStyleMovement = false;
		/// the duration of the roll-style dash, in seconds
		[Tooltip("the duration of the roll-style dash, in seconds")]
		public float RollStyleDashDuration = 0.28f;
		/// the speed multiplier used while the roll-style dash is active
		[Tooltip("the speed multiplier used while the roll-style dash is active")]
		public float RollStyleDashSpeed = 2.2f;
		/// if true, horizontal input can steer the roll-style dash while it is active
		[Tooltip("if true, horizontal input can steer the roll-style dash while it is active")]
		public bool RollStyleDashReadsInput = true;
		/// if true, roll-style dash always stays horizontal and ignores joystick up/down when starting the dash
		[Tooltip("if true, roll-style dash always stays horizontal and ignores joystick up/down when starting the dash")]
		public bool RollStyleDashIgnoresVerticalInput = true;
		/// if true, pressing up or down while pressing dash prevents the dash from starting
		[Tooltip("if true, pressing up or down while pressing dash prevents the dash from starting")]
		public bool RequireNeutralVerticalInput = false;

		[Header("Airborne Dash Kick")]
		/// if true, player dashes create a damage hitbox, but only while airborne
		[Tooltip("if true, player dashes create a damage hitbox, but only while airborne")]
		public bool EnableAirborneDashKick = true;
		/// the layers damaged by the airborne dash kick
		[Tooltip("the layers damaged by the airborne dash kick")]
		public LayerMask AirborneDashKickTargetLayerMask = LayerManager.EnemiesLayerMask;
		/// the damage caused by the airborne dash kick
		[Tooltip("the damage caused by the airborne dash kick")]
		public float AirborneDashKickDamage = 10f;
		/// the size of the airborne dash kick hitbox
		[Tooltip("the size of the airborne dash kick hitbox")]
		public Vector2 AirborneDashKickAreaSize = new Vector2(2f, 1.2f);
		/// the offset of the airborne dash kick hitbox, relative to the character and flipped with dash direction
		[Tooltip("the offset of the airborne dash kick hitbox, relative to the character and flipped with dash direction")]
		public Vector2 AirborneDashKickAreaOffset = new Vector2(1.05f, 0.05f);
		/// invincibility duration applied to the target after being hit by the airborne dash kick
		[Tooltip("invincibility duration applied to the target after being hit by the airborne dash kick")]
		public float AirborneDashKickInvincibilityDuration = 0.1f;
		/// the knockback force applied to enemies hit by the airborne dash kick
		[Tooltip("the knockback force applied to enemies hit by the airborne dash kick")]
		public Vector2 AirborneDashKickEnemyKnockback = new Vector2(6f, 2f);
		/// if true, hitting a damageable target while airborne refreshes one dash use
		[Tooltip("if true, hitting a damageable target while airborne refreshes one dash use")]
		public bool RefreshDashOnAirborneKickHit = true;
		/// if true, hitting a damageable target while airborne moves the character forward to help land on the other side
		[Tooltip("if true, hitting a damageable target while airborne moves the character forward to help land on the other side")]
		public bool JumpPastTargetOnAirborneKickHit = true;
		/// how far forward the character is moved after an airborne dash kick hit
		[Tooltip("how far forward the character is moved after an airborne dash kick hit")]
		public float AirborneKickHitHorizontalMove = 1.25f;
		/// how much vertical lift is added after an airborne dash kick hit
		[Tooltip("how much vertical lift is added after an airborne dash kick hit")]
		public float AirborneKickHitVerticalMove = 0.2f;
		/// how long the side switch takes after hitting an enemy, in seconds
		[Tooltip("how long the side switch takes after hitting an enemy, in seconds")]
		public float AirborneKickHitMoveDuration = 0.08f;
		/// the force applied after an airborne dash kick hit
		[Tooltip("the force applied after an airborne dash kick hit")]
		public Vector2 AirborneKickHitForce = new Vector2(8f, 6f);
		/// cooldown applied after an airborne hit refresh, usually 0 so the next kick can happen immediately
		[Tooltip("cooldown applied after an airborne hit refresh, usually 0 so the next kick can happen immediately")]
		public float AirborneKickHitDashCooldown = 0f;
		/// the layers used to prevent the side switch from moving the character into walls
		[Tooltip("the layers used to prevent the side switch from moving the character into walls")]
		public LayerMask AirborneKickHitObstacleMask = LayerManager.ObstaclesLayerMask;
		/// optional prefab to spawn when the airborne dash kick hits an enemy
		[Tooltip("optional prefab to spawn when the airborne dash kick hits an enemy")]
		public GameObject AirborneDashKickVfxPrefab;
		/// optional animation clip to play on a temporary SpriteRenderer when the airborne dash kick hits an enemy
		[Tooltip("optional animation clip to play on a temporary SpriteRenderer when the airborne dash kick hits an enemy")]
		public AnimationClip AirborneDashKickVfxClip;
		/// the local offset of the dash kick VFX from the character, flipped with dash direction
		[Tooltip("the local offset of the dash kick VFX from the character, flipped with dash direction")]
		public Vector2 AirborneDashKickVfxOffset = new Vector2(1.05f, 0.05f);
		/// the exact gizmo size and local scale used by the dash kick VFX
		[Tooltip("the exact gizmo size and local scale used by the dash kick VFX")]
		public Vector2 AirborneDashKickVfxSize = new Vector2(1.6f, 1f);
		/// how long the dash kick VFX stays alive after hit
		[Tooltip("how long the dash kick VFX stays alive after hit")]
		public float AirborneDashKickVfxDuration = 0.18f;
		/// sorting order offset applied to temporary dash kick VFX renderers
		[Tooltip("sorting order offset applied to temporary dash kick VFX renderers")]
		public int AirborneDashKickVfxSortingOrderOffset = 3;
		/// if true, an airborne kick dash that hits no enemy loses its remaining dash force when it ends
		[Tooltip("if true, an airborne kick dash that hits no enemy loses its remaining dash force when it ends")]
		public bool StopAirborneKickMomentumOnMiss = true;
		/// if true, draws the actual airborne dash kick damage hitbox when this character is selected
		[Tooltip("if true, draws the actual airborne dash kick damage hitbox in the Scene view")]
		public bool ShowAirborneDashKickHitboxGizmo = true;
		/// if true, only draws airborne dash kick gizmos when the character is selected
		[Tooltip("if true, only draws airborne dash kick gizmos when the character is selected")]
		public bool ShowAirborneDashKickGizmosOnlyWhenSelected = false;
		/// the color used for the airborne dash kick damage hitbox gizmo
		[Tooltip("the color used for the airborne dash kick damage hitbox gizmo")]
		public Color AirborneDashKickHitboxGizmoColor = new Color(1f, 0.05f, 0.05f, 0.9f);
		/// if true, draws the airborne dash kick VFX preview separately from the damage hitbox
		[Tooltip("if true, draws the airborne dash kick VFX preview separately from the damage hitbox")]
		public bool ShowAirborneDashKickVfxGizmo = true;
		/// the color used for the airborne dash kick VFX preview gizmo
		[Tooltip("the color used for the airborne dash kick VFX preview gizmo")]
		public Color AirborneDashKickVfxGizmoColor = new Color(0.2f, 0.8f, 1f, 0.75f);

		protected float _cooldownTimeStamp = 0;
		protected float _startTime ;
		protected Vector2 _initialPosition ;
		protected Vector2 _dashDirection;
		protected float _distanceTraveled = 0f;
		protected bool _shouldKeepDashing = true;
		protected float _slopeAngleSave = 0f;
		protected bool _dashEndedNaturally = true;
		protected IEnumerator _dashCoroutine;
		protected CharacterDive _characterDive;
		protected float _lastDashAt = 0f;
		protected float _averageDistancePerFrame;
		protected int _startFrame;
		protected float _currentDirection;
		protected float _drivenInput;
		protected float _originalMultiplier = 1f;
		protected float _dashInputBufferedUntil = 0f;
		protected DamageOnTouch _airborneDashKickDamageOnTouch;
		protected Transform _airborneDashKickHitboxTransform;
		protected float _lastAirborneDashKickHitAt = -100f;
		protected bool _airborneDashKickHitThisDash = false;
		protected bool _airborneDashKickActiveThisDash = false;
		protected Coroutine _airborneKickSideSwitchCoroutine;
		protected const float _airborneDashKickHitLockout = 0.05f;

		// animation parameters
		protected const string _dashingAnimationParameterName = "Dashing";
		protected int _dashingAnimationParameter;

		/// <summary>
		/// Initializes this ability and finds the visible character sprite when it lives on the model child.
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			if ((_spriteRenderer == null) && (_character?.CharacterModel != null))
			{
				_spriteRenderer = _character.CharacterModel.GetComponentInChildren<SpriteRenderer>();
			}
			Aim.Initialization();
			_characterDive = _character?.FindAbility<CharacterDive>();
			SuccessiveDashesLeft = SuccessiveDashAmount;
			SetupAirborneDashKickHitbox();
		}

		/// <summary>
		/// At the start of each cycle, we check if we're pressing the dash button. If we
		/// </summary>
		protected override void HandleInput()
		{
			if (_inputManager.DashButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
			{
				_dashInputBufferedUntil = Time.time + DashInputBufferDuration;
				TryStartBufferedDash();
			}
		}

		/// <summary>
		/// The second of the 3 passes you can have in your ability. Think of it as Update()
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();

			// If the character is dashing with the original force dash, we cancel the gravity.
			// Roll-style dash keeps gravity so it behaves like the working slide/roll path.
			if (!UseRollStyleMovement && (_movement.CurrentState == CharacterStates.MovementStates.Dashing)) 
			{
				_controller.GravityActive(false);
			}

			// we reset our slope tolerance if dash didn't end naturally
			if ((!_dashEndedNaturally) && (_movement.CurrentState != CharacterStates.MovementStates.Dashing))
			{
				_dashEndedNaturally = true;
				_controller.Parameters.MaximumSlopeAngle = _slopeAngleSave;
			}

			HandleAmountOfDashesLeft();
			TryStartBufferedDash();
			UpdateAirborneDashKickHitbox();
		}

		/// <summary>
		/// Starts a buffered dash as soon as the dash is allowed.
		/// </summary>
		protected virtual void TryStartBufferedDash()
		{
			if (_dashInputBufferedUntil < Time.time)
			{
				return;
			}

			if (_movement.CurrentState == CharacterStates.MovementStates.Dashing)
			{
				return;
			}

			if (!DashAuthorized() || !DashConditions())
			{
				return;
			}

			_dashInputBufferedUntil = 0f;
			InitiateDash();
		}

		/// <summary>
		/// Causes the character to dash or dive (depending on the vertical movement at the start of the dash)
		/// </summary>
		public virtual void StartDash()
		{
			if (!DashAuthorized())
			{
				return; 
			}

			if (!DashConditions())
			{
				return;
			}

			InitiateDash();
		}

		/// <summary>
		/// This method evaluates the internal conditions for a dash (cooldown between dashes, amount of dashes left) and returns true if a dash can be performed, false otherwise
		/// </summary>
		/// <returns></returns>
		public virtual bool DashConditions()
		{
			// if we're in cooldown between two dashes, we prevent dash
			if (_cooldownTimeStamp > Time.time)
			{
				return false;
			}

			// if we don't have dashes left, we prevent dash
			if (SuccessiveDashesLeft <= 0)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if conditions are met to reset the amount of dashes left
		/// </summary>
		protected virtual void HandleAmountOfDashesLeft()
		{
			if ((SuccessiveDashesLeft >= SuccessiveDashAmount) || (Time.time - _lastDashAt < DashCooldown))
			{
				return;
			}

			switch (SuccessiveDashResetMethod)
			{
				case SuccessiveDashResetMethods.Time:
					if (Time.time - _lastDashAt > SuccessiveDashResetDuration)
					{
						SetSuccessiveDashesLeft(SuccessiveDashAmount);
					}
					break;
				case SuccessiveDashResetMethods.Grounded:
					if (_controller.State.IsGrounded)
					{
						SetSuccessiveDashesLeft(SuccessiveDashAmount);
					}
					break;
			}
		}

		/// <summary>
		/// A method to reset the amount of successive dashes left
		/// </summary>
		/// <param name="newAmount"></param>
		public virtual void SetSuccessiveDashesLeft(int newAmount)
		{
			SuccessiveDashesLeft = newAmount;
		}

		/// <summary>
		/// This method evaluates the external conditions (state, other abilities) for a dash, and returns true if a dash can be performed, false otherwise
		/// </summary>
		/// <returns></returns>
		public virtual bool DashAuthorized()
		{
			// if the Dash action is enabled in the permissions, we continue, if not we do nothing
			if (!AbilityAuthorized
			    || (_condition.CurrentState != CharacterStates.CharacterConditions.Normal)
			    || (_movement.CurrentState == CharacterStates.MovementStates.LedgeHanging)
			    || (_movement.CurrentState == CharacterStates.MovementStates.Gripping))
				return false;

			bool ignoreVerticalInputForDash = UseRollStyleMovement && RollStyleDashIgnoresVerticalInput;

			// If the user presses the dash button and is not aiming down
			if (!ignoreVerticalInputForDash && (_characterDive != null))
			{
				if ((_characterDive.AbilityAuthorized) && (_characterDive.enabled) && (_inputManager != null))
				{
					if (_verticalInput < -_inputManager.Threshold.y)
					{
						return false;
					}
				}
			}

			if (!ignoreVerticalInputForDash
			    && RequireNeutralVerticalInput
			    && (_inputManager != null)
			    && (Mathf.Abs(_verticalInput) > _inputManager.Threshold.y))
			{
				return false;
			}

			return true;
		}
        
		/// <summary>
		/// initializes all parameters prior to a dash and triggers the pre dash feedbacks
		/// </summary>
		public virtual void InitiateDash()
		{
			// we set its dashing state to true
			_dashInputBufferedUntil = 0f;
			_movement.ChangeState(CharacterStates.MovementStates.Dashing);

			// we start our sounds
			PlayAbilityStartFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Dash, MMCharacterEvent.Moments.Start);

			// we initialize our various counters and checks
			_startTime = Time.time;
			_startFrame = Time.frameCount;
			_dashEndedNaturally = false;
			_initialPosition = _characterTransform.position;
			_distanceTraveled = 0;
			_shouldKeepDashing = true;
			_airborneDashKickHitThisDash = false;
			_airborneDashKickActiveThisDash = EnableAirborneDashKick
			                                  && (_character != null)
			                                  && (_character.CharacterType == Character.CharacterTypes.Player)
			                                  && (_controller != null)
			                                  && !_controller.State.IsGrounded
			                                  && !_controller.State.IsCollidingBelow;
			_cooldownTimeStamp = Time.time + DashCooldown;
			_lastDashAt = Time.time;
			if (LimitedDashes)
			{
				SuccessiveDashesLeft -= 1;
			}

			if (InvincibleWhileDashing)
			{
				_health.DamageDisabled();
			}

			_slopeAngleSave = _controller.Parameters.MaximumSlopeAngle;
			if (UseRollStyleMovement)
			{
				_originalMultiplier = _characterHorizontalMovement.AbilityMovementSpeedMultiplier;
			}
			else
			{
				// we prevent our character from going through slopes
				_controller.Parameters.MaximumSlopeAngle = 0;
				_controller.SlowFall(0f);
				_controller.State.IsCollidingLeft = false;
				_controller.State.IsCollidingRight = false;
				_controller.State.IsCollidingAbove = false;
				_controller.State.DistanceToLeftCollider = -1;
				_controller.State.DistanceToRightCollider = -1;
			}

			ComputeDashDirection();
			CheckFlipCharacter();

			// we launch the boost corountine with the right parameters
			_dashCoroutine = Dash();
			StartCoroutine(_dashCoroutine);
		}

		/// <summary>
		/// Computes the dash direction based on the selected options
		/// </summary>
		protected virtual void ComputeDashDirection()
		{
			// we compute our direction
			if (_character.LinkedInputManager != null)
			{
				Aim.PrimaryMovement = _character.LinkedInputManager.PrimaryMovement;
				Aim.SecondaryMovement = _character.LinkedInputManager.SecondaryMovement;
			}
            
			Aim.CurrentPosition = _characterTransform.position;
			_dashDirection = Aim.GetCurrentAim();

			if (UseRollStyleMovement && RollStyleDashIgnoresVerticalInput)
			{
				_dashDirection.y = 0f;
			}

			CheckAutoCorrectTrajectory();
            
			if (_dashDirection.magnitude < MinimumInputThreshold)
			{
				_dashDirection = _character.IsFacingRight ? Vector2.right : Vector2.left;
			}
			else
			{
				_dashDirection = _dashDirection.normalized;
			}

			_currentDirection = _dashDirection.x >= 0f ? 1f : -1f;
		}

		/// <summary>
		/// Prevents the character from dashing into the ground when already grounded and if AutoCorrectTrajectory is checked
		/// </summary>
		protected virtual void CheckAutoCorrectTrajectory()
		{
			if (AutoCorrectTrajectory && _controller.State.IsCollidingBelow && (_dashDirection.y < 0f))
			{
				_dashDirection.y = 0f;
			}
		}

		/// <summary>
		/// Checks whether or not a character flip is required, and flips the character if needed
		/// </summary>
		protected virtual void CheckFlipCharacter()
		{
			// we flip the character if needed
			if (FlipCharacterIfNeeded && (Mathf.Abs(_dashDirection.x) > 0.05f))
			{
				if (_character.IsFacingRight != (_dashDirection.x > 0f))
				{
					_character.Flip();
				}
			}
		}

		/// <summary>
		/// Coroutine used to move the player in a direction over time
		/// </summary>
		protected virtual IEnumerator Dash()
		{
			// if the character is not in a position where it can move freely, we do nothing.
			if ( !AbilityAuthorized
			     || (_condition.CurrentState != CharacterStates.CharacterConditions.Normal))
			{
				yield break;
			}

			if (UseRollStyleMovement)
			{
				yield return RollStyleDash();
				yield break;
			}

			// the controller collision state can still reflect the previous frame when a dash starts,
			// so we let the first dash frame apply its force before honoring stop flags
			bool isInitialDashFrame = true;
			const float staleCollisionGraceDistance = 1.5f;
			const float staleCollisionGraceDuration = 0.12f;

			// we keep dashing until we've reached our target distance or until we get interrupted
			while (_distanceTraveled < DashDistance 
			       && _shouldKeepDashing 
			       && TestForExactDistance()
			       && _movement.CurrentState == CharacterStates.MovementStates.Dashing)
			{
				_distanceTraveled = Vector3.Distance(_initialPosition,_characterTransform.position);

				// if we collide with something on our left or right (wall, slope), we stop dashing, otherwise we apply horizontal force
				if ( !isInitialDashFrame
				     && (Time.time - _startTime > staleCollisionGraceDuration)
				     && (_distanceTraveled > staleCollisionGraceDistance)
				     && ((_controller.State.IsCollidingLeft && _dashDirection.x < 0f)
				     || (_controller.State.IsCollidingRight && _dashDirection.x > 0f)
				     || (_controller.State.IsCollidingAbove && _dashDirection.y > 0f)
				     || (_controller.State.IsCollidingBelow && _dashDirection.y < 0f)))
				{
					_shouldKeepDashing = false;
					_controller.SetForce (Vector2.zero);
				}
				else
				{
					_controller.GravityActive(false);
					_controller.SetForce(_dashDirection * DashForce);
				}
				isInitialDashFrame = false;
				yield return null;
			}

			StopDash();				
		}

		/// <summary>
		/// Drives dash using the same stable movement style as CharacterRoll, while keeping dash state/input/animation.
		/// </summary>
		protected virtual IEnumerator RollStyleDash()
		{
			_characterHorizontalMovement.ReadInput = false;
			_characterHorizontalMovement.AbilityMovementSpeedMultiplier = RollStyleDashSpeed;

			float dashStartedAt = Time.time;

			while ((Time.time - dashStartedAt < RollStyleDashDuration)
			       && _shouldKeepDashing
			       && !_controller.State.TouchingLevelBounds
			       && _movement.CurrentState == CharacterStates.MovementStates.Dashing)
			{
				if (RollStyleDashReadsInput)
				{
					_drivenInput = _horizontalInput;
				}

				bool gravityShouldReverseInput = false;
				if (_characterGravity != null)
				{
					gravityShouldReverseInput = _characterGravity.ShouldReverseInput();
				}

				if (_drivenInput != 0f)
				{
					_drivenInput = gravityShouldReverseInput ? -_drivenInput : _drivenInput;
					_currentDirection = (_drivenInput < 0f) ? -1f : 1f;
				}

				float speed = _characterHorizontalMovement.MovementSpeed
				              * _controller.Parameters.SpeedFactor
				              * _characterHorizontalMovement.MovementSpeedMultiplier
				              * _characterHorizontalMovement.ContextSpeedMultiplier
				              * _characterHorizontalMovement.AbilityMovementSpeedMultiplier
				              * _characterHorizontalMovement.StateSpeedMultiplier
				              * _characterHorizontalMovement.PushSpeedMultiplier;
				_controller.SetHorizontalForce((gravityShouldReverseInput ? -_currentDirection : _currentDirection) * speed);

				yield return null;
			}

			StopDash();
		}

		/// <summary>
		/// Checks (if needed) if we've exceeded our distance, and positions the character at the exact final position
		/// </summary>
		/// <returns></returns>
		protected virtual bool TestForExactDistance()
		{
			if (!ForceExactDistance)
			{
				return true;
			}
			
			int framesSinceStart = Time.frameCount - _startFrame;
			_averageDistancePerFrame = _distanceTraveled / framesSinceStart;
			
			if (DashDistance - _distanceTraveled < _averageDistancePerFrame)
			{
				_characterTransform.position = _initialPosition + (_dashDirection * DashDistance);
				return false;
			}
			
			
			return true;
		}

		/// <summary>
		/// Stops the dash coroutine and resets all necessary parts of the character
		/// </summary>
		public virtual void StopDash()
		{
			if (_dashCoroutine != null)
			{
				StopCoroutine(_dashCoroutine);    
			}

			if (UseRollStyleMovement && (_characterHorizontalMovement != null))
			{
				_characterHorizontalMovement.ReadInput = true;
				_characterHorizontalMovement.AbilityMovementSpeedMultiplier = _originalMultiplier;
			}

			// once our dash is complete, we reset our various states
			_controller.DefaultParameters.MaximumSlopeAngle = _slopeAngleSave;
			_controller.Parameters.MaximumSlopeAngle = _slopeAngleSave;
			_controller.GravityActive(true);
			_dashEndedNaturally = true;

			// we reset our forces
			if (ResetForcesOnExit)
			{
				_controller.SetForce(Vector2.zero);
			}
			else if (StopAirborneKickMomentumOnMiss && _airborneDashKickActiveThisDash && !_airborneDashKickHitThisDash)
			{
				_controller.SetForce(Vector2.zero);
			}

			if (InvincibleWhileDashing)
			{
				_health.DamageEnabled();
			}

			SetAirborneDashKickHitboxActive(false);
            
			// we play our exit sound
			StopStartFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Dash, MMCharacterEvent.Moments.End);
			PlayAbilityStopFeedbacks();

			// once the boost is complete, if we were dashing, we make it stop and start the dash cooldown
			if (_movement.CurrentState == CharacterStates.MovementStates.Dashing)
			{
				if (_controller.State.IsGrounded)
				{
					_movement.ChangeState(CharacterStates.MovementStates.Idle);
				}
				else
				{
					_movement.RestorePreviousState();
				}                
			}
		}

		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter(_dashingAnimationParameterName, AnimatorControllerParameterType.Bool, out _dashingAnimationParameter);
		}

		/// <summary>
		/// At the end of the cycle, we update our animator's Dashing state 
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _dashingAnimationParameter, (_movement.CurrentState == CharacterStates.MovementStates.Dashing), _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			StopAirborneKickSideSwitch();
			if (_condition.CurrentState == CharacterStates.CharacterConditions.Normal)
			{
				StopDash();	
			}

			if (_animator != null)
			{
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _dashingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);	
			}
		}

		protected virtual void SetupAirborneDashKickHitbox()
		{
			if (!EnableAirborneDashKick || (_character == null) || (_character.CharacterType != Character.CharacterTypes.Player))
			{
				return;
			}

			GameObject hitbox = new GameObject("AirborneDashKickHitbox");
			hitbox.transform.SetParent(_characterTransform);
			hitbox.transform.localPosition = Vector3.zero;
			hitbox.transform.localRotation = Quaternion.identity;
			hitbox.transform.localScale = Vector3.one;

			BoxCollider2D boxCollider = hitbox.AddComponent<BoxCollider2D>();
			boxCollider.isTrigger = true;
			boxCollider.size = AirborneDashKickAreaSize;

			_airborneDashKickDamageOnTouch = hitbox.AddComponent<DamageOnTouch>();
			_airborneDashKickDamageOnTouch.Owner = _character.gameObject;
			_airborneDashKickDamageOnTouch.TargetLayerMask = AirborneDashKickTargetLayerMask;
			_airborneDashKickDamageOnTouch.MinDamageCaused = AirborneDashKickDamage;
			_airborneDashKickDamageOnTouch.MaxDamageCaused = AirborneDashKickDamage;
			_airborneDashKickDamageOnTouch.InvincibilityDuration = AirborneDashKickInvincibilityDuration;
			_airborneDashKickDamageOnTouch.DamageCausedKnockbackType = DamageOnTouch.KnockbackStyles.SetForce;
			_airborneDashKickDamageOnTouch.DamageCausedKnockbackDirection = DamageOnTouch.CausedKnockbackDirections.BasedOnOwnerPosition;
			_airborneDashKickDamageOnTouch.DamageCausedKnockbackForce = AirborneDashKickEnemyKnockback;
			_airborneDashKickDamageOnTouch.DamageTakenEveryTime = 0f;
			_airborneDashKickDamageOnTouch.DamageTakenDamageable = 0f;
			_airborneDashKickDamageOnTouch.DamageTakenNonDamageable = 0f;
			_airborneDashKickDamageOnTouch.OnHitDamageable += OnAirborneDashKickHitDamageable;
			_airborneDashKickHitboxTransform = hitbox.transform;
			hitbox.SetActive(false);
		}

		protected virtual void UpdateAirborneDashKickHitbox()
		{
			if (_airborneDashKickDamageOnTouch == null)
			{
				return;
			}

			float direction = (_currentDirection == 0f) ? (_character.IsFacingRight ? 1f : -1f) : Mathf.Sign(_currentDirection);
			_airborneDashKickHitboxTransform.localPosition = new Vector3(AirborneDashKickAreaOffset.x * direction, AirborneDashKickAreaOffset.y, 0f);

			BoxCollider2D boxCollider = _airborneDashKickDamageOnTouch.GetComponent<BoxCollider2D>();
			if (boxCollider != null)
			{
				boxCollider.size = AirborneDashKickAreaSize;
			}

			SetAirborneDashKickHitboxActive(ShouldEnableAirborneDashKickHitbox());
		}

		protected virtual bool ShouldEnableAirborneDashKickHitbox()
		{
			return EnableAirborneDashKick
			       && (_character != null)
			       && (_character.CharacterType == Character.CharacterTypes.Player)
			       && (_movement != null)
			       && (_movement.CurrentState == CharacterStates.MovementStates.Dashing)
			       && (_controller != null)
			       && !_controller.State.IsGrounded
			       && !_controller.State.IsCollidingBelow;
		}

		protected virtual void SetAirborneDashKickHitboxActive(bool active)
		{
			if ((_airborneDashKickDamageOnTouch != null) && (_airborneDashKickDamageOnTouch.gameObject.activeSelf != active))
			{
				_airborneDashKickDamageOnTouch.gameObject.SetActive(active);
			}
		}

		protected virtual void OnAirborneDashKickHitDamageable()
		{
			if (!ShouldEnableAirborneDashKickHitbox() || (Time.time - _lastAirborneDashKickHitAt < _airborneDashKickHitLockout))
			{
				return;
			}

			_lastAirborneDashKickHitAt = Time.time;
			_airborneDashKickHitThisDash = true;
			PlayAirborneDashKickHitVfx();

			if (RefreshDashOnAirborneKickHit)
			{
				SetSuccessiveDashesLeft(Mathf.Max(SuccessiveDashesLeft, 1));
				_cooldownTimeStamp = Time.time + Mathf.Max(0f, AirborneKickHitDashCooldown);
			}

			if (JumpPastTargetOnAirborneKickHit)
			{
				MovePastAirborneKickTarget();
			}
		}

		protected virtual void MovePastAirborneKickTarget()
		{
			if ((_characterTransform == null) || (_controller == null))
			{
				return;
			}

			float direction = (_currentDirection == 0f) ? (_character.IsFacingRight ? 1f : -1f) : Mathf.Sign(_currentDirection);
			float horizontalMove = Mathf.Max(0f, AirborneKickHitHorizontalMove);
			Vector2 moveDirection = Vector2.right * direction;
			RaycastHit2D hit = Physics2D.BoxCast(_controller.BoundsCenter, _controller.Bounds, 0f, moveDirection, horizontalMove, AirborneKickHitObstacleMask);
			if (hit.collider != null)
			{
				horizontalMove = Mathf.Max(0f, hit.distance - 0.05f);
			}

			if (_airborneKickSideSwitchCoroutine != null)
			{
				StopCoroutine(_airborneKickSideSwitchCoroutine);
			}

			StopDash();
			SetAirborneDashKickHitboxActive(false);
			_airborneKickSideSwitchCoroutine = StartCoroutine(AirborneKickSideSwitchCo(direction, horizontalMove));
		}

		protected virtual IEnumerator AirborneKickSideSwitchCo(float crossingDirection, float horizontalMove)
		{
			Vector3 startPosition = _characterTransform.position;
			Vector3 targetPosition = startPosition + new Vector3(horizontalMove * crossingDirection, AirborneKickHitVerticalMove, 0f);
			float duration = Mathf.Max(0.01f, AirborneKickHitMoveDuration);
			float elapsed = 0f;

			_controller.SetForce(Vector2.zero);
			_controller.GravityActive(false);
			if (_characterHorizontalMovement != null)
			{
				_characterHorizontalMovement.ReadInput = false;
			}

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float normalizedTime = Mathf.Clamp01(elapsed / duration);
				float easedTime = normalizedTime * normalizedTime * (3f - (2f * normalizedTime));
				_characterTransform.position = Vector3.Lerp(startPosition, targetPosition, easedTime);
				yield return null;
			}

			_characterTransform.position = targetPosition;
			FaceBackTowardAirborneKickTarget(crossingDirection);

			if (_characterHorizontalMovement != null)
			{
				_characterHorizontalMovement.ReadInput = true;
			}
			_controller.GravityActive(true);

			float controlledDirection = Mathf.Abs(_horizontalInput) > MinimumInputThreshold ? Mathf.Sign(_horizontalInput) : -crossingDirection;
			_controller.SetForce(new Vector2(AirborneKickHitForce.x * controlledDirection, AirborneKickHitForce.y));
			_airborneKickSideSwitchCoroutine = null;
		}

		protected virtual void FaceBackTowardAirborneKickTarget(float crossingDirection)
		{
			if (_character == null)
			{
				return;
			}

			bool shouldFaceRight = crossingDirection < 0f;
			if (_character.IsFacingRight != shouldFaceRight)
			{
				_character.Flip();
			}

			_currentDirection = shouldFaceRight ? 1f : -1f;
			_drivenInput = 0f;
		}

		protected virtual void PlayAirborneDashKickHitVfx()
		{
			if ((AirborneDashKickVfxPrefab == null) && (AirborneDashKickVfxClip == null))
			{
				return;
			}

			float direction = (_currentDirection == 0f) ? (_character.IsFacingRight ? 1f : -1f) : Mathf.Sign(_currentDirection);
			Vector3 vfxPosition = _characterTransform.position + new Vector3(AirborneDashKickVfxOffset.x * direction, AirborneDashKickVfxOffset.y, 0f);
			GameObject vfxObject = (AirborneDashKickVfxPrefab != null)
				? Instantiate(AirborneDashKickVfxPrefab, vfxPosition, Quaternion.identity)
				: new GameObject("Dash Kick VFX");

			vfxObject.transform.position = vfxPosition;
			vfxObject.transform.localScale = new Vector3(AirborneDashKickVfxSize.x * direction, AirborneDashKickVfxSize.y, 1f);

			SpriteRenderer vfxRenderer = vfxObject.GetComponent<SpriteRenderer>();
			if (vfxRenderer == null)
			{
				vfxRenderer = vfxObject.AddComponent<SpriteRenderer>();
			}
			if (_spriteRenderer != null)
			{
				vfxRenderer.sortingLayerID = _spriteRenderer.sortingLayerID;
				vfxRenderer.sortingOrder = _spriteRenderer.sortingOrder + AirborneDashKickVfxSortingOrderOffset;
			}

			if (AirborneDashKickVfxClip != null)
			{
				Animator animator = vfxObject.GetComponent<Animator>();
				if (animator == null)
				{
					animator = vfxObject.AddComponent<Animator>();
				}
				DashKickVfxPlayable playable = vfxObject.AddComponent<DashKickVfxPlayable>();
				playable.Play(animator, AirborneDashKickVfxClip);
			}

			Destroy(vfxObject, Mathf.Max(0.01f, AirborneDashKickVfxDuration));
		}

		protected virtual void OnDestroy()
		{
			StopAirborneKickSideSwitch();
			if (_airborneDashKickDamageOnTouch != null)
			{
				_airborneDashKickDamageOnTouch.OnHitDamageable -= OnAirborneDashKickHitDamageable;
			}
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			UnityEditor.SceneView.RepaintAll();
		}
#endif

		protected virtual void StopAirborneKickSideSwitch()
		{
			if (_airborneKickSideSwitchCoroutine != null)
			{
				StopCoroutine(_airborneKickSideSwitchCoroutine);
				_airborneKickSideSwitchCoroutine = null;
			}
			if (_characterHorizontalMovement != null)
			{
				_characterHorizontalMovement.ReadInput = true;
			}
			if (_controller != null)
			{
				_controller.GravityActive(true);
			}
		}
	}

	public class DashKickVfxPlayable : MonoBehaviour
	{
		protected PlayableGraph _graph;

		public virtual void Play(Animator animator, AnimationClip clip)
		{
			if ((animator == null) || (clip == null))
			{
				return;
			}

			_graph = PlayableGraph.Create("DashKickVfx");
			AnimationPlayableOutput output = AnimationPlayableOutput.Create(_graph, "Animation", animator);
			AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, clip);
			output.SetSourcePlayable(playable);
			_graph.Play();
		}

		protected virtual void OnDestroy()
		{
			if (_graph.IsValid())
			{
				_graph.Destroy();
			}
		}
	}
}
