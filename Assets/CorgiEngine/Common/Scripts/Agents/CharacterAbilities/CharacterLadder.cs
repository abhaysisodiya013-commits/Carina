using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using System.Collections.Generic;

namespace MoreMountains.CorgiEngine
{	
	/// <summary>
	/// Add this component to a Character and it'll be able to go up and down ladders.
	/// Animator parameters : LadderClimbing (bool), LadderClimbingSpeed (float)
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Ladder")] 
	public class CharacterLadder : CharacterAbility 
	{
		[Header("Ladder Climbing")]
		/// the speed of the character when climbing a ladder
		[Tooltip("the speed of the character when climbing a ladder")]
		public float LadderClimbingSpeed = 2f;
		/// force face right when on a ladder (useful for 3D characters)
		[Tooltip("force face right when on a ladder (useful for 3D characters)")]
		public bool ForceRightFacing = false;
		/// forces the character to teleport to the ladder platform when reaching the ladder's top
		[Tooltip("forces the chaForceTeleportOnExitracter to teleport to the ladder platform when reaching the ladder's top")]
		public bool ForceAnchorToGroundOnExit = false;
		[Header("Ladder Positioning")]
		[Tooltip("If true, the character moves smoothly to the ladder x position instead of snapping instantly.")]
		public bool SmoothCenterCharacterOnLadder = true;
		[Tooltip("How fast the character moves toward the ladder x position. Set to 0 to snap instantly.")]
		public float LadderCenteringSpeed = 8f;
		[Header("Ladder Animation")]
		[Tooltip("If true, stopping on a ladder keeps the last climb pose instead of returning to ladder idle.")]
		public bool HoldLastClimbAnimationWhenStopped = true;
		[Tooltip("If true, the Animator is paused while the character is idle on a ladder, freezing the last played climb frame.")]
		public bool FreezeLastLadderFrameWhenStopped = true;
		[Header("Ladder Top Exit")]
		[Tooltip("If true, reaching the top of the ladder exits the ladder state.")]
		public bool ExitAtLadderTop = true;
		[Tooltip("If true, reaching the ladder platform stops the character instead of carrying ladder movement into the platform.")]
		public bool StopAtLadderPlatform = true;
		[Tooltip("How close the character's feet can be to the top of the ladder before platform stop triggers.")]
		public float LadderPlatformStopDistance = 0.2f;
		[Tooltip("The vertical force applied when pressing jump from the stopped top-of-ladder state.")]
		public float LadderTopManualJumpForce = 10f;
		[Header("Ladder Jump")]
		[Tooltip("If true, pressing the jump button while climbing gives the character a short upward ladder boost.")]
		public bool AllowLadderJumpBoost = true;
		[Tooltip("If true, pressing the jump button while holding down on a ladder drops the character instead of boosting up.")]
		public bool DropFromLadderWhenJumpingDown = true;
		[Tooltip("The vertical speed applied during a ladder jump boost.")]
		public float LadderJumpBoostSpeed = 6f;
		[Tooltip("How long the ladder jump boost lasts, in seconds.")]
		public float LadderJumpBoostDuration = 0.18f;
		[Tooltip("Minimum delay between two ladder jump boosts, in seconds.")]
		public float LadderJumpBoostCooldown = 0.2f;
		[Tooltip("If true, the regular jump ability is disabled while climbing so Space only performs the ladder boost.")]
		public bool BlockNormalJumpWhileClimbing = true;
		/// the current ladder climbing speed of the character
		public Vector2 CurrentLadderClimbingSpeed{get; set;}
		/// true if the character is colliding with a ladder
		public bool LadderColliding
		{
			get
			{
				return (_colliders.Count > 0);
			}
		}
		/// the ladder the character is currently on
		public Ladder CurrentLadder { get; set; }
		/// the highest ladder the character is currently colliding with
		public Ladder HighestLadder { get; set; }
		/// the lowest ladder the character is currently colliding with
		public Ladder LowestLadder { get; set; }
		
