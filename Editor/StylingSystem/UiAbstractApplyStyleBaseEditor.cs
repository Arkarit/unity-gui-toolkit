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
		private SerializedProperty m_isResolutionDependentProp;
		private SerializedProperty m_optionalStyleConfigProp;
		private SerializedProperty m_onBeforeApplyStyleProp;
		private SerializedProperty m_onAfterApplyStyleProp;
		private bool m_eventsOpen;


		protected virtual void OnEnable()
		{
			m_thisAbstractApplyStyleBase = target as UiAbstractApplyStyleBase;
			m_nameProp = serializedObject.FindProperty("m_name");
			m_fixedSkinNameProp = serializedObject.FindProperty("m_fixedSkinName");
			m_isResolutionDependentProp = serializedObject.FindProperty("m_isResolutionDependent");
			m_optionalStyleConfigProp = serializedObject.FindProperty("m_optionalStyleConfig");
			m_onBeforeApplyStyleProp = serializedObject.FindProperty("OnBeforeApplyStyle");
			m_onAfterApplyStyleProp = serializedObject.FindProperty("OnAfterApplyStyle");
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
			if (m_thisAbstractApplyStyleBase.Style == null)
				m_thisAbstractApplyStyleBase.SetStyle();

			UiStyleConfig styleConfig = m_thisAbstractApplyStyleBase.StyleConfig;
			string selectedName;

			EditorGUILayout.LabelField("Local Settings", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(m_isResolutionDependentProp);
			if (!m_thisAbstractApplyStyleBase.IsResolutionDependent)
				EditorGUILayout.PropertyField(m_optionalStyleConfigProp);
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
