using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

namespace MoreMountains.CorgiEngine
{
	/// <summary>
	/// Gives health to the player who collects it
	/// </summary>
	[AddComponentMenu("Corgi Engine/Items/Stimpack")]
	public class Stimpack : PickableItem
	{
		/// the amount of health to give the player when collected
		[Tooltip("the amount of health to give the player when collected")]
		public float HealthToGive = 10f;

		/// <summary>
		/// What happens when the object gets picked
		/// </summary>
		protected override void Pick(GameObject picker)
		{
			if (name.Contains("RetroStimpackPicker"))
			{
				if (TryAddAkerBlood(1))
				{
					return;
				}

				Debug.LogWarning("RetroStimpackPicker was picked but no PlayerUpgradeManager was found.", this);
				return;
			}

			Health characterHealth = _pickingCollider.GetComponent<Health>();
			// else, we give health to the player
			characterHealth.GetHealth(HealthToGive,gameObject);
		}

		protected virtual bool TryAddAkerBlood(int amount)
		{
			Type managerType = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				managerType = assemblies[i].GetType("PlayerUpgradeManager");
				if (managerType != null)
				{
					break;
				}
			}

			if (managerType == null)
			{
				return false;
			}

			PropertyInfo instanceProperty = managerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
			if (instanceProperty == null)
			{
				return false;
			}

			object instance = instanceProperty.GetValue(null, null);
			if (instance == null)
			{
				return false;
			}

			MethodInfo addAkerBloodMethod = managerType.GetMethod("AddAkerBlood", BindingFlags.Public | BindingFlags.Instance);
			if (addAkerBloodMethod == null)
			{
				return false;
			}

			addAkerBloodMethod.Invoke(instance, new object[] { amount });
			return true;
		}
	}
}
