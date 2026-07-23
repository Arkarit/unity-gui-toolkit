using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A MonoBehaviour spawner for transient toast messages: its Show(message, duration) pool-instantiates a
	/// UiToastMessagePanel into a container RectTransform and displays it.
	/// </summary>
	public class UiToastMessageContainer : MonoBehaviour
	{
		public UiToastMessagePanel m_panelPrefab;
		public RectTransform m_containerTransform;

		public virtual UiToastMessagePanel Show(string _message, float _duration = 2)
		{
			UiToastMessagePanel result = m_panelPrefab.PoolInstantiate(m_containerTransform);
			result.Show(_message, _duration);
			return result;
		}
	}
}
