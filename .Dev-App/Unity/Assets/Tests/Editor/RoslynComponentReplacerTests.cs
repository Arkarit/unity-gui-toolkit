using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuiToolkit.Editor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class RoslynComponentReplacerTests
	{
		[Test]
		public void Field_Parameter_Local_Generic_Typeof_Nameof_Cast_Attribute_Are_Replaced()
		{
			const string input = @"
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class Foo : MonoBehaviour
{
    public Text label;
    void Bar(Text p)
    {
        var l = (Text)null;
        var t = typeof(Text);
        var n = nameof(Text);
        var c = GetComponent<Text>();
        TryGetComponent<Text>(out var _);
    }
}
";
			var output = EditorCodeUtility.ReplaceComponent<Text,TMP_Text>(input);

			// Expectations; without order
			StringAssert.Contains("using TMPro;", output);
			StringAssert.Contains("RequireComponent(typeof(TMP_Text))", output);
			StringAssert.Contains("public TMP_Text label;", output);
			StringAssert.Contains("Bar(TMP_Text p)", output);
			StringAssert.Contains("(TMP_Text)null", output);
			StringAssert.Contains("typeof(TMP_Text)", output);
			StringAssert.Contains("nameof(TMP_Text)", output);
			StringAssert.Contains("GetComponent<TMP_Text>()", output);
			StringAssert.Contains("TryGetComponent<TMP_Text>", output);

			var refs = new List<MetadataReference>();
			var monoApiDir = Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge", "lib", "mono", "4.7.1-api");
			foreach (var dll in Directory.EnumerateFiles(monoApiDir, "*.dll"))
				refs.Add(MetadataReference.CreateFromFile(dll));

			var facadesDir = Path.Combine(monoApiDir, "Facades");
			foreach (var dll in Directory.EnumerateFiles(facadesDir, "*.dll"))
				refs.Add(MetadataReference.CreateFromFile(dll));

			AddRef(refs, typeof(void).Assembly.Location);
			AddRef(refs, typeof(object).Assembly.Location);
			AddRef(refs, typeof(UnityEngine.Object).Assembly.Location);
			AddRef(refs, typeof(Text).Assembly.Location);
			AddRef(refs, typeof(TMP_Text).Assembly.Location);
			TryAddRef(refs, typeof(Enumerable).Assembly.Location);

			AssertCompiles(output, refs);
		}

		[Test]
		public void Alias_Using_Is_Handled()
		{
			const string input = @"
using UnityEngine;
using UI = UnityEngine.UI;

public class Foo : MonoBehaviour
{
    public UI.Text label;
}
";
			var output = EditorCodeUtility.ReplaceComponent<Text,TMP_Text>(input);

			StringAssert.Contains("TMP_Text label;", output);
			StringAssert.Contains("using TMPro;", output);
		}

		[Test]
		public void Idempotent_When_No_Source_Type_In_Code()
		{
			const string input = @"
using UnityEngine;
public class Foo : MonoBehaviour
{
    public int x;
}
";
			var output = EditorCodeUtility.ReplaceComponent<Text,TMP_Text>(input);
			Assert.AreEqual(Normalize(input), Normalize(output));
		}

		[Test]
		public void Already_Target_Type_Is_NoOp()
		{
			const string input = @"
using UnityEngine;
using TMPro;
public class Foo : MonoBehaviour
{
    public TMP_Text label;
}
";
			var output = EditorCodeUtility.ReplaceComponent<Text,TMP_Text>(input);
			Assert.AreEqual(Normalize(input), Normalize(output));
		}

		private static string Normalize( string s ) => CSharpSyntaxTree.ParseText(s).GetRoot().NormalizeWhitespace().ToFullString();

		private static void AddRef( List<MetadataReference> list, string path )
		{
			if (!string.IsNullOrEmpty(path))
				list.Add(MetadataReference.CreateFromFile(path));
		}
		private static void TryAddRef( List<MetadataReference> list, string path )
		{
			try { AddRef(list, path); } catch { /* ignore */ }
		}

		private static void AssertCompiles( string code, IEnumerable<MetadataReference> refs )
		{
			var tree = CSharpSyntaxTree.ParseText(code, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
			var comp = CSharpCompilation.Create("TestAsm", new[] { tree }, refs,
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
			var diags = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
			if (diags.Length > 0)
			{
				Assert.Fail("Compilation failed:\n" + string.Join("\n", diags.Select(d => d.ToString())));
			}
		}
	}
}