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
				if (UiStyleConfigEditor.SortType >= UiStyleConfigEditor.ESortType.FlatPathAscending)
				{
					LabelField("   " + currentStyle.Name, 0, EditorStyles.boldLabel);
					IncreaseX(EditorGUIUtility.labelWidth + 18);
					LabelField($"Type: {currentStyle.SupportedMonoBehaviourType.Name}", 0, EditorStyles.boldLabel);
					IncreaseX(-60);
				}
				else
				{
					LabelField($"Type: {currentStyle.SupportedMonoBehaviourType.Name}", 0, EditorStyles.boldLabel);
					IncreaseX(-60);
				}

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
						UiEventDefinitions.EvDeleteStyle.InvokeAlways(null, currentStyle);
						return;
					}
				}
			});

			Space(5);
			Line(LineGapVert, m_currentRect.width - 5);

			Indent(() =>
			{
				Space(5);

				EditorGUI.BeginChangeCheck();
				
				var screenOrientationConditionProp = Property.FindPropertyRelative("m_screenOrientationCondition");
				EnumPopupField<UiAbstractStyleBase.EScreenOrientationCondition>("Condition:", screenOrientationConditionProp);

				var oldVal = ApplicableValueBaseDrawer.DrawCondition;
				ApplicableValueBaseDrawer.DrawCondition = ApplicableValueBaseDrawer.EDrawCondition.OnlyEnabled;
				DrawProperties();

				ApplicableValueBaseDrawer.DrawCondition = ApplicableValueBaseDrawer.EDrawCondition.OnlyDisabled;
				if (HasHiddenProperties())
				{
					Foldout(currentStyle, "Unused Properties", false, () =>
					{
						DrawProperties();
					});
				}

				Space(-SingleLineHeight);

				ApplicableValueBaseDrawer.DrawCondition = oldVal;

				if (EditorGUI.EndChangeCheck())
				{
					Property.serializedObject.ApplyModifiedProperties();
					UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
					if (m_applicableChanged)
					{
						UiEventDefinitions.EvStyleApplicableChanged.InvokeAlways(null, currentStyle);
#if UNITY_EDITOR
						UiStyleConfig.EditorSave(UiStyleConfig.Instance);
#endif
					}
				}
			});

			Space(EndGap);
		}

		private bool HasHiddenProperties()
		{
			bool result = false;

			ForEachChildProperty(Property, childProperty =>
			{
				if (childProperty.name == "m_name")
					return true;

				var val = childProperty.boxedValue as ApplicableValueBase;
				if (val != null && !val.IsApplicable)
				{
					result = true;
					return false;
				}

				return true;
			});

			return result;
		}

		private void DrawProperties()
		{
			EditorGUI.BeginChangeCheck();

			ForEachChildProperty(Property, childProperty =>
			{
				if (childProperty.name == "m_name")
					return true;

				PropertyField(childProperty);
				return true;
			});

			if (EditorGUI.EndChangeCheck())
				m_applicableChanged = true;
		}
	}
}