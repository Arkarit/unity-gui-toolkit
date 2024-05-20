using System;
using System.Linq;
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
	}
}