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

		private bool ReadTranslation( string _languageId, string _group )
		{
			_group = _group == null ? string.Empty : "_" + _group;
			TextAsset text = Resources.Load<TextAsset>($"{_languageId}{_group}.po");
			if (text == null)
				return false;

			string[] lines = text.text.Split(new[] { '\r', '\n' });
			lines = CleanUpLines(lines);

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
			if (string.IsNullOrEmpty(_group))
				_group = DEFAULT_LOCA_GROUP;

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
			if (string.IsNullOrEmpty(_group))
				_group = DEFAULT_LOCA_GROUP;

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
			if (string.IsNullOrEmpty(_group))
				_group = DEFAULT_LOCA_GROUP;

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

#if UNITY_EDITOR
		private readonly SortedSet<string> m_keys = new SortedSet<string>();
		private readonly SortedDictionary<string, string> m_pluralKeys = new SortedDictionary<string, string>();
		private string PotPath => EditorFileUtility.GetApplicationDataDir() + UiToolkitConfiguration.Instance.m_potPath;

		private string[] m_availableLanguages = null;
		public override string[] AvailableLanguages
		{
			get
			{
				if (m_availableLanguages == null)
				{
					List<string> availableLanguages = new List<string>();
					availableLanguages.Add("dev");

					string[] guids = AssetDatabase.FindAssets(".po t:textasset");
					for (int i = 0; i < guids.Length; i++)
					{
						string guid = guids[i];
						string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
						string language = Path.GetFileNameWithoutExtension(assetPath);
						availableLanguages.Add(language.Substring(0, language.Length - 3));
					}

					m_availableLanguages = availableLanguages.ToArray();
				}

				return m_availableLanguages;
			}
		}

		public override void Clear()
		{
			m_keys.Clear();
			m_pluralKeys.Clear();
		}

		public override void AddKey( string _singularKey, string _pluralKey = null, string _group = null )
		{
			if (string.IsNullOrEmpty(_group))
				_group = DEFAULT_LOCA_GROUP;

			if (string.IsNullOrEmpty(_singularKey))
				return;

			if (_pluralKey != null)
			{
				if (DebugLoca)
					Log($"Adding plural key '{_singularKey}', '{_pluralKey}'");
				Debug.Assert(!Application.isPlaying);
				m_pluralKeys.Add(_singularKey, _pluralKey);
				return;
			}

			if (DebugLoca)
				Log($"Adding key '{_singularKey}'");
			Debug.Assert(!Application.isPlaying);
			m_keys.Add(_singularKey);
		}

		public override void ReadKeyData()
		{
			if (DebugLoca)
				Log($"Read POT file at '{PotPath}'");

			m_keys.Clear();
			try
			{
				string[] lines = File.ReadAllLines(PotPath);
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
							m_pluralKeys.Add(line, line2);
							i += 1;
							continue;
						}
					}

					if (DebugLoca)
						Log($"Adding POT key '{line}'");
					m_keys.Add(line);
				}

				if (DebugLoca)
					Log("Success");
			}
			catch (Exception e)
			{
				// This is not necessarily an error, since it may just not exist yet.
				UiLog.LogWarning($"Could not read POT file at '{PotPath}':'{e.Message}'");
			}
		}

		public override void WriteKeyData()
		{
			if (DebugLoca)
				Log($"Write POT file at '{PotPath}'");

			try
			{
				string dir = Path.GetDirectoryName(PotPath);
				Directory.CreateDirectory(dir);

				string s = "";
				foreach (string key in m_keys)
				{
					string cleanKey = Escape(key);
					s +=
						  $"msgid \"{cleanKey}\"\n"
						+ $"msgstr \"\"\n\n";
				}

				foreach (var kv in m_pluralKeys)
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

				File.WriteAllText(PotPath, s);

				AssetDatabase.Refresh();
				if (DebugLoca)
					Log("Success");
			}
			catch (Exception e)
			{
				UiLog.LogError($"Write Fail for POT file at '{PotPath}':'{e.Message}'");
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