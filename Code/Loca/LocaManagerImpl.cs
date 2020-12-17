#define DEBUG_LOCA
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class LocaManagerImpl : LocaManager
	{
		private bool m_isDev = true;
		private string m_languageId = "dev";

		private class TranslationDictGroup
		{
			public readonly Dictionary<string, string> TranslationDict = new Dictionary<string, string>();
			public readonly Dictionary<string, List<string>> TranslationDictPlural = new Dictionary<string, List<string>>();
		}

		private readonly Dictionary<string, TranslationDictGroup> m_groupDict = new Dictionary<string, TranslationDictGroup>();

		public override bool ChangeLanguageImpl( string _languageId )
		{
			if (string.IsNullOrEmpty(_languageId))
			{
				Debug.LogError("null/Empty language Id");
				return false;
			}

			if (_languageId == m_languageId)
				return true;

			Log($"Language changed: '{_languageId}'");

			m_languageId = _languageId;

			m_isDev = _languageId == "dev";
			if (m_isDev)
			{
				m_groupDict.Clear();
				return true;
			}

			return ReadTranslation(_languageId);
		}

		public override string Translate( string _s, string _group = "", bool _fallbackToDefaultGroup = true )
		{
			if (m_isDev)
				return _s;

			if (m_groupDict.TryGetValue(_group, out TranslationDictGroup group))
			{
				if (group.TranslationDict.TryGetValue(_s, out string result))
					return result;
			}

			if (_group != "" && _fallbackToDefaultGroup)
				return Translate(_s, "", false);

			return _s;
		}

		public override string Translate(string _singularKey, string _pluralKey, int _n, string _group = "", bool _fallbackToDefaultGroup = true )
		{
			(int numPluralForms, int pluralIdx) = LocaPlurals.GetPluralIdx(m_languageId, _n);
			if (pluralIdx == 0)
				return Translate(_singularKey, _group);

			if (m_isDev)
				return _pluralKey;

			if (m_groupDict.TryGetValue(_group, out TranslationDictGroup group))
			{
				if (group.TranslationDictPlural.TryGetValue(_pluralKey, out List<string> plurals))
				{
					if (pluralIdx < plurals.Count)
					{
						return plurals[pluralIdx];
					}
				}
			}

			if (_group != "" && _fallbackToDefaultGroup)
				return Translate(_singularKey, _pluralKey, _n, "", false);

			return _pluralKey;
		}

		private bool ReadTranslation( string _languageId )
		{
			m_groupDict.Clear();
			
			LocaGroup[] locaGroups = UiSettings.Instance.m_locaGroups;

			foreach (var locaGroup in locaGroups)
			{
				ReadTranslation( _languageId, locaGroup.Token);
			}

			return true;
		}

		private void ReadTranslation( string _languageId, string _groupToken )
		{
			string assetName = _languageId + (_groupToken == "" ? "" : _groupToken + "_") + ".po";
			TextAsset text = Resources.Load<TextAsset>(assetName);
			if (text == null)
				return;

			TranslationDictGroup group = new TranslationDictGroup();
			m_groupDict.Add(_groupToken, group);

			string[] lines = text.text.Split(new [] { '\r', '\n' });
			lines = CleanUpLines(lines);

			for (int i=0; i<lines.Length; i++)
			{
				string line1 = lines[i];

				if (line1.StartsWith("msgid"))
				{
					if (i >= lines.Length-1)
					{
						Debug.LogError("Malformed PO file");
						break;
					}

					string line2 = lines[i+1];
					if (line2.StartsWith("msgstr"))
					{
						string cleanKey = Unescape(line1.Substring(7, line1.Length - 8));
						string cleanValue = Unescape(line2.Substring(8, line2.Length - 9));
						group.TranslationDict.Add(cleanKey, cleanValue);
						i++;
						continue;
					}

					Debug.Assert(line2.StartsWith("msgid_plural"));
					if (!line2.StartsWith("msgid_plural"))
						continue;

					string cleanKeySingular = Unescape(line1.Substring(7, line1.Length - 8));
					string cleanKeyPlural = Unescape(line2.Substring(14, line2.Length - 15));

					i += 1;
					if (i >= lines.Length-1)
					{
						Debug.LogError("Malformed PO file");
						break;
					}

					List<string> currentPlurals = new List<string>();
					while (i+1 < lines.Length && lines[i+1].StartsWith("msgstr["))
					{
						currentPlurals.Add(Unescape(lines[i+1].Substring(11, lines[i+1].Length - 12)));
						i++;
					}

					if (currentPlurals.Count < 1)
					{
						Debug.LogError("Malformed PO file");
						continue;
					}

					group.TranslationDict.Add(cleanKeySingular, currentPlurals[0]);
					group.TranslationDictPlural.Add(cleanKeyPlural, currentPlurals);
				}
			}

			//DebugDump();

		}

		// removes empty lines, concatenates strings
		private string[] CleanUpLines( string[] _lines )
		{
			List <string> result = new List<string>();

			int lastKeyword = -1;
			for (int i=0; i<_lines.Length; i++)
			{
				string line = _lines[i].Trim();
				if (   line.StartsWith("#")
					|| line.Length <= 2)
				{
					_lines[i] = null;
					continue;
				}
				if (line.StartsWith("\""))
				{
					if (lastKeyword == -1)
					{
						Debug.LogError("Malformed PO file");
						continue;
					}

					_lines[lastKeyword] = _lines[lastKeyword].Substring(0, _lines[lastKeyword].Length-1) + line.Substring(1, line.Length-1);
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

		[System.Diagnostics.Conditional("DEBUG_LOCA")]
		private void Log(string _s)
		{
			Debug.Log(_s);
		}

		private string Escape(string _s)
		{
			_s = _s.Replace("\"", "\\\"");
			_s = _s.Replace("\n", "\\n");
			_s = _s.Replace("\r", "\\r");
			return _s;
		}

		private string Unescape(string _s)
		{
			_s = _s.Replace("\\\"", "\"");
			_s = _s.Replace("\\n", "\n");
			_s = _s.Replace("\\r", "\r");
			return _s;
		}

#if UNITY_EDITOR

		private class TranslationSetGroup
		{
			public readonly SortedSet<string> Keys = new SortedSet<string>();
			public readonly SortedDictionary<string, string> PluralKeys = new SortedDictionary<string, string>();
		}

		private readonly Dictionary<string, TranslationSetGroup> m_groupSetDict = new Dictionary<string, TranslationSetGroup>();

		public override void Clear()
		{
			m_groupSetDict.Clear();
		}

		public override void AddKey( string _group, string _singularKey, string _pluralKey = null )
		{
			if (string.IsNullOrEmpty(_singularKey))
				return;

			TranslationSetGroup group;
			if (!m_groupSetDict.TryGetValue(_group, out group))
			{
				group = new TranslationSetGroup();
				m_groupSetDict.Add(_group, group);
			}

			if (_pluralKey != null)
			{
				Log($"Adding plural key '{_singularKey}', '{_pluralKey}'");
				Debug.Assert(!Application.isPlaying);
				group.PluralKeys.Add(_singularKey, _pluralKey);
				return;
			}

			Log($"Adding key '{_singularKey}'");
			Debug.Assert(!Application.isPlaying);
			group.Keys.Add(_singularKey);
		}

		public override void ReadKeyData(LocaGroup _locaGroup)
		{
			string potPath = _locaGroup.PotPath;
			TranslationSetGroup group = new TranslationSetGroup();
			m_groupSetDict.Add(_locaGroup.Token, group);

			Log($"Read POT file at '{potPath}'");

			try
			{
				string[] lines = File.ReadAllLines(potPath);
				for (int i=0; i<lines.Length; i++)
				{
					string line = lines[i];

					if (!line.StartsWith("msgid"))
						continue;

					line = line.Substring(7, line.Length - 8);
					line = Unescape(line);

					if (i<lines.Length-1)
					{
						string line2 = lines[i+1];
						if (line2.StartsWith("msgid_plural"))
						{
							line2 = line2.Substring(14, line.Length - 15);
							line2 = Unescape(line);
							Log($"Adding POT plural key '{line}', '{line2}'");
							group.PluralKeys.Add(line, line2);
							i += 1;
							continue;
						}
					}

					Log($"Adding POT key '{line}'");
					group.Keys.Add(line);
				}

				Log("Success");
			}
			catch( Exception e )
			{
				// This is not necessarily an error, since it may just not exist yet.
				Debug.LogWarning($"Could not read POT file at '{potPath}':'{e.Message}'");
			}
		}

		public override void WriteKeyData(LocaGroup _locaGroup)
		{
			TranslationSetGroup group;
			if (!m_groupSetDict.TryGetValue(_locaGroup.Token, out group))
				return;

			string potPath = _locaGroup.PotPath;

			Log($"Write POT file at '{potPath}'");
			try
			{
				string dir = Path.GetDirectoryName(potPath);
				Directory.CreateDirectory(dir);

				string s = "";
				foreach (string key in group.Keys)
				{
					string cleanKey = Escape(key);
					s += 
						  $"msgid \"{cleanKey}\"\n"
						+ $"msgstr \"\"\n\n";
				}

				foreach (var kv in group.PluralKeys)
				{
					string cleanSingular = Escape(kv.Key);
					string cleanPlural = Escape(kv.Value);
					s += 
						  $"msgid \"{cleanSingular}\"\n" 
						+ $"msgid_plural \"{cleanPlural}\"\n";

					for (int i=0; i<4; i++)
						s += $"msgstr[{i}] \"\"\n";

					s += "\n";
				}

				File.WriteAllText(potPath, s);

				AssetDatabase.Refresh();
				Log("Success");
			}
			catch( Exception e )
			{
				Debug.LogError($"Write Fail for POT file at '{potPath}':'{e.Message}'");
			}
		}
/*
		private void DebugDump()
		{
			string s = $"Language:'{m_languageId}'\n";

			foreach (var kv in m_translationDict)
			{
				s += "*************************************************\n";
				s += "key:" + kv.Key + "\n";
				s += "-------------------------------------------------\n";
				s += "val:" + kv.Value + "\n\n";
			}

			foreach (var kv in m_translationDictPlural)
			{
				s += "*************************************************\n";
				s += "pluralkey:" + kv.Key + "\n";
				s += "-------------------------------------------------\n";
				for (int i=0; i<kv.Value.Count; i++)
					s += $"val[{i}]:{kv.Value[i]}\n";
				s += "\n";
			}

			try
			{
				File.WriteAllText($"C:\\temp\\{m_languageId}_dump.txt", s);
			}
			catch
			{
				Debug.LogError("Could not write dump file");
			}
		}
*/

#endif
	}
}