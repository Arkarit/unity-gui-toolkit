#if UNITY_6000_0_OR_NEWER
#define UITK_USE_ROSLYN
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor tool that scans scenes, prefabs, and C# scripts for localization keys and generates POT template files.
	/// Invoked via Unity menu: Tools > Loca > Process Loca Keys.
	/// Also processes <see cref="ILocaProvider"/> assets to generate the provider registry JSON.
	/// </summary>
	[EditorAware]
	public static class LocaProcessor
	{
		private static int m_numScripts;
		private static int m_currentScriptIdx;
		private static readonly	EditorAssetUtility.AssetSearchOptions s_options = new()
		{
			Folders = new[]
			{
				"Assets", 
				"Packages/de.phoenixgrafik.ui-toolkit"
			},
			ExcludeFolders = new []
			{
				"Assets/Test"
			}
		};

		
		/// <summary>
		/// Main entry point for the Loca processor.
		/// Scans all scenes, prefabs, ScriptableObjects, and C# scripts for localization keys,
		/// then writes POT template files and processes localization providers.
		/// Displays a progress bar during execution.
		/// </summary>
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

			try
			{
				EditorUtility.DisplayProgressBar("Processing Loca", "Processing scenes", 0);
				EditorAssetUtility.FindAllComponentsInAllScenes<ILocaKeyProvider>(
					(_component, _scene, _scenePath) => { FoundComponent(_component, _scenePath); return true; },
					s_options);
				EditorUtility.DisplayProgressBar("Processing Loca", "Processing prefabs", 0.1f);
				EditorAssetUtility.FindAllComponentsInAllPrefabs<ILocaKeyProvider>(
					(_component, _asset, _assetPath) => { FoundComponent(_component, _assetPath); return true; },
					s_options);
				EditorUtility.DisplayProgressBar("Processing Loca", "Processing scripts", 0.2f);
				EditorAssetUtility.FindAllScriptableObjects<ILocaKeyProvider>(
					_component => FoundComponent(_component, AssetDatabase.GetAssetPath((ScriptableObject)(object)_component)),
					s_options);

				m_numScripts = EditorAssetUtility.FindAllScriptsCount();
				m_currentScriptIdx = 0;

				EditorAssetUtility.FindAllScripts(FoundScript, s_options);

#if UITK_USE_ROSLYN
				ProcessLocaProviders();
#endif
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			LocaManager.Instance.EdWriteKeyData();

			if (UiToolkitConfiguration.Instance.AutoMergePotToPo)
				LocaPoMerger.MergeAfterProcessing();

			if (UiToolkitConfiguration.Instance.AutoSyncAfterMerge)
				AutoSyncBridges();
		}

		private static void AutoSyncBridges()
		{
			string[] guids = AssetDatabase.FindAssets("t:LocaExcelBridge");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var bridge  = AssetDatabase.LoadAssetAtPath<LocaExcelBridge>(path);
				if (bridge == null)
					continue;

				if (bridge.EdSourceType != LocaExcelBridge.SourceType.GoogleDocs || !bridge.CanPush)
					continue;

				LocaGettextSheetsSyncer.PushNewKeysToSheets(bridge);
			}
		}

		/// <summary>
		/// Processes all <see cref="ILocaProvider"/> ScriptableObjects in the project.
		/// Calls <see cref="ILocaProvider.CollectData"/> on each and writes a provider registry JSON
		/// so that <see cref="LocaManagerDefaultImpl"/> can load them at runtime.
		/// Invoked via Unity menu: Tools > Loca > Process Loca Providers.
		/// </summary>
		[MenuItem(StringConstants.LOCA_PROCESSOR_MENU_NAME_PROVIDERS, priority = Constants.LOCA_PROCESSOR_MENU_PRIORITY + 1)]
		public static void ProcessLocaProviders()
		{
#if UITK_USE_ROSLYN

			LocaProviderList locaProviderList = new();
			EditorAssetUtility.FindAllScriptableObjects<ILocaProvider>(locaProvider =>
			{
				var so = (ScriptableObject) locaProvider;
				UiLog.Log($"Processing Loca Provider {AssetDatabase.GetAssetPath(so)}");
				var providerType = locaProvider.GetType();
				var typeName = providerType.AssemblyQualifiedName ?? providerType.FullName;
				locaProviderList.Providers.Add(new LocaProviderEntry { Path = so.name, TypeName = typeName });
				locaProvider.CollectData();
			}, s_options);
			locaProviderList.Save();
#endif
		}

		private static void FoundComponent( ILocaKeyProvider _component, string _sourcePath = null )
		{
			if (!_component.UsesLocaKey)
				return;

			string sourceRef = _sourcePath;
			if (_component is MonoBehaviour mb)
			{
				string goPath = GetTransformPath(mb.transform);
				if (!string.IsNullOrEmpty(goPath))
					sourceRef = $"{_sourcePath} ({goPath})";
			}

			if (_component.UsesMultipleLocaKeys)
			{
				var keys = _component.LocaKeys;
				foreach (var key in keys)
					LocaManager.Instance.EdAddKey(key, null, _component.Group, sourceRef);

				return;
			}

			string locaKey = _component.LocaKey;
			string group = _component.Group;
			if (!string.IsNullOrEmpty(locaKey))
				LocaManager.Instance.EdAddKey(locaKey, null, group, sourceRef);
		}

		private static string GetTransformPath( Transform _t )
		{
			var parts = new StringBuilder();
			while (_t != null)
			{
				if (parts.Length > 0)
					parts.Insert(0, '/');
				parts.Insert(0, _t.name);
				_t = _t.parent;
			}
			return parts.ToString();
		}

		private static void FoundScript( string _path, string _content )
		{
			float progress = ((float)m_currentScriptIdx / (float)m_numScripts) * .8f + .2f;
			EditorUtility.DisplayProgressBar("Processing Loca", $"Processing script '{Path.GetFileName(_path)}'", progress);
			m_currentScriptIdx++;

			List<string> strings = EditorCodeUtility.SeparateCodeAndStrings(_content);
			//DebugDump(_path, strings);

			string[] lines = _content.Split('\n');

			int numStrings = strings.Count;
			for (int i = 0; i < numStrings; )
			{
				bool found =
					Evaluate(_path, strings, lines, ref i, "__(", false) ||
					Evaluate(_path, strings, lines, ref i, "_(", false) ||
					Evaluate(_path, strings, lines, ref i, "gettext(", false) ||
					Evaluate(_path, strings, lines, ref i, "_n(", true) ||
					Evaluate(_path, strings, lines, ref i, "ngettext(", true);

				if (!found)
					i += 2;
			}
		}

		private static bool Evaluate( string _path, List<string> _strings, string[] _lines, ref int _idx, string _expectedKeyword, bool _expectsPlural )
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

			string keyword = _strings[_idx].RemoveWhitespace();
			string locaKey = _strings[_idx + 1];

			if (!keyword.EndsWith(_expectedKeyword, StringComparison.Ordinal))
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
			if (AreTwoTokensLeft(_idx))
			{
				var s = _strings[_idx].Trim();
				if (s.StartsWith(",") && !s.Contains(")"))
				{
					_idx++; // consume comma
					if (!IsOneTokenLeft(_idx))
						return Error("Unexpected end after group comma", _idx);

					groupKey = _strings[_idx++];
					if (groupKey == string.Empty)
						groupKey = null;
				}
			}

			LocaManager.Instance.EdAddKey(locaKey, locaKeyPlural, groupKey, GetSourceRefForScript(_path, _lines, locaKey));

			// Found and added Key
			return true;
		}

		private static string GetSourceRefForScript( string _path, string[] _lines, string _key )
		{
			if (_lines == null || string.IsNullOrEmpty(_key))
				return _path;

			// Search for a line containing the key as a quoted string literal
			string keyStart = _key.Length > 20 ? _key.Substring(0, 20) : _key;
			string searchTerm = "\"" + keyStart;
			foreach (string line in _lines)
			{
				string trimmed = line.TrimStart();
				if (trimmed.Contains(searchTerm))
				{
					string snippet = trimmed.Length > 30 ? trimmed.Substring(0, 30) : trimmed;
					return $"{_path} ({snippet})";
				}
			}

			return _path;
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
