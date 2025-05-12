using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;


namespace GuiToolkit.Editor
{
	public class SkipSavingInPackageFolder : AssetModificationProcessor
	{
		private static readonly List<string> s_paths = new();
		
		static string[] OnWillSaveAssets( string[] _paths )
		{
			s_paths.Clear();
			foreach (string path in _paths)
				if (!path.StartsWith("Packages"))
					s_paths.Add(path);
			
			return s_paths.ToArray();
		}
	}

}
