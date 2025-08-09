#if UNITY_EDITOR

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GuiToolkit.Editor;
using UnityEngine;

public class EditorCodeUtilityTests
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

		// Specific expected pairs
		var expected = new List<(string code, string str)>
		{
			("\t\tstring a = ", "Hello"),
			(";\n\t\t// Comment with \"not a string\"\n\t\t/* Comment with \"not a string\" */\n\t\tstring b = $\"", "World "),
			("name", ""),
			("\";\n\t\tstring c = $\"", "World "),
			("name", ""),
			("\";\n\t\tstring d = $\"", "World "),
			("name", " bla"),
			("\";", "")
		};

		for (int i = 0; i < expected.Count; i++)
		{
			int partIndex = i * 2;
			AssertCodeTextPair(parts[partIndex], parts[partIndex + 1], expected[i].code, expected[i].str, $"Pair {i}");
		}
	}

	private void AssertCodeTextPair(string code, string str, string shouldCode, string shouldStr, string message)
	{
		code = code.Replace("\r", "");
		str = str.Replace("\r", "");
		Assert.That(code, Is.EqualTo(shouldCode), message + " - code mismatch");
		Assert.That(str, Is.EqualTo(shouldStr), message + " - string mismatch");
	}

	private static void LogParts(List<string> parts)
	{
		string debugStr = "Parts:\n";
		var code = true;
		foreach (var part in parts)
		{
			debugStr += $"{(code ? "code:\n" : "string:\n")}{part}\n_____\n\n";
			code = !code;
		}

		Debug.Log(debugStr);
	}

	public void TestData()
	{
		string name = string.Empty;

		
		//=== BEGIN TEST SOURCE ===

		string a = "Hello";
		// Comment with "not a string"
		/* Comment with "not a string" */

		string b = $"World {name}";
		string c = $"World {name /* Crazy but allowed comment*/ }";
		string d = $"World {name} bla";

		//=== END TEST SOURCE ===
	}
}

#endif
