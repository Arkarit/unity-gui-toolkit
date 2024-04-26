using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	[Serializable]
	public abstract class UiAbstractStyleBase
	{
		[SerializeField] private string m_name;

		public string Name => m_name;

		public abstract Type SupportedMonoBehaviour { get; }
	}
}