#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace GuiToolkit
{

	public static class EditorFileUtility
	{
		public static string GetApplicationDataDir( bool _withAssetsFolder = false )
		{
			string result = Application.dataPath;
			if (_withAssetsFolder)
				return result;

			return result.Replace("Assets", "");
		}

		public static string GetAssetProjectDir( string _assetPath )
		{
			int idx = _assetPath.LastIndexOf("/");
			if (idx == -1)
				return "";

			return _assetPath.Substring(0, idx + 1);
		}

		public static string GetNativePath( string _assetPath )
		{
			return GetApplicationDataDir() + "/" + _assetPath;
		}

		public static string GetDirectoryName( string _path )
		{
			return Path.GetDirectoryName(_path).Replace('\\', '/');
		}

		public static string GetUnityPath(string _nativePath, bool _removeExtension = false) =>
			GetUnityPathInternal(_nativePath, _removeExtension, false);

		public static string GetUnityPathInternal( string _nativePath, bool _removeExtension, bool _fixSymlinks )
		{
			string nativePath = _nativePath.Replace("\\", "/");

			if (_fixSymlinks)
			{
				nativePath = nativePath.Replace("unity-gui-toolkit/Runtime", "unity-gui-toolkit/.Dev-App/Unity/Assets/External/unity-gui-toolkit");
				nativePath = nativePath.Replace("unity-gui-toolkit/Editor", "unity-gui-toolkit/.Dev-App/Unity/Assets/External/unity-gui-toolkit-editor");
			}

			int idx = nativePath.IndexOf("/Assets");
			if (idx == -1)
				return string.Empty;

			string result = nativePath.Substring(idx + 1);
			if (_removeExtension)
			{
				result = Path.GetDirectoryName(result) + "/" + Path.GetFileNameWithoutExtension(result);
			}

			return result;
		}

		public static bool EnsureFolderExists( string _unityPath )
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
		
		public static string PathField( string _label, string _path, bool _save, bool _folder)
		{
			if (_folder)
			{
				if (_save)
				{
					return PathField(_label, _path, () => EditorUtility.SaveFolderPanel(_label, _path, null));
				}
				
				return PathField(_label, _path, () => EditorUtility.OpenFolderPanel(_label, _path, null));
			}
			
			if (_save)
			{
				string dir;
				string name;
				try
				{
					dir = Path.GetDirectoryName(_path);
					name = Path.GetFileName(_path);
				}
				catch
				{
					dir = _path;
					name = null;
				}
				
				return PathField(_label, _path, () => EditorUtility.SaveFilePanel(_label, dir, name, null));
			}
			
			return PathField(_label,_path, () => EditorUtility.OpenFilePanel(_label, _path, null));
		}

		public static string PathFieldSaveFile(string _label, string _path) => PathField(_label, _path, _save:true, _folder:false);
		public static string PathFieldReadFile(string _label, string _path) => PathField(_label, _path, _save:false, _folder:false);
		public static string PathFieldSaveFolder(string _label, string _path) => PathField(_label, _path, _save:true, _folder:true);
		public static string PathFieldReadFolder(string _label, string _path) => PathField(_label, _path, _save:false, _folder:true);
		
		private static string PathField(string _label, string _path, Func<string> _callback)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(_label);
			var result = GUILayout.TextField(_path);
			
			if (GUILayout.Button("...", GUILayout.ExpandWidth(false), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
			{
				string path = _callback();
				if (!string.IsNullOrEmpty(path))
					result = path;
			}

			GUILayout.EndHorizontal();
			return result;
		}
	}
}

#endif
