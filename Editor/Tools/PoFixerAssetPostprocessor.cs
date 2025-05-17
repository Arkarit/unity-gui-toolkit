using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// See PoFixer
	/// </summary>
	public class PoFixerAssetPostprocessor : AssetPostprocessor
	{
		public const string PoExtension = ".po";
		public const string PoTxtExtension = ".po.txt";
		private static readonly HashSet<string> s_lastWritten = new();
		
		private void OnPreprocessAsset()
		{
			if (!PoFixer.IsEnabled)
			{
				s_lastWritten.Clear();
				return;
			}
			
			string assetPathLower = assetPath.ToLowerInvariant();
			if (s_lastWritten.Contains(assetPathLower))
			{
				s_lastWritten.Remove(assetPathLower);
				return;
			}
			
			bool isPoTxt = assetPath.EndsWith(".po.txt", StringComparison.OrdinalIgnoreCase);
			bool isPo = assetPath.EndsWith(".po", StringComparison.OrdinalIgnoreCase);
			if (!isPoTxt && !isPo)
				return;
			
			string pathToLoad = assetPath;
			string dir = Path.GetDirectoryName(assetPath)
				.Replace('\\', '/');
			
			string fileNameToSave = Path.GetFileName(assetPath)
				.Replace(".po", "", StringComparison.OrdinalIgnoreCase)
				.Replace(".txt", "", StringComparison.OrdinalIgnoreCase);
			
			string pathToSave = $"{dir}/{fileNameToSave}{(isPo ? PoTxtExtension : PoExtension)}";
			CopyFile(pathToLoad, pathToSave);
		}

		private void CopyFile(string _pathToLoad, string _pathToSave)
		{
			// We have to avoid an endless loop: .po imports create a .po.txt create a .po.txt ...
			s_lastWritten.Add(_pathToSave.ToLowerInvariant());
			File.Copy(_pathToLoad, _pathToSave, true);
			Debug.Log($"Synced '{_pathToLoad}' to '{_pathToSave}'");
		}
	}
}