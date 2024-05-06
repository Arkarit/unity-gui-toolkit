using System;

namespace GuiToolkit
{
	// Just a stub to satisfy PropertyDrawer
	[Serializable]
	public class ApplicableValueBase
	{
		public bool IsApplicable = false;
	}
	
	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		public T Value;

		public void Apply(ref T to)
		{
			if (IsApplicable)
				to = Value;
		}
	}
}
