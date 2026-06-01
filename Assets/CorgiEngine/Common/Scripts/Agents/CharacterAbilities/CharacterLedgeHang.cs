using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Add this component to a character and it'll be able to hang from ledges and climb up
	/// Animator parameters : LedgeHanging, LedgeClimbing
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Ledge Hang")]
	public class CharacterLedgeHang : CharacterAbility, MMEventListener<LedgeEvent>
	{                
		public override string HelpBoxText() { return "This component allows you to hang from objects with a Ledge component on them. From the inspector here you'll be able to specify the name of the Idle animation to return to after the climb, the duration of the climb animation (in seconds), and a minimum buffer delay for the hang time (0.2s is a safe value)."; }

		[Header("Animation")]

		/// the name of the animation to play after the climb animation is complete (usually your Idle animation)
		[Tooltip("the name of the animation to play after the climb animation is complete (usually your Idle animation)")]
		public string IdleAnimationName = "Idle";
		/// the duration of your climbing animation, after this it'll transition to IdleAnimationName automatically
		[Tooltip("the duration of your climbing animation, after this it'll transition to IdleAnimationName automatically")]
		public float ClimbingAnimationDuration = 0.5f;
		[Header("Settings")]
		/// the minimum time the Character must have been LedgeHanging before it can LedgeClimb. 0.2s (or more) will prevent any glitches and unwanted input conflicts
		[Tooltip("the minimum time the Character must have been LedgeHanging before it can LedgeClimb. 0.2s (or more) will prevent any glitches and unwanted input conflicts")]
		public float MinimumHangingTime = 0.2f;
		[Header("Ledge Climb Jump")]
		[Tooltip("If true, pressing the ledge climb jump key while hanging plays the ledge climb animation, then launches the character away from the ledge.")]
		public bool AllowLedgeClimbJump = true;
		[Tooltip("The key used to start the ledge climb jump sequence.")]
		public KeyCode LedgeClimbJumpKey = KeyCode.Space;
		[Tooltip("If true, this ability uses the default ledge climb jump values below at runtime, ignoring old serialized Inspector values.")]
		public bool UseDefaultLedgeClimbJumpSettings = true;
		[Tooltip("How long the ledge climb animation plays before the ledge jump happens.")]
		public float LedgeClimbJumpAnimationTime = 0.2f;
		[Tooltip("If true, the character is moved to the ledge climb offset before the ledge jump force is applied.")]
		public bool MoveToClimbOffsetBeforeLedgeJump = true;
		[Tooltip("Horizontal force applied after the ledge climb animation. Increase this to change ledge jump distance.")]
		public float LedgeClimbJumpHorizontalForce = 6f;
		[Tooltip("Vertical force applied after the ledge climb animation.")]
		public float LedgeClimbJumpVerticalForce = 8f;
		[Tooltip("If true, the ledge hop horizontal direction uses the ledge's climb offset direction instead of the character facing direction.")]
		public bool UseClimbOffsetDirectionForLedgeHop = true;

		protected Ledge _ledge = null;
		protected CharacterJump _characterJump;
		protected WaitForSeconds _climbingAnimationDelay;
		protected float _ledgeHangingStartedTimestamp;
		protected Coroutine _climbCoroutine;
		protected bool _storedJumpAbilityPermitted;
		protected bool _jumpAbilityWasBlocked;
		protected bool _restoreJumpWhenLedgeJumpKeyReleased;

		/// <summary>
		/// On Start() we grab a few components for storage
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_characterJump = _character?.FindAbility<CharacterJump>();
			ApplyDefaultLedgeClimbJumpSettings();
			_climbingAnimationDelay = new WaitForSeconds(ClimbingAnimationDuration);
		}

		/// <summary>
		/// Applies a known working baseline so old serialized inspector values don't keep bad tuning.
		/// </summary>
		protected virtual void ApplyDefaultLedgeClimbJumpSettings()
		{
			if (!UseDefaultLedgeClimbJumpSettings)
			{
				return;
			}

			LedgeClimbJumpAnimationTime = 0.2f;
			MoveToClimbOffsetBeforeLedgeJump = true;
			LedgeClimbJumpHorizontalForce = 6f;
			LedgeClimbJumpVerticalForce = 8f;
			UseClimbOffsetDirectionForLedgeHop = true;
		}

		/// <summary>
		/// Every frame, we check the input for a up input, in case we're hanging
		/// </summary>
		protected override void HandleInput()
		{
		}

		/// <summary>
		/// Every frame we make sure we don't have to detach from the ledge
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();
			RestoreNormalJumpAfterLedgeJumpKeyRelease();
			HandleLedgeHopInput();
			HandleLedge();

			if ((_movement.CurrentState != CharacterStates.MovementStates.LedgeHanging)
			    && (_movement.CurrentState != CharacterStates.MovementStates.LedgeClimbing)
			    && (_movement.PreviousState == CharacterStates.MovementStates.LedgeHanging))
			{
				DetachFromLedge();
			}
		}

		/// <summary>
		/// When getting a ledge event, we make sure it's this Character, and if it is, we grab the ledge
		/// </summary>
		/// <param name="ledgeEvent"></param>
		public virtual void OnMMEvent(LedgeEvent ledgeEvent)
		{
			if (ledgeEvent.CharacterCollider.gameObject != _character.gameObject)
			{
				return;
			}
			StartGrabbingLedge(ledgeEvent.LedgeGrabbed);
		}

		/// <summary>
		/// Grabs the ledge if possible
		/// </summary>
		/// <param name="ledge"></param>
		public virtual void StartGrabbingLedge(Ledge ledge)
		{
			// we make sure we're facing the right direction
			if ( (_character.IsFacingRight && (ledge.LedgeGrabDirection == Ledge.LedgeGrabDirections.Left))
			     || (!_character.IsFacingRight && (ledge.LedgeGrabDirection == Ledge.LedgeGrabDirections.Right)))
			{
				return;
			}

			// we make sure we can grab the ledge
			if (!AbilityAuthorized
			    || (_movement.CurrentState == CharacterStates.MovementStates.Jetpacking))
			{
				return;
			}

			// we start hanging from the ledge
			_ledgeHangingStartedTimestamp = Time.time;
			_ledge = ledge;
			_controller.CollisionsOff();
			BlockNormalJumpWhileOnLedge();
			PlayAbilityStartFeedbacks();
			_movement.ChangeState(CharacterStates.MovementStates.LedgeHanging);
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.LedgeHang, MMCharacterEvent.Moments.Start);
		}

		/// <summary>
		/// Every frame, if we're hanging from a ledge, we prevent any force from moving our character, prevent flip and force our position to the ledge's offset
		/// </summary>
		protected virtual void HandleLedge()
		{
			if (_movement.CurrentState == CharacterStates.MovementStates.LedgeHanging)
			{
				_controller.SetForce(Vector2.zero);
				_controller.GravityActive(false);
				if (_characterJump != null)
				{
					_characterJump.ResetNumberOfJumps();
				}
				_characterHorizontalMovement.AbilityPermitted = false;
				_character.CanFlip = false;
				_controller.transform.position = _ledge.transform.position + _ledge.HangOffset;
			}
		}

		/// <summary>
		/// This coroutine handles the climb sequence
		/// </summary>
		/// <returns></returns>
		protected virtual IEnumerator Climb()
		{
			// we start to climb
			_movement.ChangeState(CharacterStates.MovementStates.LedgeClimbing);
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeClimbingAnimationParameter, true, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			// we prevent all other input
			_inputManager.InputDetectionActive = false;

			// we wait until the climb animation is complete
			yield return _climbingAnimationDelay;

			// we restore input and go to idle
			_inputManager.InputDetectionActive = true;
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeClimbingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _idleAnimationParameter, true, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			_animator.Play(IdleAnimationName);
            
			// we teleport our character to its new position (this offset is specified on the Ledge object)
			_character.transform.position = _ledge.transform.position + _ledge.ClimbOffset;
            
			// we go back to idle and detach from the ledge
			_movement.ChangeState(CharacterStates.MovementStates.Idle);
			_controller.GravityActive(true);
			_climbCoroutine = null;
			DetachFromLedge();
		}

		/// <summary>
		/// Prevents the normal CharacterJump ability from firing while hanging on a ledge.
		/// </summary>
		protected virtual void BlockNormalJumpWhileOnLedge()
		{
			if ((_characterJump == null) || _jumpAbilityWasBlocked)
			{
				return;
			}

			_storedJumpAbilityPermitted = _characterJump.AbilityPermitted;
			_characterJump.AbilityPermitted = false;
			_jumpAbilityWasBlocked = true;
		}

		/// <summary>
		/// Handles the instant ledge hop directly in ProcessAbility so it doesn't rely on normal input routing.
		/// </summary>
		protected virtual void HandleLedgeHopInput()
		{
			if (!AllowLedgeClimbJump
			    || (_movement.CurrentState != CharacterStates.MovementStates.LedgeHanging)
			    || !Input.GetKeyDown(LedgeClimbJumpKey))
			{
				return;
			}

			StartLedgeClimbJump();
		}

		/// <summary>
		/// Starts the ledge climb animation, then jumps away from the ledge after a short delay.
		/// </summary>
		protected virtual void StartLedgeClimbJump()
		{
			if (_climbCoroutine != null)
			{
				StopCoroutine(_climbCoroutine);
				_climbCoroutine = null;
			}

			_climbCoroutine = StartCoroutine(LedgeClimbJumpSequence());
		}

		/// <summary>
		/// Plays the ledge climb animation first, then performs the ledge jump.
		/// </summary>
		protected virtual IEnumerator LedgeClimbJumpSequence()
		{
			_movement.ChangeState(CharacterStates.MovementStates.LedgeClimbing);
			_controller.SetForce(Vector2.zero);
			_controller.GravityActive(false);
			_inputManager.InputDetectionActive = false;
			_characterHorizontalMovement.AbilityPermitted = false;
			_character.CanFlip = false;
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeHangingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeClimbingAnimationParameter, true, _character._animatorParameters, _character.PerformAnimatorSanityChecks);

			yield return new WaitForSeconds(Mathf.Max(0f, LedgeClimbJumpAnimationTime));

			_climbCoroutine = null;
			PerformLedgeClimbJump();
		}

		/// <summary>
		/// Launches the character away from the ledge without using the normal jump ability.
		/// </summary>
		protected virtual void PerformLedgeClimbJump()
		{
			_inputManager.InputDetectionActive = true;
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeClimbingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeHangingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);

			float horizontalDirection = GetLedgeHopHorizontalDirection();
			if (MoveToClimbOffsetBeforeLedgeJump && (_ledge != null))
			{
				_character.transform.position = _ledge.transform.position + _ledge.ClimbOffset;
			}

			_controller.CollisionsOn();
			_controller.GravityActive(true);
			_character.CanFlip = true;
			_characterHorizontalMovement.AbilityPermitted = true;
			_movement.ChangeState(CharacterStates.MovementStates.Jumping);
			_controller.SetForce(new Vector2(horizontalDirection * LedgeClimbJumpHorizontalForce, LedgeClimbJumpVerticalForce));
			_restoreJumpWhenLedgeJumpKeyReleased = true;
			DetachFromLedge();
		}

		/// <summary>
		/// Determines the horizontal direction for the ledge hop.
		/// </summary>
		protected virtual float GetLedgeHopHorizontalDirection()
		{
			if (UseClimbOffsetDirectionForLedgeHop && (_ledge != null))
			{
				float climbDirection = _ledge.ClimbOffset.x - _ledge.HangOffset.x;
				if (Mathf.Abs(climbDirection) > 0.01f)
				{
					return Mathf.Sign(climbDirection);
				}
			}

			return _character.IsFacingRight ? 1f : -1f;
		}

		/// <summary>
		/// Detaches the Character from the ledge, losing any reference to it, and restoring permissions
		/// </summary>
		protected virtual void DetachFromLedge()
		{
			_ledge = null;
			_character.CanFlip = true;
			_characterHorizontalMovement.AbilityPermitted = true;
			if (!_restoreJumpWhenLedgeJumpKeyReleased)
			{
				RestoreNormalJump();
			}
			_controller.CollisionsOn();
			if (_startFeedbackIsPlaying)
			{
				StopStartFeedbacks();
				PlayAbilityStopFeedbacks();
				MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.LedgeHang, MMCharacterEvent.Moments.End);
			}            
		}

		/// <summary>
		/// Restores the normal jump ability after leaving the ledge.
		/// </summary>
		protected virtual void RestoreNormalJump()
		{
			if ((_characterJump == null) || !_jumpAbilityWasBlocked)
			{
				return;
			}

			_characterJump.AbilityPermitted = _storedJumpAbilityPermitted;
			_jumpAbilityWasBlocked = false;
		}

		/// <summary>
		/// Restores normal jump after the ledge jump key is released, preventing a second jump from the same Space press.
		/// </summary>
		protected virtual void RestoreNormalJumpAfterLedgeJumpKeyRelease()
		{
			if (!_restoreJumpWhenLedgeJumpKeyReleased || Input.GetKey(LedgeClimbJumpKey))
			{
				return;
			}

			_restoreJumpWhenLedgeJumpKeyReleased = false;
			RestoreNormalJump();
		}


		// animation parameters
		protected const string _ledgeHangingAnimationParameterName = "LedgeHanging";
		protected const string _ledgeClimbingAnimationParameterName = "LedgeClimbing";
		protected int _ledgeHangingAnimationParameter;
		protected int _ledgeClimbingAnimationParameter;
		protected int _idleAnimationParameter;

		/// <summary>
		/// Initializes the LedgeHanging and LedgeClimbing animator parameters
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			_idleAnimationParameter = Animator.StringToHash(IdleAnimationName);
			RegisterAnimatorParameter(_ledgeHangingAnimationParameterName, AnimatorControllerParameterType.Bool, out _ledgeHangingAnimationParameter);
			RegisterAnimatorParameter(_ledgeClimbingAnimationParameterName, AnimatorControllerParameterType.Bool, out _ledgeClimbingAnimationParameter);
		}

		/// <summary>
		/// At the end of each cycle, we send our current LookingUp status to the animator
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeHangingAnimationParameter, (_movement.CurrentState == CharacterStates.MovementStates.LedgeHanging), _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		/// <summary>
		/// On enable, we start listening for LedgeEvents
		/// </summary>
		protected override void OnEnable()
		{
			base.OnEnable();
			this.MMEventStartListening<LedgeEvent>();
		}

		/// <summary>
		/// On disable, we stop listening for ledge events
		/// </summary>
		protected override void OnDisable()
		{
			base.OnDisable();
			this.MMEventStopListening<LedgeEvent>();
		}
        
		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			if (_climbCoroutine != null)
			{
				StopCoroutine(_climbCoroutine);
				_climbCoroutine = null;
			}
			if (_condition.CurrentState == CharacterStates.CharacterConditions.Normal)
			{
				DetachFromLedge();
			}
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ledgeHangingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}
	}
}
