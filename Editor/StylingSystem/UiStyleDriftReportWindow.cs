using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Shows how far one style config has drifted from another, and what inheriting would change.
	///
	/// Read-only, on purpose: the question "should this clone inherit?" has to be answerable before anything
	/// is touched, and the answer is a number - how much of the clone is a copy that carries no information.
	/// </summary>
	public class UiStyleDriftReportWindow : EditorWindow, IEditorAware
	{
		private UiStyleConfig m_config;
		private UiStyleConfig m_other;
		private bool m_compareSingleSkins;
		private int m_skinIndex;
		private int m_otherSkinIndex;
		private string m_report;
		private Vector2 m_scrollPosition;

		[MenuItem(StringConstants.STYLE_DRIFT_REPORT)]
		public static void ShowWindow()
		{
			var window = GetWindow<UiStyleDriftReportWindow>();
			window.titleContent = new GUIContent("Style Drift");
			window.Show();
		}

		private void OnGUI()
		{
			if (!AssetReadyGate.Ready)
				GUIUtility.ExitGUI();

			// Prefilled with the pair that is nearly always meant: this project's config against the one it
			// inherits from, or - before that is set up - against the config shipped with the package.
			if (m_config == null)
				m_config = UiStyleConfig.Instance;

			if (m_other == null && m_config != null)
				m_other = m_config.Parent != null ? m_config.Parent : FindPackageConfig();

			EditorGUILayout.HelpBox
			(
				"Compares what each config declares itself, skin by skin. Nothing is written.\n"
				+ "Skins are matched the way inheritance matches them: by 'Inherits skin from' where one is "
				+ "set, by name otherwise.",
				MessageType.Info
			);

			m_config = (UiStyleConfig) EditorGUILayout.ObjectField("Config", m_config, typeof(UiStyleConfig), false);
			m_other = (UiStyleConfig) EditorGUILayout.ObjectField("Compare against", m_other, typeof(UiStyleConfig), false);

			// The other question this answers: not "how far apart are these two configs" but "which of these
			// skins should that skin build on?". Answered by running one skin against each candidate and
			// comparing the counts - including candidates in the same config, since a skin may build on a
			// sibling.
			m_compareSingleSkins = EditorGUILayout.Toggle("Compare single skins", m_compareSingleSkins);

			if (m_compareSingleSkins)
			{
				m_skinIndex = SkinPopup("Skin", m_config, m_skinIndex);
				m_otherSkinIndex = SkinPopup("against skin", m_other, m_otherSkinIndex);
			}

			bool sameConfigWholeCompare = !m_compareSingleSkins && m_config != null && m_config == m_other;

			using (new EditorGUI.DisabledScope(m_config == null || m_other == null || sameConfigWholeCompare))
			{
				if (GUILayout.Button("Analyze"))
					m_report = Analyze().ToText();
			}

			if (sameConfigWholeCompare)
				EditorGUILayout.HelpBox("Pick two different configs, or compare single skins.", MessageType.Warning);

			if (string.IsNullOrEmpty(m_report))
				return;

			EditorGUILayout.Space(5);
			if (GUILayout.Button("Copy to Clipboard"))
				EditorGUIUtility.systemCopyBuffer = m_report;

			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

			// A selectable label rather than a text area: the report is long, and this way a single line of
			// it can be picked out and pasted somewhere without the whole thing coming along.
			EditorGUILayout.SelectableLabel
			(
				m_report,
				EditorStyles.textArea,
				GUILayout.ExpandHeight(true),
				GUILayout.ExpandWidth(true)
			);

			EditorGUILayout.EndScrollView();
		}

		private UiStyleConfigDrift Analyze()
		{
			if (!m_compareSingleSkins)
				return UiStyleDriftAnalyzer.Analyze(m_config, m_other);

			var skin = SkinAt(m_config, m_skinIndex);
			var otherSkin = SkinAt(m_other, m_otherSkinIndex);

			var result = new UiStyleConfigDrift
			{
				Name = m_config.name,
				OtherName = m_other.name,
				AlreadyInherits = skin != null && skin.ParentSkin == otherSkin,
			};

			result.Skins.Add(UiStyleDriftAnalyzer.Analyze(skin, otherSkin));
			return result;
		}

		private static int SkinPopup( string _label, UiStyleConfig _config, int _index )
		{
			if (_config == null || _config.NumSkins == 0)
			{
				EditorGUILayout.LabelField(_label, "<no skins>");
				return 0;
			}

			var names = _config.SkinNames;
			return EditorGUILayout.Popup(_label, Mathf.Clamp(_index, 0, names.Count - 1), names.ToArray());
		}

		private static UiSkin SkinAt( UiStyleConfig _config, int _index )
		{
			if (_config == null || _config.NumSkins == 0)
				return null;

			return _config.Skins[Mathf.Clamp(_index, 0, _config.NumSkins - 1)];
		}

		/// <summary>
		/// The config that ships inside the package - the thing a project's own config was cloned from, and
		/// therefore the other side of the comparison as long as no parent is set yet.
		/// </summary>
		private static UiStyleConfig FindPackageConfig()
		{
			var candidate = GuiToolkit.Editor.EditorAssetUtility.FindScriptableObject<UiStyleConfig>(
				new GuiToolkit.Editor.EditorAssetUtility.AssetSearchOptions
				{
					Folders = new[] { "Assets", "Packages" },
					SearchString = "UiMainStyleConfig",
				});

			return candidate;
		}
	}
}
