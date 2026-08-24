using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	/// <summary>
	/// Shows how far one style config has drifted from another, what inheriting would change - and carries
	/// the change out once someone has looked at it.
	///
	/// Looking comes first, and separately: the question "should this clone inherit?" has to be answerable
	/// before anything is touched, and the answer is a number, namely how much of the clone is a copy that
	/// carries no information.
	/// </summary>
	public class UiStyleDriftReportWindow : EditorWindow, IEditorAware
	{
		private UiStyleConfig m_config;
		private UiStyleConfig m_other;
		private bool m_compareSingleSkins;
		private int m_skinIndex;
		private int m_otherSkinIndex;
		private string m_report;
		private UiStyleConversionPlan m_plan;
		private bool m_planOpen;
		private string m_applyResult;
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
				"Compares what each config declares itself, skin by skin. Analyzing writes nothing.\n"
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
					Analyze();
			}

			if (sameConfigWholeCompare)
				EditorGUILayout.HelpBox("Pick two different configs, or compare single skins.", MessageType.Warning);

			if (!string.IsNullOrEmpty(m_applyResult))
				EditorGUILayout.HelpBox(m_applyResult, MessageType.Info);

			if (string.IsNullOrEmpty(m_report))
				return;

			EditorGUILayout.Space(5);
			if (GUILayout.Button("Copy report to Clipboard"))
				EditorGUIUtility.systemCopyBuffer = m_report;

			m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

			DrawConversion();

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
		/// The part that writes. Everything droppable is listed by name, so what is about to be removed can
		/// be read before it is - and any single one of them can be kept.
		/// </summary>
		private void DrawConversion()
		{
			if (m_plan == null || m_plan.Entries.Count == 0)
				return;

			EditorGUILayout.Space(5);
			EditorGUILayout.LabelField("Convert", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(m_plan.Describe(), MessageType.None);

			m_planOpen = EditorGUILayout.Foldout(m_planOpen, $"Copies to drop ({m_plan.DropCount} of {m_plan.Entries.Count})");
			if (m_planOpen)
			{
				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Drop all"))
					SetDropAll(true);

				if (GUILayout.Button("Keep all"))
					SetDropAll(false);

				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;
				foreach (var entry in m_plan.Entries)
				{
					entry.Drop = EditorGUILayout.ToggleLeft
					(
						new GUIContent
						(
							$"{entry.Alias}   ({entry.TypeName})   -   skin '{entry.Skin.Name}'",
							"On: the copy is dropped and the style is inherited.\n"
							+ "Off: the copy is kept as an override - identical today, and free to differ "
							+ "tomorrow because it no longer follows the other config."
						),
						entry.Drop
					);
				}

				EditorGUI.indentLevel--;
			}

			using (new EditorGUI.DisabledScope(m_plan.DropCount == 0 && m_plan.ParentToSet == null))
			{
				if (GUILayout.Button("Apply - changes the asset"))
					ApplyPlan();
			}

			EditorGUILayout.Space(5);
		}

		private void SetDropAll( bool _drop )
		{
			foreach (var entry in m_plan.Entries)
				entry.Drop = _drop;
		}

		private void ApplyPlan()
		{
			if (!EditorUtility.DisplayDialog
			(
				"Convert to inheritance?",
				m_plan.Describe() + "\nThis changes the asset. It is one step in the undo history, and no "
					+ "style is removed unless there is something to inherit it from.",
				"Convert",
				"Cancel"
			))
			{
				return;
			}

			m_applyResult = UiStyleConversion.Apply(m_plan);

			// Re-analyzed right away, so what is on screen afterwards is the new state and not the one the
			// decision was made on.
			Analyze();
		}

		private void Analyze()
		{
			m_report = BuildDrift().ToText();
			m_plan = m_compareSingleSkins ? null : UiStyleConversion.Plan(m_config, m_other);
		}

		private UiStyleConfigDrift BuildDrift()
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
			return GuiToolkit.Editor.EditorAssetUtility.FindScriptableObject<UiStyleConfig>(
				new GuiToolkit.Editor.EditorAssetUtility.AssetSearchOptions
				{
					Folders = new[] { "Assets", "Packages" },
					SearchString = "UiMainStyleConfig",
				});
		}
	}
}
