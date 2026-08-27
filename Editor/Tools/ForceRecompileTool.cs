using UnityEditor;
using UnityEditor.Compilation;

namespace GuiToolkit.Editor.Tools
{
	/// <summary>
	/// Forces a script recompile, for the cases where the editor does not notice a change on its own -
	/// a source file written from outside Unity, a package that was re-resolved, or a compile that ended
	/// in a state where nothing rebuilds until something is touched.
	///
	/// Two steps, and both are needed: <see cref="AssetDatabase.Refresh"/> imports files that changed on
	/// disk while Unity was in the background, and <see cref="CompilationPipeline.RequestScriptCompilation"/>
	/// rebuilds even when the importer saw nothing new. Refresh alone is what people usually reach for,
	/// and it is exactly the half that does nothing when the assemblies are stale but the files are not.
	///
	/// Same pair the MCP bridge uses for its "recompile" command; this is the hand-operated door to it.
	/// </summary>
	public static class ForceRecompileTool
	{
		[MenuItem(StringConstants.FORCE_RECOMPILE, false, Constants.FORCE_RECOMPILE_MENU_PRIORITY)]
		public static void ForceRecompile()
		{
			// The domain reload this triggers tears down everything static, this class included. It does
			// not happen inside this call though - RequestScriptCompilation only sets the flag, and the
			// editor acts on it on one of the next ticks - so there is nothing here to guard against it.
			UiLog.Log("Forcing a script recompile.");
			AssetDatabase.Refresh();
			CompilationPipeline.RequestScriptCompilation();
		}

		// A recompile in play mode either kills the session or is silently postponed, depending on the
		// project's setting for it. Neither is what someone clicking this expects, so it is simply off.
		[MenuItem(StringConstants.FORCE_RECOMPILE, true)]
		private static bool ForceRecompileValidation() => !EditorApplication.isPlayingOrWillChangePlaymode;
	}
}
