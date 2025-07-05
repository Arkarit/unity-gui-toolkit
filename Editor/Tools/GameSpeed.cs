#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class GameSpeed : EditorWindow
	{
		float m_speed = 1;

		private void OnGUI()
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUILayout.LabelField("Speed", GUILayout.Width(50));
			m_speed = EditorGUILayout.Slider(m_speed, 0, 2);

			EditorGUILayout.EndHorizontal();
			Time.timeScale = m_speed;
			EditorUpdater.TimeScale = m_speed;
		}

		[MenuItem(StringConstants.GAME_SPEED_MENU_NAME, priority = Constants.GAME_SPEED_MENU_PRIORITY)]
		public static GameSpeed GetWindow()
		{
			var window = GetWindow<GameSpeed>();
			window.titleContent = new GUIContent("Game speed");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}
#endif