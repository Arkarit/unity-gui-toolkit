using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public class UiStyleConfig : MonoBehaviour
	{
		[SerializeField] private List<string> m_styles = new ();
		[SerializeField] private int m_index = 0;

		public List<string> Styles => m_styles;
		public int Index => m_index;

		public string CurrentStyle
		{
			get
			{
				if (m_index < 0 || m_index >= m_styles.Count)
					return string.Empty;

				return m_styles[m_index];
			}
		}

		public static UiStyleConfig Instance => UiToolkitConfiguration.Instance.m_styleConfig;

	}
}