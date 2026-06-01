using System.Collections;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Door/edge trigger that fades, switches the active camera room bounds, then fades back in.
	/// </summary>
	[RequireComponent(typeof(Collider2D))]
	public class CameraRoomTransitionTrigger : MonoBehaviour
	{
		[Header("Target")]
		[Tooltip("The room bounds to activate when the player enters this trigger.")]
		public CameraRoomBounds TargetRoom;

		[Tooltip("Optional point to move the player to while the screen is black. Leave empty if this trigger should only switch bounds.")]
		public Transform PlayerExitPoint;

		[Header("Fade")]
		[Tooltip("If true, fades to black before switching bounds, then fades back in.")]
		public bool UseFade = true;

		[Tooltip("Fader ID used by the scene's MMFader.")]
		public int FaderID = 0;

		[Tooltip("Fade curve used for fade out and fade in.")]
		public MMTweenType FadeTween = new MMTweenType(MMTween.MMTweenCurve.EaseInOutCubic);

		[Tooltip("Time to fade to black.")]
		public float FadeOutDuration = 0.15f;

		[Tooltip("Extra time to stay black while switching bounds/player position.")]
		public float BlackScreenHoldDuration = 0.05f;

		[Tooltip("Time to fade back into gameplay.")]
		public float FadeInDuration = 0.15f;

		[Header("Player")]
		[Tooltip("Freezes the player during the fade transition.")]
		public bool FreezePlayerDuringTransition = true;

		[Tooltip("If true and PlayerExitPoint is assigned, moves the player while the screen is black.")]
		public bool MovePlayerToExitPoint = false;

		[Header("Camera")]
		[Tooltip("If true, teleports the camera to the player while the screen is black after switching bounds.")]
		public bool SnapCameraToPlayerDuringBlack = true;

		protected bool _transitionInProgress = false;

		protected virtual void Reset()
		{
			Collider2D trigger = GetComponent<Collider2D>();
			trigger.isTrigger = true;
		}

		protected virtual void OnTriggerEnter2D(Collider2D other)
		{
			if (_transitionInProgress || TargetRoom == null)
			{
				return;
			}

			Character character = other.gameObject.MMGetComponentNoAlloc<Character>();
			if (character == null)
			{
				character = other.GetComponentInParent<Character>();
			}

			if (character == null)
			{
				return;
			}

			StartCoroutine(TransitionCoroutine(character));
		}

		protected virtual IEnumerator TransitionCoroutine(Character character)
		{
			_transitionInProgress = true;

			if (FreezePlayerDuringTransition)
			{
				character.Freeze();
			}

			if (UseFade)
			{
				MMFadeInEvent.Trigger(FadeOutDuration, FadeTween, FaderID, false, character.transform.position);
				yield return new WaitForSecondsRealtime(FadeOutDuration);
			}

			TargetRoom.ApplyBounds();

			if (MovePlayerToExitPoint && PlayerExitPoint != null)
			{
				Vector3 newPosition = PlayerExitPoint.position;
				newPosition.z = character.transform.position.z;
				character.transform.position = newPosition;
			}

			if (SnapCameraToPlayerDuringBlack && LevelManager.HasInstance && LevelManager.Instance.LevelCameraController != null)
			{
				LevelManager.Instance.LevelCameraController.TeleportCameraToTarget();
				MMCameraEvent.Trigger(MMCameraEventTypes.StartFollowing);
			}

			if (BlackScreenHoldDuration > 0f)
			{
				yield return new WaitForSecondsRealtime(BlackScreenHoldDuration);
			}

			if (UseFade)
			{
				MMFadeOutEvent.Trigger(FadeInDuration, FadeTween, FaderID, false, character.transform.position);
				yield return new WaitForSecondsRealtime(FadeInDuration);
			}

			if (FreezePlayerDuringTransition)
			{
				character.UnFreeze();
			}

			_transitionInProgress = false;
		}
	}
}
