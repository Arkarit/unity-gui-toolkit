using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
				m_verticalLayoutGroup.padding.top = string.IsNullOrEmpty(value) ? m_topPaddingWithoutText : m_topPaddingWithText;
			}
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(UiTabChapter))]
	public class UiTabChapterEditor : UiTextContainerEditor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			DrawDefaultInspector();
		}
	}
#endif
}