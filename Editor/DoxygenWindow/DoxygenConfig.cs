/*
Original copyright notice by Jacob Pennock:

/// <summary>
/// <para> A Editor Plugin for automatic doc generation through Doxygen</para>
/// <para> Author: Jacob Pennock (http://Jacobpennock.com)</para>
/// <para> Version: 1.0</para>	 
/// </summary>

Permission is hereby granted, free of charge, to any person  obtaining a copy of this software and associated documentation  files (the "Software"), to deal in the Software without  restriction, including without limitation the rights to use,  copy, modify, merge, publish, distribute, sublicense, and/or sell  copies of the Software, and to permit persons to whom the  Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/


using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit.Editor
{
	/// <summary> 
	/// <para>A small data structure class hold values for making Doxygen config files </para>
	/// </summary>
	///
	[CreateAssetMenu(fileName = nameof(DoxygenConfig), menuName = StringConstants.CREATE_DOXYGEN_CONFIG)]
	public class DoxygenConfig : AbstractSingletonScriptableObject<DoxygenConfig>
	{
		public string Project;
		public string Synopsis = "";
		public string Version = "";
		public string ScriptsDirectory;

		[PathField(isFolder:true, relativeToPath:".")]
		public PathField DocumentDirectory;

		public string Defines;
		public List<string> ExcludePatterns;

		public string UnityProjectID;

		public int SelectedTheme = 1;


		public bool DocsGenerated = false;

		public string ThemeFolder => Path.GetDirectoryName(EditorGeneralUtility.GetCallingScriptPath()) + "/Resources";

		public override void OnEditorInitialize()
		{
			base.OnEditorInitialize();
			UnityProjectID = UnityEditor.PlayerSettings.productName + ":";
		}

		public void SetTheme(int theme)
		{
return;
			EditorFileUtility.EnsureFolderExists(DocumentDirectory.FullPath + "/html");

			switch (theme)
			{
				case 1:
					FileUtil.ReplaceFile(ThemeFolder + "/DarkTheme/doxygen.css",
						DocumentDirectory.FullPath + "/html/doxygen.css");
					FileUtil.ReplaceFile(ThemeFolder + "/DarkTheme/tabs.css",
						DocumentDirectory.FullPath + "/html/tabs.css");
					FileUtil.ReplaceFile(ThemeFolder + "/DarkTheme/img_downArrow.png",
						DocumentDirectory.FullPath + "/html/img_downArrow.png");
					break;
				case 2:
					FileUtil.ReplaceFile(ThemeFolder + "/LightTheme/doxygen.css",
						DocumentDirectory.FullPath + "/html/doxygen.css");
					FileUtil.ReplaceFile(ThemeFolder + "/LightTheme/tabs.css",
						DocumentDirectory.FullPath + "/html/tabs.css");
					FileUtil.ReplaceFile(ThemeFolder + "/LightTheme/img_downArrow.png",
						DocumentDirectory.FullPath + "/html/img_downArrow.png");
					FileUtil.ReplaceFile(ThemeFolder + "/LightTheme/background_navigation.png",
						DocumentDirectory.FullPath + "/html/background_navigation.png");
					break;
			}
		}

	}
}
