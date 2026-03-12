using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Manages automatic backup creation and restoration for PO and POT files.
	/// Backups are stored outside the Assets folder to avoid cluttering the Unity project.
	/// </summary>
	public static class PoBackupManager
	{
		/// <summary>Maximum number of backup files retained per source file.</summary>
		public const int MAX_BACKUPS = 10;

		private static string BackupDirectory =>
			Path.Combine(Application.dataPath, "..", "Localization", "Backups");

		/// <summary>
		/// Creates a time-stamped backup of <paramref name="_sourcePath"/> and prunes old backups.
		/// </summary>
		/// <param name="_sourcePath">Absolute path of the file to back up.</param>
		/// <returns>Absolute path of the created backup file, or null on failure.</returns>
		public static string CreateBackup(string _sourcePath)
		{
			if (!File.Exists(_sourcePath))
			{
				UiLog.LogWarning($"PoBackupManager: source file not found: '{_sourcePath}'");
				return null;
			}

			try
			{
				string backupDir = Path.GetFullPath(BackupDirectory);
				Directory.CreateDirectory(backupDir);

				string fileName   = Path.GetFileName(_sourcePath);
				string timestamp  = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
				string backupName = $"{fileName}.{timestamp}.bak";
				string backupPath = Path.Combine(backupDir, backupName);

				File.Copy(_sourcePath, backupPath, overwrite: true);
				PruneBackups(_sourcePath);
				return backupPath;
			}
			catch (Exception e)
			{
				UiLog.LogError($"PoBackupManager: failed to create backup of '{_sourcePath}': {e.Message}");
				return null;
			}
		}

		/// <summary>
		/// Returns all backup paths for <paramref name="_sourcePath"/>, sorted newest first.
		/// </summary>
		/// <param name="_sourcePath">Absolute path of the original file.</param>
		/// <returns>Array of backup file paths; empty if none exist.</returns>
		public static string[] GetBackups(string _sourcePath)
		{
			string backupDir = Path.GetFullPath(BackupDirectory);
			if (!Directory.Exists(backupDir))
				return Array.Empty<string>();

			string prefix = Path.GetFileName(_sourcePath) + ".";
			return Directory.GetFiles(backupDir, prefix + "*.bak")
				.OrderByDescending(p => p)
				.ToArray();
		}

		/// <summary>
		/// Restores <paramref name="_backupPath"/> over <paramref name="_targetPath"/>.
		/// </summary>
		/// <param name="_backupPath">Absolute path of the backup file.</param>
		/// <param name="_targetPath">Absolute path of the file to restore into.</param>
		/// <returns>True on success.</returns>
		public static bool Restore(string _backupPath, string _targetPath)
		{
			if (!File.Exists(_backupPath))
			{
				UiLog.LogError($"PoBackupManager: backup not found: '{_backupPath}'");
				return false;
			}

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_targetPath));
				File.Copy(_backupPath, _targetPath, overwrite: true);
				string unityPath = ConvertToUnityPath(_targetPath);
				if (!string.IsNullOrEmpty(unityPath))
					AssetDatabase.ImportAsset(unityPath);
				return true;
			}
			catch (Exception e)
			{
				UiLog.LogError($"PoBackupManager: restore failed: {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// Removes the oldest backup files for <paramref name="_sourcePath"/>, keeping at most <paramref name="_maxCount"/>.
		/// </summary>
		/// <param name="_sourcePath">Absolute path of the original file.</param>
		/// <param name="_maxCount">Maximum backups to keep.</param>
		public static void PruneBackups(string _sourcePath, int _maxCount = MAX_BACKUPS)
		{
			var backups = GetBackups(_sourcePath);
			for (int i = _maxCount; i < backups.Length; i++)
			{
				try
				{
					File.Delete(backups[i]);
				}
				catch (Exception e)
				{
					UiLog.LogWarning($"PoBackupManager: could not delete old backup '{backups[i]}': {e.Message}");
				}
			}
		}

		private static string ConvertToUnityPath(string _absolutePath)
		{
			string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
			string full     = Path.GetFullPath(_absolutePath).Replace('\\', '/');
			if (full.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
				return "Assets" + full.Substring(dataPath.Length);
			return null;
		}

		[MenuItem(StringConstants.LOCA_BACKUP_MENU_NAME, priority = Constants.LOCA_BACKUP_MENU_PRIORITY)]
		private static void OpenBackupManager()
		{
			PoBackupManagerWindow.Open();
		}
	}

	/// <summary>
	/// Editor window that lists recent PO backups and provides restore functionality.
	/// Open via <c>Gui Toolkit / Localization / Manage PO Backups</c>.
	/// </summary>
	public class PoBackupManagerWindow : EditorWindow
	{
		private string m_backupDir;
		private string[] m_allBackups = Array.Empty<string>();
		private Vector2 m_scroll;
		private string m_statusMessage = string.Empty;

		/// <summary>Opens or focuses the Backup Manager window.</summary>
		public static void Open()
		{
			var window = GetWindow<PoBackupManagerWindow>("PO Backup Manager");
			window.Refresh();
			window.Show();
		}

		private void OnEnable()
		{
			Refresh();
		}

		private void Refresh()
		{
			m_backupDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Localization", "Backups"));
			if (Directory.Exists(m_backupDir))
				m_allBackups = Directory.GetFiles(m_backupDir, "*.bak")
					.OrderByDescending(p => p)
					.ToArray();
			else
				m_allBackups = Array.Empty<string>();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("PO Backup Directory", EditorStyles.boldLabel);
			EditorGUILayout.LabelField(m_backupDir, EditorStyles.miniLabel);
			EditorGUILayout.Space(4);

			if (GUILayout.Button("Refresh"))
				Refresh();

			if (m_allBackups.Length == 0)
			{
				EditorGUILayout.HelpBox("No backups found.", MessageType.Info);
				return;
			}

			EditorGUILayout.LabelField($"Backups ({m_allBackups.Length})", EditorStyles.boldLabel);
			m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

			foreach (var backup in m_allBackups)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(Path.GetFileName(backup), GUILayout.ExpandWidth(true));

				if (GUILayout.Button("Restore", GUILayout.Width(70)))
				{
					if (TryFindOriginalPath(backup, out string originalPath))
					{
						if (EditorUtility.DisplayDialog(
							"Restore Backup",
							$"Restore\n{Path.GetFileName(backup)}\nover\n{originalPath}?",
							"Restore", "Cancel"))
						{
							bool ok = PoBackupManager.Restore(backup, originalPath);
							m_statusMessage = ok ? "Restored successfully." : "Restore failed — see console.";
						}
					}
					else
					{
						m_statusMessage = "Could not determine original path. Restore manually.";
					}
				}

				if (GUILayout.Button("Delete", GUILayout.Width(60)))
				{
					if (EditorUtility.DisplayDialog("Delete Backup", $"Delete {Path.GetFileName(backup)}?", "Delete", "Cancel"))
					{
						try { File.Delete(backup); } catch { }
						Refresh();
					}
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.EndScrollView();

			if (!string.IsNullOrEmpty(m_statusMessage))
			{
				EditorGUILayout.Space(4);
				EditorGUILayout.HelpBox(m_statusMessage, MessageType.Info);
			}
		}

		private static bool TryFindOriginalPath(string _backupPath, out string _originalPath)
		{
			_originalPath = null;
			// Backup name format: {originalFileName}.{yyyy-MM-dd_HH-mm-ss}.bak
			string backupName = Path.GetFileName(_backupPath);
			// Strip the .bak suffix and then the timestamp (last segment after the last dot that matches the timestamp format)
			if (!backupName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
				return false;

			string withoutBak = backupName.Substring(0, backupName.Length - 4);
			// Find the timestamp suffix yyyy-MM-dd_HH-mm-ss (20 chars)
			const int TIMESTAMP_LEN = 19; // "yyyy-MM-dd_HH-mm-ss"
			if (withoutBak.Length <= TIMESTAMP_LEN + 1)
				return false;

			string originalFileName = withoutBak.Substring(0, withoutBak.Length - TIMESTAMP_LEN - 1);
			string resourcesPath = Path.Combine(Application.dataPath, "Resources", originalFileName);
			if (File.Exists(resourcesPath))
			{
				_originalPath = resourcesPath;
				return true;
			}

			// Try to locate in the project
			string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(originalFileName));
			foreach (var guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				if (Path.GetFileName(assetPath).Equals(originalFileName, StringComparison.OrdinalIgnoreCase))
				{
					_originalPath = Path.GetFullPath(assetPath);
					return true;
				}
			}

			return false;
		}
	}
}
