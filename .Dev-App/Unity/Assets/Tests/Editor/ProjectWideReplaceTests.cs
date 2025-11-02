#define ROSLYN_VERBOSE
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GuiToolkit.Editor.Roslyn;
using Microsoft.CodeAnalysis.CSharp;
using TMPro;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class ProjectWideReplaceTests
	{
		[Test]
		public void ProjectWide_TextToTMP_OnlyIntendedChanges()
		{
			var guids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets/" });
			int checkedFiles = 0;
			int changedFiles = 0;
			int skippedFiles = 0;
			int ignoredFiles = 0;

			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
					continue;

				//TODO: Fix issues with qualified class declarations
				if 
				(
					path.EndsWith("UiStyleText.cs", StringComparison.OrdinalIgnoreCase)
					|| path.EndsWith("UiApplyStyleText.cs", StringComparison.OrdinalIgnoreCase)
				)
				{
					RoslynComponentReplacer.LogVerbose($"Skipping '{path}'");
					skippedFiles++;
					continue;
				}

				var srcBytes = System.IO.File.ReadAllBytes(path);
				var src = Encoding.UTF8.GetString(srcBytes);
				if (ContainsIdentifierOutsideStrings(src, "TMP_Text"))
				{
					RoslynComponentReplacer.LogVerbose($"Skipping '{path}'");
					skippedFiles++;
					continue;
				}
				
				var newline = DetectNewline(src);

				var dst = GuiToolkit.Editor.EditorCodeUtility.ReplaceComponent<Text,TMP_Text>(src, _addUsing: true);
				dst = NormalizeNewlines(dst, newline);

				if (src == dst)
				{
					RoslynComponentReplacer.LogVerbose($"Ignoring (no TMP_Text) '{path}'");
					ignoredFiles++;
					continue;
				}

				RoslynComponentReplacer.LogVerbose($"Testing '{path}'");
				changedFiles++;

				// Build a "revert to original" version from dst, but ONLY undo the intended changes:
				// 1) Replace TMP_Text back to Text for identifier tokens (roughly)
				// 2) Remove exactly one added "using TMPro;" line if it was not present in src
				var reverted = RevertIntendedChanges(dst, src);

				// Now the reverted should be exactly equal to the original
				var srcTree = CSharpSyntaxTree.ParseText(src);
				var revTree = CSharpSyntaxTree.ParseText(reverted);

				bool isEquivalent = SyntaxFactory.AreEquivalent(srcTree.GetRoot(), revTree.GetRoot());
				if (!isEquivalent)
				{
					WriteFaultyFiles(path, src, dst, reverted, "Files differ");
				}

				Assert.IsTrue(isEquivalent, $"Unexpected semantic changes in file: {path}");

				checkedFiles++;
			}

			UiLog.Log($"[ProjectWide_TextToTMP_OnlyIntendedChanges] Ignored: {ignoredFiles} Skipped: {skippedFiles} Checked: {checkedFiles}, Changed: {changedFiles}");
		}

		private static void WriteFaultyFiles( string path, string _source, string _destination, string _reverted, string _message )
		{
			try
			{
				var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				docs += "/GuiToolkitDebug";
				EditorFileUtility.EnsureFolderExists(docs);
				var fileName = System.IO.Path.GetFileName(path);

				var sourcePath = System.IO.Path.Combine(docs, $"___{fileName}_0source.cs");
				var destinationPath = System.IO.Path.Combine(docs, $"___{fileName}_1destination.cs");
				var revertedPath = System.IO.Path.Combine(docs, $"___{fileName}_2reverted.cs");

				System.IO.File.WriteAllText(sourcePath, _source);
				System.IO.File.WriteAllText(destinationPath, _destination);
				System.IO.File.WriteAllText(revertedPath, _reverted);

				UiLog.LogError($"{_message}: {fileName}\nSaved as:\n{sourcePath}\n{destinationPath}");
			}
			catch (Exception ex)
			{
				UiLog.LogError($"Failed to save diff files: {ex}");
			}
		}

		// --- Helpers ---

		private static string DetectNewline( string s )
			=> s.Contains("\r\n") ? "\r\n" : "\n";

		private static string NormalizeNewlines( string s, string newline )
		{
			var lf = s.Replace("\r\n", "\n");
			return newline == "\n" ? lf : lf.Replace("\n", "\r\n");
		}

		private static string RevertIntendedChanges( string dst, string srcOriginal )
		{
			// 1) If dst added "using TMPro;" but src did not have it, remove that one line.
			bool srcHasUsingTMPro = HasUsingTMPro(srcOriginal);
			if (!srcHasUsingTMPro)
			{
				// remove a single "using TMPro;" line (tolerate optional whitespace)
				var pattern = @"^\s*using\s+TMPro\s*;\s*\r?\n";
				var removedOnce = new Regex(pattern, RegexOptions.Multiline).Replace(dst, "", 1);
				dst = removedOnce;
			}

			// 2) Replace TMP_Text back to Text, but avoid touching comments/strings.
			// We do a light-weight token-aware revert:
			// - Replace whole-word occurrences of TMP_Text (identifiers), not inside quotes.
			// - This is a heuristic good enough because our forward replacer used Roslyn (no strings/comments).
			dst = ReplaceOutsideStrings(dst, "TMP_Text", "Text");

			return dst;
		}

		private static bool HasUsingTMPro( string s )
		{
			// quick check ignoring leading/trailing whitespace on the line
			foreach (var line in SplitLines(s))
			{
				var t = line.Trim();
				if (string.Equals(t, "using TMPro;", StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		private static IEnumerable<string> SplitLines( string s )
		{
			using (var reader = new System.IO.StringReader(s))
			{
				string line;
				while ((line = reader.ReadLine()) != null)
					yield return line;
			}
		}

		private static string ReplaceOutsideStrings( string input, string needle, string replacement )
		{
			var sb = new System.Text.StringBuilder(input.Length);
			bool inString = false, verbatim = false;
			int i = 0, n = input.Length, L = needle.Length;

			while (i < n)
			{
				char c = input[i];

				// enter string
				if (!inString && c == '"')
				{
					inString = true;
					verbatim = i > 0 && input[i - 1] == '@';
					sb.Append(c); i++; continue;
				}

				if (inString)
				{
					sb.Append(c); i++;
					if (!verbatim)
					{
						if (c == '\\' && i < n) { sb.Append(input[i]); i++; continue; }
						if (c == '"') { inString = false; verbatim = false; }
					}
					else
					{
						if (c == '"' && i < n && input[i] == '"') { sb.Append('"'); i++; continue; }
						if (c == '"') { inString = false; verbatim = false; }
					}
					continue;
				}

				// try whole-word token match
				if (i + L <= n && string.CompareOrdinal(input, i, needle, 0, L) == 0
					&& IsBoundary(input, i - 1) && IsBoundary(input, i + L))
				{
					sb.Append(replacement);
					i += L;
					continue;
				}

				sb.Append(c); i++;
			}
			return sb.ToString();

			static bool IsBoundary( string s, int idx )
			{
				if (idx < 0 || idx >= s.Length) return true;
				char ch = s[idx];
				return !(char.IsLetterOrDigit(ch) || ch == '_');
			}
		}

		private static bool ContainsIdentifierOutsideStrings( string input, string ident )
		{
			bool inString = false, verbatim = false;
			for (int i = 0; i < input.Length;)
			{
				char c = input[i];

				// String Start?
				if (!inString && c == '"')
				{
					inString = true;
					verbatim = i > 0 && input[i - 1] == '@';
					i++;
					continue;
				}

				if (inString)
				{
					i++;
					if (!verbatim)
					{
						if (c == '\\' && i < input.Length) { i++; continue; }
						if (c == '"') { inString = false; verbatim = false; }
					}
					else
					{
						if (c == '"' && i < input.Length && input[i] == '"') { i++; continue; }
						if (c == '"') { inString = false; verbatim = false; }
					}
					continue;
				}

				// token check
				if (IsWordAt(input, i, ident))
					return true;

				i++;
			}
			return false;

			static bool IsWordAt( string s, int i, string w )
			{
				if (i + w.Length > s.Length) return false;
				if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
				return IsBoundary(s, i - 1) && IsBoundary(s, i + w.Length);
			}
			static bool IsBoundary( string s, int idx )
			{
				if (idx < 0 || idx >= s.Length) return true;
				char ch = s[idx];
				return !(char.IsLetterOrDigit(ch) || ch == '_');
			}
		}

	}
}