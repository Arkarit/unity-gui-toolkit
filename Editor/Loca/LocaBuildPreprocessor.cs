using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Runs loca-related build preparation steps before every player build:
	/// <list type="bullet">
	/// <item><description>Regenerates <c>uitk_available_languages.txt</c> so the runtime can enumerate languages.</description></item>
	/// <item><description>
	/// When <see cref="UiToolkitConfiguration.AutoPullFromSheetsOnBuild"/> is enabled, pulls the
	/// latest translations from all configured Google Sheets bridges before the build.
	/// Dialogs are suppressed; results appear in the console log.
	/// In Unity 6000+, data is downloaded live from Google Sheets. In older versions, the last
	/// cached data from the bridge asset (committed to git) is used.
	/// </description></item>
	/// </list>
	/// callbackOrder is -1 to ensure this preprocessor runs before client-side preprocessors (order 0)
	/// so PO files are up-to-date before any loca processing.
	/// </summary>
	[EditorAware]
	class LocaBuildPreprocessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => -1;

		public void OnPreprocessBuild( BuildReport _report )
		{
			var config = UiToolkitConfiguration.Instance;
			if (config != null && config.AutoPullFromSheetsOnBuild)
				LocaGettextSheetsSyncer.PullAllFromSheets(true);

			if (LocaManager.Instance is LocaManagerDefaultImpl impl)
				impl.EdWriteAvailableLanguagesFile();
		}
	}
}
