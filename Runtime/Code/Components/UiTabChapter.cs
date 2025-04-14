using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiTabChapter : UiTextContainer
	{
		[SerializeField] protected VerticalLayoutGroup m_verticalLayoutGroup;
		[SerializeField] int m_topPaddingWithText = 91;
		[SerializeField] int m_topPaddingWithoutText = 30;

		public override string Text
		{
			get => base.Text;
			set
			{
				base.Text = value;
				m_verticalLayoutGroup.padding.top = string.IsNullOrEmpty(base.Text) ? m_topPaddingWithoutText : m_topPaddingWithText;
			}
		}
	}
}