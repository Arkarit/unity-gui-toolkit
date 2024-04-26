using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	public abstract class UiAbstractApplyStyleBase : MonoBehaviour
	{
		[SerializeField] private string m_name;

		public abstract Type SupportedMonoBehaviourType { get; }
		public abstract Type SupportedStyleType { get; }
		public abstract MonoBehaviour MonoBehaviour { get; }
		public abstract int Key { get; }

		public abstract UiAbstractStyleBase CreateStyle();

		public string Name => m_name;
	}
}