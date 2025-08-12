#if UNITY_EDITOR && !UNITY_6000_0_OR_NEWER
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

[InitializeOnLoad]
internal static class Roslyn2022Hack
{
    private const string DestDir = "Assets/Plugins/Roslyn2022Hack";
    private const string AsmdefFile = "Roslyn2022Hack.asmdef";
    private const string PackageRoslynFolder = "Packages/de.phoenixgrafik.ui-toolkit/Editor/3rdParty/Roslyn";

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
        // Run after domain reload
//        EditorApplication.delayCall += EnsureBridgeInstalled;
    }

    [MenuItem("Tools/UIToolkit/Install Roslyn (Bridge for <6000)")]
    private static void InstallMenu() => EnsureBridgeInstalled(force:true);

    private static void EnsureBridgeInstalled() => EnsureBridgeInstalled(false);
    private static void EnsureBridgeInstalled(bool force)
    {
        // If an assembly named "Roslyn" is already present, we're done
        if (!force && CompilationPipeline.GetAssemblies().Any(a => a.name == "Roslyn"))
            return;

        // Package source folder available?
        if (!Directory.Exists(PackageRoslynFolder))
            return; // keep quiet in case package path changed

        Directory.CreateDirectory(DestDir);

        // Copy DLLs and mark Editor-only
        foreach (var dll in Dlls)
        {
            var src = Path.Combine(PackageRoslynFolder, dll).Replace('\\','/');
            var dst = Path.Combine(DestDir, dll).Replace('\\','/');

            if (!File.Exists(src))
            {
                Debug.LogWarning($"Roslyn bridge: source missing: {src}");
                continue;
            }

            if (!File.Exists(dst) || new FileInfo(dst).Length != new FileInfo(src).Length)
                FileUtil.ReplaceFile(src, dst);

            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);

            var imp = AssetImporter.GetAtPath(dst) as PluginImporter;
            if (imp != null)
            {
                imp.SetCompatibleWithAnyPlatform(false);
                imp.SetCompatibleWithEditor(true);
                // disable all build targets explicitly
                foreach (BuildTarget t in System.Enum.GetValues(typeof(BuildTarget)))
                    imp.SetCompatibleWithPlatform(t, false);
                imp.SaveAndReimport();
            }
        }

        // Create/update asmdef named "Roslyn2022Hack"
        var asmdefPath = Path.Combine(DestDir, AsmdefFile).Replace('\\','/');
        var json =
@"{
  ""name"": ""Roslyn2022Hack"",
  ""rootNamespace"": """",
  ""references"": [],
  ""includePlatforms"": [ ""Editor"" ],
  ""excludePlatforms"": [],
  ""allowUnsafeCode"": false,
  ""overrideReferences"": true,
  ""precompiledReferences"": [
    ""Microsoft.CodeAnalysis.dll"",
    ""Microsoft.CodeAnalysis.CSharp.dll"",
    ""System.Collections.Immutable.dll"",
    ""System.Reflection.Metadata.dll"",
    ""System.Runtime.CompilerServices.Unsafe.dll""
  ],
  ""autoReferenced"": true,
  ""noEngineReferences"": false
}";
        File.WriteAllText(asmdefPath, json);
        AssetDatabase.ImportAsset(asmdefPath, ImportAssetOptions.ForceUpdate);

        Debug.Log("Installed Roslyn bridge into Assets/Plugins/Roslyn (Unity < 6000).");
    }
}
#endif
