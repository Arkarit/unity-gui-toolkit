using GuiToolkit.Editor;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
			
			// The config this row belongs to, taken from the property rather than from the style.
			//
			// UiAbstractStyleBase.m_styleConfig looks like the obvious source and is not: it is set in the
			// constructor and never maintained, and in the config shipped with the package 64 of 70 styles
			// have it null (which is why UiAbstractStyleBase.EffectiveStyleConfig exists at all). Anything
			// that trusts it does nothing at all for those styles - silently, because the config's event
			// handlers filter on it. The SerializedProperty, by contrast, physically comes from the asset the
			// style lives in and cannot be stale.
			var owningConfig = Property.serializedObject.targetObject as UiStyleConfig ?? thisStyle.StyleConfig;

			// So a row is inherited exactly when it belongs to another config than the one being edited.
			var editedConfig = UiStyleConfigEditor.EditedConfig;
			bool isInherited = owningConfig != null && editedConfig != null && owningConfig != editedConfig;

			if (isInherited)
				Background(InheritedTint, InheritedTint, -3, 0, 0, -10);
			else
				Background(-3, 0, 0, -10);
			Space(3);
			Horizontal(SingleLineHeight, () =>
			{
				LabelField("   " + thisStyle.Alias, 0, EditorStyles.boldLabel);
				IncreaseX(EditorGUIUtility.labelWidth + 18);
				LabelField
				(
					isInherited
						? $"Type: {thisStyle.SupportedComponentType.Name}   -  inherited from '{owningConfig.name}'"
						: $"Type: {thisStyle.SupportedComponentType.Name}",
					0,
					EditorStyles.boldLabel
				);
				IncreaseX(-170);

				if (isInherited)
				{
					IncreaseX(30);
					if (Button("Override here", 140))
						OverrideInherited(thisStyle);

					return;
				}

				if (CanRevert(thisStyle))
				{
					IncreaseX(-70);
					if (Button("Revert", 65))
						RevertToInherited(thisStyle);

					IncreaseX(70);
				}

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
								UiEventDefinitions.EvSetStyleAlias.InvokeAlways(owningConfig, thisStyle, null);
								dialog.Cancel();
							}
							
							EditorGUILayout.Space(20);
						};
					
						var newName = EditorInputDialog.Show("Rename", "Please enter new name/path", thisStyle.Alias, additionalContent);
						if (!string.IsNullOrEmpty(newName))
						{
							UiEventDefinitions.EvSetStyleAlias.InvokeAlways(owningConfig, thisStyle, newName);
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
						UiEventDefinitions.EvDeleteStyle.InvokeAlways(owningConfig, thisStyle);
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
				
				// Values of an inherited entry are shown, not edited: the instance behind them belongs to
				// another asset, so a change would edit that config - and for the one inside the package it
				// would be dropped on save without a word. "Override here" is the way in.
				using var readOnly = new EditorGUI.DisabledScope(isInherited);

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
						UiEventDefinitions.EvStyleApplicableChanged.InvokeAlways(owningConfig, thisStyle);
#if UNITY_EDITOR
						UiStyleConfig.SetDirty(owningConfig);
#endif
					}
				}
			});

			Space(EndGap);
		}

		/// <summary>Subtle tint that sets an inherited entry apart without shouting.</summary>
		private static readonly Color InheritedTint = new Color(0.35f, 0.55f, 0.75f, 0.10f);

		/// <summary>
		/// The skin this row belongs to, read from the property path of the object it lives in. An inherited
		/// row comes from another asset, where the same skin may sit at a different index - the NAME is what
		/// both sides agree on, which is exactly why inheritance matches skins by name.
		/// </summary>
		private string SkinNameOfThisRow()
		{
			var match = Regex.Match(Property.propertyPath, @"^m_skins\.Array\.data\[(\d+)\]");
			if (!match.Success)
				return null;

			int skinIndex = int.Parse(match.Groups[1].Value);
			var skinsProp = Property.serializedObject.FindProperty("m_skins");
			if (skinsProp == null || skinIndex >= skinsProp.arraySize)
				return null;

			return skinsProp.GetArrayElementAtIndex(skinIndex).FindPropertyRelative("m_name")?.stringValue;
		}

		/// <summary>
		/// The OWN skin of the edited config that this row is shown under. Own, because materialising into a
		/// skin that is itself inherited would write into the parent asset - the thing all of this prevents.
		/// </summary>
		private UiSkin EditedSkinOfThisRow()
		{
			var skinName = SkinNameOfThisRow();
			if (string.IsNullOrEmpty(skinName))
				return null;

			return UiStyleConfigEditor.EditedConfig?.GetOwnSkinByNameOrAlias(skinName, false);
		}

		private bool CanRevert( UiAbstractStyleBase _style )
		{
			var skin = EditedSkinOfThisRow();
			return skin != null && skin.OwnsStyle(_style.Key) && skin.InheritedStyleByKey(_style.Key) != null;
		}

		/// <summary>
		/// Copies an inherited style into the edited config so it can be changed there. Deferred, because it
		/// adds to the very list that is being drawn at that moment.
		/// </summary>
		private void OverrideInherited( UiAbstractStyleBase _style )
		{
			var skin = EditedSkinOfThisRow();
			int key = _style.Key;
			if (skin == null)
			{
				UiLog.LogError($"Cannot override '{_style.Name}': the edited config does not declare the skin " +
				               "this style is shown under. Add that skin to it first.");
				return;
			}

			EditorApplication.delayCall += () =>
			{
				skin.MaterializeStyle(key);
				PropertyDrawerView.ClearHeightCache();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			};
		}

		/// <summary>
		/// Drops the edited config's own copy so the style is inherited again. Deferred for the same reason.
		/// </summary>
		private void RevertToInherited( UiAbstractStyleBase _style )
		{
			var skin = EditedSkinOfThisRow();
			int key = _style.Key;
			string alias = _style.Alias;
			if (skin == null)
				return;

			if (!EditorUtility.DisplayDialog
			(
				"Revert to inherited?",
				$"'{alias}' loses the values set here and follows the config it is inherited from again.",
				"Revert",
				"Cancel"
			))
			{
				return;
			}

			EditorApplication.delayCall += () =>
			{
				skin.RevertStyleToInherited(key);
				PropertyDrawerView.ClearHeightCache();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			};
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
				UiLog.LogInternal($"No Appliers of type {type.FullName} found");
				return;
			}
			
			string s = $"Found Appliers of type {type.FullName}\n";
			
			if (!string.IsNullOrEmpty(prefabPaths))
				s += $"Prefabs:\n{prefabPaths}\n";
			
			if (!string.IsNullOrEmpty(scenePaths))
				s += $"Scenes:\n{scenePaths}\n";
			
			GUIUtility.systemCopyBuffer = s;
			
			s += "\nA copy of this has been pasted to clipboard.";
			UiLog.LogInternal(s);
		}

		/// <summary>
		/// This drawer measures the same value rows twice with different outcomes - once for the applicable
		/// ones and once for the unused ones - by flipping ApplicableValueBaseDrawer.DrawCondition. So the
		/// condition has to be part of the cache key, or the second pass reads the first one's heights.
		/// </summary>
		protected override string HeightCacheKeySuffix => ApplicableValueBaseDrawer.DrawCondition.ToString();

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
