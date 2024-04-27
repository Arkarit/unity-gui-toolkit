using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	[Serializable]
	public abstract class UiAbstractStyleBase
	{
		[SerializeField] private string m_name;
		[SerializeField] private int m_key;

		public string Name
		{
			get => m_name;
			set => m_name = value;
		}

		public abstract Type SupportedMonoBehaviourType { get; }
		
		public int Key
		{
			get
			{
				if (m_key == 0 || !Application.isPlaying)
					m_key = UiStyleUtility.GetKey(SupportedMonoBehaviourType, Name);

				return m_key;
			}
		}

	}
}