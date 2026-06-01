using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// This component allows your character to fly by moving gravity-free on both x and y axis. Here you can define the flight speed, as well as whether or not the character is always flying (in which case you don't have to press a button to fly). Important note : slope ceilings are not supported for now.
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Fly")]
	public class CharacterFly : CharacterAbility
	{
		public override string HelpBoxText() { return "This component allows your character to fly by moving gravity-free on both x and y axis. Here you can define the flight speed, as well as whether or not the character is always flying (in which case you don't have to press a button to fly). Important note : slope ceilings are not supported for now."; }

		/// the speed at which the character should fly
		[Tooltip("the speed at which the character should fly")]
		public float FlySpeed = 6f;
		/// whether or not flight force should ramp smoothly instead of snapping instantly
		[Tooltip("whether or not flight force should ramp smoothly instead of snapping instantly")]
		public bool SmoothFlight = false;
		/// how fast flight force reaches its target when SmoothFlight is true
		[Tooltip("how fast flight force reaches its target when SmoothFlight is true")]
		public float FlightAcceleration = 20f;
		/// the vertical input to use while flying when the player isn't pressing up or down
		[Tooltip("the vertical input to use while flying when the player isn't pressing up or down")]
		public float NeutralVerticalInput = 1f;
		/// a multiplier you can target to increase/reduce the flight speed
		public float MovementSpeedMultiplier { get; set; }
		/// whether or not the Character is always flying, in which case it'll start immune to gravity 
		[Tooltip("whether or not the Character is always flying, in which case it'll start immune to gravity ")]
		public bool AlwaysFlying = false;
		/// whether or not the Character should stop flying on death
		[Tooltip("whether or not the Character should stop flying on death")]
		public bool StopFlyingOnDeath = true;

		[Header("Fuel")]
		/// if true, this character can fly forever
		[Tooltip("if true, this character can fly forever")]
		public bool FlightUnlimited = false;
		/// the maximum amount of time, in seconds, the character can fly before needing to refuel
		[Tooltip("the maximum amount of time, in seconds, the character can fly before needing to refuel")]
		public float FlightFuelDuration = 1.5f;
		/// the cooldown, in seconds, before flight starts refueling
		[Tooltip("the cooldown, in seconds, before flight starts refueling")]
		public float FlightRefuelCooldown = 0.75f;
		/// how fast flight fuel refills
		[Tooltip("how fast flight fuel refills")]
		public float FlightRefuelSpeed = 0.75f;
		/// the minimum fuel needed to start a new flight burst
		[Tooltip("the minimum fuel needed to start a new flight burst")]
		public float MinimumFuelRequirement = 0.2f;

		protected float _horizontalMovement;
		protected float _verticalMovement;
		protected bool _flying;
		protected float _flightFuelDurationLeft;
		protected float _flightStoppedAt;
		protected Vector2 _currentFlightVelocity;
        
		// animation parameters
		protected const string _flyingAnimationParameterName = "Flying";
		protected const string _flySpeedAnimationParameterName = "FlySpeed";
		protected int _flyingAnimationParameter;
		protected int _flySpeedAnimationParameter;

		/// <summary>
		/// On Start, we initialize our flight if needed
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();

			MovementSpeedMultiplier = 1f;
			_flightFuelDurationLeft = FlightFuelDuration;
			_flightStoppedAt = -FlightRefuelCooldown;
			_currentFlightVelocity = Vector2.zero;

			if (AlwaysFlying)
			{
				StartFlight();
			}
		}

		/// <summary>
		/// Looks for hztal and vertical input, and for flight button if needed
		/// </summary>
		protected override void HandleInput()
		{
			_horizontalMovement = _horizontalInput;
			_verticalMovement = _verticalInput;

			if (!AlwaysFlying)
			{
				if (_inputManager.FlyButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
				{
					StartFlight();
				}

				if ((_inputManager.FlyButton.State.CurrentState == MMInput.ButtonStates.ButtonUp) && (_movement.CurrentState == CharacterStates.MovementStates.Flying))
				{
					StopFlight();
				}
			}
		}

		/// <summary>
		/// Sets the horizontal move value.
		/// </summary>
		/// <param name="value">Horizontal move value, between -1 and 1 - positive : will move to the right, negative : will move left </param>
		public virtual void SetHorizontalMove(float value)
		{
			_horizontalMovement = value;
		}

		/// <summary>
		/// Sets the horizontal move value.
		/// </summary>
		/// <param name="value">Horizontal move value, between -1 and 1 - positive : will move to the right, negative : will move left </param>
		public virtual void SetVerticalMove(float value)
		{
			_verticalMovement = value;
		}

		/// <summary>
		/// Starts the flight sequence
		/// </summary>
		public virtual void StartFlight()
		{
			if ((!AbilityAuthorized) // if the ability is not permitted
			    || (!HasEnoughFuelToStartOrContinue())
			    || (_movement.CurrentState == CharacterStates.MovementStates.Dashing) // or if we're dashing
			    || (_movement.CurrentState == CharacterStates.MovementStates.Gripping) // or if we're in the gripping state
			    || (_condition.CurrentState != CharacterStates.CharacterConditions.Normal)) // or if we're not in normal conditions
			{
				return;
			}

			// if this is the first time we're here, we trigger our sounds
			if (_movement.CurrentState != CharacterStates.MovementStates.Flying)
			{
				// we play the jetpack start sound 
				PlayAbilityStartFeedbacks();
				MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Fly, MMCharacterEvent.Moments.Start);
				_flying = true;
				_currentFlightVelocity = new Vector2(_controller.Speed.x, Mathf.Max(0f, _controller.Speed.y));
			}

			// we set the various states
			_movement.ChangeState(CharacterStates.MovementStates.Flying);

			MovementSpeedMultiplier = 1f;
			_controller.GravityActive(false);
		}

		/// <summary>
		/// Returns true if there is enough fuel to start or keep a flight burst going.
		/// </summary>
		protected virtual bool HasEnoughFuelToStartOrContinue()
		{
			if (FlightUnlimited)
			{
				return true;
			}

			if ((_movement.CurrentState == CharacterStates.MovementStates.Flying) || _flying)
			{
				return _flightFuelDurationLeft > 0f;
			}

			return _flightFuelDurationLeft >= MinimumFuelRequirement;
		}

		/// <summary>
		/// Stops the flight
		/// </summary>
		public virtual void StopFlight()
		{
			_flying = false;
			if (_movement.CurrentState == CharacterStates.MovementStates.Flying)
			{
				StopStartFeedbacks();
				PlayAbilityStopFeedbacks();
				MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Fly, MMCharacterEvent.Moments.End);
			}
			if (_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing)
			{
				return;
			}
			_controller.GravityActive(true);
			_movement.RestorePreviousState();
			_flightStoppedAt = Time.time;
			_currentFlightVelocity = Vector2.zero;
		}

		/// <summary>
		/// On Update, checks if we should stop flying
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();

			if (StopFlyingOnDeath && (_character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Dead))
			{
				return;
			}

			if (AlwaysFlying)
			{
				if (_movement.CurrentState != CharacterStates.MovementStates.Flying)
				{
					_movement.ChangeState(CharacterStates.MovementStates.Flying);
				}                
				_flying = true;
			}

			if (_flying)
			{
				_controller.GravityActive(false);
			}

			HandleMovement();
			BurnFuel();
			Refuel();
            
			// if we're not walking anymore, we stop our walking sound
			if (_movement.CurrentState != CharacterStates.MovementStates.Flying && _startFeedbackIsPlaying)
			{
				StopStartFeedbacks();
			}

			if (_movement.CurrentState != CharacterStates.MovementStates.Flying && _flying)
			{
				StopFlight();
			}

			if (_controller.State.IsCollidingAbove && (_movement.CurrentState != CharacterStates.MovementStates.Flying))
			{
				_controller.SetVerticalForce(0);
			}
		}

		/// <summary>
		/// Consumes flight fuel while flying.
		/// </summary>
		protected virtual void BurnFuel()
		{
			if (FlightUnlimited)
			{
				return;
			}

			if ((_movement.CurrentState == CharacterStates.MovementStates.Flying) && (_flightFuelDurationLeft > 0f))
			{
				_flightFuelDurationLeft -= Time.deltaTime;
				if (_flightFuelDurationLeft <= 0f)
				{
					_flightFuelDurationLeft = 0f;
					StopFlight();
				}
			}
		}

		/// <summary>
		/// Refuels flight after a short cooldown.
		/// </summary>
		protected virtual void Refuel()
		{
			if (FlightUnlimited)
			{
				return;
			}

			if (_movement.CurrentState == CharacterStates.MovementStates.Flying)
			{
				return;
			}

			if (Time.time - _flightStoppedAt < FlightRefuelCooldown)
			{
				return;
			}

			if (_flightFuelDurationLeft < FlightFuelDuration)
			{
				_flightFuelDurationLeft += Time.deltaTime * FlightRefuelSpeed;
				if (_flightFuelDurationLeft > FlightFuelDuration)
				{
					_flightFuelDurationLeft = FlightFuelDuration;
				}
			}
		}


		/// <summary>
		/// Makes the character move in the air
		/// </summary>
		protected virtual void HandleMovement()
		{
			// if we're not walking anymore, we stop our walking sound
			if (_movement.CurrentState != CharacterStates.MovementStates.Flying && _startFeedbackIsPlaying)
			{
				StopStartFeedbacks();
			}

			// if movement is prevented, or if the character is dead/frozen/can't move, we exit and do nothing
			if (!AbilityAuthorized
			    || (_condition.CurrentState != CharacterStates.CharacterConditions.Normal)
			    || (_movement.CurrentState == CharacterStates.MovementStates.Gripping))
			{
				return;
			}
            
			// If the value of the horizontal axis is positive, the character must face right.
			if (_horizontalMovement > 0.1f)
			{
				if (!_character.IsFacingRight)
					_character.Flip();
			}
			// If it's negative, then we're facing left
			else if (_horizontalMovement < -0.1f)
			{
				if (_character.IsFacingRight)
					_character.Flip();
			}
            
			if (_flying)
			{
				float verticalInput = (Mathf.Abs(_verticalMovement) > 0.1f) ? _verticalMovement : NeutralVerticalInput;
				// we pass the horizontal force that needs to be applied to the controller.
				float horizontalMovementSpeed = _horizontalMovement * FlySpeed * _controller.Parameters.SpeedFactor * MovementSpeedMultiplier;
				float verticalMovementSpeed = verticalInput * FlySpeed * _controller.Parameters.SpeedFactor * MovementSpeedMultiplier;

				if (SmoothFlight)
				{
					Vector2 targetVelocity = new Vector2(horizontalMovementSpeed, verticalMovementSpeed);
					_currentFlightVelocity = Vector2.MoveTowards(_currentFlightVelocity, targetVelocity, FlightAcceleration * Time.deltaTime);
					horizontalMovementSpeed = _currentFlightVelocity.x;
					verticalMovementSpeed = _currentFlightVelocity.y;
				}

				// we set our newly computed speed to the controller
				_controller.SetHorizontalForce(horizontalMovementSpeed);
				_controller.SetVerticalForce(verticalMovementSpeed);
			}            
		}

		/// <summary>
		/// When the character respawns we reinitialize it
		/// </summary>
		protected virtual void OnRevive()
		{
			Initialization();
		}

		/// <summary>
		/// On death the character stops flying if needed
		/// </summary>
		protected override void OnDeath()
		{
			base.OnDeath();
			if (StopFlyingOnDeath)
			{
				StopFlight();
			}
		}

		/// <summary>
		/// When the player respawns, we reinstate this agent.
		/// </summary>
		/// <param name="checkpoint">Checkpoint.</param>
		/// <param name="player">Player.</param>
		protected override void OnEnable()
		{
			base.OnEnable();
			if (gameObject.GetComponentInParent<Health>() != null)
			{
				gameObject.GetComponentInParent<Health>().OnRevive += OnRevive;
			}
		}

		/// <summary>
		/// Stops listening for revive events
		/// </summary>
		protected override void OnDisable()
		{
			base.OnDisable();
			if (_health != null)
			{
				_health.OnRevive -= OnRevive;
			}
		}

		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter(_flyingAnimationParameterName, AnimatorControllerParameterType.Bool, out _flyingAnimationParameter);
			RegisterAnimatorParameter(_flySpeedAnimationParameterName, AnimatorControllerParameterType.Float, out _flySpeedAnimationParameter);
		}

		/// <summary>
		/// At the end of each cycle, we send our character's animator the current flying status
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _flyingAnimationParameter, (_movement.CurrentState == CharacterStates.MovementStates.Flying), _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _flySpeedAnimationParameter, Mathf.Abs(_controller.Speed.magnitude), _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			StopFlight();
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _flyingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _flySpeedAnimationParameter, 0f, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}
	}
}
