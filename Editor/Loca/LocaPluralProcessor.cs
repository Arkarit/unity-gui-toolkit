using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[EditorAware]
	public static class LocaPluralProcessor
	{
		private const string FILE_HEADER =
		  "// Auto-generated, please do not change!\n"
		+ "// Use menu '" + StringConstants.LOCA_PLURAL_PROCESSOR_MENU_NAME + "' to add new language plurals!\n"
		+ "namespace GuiToolkit\n"
		+ "{\n"
		+ "	public static partial class LocaPlurals\n"
		+ "	{\n"
		+ "		static partial void GetPluralIdx(string _languageId, int _number, ref int _numPluralForms, ref int _pluralIdx)\n"
		+ "		{\n"
		+ "			int nplurals = 0, n = _number;\n"
		+ "			CBool plural = 0;\n"
		+ "			\n"
		+ "			switch (_languageId)\n"
		+ "			{\n"
		+ "				case \"dev\":\n"
		+ "					nplurals=2; plural=(n != 1);\n"
		+ "					break;\n";

		private const string FILE_FOOTER =
		  "			}\n"
		+ "\n"	
		+ "			_numPluralForms = nplurals;\n"
		+ "			_pluralIdx = plural;\n"
		+ "		}\n"
		+ "	}\n"
		+ "}\n";

		[MenuItem(StringConstants.LOCA_PLURAL_PROCESSOR_MENU_NAME, priority = Constants.LOCA_PLURAL_PROCESSOR_MENU_PRIORITY)]
		public static void Process()
		{
			AssetReadyGate.WhenReady
			(
				() => SafeProcess()
			);
		}
		
		public static void SafeProcess()
		{
			string internalClassProjectPath = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir() + "Code/Loca/LocaPlurals.cs";
			string internalClassFilePath = EditorFileUtility.GetApplicationDataDir() + internalClassProjectPath;
			string userClassProjectDir = UiToolkitConfiguration.Instance.GeneratedAssetsDir + "/";
			string userClassProjectPath = userClassProjectDir + "LocaPlurals.cs";
			string nativeAsmrefDir = EditorFileUtility.GetNativePath(UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir()) + "Misc/";
			string nativeAsmrefSource = nativeAsmrefDir + "unity-gui-toolkit.asmref";
			string nativeAsmrefTarget = userClassProjectDir + "unity-gui-toolkit.asmref";

			string filePath = EditorFileUtility.GetApplicationDataDir() + userClassProjectPath;

			if (internalClassFilePath == filePath)
			{
				UiLog.LogError("Overwrite of internal class not allowed");
				EditorUtility.DisplayDialog("Overwrite of internal class not allowed", "Please change 'Loca Plurals Dir' in the settings so that it doesn't have the same location as the internal\n" +
					"counterpart of this class.", "Ok");
				return;
			}

			string[] allAssetPathGuids = AssetDatabase.FindAssets(".po t:TextAsset");

			string fileContent = FILE_HEADER;

			foreach (string guid in allAssetPathGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
				StringReader reader = new StringReader(textAsset.text);

				string language = Path.GetFileName(assetPath);
				language = language.Substring(0, language.Length-7).Trim();

				string pluralFn = string.Empty;
				bool inConcatenation = false;
				for(;;)
				{
					string line = reader.ReadLine();
					if (line == null)
						break;

					if (inConcatenation)
					{
						if (line.Length < 2)
							break;

						pluralFn += line.Substring(1, line.Length - 2);
						if (pluralFn.EndsWith("\\n"))
							break;

						continue;
					}

					if (line.StartsWith("\"Plural-Forms:"))
					{
						pluralFn = line.Substring(14, line.Length-15).Trim();
						if (!pluralFn.EndsWith("\\n"))
						{
							inConcatenation = true;
							continue;
						}

						break;
					}
				}

				if (!string.IsNullOrEmpty(pluralFn))
				{
					while (pluralFn.EndsWith("\\n"))
						pluralFn = pluralFn.Substring(0, pluralFn.Length-2);

					fileContent += 
						  "				case \"" + language + "\":\n"
						+ "					" + pluralFn + "\n"
						+ "					break;\n";
				}
			}

			fileContent += FILE_FOOTER;

			try
			{
				File.WriteAllText(filePath, fileContent);
			}
			catch( Exception e)
			{
				UiLog.LogError($"Failed to write Plurals.cs at '{filePath}': {e.Message}");
				return;
			}

			// We only copy the file itself to avoid meta nuisance
			File.Copy(nativeAsmrefSource, nativeAsmrefTarget, true);

			UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
		}
	}
}
