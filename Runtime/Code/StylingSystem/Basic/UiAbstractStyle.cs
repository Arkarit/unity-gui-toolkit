using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public abstract class UiAbstractStyle<MB> : UiAbstractStyleBase where MB : MonoBehaviour
	{
		[SerializeField] private int m_key;

		public override Type SupportedMonoBehaviourType => typeof(MB);
		public override int Key
		{
			get
			{
				if (m_key == 0)
					m_key = UiStyleUtility.GetKey(SupportedMonoBehaviourType, Name);

				return m_key;
			}
		}
	}
}
