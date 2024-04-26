using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	public abstract class UiAbstractApplyStyleBase : MonoBehaviour
	{
		public abstract Type SupportedMonoBehaviourType { get; }
		public abstract Type SupportedStyleType { get; }
		public abstract MonoBehaviour MonoBehaviour { get; }

		public abstract UiAbstractStyleBase CreateStyle();
	}
}