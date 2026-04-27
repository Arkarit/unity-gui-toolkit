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
	/// Requires Unity 6000 or newer for the actual download to work.
	/// </description></item>
	/// </list>
	/// </summary>
	class LocaBuildPreprocessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

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
