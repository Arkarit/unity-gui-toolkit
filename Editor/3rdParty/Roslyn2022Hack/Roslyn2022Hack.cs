#if UNITY_EDITOR // && !UNITY_6000_0_OR_NEWER
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
internal static class Roslyn2022Hack
{
    private const string DestDir = "Assets/Plugins/Roslyn2022Hack";
    private const string AsmdefFile = "Roslyn2022Hack.asmdef";
    private const string PackageRoslynFolder = "Packages/de.phoenixgrafik.ui-toolkit/Editor/3rdParty/Roslyn";

    private const string GlobalDefine = "UITK_USE_ROSLYN";

    // EditorPrefs keys
    private const string PrefsConsentKey = "Roslyn2022Hack.Consent"; // 1=approved, -1=declined, 0/absent=unknown
    private const string PrefsShownKey   = "Roslyn2022Hack.ConsentShown"; // 1=shown, 0/absent=not shown

    private static readonly string[] Dlls =
    {
        "Microsoft.CodeAnalysis.dll",
        "Microsoft.CodeAnalysis.CSharp.dll",
        "System.Collections.Immutable.dll",
        "System.Reflection.Metadata.dll",
        "System.Runtime.CompilerServices.Unsafe.dll"
    };

    static Roslyn2022Hack()
    {
        // Run shortly after domain reload
        EditorApplication.delayCall += MaybeAutoInstall;
    }

    [MenuItem("Tools/UIToolkit/Install Roslyn (Bridge for <6000)")]
    private static void InstallMenu() => EnsureBridgeInstalled(force: true, showConfirm: true);

    [MenuItem("Tools/UIToolkit/Roslyn Bridge ▶ Reset consent")]
    private static void ResetConsentMenu()
    {
        EditorPrefs.DeleteKey(PrefsConsentKey);
        EditorPrefs.DeleteKey(PrefsShownKey);
        Debug.Log("Roslyn bridge: consent has been reset. The automatic prompt will show once again.");
    }

    private static void MaybeAutoInstall()
    {
        // If an assembly named "Roslyn" is already present, we're done
        if (CompilationPipeline.GetAssemblies().Any(a => a.name == "Roslyn"))
            return;

        // Package source folder available?
        if (!Directory.Exists(PackageRoslynFolder))
            return; // keep quiet in case package path changed

        var shown = EditorPrefs.GetInt(PrefsShownKey, 0) == 1;
        var consent = EditorPrefs.GetInt(PrefsConsentKey, 0);

        if (!shown)
        {
            // Show once
            EditorPrefs.SetInt(PrefsShownKey, 1);

            var ok = EditorUtility.DisplayDialog(
                "Install Roslyn bridge for Unity 2022?",
                "This will copy the Roslyn DLLs into your project (Editor-only),\n" +
                $"create/update {AsmdefFile}, and add the global scripting define '{GlobalDefine}'\n" +
                "to all build target groups.\n" + 
                "Without doing that, some localization features won't be available.\n\nProceed?",
                "Install & set define",
                "Cancel"
            );

            if (!ok)
            {
            }
            EditorPrefs.SetInt(PrefsConsentKey, ok ? 1 : -1);
            consent = ok ? 1 : -1;
        }

        if (consent == 1)
            EnsureBridgeInstalled(force: false, showConfirm: false);
        // if declined, stay quiet until user uses the menu or resets consent
    }

    private static void EnsureBridgeInstalled(bool force, bool showConfirm)
    {
        if (!force && CompilationPipeline.GetAssemblies().Any(a => a.name == "Roslyn"))
            return;

        if (!Directory.Exists(PackageRoslynFolder))
            return;

        if (showConfirm)
        {
            var ok = EditorUtility.DisplayDialog(
                "Install Roslyn bridge",
                $"Copy DLLs from:\n{PackageRoslynFolder}\n\n" +
                $"into:\n{DestDir}\n\n" +
                $"and set define '{GlobalDefine}'?",
                "Install",
                "Cancel"
            );
            if (!ok) return;
        }

        Directory.CreateDirectory(DestDir);

        // Copy DLLs and mark Editor-only
        foreach (var dll in Dlls)
        {
            var src = Path.Combine(PackageRoslynFolder, dll).Replace('\\', '/');
            var dst = Path.Combine(DestDir, dll).Replace('\\', '/');

            if (!File.Exists(src))
            {
                Debug.LogWarning($"Roslyn bridge: source missing: {src}");
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
                Debug.LogError($"Roslyn bridge: failed to copy/import {dll}: {ex.Message}");
            }
        }

        // Create/update asmdef
        var asmdefPath = Path.Combine(DestDir, AsmdefFile).Replace('\\', '/');
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
            .AppendLine(@"    ""System.Runtime.CompilerServices.Unsafe.dll""")
            .AppendLine(@"  ],")
            .AppendLine(@"  ""autoReferenced"": true,")
            .AppendLine(@"  ""noEngineReferences"": false")
            .AppendLine("}")
            .ToString();

        File.WriteAllText(asmdefPath, json);
        AssetDatabase.ImportAsset(asmdefPath, ImportAssetOptions.ForceUpdate);

        // Add global define to all target groups
        AddDefineToAllBuildTargetGroups(GlobalDefine);

        Debug.Log("Installed Roslyn bridge (Unity < 6000) and set scripting define '" + GlobalDefine + "'.");
    }

    private static void AddDefineToAllBuildTargetGroups(string define)
    {
        foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            if (!IsValidGroup(group)) continue;

            try
            {
                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
                if (string.IsNullOrEmpty(defines))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, define);
                }
                else if (!defines.Split(';').Contains(define))
                {
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, $"{defines};{define}");
                }
            }
            catch
            {
                // ignore groups that throw (obsolete/unsupported)
            }
        }
    }

    private static bool IsValidGroup(BuildTargetGroup group)
    {
        // Skip unknown/obsolete pseudo-groups
        if (group == BuildTargetGroup.Unknown) return false;

        // In some 2022 editors, obsolete groups still exist but throw on access; try a cheap probe
        try
        {
            _ = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
#endif
