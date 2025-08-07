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
		var parts = EditorCodeUtility.SeparateCodeAndStringsDeprecated(source);
		
		string debugStr = "Parts:\n";
		foreach (var part in parts)
			debugStr += $"{part}\n";
		Debug.Log(debugStr);

		Assert.That(parts, Does.Contain("Hello"));
		Assert.That(parts, Does.Contain("World {name}"));
	}

	public void TestData()
	{
		string name = string.Empty;

		//=== BEGIN TEST SOURCE ===
		
		string a = "Hello";
		// Comment with "not a string"
		string b = $"World {name}";
		
		//=== END TEST SOURCE ===
	}
}

#endif
