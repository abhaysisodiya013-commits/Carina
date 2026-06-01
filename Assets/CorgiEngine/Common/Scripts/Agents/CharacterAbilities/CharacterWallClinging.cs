using UnityEngine;
using System.Collections;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{	
	/// <summary>
	/// Add this component to a Character and it'll be able to cling to walls when being in the air, 
	// facing a wall, and moving in its direction
	/// Animator parameters : WallClinging (bool)
	/// </summary>
	[AddComponentMenu("Corgi Engine/Character/Abilities/Character Wallclinging")] 
	public class CharacterWallClinging : CharacterAbility 
	{
		/// This method is only used to display a helpbox text at the beginning of the ability's inspector
		public override string HelpBoxText() { return "Add this component to your character and it'll be able to cling to walls, slowing down its fall. Here you can define the slow factor (close to 0 : super slow, 1 : normal fall) and the tolerance (to account for tiny holes in the wall."; }

		[Header("Wall Clinging")]

		/// the slow factor when wall clinging
		[Tooltip("the slow factor when wall clinging")]
		[Range(0.01f, 1)]
		public float WallClingingSlowFactor = 0.6f;
		/// the vertical offset to apply to raycasts for wall clinging
		[Tooltip("the vertical offset to apply to raycasts for wall clinging")]
		public float RaycastVerticalOffset = 0f;
		/// extra horizontal distance added to the wall cling probe origin, useful to manually line up cling contact with the wall
		[Tooltip("extra horizontal distance added to the wall cling probe origin, useful to manually line up cling contact with the wall")]
		public float HorizontalProbeOffset = 0f;
		/// extra horizontal distance added only when probing on the right side
		[Tooltip("extra horizontal distance added only when probing on the right side")]
		public float RightHorizontalProbeOffset = 0f;
		/// extra horizontal distance added only when probing on the left side
		[Tooltip("extra horizontal distance added only when probing on the left side")]
		public float LeftHorizontalProbeOffset = 0f;
		/// the tolerance applied to compensate for tiny irregularities in the wall (slightly misplaced tiles for example)
		[Tooltip("the tolerance applied to compensate for tiny irregularities in the wall (slightly misplaced tiles for example)")]
		public float WallClingingTolerance = 0.3f;
		/// the maximum raycast distance allowed for wall cling detection
		[Tooltip("the maximum raycast distance allowed for wall cling detection. Keeps the character from clinging while floating away from the wall.")]
		public float MaximumWallClingDistance = 0.2f;
		/// the minimum side-surface angle required to treat a collision as a vertical wall
		[Tooltip("the minimum side-surface angle required to treat a collision as a wall. 90 means perfectly vertical, lower values allow steeper slopes.")]
		[Range(0f, 90f)]
		public float MinimumWallAngleForCling = 80f;

		[Header("Automation")]

		/// if this is set to true, you won't need to press the opposite direction to wall cling, it'll be automatic anytime the character faces a wall
		[Tooltip("if this is set to true, you won't need to press the opposite direction to wall cling, it'll be automatic anytime the character faces a wall")]
		public bool InputIndependent = false;        

		[Header("Visuals")]

		/// if true, the ability will invert the character model's sprite renderer while wall clinging
		[Tooltip("if true, the ability will invert the character model's sprite renderer while wall clinging. Useful when the wall cling sprites are authored facing the opposite direction from the rest of the moveset.")]
		public bool InvertSpriteRendererDuringWallCling = false;
		/// horizontal visual offset applied to the character model while wall clinging
		[Tooltip("horizontal visual offset applied to the character model while wall clinging. Increase this to pull the sprite out of the wall until the leg touches the side.")]
		[Range(-2f, 2f)]
		public float WallClingVisualGap = 0.36f;
		/// vertical visual offset applied to the character model while wall clinging
		[Tooltip("vertical visual offset applied to the character model while wall clinging.")]
		[Range(-2f, 2f)]
		public float WallClingVisualVerticalOffset = 0f;

		[Header("Debug")]

		/// if true, only the wall cling raycasts will draw gizmos for this ability
		[Tooltip("if true, only the wall cling raycasts will draw gizmos for this ability")]
		public bool DrawWallClingingGizmos = false;

		protected CharacterStates.MovementStates _stateLastFrame;
		protected RaycastHit2D _raycast;
		protected WallClingingOverride _wallClingingOverride;
		protected SpriteRenderer _wallClingingSpriteRenderer;
		protected bool _wallClingingFlipApplied;
		protected bool _wallClingingInitialFlipX;
		protected bool _lastWallClingRaycastTestedRight;
		protected Transform _wallClingingModelTransform;
		protected Vector3 _wallClingingModelBaseLocalPosition;
		protected bool _wallClingingVisualOffsetApplied;

		// animation parameters
		protected const string _wallClingingAnimationParameterName = "WallClinging";
		protected int _wallClingingAnimationParameter;

		/// <summary>
		/// Checks the input to see if we should enter the WallClinging state
		/// </summary>
		protected override void HandleInput()
		{
			WallClinging();
		}

		/// <summary>
		/// Every frame, checks if the wallclinging state should be exited
		/// </summary>
		public override void ProcessAbility()
		{
			base.ProcessAbility();

			if (_movement.CurrentState == CharacterStates.MovementStates.WallClinging)
			{
				FaceCurrentWall();
			}

			ExitWallClinging();
			WallClingingLastFrame ();
		}

		/// <summary>
		/// Caches the sprite renderer we may need to invert during wall cling.
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();

			if ((_character != null) && (_character.CharacterModel != null))
			{
				_wallClingingModelTransform = _character.CharacterModel.transform;
				_wallClingingModelBaseLocalPosition = _wallClingingModelTransform.localPosition;
				_wallClingingSpriteRenderer = _character.CharacterModel.GetComponent<SpriteRenderer>();
				if (_wallClingingSpriteRenderer == null)
				{
					_wallClingingSpriteRenderer = _character.CharacterModel.GetComponentInChildren<SpriteRenderer>();
				}
			}

			if (_wallClingingSpriteRenderer == null)
			{
				_wallClingingSpriteRenderer = _spriteRenderer;
			}
		}

		/// <summary>
		/// Makes the player stick to a wall when jumping
		/// </summary>
		protected virtual void WallClinging()
		{
			if (!AbilityAuthorized
			    || (_condition.CurrentState != CharacterStates.CharacterConditions.Normal)
			    || (_controller.State.IsGrounded)
			    || (_controller.State.ColliderResized)
			    || (_controller.Speed.y >= 0) )
			{
				return;
			}
            
			if (InputIndependent)
			{
				if (TestForWall())
				{
					EnterWallClinging();
				}
			}
			else
			{
				if (IsValidWallFromController()
				    && (((_controller.State.IsCollidingLeft) && (_horizontalInput <= -_inputManager.Threshold.x))
				    || ((_controller.State.IsCollidingRight) && (_horizontalInput >= _inputManager.Threshold.x))))
				{
					EnterWallClinging();
				}
			}            
		}

		/// <summary>
		/// Casts a ray to check if we're facing a wall
		/// </summary>
		/// <returns></returns>
		protected virtual bool TestForWall()
		{
			bool testRightSide = ShouldTestRightWall();

			if (TryWallClingRaycast(testRightSide, out _raycast))
			{
				_lastWallClingRaycastTestedRight = testRightSide;
				return true;
			}

			if (InputIndependent && TryWallClingRaycast(!testRightSide, out _raycast))
			{
				_lastWallClingRaycastTestedRight = !testRightSide;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Casts the wall cling probe on one side of the character.
		/// </summary>
		protected virtual bool TryWallClingRaycast(bool testRightSide, out RaycastHit2D hit)
		{
			Vector3 raycastOrigin = _characterTransform.position;
			Vector3 raycastDirection;
			float verticalOffset = RaycastVerticalOffset;
			float sideOffset = testRightSide ? RightHorizontalProbeOffset : LeftHorizontalProbeOffset;
			float horizontalProbeOffset = _controller.Width() / 2f + HorizontalProbeOffset + sideOffset;
			float probeDistance = Mathf.Min(WallClingingTolerance, MaximumWallClingDistance);
			if (testRightSide)
			{
				raycastOrigin = raycastOrigin + _characterTransform.right * horizontalProbeOffset + _characterTransform.up * verticalOffset;
				raycastDirection = _characterTransform.right - _characterTransform.up;
			}
			else
			{
				raycastOrigin = raycastOrigin - _characterTransform.right * horizontalProbeOffset + _characterTransform.up * verticalOffset;
				raycastDirection = -_characterTransform.right - _characterTransform.up;
			}

			// we cast a straight side ray first, then keep the original diagonal ray as a tolerance fallback
			Vector3 horizontalRaycastDirection = testRightSide ? _characterTransform.right : -_characterTransform.right;
			LayerMask wallClingMask = _controller.PlatformMask & ~(_controller.OneWayPlatformMask | _controller.MovingOneWayPlatformMask);
			hit = MMDebug.RayCast(raycastOrigin, horizontalRaycastDirection, probeDistance, wallClingMask, Color.black, DrawWallClingingGizmos);
			if (!IsValidWallHit(hit))
			{
				hit = MMDebug.RayCast(raycastOrigin, raycastDirection, probeDistance, wallClingMask, Color.black, DrawWallClingingGizmos);
			}

			// we check if the ray hit anything and if the hit is close enough to a vertical wall
			return IsValidWallHit(hit);
		}

		/// <summary>
		/// Picks the actual wall side for input independent clinging, falling back to facing direction.
		/// </summary>
		protected virtual bool ShouldTestRightWall()
		{
			if (InputIndependent)
			{
				if (_horizontalInput >= _inputManager.Threshold.x)
				{
					return true;
				}

				if (_horizontalInput <= -_inputManager.Threshold.x)
				{
					return false;
				}

				if (_controller.State.IsCollidingRight && !_controller.State.IsCollidingLeft)
				{
					return true;
				}

				if (_controller.State.IsCollidingLeft && !_controller.State.IsCollidingRight)
				{
					return false;
				}
			}

			return _character.IsFacingRight;
		}

		/// <summary>
		/// Enters the wall clinging state
		/// </summary>
		protected virtual void EnterWallClinging()
		{
			FaceCurrentWall();

			// we check for an override
			if (_controller.CurrentWallCollider != null)
			{
				_wallClingingOverride = _controller.CurrentWallCollider.gameObject.MMGetComponentNoAlloc<WallClingingOverride>();
			}
			else if (_raycast.collider != null)
			{
				_wallClingingOverride = _raycast.collider.gameObject.MMGetComponentNoAlloc<WallClingingOverride>();
			}
            
			if (_wallClingingOverride != null)
			{
				// if we can't wallcling to this wall, we do nothing and exit
				if (!_wallClingingOverride.CanWallClingToThis)
				{
					return;
				}
				_controller.SlowFall(_wallClingingOverride.WallClingingSlowFactor);
			}
			else
			{
				// we slow the controller's fall speed
				_controller.SlowFall(WallClingingSlowFactor);
			}

			// if we weren't wallclinging before this frame, we start our sounds
			if ((_movement.CurrentState != CharacterStates.MovementStates.WallClinging) && !_startFeedbackIsPlaying)
			{
				// we start our feedbacks
				PlayAbilityStartFeedbacks();
				MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.WallCling, MMCharacterEvent.Moments.Start);
			}

			_movement.ChangeState(CharacterStates.MovementStates.WallClinging);
		}

		/// <summary>
		/// Ensures the character faces the wall it is currently clinging to.
		/// </summary>
		protected virtual void FaceCurrentWall()
		{
			if (_raycast.collider != null)
			{
				_character.Face(_lastWallClingRaycastTestedRight ? Character.FacingDirections.Right : Character.FacingDirections.Left);
				return;
			}

			if (_controller.State.IsCollidingLeft && !_controller.State.IsCollidingRight)
			{
				_character.Face(Character.FacingDirections.Left);
				return;
			}

			if (_controller.State.IsCollidingRight && !_controller.State.IsCollidingLeft)
			{
				_character.Face(Character.FacingDirections.Right);
			}
		}

		/// <summary>
		/// If the character is currently wallclinging, checks if we should exit the state
		/// </summary>
		protected virtual void ExitWallClinging()
		{
			if (_movement.CurrentState == CharacterStates.MovementStates.WallClinging)
			{
				// we prepare a boolean to store our exit condition value
				bool shouldExit = false;
				if ((_controller.State.IsGrounded) // if the character is grounded
				    || (_controller.Speed.y >= 0))  // or if it's moving up
				{
					// then we should exit
					shouldExit = true;
				}

				// we check if the ray hit anything. If it didn't, or if we're not moving in the direction of the wall, we exit
				if (!InputIndependent)
				{
					// we cast our ray 
					RaycastHit2D hit;
					bool validWall = TryWallClingRaycast(_character.IsFacingRight, out hit);
                    
					if (_character.IsFacingRight)
					{
						if ((!validWall) || (_horizontalInput <= _inputManager.Threshold.x))
						{
							shouldExit = true;
						}
					}
					else
					{
						if ((!validWall) || (_horizontalInput >= -_inputManager.Threshold.x))
						{
							shouldExit = true;
						}
					}
				}
				else
				{
					if (!TryWallClingRaycast(_lastWallClingRaycastTestedRight, out _raycast))
					{
						shouldExit = true;
					}
				}
				
				if (shouldExit)
				{
					ProcessExit();
				}
			}

			if ((_stateLastFrame == CharacterStates.MovementStates.WallClinging) 
			    && (_movement.CurrentState != CharacterStates.MovementStates.WallClinging)
			    && _startFeedbackIsPlaying)
			{
				// we play our exit feedbacks
				StopStartFeedbacks();
				PlayAbilityStopFeedbacks();
				MMCharacterEvent.Trigger(_character, MMCharacterEventTypes.WallCling, MMCharacterEvent.Moments.End);
			}

			_stateLastFrame = _movement.CurrentState;
		}

		protected virtual void ProcessExit()
		{
			RestoreWallClingVisualOffset();
			RestoreWallClingSpriteFlip();

			// if we're not wallclinging anymore, we reset the slowFall factor, and reset our state.
			_controller.SlowFall(0f);
			// we reset the state
			_movement.ChangeState(CharacterStates.MovementStates.Idle);
		}

		/// <summary>
		/// This methods tests if we were wallcling previously, and if so, resets the slowfall factor and stops the wallclinging sound
		/// </summary>
		protected virtual void WallClingingLastFrame()
		{
			if ((_movement.PreviousState == CharacterStates.MovementStates.WallClinging) 
			    && (_movement.CurrentState != CharacterStates.MovementStates.WallClinging)
			    && _startFeedbackIsPlaying)
			{
				_controller.SlowFall (0f);	
				StopStartFeedbacks();
			}
		}

		/// <summary>
		/// Applies the wall cling sprite inversion after the normal ability processing.
		/// </summary>
		public override void LateProcessAbility()
		{
			base.LateProcessAbility();

			if (_movement.CurrentState == CharacterStates.MovementStates.WallClinging)
			{
				ApplyWallClingVisualOffset();
				if (InvertSpriteRendererDuringWallCling)
				{
					ApplyWallClingSpriteFlip();
				}
			}
			else
			{
				RestoreWallClingVisualOffset();
				RestoreWallClingSpriteFlip();
			}
		}

		/// <summary>
		/// Applies the model-only wall cling offset so the sprite can be lined up without changing collisions.
		/// </summary>
		protected virtual void ApplyWallClingVisualOffset()
		{
			if (_wallClingingModelTransform == null)
			{
				return;
			}

			float awayFromWall = _character.IsFacingRight ? -1f : 1f;
			_wallClingingModelTransform.localPosition = _wallClingingModelBaseLocalPosition + new Vector3(WallClingVisualGap * awayFromWall, WallClingVisualVerticalOffset, 0f);
			_wallClingingVisualOffsetApplied = true;
		}

		/// <summary>
		/// Restores the model position after wall clinging.
		/// </summary>
		protected virtual void RestoreWallClingVisualOffset()
		{
			if ((_wallClingingModelTransform == null) || !_wallClingingVisualOffsetApplied)
			{
				return;
			}

			_wallClingingModelTransform.localPosition = _wallClingingModelBaseLocalPosition;
			_wallClingingVisualOffsetApplied = false;
		}

		/// <summary>
		/// Inverts the sprite renderer used by the wall cling animation.
		/// </summary>
		protected virtual void ApplyWallClingSpriteFlip()
		{
			if (_wallClingingSpriteRenderer == null)
			{
				return;
			}

			if (!_wallClingingFlipApplied)
			{
				_wallClingingInitialFlipX = _wallClingingSpriteRenderer.flipX;
				_wallClingingFlipApplied = true;
			}

			_wallClingingSpriteRenderer.flipX = !_wallClingingInitialFlipX;
		}

		/// <summary>
		/// Restores the sprite renderer's original flip once wall clinging ends.
		/// </summary>
		protected virtual void RestoreWallClingSpriteFlip()
		{
			if ((_wallClingingSpriteRenderer == null) || !_wallClingingFlipApplied)
			{
				return;
			}

			_wallClingingSpriteRenderer.flipX = _wallClingingInitialFlipX;
			_wallClingingFlipApplied = false;
		}

		/// <summary>
		/// Returns true if the controller's current side collision is steep enough to be treated as a vertical wall.
		/// </summary>
		protected virtual bool IsValidWallFromController()
		{
			return Mathf.Abs(_controller.State.LateralSlopeAngle) >= MinimumWallAngleForCling;
		}

		/// <summary>
		/// Returns true if the provided hit is steep enough to count as a wall cling surface.
		/// </summary>
		protected virtual bool IsValidWallHit(RaycastHit2D hit)
		{
			if (!hit)
			{
				return false;
			}

			float hitAngle = Vector2.Angle(hit.normal, Vector2.up);
			return (hitAngle >= MinimumWallAngleForCling) && (hitAngle <= 180f - MinimumWallAngleForCling);
		}
        
		protected override void OnDeath()
		{
			base.OnDeath();
			RestoreWallClingVisualOffset();
			ProcessExit();
		}

		/// <summary>
		/// Adds required animator parameters to the animator parameters list if they exist
		/// </summary>
		protected override void InitializeAnimatorParameters()
		{
			RegisterAnimatorParameter (_wallClingingAnimationParameterName, AnimatorControllerParameterType.Bool, out _wallClingingAnimationParameter);
		}

		/// <summary>
		/// Updates the animator with the current wallclinging state
		/// </summary>
		public override void UpdateAnimator()
		{
			MMAnimatorExtensions.UpdateAnimatorBool(_animator, _wallClingingAnimationParameter, (_movement.CurrentState == CharacterStates.MovementStates.WallClinging), _character._animatorParameters, _character.PerformAnimatorSanityChecks);
		}
		
		/// <summary>
		/// On reset ability, we cancel all the changes made
		/// </summary>
		public override void ResetAbility()
		{
			base.ResetAbility();
			if (_condition.CurrentState == CharacterStates.CharacterConditions.Normal)
			{
				ProcessExit();	
			}

			if (_animator != null)
			{
				MMAnimatorExtensions.UpdateAnimatorBool(_animator, _wallClingingAnimationParameter, false, _character._animatorParameters, _character.PerformAnimatorSanityChecks);	
			}
		}
	}
}
