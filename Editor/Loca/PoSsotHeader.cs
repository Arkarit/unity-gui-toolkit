using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Parsed information extracted from an SSoT header block in a PO file.
	/// </summary>
	public class PoSsotInfo
	{
		/// <summary>Human-readable name of the linked bridge asset.</summary>
		public string BridgeName;

		/// <summary>Unity GUID of the linked bridge asset.</summary>
		public string BridgeGuid;

		/// <summary>URL of the source spreadsheet.</summary>
		public string SourceUrl;

		/// <summary>Timestamp when the file was generated.</summary>
		public DateTime GeneratedAt;
	}

	/// <summary>
	/// Utilities for detecting, generating, and writing the SSoT (Single Source of Truth) header
	/// that is prepended to PO/POT files generated from a linked spreadsheet.
	/// Files carrying this header should not be edited manually; use the merge pipeline instead.
	/// </summary>
	public static class PoSsotHeader
	{
		private const string HEADER_LINE_1       = "# Generated from Spreadsheet SSoT";
		private const string HEADER_BRIDGE_PREFIX = "# Bridge: ";
		private const string HEADER_SOURCE_PREFIX = "# Source: ";
		private const string HEADER_GENERATED_PREFIX = "# Generated: ";
		private const string HEADER_WARNING      = "# DO NOT EDIT MANUALLY \u2014 Changes will be overwritten. Use the linked spreadsheet or \"Make Local Copy\" to detach.";
		private const string GUID_OPEN           = " (GUID: ";
		private const string GUID_CLOSE          = ")";

		/// <summary>
		/// Checks whether the file at <paramref name="_filePath"/> begins with an SSoT header.
		/// Reads only the first few lines; does not parse the whole file.
		/// </summary>
		/// <param name="_filePath">Absolute path to the PO/POT file.</param>
		/// <returns>True if the SSoT header marker is present.</returns>
		public static bool HasSsotHeader(string _filePath)
		{
			if (!File.Exists(_filePath))
				return false;

			try
			{
				using var reader = new StreamReader(_filePath, Encoding.UTF8);
				string line = reader.ReadLine();
				while (line != null && string.IsNullOrWhiteSpace(line))
					line = reader.ReadLine();
				return line != null && line.TrimStart().StartsWith(HEADER_LINE_1, StringComparison.Ordinal);
			}
			catch (Exception e)
			{
				UiLog.LogWarning($"PoSsotHeader: could not read '{_filePath}': {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// Parses the SSoT header from the file at <paramref name="_filePath"/>.
		/// </summary>
		/// <param name="_filePath">Absolute path to the PO/POT file.</param>
		/// <returns>A <see cref="PoSsotInfo"/> if the header is present and parseable; null otherwise.</returns>
		public static PoSsotInfo ParseHeader(string _filePath)
		{
			if (!HasSsotHeader(_filePath))
				return null;

			try
			{
				var info = new PoSsotInfo();
				foreach (var line in File.ReadLines(_filePath, Encoding.UTF8))
				{
					if (!line.StartsWith("#"))
						break;

					if (line.StartsWith(HEADER_BRIDGE_PREFIX))
					{
						string rest = line.Substring(HEADER_BRIDGE_PREFIX.Length);
						int guidOpen = rest.IndexOf(GUID_OPEN, StringComparison.Ordinal);
						if (guidOpen >= 0)
						{
							info.BridgeName = rest.Substring(0, guidOpen).Trim();
							int guidClose = rest.IndexOf(GUID_CLOSE, guidOpen + GUID_OPEN.Length, StringComparison.Ordinal);
							if (guidClose > guidOpen)
								info.BridgeGuid = rest.Substring(guidOpen + GUID_OPEN.Length, guidClose - guidOpen - GUID_OPEN.Length).Trim();
						}
						else
						{
							info.BridgeName = rest.Trim();
						}
					}
					else if (line.StartsWith(HEADER_SOURCE_PREFIX))
					{
						info.SourceUrl = line.Substring(HEADER_SOURCE_PREFIX.Length).Trim();
					}
					else if (line.StartsWith(HEADER_GENERATED_PREFIX))
					{
						string ts = line.Substring(HEADER_GENERATED_PREFIX.Length).Trim();
						if (DateTime.TryParse(ts, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
							info.GeneratedAt = dt;
					}
				}
				return info;
			}
			catch (Exception e)
			{
				UiLog.LogWarning($"PoSsotHeader: failed to parse header in '{_filePath}': {e.Message}");
				return null;
			}
		}

		/// <summary>
		/// Generates the SSoT header comment lines for writing into a PO/POT file.
		/// </summary>
		/// <param name="_bridgeName">Human-readable name of the bridge asset.</param>
		/// <param name="_bridgeGuid">Unity GUID of the bridge asset.</param>
		/// <param name="_sourceUrl">URL of the source spreadsheet.</param>
		/// <returns>List of comment lines (each starting with <c>#</c>) ready to prepend to a PO file's HeaderLines.</returns>
		public static List<string> GenerateHeaderLines(string _bridgeName, string _bridgeGuid, string _sourceUrl)
		{
			return new List<string>
			{
				HEADER_LINE_1,
				$"{HEADER_BRIDGE_PREFIX}{_bridgeName}{GUID_OPEN}{_bridgeGuid}{GUID_CLOSE}",
				$"{HEADER_SOURCE_PREFIX}{_sourceUrl}",
				$"{HEADER_GENERATED_PREFIX}{DateTime.UtcNow:O}",
				HEADER_WARNING,
			};
		}

		/// <summary>
		/// Prepends the SSoT header comment block to the existing content of the file at <paramref name="_filePath"/>.
		/// If the file already has an SSoT header it is replaced.
		/// </summary>
		/// <param name="_filePath">Absolute path of the PO/POT file to modify.</param>
		/// <param name="_bridgeName">Human-readable name of the bridge asset.</param>
		/// <param name="_bridgeGuid">Unity GUID of the bridge asset.</param>
		/// <param name="_sourceUrl">URL of the source spreadsheet.</param>
		public static void WriteHeaderToFile(string _filePath, string _bridgeName, string _bridgeGuid, string _sourceUrl)
		{
			try
			{
				string existing = File.Exists(_filePath) ? File.ReadAllText(_filePath, Encoding.UTF8) : string.Empty;

				// Strip existing SSoT header if present
				if (existing.TrimStart().StartsWith(HEADER_LINE_1, StringComparison.Ordinal))
				{
					var po = PoFile.Parse(existing);
					po.HeaderLines = StripSsotLines(po.HeaderLines);
					existing = po.Serialize();
				}

				var headerLines = GenerateHeaderLines(_bridgeName, _bridgeGuid, _sourceUrl);
				var sb = new StringBuilder();
				foreach (var line in headerLines)
					sb.AppendLine(line);
				sb.Append(existing);

				File.WriteAllText(_filePath, sb.ToString(), Encoding.UTF8);
			}
			catch (Exception e)
			{
				UiLog.LogError($"PoSsotHeader: failed to write header to '{_filePath}': {e.Message}");
			}
		}

		/// <summary>
		/// Strips all SSoT header comment lines from the supplied list, leaving other comment lines intact.
		/// </summary>
		/// <param name="_lines">Input comment lines.</param>
		/// <returns>New list with SSoT lines removed.</returns>
		public static List<string> StripSsotLines(List<string> _lines)
		{
			var result = new List<string>();
			foreach (var line in _lines)
			{
				if (line.StartsWith(HEADER_LINE_1,       StringComparison.Ordinal)) continue;
				if (line.StartsWith(HEADER_BRIDGE_PREFIX,StringComparison.Ordinal)) continue;
				if (line.StartsWith(HEADER_SOURCE_PREFIX,StringComparison.Ordinal)) continue;
				if (line.StartsWith(HEADER_GENERATED_PREFIX, StringComparison.Ordinal)) continue;
				if (line.StartsWith(HEADER_WARNING,      StringComparison.Ordinal)) continue;
				result.Add(line);
			}
			return result;
		}
	}
}