		const float _climbingDownInitialYTranslation = 0.1f;
		const float _ladderTopSkinHeight = 0.01f;

		protected BoxCollider2D _boxCollider;
		protected List<Collider2D> _colliders;
		protected CharacterHandleWeapon _characterHandleWeapon;
		protected CharacterJump _characterJump;
		protected Vector2 _lastNonZeroLadderClimbingSpeed;
		protected bool _ladderAnimatorFrozen;
		protected float _storedLadderAnimatorSpeed = 1f;
		protected bool _jumpAbilityWasBlocked;
		protected bool _storedJumpAbilityPermitted;
		protected float _ladderJumpBoostUntil;
		protected float _lastLadderJumpBoostAt = -Mathf.Infinity;
		protected bool _ladderTopStopLock;
		protected bool _waitingAtLadderPlatform;

		// animation parameters
		protected const string _ladderClimbingUpAnimationParameterName = "LadderClimbing";
		protected const string _ladderClimbingSpeedXAnimationParameterName = "LadderClimbingSpeedX";
		protected const string _ladderClimbingSpeedYpAnimationParameterName = "LadderClimbingSpeedY";
		protected int _ladderClimbingUpAnimationParameter;
		protected int _ladderClimbingSpeedXAnimationParameter;
		protected int _ladderClimbingSpeedYAnimationParameter;

		/// <summary>
		/// On Start(), we initialize our various flags
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			CurrentLadderClimbingSpeed = Vector2.zero;
			_boxCollider = this.gameObject.GetComponentInParent<BoxCollider2D>();
			_colliders = new List<Collider2D>();
			_characterHandleWeapon = this.gameObject.GetComponentInParent<Character>()?.FindAbility<CharacterHandleWeapon>();
			_characterJump = this.gameObject.GetComponentInParent<Character>()?.FindAbility<CharacterJump>();
		}

