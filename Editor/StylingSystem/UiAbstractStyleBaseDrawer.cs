using GuiToolkit.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiAbstractStyleBase), true)]
	public class UiAbstractStyleBaseDrawer : AbstractPropertyDrawer<UiAbstractStyleBase>
	{
		private const float LineEndGapHor = 20;
		private const float LineGapVert = 4;
		private const float EndGap = 20;
		private bool m_applicableChanged;

		protected override void OnInspectorGUI()
		{
			m_applicableChanged = false;
			var currentStyle = Property.boxedValue as UiAbstractStyleBase;
			if (currentStyle == null)
				return;

			LabelField(UiStyleUtility.GetName(currentStyle.SupportedMonoBehaviourType, currentStyle.Name));
			Line(LineGapVert, EditorGUIUtility.labelWidth - LineEndGapHor);

			Space(5);

			Indent(() =>
			{
				EditorGUI.BeginChangeCheck();
				
				var oldVal = ApplicableValueBaseDrawer.DrawCondition;
				ApplicableValueBaseDrawer.DrawCondition = ApplicableValueBaseDrawer.EDrawCondition.OnlyEnabled;
				DrawProperties();

				ApplicableValueBaseDrawer.DrawCondition = ApplicableValueBaseDrawer.EDrawCondition.OnlyDisabled;
				Foldout(currentStyle, "Unused Properties", false, () =>
				{
					DrawProperties();
				});

				ApplicableValueBaseDrawer.DrawCondition = oldVal;

				if (EditorGUI.EndChangeCheck())
				{
					Property.serializedObject.ApplyModifiedProperties();
					UiEvents.EvSkinChanged.InvokeAlways();
					if (m_applicableChanged)
					{
						UiEvents.EvStyleApplicableChanged.InvokeAlways(currentStyle);
#if UNITY_EDITOR
						UiMainStyleConfig.EditorSave(UiMainStyleConfig.Instance);
#endif
					}
				}
			});

			Space(EndGap);
		}

		private void DrawProperties()
		{
			EditorGUI.BeginChangeCheck();

			ForEachChildProperty(Property, childProperty =>
			{
				if (childProperty.name == "m_key" || childProperty.name == "m_name")
					return;

				PropertyField(childProperty);
			});

			if (EditorGUI.EndChangeCheck())
				m_applicableChanged = true;
		}
	}
}