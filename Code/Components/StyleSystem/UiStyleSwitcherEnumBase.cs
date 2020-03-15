using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiStyleSwitcherEnumBase<T> : UiStyleSwitcher where T : Enum
	{
		public T CurrentStyle
		{
			get
			{
				return (T) (object) CurrentStyleIndex;
			}
			set
			{
				CurrentStyleIndex = (int)(object) value;
			}
		}
	}
}