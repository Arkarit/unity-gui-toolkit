using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[EditorAware]
	public static class LocaProcessor
	{
		private static int m_numScripts;
		private static int m_currentScriptIdx;

		[MenuItem(StringConstants.LOCA_PROCESSOR_MENU_NAME, priority = Constants.LOCA_PROCESSOR_MENU_PRIORITY)]
		public static void Process()
		{
			AssetReadyGate.WhenReady
			(
				() => SafeProcess()
			);
		}

		private static void SafeProcess()
		{
			string potPath = UiToolkitConfiguration.Instance.m_potPath;
			if (string.IsNullOrEmpty(potPath))
			{
				UiLog.LogError("No POT path in settings");
				EditorUtility.DisplayDialog("No POT path in settings",
					"Using Loca requires a path to your POT file (translation template) in your settings dialog", "Ok");
				return;
			}

			LocaManager.Instance.EdClear();

			EditorAssetUtility.AssetSearchOptions options = new()
				{ Folders = new[] { "Assets", "Packages/de.phoenixgrafik.ui-toolkit" } };

			try
			{
				EditorUtility.DisplayProgressBar("Processing Loca", "Processing scenes", 0);
				EditorAssetUtility.FindAllComponentsInAllScenes<ILocaClient>(FoundComponent, options);
				EditorUtility.DisplayProgressBar("Processing Loca", "Processing prefabs", 0.1f);
				EditorAssetUtility.FindAllComponentsInAllPrefabs<ILocaClient>(FoundComponent, options);
				EditorUtility.DisplayProgressBar("Processing Loca", "Processing scriptable objects", 0.2f);
				EditorAssetUtility.FindAllScriptableObjects<ILocaClient>(FoundComponent, options);

				m_numScripts = EditorAssetUtility.FindAllScriptsCount();
				m_currentScriptIdx = 0;

				EditorAssetUtility.FindAllScripts(FoundScript, options);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}


			LocaManager.Instance.EdWriteKeyData();
		}

		private static void FoundComponent( ILocaClient _component )
		{
			if (_component.UsesMultipleLocaKeys)
			{
				var keys = _component.LocaKeys;
				foreach (var key in keys)
					LocaManager.Instance.EdAddKey(key);

				return;
			}

			string locaKey = _component.LocaKey;
			if (!string.IsNullOrEmpty(locaKey))
				LocaManager.Instance.EdAddKey(locaKey);
		}

		private static void FoundScript( string _path, string _content )
		{
			float progress = ((float)m_currentScriptIdx / (float)m_numScripts) * .8f + .2f;
			EditorUtility.DisplayProgressBar("Processing Loca", $"Processing script '{Path.GetFileName(_path)}'", progress);
			m_currentScriptIdx++;

			List<string> strings = EditorCodeUtility.SeparateCodeAndStrings(_content);
			//DebugDump(_path, strings);

			int numStrings = strings.Count;

			for (int i = 0; i < numStrings; i += 2)
			{
				if (i > numStrings - 2)
					break;

				string code = strings[i];
				string str = strings[i + 1];

				if (EvaluateDeprecated(code, "_(", str, null) || EvaluateDeprecated(code, "__(", str, null) || EvaluateDeprecated(code, "gettext(", str, null))
					continue;

				if (i > numStrings - 4)
					continue;

				string code2 = strings[i + 2];
				string str2 = strings[i + 3];

				if (code2.Trim() != ",")
					continue;

				if (EvaluateDeprecated(code, "_n(", str, str2) || EvaluateDeprecated(code, "ngettext(", str, str2))
					i += 2;
			}

		}

		private static bool Evaluate(string _path, List<string> strings, ref int i, string _keyword, bool _isPluralKeyword)
		{
			if (AtEnd(i))
				return false;

			string keyword = strings[i++];
			string locaKey = strings[i++];

			if (!keyword.EndsWith(_keyword, StringComparison.Ordinal))
				return AtEnd(i);

			if (string.IsNullOrEmpty(locaKey))
				return Error("Syntax error: empty loca key", i);

			string locaKeyPlural = null;
			if (_isPluralKeyword)
			{
				if (AtEnd(i))
					return Error("Unexpected end of file", i);

				string comma = strings[i++];
				string pluralKey = strings[i++];

				if (comma.Trim() != ",")
					return Error("Syntax error: missing ',' in plural", i);

				if (string.IsNullOrEmpty(pluralKey))
					return Error("Syntax error: empty plural loca key in plural", i);

				locaKeyPlural = pluralKey;
			}

			string groupKey = null;
			if (!AtEnd(i))
			{
				if (strings[i].Trim() == ",")
				{
					i++;
					groupKey = strings[i++];
					if (groupKey == string.Empty)
						groupKey = null;
				}
			}

			LocaManager.Instance.EdAddKey(locaKey, locaKeyPlural, groupKey);
			return AtEnd(i);

			bool AtEnd(int _idx) => _idx >= strings.Count - 2;

			bool Error(string _message, int _idx)
			{
				UiLog.LogError($"Loca parsing error '{_message}'\nin '{_path}' near line {_idx}");
				return AtEnd(_idx);
			}
		}

		private static bool EvaluateDeprecated( string _code, string _keyword, string _singular, string _plural )
		{
			int codeLength = _code.Length;
			int keywordLength = _keyword.Length;
			if (codeLength < keywordLength)
				return false;

			if (_code.EndsWith(_keyword))
			{
				if (codeLength == keywordLength)
				{
					LocaManager.Instance.EdAddKey(_singular, _plural);
					return true;
				}

				char c = _code[codeLength - keywordLength - 1];

				if ((char.IsWhiteSpace(c) || !char.IsLetterOrDigit(c)) && c != '_')
				{
					LocaManager.Instance.EdAddKey(_singular, _plural);
					return true;
				}
			}

			return false;
		}

		private static void DebugDump( string _path, List<string> _strings )
		{
			string outPath = "C:\\temp\\LocaProcessorTest\\";
			try
			{
				Directory.CreateDirectory(outPath);
			}
			catch
			{
				UiLog.LogError("Failed to create directory");
				return;
			}
			outPath += _path.Replace("/", "_") + ".txt";

			string str = "";
			for (int i = 0; i < _strings.Count; i++)
			{
				if ((i & 1) == 0)
					str += "\n\n---Program---\n";
				else
					str += "\n\n---String---\n";

				str += _strings[i];
			}

			try
			{
				File.WriteAllText(outPath, str);
			}
			catch
			{
				UiLog.LogError("Failed to write file");
				return;
			}
		}
	}
}
