using GuiToolkit;
using GuiToolkit.Editor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class LocaPreBuildProcessor : IPreprocessBuildWithReport
{
	public int callbackOrder => 0;
	public void OnPreprocessBuild(BuildReport report)
	{
		UiLog.Log("Loca Pre Build Step");
		LocaProcessor.ProcessLocaProviders();
		UiLog.Log("Loca Pre Build Step done");
	}
}
