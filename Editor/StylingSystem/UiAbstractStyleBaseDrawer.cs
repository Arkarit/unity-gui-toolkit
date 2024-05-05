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

		protected override void OnInspectorGUI()
		{
			var currentStyle = Property.boxedValue as UiAbstractStyleBase;
			if (currentStyle != null)
			{
				LabelField(UiStyleUtility.GetName(currentStyle.SupportedMonoBehaviourType, currentStyle.Name));
				Line(LineGapVert, EditorGUIUtility.labelWidth - LineEndGapHor);
			}

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
				}
			});

			Space(EndGap);
		}

		private void DrawProperties()
		{
			foreach (var childProperty in ChildProperties)
			{
				if (childProperty.name == "m_key" || childProperty.name == "m_name")
					continue;

				PropertyField(childProperty);
			}
		}
	}
}