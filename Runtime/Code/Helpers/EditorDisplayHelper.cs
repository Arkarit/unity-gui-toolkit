#if UNITY_EDITOR
using GuiToolkit.Style;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class EditorDisplayHelperStyle : EditorDisplayHelper<UiAbstractStyleBase>
	{
		protected class DisplayHelperObjectStyle : DisplayHelperObject<UiAbstractStyleBase>
		{
			[SerializeReference] public UiAbstractStyleBase m_obj;
			public override UiAbstractStyleBase Obj
			{
				get => m_obj;
				set => m_obj = value;
			}
		}

		private DisplayHelperObjectStyle m_helper;
		private static EditorDisplayHelperStyle s_instance;

		public static EditorDisplayHelperStyle Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new EditorDisplayHelperStyle();
				return s_instance;
			}
		}

		protected override DisplayHelperObject<UiAbstractStyleBase> Helper
		{
			get
			{
				if (m_helper == null)
					m_helper = ScriptableObject.CreateInstance<DisplayHelperObjectStyle>();
				return m_helper;
			}
		}
	}

	public abstract class EditorDisplayHelper<TC>
	{
		private DisplayHelperObject<TC> m_helper;
		private DisplayHelperObjectEditor<TC> m_helperEditor;
		

		// Draw a style in the inspector without the need to actually [SerializeReference] it (which totally bloats stuff)
		public void Draw(TC _obj)
		{
			var styleHelper = Helper;
			styleHelper.Obj = _obj;
			HelperEditor.OnInspectorGUI();
		}

		protected abstract class DisplayHelperObject<T> : ScriptableObject
		{
			public abstract T Obj { get; set; }
		}

		private class DisplayHelperObjectEditor<T> : Editor
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
//					m_helper = null;
//					m_helperEditor = null;
				}
			}
		}

		protected abstract DisplayHelperObject<TC> Helper { get; }
//		{
//			get
//			{
//				if (m_helper == null)
//				{
//					var type = typeof(DisplayHelperObject<TC>);
//					var bla = (DisplayHelperObject<TC>)ScriptableObject.CreateInstance(type);
//					m_helper = bla;
//				}
//
//				return m_helper;
//			}
//		}

		private Editor HelperEditor
		{
			get
			{
				if (   m_helperEditor == null 
				    || m_helperEditor.serializedObject == null 
				    || m_helperEditor.target == null)
					m_helperEditor = (DisplayHelperObjectEditor<TC>)Editor.CreateEditor(Helper, typeof(DisplayHelperObjectEditor<TC>));

				return m_helperEditor;
			}

		}
	}

}

#endif
