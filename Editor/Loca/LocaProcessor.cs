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
#if false
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
#endif
			for (int i = 0;;)
			{
				bool found =
					Evaluate(_path, strings, ref i, "_(", false) ||
					Evaluate(_path, strings, ref i, "__(", false) ||
					Evaluate(_path, strings, ref i, "gettext(", false) ||
					Evaluate(_path, strings, ref i, "_n(", true) ||
					Evaluate(_path, strings, ref i, "ngettext(", true);

				if (!found)
					i += 2;
			}
		}

		private static bool Evaluate( string _path, List<string> _strings, ref int _idx, string _expectedKeyword, bool _expectsPlural )
		{
			// Peek helpers
			bool AreTwoTokensLeft( int _idx ) => _idx <= _strings.Count - 2;
			bool IsOneTokenLeft( int _idx ) => _idx <= _strings.Count - 1;

			bool Error( string _message, int _idx )
			{
				UiLog.LogError($"Loca parsing error '{_message}'\n" +
							   $"in '{_path}' near line {_idx}");
				return false;
			}

			// Peek keyword + locaKey without consuming on mismatch
			if (!AreTwoTokensLeft(_idx))
				return false;

			if (_strings[_idx] == null)
				return Error("null string", _idx);

			string keyword = _strings[_idx].Replace(" ", "");
			string locaKey = _strings[_idx + 1];

			if (!string.Equals(keyword, _expectedKeyword, StringComparison.Ordinal))
				return false;

			// Now consume the two tokens
			_idx += 2;

			if (string.IsNullOrEmpty(locaKey))
				return Error("Syntax error: empty loca key", _idx);

			string locaKeyPlural = null;
			if (_expectsPlural)
			{
				if (!AreTwoTokensLeft(_idx))
					return Error("Unexpected end of file (plural)", _idx);

				string comma = _strings[_idx++];
				string pluralKey = _strings[_idx++];

				if (comma.Trim() != ",")
					return Error("Syntax error: missing ',' in plural", _idx);

				if (string.IsNullOrEmpty(pluralKey))
					return Error("Syntax error: empty plural key", _idx);

				locaKeyPlural = pluralKey;
			}

			string groupKey = null;
			if (AreTwoTokensLeft(_idx) && _strings[_idx].Trim() == ",")
			{
				_idx++; // consume comma
				if (!IsOneTokenLeft(_idx))
					return Error("Unexpected end after group comma", _idx);

				groupKey = _strings[_idx++];
				if (groupKey == string.Empty)
					groupKey = null;
			}

			LocaManager.Instance.EdAddKey(locaKey, locaKeyPlural, groupKey);

			// Found and added Key
			return true;
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
