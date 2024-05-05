using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleUtility
	{
		public static string GetName(Type _supportedMonoBehaviourType, string _name)
		{
			return $"{_supportedMonoBehaviourType.Name} Style: {_name}";
		}

		public static int GetKey(Type _supportedMonoBehaviourType, string _name)
		{
			return Animator.StringToHash(GetName(_supportedMonoBehaviourType, _name));
		}
	}
}