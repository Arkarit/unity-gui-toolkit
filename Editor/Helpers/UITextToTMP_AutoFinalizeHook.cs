using System;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[InitializeOnLoad]
	internal static class UITextToTMP_AutoFinalizeHook
	{
		static UITextToTMP_AutoFinalizeHook()
		{
			AssemblyReloadEvents.afterAssemblyReload += OnAfterReload;
		}

		private static void OnAfterReload()
		{
			try
			{
				// If there is a registry in current context, run finalize
				var scene = EditorCodeUtility.GetCurrentContextScene();
				if (!scene.IsValid()) 
					return;

				ReferencesRewireRegistry reg = ReferencesRewireRegistry.Get(scene);
				if (reg == null || reg.Entries == null || reg.Entries.Count == 0)
					return;

				var result = EditorCodeUtility.FinalizeUITextToTMP_Migration_CurrentContext();
				Debug.Log($"[Finalize Text->TMP] Replaced={result.replaced}, Rewired={result.rewired}, Missing={result.missingTargets}");
			}
			catch (Exception ex)
			{
				Debug.LogError("[Finalize Text->TMP] Exception after reload: " + ex.Message);
			}
		}
	}
}