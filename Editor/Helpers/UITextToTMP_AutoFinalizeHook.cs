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

				if (!ReferencesRewireRegistry.HasRegistryWithEntries(scene))
					return;

				var replacementInfo = EditorCodeUtility.ApplyRewireRegistryIfFound();
				Debug.Log($"[Finalize Text->TMP] Replaced={replacementInfo.replaced}, Rewired={replacementInfo.rewired}, Missing={replacementInfo.missingTargets}");
			}
			catch (Exception ex)
			{
				Debug.LogError("[Finalize Text->TMP] Exception after reload: " + ex.Message);
			}
		}
	}
}