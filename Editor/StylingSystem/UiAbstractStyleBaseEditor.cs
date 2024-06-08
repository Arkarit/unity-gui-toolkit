using GuiToolkit.Style;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiAbstractStyleBase), true)]
	public class UiAbstractStyleBaseEditor : UnityEditor.Editor
	{
		private UiAbstractStyleBase m_thisAbstractStyleBase;

		protected virtual void OnEnable()
		{
			m_thisAbstractStyleBase = target as UiAbstractStyleBase;
		}

		public override void OnInspectorGUI()
		{
			if (m_thisAbstractStyleBase.Empty)
			{
				EditorGUILayout.HelpBox("This style has no skins yet. Please define a skin in Ui Style Config before you can edit the values", MessageType.Info);
				return;
			}

			base.OnInspectorGUI();
		}
	}
}