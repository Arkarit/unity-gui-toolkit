using System;
using System.Collections.Generic;
using System.Text;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Represents a parsed GNU gettext PO or POT file.
	/// Provides parsing from raw content, serialization back to PO format, and lookup building.
	/// String values stored in entries are unescaped (actual characters); the header msgstr is
	/// kept in its raw escaped form to preserve continuation-line structure during round-trips.
	/// </summary>
	public class PoFile
	{
		/// <summary>Comment lines (lines starting with <c>#</c>) before the header <c>msgid ""</c> entry.</summary>
		public List<string> HeaderLines = new List<string>();

		/// <summary>
		/// Raw (escape-preserved) content of the header <c>msgstr</c> block.
		/// The literal two-character sequence <c>\n</c> is used as a field separator,
		/// matching the PO file convention for header fields.
		/// </summary>
		public string HeaderMsgStr = string.Empty;

		/// <summary>All non-header entries in the file, including obsolete ones.</summary>
		public List<PoEntry> Entries = new List<PoEntry>();

		/// <summary>True when the file contained a header entry (<c>msgid ""</c>) during parsing, or was explicitly set.</summary>
		public bool HasHeader;

		/// <summary>
		/// Parses PO or POT file content into a <see cref="PoFile"/> object.
		/// Handles singular, plural, context, obsolete, fuzzy, and multi-line string entries.
		/// </summary>
		/// <param name="_content">The raw PO/POT file text.</param>
		/// <returns>A populated <see cref="PoFile"/> instance; never null.</returns>
		public static PoFile Parse(string _content)
		{
			var result = new PoFile();
			if (string.IsNullOrEmpty(_content))
				return result;

			var lines = _content.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
			var blocks = SplitIntoBlocks(lines);

			bool headerFound = false;
			foreach (var block in blocks)
			{
				if (IsObsoleteBlock(block))
				{
					var entry = ParseObsoleteBlock(block);
					if (entry != null)
						result.Entries.Add(entry);
					continue;
				}

				if (!headerFound && IsHeaderBlock(block))
				{
					ParseHeaderBlock(block, result);
					headerFound = true;
					result.HasHeader = true;
					continue;
				}

				var regular = ParseEntryBlock(block);
				if (regular != null)
					result.Entries.Add(regular);
			}

			return result;
		}

		/// <summary>
		/// Serializes this <see cref="PoFile"/> back to a PO-formatted string.
		/// Active entries are written first (in their current order), followed by obsolete entries.
		/// </summary>
		/// <returns>The serialized PO file content.</returns>
		public string Serialize()
		{
			var sb = new StringBuilder();

			if (HasHeader || HeaderLines.Count > 0 || !string.IsNullOrEmpty(HeaderMsgStr))
			{
				foreach (var line in HeaderLines)
					sb.AppendLine(line);

				sb.AppendLine("msgid \"\"");
				sb.AppendLine("msgstr \"\"");

				if (!string.IsNullOrEmpty(HeaderMsgStr))
				{
					// HeaderMsgStr uses literal \n as field separators; split and re-emit continuation lines.
					string[] parts = HeaderMsgStr.Split(new[] { @"\n" }, StringSplitOptions.None);
					foreach (var part in parts)
					{
						if (!string.IsNullOrEmpty(part))
							sb.AppendLine($"\"{part}\\n\"");
					}
				}

				sb.AppendLine();
			}

			foreach (var entry in Entries)
			{
				if (!entry.IsObsolete)
					AppendEntry(sb, entry);
			}

			foreach (var entry in Entries)
			{
				if (entry.IsObsolete)
					AppendEntry(sb, entry);
			}

			return sb.ToString();
		}

		/// <summary>
		/// Builds a lookup dictionary of all active (non-obsolete) entries keyed by <see cref="PoEntry.ComposedKey"/>.
		/// When duplicate keys exist, the first occurrence wins.
		/// </summary>
		/// <returns>Dictionary mapping composed key to entry.</returns>
		public Dictionary<string, PoEntry> BuildLookup()
		{
			var dict = new Dictionary<string, PoEntry>(StringComparer.Ordinal);
			foreach (var entry in Entries)
			{
				if (entry.IsObsolete)
					continue;
				if (!dict.ContainsKey(entry.ComposedKey))
					dict[entry.ComposedKey] = entry;
			}
			return dict;
		}

		private static List<List<string>> SplitIntoBlocks(string[] _lines)
		{
			var blocks = new List<List<string>>();
			var current = new List<string>();
			foreach (var line in _lines)
			{
				if (string.IsNullOrWhiteSpace(line))
				{
					if (current.Count > 0)
					{
						blocks.Add(current);
						current = new List<string>();
					}
				}
				else
				{
					current.Add(line);
				}
			}
			if (current.Count > 0)
				blocks.Add(current);
			return blocks;
		}

		private static bool IsHeaderBlock(List<string> _block)
		{
			foreach (var line in _block)
			{
				string trimmed = line.TrimStart();
				if (trimmed.StartsWith("#"))
					continue;
				if (!trimmed.StartsWith("msgid"))
					return false;
				string afterKeyword = trimmed.Substring(5).TrimStart();
				return afterKeyword == "\"\"";
			}
			return false;
		}

		private static bool IsObsoleteBlock(List<string> _block)
		{
			foreach (var line in _block)
				if (line.TrimStart().StartsWith("#~"))
					return true;
			return false;
		}

		private static void ParseHeaderBlock(List<string> _block, PoFile _result)
		{
			int i = 0;
			while (i < _block.Count && _block[i].StartsWith("#") && !_block[i].StartsWith("#~"))
			{
				_result.HeaderLines.Add(_block[i]);
				i++;
			}

			// Advance past msgid ""
			while (i < _block.Count && !_block[i].TrimStart().StartsWith("msgid"))
				i++;
			if (i < _block.Count)
				i++;

			// Skip msgid continuation lines (unusual for header but handle defensively)
			while (i < _block.Count && _block[i].TrimStart().StartsWith("\"") && !_block[i].TrimStart().StartsWith("#"))
				i++;

			var sb = new StringBuilder();
			if (i < _block.Count && _block[i].TrimStart().StartsWith("msgstr"))
			{
				string line = _block[i].TrimStart();
				string rest = line.Substring(6).TrimStart();
				sb.Append(ExtractInlineString(rest));
				i++;
				while (i < _block.Count && _block[i].TrimStart().StartsWith("\"") && !_block[i].TrimStart().StartsWith("#"))
				{
					sb.Append(ExtractInlineString(_block[i].Trim()));
					i++;
				}
			}
			_result.HeaderMsgStr = sb.ToString();
		}

		private static PoEntry ParseEntryBlock(List<string> _block)
		{
			if (_block == null || _block.Count == 0)
				return null;

			var entry = new PoEntry();
			int i = 0;

			while (i < _block.Count && _block[i].StartsWith("#") && !_block[i].StartsWith("#~"))
			{
				string line = _block[i];
				if (line.StartsWith("#."))
					entry.TranslatorComments.Add(line.Substring(2).Trim());
				else if (line.StartsWith("#:"))
					entry.SourceReferences.Add(line.Substring(2).Trim());
				else if (line.StartsWith("#,") && line.Contains("fuzzy"))
					entry.IsFuzzy = true;
				i++;
			}

			if (i >= _block.Count)
				return null;

			if (_block[i].TrimStart().StartsWith("msgctxt"))
				entry.Context = UnescapePoString(ReadBlockString(_block, ref i, "msgctxt"));

			if (i >= _block.Count)
				return null;

			string msgidLine = _block[i].TrimStart();
			if (!msgidLine.StartsWith("msgid"))
				return null;

			entry.MsgId = UnescapePoString(ReadBlockString(_block, ref i, "msgid"));

			if (i >= _block.Count)
				return entry;

			if (_block[i].TrimStart().StartsWith("msgid_plural"))
			{
				entry.MsgIdPlural = UnescapePoString(ReadBlockString(_block, ref i, "msgid_plural"));
				var forms = new List<string>();
				while (i < _block.Count && _block[i].TrimStart().StartsWith("msgstr["))
					forms.Add(UnescapePoString(ReadBlockStringIndexed(_block, ref i)));
				entry.MsgStrForms = forms.ToArray();
			}
			else if (_block[i].TrimStart().StartsWith("msgstr"))
			{
				entry.MsgStr = UnescapePoString(ReadBlockString(_block, ref i, "msgstr"));
			}

			return entry;
		}

		private static PoEntry ParseObsoleteBlock(List<string> _block)
		{
			var entry = new PoEntry();
			entry.IsObsolete = true;

			string lastKeyword = null;
			var currentValue = new StringBuilder();
			var msgStrForms = new List<string>();
			int msgStrIdx = -1;

			void Flush()
			{
				if (lastKeyword == null)
					return;
				string val = currentValue.ToString();
				switch (lastKeyword)
				{
					case "msgctxt":     entry.Context     = UnescapePoString(val); break;
					case "msgid":       entry.MsgId       = UnescapePoString(val); break;
					case "msgid_plural":entry.MsgIdPlural = UnescapePoString(val); break;
					case "msgstr":      entry.MsgStr      = UnescapePoString(val); break;
					default:
						if (msgStrIdx >= 0)
						{
							while (msgStrForms.Count <= msgStrIdx)
								msgStrForms.Add(string.Empty);
							msgStrForms[msgStrIdx] = UnescapePoString(val);
							msgStrIdx = -1;
						}
						break;
				}
				lastKeyword = null;
				currentValue.Clear();
			}

			foreach (var rawLine in _block)
			{
				int tildeIdx = rawLine.IndexOf("#~", StringComparison.Ordinal);
				if (tildeIdx < 0)
					continue;

				string line = rawLine.Substring(tildeIdx + 2).TrimStart();
				if (string.IsNullOrEmpty(line))
					continue;

				if (line.StartsWith("\""))
				{
					currentValue.Append(ExtractInlineString(line.Trim()));
					continue;
				}

				Flush();

				if (line.StartsWith("msgctxt ") || line.StartsWith("msgctxt\t"))
				{
					lastKeyword = "msgctxt";
					currentValue.Append(ExtractInlineString(line.Substring(7).TrimStart()));
				}
				else if (line.StartsWith("msgid_plural ") || line.StartsWith("msgid_plural\t"))
				{
					lastKeyword = "msgid_plural";
					currentValue.Append(ExtractInlineString(line.Substring(12).TrimStart()));
				}
				else if (line.StartsWith("msgid ") || line.StartsWith("msgid\t"))
				{
					lastKeyword = "msgid";
					currentValue.Append(ExtractInlineString(line.Substring(5).TrimStart()));
				}
				else if (line.StartsWith("msgstr["))
				{
					int closeBracket = line.IndexOf(']');
					if (closeBracket > 7 && int.TryParse(line.Substring(7, closeBracket - 7), out int idx))
					{
						msgStrIdx = idx;
						lastKeyword = $"msgstr_form_{idx}";
						string rest = line.Substring(closeBracket + 1).TrimStart();
						currentValue.Append(ExtractInlineString(rest.Trim()));
					}
				}
				else if (line.StartsWith("msgstr ") || line.StartsWith("msgstr\t") || line == "msgstr \"\"")
				{
					lastKeyword = "msgstr";
					string rest = line.StartsWith("msgstr") ? line.Substring(6).TrimStart() : "\"\"";
					currentValue.Append(ExtractInlineString(rest));
				}
			}

			Flush();

			if (msgStrForms.Count > 0)
				entry.MsgStrForms = msgStrForms.ToArray();

			return string.IsNullOrEmpty(entry.MsgId) ? null : entry;
		}

		private static string ReadBlockString(List<string> _block, ref int _i, string _keyword)
		{
			if (_i >= _block.Count)
				return string.Empty;

			var sb = new StringBuilder();
			string line = _block[_i].TrimStart();

			// Find the value portion after the keyword
			string valueStr;
			if (line.Length > _keyword.Length && (line[_keyword.Length] == ' ' || line[_keyword.Length] == '\t'))
				valueStr = line.Substring(_keyword.Length + 1).TrimStart();
			else
				valueStr = "\"\"";

			sb.Append(ExtractInlineString(valueStr));
			_i++;

			while (_i < _block.Count && _block[_i].TrimStart().StartsWith("\"") && !_block[_i].TrimStart().StartsWith("#"))
			{
				sb.Append(ExtractInlineString(_block[_i].Trim()));
				_i++;
			}

			return sb.ToString();
		}

		private static string ReadBlockStringIndexed(List<string> _block, ref int _i)
		{
			if (_i >= _block.Count)
				return string.Empty;

			var sb = new StringBuilder();
			string line = _block[_i].TrimStart();
			int closeBracket = line.IndexOf(']');
			if (closeBracket < 0)
			{
				_i++;
				return string.Empty;
			}

			string valueStr = line.Substring(closeBracket + 1).TrimStart();
			sb.Append(ExtractInlineString(valueStr));
			_i++;

			while (_i < _block.Count && _block[_i].TrimStart().StartsWith("\"") && !_block[_i].TrimStart().StartsWith("#"))
			{
				sb.Append(ExtractInlineString(_block[_i].Trim()));
				_i++;
			}

			return sb.ToString();
		}

		private static string ExtractInlineString(string _s)
		{
			_s = _s.Trim();
			if (_s.Length >= 2 && _s[0] == '"' && _s[_s.Length - 1] == '"')
				return _s.Substring(1, _s.Length - 2);
			return _s;
		}

		private static string UnescapePoString(string _s)
		{
			if (_s == null)
				return string.Empty;

			var sb = new StringBuilder(_s.Length);
			for (int i = 0; i < _s.Length; i++)
			{
				if (_s[i] == '\\' && i + 1 < _s.Length)
				{
					i++;
					switch (_s[i])
					{
						case '\\': sb.Append('\\'); break;
						case '"':  sb.Append('"');  break;
						case 'n':  sb.Append('\n'); break;
						case 'r':  sb.Append('\r'); break;
						case 't':  sb.Append('\t'); break;
						default:   sb.Append('\\'); sb.Append(_s[i]); break;
					}
				}
				else
				{
					sb.Append(_s[i]);
				}
			}
			return sb.ToString();
		}

		private static string EscapePoString(string _s)
		{
			if (_s == null)
				return string.Empty;

			var sb = new StringBuilder(_s.Length + 8);
			foreach (char c in _s)
			{
				switch (c)
				{
					case '\\': sb.Append("\\\\"); break;
					case '"':  sb.Append("\\\""); break;
					case '\n': sb.Append("\\n");  break;
					case '\r': sb.Append("\\r");  break;
					case '\t': sb.Append("\\t");  break;
					default:   sb.Append(c);      break;
				}
			}
			return sb.ToString();
		}

		private static void AppendEntry(StringBuilder _sb, PoEntry _entry)
		{
			string prefix = _entry.IsObsolete ? "#~ " : string.Empty;

			if (!_entry.IsObsolete)
			{
				foreach (var comment in _entry.TranslatorComments)
					_sb.AppendLine($"#. {comment}");
				foreach (var sourceRef in _entry.SourceReferences)
					_sb.AppendLine($"#: {sourceRef}");
				if (_entry.IsFuzzy)
					_sb.AppendLine("#, fuzzy");
			}

			if (!string.IsNullOrEmpty(_entry.Context))
				_sb.AppendLine($"{prefix}msgctxt \"{EscapePoString(_entry.Context)}\"");

			_sb.AppendLine($"{prefix}msgid \"{EscapePoString(_entry.MsgId)}\"");

			if (_entry.IsPlural)
			{
				_sb.AppendLine($"{prefix}msgid_plural \"{EscapePoString(_entry.MsgIdPlural)}\"");
				if (_entry.MsgStrForms != null && _entry.MsgStrForms.Length > 0)
				{
					for (int i = 0; i < _entry.MsgStrForms.Length; i++)
						_sb.AppendLine($"{prefix}msgstr[{i}] \"{EscapePoString(_entry.MsgStrForms[i] ?? string.Empty)}\"");
				}
				else
				{
					_sb.AppendLine($"{prefix}msgstr[0] \"\"");
					_sb.AppendLine($"{prefix}msgstr[1] \"\"");
				}
			}
			else
			{
				_sb.AppendLine($"{prefix}msgstr \"{EscapePoString(_entry.MsgStr ?? string.Empty)}\"");
			}

			_sb.AppendLine();
		}
	}
}
