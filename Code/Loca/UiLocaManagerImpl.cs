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
	public class UiLocaManagerImpl : UiLocaManager
	{
		private bool m_isDev;

		private string m_languageId = "";

		private readonly Dictionary<string, string> m_translationDict = new Dictionary<string, string>();

		public override bool ChangeLanguageImpl( string _languageId )
		{
			if (string.IsNullOrEmpty(_languageId))
			{
				Debug.LogError("null/Empty language Id");
				return false;
			}

			if (_languageId.Equals(m_languageId))
				return true;

			Log($"Language changed: '{_languageId}'");

			m_isDev = _languageId.Equals("dev");
			if (m_isDev)
			{
				m_translationDict.Clear();
				return true;
			}

			return ReadTranslation(_languageId);
		}

		private bool ReadTranslation( string _languageId )
		{
			m_translationDict.Clear();
			TextAsset text = Resources.Load<TextAsset>(_languageId + ".po");
			if (text == null)
				return false;

			string currentKey = "";
			string[] lines = text.text.Split(new [] { '\r', '\n' });
			foreach (string line in lines)
			{
				if (line.StartsWith("msgid"))
				{
					currentKey = Unescape(line.Substring(7, line.Length - 8));
					continue;
				}

				if (line.StartsWith("msgstr") && !string.IsNullOrEmpty(currentKey))
					m_translationDict.Add(currentKey, Unescape(line.Substring(8, line.Length - 9)));
			}

			return true;
		}

		public override string Translate( string _s )
		{
			if (m_isDev)
				return _s;

			if (m_translationDict.TryGetValue(_s, out string result))
				return result;

			return _s;
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
		private const string POT_PATH = "/../Loca/dev.pot";
		private readonly SortedSet<string> m_keys = new SortedSet<string>();
		private string PotPath => Application.dataPath + POT_PATH;

		public override void Clear()
		{
			m_keys.Clear();
		}

		public override void AddKey( string _newKey )
		{
			if (string.IsNullOrEmpty(_newKey))
				return;

			Log($"Adding key '{_newKey}'");

			Debug.Assert(!Application.isPlaying);
			m_keys.Add(_newKey);
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

					Log($"Adding POT key '{line}'");
					m_keys.Add(line);
				}
			}
			catch( Exception e )
			{
				// This is not necessarily an error, since it may just not exist yet.
				Debug.LogWarning($"Could not read POT file at '{PotPath}':'{e.Message}'");
				return;
			}
			Log("Success");
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

					s += $"msgid \"{cleanKey}\"\nmsgstr \"\"\n\n";
				}

				File.WriteAllText(PotPath, s);
			}
			catch( Exception e )
			{
				Debug.LogError($"Write Fail for POT file at '{PotPath}':'{e.Message}'");
				return;
			}

			Log("Success");
		}
#endif
	}
}