// From https://forum.unity.com/threads/is-there-a-way-to-input-text-using-a-unity-editor-utility.473743/
// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class AbstractEditorInputDialog : EditorWindow
	{
		protected string m_description;
		protected string m_okButtonText; 
		protected string m_cancelButtonText;
		protected bool m_positionInitialized = false;
		protected Action m_onOKButton;

		protected bool m_shouldClose = false;
		protected Vector2 m_maxScreenPos;

		protected Action<AbstractEditorInputDialog> m_additionalContent;
		
		protected abstract void ShowContentField();
		
		private void OnGUI()
		{
			// Check if Esc/Return have been pressed
			var e = Event.current;
			if (e.type == EventType.KeyDown)
			{
				switch (e.keyCode)
				{
					// Escape pressed
					case KeyCode.Escape:
						m_shouldClose = true;
						e.Use();
						break;

					// Enter pressed
					case KeyCode.Return:
					case KeyCode.KeypadEnter:
						m_onOKButton?.Invoke();
						m_shouldClose = true;
						e.Use();
						break;
				}
			}

			if (m_shouldClose)
				Close();

			// Draw our control
			var rect = EditorGUILayout.BeginVertical();

			EditorGUILayout.Space(12);
			EditorGUILayout.LabelField(m_description);

			EditorGUILayout.Space(8);
			GUI.SetNextControlName("inText");
			ShowContentField();
			GUI.FocusControl("inText");   // Focus text field
			EditorGUILayout.Space(12);

			m_additionalContent?.Invoke(this);

			// Draw OK / Cancel buttons
			var r = EditorGUILayout.GetControlRect();
			r.width /= 2;
			if (GUI.Button(r, m_okButtonText))
				Ok();

			r.x += r.width;
			if (GUI.Button(r, m_cancelButtonText))
				Cancel();

			EditorGUILayout.Space(8);
			EditorGUILayout.EndVertical();

			// Force change size of the window
			if (rect.width != 0 && minSize != rect.size)
			{
				minSize = maxSize = rect.size;
			}

			// Set dialog position next to mouse position
			if (!m_positionInitialized && e.type == EventType.Layout)
			{
				m_positionInitialized = true;

				// Move window to a new position. Make sure we're inside visible window
				var mousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
				mousePos.x += 32;
				if (mousePos.x + position.width > m_maxScreenPos.x)
					mousePos.x -= position.width + 64; // Display on left side of mouse
				if (mousePos.y + position.height > m_maxScreenPos.y)
					mousePos.y = m_maxScreenPos.y - position.height;

				position = new Rect(mousePos.x, mousePos.y, position.width, position.height);

				// Focus current window
				Focus();
			}
		}

		public virtual void Ok()
		{
			m_onOKButton?.Invoke();
			m_shouldClose = true;
		}
		
		public virtual void Cancel()
		{
			m_shouldClose = true;
		}
	}
	
	public class EditorInputDialog : AbstractEditorInputDialog
	{
		private string m_inputText;

		protected override void ShowContentField()
		{
			m_inputText = EditorGUILayout.TextField("", m_inputText);
		}

		/// <summary>
		/// Returns text player entered, or null if player cancelled the dialog.
		/// </summary>
		/// <param name="_title"></param>
		/// <param name="_description"></param>
		/// <param name="_inputText"></param>
		/// <param name="_okButtonText"></param>
		/// <param name="_cancelButtonText"></param>
		/// <returns></returns>
		public static string Show(string _title, string _description, string _inputText, Action<AbstractEditorInputDialog> _additionalContent = null, string _okButtonText = "OK", string _cancelButtonText = "Cancel")
		{
			// Make sure our popup is always inside parent window, and never offscreen
			// So get caller's window size
			var maxPos = GUIUtility.GUIToScreenPoint(new Vector2(Screen.width, Screen.height));

			string result = null;
			//var window = EditorWindow.GetWindow<InputDialog>();
			var window = CreateInstance<EditorInputDialog>();
			window.m_maxScreenPos = maxPos;
			window.titleContent = new GUIContent(_title);
			window.m_description = _description;
			window.m_inputText = _inputText;
			window.m_additionalContent = _additionalContent;
			window.m_okButtonText = _okButtonText;
			window.m_cancelButtonText = _cancelButtonText;
			window.m_onOKButton += () => result = window.m_inputText;
			//window.ShowPopup();
			window.ShowModal();

			return result;
		}
		
		public override void Cancel()
		{
			base.Cancel();
			m_inputText = null;
		}

	}
	
	public class EditorGameObjectInputDialog : AbstractEditorInputDialog
	{
		private GameObject m_gameObject;

		protected override void ShowContentField()
		{
			m_gameObject = (GameObject) EditorGUILayout.ObjectField("", m_gameObject, typeof(GameObject));
		}

		/// <summary>
		/// Returns game object player entered, or null if player cancelled the dialog.
		/// </summary>
		/// <param name="_title"></param>
		/// <param name="_description"></param>
		/// <param name="_gameObject"></param>
		/// <param name="_okButtonText"></param>
		/// <param name="_cancelButtonText"></param>
		/// <returns></returns>
		public static void Show(string _title, string _description, GameObject _gameObject, Action<GameObject> _onOk, Action<AbstractEditorInputDialog> _additionalContent = null, string _okButtonText = "OK", string _cancelButtonText = "Cancel")
		{
			// Make sure our popup is always inside parent window, and never offscreen
			// So get caller's window size
			var maxPos = GUIUtility.GUIToScreenPoint(new Vector2(Screen.width, Screen.height));

			GameObject result = null;
			//var window = EditorWindow.GetWindow<InputDialog>();
			var window = CreateInstance<EditorGameObjectInputDialog>();
			window.m_maxScreenPos = maxPos;
			window.titleContent = new GUIContent(_title);
			window.m_description = _description;
			window.m_gameObject = _gameObject;
			window.m_additionalContent = _additionalContent;
			window.m_okButtonText = _okButtonText;
			window.m_cancelButtonText = _cancelButtonText;
			window.m_onOKButton += () => _onOk.Invoke(window.m_gameObject);
			window.ShowUtility();
			//window.ShowModal();
		}
	}
}
#endif
