using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Ensures that <c>uitk_available_languages.txt</c> is up-to-date before every player build.
	///
	/// Without this file, <see cref="LocaManager.GetAvailableLanguages"/> returns an empty array in
	/// the player (it cannot fall back to <c>AssetDatabase</c> at runtime).  This preprocessor
	/// regenerates the file from the .po assets currently in Resources so that the build always
	/// contains the correct language list regardless of whether "Process Loca" was manually run.
	/// </summary>
	class LocaBuildPreprocessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild( BuildReport _report )
		{
			if (LocaManager.Instance is LocaManagerDefaultImpl impl)
				impl.EdWriteAvailableLanguagesFile();
		}
	}
}
