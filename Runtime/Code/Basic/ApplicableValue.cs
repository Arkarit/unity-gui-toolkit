using System;
using UnityEngine;

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
		public delegate T GetterDelegate();
		public delegate void SetterDelegate(T val);

		public T Value;
		public GetterDelegate Getter;
		public SetterDelegate Setter;

		public void Apply()
		{
			if (!IsApplicable)
				return;

			if (Setter == null)
			{
				Debug.Log("Attempt to use setter, but setter is not supplied");
				return;
			}

			Setter(Value);
		}
	}
}
