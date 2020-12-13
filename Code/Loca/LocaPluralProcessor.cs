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
		+ "	public static class LocaPlurals\n"
		+ "	{\n"
		+ "		// Mimicking C bool handling\n"
		+ "		public struct CBool\n"
		+ "		{\n"
		+ "			private int m_value;\n"
		+ "			public static implicit operator bool(CBool _val) => _val.m_value != 0;\n"
		+ "			public static implicit operator int(CBool _val) => _val.m_value;\n"
		+ "			public static implicit operator CBool(int _val) => new CBool(_val);\n"
		+ "			public static implicit operator CBool(bool _val) => new CBool(_val);\n"
		+ "			public CBool(int _val = 0) { m_value = _val; }\n"
		+ "			public CBool(bool _val) { m_value = _val ? 1 : 0; }\n"
		+ "		}\n"
		+ "		\n"
		+ "		public static (int numPluralForms, int pluralIdx) GetPluralIdx(string _languageId, int _number)\n"
		+ "		{\n"
		+ "			int nplurals = 0, n = _number;\n"
		+ "			CBool plural = 0;\n"
		+ "			\n"
		+ "			switch (_languageId)\n"
		+ "			{\n";

		private const string FILE_FOOTER =
		  "			}\n"
		+ "			\n"
		+ "			int numPluralForms = nplurals;\n"
		+ "			int pluralIdx = plural;\n"
		+ "			\n"
		+ "			return (numPluralForms, pluralIdx);\n"
		+ "		}\n"
		+ "	}\n"
		+ "}\n";


		[MenuItem(StringConstants.LOCA_PLURAL_PROCESSOR_MENU_NAME, priority = Constants.LOCA_PLURAL_PROCESSOR_MENU_PRIORITY)]
		public static void Process()
		{
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

			string filePath = UiEditorUtility.GetApplicationDataDir() + UiSettings.UiToolkitRootProjectDir + "/Code/Loca/LocaPlurals.cs";
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