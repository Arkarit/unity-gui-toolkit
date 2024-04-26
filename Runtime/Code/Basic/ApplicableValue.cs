using System;

namespace GuiToolkit
{
	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		public bool IsApplicable = false;
		public T Value;

		public void Apply(ref T to)
		{
			if (IsApplicable)
				to = Value;
		}
	}
}
