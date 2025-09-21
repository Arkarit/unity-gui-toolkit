#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// This class helps to very easily display a generic serializable class within another context (e.g. an Editor or EditorWindow)
	/// </summary>
	public static class EditorDisplayHelper
	{
		private class HelperObject : ScriptableObject
		{
			[SerializeReference] public object GenericInstanceObject;
			public Object ObjectInstanceObject;
		}

		private class HelperObjectEditor : UnityEditor.Editor
		{
			private UnityEditor.Editor m_targetHelperEditor;
			private Type m_lastType = null;
			private SerializedObject m_serializedObject;

			private SerializedObject GetCachedSerializedObject( HelperObject _helperObject )
			{
				bool serializedObjectCreated = m_serializedObject != null;
				bool isGenericInstanceObject = serializedObjectCreated && _helperObject.GenericInstanceObject == m_serializedObject.targetObject;
				bool isObjectInstanceObject = serializedObjectCreated && _helperObject.ObjectInstanceObject == m_serializedObject.targetObject;
				bool isInvalid = !(isObjectInstanceObject || isGenericInstanceObject);

				if (isInvalid)
					m_serializedObject = new SerializedObject(_helperObject);

				return m_serializedObject;
			}

			public UnityEditor.Editor TargetHelperEditor => m_targetHelperEditor;

			private void OnDisable()
			{
				m_targetHelperEditor.SafeDestroy();
				m_targetHelperEditor = null;
				m_lastType = null;
			}

			public override void OnInspectorGUI()
			{
				try
				{
					var thisHelperObject = (HelperObject) target;
					var serObj = GetCachedSerializedObject(thisHelperObject);
					if (thisHelperObject.GenericInstanceObject != null)
					{
						EditorGeneralUtility.DrawInspectorExceptFields(serObj, new HashSet<string>{"m_Script", "ObjectInstanceObject"});
						return;
					}

					if (thisHelperObject.ObjectInstanceObject is ScriptableObject so)
					{
						if (!m_targetHelperEditor || m_lastType != so.GetType())
						{
							m_lastType = so.GetType();
							m_targetHelperEditor.SafeDestroy();
							m_targetHelperEditor = CreateEditor(so);
						}

						m_targetHelperEditor.OnInspectorGUI();
						return;
					}

					EditorGUI.BeginChangeCheck();
					DrawPropertiesExcluding(serObj, "m_name");
					if (EditorGUI.EndChangeCheck())
						serObj.ApplyModifiedProperties();
				}
				catch (ExitGUIException)
			    {
				    // We need to rethrow, since Unity abuses this exception for its control flow
			        throw;
			    }				
				catch (Exception e)
				{
					// Sometimes when editing colors in a style, Unity begins to spit "The operation is not possible when moved past all properties"
					// without any determinable reason.
					// Just refreshing the style helper and editor helps.
					UiLog.Log($"Refreshing style helper, exception:'{e.Message}'");
					s_helper.SafeDestroy();
					s_helperEditor.SafeDestroy();
					s_helper = null;
					s_helperEditor = null;
				}
			}
		}

		private static HelperObject s_helper;
		private static HelperObjectEditor s_helperEditor;

		public static T GetTargetHelperEditor<T>() where T : UnityEditor.Editor
		{
			if (!HelperEditor)
				return null;

			return HelperEditor.TargetHelperEditor as T;
		}

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

		private static HelperObjectEditor HelperEditor
		{
			get
			{
				if (s_helperEditor == null || s_helperEditor.target == null)
				{
					s_helperEditor.SafeDestroy();
					s_helperEditor = (HelperObjectEditor)UnityEditor.Editor.CreateEditor(Helper, typeof(HelperObjectEditor));
				}

				return s_helperEditor;
			}
		}
	}

}

#endif
