#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public static class LocaProcessor
	{
		private static int m_numScripts;
		private static int m_currentScriptIdx;

		[MenuItem(StringConstants.LOCA_PROCESSOR_MENU_NAME, priority = Constants.LOCA_PROCESSOR_MENU_PRIORITY)]
		public static void Process()
		{
			UiMain.LocaManager.Clear();

			EditorUtility.DisplayProgressBar("Processing Loca", "Processing scenes", 0);
			UiEditorUtility.FindAllComponentsInAllScenes<ILocaClient>(LocaHelper.AddKeyFromClient);
			EditorUtility.DisplayProgressBar("Processing Loca", "Processing prefabs", 0.1f);
			UiEditorUtility.FindAllComponentsInAllPrefabs<ILocaClient>(LocaHelper.AddKeyFromClient);
			EditorUtility.DisplayProgressBar("Processing Loca", "Processing scriptable objects", 0.2f);
			UiEditorUtility.FindAllComponentsInAllScriptableObjects<ILocaClient>(LocaHelper.AddKeyFromClient);

			m_numScripts = UiEditorUtility.FindAllScriptsCount();
			m_currentScriptIdx = 0;

			UiEditorUtility.FindAllScripts(FoundScript);

			EditorUtility.ClearProgressBar();

			UiMain.LocaManager.WriteKeyData();
		}

		private static void FoundScript( string _path, string _content )
		{
			float progress = ((float)m_currentScriptIdx / (float)m_numScripts) * .8f + .2f;
			EditorUtility.DisplayProgressBar("Processing Loca", $"Processing script '{Path.GetFileName(_path)}'", progress);
			m_currentScriptIdx++;

			List<string> strings = ExtractAllStrings(_content);
			//DebugDump(_path, strings);

			int numStrings = strings.Count;

			for (int i=0; i<numStrings; i += 2)
			{
				if ( i > numStrings - 2 )
					break;

				string code = strings[i];
				string str = strings[i+1];

				if (Evaluate(code, "_(", str) || Evaluate(code, "gettext(", str))
					continue;

				if ( i > numStrings - 4)
					continue;

				string code2 = strings[i+2];
				string str2 = strings[i+3];

				if (code2.Trim() != ",")
					continue;

				if (Evaluate(code, "_n(", str, str2) || Evaluate(code, "ngettext(", str, str2))
					i += 2;
			}

 		}

		private static bool Evaluate(string _code, string _keyword, string _singular, string _plural = null)
		{
			int codeLength = _code.Length;
			int keywordLength = _keyword.Length;
			if (codeLength < keywordLength)
				return false;

			if (_code.EndsWith(_keyword))
			{
				if (codeLength == keywordLength)
				{
					UiMain.LocaManager.AddKey(_singular, _plural);
					return true;
				}

				char c = _code[codeLength - keywordLength - 1];

				if ((char.IsWhiteSpace(c) || !char.IsLetterOrDigit(c)) && c != '_')
				{
					UiMain.LocaManager.AddKey(_singular, _plural);
					return true;
				}
			}

			return false;
		}

		// Separate all strings from other program code, remove all quotation marks and comments.
		// Program code and string is always alternating.
		private static List<string> ExtractAllStrings( string _content )
		{
			List<string> result = new List<string>();

			bool inString = false;
			bool inEscape = false;
			bool inScopeComment = false;
			bool inLineComment = false;

			string current = "";

			for (int i=0; i<_content.Length; i++)
			{
				char c = _content[i];

				if (inEscape)
				{
					Debug.Assert(inString);
					current += c;
					inEscape = false;
					continue;
				}

				if (inLineComment)
				{
					Debug.Assert(!inString);
					if (c == '\n' || c == '\r')
					{
						current += '\n';
						inLineComment = false;
						continue;
					}
					continue;
				}

				if (inScopeComment)
				{
					if (c == '*')
					{
						// A * is the last char of the source.
						// Definitely an error, but we have to handle it to avoid oob
						if (i == _content.Length - 1)
						{
							result.Add(current);
							break;
						}

						char c2 = _content[i+1];

						if (c2 == '/')
						{
							i += 1;
							inScopeComment = false;
							continue;
						}

					}
					continue;
				}

				if (inString)
				{
					Debug.Assert(!inScopeComment && !inLineComment);

					if (c == '\"')
					{
						result.Add(current);
						current = "";
						inString = false;
						continue;
					}

					if (c == '\\')
					{
						current += c;
						inEscape = true;
						continue;
					}

					current += c;
					continue;
				}

				if (c == '\"')
				{
					result.Add(current);
					current = "";
					inString = true;
					continue;
				}

				if (c == '/')
				{
					// A / is the last char of the source.
					// Definitely an error, but we have to handle it to avoid oob
					if (i == _content.Length - 1)
					{
						result.Add(current);
						break;
					}

					char c2 = _content[i+1];

					if (c2 == '/')
					{
						i += 1;
						inLineComment = true;
						continue;
					}

					if (c2 == '*')
					{
						i += 1;
						inScopeComment = true;
						continue;
					}

					current += c;
					continue;
				}

				current += c;
			}

			if (current.Length > 0)
				result.Add(current);

			return result;
		}

		private static void DebugDump(string _path, List <string> _strings)
		{
			string outPath = "C:\\temp\\LocaProcessorTest\\";
			try {
				Directory.CreateDirectory(outPath);
			} catch
			{
				Debug.LogError("Failed to create directory");
				return;
			}
			outPath += _path.Replace("/", "_") + ".txt";
			
			string str = "";
			for (int i = 0; i<_strings.Count; i++)
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
				Debug.LogError("Failed to write file");
				return;
			}
		}
	}
}
#endif