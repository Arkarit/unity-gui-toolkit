#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// This class helps to very easily display a generic serializable class within another context (e.g. an Editor or EditorWindow)
	/// </summary>
	public static class EditorDisplayHelper
	{
		private class HelperObject : ScriptableObject
		{
			[SerializeReference]
			public object Obj;
		}

		private class HelperObjectEditor : Editor
		{
			public override void OnInspectorGUI()
			{
				try
				{
					EditorGeneralUtility.DrawInspectorExceptField(serializedObject, "m_Script");
				}
				catch
				{
					// Sometimes when editing colors in a style, Unity begins to spit "The operation is not possible when moved past all properties"
					// without any determinable reason.
					// Just refreshing the style helper and editor helps.
					s_helper.SafeDestroy();
					s_helperEditor.SafeDestroy();
					s_helper = null;
					s_helperEditor = null;
				}
			}
		}

		private static HelperObject s_helper;
		private static HelperObjectEditor s_helperEditor;

		public static void Draw(object _object, string _messageIfObjIsNull)
		{
			if (_object == null)
			{
				if (!string.IsNullOrEmpty(_messageIfObjIsNull))
				{
					EditorGUILayout.HelpBox(_messageIfObjIsNull, MessageType.Warning);
					EditorGUILayout.Space(10);
				}

				return;
			}
	
			var helper = Helper;
			helper.Obj = _object;
			HelperEditor.OnInspectorGUI();
		}

		private static HelperObject Helper
		{
			get
			{
				if (s_helper == null)
					s_helper = ScriptableObject.CreateInstance<HelperObject>();

				return s_helper;
			}
		}

		private static Editor HelperEditor
		{
			get
			{
				if (s_helperEditor == null || s_helperEditor.target == null)
				{
					s_helperEditor.SafeDestroy();
					s_helperEditor = (HelperObjectEditor)Editor.CreateEditor(Helper, typeof(HelperObjectEditor));
				}

				return s_helperEditor;
			}
		}
	}

}

#endif
