using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{

	[CustomEditor (typeof(CharacterAbility),true)]
	[CanEditMultipleObjects]

	/// <summary>
	/// Adds custom labels to the Character inspector
	/// </summary>

	public class CharacterAbilityInspector : Editor 
	{
		protected SerializedProperty _abilityStartFeedbacks;
		protected SerializedProperty _abilityStopFeedbacks;

		protected List<String> _propertiesToHide;
		protected bool _hasHiddenProperties = false;

		private void OnEnable()
		{
			_propertiesToHide = new List<string>();

			if (target == null)
			{
				return;
			}

			SerializedObject safeSerializedObject = null;
			try
			{
				safeSerializedObject = serializedObject;
			}
			catch (Exception)
			{
				return;
			}

			if (safeSerializedObject == null)
			{
				return;
			}

			_abilityStartFeedbacks = safeSerializedObject.FindProperty("AbilityStartFeedbacks");
			_abilityStopFeedbacks = safeSerializedObject.FindProperty("AbilityStopFeedbacks");

			MMHiddenPropertiesAttribute[] attributes = (MMHiddenPropertiesAttribute[])target.GetType().GetCustomAttributes(typeof(MMHiddenPropertiesAttribute), false);
			if (attributes != null)
			{
				if (attributes.Length != 0)
				{
					if (attributes[0].PropertiesNames != null)
					{
						_propertiesToHide = new List<String>(attributes[0].PropertiesNames);                        
						_hasHiddenProperties = true;
					}
				}                
			}
		}
        
		/// <summary>
		/// When inspecting a Character, adds to the regular inspector some labels, useful for debugging
		/// </summary>
		public override void OnInspectorGUI()
		{
			CharacterAbility t = (target as CharacterAbility);
			if (t == null)
			{
				return;
			}

			SerializedObject safeSerializedObject = null;
			try
			{
				safeSerializedObject = serializedObject;
			}
			catch (Exception)
			{
				return;
			}

			if (safeSerializedObject == null)
			{
				return;
			}

			safeSerializedObject.Update();
			EditorGUI.BeginChangeCheck();

			if (t.HelpBoxText() != "")
			{
				EditorGUILayout.HelpBox(t.HelpBoxText(),MessageType.Info);
			}

			Editor.DrawPropertiesExcluding(safeSerializedObject, new string[] { "AbilityStartFeedbacks", "AbilityStopFeedbacks" });

			EditorGUILayout.Space();
                        
			if (_propertiesToHide.Count > 0)
			{
				if (_propertiesToHide.Count < 2)
				{
					EditorGUILayout.LabelField("Feedbacks", EditorStyles.boldLabel);
				}                
				if (!_propertiesToHide.Contains("AbilityStartFeedbacks"))
				{
					if (_abilityStartFeedbacks != null)
					{
						EditorGUILayout.PropertyField(_abilityStartFeedbacks);
					}
				}
				if (!_propertiesToHide.Contains("AbilityStopFeedbacks"))
				{
					if (_abilityStopFeedbacks != null)
					{
						EditorGUILayout.PropertyField(_abilityStopFeedbacks);
					}
				}
			}
			else
			{
				EditorGUILayout.LabelField("Feedbacks", EditorStyles.boldLabel);
				if (_abilityStartFeedbacks != null)
				{
					EditorGUILayout.PropertyField(_abilityStartFeedbacks);
				}
				if (_abilityStopFeedbacks != null)
				{
					EditorGUILayout.PropertyField(_abilityStopFeedbacks);
				}
			}

			if (EditorGUI.EndChangeCheck())
			{
				safeSerializedObject.ApplyModifiedProperties();
			}                
		}	
	}
}
