using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiTextContainer : MonoBehaviour
	{
		[SerializeField] protected TMP_Text m_text;

		public RectTransform RectTransform => (RectTransform) transform;

		public string Text
		{
			get
			{
				if (m_text != null)
					return m_text.text;
				return "";
			}
			set
			{
				if (m_text != null)
					m_text.text = value;
			}
		}
	}
}