#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	public static class ComponentReplaceLog
	{
		/// <summary>
		/// Append a single log line to the dialog-specific log file.
		/// File name equals dialogName; only the message line contains the time.
		/// </summary>
		public static void Log(string _message )
		{
			string scenePath = GetLogScenePath();
			if (string.IsNullOrEmpty(scenePath))
			{
				Debug.LogWarning( $"Invalid scene! {_message}" );
				return;
			}
			
			string path = GetFilePath(scenePath);
			string line = $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss}] {_message}";
			File.AppendAllText(path, line + "\n");
			Debug.Log(line);
		}
		
		public static void LogCr(int _howMany)
		{
			string scenePath = GetLogScenePath();
			if (string.IsNullOrEmpty(scenePath))
			{
				Debug.LogWarning( $"Invalid scene!" );
				return;
			}
			
			string path = GetFilePath(scenePath);
			string line = new string('\n', _howMany);
			File.AppendAllText(path, line);
		}
		
		public static string GetLogScenePath()
		{
			var scene = GetCurrentContextScene(out bool isPrefab);
			if (!scene.IsValid())
				return null;
			
			return isPrefab ? PrefabStageUtility.GetCurrentPrefabStage().assetPath : scene.path;
		}

		// ProjectRoot/Logs/ComponentReplacement/<DialogName>.log
		private static string GetFilePath( string _scenePath )
		{
			if (string.IsNullOrEmpty(_scenePath))
				throw new InvalidOperationException($"{nameof(ComponentReplaceLog)} needs a valid dialog name");

			_scenePath = Path.ChangeExtension(_scenePath, null).ToLower();
			foreach (char c in Path.GetInvalidFileNameChars())
				_scenePath = _scenePath.Replace(c, '_');

			string projectRoot = Directory.GetParent(Application.dataPath).FullName;
			string dir = Path.Combine(projectRoot, "Logs", "ComponentReplacement");
			EditorFileUtility.EnsureFolderExists(dir);

			return Path.Combine(dir, _scenePath + ".log");
		}
		
		private static Scene GetCurrentContextScene(out bool _isPrefab)
		{
			_isPrefab = false;
			var stage = PrefabStageUtility.GetCurrentPrefabStage();
			if (stage != null && stage.scene.IsValid())
			{
				_isPrefab = true;
				return stage.scene;
			}
			
			return SceneManager.GetActiveScene();
		}

	}
}
#endif
