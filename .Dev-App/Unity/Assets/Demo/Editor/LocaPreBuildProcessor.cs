using System;
using System.Collections.Generic;
using GuiToolkit;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class LocaPreBuildProcessor : IPreprocessBuildWithReport
{
	public int callbackOrder => 0;

	public void OnPreprocessBuild(BuildReport report)
	{
		UiLog.Log("Loca Pre Build Step");

		try
		{
			LocaProcessor.ProcessLocaProviders();
		}
		catch (Exception e)
		{
			// BuildFailedException only accepts a string message, so include full exception details
			throw new BuildFailedException($"Loca pre-build failed: {e}");
		}

		UiLog.Log("Loca Pre Build Step done");

		CheckLocaCoverage();
	}

	private static void CheckLocaCoverage()
	{
		string[] guids = AssetDatabase.FindAssets("t:LocaExcelBridge");
		foreach (string guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var bridge = AssetDatabase.LoadAssetAtPath<LocaExcelBridge>(path);
			if (bridge == null)
				continue;

			var loca = bridge.Localization;
			if (loca == null || loca.Entries == null || loca.Entries.Count == 0)
				continue;

			// Collect configured languages and count their entries
			var entryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (var entry in loca.Entries)
			{
				if (string.IsNullOrEmpty(entry?.LanguageId))
					continue;

				if (!entryCounts.ContainsKey(entry.LanguageId))
					entryCounts[entry.LanguageId] = 0;

				bool hasContent = !string.IsNullOrEmpty(entry.Text) ||
				                  (entry.Forms != null && entry.Forms.Length > 0);
				if (hasContent)
					entryCounts[entry.LanguageId]++;
			}

			foreach (var kvp in entryCounts)
			{
				if (kvp.Value == 0)
					UiLog.LogWarning($"[Loca] '{path}': language '{kvp.Key}' has zero translated entries — check your localization data.");
			}
		}
	}
}
