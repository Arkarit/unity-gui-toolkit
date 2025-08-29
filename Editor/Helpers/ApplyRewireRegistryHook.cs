using System;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Auto-applies the rewiring registry after a domain reload (e.g. after script recompilation).
	/// Runs automatically via static constructor using InitializeOnLoad.
	/// </summary>
	[InitializeOnLoad]
	internal static class ApplyRewireRegistryHook
	{
		/// <summary>
		/// Static constructor adds hook to AssemblyReloadEvents.
		/// </summary>
		static ApplyRewireRegistryHook()
		{
			AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;
		}

		/// <summary>
		/// Called after domain reload to finalize rewiring from registry, if present.
		/// </summary>
		private static void OnAfterReload()
		{
			try
			{
				// Get current context scene (prefab stage or main scene)
				var scene = EditorCodeUtility.GetCurrentContextScene();
				if (!scene.IsValid())
					return;

				// Apply rewiring if registry exists
				if (!EditorCodeUtility.ApplyRewireRegistryIfFound(out int replaced, out int rewired, out int missing))
					return;

				ComponentReplaceLog.Log($"[Finalize Text->TMP] Replaced={replaced}, Rewired={rewired}, Missing={missing}");
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}
	}
}
