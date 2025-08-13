using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GuiToolkit.Editor;
using UnityEngine;
using System.Linq;
using FindInactive = UnityEngine.FindObjectsInactive;
using FindSort = UnityEngine.FindObjectsSortMode;
using TMPro;
using UnityEditor.SceneManagement;


namespace GuiToolkit.Test
{
	public class TestEditorCodeUtility
	{
		private GameObject m_go1;
		private GameObject m_go2;
		
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

		[SetUp]
		public void SetUp()
		{
			// Create a new empty scene for isolation
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			// Create two new test GameObjects with Text components
			m_go1 = new GameObject("Test1");
			m_go2 = new GameObject("Test2");
			m_go1.AddComponent<UnityEngine.UI.Text>();
			m_go2.AddComponent<UnityEngine.UI.Text>();
		}

		[TearDown]
		public void TearDown()
		{
			// Ensure cleanup of created objects
			if (m_go1 != null) Object.DestroyImmediate(m_go1);
			if (m_go2 != null) Object.DestroyImmediate(m_go2);

			// Optionally close the test scene
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}

		[Test]
		public void ReplaceComponentsInActiveScene_ReplacesAllInstances()
		{
			// Count any Text components that already exist in the active scene
			int existing = Object.FindObjectsByType<UnityEngine.UI.Text>(FindInactive.Include, FindSort.None).Length;

			// Create two new test GameObjects with Text components
			var go1 = new GameObject("Test1");
			var go2 = new GameObject("Test2");
			go1.AddComponent<UnityEngine.UI.Text>();
			go2.AddComponent<UnityEngine.UI.Text>();

			var expectedTotal = existing + 2;

			// Perform the replacement
			var results = GuiToolkit.Editor.EditorCodeUtility.ReplaceComponentsInActiveScene<UnityEngine.UI.Text, TMPro.TextMeshProUGUI>();

			// Verify that all (existing + newly created) Text components were replaced
			Assert.AreEqual(expectedTotal, results.Count);
			Assert.IsTrue(results.All(r => r.NewComp is TMPro.TMP_Text));

			// Ensure there are no remaining UnityEngine.UI.Text components in the scene
			var remainingTexts = Object.FindObjectsByType<UnityEngine.UI.Text>(FindInactive.Include, FindSort.None).Length;
			Assert.AreEqual(0, remainingTexts);

			// Clean up the two test GameObjects (optional if they can remain in the scene)
			Object.DestroyImmediate(go1);
			Object.DestroyImmediate(go2);
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
