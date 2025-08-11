using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GuiToolkit.Editor;
using UnityEngine;

namespace GuiToolkit.Test
{
	public class TestEditorCodeUtility
	{
		[Test]
		public void SeparateCodeAndStrings_SelfExtractedSource()
		{
			string path = EditorCodeUtility.GetThisFilePath();
			string content = File.ReadAllText(path);

			// Extract between markers
			var match = Regex.Match(content,
				@"//=== BEGIN TEST SOURCE ===\s*\r?\n(.*?)\r?\n\s*//=== END TEST SOURCE ===",
				RegexOptions.Singleline);

			Assert.IsTrue(match.Success, "Test source markers not found");

			string source = match.Groups[1].Value;
			var parts = EditorCodeUtility.SeparateCodeAndStrings(source);

			LogParts(parts);

			// Basic structure test: should alternate and end with string
			Assert.That(parts.Count % 2, Is.EqualTo(0), "Parts list must alternate code and string");
			Assert.That(parts.Count, Is.GreaterThan(0), "Parts list must not be empty");

			// Test data in a fake local function
			void TestData()
			{
				string name = string.Empty;
				bool bl = false;
				int idx = 7;
				string str = string.Empty;
				string _( string msg ) => msg;
	
				//=== BEGIN TEST SOURCE ===
				string a = "Literal";
				// Comment with "not a string"
				/* Comment with "not a string" */
	
				string b = $"Literal {name}";
				string c = $"Literal {name /* Crazy but allowed comment*/ }";
				string d = $"Literal {name} bla";
				string e = $"Literal {(bl ? "1" : "2")}";
				string f = $"Literal {(bl ? _("1") : _("2"))}";
				string g = $"Literal 1" + "Literal 2" +
						   "Literal 3";
				string h = "";
				string i = "He said \"Hi\"";
				string j = $"{name}";
				string k = $"{_("key")}";
				string l = "" + "";
				string m = $"{idx,3:D2}";
				string n = str;
				//=== END TEST SOURCE ===
	
			}
		
			// Specific expected pairs
			var expected = new List<(string code, string str)>
			{
				("string a=", "Literal"),			// Pair 0
			    (";string b=", "Literal "),			// Pair 1
			    ("name", ""),						// Pair 2
			    (";string c=", "Literal "),			// Pair 3
			    ("name", ""),						// Pair 4
			    (";string d=", "Literal "),			// Pair 5
			    ("name", " bla"),					// Pair 6
			    (";string e=", "Literal "),			// Pair 7
			    ("(bl?", "1"),						// Pair 8
			    (":", "2"),							// Pair 9
			    (")", ""),							// Pair 10
			    (";string f=", "Literal "),			// Pair 11
			    ("(bl?_(", "1"),					// Pair 12
			    ("):_(", "2"),						// Pair 13
			    ("))", ""),							// Pair 14
			    (";string g=", "Literal 1"),		// Pair 15
			    ("+", "Literal 2"),					// Pair 16
			    ("+", "Literal 3"),					// Pair 17
			    (";string h=", ""),					// Pair 18
			    (";string i=", "He said \"Hi\""),	// Pair 19
			    (";string j=", ""),					// Pair 20
			    ("name", ""),						// Pair 21
			    (";string k=", ""),					// Pair 22
			    ("_(", "key"),						// Pair 23
			    (")", ""),							// Pair 24
			    (";string l=", ""),					// Pair 25
			    ("+", ""),							// Pair 26
			    (";string m=", ""),					// Pair 27
			    ("idx", ""),						// Pair 28
			    (";string n=str;", ""),				// Pair 29
			};

			for (int i = 0; i < expected.Count; i++)
			{
				int partIndex = i * 2;
				AssertCodeTextPair(parts[partIndex], parts[partIndex + 1], expected[i].code, expected[i].str, $"Pair {i}");
			}
		}

		private void AssertCodeTextPair( string _code, string _str, string _shouldCode, string _shouldStr, string _message )
		{
			_code = _code.Replace("\r", "");
			_str = _str.Replace("\r", "");
			Assert.That(_code, Is.EqualTo(_shouldCode), _message + " - code mismatch");
			Assert.That(_str, Is.EqualTo(_shouldStr), _message + " - string mismatch");
		}

		private static void LogParts( List<string> _parts )
		{
			string debugStr = "Parts:\n";
			var code = true;
			for (var i = 0; i < _parts.Count; i++)
			{
				if (code)
					debugStr += $"\nPair {i / 2}: ";

				var part = _parts[i];
				debugStr += $"{(code ? "'" : "::: '")}{part}' ";
				code = !code;
			}

			Debug.Log(debugStr);
		}
	}
}
