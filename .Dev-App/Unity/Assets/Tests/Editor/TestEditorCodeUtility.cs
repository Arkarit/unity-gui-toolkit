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
using UnityEngine.UI;
using UnityEditor;


namespace GuiToolkit.Test
{
	[EditorAware]
	public class TestEditorCodeUtility
	{
		GameObject go1, go2;

		[SetUp]
		public void Setup()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			go1 = new GameObject("A");
			go2 = new GameObject("B");

			var t1 = go1.AddComponent<Text>();
			var t2 = go2.AddComponent<Text>();

			// Preconfigure some values to see they carry over
			t1.text = "Hello";
			t1.color = Color.red;
			t1.fontSize = 18;
			t1.supportRichText = true;
			t1.alignment = TextAnchor.MiddleCenter;

			t2.text = "World";
			t2.color = Color.green;
			t2.fontSize = 12;
			t2.supportRichText = false;
			t2.alignment = TextAnchor.UpperLeft;
		}

		[TearDown]
		public void TearDown()
		{
			if (go1) Object.DestroyImmediate(go1);
			if (go2) Object.DestroyImmediate(go2);
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}

		[Test]
		public void ReplaceUITextWithTMP_MapsEssentialFields()
		{
			var results = GuiToolkit.Editor.EditorCodeUtility.ReplaceUITextWithTMPInActiveScene();

			Assert.AreEqual(2, results.Count);

			var tmp1 = go1.GetComponent<TMP_Text>();
			var tmp2 = go2.GetComponent<TMP_Text>();
			Assert.NotNull(tmp1);
			Assert.NotNull(tmp2);

			// Content
			Assert.AreEqual("Hello", tmp1.text);
			Assert.AreEqual("World", tmp2.text);

			// Visuals / sizing
			Assert.AreEqual(Color.red, tmp1.color);
			Assert.AreEqual(Color.green, tmp2.color);
			Assert.AreEqual(18, tmp1.fontSize, 0.001f);
			Assert.AreEqual(12, tmp2.fontSize, 0.001f);

			// RichText
			Assert.IsTrue(tmp1.richText);
			Assert.IsFalse(tmp2.richText);
		}

		[Test]
		public void ReplaceUITextWithTMP_RemovesAllLegacyText()
		{
			var before = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
			Assert.AreEqual(2, before);

			GuiToolkit.Editor.EditorCodeUtility.ReplaceUITextWithTMPInActiveScene();

			var after = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
			Assert.AreEqual(0, after);
		}

		[Test]
		public void ReplaceUITextWithTMP_UndoRestoresText()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
			var go = new GameObject("X");
			var t = go.AddComponent<Text>();
			t.text = "Hello";

			GuiToolkit.Editor.EditorCodeUtility.ReplaceUITextWithTMPInActiveScene();

			Assert.IsNotNull(go.GetComponent<TMPro.TextMeshProUGUI>());
			Assert.IsNull(go.GetComponent<Text>());

			Undo.PerformUndo();

			Assert.IsNull(go.GetComponent<TMPro.TextMeshProUGUI>());
			var restored = go.GetComponent<Text>();
			Assert.IsNotNull(restored);
			Assert.AreEqual("Hello", restored.text);
		}

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
				string fakeKey = string.Empty;
				string fakeGroup = string.Empty;

				string _( string msg, string _ = null ) => msg;

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
				string o = $"{_(fakeKey)}";
				string p = $"{_("key", "group")}";
				string q = $"{_("key", fakeGroup)}";
				string r = $"{_(fakeKey, fakeGroup)}";
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

				// n, o
			    (";string n=str;string o=", ""),	// Pair 29/30

				// o
				("_(fakeKey)", ""),                  // Pair 31
				
				// p
				(";string p=", ""),                  // Pair 32
				("_(", "key"),                       // Pair 33
				(",", "group"),                      // Pair 34
				(")", ""),                           // Pair 35
				
				// q
				(";string q=", ""),                  // Pair 36
				("_(", "key"),                       // Pair 37
				(",fakeGroup)", ""),                 // Pair 38
				
				// r
				(";string r=", ""),                  // Pair 39
				("_(fakeKey,fakeGroup)", ""),        // Pair 40
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

			UiLog.Log(debugStr);
		}
	}
}
