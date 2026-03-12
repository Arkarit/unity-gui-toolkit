using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Unity <see cref="AssetModificationProcessor"/> that intercepts attempts to save SSoT-linked PO files.
	/// When a save is detected, the user is presented with three options:
	/// <list type="bullet">
	/// <item><description><b>Cancel</b> – removes the file from the save list (file is not saved).</description></item>
	/// <item><description><b>Make Local Copy (Detach)</b> – strips the SSoT header from the file and saves.</description></item>
	/// <item><description><b>Save Anyway (Create Backup)</b> – creates a backup first, then saves.</description></item>
	/// </list>
	/// </summary>
	public class PoSsotProtector : UnityEditor.AssetModificationProcessor
	{
		static string[] OnWillSaveAssets(string[] _paths)
		{
			var filtered = new List<string>(_paths);

			for (int i = filtered.Count - 1; i >= 0; i--)
			{
				string assetPath = filtered[i];
				if (!assetPath.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
					continue;

				string fullPath = Path.GetFullPath(assetPath);
				if (!PoSsotHeader.HasSsotHeader(fullPath))
					continue;

				var info     = PoSsotHeader.ParseHeader(fullPath);
				string label = Path.GetFileName(assetPath);
				string message =
					$"'{label}' is linked to a Spreadsheet SSoT.\n" +
					$"Bridge: {info?.BridgeName ?? "unknown"}\n\n" +
					"Saving will overwrite SSoT-generated content.";

				// 0 = Save Anyway, 1 = Cancel, 2 = Detach
				int choice = EditorUtility.DisplayDialogComplex(
					"SSoT-Linked PO File",
					message,
					"Save Anyway (Create Backup)",
					"Cancel",
					"Make Local Copy (Detach)"
				);

				if (choice == 1) // Cancel
				{
					filtered.RemoveAt(i);
				}
				else if (choice == 0) // Save Anyway with backup
				{
					PoBackupManager.CreateBackup(fullPath);
					// keep in list — Unity proceeds with save
				}
				else if (choice == 2) // Detach
				{
					try
					{
						string content = File.ReadAllText(fullPath, Encoding.UTF8);
						var po = PoFile.Parse(content);
						po.HeaderLines = PoSsotHeader.StripSsotLines(po.HeaderLines);
						File.WriteAllText(fullPath, po.Serialize(), Encoding.UTF8);
					}
					catch (Exception e)
					{
						UiLog.LogError($"PoSsotProtector: failed to strip SSoT header from '{fullPath}': {e.Message}");
					}
					// keep in list — Unity proceeds with save (will overwrite with in-memory version)
				}
			}

			return filtered.ToArray();
		}
	}
}
