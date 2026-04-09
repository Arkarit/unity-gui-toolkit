using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor tool that permanently removes obsolete entries from all PO files.
	/// Obsolete entries are those marked with <c>#~</c> — keys that existed in the PO
	/// but are no longer present in the POT template (e.g. renamed or deleted strings).
	/// A backup is created for each modified file before writing.
	/// Invoked via Unity menu: Gui Toolkit / Localization / Remove Obsolete PO Keys.
	/// </summary>
	[EditorAware]
	public static class LocaPoObsoleteRemover
	{
		[MenuItem(StringConstants.LOCA_REMOVE_OBSOLETE_MENU_NAME, priority = Constants.LOCA_REMOVE_OBSOLETE_MENU_PRIORITY)]
		public static void RemoveObsoleteKeys()
		{
			AssetReadyGate.WhenReady(SafeRemoveObsoleteKeys);
		}

		private static void SafeRemoveObsoleteKeys()
		{
			// Find all PO files across all groups.
			var poFiles = LocaCsvExporter.FindPoFiles(null);

			if (poFiles.Count == 0)
			{
				EditorUtility.DisplayDialog("Remove Obsolete PO Keys", "No PO files found.", "OK");
				return;
			}

			// Count obsolete entries for the confirmation dialog.
			int totalObsolete = 0;
			int affectedFiles = 0;

			foreach (var (_, _, filePath) in poFiles)
			{
				string content = File.ReadAllText(filePath, Encoding.UTF8);
				var poFile = PoFile.Parse(content);
				int count = poFile.Entries.Count(e => e.IsObsolete);
				if (count > 0)
				{
					totalObsolete += count;
					affectedFiles++;
				}
			}

			if (totalObsolete == 0)
			{
				EditorUtility.DisplayDialog("Remove Obsolete PO Keys", "No obsolete keys found in any PO file.", "OK");
				return;
			}

			if (!EditorUtility.DisplayDialog(
					"Remove Obsolete PO Keys",
					$"Permanently remove {totalObsolete} obsolete key(s) from {affectedFiles} PO file(s)?\n\n" +
					"Backups will be created before writing.",
					"Remove", "Cancel"))
				return;

			int removedTotal = 0;
			int modifiedFiles = 0;

			foreach (var (_, _, filePath) in poFiles)
			{
				string content = File.ReadAllText(filePath, Encoding.UTF8);
				var poFile = PoFile.Parse(content);

				int before = poFile.Entries.Count;
				poFile.Entries.RemoveAll(e => e.IsObsolete);
				int removed = before - poFile.Entries.Count;

				if (removed > 0)
				{
					PoBackupManager.CreateBackup(filePath);
					File.WriteAllText(filePath, poFile.Serialize(), new UTF8Encoding(false));
					removedTotal += removed;
					modifiedFiles++;
				}
			}

			AssetDatabase.Refresh();

			UiLog.Log($"LocaPoObsoleteRemover: Removed {removedTotal} obsolete key(s) from {modifiedFiles} PO file(s).");
			EditorUtility.DisplayDialog(
				"Remove Obsolete PO Keys",
				$"Removed {removedTotal} obsolete key(s) from {modifiedFiles} PO file(s).", "OK");
		}
	}
}
