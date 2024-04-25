using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	public abstract class UiAbstractStyleBase
	{
		public abstract Type SupportedMonoBehaviour { get; }
	}
}