using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleUtility
	{
		public static string GetName( Type _supportedMonoBehaviourType, string _name )
		{
			return $"{_supportedMonoBehaviourType.Name} Style: {_name}";
		}

		public static int GetKey( Type _supportedMonoBehaviourType, string _name )
		{
			return Animator.StringToHash(GetName(_supportedMonoBehaviourType, _name));
		}

		public static void SynchronizeApplicableness( UiAbstractStyleBase _from, UiAbstractStyleBase _to )
		{
			if (_from == null
				|| _to == null
				|| _from.GetType() != _to.GetType())
				return;

			var members = _from.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
			foreach (var member in members)
			{
				ApplicableValueBase fromVal = member.GetValue(_from) as ApplicableValueBase;
				ApplicableValueBase toVal = member.GetValue(_to) as ApplicableValueBase;
				if (fromVal == null || toVal == null)
					continue;

				toVal.IsApplicable = fromVal.IsApplicable;
			}
		}

		// There's an issue if you manually change component properties, which are also handled by a style.
		// Find an example in UiDistortGroup, which sets a distort modifier component enabled or disabled according to its needs.
		// As the complete distort modifier might be disabled by the skin/style, this might interfere.
		// Thus, UiDistortGroup calls ReApplyAppliers() after handling its changes to ensure the component is not enabled if forbidden by style.
		// This is better than reapplying the complete skin, but not completely lightweight, since components need to be collected, so handle with care.
		public static void ReApplyApplier(Component component)
		{
			var appliers = component.GetComponents<UiAbstractApplyStyleBase>();
			foreach (var applier in appliers)
			{
				if (!applier.enabled)
					continue;

				if (applier.Component == component)
				{
					applier.Apply();
					return;
				}
			}
		}

		public static void ReApplyAppliers<T>(IEnumerable<T> list) where T : Component
		{
			if (!AssetReadyGate.Ready)
				return;
			
			foreach (var elem in list)
				ReApplyApplier(elem);
		}
	}
}
