using System;
using UnityEngine;

namespace GuiToolkit
{
	// Just a stub to satisfy PropertyDrawer
	[Serializable]
	public abstract class ApplicableValueBase
	{
		public bool IsApplicable = false;
#if UNITY_EDITOR
		[NonSerialized] public ETriState ValueHasChildren = ETriState.Indeterminate;
#endif
		public abstract object ValueObj { get;}
	}
	
	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		public T Value;
		public override object ValueObj => Value;
	}
}
