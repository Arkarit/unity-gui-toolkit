using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public abstract class UiAbstractStyle<CO> : UiAbstractStyleBase where CO : Component
	{
		public override Type SupportedComponentType => typeof(CO);
	}
}
