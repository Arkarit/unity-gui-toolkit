using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit.Style.Editor
{
	[CustomEditor(typeof(UiAbstractApplyStyleBase), true)]
	public class UiAbstractApplyStyleBaseEditor : UnityEditor.Editor
	{
		private UiAbstractApplyStyleBase m_thisAbstractApplyStyleBase;
		private SerializedProperty m_nameProp;
		private SerializedProperty m_fixedSkinNameProp;
		private SerializedProperty m_isAspectRatioDependentProp;
		private SerializedProperty m_optionalStyleConfigProp;
		private SerializedProperty m_onBeforeApplyStyleProp;
		private SerializedProperty m_onAfterApplyStyleProp;
		private SerializedProperty m_frameDelayProp;
		private bool m_eventsOpen;


		protected virtual void OnEnable()
		{
			m_thisAbstractApplyStyleBase = target as UiAbstractApplyStyleBase;
			m_nameProp = serializedObject.FindProperty("m_name");
			m_fixedSkinNameProp = serializedObject.FindProperty("m_fixedSkinName");
			m_isAspectRatioDependentProp = serializedObject.FindProperty("m_isAspectRatioDependent");
			m_optionalStyleConfigProp = serializedObject.FindProperty("m_optionalStyleConfig");
			m_onBeforeApplyStyleProp = serializedObject.FindProperty("OnBeforeApplyStyle");
			m_onAfterApplyStyleProp = serializedObject.FindProperty("OnAfterApplyStyle");
			m_frameDelayProp = serializedObject.FindProperty("m_frameDelay");
			Undo.undoRedoPerformed += OnUndoOrRedo;
		}

		protected void OnDisable()
		{
			Undo.undoRedoPerformed -= OnUndoOrRedo;
		}

		private void OnUndoOrRedo()
		{
			EditorApplication.delayCall += () => UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		public override void OnInspectorGUI()
		{
			if (!AssetReadyGate.Ready)
				return;

			if (m_thisAbstractApplyStyleBase.Style == null)
				m_thisAbstractApplyStyleBase.SetStyle();

			UiStyleConfig styleConfig = m_thisAbstractApplyStyleBase.StyleConfig;
			string selectedName;

			EditorGUILayout.LabelField("Local Settings", EditorStyles.boldLabel);
			var effectiveStyleConfig = m_thisAbstractApplyStyleBase.StyleConfig;
			if (effectiveStyleConfig == null)
				EditorGUILayout.LabelField("Effective Style Config: <null>",EditorStyles.miniLabel);
			else
				EditorGUILayout.LabelField($"Effective Style Config: '{AssetDatabase.GetAssetPath(effectiveStyleConfig)}'",EditorStyles.miniLabel);

			EditorGUILayout.Space(5);
			
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_optionalStyleConfigProp);

			var config = (UiStyleConfig) m_optionalStyleConfigProp.objectReferenceValue;
			if (config == null)
			{
				EditorGUILayout.PropertyField(m_isAspectRatioDependentProp);
			}
			else
			{
				m_isAspectRatioDependentProp.boolValue = config is UiAspectRatioDependentStyleConfig;
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.PropertyField(m_isAspectRatioDependentProp);
				EditorGUI.EndDisabledGroup();
			}

			if (EditorGUI.EndChangeCheck())
			{
				m_thisAbstractApplyStyleBase.Reset();
				serializedObject.ApplyModifiedProperties();
				return;
			}
			
			var styleAliases = styleConfig.GetStyleAliasesByMonoBehaviourType(m_thisAbstractApplyStyleBase.SupportedComponentType);
			var styleNames = styleConfig.GetStyleNamesByMonoBehaviourType(m_thisAbstractApplyStyleBase.SupportedComponentType);
			
			int styleCountBefore = styleAliases.Count;
			string currentDisplayName = string.Empty;
			if (m_thisAbstractApplyStyleBase.Style != null)
				currentDisplayName = m_thisAbstractApplyStyleBase.Style.Alias;

			string styleNameSuggestion = FindStyleNameSuggestion();
			int styleIdx = EditorUiUtility.StringPopup("Style", styleAliases, currentDisplayName, out selectedName,
				    null, false, "Add Style", "Adds a new style", styleNameSuggestion);
			
			if (styleIdx != -1)
			{
				if (styleAliases.Count > styleCountBefore)
				{
					if (styleConfig.NumSkins == 0)
						UiStyleEditorUtility.AddSkin(styleConfig, "Default");

					styleConfig.ForeachSkin(skin =>
					{
						var newStyle = m_thisAbstractApplyStyleBase.CreateStyle(m_thisAbstractApplyStyleBase.StyleConfig, selectedName, m_thisAbstractApplyStyleBase.Style);
						newStyle.Init();
						skin.Styles.Add(newStyle);
					});

					UiStyleConfig.SetDirty(styleConfig);
					m_thisAbstractApplyStyleBase.Name = selectedName;
				}
				else
				{
					m_thisAbstractApplyStyleBase.Name = styleNames[styleIdx];
				}

				m_thisAbstractApplyStyleBase.Apply();
				EditorGeneralUtility.SetDirty(m_thisAbstractApplyStyleBase);
			}
			
			
			m_thisAbstractApplyStyleBase.FixedSkinName = UiStyleEditorUtility.GetSelectSkinPopup(styleConfig, m_thisAbstractApplyStyleBase.FixedSkinName, out bool _, true);
			
			if (!m_thisAbstractApplyStyleBase.SkinIsFixed)
				m_thisAbstractApplyStyleBase.Tweenable = EditorGUILayout.Toggle("Tweenable", m_thisAbstractApplyStyleBase.Tweenable);

			m_thisAbstractApplyStyleBase.RebuildLayoutOnApply = EditorGUILayout.Toggle("Rebuild Layout",
				m_thisAbstractApplyStyleBase.RebuildLayoutOnApply);
			EditorGUILayout.PropertyField(m_frameDelayProp);

			m_eventsOpen = EditorGUILayout.Foldout(m_eventsOpen, "Events");
			if (m_eventsOpen)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(m_onBeforeApplyStyleProp);
				EditorGUILayout.PropertyField(m_onAfterApplyStyleProp);
				EditorGUI.indentLevel--;
			}
			
			EditorGUILayout.Space(10);

			EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

			if (!m_thisAbstractApplyStyleBase.SkinIsFixed)
				UiStyleEditorUtility.SelectSkinByPopup(styleConfig);

			UiStyleEditorUtility.DrawStyle(m_thisAbstractApplyStyleBase, m_thisAbstractApplyStyleBase.Style);
			
			if (GUILayout.Button("Record"))
				m_thisAbstractApplyStyleBase.Record();

			serializedObject.ApplyModifiedProperties();
		}

		private string FindStyleNameSuggestion()
		{
			var styleAppliers = m_thisAbstractApplyStyleBase.GetComponents<UiAbstractApplyStyleBase>();
			if (styleAppliers.Length <= 1)
				return null;

			foreach (var styleApplier in styleAppliers)
			{
				if (styleApplier == m_thisAbstractApplyStyleBase)
					continue;

				string possibleResult = styleApplier.Style == null ? null : styleApplier.Style.Name;
				if (!string.IsNullOrEmpty(possibleResult))
				{
					var config = styleApplier.Style.EffectiveStyleConfig;
					if (!config.StyleExists(m_thisAbstractApplyStyleBase.SupportedStyleType, possibleResult))
						return possibleResult;
				}
			}

			return null;
		}
	}
}
