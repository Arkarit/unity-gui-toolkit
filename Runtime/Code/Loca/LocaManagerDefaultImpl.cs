using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class LocaManagerDefaultImpl : LocaManager
	{
		private const string GROUPS_RESOURCE_NAME = "uitk_loca_groups";
		private const string DEFAULT_LOCA_GROUP = "__default__";
		private bool m_isDev = true;

		// Key 0: Group Key 1: key
		private readonly Dictionary<string, Dictionary<string, string>> m_translationDict = new();
		private readonly Dictionary<string, Dictionary<string, List<string>>> m_translationDictPlural = new();
		private List<string> m_groups;

		public override bool ChangeLanguageImpl( string _language )
		{
			if (string.IsNullOrEmpty(_language))
			{
				UiLog.LogError("null/Empty language Id");
				return false;
			}

			if (DebugLoca)
				Log($"Language changed: '{_language}'");

			m_isDev = _language == "dev";
			if (m_isDev)
			{
				m_translationDict.Clear();
				m_translationDictPlural.Clear();
				return true;
			}

			return ReadTranslation(_language);
		}

		private bool ReadTranslation( string _languageId )
		{
			m_translationDict.Clear();
			m_translationDictPlural.Clear();
			m_groups = AssetUtility.ReadLines(GROUPS_RESOURCE_NAME);

			bool result = ReadTranslation(_languageId, null);
			foreach (var group in m_groups)
				result |= ReadTranslation(_languageId, group);

			DebugDump();
			return result;
		}


		private void SetEffectiveGroup( ref string _group )
		{
			if (string.IsNullOrEmpty(_group))
				_group = DEFAULT_LOCA_GROUP;
		}

		private bool ReadTranslation( string _languageId, string _group )
		{
			SetEffectiveGroup(ref _group);
			string[] lines = LoadPo(_languageId, _group);

			for (int i = 0; i < lines.Length; i++)
			{
				string line1 = lines[i];

				if (line1.StartsWith("msgid"))
				{
					if (i >= lines.Length - 1)
					{
						UiLog.LogError("Malformed PO file");
						break;
					}

					string line2 = lines[i + 1];
					if (line2.StartsWith("msgstr"))
					{
						string cleanKey = Unescape(line1.Substring(7, line1.Length - 8));
						string cleanValue = Unescape(line2.Substring(8, line2.Length - 9));
						Add(_group, cleanKey, cleanValue);
						i++;
						continue;
					}

					Debug.Assert(line2.StartsWith("msgid_plural"));
					if (!line2.StartsWith("msgid_plural"))
						continue;

					string cleanKeySingular = Unescape(line1.Substring(7, line1.Length - 8));
					string cleanKeyPlural = Unescape(line2.Substring(14, line2.Length - 15));

					i += 1;
					if (i >= lines.Length - 1)
					{
						UiLog.LogError("Malformed PO file");
						break;
					}

					List<string> currentPlurals = new List<string>();
					while (i + 1 < lines.Length && lines[i + 1].StartsWith("msgstr["))
					{
						currentPlurals.Add(Unescape(lines[i + 1].Substring(11, lines[i + 1].Length - 12)));
						i++;
					}

					if (currentPlurals.Count < 1)
					{
						UiLog.LogError("Malformed PO file");
						continue;
					}

					Add(_group, cleanKeySingular, currentPlurals[0], cleanKeyPlural, currentPlurals);
				}
			}

			//DebugDump();

			return true;
		}


		private void Add( string _group, string _key, string _value, string _keyPlural = null, List<string> _plurals = null )
		{
			SetEffectiveGroup(ref _group);

			if (!m_translationDict.ContainsKey(_group))
				m_translationDict.Add(_group, new Dictionary<string, string>());

			var groupDict = m_translationDict[_group];
			if (groupDict.TryGetValue(_key, out string existingValue))
			{
				if (existingValue != _value)
					UiLog.LogWarning($"Group '{_group}': Multiple Key '{_key}', existing:'{existingValue}', new value:'{_value}'. Ignoring new value.");
			}
			else
			{
				groupDict.Add(_key, _value);
			}

			if (_plurals == null)
				return;


			if (!m_translationDictPlural.ContainsKey(_group))
				m_translationDictPlural.Add(_group, new Dictionary<string, List<string>>());

			var groupDictPlural = m_translationDictPlural[_group];
			if (groupDictPlural.TryGetValue(_keyPlural, out List<string> existingValues))
			{
				if (existingValues == null)
				{
					UiLog.LogError($"Group '{_group}': Internal error: Existing plurals list for key '{_keyPlural}' is null!");
					groupDictPlural[_keyPlural] = _plurals;
				}
				else if (existingValues.Count != _plurals.Count)
				{
					UiLog.LogWarning($"Group '{_group}': Multiple Plural Key '{_keyPlural}', existing values length mismatch:"
									 + $"{existingValues.Count} entries, new values:{_plurals.Count} entries. Ignoring new values.");
				}
				else
				{
					for (int i = 0; i < existingValues.Count; i++)
					{
						if (existingValues[i] != _plurals[i])
						{
							if (existingValue != _value)
								UiLog.LogWarning($"Group '{_group}': Multiple Plural Key '{_keyPlural}' {i}, " +
												 $"existing:'{existingValues[i]}', new value:'{_plurals[i]}'. Ignoring new value.");
						}
					}
				}
			}
			else
			{
				groupDictPlural.Add(_keyPlural, _plurals);
			}
		}

		// removes empty lines, concatenates strings
		private string[] CleanUpLines( string[] _lines )
		{
			List<string> result = new List<string>();

			int lastKeyword = -1;
			for (int i = 0; i < _lines.Length; i++)
			{
				string line = _lines[i].Trim();
				if (line.StartsWith("#")
					|| line.Length <= 2)
				{
					_lines[i] = null;
					continue;
				}
				if (line.StartsWith("\""))
				{
					if (lastKeyword == -1)
					{
						UiLog.LogError("Malformed PO file");
						continue;
					}

					_lines[lastKeyword] = _lines[lastKeyword].Substring(0, _lines[lastKeyword].Length - 1) + line.Substring(1, line.Length - 1);
					_lines[i] = null;
					continue;
				}

				lastKeyword = i;
			}

			foreach (var str in _lines)
			{
				if (str != null)
					result.Add(str);
			}

			return result.ToArray();
		}

		public override string Translate( string _s, string _group = null )
		{
			SetEffectiveGroup(ref _group);

			if (string.IsNullOrEmpty(_s))
			{
				if (DebugLoca)
					Log("Empty key - intentional?");

				return "";
			}

			if (m_isDev)
				return _s;

			if (TryGetTranslation(_group, _s, out string result))
			{
				if (string.IsNullOrEmpty(result))
					result = DebugLoca ? $"#{_s}" : _s;

				return Regex.Unescape(result);
			}

			if (DebugLoca)
				_s = $"*{_s}";

			return _s;
		}

		private bool TryGetTranslation( string _group, string _key, out string _result )
		{
			_result = string.Empty;
			if (!m_translationDict.TryGetValue(_group, out var entry))
				return false;

			return entry.TryGetValue(_key, out _result);
		}

		public override string Translate( string _singularKey, string _pluralKey, int _n, string _group = null )
		{
			SetEffectiveGroup(ref _group);

			(int numPluralForms, int pluralIdx) = LocaPlurals.GetPluralIdx(Language, _n);
			if (pluralIdx == 0)
				return Translate(_singularKey);

			if (m_isDev)
				return _pluralKey;

			if (TryGetTranslation(_group, _pluralKey, out List<string> plurals))
			{
				if (pluralIdx < plurals.Count)
				{
					var result = plurals[pluralIdx];
					if (string.IsNullOrEmpty(result))
						result = DebugLoca ? $"_{pluralIdx}_{_pluralKey}" : "";

					return result;
				}
			}

			_pluralKey = $"*{_pluralKey}";
			return _pluralKey;
		}

		private bool TryGetTranslation( string _group, string _key, out List<string> result )
		{
			result = new List<string>();
			if (!m_translationDictPlural.TryGetValue(_group, out var entry))
				return false;

			return entry.TryGetValue(_key, out result);
		}

		private void Log( string _s )
		{
			UiLog.Log($"Debug Loca:{_s}");
		}

		private string Escape( string _s )
		{
			_s = _s.Replace("\"", "\\\"");
			_s = _s.Replace("\n", "\\n");
			_s = _s.Replace("\r", "\\r");
			return _s;
		}

		private string Unescape( string _s )
		{
			_s = _s.Replace("\\\"", "\"");
			_s = _s.Replace("\\n", "\n");
			_s = _s.Replace("\\r", "\r");
			return _s;
		}

		private string GetPoUnityPath( string _languageId, string _group )
		{
			_group = _group == null || _group == DEFAULT_LOCA_GROUP ? string.Empty : "_" + _group;
			return $"{_languageId}{_group}.po";
		}

		private string[] LoadPo( string _languageId, string _group )
		{
			var path = GetPoUnityPath(_languageId, _group);
			if (path == null)
				return null;

			TextAsset text = Resources.Load<TextAsset>($"{_languageId}{_group}.po");
			if (text == null)
				return null;

			string[] lines = text.text.Split(new[] { '\r', '\n' });
			return CleanUpLines(lines);
		}


#if UNITY_EDITOR
		private readonly SortedDictionary<string, SortedSet<string>> m_keys = new();
		private readonly SortedDictionary<string, SortedDictionary<string, string>> m_pluralKeys = new();

		private string[] m_availableLanguages = null;

		public override string[] EdAvailableLanguages
		{
			get
			{
				if (m_availableLanguages == null)
				{
					var availableLanguages = new HashSet<string> { "dev" };

					string[] guids = AssetDatabase.FindAssets(".po t:textasset");
					for (int i = 0; i < guids.Length; i++)
					{
						string guid = guids[i];
						string assetPath = AssetDatabase.GUIDToAssetPath(guid);
						string language = Path.GetFileNameWithoutExtension(assetPath);
						availableLanguages.Add(language.Substring(0, language.Length - 3));
					}

					m_availableLanguages = availableLanguages.ToArray();
				}

				return m_availableLanguages;
			}
		}

		public override void EdClear()
		{
			m_keys.Clear();
			m_pluralKeys.Clear();
		}

		public override void EdAddKey( string _singularKey, string _pluralKey = null, string _group = null )
		{
			SetEffectiveGroup(ref _group);

			if (string.IsNullOrEmpty(_singularKey))
				return;

			if (_pluralKey != null)
			{
				if (DebugLoca)
					Log($"Adding plural key '{_singularKey}', '{_pluralKey}'");

				Debug.Assert(!Application.isPlaying);
				if (!m_pluralKeys.ContainsKey(_group))
					m_pluralKeys.Add(_group, new SortedDictionary<string, string>());

				var groupEntryPlural = m_pluralKeys[_group];
				groupEntryPlural.Add(_singularKey, _pluralKey);
				return;
			}

			if (DebugLoca)
				Log($"Adding key '{_singularKey}'");

			Debug.Assert(!Application.isPlaying);
			if (!m_keys.ContainsKey(_group))
				m_keys.Add(_group, new SortedSet<string>());

			var groupEntry = m_keys[_group];
			groupEntry.Add(_singularKey);
		}

		public override void EdReadKeyData()
		{
			if (DebugLoca)
				Log("Reading POT files");

			m_keys.Clear();

			var groups = AssetUtility.ReadLines(GROUPS_RESOURCE_NAME);
			EdReadKeyData(DEFAULT_LOCA_GROUP);
			foreach (var group in groups)
				EdReadKeyData(group);
		}

		private string EdGetPotSystemPath( string _group )
		{
			var result = EditorFileUtility.GetApplicationDataDir() + UiToolkitConfiguration.Instance.m_potPath;
			if (File.Exists(result))
				result = Path.GetDirectoryName(result);
			else if (!Directory.Exists(result))
				return null;

			string groupAppendix = string.Empty;
			if (!string.IsNullOrEmpty(_group) && _group != DEFAULT_LOCA_GROUP)
				groupAppendix = $"_{_group}";

			result += $"/loca{groupAppendix}.pot";
			result = EditorFileUtility.GetSafeFileName(result);
			return result;
		}

		private void EdReadKeyData( string _group )
		{
			var path = EdGetPotSystemPath(_group);
			if (string.IsNullOrEmpty(path))
				return;

			if (DebugLoca)
				Log($"Read POT file at '{path}'");

			if (!m_keys.ContainsKey(_group))
				m_keys.Add(_group, new SortedSet<string>());

			var keys = m_keys[_group];

			try
			{
				string[] lines = File.ReadAllLines(path);
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i];

					if (!line.StartsWith("msgid"))
						continue;

					line = line.Substring(7, line.Length - 8);
					line = Unescape(line);

					if (i < lines.Length - 1)
					{
						string line2 = lines[i + 1];
						if (line2.StartsWith("msgid_plural"))
						{
							line2 = line2.Substring(14, line.Length - 15);
							line2 = Unescape(line);
							if (DebugLoca)
								Log($"Adding POT plural key '{line}', '{line2}'");

							if (!m_pluralKeys.ContainsKey(_group))
								m_pluralKeys.Add(_group, new SortedDictionary<string, string>());

							var pluralKeys = m_pluralKeys[_group];
							pluralKeys.Add(line, line2);
							i += 1;
							continue;
						}
					}

					if (DebugLoca)
						Log($"Adding POT key '{line}'");
					keys.Add(line);
				}

				if (DebugLoca)
					Log("Success");
			}
			catch (Exception e)
			{
				// This is not necessarily an error, since it may just not exist yet.
				UiLog.LogWarning($"Could not read POT file at '{path}':'{e.Message}'");
			}
		}

		public override void EdWriteKeyData()
		{
			string groups = string.Empty;

			foreach (var kv in m_keys)
			{
				var path = EdGetPotSystemPath(kv.Key);
				m_keys.TryGetValue(kv.Key, out var keys);
				m_pluralKeys.TryGetValue(kv.Key, out var pluralKeys);
				WriteKeyData(path, keys, pluralKeys);
				if (kv.Key != DEFAULT_LOCA_GROUP)
					groups += $"{kv.Key}\n";
			}

			if (!string.IsNullOrEmpty(groups))
			{
				var groupAsset = new TextAsset(groups);
				AssetDatabase.CreateAsset(groupAsset, $"Assets/Resources/GROUPS_RESOURCE_NAME");
			}
		}

		private void WriteKeyData(string _path, SortedSet<string> _keys, SortedDictionary<string, string> _pluralKeys)
		{
			if (DebugLoca)
				Log($"Write POT file at '{_path}'");

			try
			{
				string dir = Path.GetDirectoryName(_path);
				Directory.CreateDirectory(dir);

				string s = "";

				if (_keys != null)
				{
					foreach (string key in _keys)
					{
						string cleanKey = Escape(key);
						s +=
							$"msgid \"{cleanKey}\"\n"
							+ $"msgstr \"\"\n\n";
					}
				}

				if (_pluralKeys != null)
				{
					foreach (var kv in _pluralKeys)
					{
						string cleanSingular = Escape(kv.Key);
						string cleanPlural = Escape(kv.Value);
						s +=
							$"msgid \"{cleanSingular}\"\n"
							+ $"msgid_plural \"{cleanPlural}\"\n";

						for (int i = 0; i < 4; i++)
							s += $"msgstr[{i}] \"\"\n";

						s += "\n";
					}
				}

				File.WriteAllText(_path, s);

				AssetDatabase.Refresh();
				if (DebugLoca)
					Log("Success");
			}
			catch (Exception e)
			{
				UiLog.LogError($"Write Fail for POT file at '{_path}':'{e.Message}'");
			}
		}

		private void DebugDump()
		{
			string s = $"Language:'{Language}'\n\nSingular:\n";


			foreach (var kvGroup in m_translationDict)
			{
				s += $"Group: '{kvGroup.Key}'\n";
				var groupentry = kvGroup.Value;

				foreach (var kv in groupentry)
				{
					s += "*************************************************\n";
					s += "key:" + kv.Key + "\n";
					s += "-------------------------------------------------\n";
					s += "val:" + kv.Value + "\n\n";
				}
			}

			s += "\n\nPlurals:";
			foreach (var kvGroup in m_translationDictPlural)
			{
				s += $"Group: '{kvGroup.Key}'\n";
				var groupentry = kvGroup.Value;
				foreach (var kv in groupentry)
				{
					s += "*************************************************\n";
					s += "pluralkey:" + kv.Key + "\n";
					s += "-------------------------------------------------\n";
					for (int i = 0; i < kv.Value.Count; i++)
						s += $"val[{i}]:{kv.Value[i]}\n";
					s += "\n";
				}
			}

			try
			{
				File.WriteAllText($"C:\\temp\\{Language}_dump.txt", s);
			}
			catch
			{
				UiLog.LogError("Could not write dump file");
			}
		}


#endif
	}
}