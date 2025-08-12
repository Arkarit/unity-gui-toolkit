#if UNITY_6000_0_OR_NEWER
#define UITK_USE_ROSLYN
#endif

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using UnityEngine;

#if UITK_USE_ROSLYN
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Linq;
using System.IO;
using UnityEditor;


#endif

namespace GuiToolkit.Editor
{
	public static class EditorCodeUtility
	{
		/// <summary>
		/// Checks if the string at a given index is followed by a localization function.
		/// </summary>
		public static bool IsFollowedByLocalizationFunction( List<string> _parts, int _index )
		{
			if (_index % 2 != 1 || _index + 1 >= _parts.Count)
				return false;

			var nextCode = _parts[_index + 1];
			return Regex.IsMatch(nextCode, @"\b(_|__|_n|gettext|ngettext)\s*\(");
		}

		/// <summary>
		/// Return the file path of caller.
		/// </summary>
		/// <param name="_path"></param>
		/// <returns></returns>
		public static string GetThisFilePath( [CallerFilePath] string _path = null ) => _path;

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
		public static List<string> SeparateCodeAndStrings( string _sourceCode )
		{
#if UITK_USE_ROSLYN
			var result = new List<string>();

			var tree = CSharpSyntaxTree.ParseText(_sourceCode);
			var root = tree.GetRoot();

			ProcessNode(result, root);

			// If odd, add empty string to ensure always code/string pairs
			if (result.Count.IsOdd())
				result.Add("");

			return result;
#else
			Debug.LogError($"Roslyn not available in your Unity version.\nPlease install Roslyn by selecting '{StringConstants.ROSLYN_INSTALL_HACK}' in menu.");
			return new List<string>();
#endif
		}

