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

			using (new EditorGUI.DisabledScope(m_config == null || m_other == null || m_config == m_other))
			{
				if (GUILayout.Button("Analyze"))
					m_report = UiStyleDriftAnalyzer.Analyze(m_config, m_other).ToText();
			}

			if (m_config != null && m_config == m_other)
				EditorGUILayout.HelpBox("Pick two different configs.", MessageType.Warning);

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
