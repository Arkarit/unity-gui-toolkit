using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	//[CreateAssetMenu]
	public class UiSettings : ScriptableObject
	{
		public static readonly string SETTINGS_RUNTIME_DIR = "";
		public static readonly string SETTINGS_RUNTIME_PATH = SETTINGS_RUNTIME_DIR + "/" + SETTINGS_FILE;
		public static readonly string SETTINGS_EDITOR_DIR = "Assets/Resources/" + SETTINGS_RUNTIME_DIR;
		public static readonly string SETTINGS_FILE = "UiSettings.asset";
		public static readonly string SETTINGS_EDITOR_PATH = SETTINGS_EDITOR_DIR + "/" + SETTINGS_FILE;

		public bool m_unloadAdditionalScenesOnPlay = false;
		public string m_scenesPath = "Scenes/";

		private static UiSettings s_instance;

		public static UiSettings Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = Resources.Load<UiSettings>(SETTINGS_RUNTIME_PATH);
					if (s_instance == null)
						s_instance = CreateInstance<UiSettings>();
				}
				return s_instance;
			}
		}

#if UNITY_EDITOR
		public static UiSettings EditorLoad()
		{
			EnsureFolderExists(SETTINGS_EDITOR_DIR);
			UiSettings settings = AssetDatabase.LoadAssetAtPath<UiSettings>(UiSettings.SETTINGS_EDITOR_PATH);
			if (settings == null)
			{
				settings = CreateInstance<UiSettings>();
				EditorSave(settings);
			}
			return settings;
		}

		public static void EditorSave(UiSettings _settings)
		{
			if (!AssetDatabase.Contains(_settings))
				AssetDatabase.CreateAsset(_settings, SETTINGS_EDITOR_PATH);
			AssetDatabase.SaveAssets();
		}

		private static bool EnsureFolderExists( string _unityPath )
		{
			try
			{
				if (!AssetDatabase.IsValidFolder(_unityPath))
				{
					string[] names = _unityPath.Split('/');
					string parentPath = "";
					string folderToCreate;
					if (names.Length == 0)
						return false;
					else
					{
						folderToCreate = names[names.Length - 1];
						if (names.Length > 1)
						{
							parentPath = _unityPath.Substring(0, _unityPath.Length - folderToCreate.Length - 1);
							if (!AssetDatabase.IsValidFolder(parentPath))
								if (!EnsureFolderExists(parentPath))
									return false;
						}
					}

					if (!string.IsNullOrEmpty(folderToCreate))
						AssetDatabase.CreateFolder(parentPath, folderToCreate);
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
#endif
	}
}