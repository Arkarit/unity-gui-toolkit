using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace GuiToolkit.Editor
{
	public static class EditorCodeUtility
	{
		/// <summary>
		/// Splits a source string into alternating code and string content.
		/// The result list always starts with code and alternates with string.
		/// Strings are unescaped and do not include quotes.
		/// </summary>
		public static List<string> SeparateCodeAndStrings( string sourceCode )
		{
			var tree = CSharpSyntaxTree.ParseText(sourceCode);
			var root = tree.GetRoot();
			var result = new List<string>();

			var spans = new List<(int start, int end, bool isString, string text)>();

			foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
			{
				if (literal.IsKind(SyntaxKind.StringLiteralExpression))
				{
					spans.Add((literal.SpanStart, literal.Span.End, true, literal.Token.ValueText));
				}
			}

			foreach (var interpolated in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
			{
				foreach (var content in interpolated.Contents)
				{
					if (content is InterpolatedStringTextSyntax text)
					{
						spans.Add((text.SpanStart, text.Span.End, true, text.TextToken.ValueText));
					}
					else if (content is InterpolationSyntax interp)
					{
						var inner = interp.Expression?.ToFullString()?.Trim() ?? "";
						spans.Add((interp.SpanStart, interp.Span.End, false, inner));
					}
				}
			}

			spans = spans.OrderBy(s => s.start).ToList();
			int pos = 0;

			foreach (var span in spans)
			{
				if (span.start > pos)
				{
					result.Add(sourceCode.Substring(pos, span.start - pos)); // code
				}

				result.Add(span.text); // already parsed as code or unescaped string
				pos = span.end;
			}

			if (pos < sourceCode.Length)
			{
				// we may need to split final segment into code + empty string if it follows a string
				var finalCode = sourceCode.Substring(pos);
				result.Add(finalCode);
				result.Add("");
			}
			else if (result.Count % 2 == 1)
			{
				// ensure list is even count (always ending with string)
				result.Add("");
			}

			return result;
		}

		/// <summary>
		/// Checks if the string at a given index is followed by a localization function.
		/// </summary>
		public static bool IsFollowedByLocalizationFunction( List<string> parts, int index )
		{
			if (index % 2 != 1 || index + 1 >= parts.Count)
				return false;

			var nextCode = parts[index + 1];
			return Regex.IsMatch(nextCode, @"\b(_|__|_n|gettext|ngettext)\s*\(");
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

			for (int i = 0; i < _content.Length; i++)
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

						char c2 = _content[i + 1];

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

					char c2 = _content[i + 1];

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