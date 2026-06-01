using UnityEditor;
using UnityEngine;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Scene view gizmos for CharacterDash custom airborne dash kick fields.
	/// </summary>
	public static class CharacterDashEditor
	{
		[DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
		private static void DrawAirborneDashKickGizmos(CharacterDash dash, GizmoType gizmoType)
		{
			if ((dash == null) || !dash.EnableAirborneDashKick)
			{
				return;
			}

			bool isSelected = (gizmoType & GizmoType.InSelectionHierarchy) != 0;
			if (dash.ShowAirborneDashKickGizmosOnlyWhenSelected && !isSelected)
			{
				return;
			}

			float direction = GetFacingDirection(dash);
			Color previousColor = Gizmos.color;
			Matrix4x4 previousMatrix = Gizmos.matrix;

			if (dash.ShowAirborneDashKickHitboxGizmo)
			{
				DrawHitboxGizmo(dash, direction);
				Gizmos.matrix = previousMatrix;
			}

			if (dash.ShowAirborneDashKickVfxGizmo)
			{
				Vector3 vfxCenter = dash.transform.position + new Vector3(
					dash.AirborneDashKickVfxOffset.x * direction,
					dash.AirborneDashKickVfxOffset.y,
					0f);

				Gizmos.color = dash.AirborneDashKickVfxGizmoColor;
				Gizmos.DrawWireCube(vfxCenter, new Vector3(
					dash.AirborneDashKickVfxSize.x,
					dash.AirborneDashKickVfxSize.y,
					0.01f));
			}

			Gizmos.color = previousColor;
			Gizmos.matrix = previousMatrix;
		}

		private static void DrawHitboxGizmo(CharacterDash dash, float direction)
		{
			BoxCollider2D actualHitbox = GetActualAirborneDashKickCollider(dash);
			Gizmos.color = dash.AirborneDashKickHitboxGizmoColor;

			if (actualHitbox != null)
			{
				Gizmos.matrix = actualHitbox.transform.localToWorldMatrix;
				Gizmos.DrawWireCube(actualHitbox.offset, new Vector3(actualHitbox.size.x, actualHitbox.size.y, 0.01f));
				return;
			}

			Vector3 hitboxCenter = dash.transform.position + new Vector3(
				dash.AirborneDashKickAreaOffset.x * direction,
				dash.AirborneDashKickAreaOffset.y,
				0f);

			Gizmos.DrawWireCube(hitboxCenter, new Vector3(
				dash.AirborneDashKickAreaSize.x * Mathf.Abs(dash.transform.lossyScale.x),
				dash.AirborneDashKickAreaSize.y * Mathf.Abs(dash.transform.lossyScale.y),
				0.01f));
		}

		private static BoxCollider2D GetActualAirborneDashKickCollider(CharacterDash dash)
		{
			Transform hitboxTransform = dash.transform.Find("AirborneDashKickHitbox");
			if (hitboxTransform == null)
			{
				return null;
			}

			return hitboxTransform.GetComponent<BoxCollider2D>();
		}

		private static float GetFacingDirection(CharacterDash dash)
		{
			Character character = dash.GetComponent<Character>();
			if (character != null)
			{
				return character.IsFacingRight ? 1f : -1f;
			}

			return dash.transform.lossyScale.x < 0f ? -1f : 1f;
		}
	}
}
