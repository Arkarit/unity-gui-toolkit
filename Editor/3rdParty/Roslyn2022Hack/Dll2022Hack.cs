#if UNITY_EDITOR && !UNITY_6000_0_OR_NEWERZ
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[InitializeOnLoad]
	internal static class Dll2022Hack
	{
		private const string RoslynDestDir = "Assets/Plugins/Roslyn2022Hack";
		private const string RoslynAsmdefFile = "Roslyn2022Hack.asmdef";
		private const string RoslynPackageFolder = "Packages/de.phoenixgrafik.ui-toolkit/Editor/3rdParty/Roslyn";

		private const string GlobalDefine = "UITK_USE_ROSLYN";

		// EditorPrefs keys
		private const string PrefsConsentKey = "Dll2022Hack.Consent"; // 1=approved, -1=declined, 0/absent=unknown
		private const string PrefsShownKey = "Dll2022Hack.ConsentShown"; // 1=shown, 0/absent=not shown

		private static readonly string[] RoslynDlls =
		{
			"Microsoft.CodeAnalysis.dll",
			"Microsoft.CodeAnalysis.CSharp.dll",
			"System.Collections.Immutable.dll",
			"System.Reflection.Metadata.dll",
			"System.Runtime.CompilerServices.Unsafe.dll",
			"ExcelDataReader.dll",
			"ExcelDataReader.DataSet.dll",
			"System.Text.Encoding.CodePages.dll"
		};

		[MenuItem(StringConstants.DLL_INSTALL_HACK)]
		private static void InstallMenu() => EnsureBridgeInstalled(force: true, showConfirm: true);

		[MenuItem(StringConstants.DLL_REMOVE_HACK)]
		private static void RemoveHack()
		{
			// 1) Remove global define
			foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (!IsValidGroup(group)) continue;

				try
				{
					var defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
					if (string.IsNullOrEmpty(defines))
						continue;

					var list = defines.Split(';').ToList();
					if (list.RemoveAll(d => d == GlobalDefine) > 0)
					{
						UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", list));
					}
				}
				catch
				{
					// ignore invalid groups
				}
			}

			// 2) Delete DLLs in DestDir
			if (Directory.Exists(RoslynDestDir))
			{
				foreach (var dll in RoslynDlls)
				{
					var path = Path.Combine(RoslynDestDir, dll);
					if (File.Exists(path))
					{
						AssetDatabase.DeleteAsset(path.Replace('\\', '/'));
					}
				}

				// 3) Delete asmdef if present
				var asmdefPath = Path.Combine(RoslynDestDir, RoslynAsmdefFile);
				if (File.Exists(asmdefPath))
				{
					AssetDatabase.DeleteAsset(asmdefPath.Replace('\\', '/'));
				}

				AssetDatabase.Refresh();

				// 4) If folder is now empty, delete it
				if (!Directory.EnumerateFileSystemEntries(RoslynDestDir).Any())
				{
					AssetDatabase.DeleteAsset(RoslynDestDir.Replace('\\', '/'));

					// Optionally also remove Plugins folder if empty
					var pluginsDir = Path.GetDirectoryName(RoslynDestDir);
					if (!string.IsNullOrEmpty(pluginsDir) &&
						Directory.Exists(pluginsDir) &&
						!Directory.EnumerateFileSystemEntries(pluginsDir).Any())
					{
						AssetDatabase.DeleteAsset(pluginsDir.Replace('\\', '/'));
					}
				}
			}

			AssetDatabase.Refresh();
			UiLog.LogInternal("Roslyn bridge removed: define cleared, DLLs deleted, folder cleaned up.");
		}

		private static void EnsureBridgeInstalled( bool force, bool showConfirm )
		{
			if (!force && CompilationPipeline.GetAssemblies().Any(a => a.name == "Roslyn"))
				return;

			if (!Directory.Exists(RoslynPackageFolder))
				return;

			if (showConfirm)
			{
				var ok = EditorUtility.DisplayDialog
				(
					"Install Roslyn bridge",
					$"Copy DLLs from packages\n\n" +
					$"into plugins\n\n" +
					$"and set define '{GlobalDefine}'?",
					"Install",
					"Cancel"
				);
				if (!ok) return;
			}

			Directory.CreateDirectory(RoslynDestDir);

			// Copy DLLs and mark Editor-only
			foreach (var dll in RoslynDlls)
			{
				var src = Path.Combine(RoslynPackageFolder, dll).Replace('\\', '/');
				var dst = Path.Combine(RoslynDestDir, dll).Replace('\\', '/');

				if (!File.Exists(src))
				{
					UiLog.LogWarning($"Roslyn bridge: source missing: {src}");
					continue;
				}

				try
				{
					var needCopy = !File.Exists(dst) || new FileInfo(dst).Length != new FileInfo(src).Length;
					if (needCopy)
					{
						FileUtil.ReplaceFile(src, dst);
					}

					AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);

					if (AssetImporter.GetAtPath(dst) is PluginImporter imp)
					{
						imp.SetCompatibleWithAnyPlatform(false);
						imp.SetCompatibleWithEditor(true);
						// disable all build targets explicitly
						foreach (BuildTarget t in Enum.GetValues(typeof(BuildTarget)))
							imp.SetCompatibleWithPlatform(t, false);
						imp.SaveAndReimport();
					}
				}
				catch (Exception ex)
				{
					UiLog.LogError($"Roslyn bridge: failed to copy/import {dll}: {ex.Message}");
				}
			}

			// Create/update asmdef
			var asmdefPath = Path.Combine(RoslynDestDir, RoslynAsmdefFile).Replace('\\', '/');
			var json = new StringBuilder()
				.AppendLine("{")
				.AppendLine(@"  ""name"": ""Roslyn2022Hack"",")
				.AppendLine(@"  ""rootNamespace"": """",")
				.AppendLine(@"  ""references"": [],")
				.AppendLine(@"  ""includePlatforms"": [ ""Editor"" ],")
				.AppendLine(@"  ""excludePlatforms"": [],")
				.AppendLine(@"  ""allowUnsafeCode"": false,")
				.AppendLine(@"  ""overrideReferences"": true,")
				.AppendLine(@"  ""precompiledReferences"": [")
				.AppendLine(@"    ""Microsoft.CodeAnalysis.dll"",")
				.AppendLine(@"    ""Microsoft.CodeAnalysis.CSharp.dll"",")
				.AppendLine(@"    ""System.Collections.Immutable.dll"",")
				.AppendLine(@"    ""System.Reflection.Metadata.dll"",")
				.AppendLine(@"    ""System.Runtime.CompilerServices.Unsafe.dll"",")
				.AppendLine(@"    ""ExcelDataReader.dll"",")
				.AppendLine(@"    ""ExcelDataReader.DataSet.dll"",")
				.AppendLine(@"    ""System.Text.Encoding.CodePages.dll""")
				.AppendLine(@"  ],")
				.AppendLine(@"  ""autoReferenced"": true,")
				.AppendLine(@"  ""noEngineReferences"": false")
				.AppendLine("}")
				.ToString();

			File.WriteAllText(asmdefPath, json);
			AssetDatabase.ImportAsset(asmdefPath, ImportAssetOptions.ForceUpdate);

			// Add global define to all target groups
			AddDefineToAllBuildTargetGroups(GlobalDefine);

			UiLog.LogInternal("Installed Roslyn bridge (Unity < 6000) and set scripting define '" + GlobalDefine + "'.");
		}

		private static void AddDefineToAllBuildTargetGroups( string define )
		{
			foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
			{
				if (!IsValidGroup(group)) continue;

				try
				{
					var defines = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
					if (string.IsNullOrEmpty(defines))
					{
						UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(group, define);
					}
					else if (!defines.Split(';').Contains(define))
					{
						UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(group, $"{defines};{define}");
					}
				}
				catch
				{
					// ignore groups that throw (obsolete/unsupported)
				}
			}
		}

		private static bool IsValidGroup( BuildTargetGroup group )
		{
			// Skip unknown/obsolete pseudo-groups
			if (group == BuildTargetGroup.Unknown) return false;

			// In some 2022 editors, obsolete groups still exist but throw on access; try a cheap probe
			try
			{
				_ = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
#endif
