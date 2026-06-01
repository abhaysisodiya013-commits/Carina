using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Defines one room/area camera bound for the existing Corgi/Cinemachine camera.
	/// </summary>
	[RequireComponent(typeof(Collider2D))]
	public class CameraRoomBounds : MonoBehaviour
	{
		[Header("Room")]
		[Tooltip("Optional name used only to make room setup easier to read in the Inspector.")]
		public string RoomName;

		[Tooltip("The collider used by Cinemachine Confiner for this room. If empty, the collider on this GameObject is used.")]
		public Collider2D BoundsCollider;

		[Tooltip("Legacy setting kept for scene compatibility. Room bounds are applied only by CameraRoomTransitionTrigger.")]
		[HideInInspector]
		public bool ActivateOnStartIfPlayerInside = false;

		[Header("Debug")]
		[Tooltip("True when this room is currently the active camera bounds room.")]
		[MMReadOnly]
		public bool IsActiveRoom = false;

		protected static CameraRoomBounds _activeRoom;

		protected virtual void Reset()
		{
			BoundsCollider = GetComponent<Collider2D>();
		}

		protected virtual void Awake()
		{
			if (BoundsCollider == null)
			{
				BoundsCollider = GetComponent<Collider2D>();
			}
		}

		public virtual void ApplyBounds()
		{
			if (BoundsCollider == null)
			{
				Debug.LogWarning($"{nameof(CameraRoomBounds)} on {name} has no bounds collider.", this);
				return;
			}

			if (_activeRoom != null && _activeRoom != this)
			{
				_activeRoom.IsActiveRoom = false;
			}
			_activeRoom = this;
			IsActiveRoom = true;
			CinemachineCameraController cameraController = FindObjectOfType<CinemachineCameraController>();
			if (cameraController != null)
			{
				cameraController.Set2DConfinerBounds(BoundsCollider);
			}

			MMCameraEvent.Trigger(MMCameraEventTypes.SetConfiner, null, null, BoundsCollider);
		}
	}
}
