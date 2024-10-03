#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;


namespace GuiToolkit
{

	public static class EditorFileUtility
	{
		public static string GetApplicationDataDir(bool _withAssetsFolder = false)
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

		public static string GetDirectoryName(string _path)
		{
			return Path.GetDirectoryName(_path).Replace('\\', '/');
		}

		public static string GetUnityPath(string _nativePath, bool _removeExtension = false)
		{
			string nativePath = _nativePath.Replace("\\", "/");
			int idx = _nativePath.IndexOf("/Assets");
			if (idx == -1)
				return string.Empty;

			string result = nativePath.Substring(idx+1);
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
	}
}

#endif
