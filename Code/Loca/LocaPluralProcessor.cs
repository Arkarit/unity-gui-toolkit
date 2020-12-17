#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public static class LocaPluralProcessor
	{
		private static int m_numScripts;
		private static int m_currentScriptIdx;

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
			string internalClassProjectPath = UiSettings.UiToolkitRootProjectDir + "/Code/Loca/LocaPlurals.cs";

			string[] allScriptAssetPathGuids = AssetDatabase.FindAssets("LocaPlurals t:Script");

			foreach (string guid in allScriptAssetPathGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				if (assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
					continue;

				if (assetPath.ToLower() == internalClassProjectPath.ToLower())
					continue;

				TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
				if (textAsset == null)
					continue;

				UnityEngine.Object.DestroyImmediate(textAsset, true);
			}

			string internalClassFilePath = UiEditorUtility.GetApplicationDataDir() + internalClassProjectPath;

			string filePath = UiEditorUtility.GetApplicationDataDir() + UiSettings.EditorLoad().m_locaPluralsDir + "/LocaPlurals.cs";
			if (internalClassFilePath == filePath)
			{
				Debug.LogError("Overwrite of internal class not allowed");
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

				bool inPlural = false;
				string pluralFn = "";
				for(;;)
				{
					string line = reader.ReadLine();
					if (line == null)
						break;

					if (line.StartsWith("\"Plural-Forms:"))
					{
						pluralFn = line.Substring(14, line.Length-15).Trim();
						inPlural = true;
						continue;
					}

					if (inPlural)
					{
						if (line.StartsWith("\""))
						{
							pluralFn += line.Substring(1, line.Length-2).Trim();
							continue;
						}
						break;
					}
				}

				if (pluralFn != "")
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
			catch
			{
				Debug.LogError($"Failed to write Plurals.cs at '{filePath}'");
				return;
			}

			AssetDatabase.Refresh();
		}
	}
}
#endif