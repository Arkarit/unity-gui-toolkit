using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiTabChapter : UiTextContainer
	{
		[SerializeField] protected VerticalLayoutGroup m_verticalLayoutGroup;
		[SerializeField] int m_topPaddingWithText = 91;
		[SerializeField] int m_topPaddingWithoutText = 30;
		[SerializeField] GameObject[] m_objectsToEnableWithText;
		[SerializeField] GameObject[] m_objectsToEnableWithoutText;

		public override string Text
		{
			get => base.Text;
			set
			{
				base.Text = value;
				bool hasText = !string.IsNullOrEmpty(base.Text);
				m_verticalLayoutGroup.padding.top = hasText ? m_topPaddingWithText : m_topPaddingWithoutText;
				foreach (var go in m_objectsToEnableWithText)
					go.SetActive(hasText);
				foreach (var go in m_objectsToEnableWithoutText)
					go.SetActive(!hasText);
			}
		}
	}
}