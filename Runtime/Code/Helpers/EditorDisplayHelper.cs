#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

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
			public object GenericInstanceObject;

			public Object ObjectInstanceObject;
		}

		private class HelperObjectEditor : Editor
		{
			private Editor m_helperEditor;
			private Type m_lastType = null;

			private void OnDisable()
			{
				m_helperEditor.SafeDestroy();
				m_helperEditor = null;
				m_lastType = null;
			}

			public override void OnInspectorGUI()
			{
				try
				{
					var thisHelperObject = (HelperObject) target;
					if (thisHelperObject.GenericInstanceObject != null)
					{
						EditorGeneralUtility.DrawInspectorExceptFields(serializedObject, new HashSet<string>{"m_Script", "ObjectInstanceObject"});
						return;
					}

					var serObj = new SerializedObject(thisHelperObject.ObjectInstanceObject);

					if (thisHelperObject.ObjectInstanceObject is ScriptableObject so)
					{
						if (!m_helperEditor || m_lastType != so.GetType())
						{
							m_lastType = so.GetType();
							m_helperEditor.SafeDestroy();
							m_helperEditor = CreateEditor(so);
						}

						m_helperEditor.OnInspectorGUI();
						return;
					}

					EditorGUI.BeginChangeCheck();
					DrawPropertiesExcluding(serObj, "m_name");
					if (EditorGUI.EndChangeCheck())
						serObj.ApplyModifiedProperties();
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

			helper.GenericInstanceObject = null;
			helper.ObjectInstanceObject = null;

			if (_object is Object unityObject)
				helper.ObjectInstanceObject = unityObject;
			else
				helper.GenericInstanceObject = _object;

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
