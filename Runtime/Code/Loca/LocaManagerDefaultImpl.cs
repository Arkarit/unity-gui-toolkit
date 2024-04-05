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
	public class LocaManagerDefaultImpl : LocaManager
	{
		private bool m_isDev = true;
		private readonly Dictionary<string, string> m_translationDict = new Dictionary<string, string>();
		private readonly Dictionary<string, List<string>> m_translationDictPlural = new Dictionary<string, List<string>>();

		public override bool ChangeLanguageImpl( string _language )
		{
			if (string.IsNullOrEmpty(_language))
			{
				Debug.LogError("null/Empty language Id");
				return false;
			}

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

			TextAsset text = Resources.Load<TextAsset>(_languageId + ".po");
			if (text == null)
				return false;

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
						m_translationDict.Add(cleanKey, cleanValue);
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

					m_translationDict.Add(cleanKeySingular, currentPlurals[0]);
					m_translationDictPlural.Add(cleanKeyPlural, currentPlurals);
				}
			}

			//DebugDump();


			return true;
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

		public override string Translate( string _s )
		{
			if (string.IsNullOrEmpty(_s))
				return "#MISSING KEY#";

			if (m_isDev)
				return _s;

			if (m_translationDict.TryGetValue(_s, out string result))
			{
				if (string.IsNullOrEmpty(result))
					result = $"#{_s}";

				return result;
			}

			_s = $"*{_s}";

			return _s;
		}

		public override string Translate(string _singularKey, string _pluralKey, int _n )
		{
			(int numPluralForms, int pluralIdx) = LocaPlurals.GetPluralIdx(Language, _n);
			if (pluralIdx == 0)
				return Translate(_singularKey);

			if (m_isDev)
				return _pluralKey;

			if (m_translationDictPlural.TryGetValue(_pluralKey, out List<string> plurals))
			{
				if (pluralIdx < plurals.Count)
				{
					var result = plurals[pluralIdx];
					if (string.IsNullOrEmpty(result))
						result = $"_{pluralIdx}_{_pluralKey}";

					return result;
				}
			}

			_pluralKey = $"*{_pluralKey}";
			return _pluralKey;
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
		private readonly SortedSet<string> m_keys = new SortedSet<string>();
		private readonly SortedDictionary<string, string> m_pluralKeys = new SortedDictionary<string, string>();
		private string PotPath => EditorFileUtility.GetApplicationDataDir() + UiToolkitConfiguration.EditorLoad().m_potPath;

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
					for (int i=0; i<guids.Length; i++)
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

		public override void AddKey( string _singularKey, string _pluralKey = null )
		{
			if (string.IsNullOrEmpty(_singularKey))
				return;

			if (_pluralKey != null)
			{
				Log($"Adding plural key '{_singularKey}', '{_pluralKey}'");
				Debug.Assert(!Application.isPlaying);
				m_pluralKeys.Add(_singularKey, _pluralKey);
				return;
			}

			Log($"Adding key '{_singularKey}'");
			Debug.Assert(!Application.isPlaying);
			m_keys.Add(_singularKey);
		}

		public override void ReadKeyData()
		{
			Log($"Read POT file at '{PotPath}'");
			m_keys.Clear();
			try
			{
				string[] lines = File.ReadAllLines(PotPath);
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
							m_pluralKeys.Add(line, line2);
							i += 1;
							continue;
						}
					}

					Log($"Adding POT key '{line}'");
					m_keys.Add(line);
				}

				Log("Success");
			}
			catch( Exception e )
			{
				// This is not necessarily an error, since it may just not exist yet.
				Debug.LogWarning($"Could not read POT file at '{PotPath}':'{e.Message}'");
			}
		}

		public override void WriteKeyData()
		{
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

					for (int i=0; i<4; i++)
						s += $"msgstr[{i}] \"\"\n";

					s += "\n";
				}

				File.WriteAllText(PotPath, s);

				AssetDatabase.Refresh();
				Log("Success");
			}
			catch( Exception e )
			{
				Debug.LogError($"Write Fail for POT file at '{PotPath}':'{e.Message}'");
			}
		}

		private void DebugDump()
		{
			string s = $"Language:'{Language}'\n";

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
				File.WriteAllText($"C:\\temp\\{Language}_dump.txt", s);
			}
			catch
			{
				Debug.LogError("Could not write dump file");
			}
		}


#endif
	}
}