		public static string ReplaceComponent<TA, TB>( string _sourceCode, bool _addUsing = true )
			where TA : Component
			where TB : Component
		{
			var refs = new List<MetadataReference>();
			AddRef(refs, typeof(object).Assembly.Location);                 // mscorlib / System.Private.CoreLib
			AddRef(refs, typeof(UnityEngine.Object).Assembly.Location);     // UnityEngine.CoreModule.dll
			AddRef(refs, typeof(Attribute).Assembly.Location); // System.Runtime.dll
			AddRef(refs, typeof(Enumerable).Assembly.Location); // System.Linq.dll
			AddRef(refs, typeof(Uri).Assembly.Location); // System.dll
			AddRef(refs, typeof(TA).Assembly.Location);                     // e.g. UnityEngine.UI.dll
			AddRef(refs, typeof(TB).Assembly.Location);                     // e.g. Unity.TextMeshPro.dll

			// Optional & defensive – only if exists:
			TryAddRef(refs, typeof(System.Linq.Enumerable).Assembly.Location);
			TryAddRef(refs, typeof(System.Threading.Tasks.Task).Assembly.Location);
			TryAddRef(refs, typeof(System.Collections.Generic.List<>).Assembly.Location);

			var baseDir = EditorApplication.applicationContentsPath; // .../Editor/Data
			var nsRefDir = Path.Combine(baseDir, "NetStandard", "ref", "2.1.0");
			bool found = false;
			if (Directory.Exists(nsRefDir))
			{
				foreach (var dll in Directory.EnumerateFiles(nsRefDir, "netstandard.dll"))
				{
					AddRef(refs, dll);
					found = true;
					break;
				}
			}
			
			if (!found)
			{
				// Fallback: MonoBleedingEdge Facades
				var facadesDir = Path.Combine(baseDir, "MonoBleedingEdge", "lib", "mono", "4.7.1-api", "Facades");
				if (Directory.Exists(facadesDir))
				{
					foreach (var dll in Directory.EnumerateFiles(facadesDir, "*.dll"))
						AddRef(refs, dll);
				}

				// last fallback: Use windows ref dll
				TryAddAllIn(refs, @"C:\Program Files (x86)\Reference Assemblies\Microsoft\NETStandard\2.1.0\ref");
			}

			return RoslynComponentReplacer.ReplaceComponent<TA, TB>(
				_sourceCode,
				refs,
				_addUsing
			);

			static void AddRef( List<MetadataReference> list, string path )
			{
				if (!string.IsNullOrEmpty(path))
					list.Add(MetadataReference.CreateFromFile(path));
			}

			static void TryAddRef( List<MetadataReference> list, string path )
			{
				try
				{
					if (!string.IsNullOrEmpty(path))
						list.Add(MetadataReference.CreateFromFile(path));
				}
				catch { }
			}
			
			static void TryAddAllIn( System.Collections.Generic.List<MetadataReference> list, string dir )
			{
				try
				{
					if (Directory.Exists(dir))
					{
						foreach (var dll in Directory.EnumerateFiles(dir, "*.dll"))
							AddRef(list, dll);
					}
				}
				catch { /* egal */ }
			}
		}

#if UITK_USE_ROSLYN
		private static void ProcessNode( List<string> _result, SyntaxNode _node )
		{
			foreach (var child in _node.ChildNodesAndTokens())
			{
				if (child.IsNode)
				{
					var childNode = child.AsNode();

					switch (childNode)
					{
						case LiteralExpressionSyntax literalExpr
							when literalExpr.IsKind(SyntaxKind.StringLiteralExpression):
							EnsureStringSlot(_result);
							_result[_result.Count - 1] += literalExpr.Token.ValueText; // unescaped
							break;

						case InterpolatedStringTextSyntax interpText:
							EnsureStringSlot(_result);
							_result[_result.Count - 1] += interpText.TextToken.ValueText; // unescaped
							break;

						case InterpolationSyntax interp:
							// We are currently in a code slot; first, close it by inserting an empty string segment
							EnsureStringSlot(_result);

							// Now create a fresh code slot for the inner expression
							EnsureCodeSlot(_result);

							// Recurse only into the inner expression (no braces)
							ProcessNode(_result, interp.Expression);

							// And ensure a trailing empty string to separate from the following code/text
							EnsureStringSlot(_result);
							break;
						default:
							// descend into other nodes
							ProcessNode(_result, childNode);
							break;
					}
				}
				else
				{
					// TOKEN: use .Text (no trivia) -> strips comments and whitespace automatically
					var token = child.AsToken();
					var text = token.Text;
					if (!string.IsNullOrEmpty(text))
					{
						EnsureCodeSlot(_result);
						AppendTokenWithMinimalSpace(_result, text);
					}
				}
			}
		}

		// Append token text into current code slot, inserting a single space only if needed
		private static void AppendTokenWithMinimalSpace( List<string> _list, string _tokenText )
		{
			// We are in a code slot by contract
			var idx = _list.Count - 1;
			var current = _list[idx];

			if (NeedsSpace(current, _tokenText))
				current += " ";

			current += _tokenText;
			_list[idx] = current.Trim('"', '$');
		}

		private static bool NeedsSpace( string _left, string _right )
		{
			if (string.IsNullOrEmpty(_left) || string.IsNullOrEmpty(_right))
				return false;

			char lc = _left[_left.Length - 1];
			char rc = _right[0];

			// add a space only when both sides are "wordy" (identifier-ish)
			bool wordLeft = char.IsLetterOrDigit(lc) || lc == '_';
			bool wordRight = char.IsLetterOrDigit(rc) || rc == '_';

			return wordLeft && wordRight;
		}

		private static void EnsureCodeSlot( List<string> _result )
		{
			// If list is empty, start with a code slot
			// Also, add an empty string entry if last slot was code
			if (_result.Count == 0 || _result.Count.IsEven())
				_result.Add("");
		}

		private static void EnsureStringSlot( List<string> _list )
		{
			// Add an empty code entry if last slot was string
			if (_list.Count.IsOdd())
				_list.Add("");
		}
#endif
	}
}