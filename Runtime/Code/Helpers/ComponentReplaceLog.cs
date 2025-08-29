#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class ComponentReplaceLog
	{
		/// <summary>
		/// Append a single log line to the dialog-specific log file.
		/// File name equals dialogName; only the message line contains the time.
		/// </summary>
		public static void Log( string _dialogName, string _message )
		{
			string path = GetFilePath(_dialogName);
			string line = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {_message}";
			File.AppendAllText(path, line + "\n");
			Debug.Log(line); // Kommentar entfernen, falls kein Console-Echo gewünscht
		}

		// ProjectRoot/Logs/ComponentReplacement/<DialogName>.log
		private static string GetFilePath( string _dialogName )
		{
			if (string.IsNullOrEmpty(_dialogName))
				throw new InvalidOperationException($"{nameof(ComponentReplaceLog)} needs a valid dialog name");

			_dialogName = Path.ChangeExtension(_dialogName, null).ToLower();
			foreach (char c in Path.GetInvalidFileNameChars())
				_dialogName = _dialogName.Replace(c, '_');

			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string dir = Path.Combine(projectRoot, "Logs", "ComponentReplacement");
			EditorFileUtility.EnsureFolderExists(dir);

			return Path.Combine(dir, _dialogName + ".log");
		}

	}
}
#endif
