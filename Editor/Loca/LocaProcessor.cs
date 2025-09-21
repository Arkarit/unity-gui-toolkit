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

			LocaManager.Instance.Clear();

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


			LocaManager.Instance.WriteKeyData();
		}

		private static void FoundComponent( ILocaClient _component )
		{
			if (_component.UsesMultipleLocaKeys)
			{
				var keys = _component.LocaKeys;
				foreach (var key in keys)
					LocaManager.Instance.AddKey(key);

				return;
			}

			string locaKey = _component.LocaKey;
			if (!string.IsNullOrEmpty(locaKey))
				LocaManager.Instance.AddKey(locaKey);
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

				if (Evaluate(code, "_(", str) || Evaluate(code, "__(", str) || Evaluate(code, "gettext(", str))
					continue;

				if (i > numStrings - 4)
					continue;

				string code2 = strings[i + 2];
				string str2 = strings[i + 3];

				if (code2.Trim() != ",")
					continue;

				if (Evaluate(code, "_n(", str, str2) || Evaluate(code, "ngettext(", str, str2))
					i += 2;
			}

		}

		private static bool Evaluate( string _code, string _keyword, string _singular, string _plural = null )
		{
			int codeLength = _code.Length;
			int keywordLength = _keyword.Length;
			if (codeLength < keywordLength)
				return false;

			if (_code.EndsWith(_keyword))
			{
				if (codeLength == keywordLength)
				{
					LocaManager.Instance.AddKey(_singular, _plural);
					return true;
				}

				char c = _code[codeLength - keywordLength - 1];

				if ((char.IsWhiteSpace(c) || !char.IsLetterOrDigit(c)) && c != '_')
				{
					LocaManager.Instance.AddKey(_singular, _plural);
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
