#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public static class LocaCleaner
	{
		[MenuItem(StringConstants.LOCA_CLEANER_MENU_NAME)]
		public static void Clean()
		{
			UiMain.LocaManager.Clear();

			UiEditorUtility.FindAllComponentsInAllAssets<ILocaClient>(FoundComponent);
			UiEditorUtility.FindAllScripts(FoundScript);

			UiMain.LocaManager.WriteKeyData();
		}

		private static void FoundComponent( ILocaClient _component )
		{
			if (_component.UsesMultipleLocaKeys)
			{
				var keys = _component.LocaKeys;
				foreach (var key in keys)
					UiMain.LocaManager.AddKey(key);

				return;
			}

			UiMain.LocaManager.AddKey(_component.LocaKey);
		}

		private static void FoundScript( string _path, string _content )
		{
			List<string> strings = ExtractAllStrings(_content);

			for (int i=0; i<strings.Count; i += 2)
			{
				if ( i >= strings.Count -1 )
					break;

				string code = strings[i];
				string str = strings[i+1];
				if (code.Length < 2)
					continue;

				if (code == "_(")
				{
					UiMain.LocaManager.AddKey(str);
					continue;
				}

				if (code.EndsWith("_("))
				{
					char c = code[code.Length - 3];
					if ((char.IsWhiteSpace(c) || !char.IsLetterOrDigit(c)) && c != '_')
					{
						UiMain.LocaManager.AddKey(str);
					}
				}
			}

			//DebugDump(_path, strings);
		}

		// First part: Separate all strings from other program code, remove all quotation marks and comments
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
			string outPath = "C:\\temp\\LocaCleanerTest\\";
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