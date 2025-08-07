using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class EditorCodeUtility
	{
		// Separate all strings from other program code, remove all quotation marks and comments.
		// Program code and string is always alternating.
		public static List<string> SeparateCodeAndStrings( string _sourceCode )
		{
			var tree = CSharpSyntaxTree.ParseText(_sourceCode);
			var root = tree.GetRoot();

			var segments = new List<(int start, int end, string type, string value)>();

			// 1. Collect all string literals
			foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
			{
				if (literal.IsKind(SyntaxKind.StringLiteralExpression))
				{
					segments.Add((
						literal.SpanStart,
						literal.Span.End,
						"string",
						literal.Token.ValueText // unescaped content
					));
				}
			}

			// 2. Collect all interpolated strings
			foreach (var interpolated in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
			{
				segments.Add((
					interpolated.SpanStart,
					interpolated.Span.End,
					"string",
					interpolated.ToFullString() // inkl. $"..."
				));
			}

			// 3. Sort by position
			var sorted = segments.OrderBy(s => s.start).ToList();

			var result = new List<string>();
			int pos = 0;

			foreach (var seg in sorted)
			{
				if (seg.start > pos)
				{
					var codePart = _sourceCode.Substring(pos, seg.start - pos);
					result.Add(codePart);
				}

				result.Add(seg.value);
				pos = seg.end;
			}

			// Remaining code
			if (pos < _sourceCode.Length)
			{
				result.Add(_sourceCode.Substring(pos));
			}

			return result;
		}

	}
}