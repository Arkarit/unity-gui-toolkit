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


using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary> 
	/// <para>A small data structure class hold values for making Doxygen config files </para>
	/// </summary>
	///
	[CreateAssetMenu(fileName = nameof(DoxygenConfig), menuName = StringConstants.CREATE_DOXYGEN_CONFIG)]
	public class DoxygenConfig : AbstractSingletonScriptableObject<DoxygenConfig>
	{
		public enum EVersionSource
		{
			Manual,
			FromLatestGitTag,
			FromProject,
		}

		public string Project;
		public string Synopsis;
		public EVersionSource VersionSource;
		[SerializeField] private string m_version;

		[Tooltip("In this directory Doxygen's output is created (in a sub directory called 'html'). It is mandatory to set this field.")]
		[PathField(_isFolder:true, _relativeToPath:".")]
		public PathField OutputDirectory;

		[Tooltip("Optional: Here you can set a MD main page, if you wish. If you leave it empty, the default Doxygen start page is used.")]
		[PathField(_isFolder:false, _relativeToPath:".", _extensions:"md")]
		public PathField OptionalMainPage;
		
		[Tooltip("Doxygen analyzes files in these directories. At least one valid input directory needs to be set.")]
		[PathField(_isFolder:true, _relativeToPath:".")]
		public List<PathField> InputDirectories;

		public List<string> Defines;
		public List<string> ExcludePatterns;
		public bool DocsGenerated => File.Exists(OutputDirectory.FullPath + "/html/index.html");

		public string Version
		{
			get
			{
				switch (VersionSource)
				{
					case EVersionSource.Manual:
						return m_version;
					case EVersionSource.FromLatestGitTag:
						return GetGitVersion();
					case EVersionSource.FromProject:
						return Application.version;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private string GetGitVersion()
		{
			var currentWorkingDir = Environment.CurrentDirectory;
			var workingFolder = EditorFileUtility.FindGitFolderInParent(currentWorkingDir);
			if (workingFolder == null)
			{
				string s = "Error: No .git folder found";
				Debug.LogError(s);
				return s;
			}

			workingFolder += "/";

			string result = string.Empty;
			int processResult = 0;

			try
			{
				// TODO Mac and Linux version
				processResult = EditorProcessHelper.Run(null, "git.exe", workingFolder, s => result = s, "describe", "--tags", "--abbrev=0");
			}
			catch (Exception e)
			{
				Debug.LogError($"Exception when starting Git:{e.Message}");
				return "Git Exception";
			}

			if (processResult != 0)
			{
				string s = $"Error: Git exited with code {processResult}";
				Debug.LogError(s);
				return s;
			}

			return result;
		}
	}
}
