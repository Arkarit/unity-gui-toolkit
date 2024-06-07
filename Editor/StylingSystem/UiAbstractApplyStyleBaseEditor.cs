using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiAbstractApplyStyleBase), true)]
	public class UiAbstractApplyStyleBaseEditor : UnityEditor.Editor
	{
		private UiAbstractApplyStyleBase m_thisAbstractApplyStyleBase;

		protected virtual void OnEnable()
		{
			m_thisAbstractApplyStyleBase = target as UiAbstractApplyStyleBase;
		}

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
		}
	}
}
