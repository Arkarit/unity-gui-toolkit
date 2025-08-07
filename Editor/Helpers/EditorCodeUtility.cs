using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UnityEngine;
using System.Runtime.CompilerServices;

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

		// Separate all strings from other program code, remove all quotation marks and comments.
		// Program code and string is always alternating.
		// FIXME: Interpolated strings are not detected in LocaProcessor
		// https://github.com/Arkarit/unity-gui-toolkit/issues/6
		public static List<string> SeparateCodeAndStringsDeprecated( string _content )
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
		
		public static string GetThisFilePath( [CallerFilePath] string _path = null ) => _path;
	}
}