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
					string cleanKey = key.Replace("\"", "\\\"");
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