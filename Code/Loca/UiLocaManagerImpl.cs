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

		public override bool ChangeLanguageImpl( string _languageId )
		{
			return true;
		}

		public override string Translate( string _s )
		{
			return _s;
		}

		[System.Diagnostics.Conditional("DEBUG_LOCA")]
		private void Log(string _s)
		{
			Debug.Log(_s);
		}

#if UNITY_EDITOR
		private const string POT_PATH = "/../Loca/dev.pot";
		private readonly SortedSet<string> m_keys = new SortedSet<string>();
		private string PotPath => Application.dataPath + POT_PATH;

		private bool m_potRead;
		private bool m_keysDirty;

		public override void ChangeKey( string _oldKey, string _newKey )
		{
			Debug.Assert(!Application.isPlaying);

			if (!m_potRead)
			{
				ReadPot();
				m_potRead = true;
			}

			if ((string.IsNullOrEmpty(_oldKey) || _oldKey.Equals(_newKey)) && m_keys.Contains(_newKey))
				return;

			Log($"Changing key from '{_oldKey}' to '{_newKey}'");

			if (!string.IsNullOrEmpty(_oldKey))
				m_keys.Remove(_oldKey);

			if (!string.IsNullOrEmpty(_newKey))
			{
				if (m_keys.Contains(_newKey))
					return;

				m_keys.Add(_newKey);
			}

			if (!m_keysDirty)
				EditorApplication.delayCall += WritePot;

			m_keysDirty = true;
		}

		public void ReadPot()
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
					line = line.Replace("\\\"", "\"");

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

		private void WritePot()
		{
			Log($"Write POT file at '{PotPath}'");

			try
			{
				string dir = Path.GetDirectoryName(PotPath);
				Directory.CreateDirectory(dir);

				string s = "";
				foreach (string key in m_keys)
				{
					string cleanKey = key.Replace("\"", "\\\"");
					s += $"msgid \"{cleanKey}\"\nmsgstr \"\"\n\n";
				}

				File.WriteAllText(PotPath, s);
			}
			catch( Exception e )
			{
				Debug.LogError($"Write Fail for POT file at '{PotPath}':'{e.Message}'");
				m_keysDirty = false;
				return;
			}

			Log("Success");
			m_keysDirty = false;
		}
#endif
	}
}