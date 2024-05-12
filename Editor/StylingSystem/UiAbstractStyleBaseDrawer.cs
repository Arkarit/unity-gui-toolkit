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
		private const float EndGap = 30;
		private bool m_applicableChanged;

		protected override void OnInspectorGUI()
		{
			m_applicableChanged = false;
			var currentStyle = Property.boxedValue as UiAbstractStyleBase;
			if (currentStyle == null)
				return;

			Background(-3, 0, 0, -10);
			Space(3);
			Horizontal(SingleLineHeight, () =>
			{
				LabelField("   " + UiStyleUtility.GetName(currentStyle.SupportedMonoBehaviourType, currentStyle.Name), 0, EditorStyles.boldLabel);
				IncreaseX(-60);
				if (Button("Delete", 50))
				{
					if (EditorUtility.DisplayDialog
					(
						    "Are you sure?",
							$"The style '{currentStyle.Name}' will be removed from UiMainStyleConfig" 
							+ " and all skins and Ui Apply Style instances which use it. This can not be undone.",
							"OK",
							"Cancel"
					))
					{
						UiEvents.EvDeleteStyle.InvokeAlways(currentStyle);
						return;
					}
				}
			});
			Space(5);
			Line(LineGapVert, m_currentRect.width - 5);

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
				if (childProperty.name == "m_name")
					return;

				PropertyField(childProperty);
			});

			if (EditorGUI.EndChangeCheck())
				m_applicableChanged = true;
		}
	}
}