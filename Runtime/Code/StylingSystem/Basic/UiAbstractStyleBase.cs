using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	[Serializable]
	public abstract class UiAbstractStyleBase
	{
		[SerializeField] private string m_name;

		public string Name
		{
			get => m_name;
			set => m_name = value;
		}

		public abstract Type SupportedMonoBehaviourType { get; }
		public abstract int Key { get; }
	}
}