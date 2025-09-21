using GuiToolkit.Editor;
using System;
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
			var thisStyle = Property.boxedValue as UiAbstractStyleBase;
			if (thisStyle == null)
				return;
			
			UiStyleConfig styleConfig = thisStyle.StyleConfig;

			Background(-3, 0, 0, -10);
			Space(3);
			Horizontal(SingleLineHeight, () =>
			{
				LabelField("   " + thisStyle.Alias, 0, EditorStyles.boldLabel);
				IncreaseX(EditorGUIUtility.labelWidth + 18);
				LabelField($"Type: {thisStyle.SupportedComponentType.Name}", 0, EditorStyles.boldLabel);
				IncreaseX(-170);

				if (Button("Find", 35))
				{
					FindAppliers(thisStyle);
				}
				
				IncreaseX(40);

				if (Button("Rename", 55))
				{
					EditorApplication.delayCall += () =>
					{
						Action<AbstractEditorInputDialog> additionalContent = dialog =>
						{
							if (GUILayout.Button("Reset Name"))
							{
								UiEventDefinitions.EvSetStyleAlias.InvokeAlways(thisStyle.StyleConfig, thisStyle, null);
								dialog.Cancel();
							}
							
							EditorGUILayout.Space(20);
						};
					
						var newName = EditorInputDialog.Show("Rename", "Please enter new name/path", thisStyle.Alias, additionalContent);
						if (!string.IsNullOrEmpty(newName))
						{
							UiEventDefinitions.EvSetStyleAlias.InvokeAlways(thisStyle.StyleConfig, thisStyle, newName);
						}
					};
				}
				
				IncreaseX(60);
				
				if (Button("Delete", 50))
				{
					if (EditorUtility.DisplayDialog
					(
						    "Are you sure?",
							$"The style '{thisStyle.Alias}' will be removed from UiStyleConfig" 
							+ " and all skins and UI Apply Style instances which use it. This can not be undone.",
							"OK",
							"Cancel"
					))
					{
						UiEventDefinitions.EvDeleteStyle.InvokeAlways(thisStyle.StyleConfig, thisStyle);
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
					Foldout(thisStyle, "Unused Properties", false, () =>
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
						InvalidateHeightCache();
						UiEventDefinitions.EvStyleApplicableChanged.InvokeAlways(thisStyle.StyleConfig, thisStyle);
#if UNITY_EDITOR
						UiStyleConfig.SetDirty(styleConfig);
#endif
					}
				}
			});

			Space(EndGap);
		}

		private static void FindAppliers(UiAbstractStyleBase style)
		{
			if (style == null)
				return;
			
			var type = style.GetType();
			HashSet<string> prefabPathsDone = new();
			string prefabPaths = string.Empty;
			string alias = style.Alias;
			
			EditorAssetUtility.FindAllComponentsInAllPrefabs<UiAbstractApplyStyleBase>((applier, _, path) =>
			{
				if (applier.Style == null)
					return true;
				
				if (applier.Style.GetType() != type)
					return true;
				
				if (applier.Style.Alias != alias)
					return true;
				
				if (!prefabPathsDone.Contains(path))
				{
					prefabPaths += $"\t{path}\n";
					prefabPathsDone.Add(path);
				}
				
				prefabPaths += $"\t\t{applier.gameObject.GetPath()}\n";
				return true;
			});
			
			string scenePaths = string.Empty;
			HashSet<string> scenePathsDone = new();
			
			EditorAssetUtility.FindAllComponentsInAllScenes<UiAbstractApplyStyleBase>((applier, _, path) =>
			{
				if (applier.Style == null)
					return true;
				
				if (applier.Style.GetType() != type)
					return true;
				
				if (applier.Style.Alias != alias)
					return true;
				
				if (!scenePathsDone.Contains(path))
				{
					scenePaths += $"\t{path}\n";
					scenePathsDone.Add(path);
				}
				
				scenePaths += $"\t\t{applier.gameObject.GetPath()}\n";
				return true;
			});
			
			if (string.IsNullOrEmpty(prefabPaths) && string.IsNullOrEmpty(scenePaths))
			{
				UiLog.Log($"No Appliers of type {type.FullName} found");
				return;
			}
			
			string s = $"Found Appliers of type {type.FullName}\n";
			
			if (!string.IsNullOrEmpty(prefabPaths))
				s += $"Prefabs:\n{prefabPaths}\n";
			
			if (!string.IsNullOrEmpty(scenePaths))
				s += $"Scenes:\n{scenePaths}\n";
			
			GUIUtility.systemCopyBuffer = s;
			
			s += "\nA copy of this has been pasted to clipboard.";
			UiLog.Log(s);
		}

		protected override float GetPropertyHeight(SerializedProperty _property)
		{
			var val = _property.boxedValue as ApplicableValueBase;
			if (val != null && !val.IsApplicable)
			{
				if (ApplicableValueBaseDrawer.DrawCondition == ApplicableValueBaseDrawer.EDrawCondition.OnlyEnabled)
					return 0;
				if (ApplicableValueBaseDrawer.DrawCondition == ApplicableValueBaseDrawer.EDrawCondition.OnlyDisabled)
					return SingleLineHeight;
			}
			
			return base.GetPropertyHeight(_property);
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
