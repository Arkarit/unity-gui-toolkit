using System;
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
			var result = new List<string>();

			var tree = CSharpSyntaxTree.ParseText(sourceCode);
			var root = tree.GetRoot();

			ProcessNode(result, root);

			// If odd, add empty string to ensure always code/string pairs
			if (result.Count.IsOdd())
				result.Add("");

			return result;
		}

		private static void ProcessNode( List<string> result, SyntaxNode node )
		{
			foreach (var child in node.ChildNodesAndTokens())
			{
				if (child.IsNode)
				{
					var childNode = child.AsNode();

					switch (childNode)
					{
						case LiteralExpressionSyntax literalExpr
							when literalExpr.IsKind(SyntaxKind.StringLiteralExpression):
							EnsureStringSlot(result);
							result[result.Count - 1] += literalExpr.Token.ValueText;
							break;

						case InterpolatedStringTextSyntax interpText:
							EnsureStringSlot(result);
							result[result.Count - 1] += interpText.TextToken.ValueText;
							break;

						case InterpolationSyntax interp:
							EnsureCodeSlot(result);
							// Recursively, expression between { }
							ProcessNode(result, interp.Expression);
							break;

						default:
							// Go deeper into normal nodes
							ProcessNode(result, childNode);
							break;
					}
				}
				else
				{
					// Tokens
					var tokenText = child.AsToken().ToFullString();
					if (!string.IsNullOrEmpty(tokenText))
					{
						EnsureCodeSlot(result);
						result[result.Count - 1] += tokenText;
					}
				}
			}
		}

		private static void EnsureCodeSlot( List<string> result )
		{
			// If list is empty, start with a code slot
			// Also, add an empty string entry if last slot was code
			if (result.Count == 0 || result.Count.IsEven())
				result.Add("");
		}

		private static void EnsureStringSlot( List<string> result )
		{
			// Add an empty code entry if last slot was string
			if (result.Count.IsOdd())
				result.Add("");
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