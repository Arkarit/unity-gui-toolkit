#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class CleanKerningTable : EditorWindow
	{

		private void OnGUI()
		{
//			EditorGUILayout.BeginHorizontal();
//
//			EditorGUILayout.LabelField("Speed", GUILayout.Width(50));
//			m_speed = EditorGUILayout.Slider(m_speed, 0, 2);
//
//			EditorGUILayout.EndHorizontal();
//			Time.timeScale = m_speed;
//			EditorUpdater.TimeScale = m_speed;
		}

		[MenuItem(StringConstants.CLEAN_KERNING_TABLE_MENU_NAME, priority = Constants.CLEAN_KERNING_TABLE_MENU_PRIORITY)]
		public static CleanKerningTable GetWindow()
		{
			var window = GetWindow<CleanKerningTable>();
			window.titleContent = new GUIContent("Clean Kerning Table");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}
#endif