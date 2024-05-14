using System;
using UnityEngine;

namespace GuiToolkit
{
	// Just a stub to satisfy PropertyDrawer
	[Serializable]
	public class ApplicableValueBase
	{
		public bool IsApplicable = false;
#if UNITY_EDITOR
		public ETriState ValueHasChildren = ETriState.Indeterminate;
#endif
	}
	
	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		public T Value;
	}
}
