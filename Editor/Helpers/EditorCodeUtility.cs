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
		/// Splits a C# source string into alternating code and string segments.
		/// The resulting list always starts with a code segment and alternates:
		/// [code, string, code, string, ..., code]. If necessary, an empty string
		/// is appended at the end to ensure this pattern.
		/// 
		/// - Code segments are raw source code.
		/// - String segments are unescaped literal or interpolated string parts.
		/// - Interpolated expressions (e.g. {value}) are included as code without braces.
		/// </summary>
		public static List<string> SeparateCodeAndStrings( string sourceCode )
		{
			var tree = CSharpSyntaxTree.ParseText(sourceCode);
			var root = tree.GetRoot();
			var result = new List<string>();

			// Collect all string and interpolation parts with position info
			var spans = new List<(int start, int end, bool isString, string text)>();

			// Handle standard string literals ("...")
			foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
			{
				if (literal.IsKind(SyntaxKind.StringLiteralExpression))
				{
					spans.Add((literal.SpanStart, literal.Span.End, true, literal.Token.ValueText));
				}
			}

			// Handle interpolated strings ($"...")
			foreach (var interpolated in root.DescendantNodes().OfType<InterpolatedStringExpressionSyntax>())
			{
				foreach (var content in interpolated.Contents)
				{
					if (content is InterpolatedStringTextSyntax text)
					{
						// Literal text part of an interpolated string
						spans.Add((text.SpanStart, text.Span.End, true, text.TextToken.ValueText));
					}
					else if (content is InterpolationSyntax interp)
					{
						// Embedded code inside { } – we extract only the inner expression
						var inner = interp.Expression?.ToFullString()?.Trim() ?? "";
						spans.Add((interp.SpanStart, interp.Span.End, false, inner));
					}
				}
			}

			// Sort all spans by their position in the source
			spans = spans.OrderBy(s => s.start).ToList();
			int pos = 0;

			foreach (var span in spans)
			{
				if (span.start > pos)
				{
					// Add code between previous span and current one
					result.Add(sourceCode.Substring(pos, span.start - pos));
				}

				// Add the actual span text (either string or code)
				result.Add(span.text);
				pos = span.end;
			}

			// last part
			if (pos < sourceCode.Length)
			{
				var trailing = sourceCode.Substring(pos);

				// if the last entry is code, add an empty string to make the count even
				if (result.Count.IsOdd())
					result.Add("");

				result.Add(trailing.Trim('\"')); // code
				result.Add(""); // a final empty string to make the count even
			}
			else if (result.Count.IsOdd())
			{
				// Ensure result always ends with a string
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