		/// <summary>
		/// Every frame, we check if we need to do something about ladders
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();
			ComputeClosestLadder();
			HandleLadderClimbing();
		}

		/// <summary>
		/// Adds a new ladder to the list of colliding ladders
		/// </summary>
		/// <param name="newCollider"></param>
		public virtual void AddCollidingLadder(Collider2D newCollider)
		{
			if (_colliders == null)
			{
				Initialization();
			}
			_colliders.Add(newCollider);
		}

		/// <summary>
		/// Removes a ladder from the list of colliding ladders
		/// </summary>
		public virtual void RemoveCollidingLadder(Collider2D newCollider)
		{
			_colliders.Remove(newCollider);
		}

		/// <summary>
		/// Determines the current, highest and lowest ladders if there are any
		/// </summary>
		protected virtual void ComputeClosestLadder()
		{
			CurrentLadder = null;
			HighestLadder = null;
			LowestLadder = null;

			if (_colliders.Count > 0)
			{
				float closestHorizontalDistance = Mathf.Infinity;
				int closestHorizontalIndex = 0;

				float highestPosition = -Mathf.Infinity;
				int highestLadderIndex = 0;

				float lowestPosition = Mathf.Infinity;
				int lowestLadderIndex = 0;

				for (int i = 0; i < _colliders.Count; i++)
				{
					float distance = Mathf.Abs(_colliders[i].bounds.center.x - _controller.BoundsCenter.x);
					float yPosition = _colliders[i].bounds.center.y;

					if (distance < closestHorizontalDistance)
					{
						closestHorizontalIndex = i;
						closestHorizontalDistance = distance;
					}

					if (yPosition > highestPosition)
					{
						highestPosition = yPosition;
						highestLadderIndex = i;
					}

					if (yPosition < lowestPosition)
					{
						lowestPosition = yPosition;
						lowestLadderIndex = i;
					}

				}
				CurrentLadder = _colliders[closestHorizontalIndex].gameObject.MMGetComponentNoAlloc<Ladder>();
				LowestLadder = _colliders[lowestLadderIndex].gameObject.MMGetComponentNoAlloc<Ladder>();
				HighestLadder = _colliders[highestLadderIndex].gameObject.MMGetComponentNoAlloc<Ladder>();
			}
		}

		/// <summary>
		/// Called at ProcessAbility(), checks if we're colliding with a ladder and if we need to do something about it
		/// </summary>	
		protected virtual void HandleLadderClimbing()
		{
			if (!AbilityAuthorized
			    || (_condition.CurrentState != CharacterStates.CharacterConditions.Normal && _condition.CurrentState != CharacterStates.CharacterConditions.ControlledMovement ))
			{
				return;
			}

			// if the character is colliding with a ladder
			if (LadderColliding) 
			{
				UpdateLadderTopStopLock();

				if ((_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing) // if the character is climbing
				    && _controller.State.IsGrounded) // and is grounded
				{
					if (StopAtLadderPlatform
					    && (HighestLadder != null)
					    && (HighestLadder.LadderPlatform != null)
					    && ShouldStopAtLadderPlatform(HighestLadder))
					{
						StopAtTopPlatform();
					}
					else
					{
						// we make it get off the ladder
						GetOffTheLadder();
					}
				}

				if (_inputManager == null)
				{
					return;
				}

				if (_verticalInput > _inputManager.Threshold.y// if the player is pressing up
				    && (_movement.CurrentState != CharacterStates.MovementStates.LadderClimbing) // and we're not climbing a ladder already
				    && (_movement.CurrentState != CharacterStates.MovementStates.Gliding) // and we're not gliding
				    && (_movement.CurrentState != CharacterStates.MovementStates.Jetpacking) // and we're not jetpacking
				    && !_ladderTopStopLock)
				{			
					// then the character starts climbing
					StartClimbing();
				}	

				// if the character is climbing the ladder (which means it previously connected with it)
				if (_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing)
				{
					if (_waitingAtLadderPlatform)
					{
						HandleTopPlatformWait();
					}
					else
					{
						Climbing();
					}
				}

				if (CurrentLadder == null)
				{
					return;
				}

				// if the highest ladder does have a ladder platform associated to it
				if ((HighestLadder != null) && (HighestLadder.LadderPlatform != null))
				{
					if ((_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing) // if the character is climbing
					    && ExitAtLadderTop
					    && ShouldStopAtLadderPlatform(HighestLadder)) // and is close to the final ladder platform
					{
						if (StopAtLadderPlatform)
						{
							StopAtTopPlatform();
						}
						else
						{
							// we make it get off the ladder
							GetOffTheLadder();
						}
						if (ForceAnchorToGroundOnExit)
						{
							_controller.AnchorToGround();	
						}
					}
				}

				// if the lowest ladder does have a ladder platform associated to it
				if ((LowestLadder != null) && (LowestLadder.LadderPlatform != null))
				{
					if ((_movement.CurrentState != CharacterStates.MovementStates.LadderClimbing) // if the character is climbing
					    && (_movement.CurrentState != CharacterStates.MovementStates.Flying)
					    && (_verticalInput < -_inputManager.Threshold.y) // and is pressing down
					    && (AboveLadderPlatform()) // and is above the ladder's platform
					    && _controller.State.IsGrounded) // and is grounded
					{
						// we make it get off the ladder
						StartClimbingDown();
					}
				}

			}
			else
			{
				// if we're not colliding with a ladder, but are still in the LadderClimbing state
				if (_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing)
				{
					GetOffTheLadder();
				}
				_ladderTopStopLock = false;
				_waitingAtLadderPlatform = false;
			}

			HandleFeedbacks();
		}

		protected virtual void HandleFeedbacks()
		{
			if (_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing)
			{
				if ((CurrentLadderClimbingSpeed == Vector2.zero) && _startFeedbackIsPlaying)
				{
					StopStartFeedbacks();
					PlayAbilityStopFeedbacks();
					MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Ladder, MMCharacterEvent.Moments.End);
				}
				if ((CurrentLadderClimbingSpeed != Vector2.zero) && !_startFeedbackIsPlaying)
				{
					PlayAbilityStartFeedbacks();
					MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Ladder, MMCharacterEvent.Moments.Start);
				}
			}
			else
			{
				if (_startFeedbackIsPlaying)
				{
					StopStartFeedbacks();
					PlayAbilityStopFeedbacks();
					MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Ladder, MMCharacterEvent.Moments.End);
				}                
			}
		}

		/// <summary>
		/// Puts the character on the ladder
		/// </summary>
		protected virtual void StartClimbing()
		{
			if (_ladderTopStopLock)
			{
				return;
			}
			_waitingAtLadderPlatform = false;

			if (CurrentLadder.LadderPlatform != null)
			{
				if (AboveLadderPlatform()
				    && (LowestLadder == HighestLadder)) 
				{
					return;
				}
			}

			// we rotate our character if requested
			if (ForceRightFacing)
			{
				_character.Face(Character.FacingDirections.Right);
			}

			SetClimbingState();

			// we set collisions
			_controller.CollisionsOn();

			if ((_characterHandleWeapon != null) && (!_characterHandleWeapon.CanShootFromLadders))
			{
				_characterHandleWeapon.ForceStop();
			}

			if (CurrentLadder.CenterCharacterOnLadder)
			{
				MoveTowardsLadderX(CurrentLadder, false);
			}
		}

		/// <summary>
		/// Puts the character on the ladder if it's standing on top of it
		/// </summary>
		protected virtual void StartClimbingDown()
		{
			_ladderTopStopLock = false;
			_waitingAtLadderPlatform = false;

			SetClimbingState();
			_controller.CollisionsOff();
			_controller.ResetColliderSize ();
			_controller.GravityActive(false);

			// we rotate our character if requested
			if (ForceRightFacing)
			{
				_character.Face(Character.FacingDirections.Right);
			}

			if (_characterGravity != null)
			{
				if (_characterGravity.ShouldReverseInput ())
				{
					if (LowestLadder.CenterCharacterOnLadder)
					{
						MoveTowardsLadderX(LowestLadder, true, _controller.transform.position.y + _climbingDownInitialYTranslation);
					}
					else
					{
						_controller.SetTransformPosition(new Vector3(transform.position.x, _controller.transform.position.y + _climbingDownInitialYTranslation, _controller.transform.position.z));
					}
					return;
				}
			}

			// we force its position to be a bit lower 
			if (LowestLadder.CenterCharacterOnLadder)
			{
				MoveTowardsLadderX(LowestLadder, true, _controller.transform.position.y - _climbingDownInitialYTranslation);
			}
			else
			{
				_controller.SetTransformPosition(new Vector3(transform.position.x, _controller.transform.position.y - _climbingDownInitialYTranslation, _controller.transform.position.z));
			}
		}

		/// <summary>
		/// Sets the various flags and states 
		/// </summary>
		protected virtual void SetClimbingState()
		{

			// we set its state to LadderClimbing
			_movement.ChangeState(CharacterStates.MovementStates.LadderClimbing);			
			// it can't move freely anymore
			_condition.ChangeState(CharacterStates.CharacterConditions.ControlledMovement);
			// we initialize the ladder climbing speed to zero
			CurrentLadderClimbingSpeed = Vector2.zero;
			// we make sure the controller won't move
			_controller.SetHorizontalForce(0);
			_controller.SetVerticalForce(0);
			// we disable the gravity
			_controller.GravityActive(false);

		}

		/// <summary>
		/// Handles movement on the ladder
		/// </summary>
		protected virtual void Climbing()
		{
			BlockNormalJumpAbility();
			if (HandleLadderJumpBoostInput())
			{
				return;
			}

			// we disable the gravity
			_controller.GravityActive(false);

			if (CurrentLadder.LadderPlatform != null)
			{
				if (!AboveLadderPlatform())
				{
					_controller.CollisionsOn();
				}
			}
			else
			{
				_controller.CollisionsOn();
			}				
			
			// we set the force according to the ladder climbing speed
			if (CurrentLadder.LadderType == Ladder.LadderTypes.Simple)
			{
				float verticalSpeed = GetLadderVerticalSpeed();
				_controller.SetVerticalForce(verticalSpeed);
				// we set the climbing speed state.
				CurrentLadderClimbingSpeed = Mathf.Abs(verticalSpeed) > 0.0001f ? transform.up : Vector2.zero;	
			}
			if (CurrentLadder.LadderType == Ladder.LadderTypes.BiDirectional)
			{
				float verticalSpeed = GetLadderVerticalSpeed();
				_controller.SetHorizontalForce(_horizontalInput * LadderClimbingSpeed);
				_controller.SetVerticalForce(verticalSpeed);
				CurrentLadderClimbingSpeed = Mathf.Abs(_horizontalInput ) * transform.right;	
				CurrentLadderClimbingSpeed += (Mathf.Abs(verticalSpeed) > 0.0001f ? 1f : 0f) * (Vector2)transform.up;	
			}

			if (CurrentLadder.CenterCharacterOnLadder)
			{
				MoveTowardsLadderX(CurrentLadder, false);
			}

			if (CurrentLadderClimbingSpeed.sqrMagnitude > 0.0001f)
			{
				_lastNonZeroLadderClimbingSpeed = CurrentLadderClimbingSpeed;
				RestoreLadderAnimatorSpeed();
			}
			else if (FreezeLastLadderFrameWhenStopped
			         && (_lastNonZeroLadderClimbingSpeed.sqrMagnitude > 0.0001f))
			{
				FreezeLadderAnimator();
			}

		}

		/// <summary>
		/// Returns the current ladder vertical speed, including a short jump-button boost when requested.
		/// </summary>
		protected virtual float GetLadderVerticalSpeed()
		{
			float verticalSpeed = _verticalInput * LadderClimbingSpeed;
			if (Time.time < _ladderJumpBoostUntil)
			{
				verticalSpeed = Mathf.Max(verticalSpeed, LadderJumpBoostSpeed);
			}
			return verticalSpeed;
		}

		/// <summary>
		/// Starts a short ladder boost when the jump button is pressed while climbing.
		/// </summary>
		protected virtual bool HandleLadderJumpBoostInput()
		{
			if (!AllowLadderJumpBoost || (_inputManager == null))
			{
				return false;
			}

			if (_inputManager.JumpButton.State.CurrentState != MMInput.ButtonStates.ButtonDown)
			{
				return false;
			}

			if (DropFromLadderWhenJumpingDown && (_verticalInput < -_inputManager.Threshold.y))
			{
				GetOffTheLadder();
				_controller.SetHorizontalForce(0f);
				_controller.SetVerticalForce(0f);
				_ladderJumpBoostUntil = 0f;
				return true;
			}

			if (Time.time < _lastLadderJumpBoostAt + LadderJumpBoostCooldown)
			{
				return false;
			}

			_lastLadderJumpBoostAt = Time.time;
			_ladderJumpBoostUntil = Time.time + LadderJumpBoostDuration;
			RestoreLadderAnimatorSpeed();
			return false;
		}

		/// <summary>
		/// Stops ladder movement as soon as the top platform is reached.
		/// </summary>
		protected virtual void StopAtTopPlatform()
		{
			_ladderTopStopLock = true;
			_waitingAtLadderPlatform = true;
			RestoreNormalJumpAbility();
			RestoreLadderAnimatorSpeed();
			_controller.GravityActive(false);
			_controller.CollisionsOn();
			_controller.SetHorizontalForce(0f);
			_controller.SetVerticalForce(0f);
			CurrentLadderClimbingSpeed = Vector2.zero;
			_ladderJumpBoostUntil = 0f;
		}

		/// <summary>
		/// Keeps the character stopped at the top of the ladder until the player manually jumps or climbs down.
		/// </summary>
		protected virtual void HandleTopPlatformWait()
		{
			RestoreNormalJumpAbility();
			_controller.GravityActive(false);
			_controller.CollisionsOn();
			_controller.SetHorizontalForce(0f);
			_controller.SetVerticalForce(0f);
			CurrentLadderClimbingSpeed = Vector2.zero;

			if (_inputManager == null)
			{
				return;
			}

			if (_verticalInput < -_inputManager.Threshold.y)
			{
				_waitingAtLadderPlatform = false;
				_ladderTopStopLock = false;
				StartClimbingDown();
				return;
			}

			if (_inputManager.JumpButton.State.CurrentState == MMInput.ButtonStates.ButtonDown)
			{
				_waitingAtLadderPlatform = false;
				_ladderTopStopLock = true;
				GetOffTheLadder();
				_controller.SetVerticalForce(LadderTopManualJumpForce);
				_movement.ChangeState(CharacterStates.MovementStates.Jumping);
			}
		}

		/// <summary>
		/// Prevents the normal jump ability from firing while Space is used as a ladder boost.
		/// </summary>
		protected virtual void BlockNormalJumpAbility()
		{
			if (!BlockNormalJumpWhileClimbing || (_characterJump == null) || _jumpAbilityWasBlocked)
			{
				return;
			}

			_storedJumpAbilityPermitted = _characterJump.AbilityPermitted;
			_characterJump.AbilityPermitted = false;
			_jumpAbilityWasBlocked = true;
		}

		/// <summary>
		/// Restores the normal jump ability after leaving the ladder.
		/// </summary>
		protected virtual void RestoreNormalJumpAbility()
		{
			if ((_characterJump == null) || !_jumpAbilityWasBlocked)
			{
				return;
			}

			_characterJump.AbilityPermitted = _storedJumpAbilityPermitted;
			_jumpAbilityWasBlocked = false;
		}

		/// <summary>
		/// Keeps the character from re-grabbing the ladder while standing in the top/platform area.
		/// </summary>
		protected virtual void UpdateLadderTopStopLock()
		{
			if (!_ladderTopStopLock)
			{
				return;
			}

			if ((_inputManager != null) && (_verticalInput < -_inputManager.Threshold.y))
			{
				_ladderTopStopLock = false;
				_waitingAtLadderPlatform = false;
				return;
			}

			if ((HighestLadder == null) || (HighestLadder.LadderPlatform == null) || !ShouldStopAtLadderPlatform(HighestLadder))
			{
				_ladderTopStopLock = false;
				_waitingAtLadderPlatform = false;
			}
		}

		/// <summary>
		/// Moves the character towards the configured ladder x position.
		/// </summary>
		protected virtual void MoveTowardsLadderX(Ladder ladder, bool forceInstant, float? forcedY = null)
		{
			if (ladder == null)
			{
				return;
			}

			float targetX = GetLadderPlayerCenterX(ladder);
			float targetY = forcedY ?? _controller.transform.position.y;
			float currentX = _controller.transform.position.x;
			float nextX = targetX;

			if (!forceInstant && SmoothCenterCharacterOnLadder && (LadderCenteringSpeed > 0f))
			{
				nextX = Mathf.MoveTowards(currentX, targetX, LadderCenteringSpeed * Time.deltaTime);
			}

			_controller.SetTransformPosition(new Vector3(nextX, targetY, _controller.transform.position.z));
		}

		/// <summary>
		/// Returns the x position the character should center on for the specified ladder.
		/// </summary>
		protected virtual float GetLadderPlayerCenterX(Ladder ladder)
		{
			if (ladder == null)
			{
				return _controller.transform.position.x;
			}

			Collider2D ladderCollider = ladder.GetComponent<Collider2D>();
			float ladderCenterX = (ladderCollider != null) ? ladderCollider.bounds.center.x : ladder.transform.position.x;
			return ladderCenterX;
		}

		/// <summary>
		/// Freezes the animator to keep the current ladder climb frame visible.
		/// </summary>
		protected virtual void FreezeLadderAnimator()
		{
			if ((_animator == null) || _ladderAnimatorFrozen)
			{
				return;
			}

			_storedLadderAnimatorSpeed = _animator.speed;
			_animator.speed = 0f;
			_ladderAnimatorFrozen = true;
		}

		/// <summary>
		/// Restores the animator speed after a ladder idle freeze.
		/// </summary>
		protected virtual void RestoreLadderAnimatorSpeed()
		{
			if ((_animator == null) || !_ladderAnimatorFrozen)
			{
				return;
			}

			_animator.speed = _storedLadderAnimatorSpeed;
			_ladderAnimatorFrozen = false;
		}

		/// <summary>
		/// Resets various states so that the Character isn't climbing anymore
		/// </summary>
		public virtual void GetOffTheLadder()
		{
			RestoreNormalJumpAbility();
			RestoreLadderAnimatorSpeed();
			// we make it stop climbing, it has reached the ground.
			_condition.ChangeState(CharacterStates.CharacterConditions.Normal);
			_movement.ChangeState(CharacterStates.MovementStates.Idle);
			CurrentLadderClimbingSpeed = Vector2.zero;	
			_lastNonZeroLadderClimbingSpeed = Vector2.zero;
			_controller.GravityActive(true);	
			_controller.CollisionsOn();
			PlayAbilityStopFeedbacks();
			MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.Ladder, MMCharacterEvent.Moments.End);
			if (_characterHorizontalMovement != null)
			{
				_characterHorizontalMovement.ResetHorizontalSpeed();
			}			
		}

		/// <summary>
		/// Determines if the player is above the ladder's platform (usually positioned near the top)
		/// </summary>
		/// <returns><c>true</c>, if the player is above the ladder's platform, <c>false</c> otherwise.</returns>
		protected virtual bool AboveLadderPlatform()
		{
			return AboveLadderPlatform(LowestLadder);
		}

		/// <summary>
		/// Determines if the player is above the specified ladder's platform.
		/// </summary>
		protected virtual bool AboveLadderPlatform(Ladder ladder)
		{
			// we make sure we have a current ladder and that it has a ladder platform associated to it
			if (ladder == null)
			{
				return false;
			}
			if (ladder.LadderPlatform == null)
			{
				return false;
			}

			float ladderColliderY = 0;

			if (ladder.LadderPlatformBoxCollider2D != null)
			{
				ladderColliderY = ladder.LadderPlatformBoxCollider2D.bounds.center.y + ladder.LadderPlatformBoxCollider2D.bounds.extents.y ;
			}
			if (ladder.LadderPlatformEdgeCollider2D != null)
			{
				ladderColliderY = ladder.LadderPlatform.transform.position.y 
				                  + ladder.LadderPlatformEdgeCollider2D.offset.y ;
			}

			bool conditionAboveLadderPlatform = (ladderColliderY < _controller.ColliderBottomPosition.y + _ladderTopSkinHeight);

			if (_characterGravity != null)
			{
				if (_characterGravity.ShouldReverseInput())
				{
					if (ladder.LadderPlatformBoxCollider2D != null)
					{
						ladderColliderY = ladder.LadderPlatformBoxCollider2D.bounds.center.y - ladder.LadderPlatformBoxCollider2D.bounds.extents.y ;
					}

					if (ladder.LadderPlatformEdgeCollider2D != null)
					{
						ladderColliderY = ladder.LadderPlatform.transform.position.y 
						                  - ladder.LadderPlatformEdgeCollider2D.offset.y ;
					}
					conditionAboveLadderPlatform = (ladderColliderY > _controller.ColliderTopPosition.y - _ladderTopSkinHeight);
				}	
			}

			// if the bottom of the player's collider is above the ladder platform, we return true
			if (conditionAboveLadderPlatform)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// When the character dies, we make sure it gets off the ladder first
		/// </summary>
		protected override void OnDeath()
		{
			base.OnDeath();
			GetOffTheLadder();
		}
        
		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter (_ladderClimbingUpAnimationParameterName, AnimatorControllerParameterType.Bool, out _ladderClimbingUpAnimationParameter);
			RegisterAnimatorParameter (_ladderClimbingSpeedXAnimationParameterName, AnimatorControllerParameterType.Float, out _ladderClimbingSpeedXAnimationParameter);
			RegisterAnimatorParameter (_ladderClimbingSpeedYpAnimationParameterName, AnimatorControllerParameterType.Float, out _ladderClimbingSpeedYAnimationParameter);
		}

		/// <summary>
		/// At the end of each cycle, we update our animator with our various states
		/// </summary>
		public override void UpdateAnimator()
		{
			Vector2 animatorLadderClimbingSpeed = CurrentLadderClimbingSpeed;
			if (HoldLastClimbAnimationWhenStopped
			    && (_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing)
			    && (animatorLadderClimbingSpeed.sqrMagnitude <= 0.0001f)
			    && (_lastNonZeroLadderClimbingSpeed.sqrMagnitude > 0.0001f))
			{
				animatorLadderClimbingSpeed = _lastNonZeroLadderClimbingSpeed;
			}

			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ladderClimbingUpAnimationParameter, (_movement.CurrentState == CharacterStates.MovementStates.LadderClimbing), _character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _ladderClimbingSpeedXAnimationParameter, animatorLadderClimbingSpeed.x,_character._animatorParameters, _character.PerformAnimatorSanityChecks);
			MMAnimatorExtensions.UpdateAnimatorFloat(_animator, _ladderClimbingSpeedYAnimationParameter, animatorLadderClimbingSpeed.y,_character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}

		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			RestoreLadderAnimatorSpeed();
			if ( (_condition.CurrentState == CharacterStates.CharacterConditions.Normal)
			     || (_condition.CurrentState == CharacterStates.CharacterConditions.ControlledMovement) )
			{
				GetOffTheLadder();	
			}
			Initialization();

			if (_animator != null)
			{
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _ladderClimbingUpAnimationParameter, false, _character._animatorParameters);    
			}
		}

		/// <summary>
		/// Returns true only when the character has reached the physical top end of the specified ladder collider.
		/// </summary>
		protected virtual bool ReachedLadderTop(Ladder ladder)
		{
			if (ladder == null)
			{
				return false;
			}

			Collider2D ladderCollider = ladder.GetComponent<Collider2D>();
			if (ladderCollider == null)
			{
				return false;
			}

			if (_characterGravity != null && _characterGravity.ShouldReverseInput())
			{
				return _controller.ColliderTopPosition.y <= ladderCollider.bounds.min.y + _ladderTopSkinHeight;
			}

			return _controller.ColliderBottomPosition.y >= ladderCollider.bounds.max.y - _ladderTopSkinHeight;
		}

		/// <summary>
		/// Returns true when the character is close enough to the top ladder/platform area to stop climbing.
		/// </summary>
		protected virtual bool ShouldStopAtLadderPlatform(Ladder ladder)
		{
			if (ladder == null)
			{
				return false;
			}

			Collider2D ladderCollider = ladder.GetComponent<Collider2D>();
			if (ladderCollider == null)
			{
				return AboveLadderPlatform(ladder);
			}

			if (_characterGravity != null && _characterGravity.ShouldReverseInput())
			{
				return _controller.ColliderTopPosition.y <= ladderCollider.bounds.min.y + LadderPlatformStopDistance;
			}

			return _controller.ColliderBottomPosition.y >= ladderCollider.bounds.max.y - LadderPlatformStopDistance;
		}

	}
}
