// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public static class EditorFileUtility
	{
		private static bool s_isPackage;
		private static string s_rootDir;

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

		public static string GetUnityPath(string _nativePath, bool removeExtension = false)
		{
			string nativePath = _nativePath.Replace("\\", "/");
			int idx = _nativePath.IndexOf("/Assets");
			if (idx == -1)
				return string.Empty;

			string result = nativePath.Substring(idx+1);
			if (removeExtension)
			{
				result = Path.GetDirectoryName(result) + "/" + Path.GetFileNameWithoutExtension(result);
Debug.Log($"result:{result}");
			}

			return result;
		}

		public static string GetDirectoryName(string _path)
		{
			return Path.GetDirectoryName(_path).Replace('\\', '/');
		}

		// creates an asset and ensures that the parent directory exists
		// (Which in a sane engine obviously would be automatically done by the engine, but this is Unity)
		public static void CreateAsset( UnityEngine.Object _object, string _path )
		{
			string directory = GetDirectoryName(_path);
			EditorFileUtility.EnsureFolderExists(directory);
			AssetDatabase.CreateAsset(_object, _path);
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

					folderToCreate = names[names.Length - 1];
					if (names.Length > 1)
					{
						parentPath = _unityPath.Substring(0, _unityPath.Length - folderToCreate.Length - 1);
						if (!AssetDatabase.IsValidFolder(parentPath))
							if (!EnsureFolderExists(parentPath))
								return false;
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


		public static bool IsPackage
		{
			get
			{
				if (string.IsNullOrEmpty(s_rootDir))
				{
					GetUiToolkitRootProjectDir();
					s_isPackage = s_rootDir.StartsWith("Packages");
				}

				return s_isPackage;
			}
		}

		public static string GetUiToolkitRootProjectDir()
		{
			if (s_rootDir == null)
			{
				string[] guids = AssetDatabase.FindAssets("unity-gui-toolkit t:folder");
				if (guids.Length >= 1)
				{
					s_rootDir = AssetDatabase.GUIDToAssetPath(guids[0]) + "/";
				}
				else
				{
					s_rootDir = "Packages/de.phoenixgrafik.ui-toolkit/Runtime/";
				}
			}
			return s_rootDir;
		}
	}
}

#endif