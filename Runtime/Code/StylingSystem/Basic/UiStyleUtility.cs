using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleUtility
	{
		public static int GetKey(Type supportedMonoBehaviourType, string name)
		{
			return Animator.StringToHash(supportedMonoBehaviourType.Name + name);
		}
	}
}