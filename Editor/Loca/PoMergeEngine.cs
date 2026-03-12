using System.Collections.Generic;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Statistics and per-key lists produced by a single <see cref="PoMergeEngine.Merge"/> run.
	/// </summary>
	public class PoMergeResult
	{
		/// <summary>Number of new keys found in the POT that were absent from the existing PO.</summary>
		public int AddedKeys;

		/// <summary>Number of keys present in the existing PO but absent from the POT (now marked obsolete).</summary>
		public int ObsoleteKeys;

		/// <summary>Number of keys found in both POT and PO; their translations were preserved.</summary>
		public int PreservedKeys;

		/// <summary>Number of preserved keys that were already marked fuzzy in the existing PO.</summary>
		public int FuzzyKeys;

		/// <summary>The individual added keys (composed keys).</summary>
		public List<string> AddedKeysList = new List<string>();

		/// <summary>The individual obsoleted keys (composed keys).</summary>
		public List<string> ObsoleteKeysList = new List<string>();
	}

	/// <summary>
	/// Merges a POT template into an existing PO file using a conservative strategy:
	/// preserve all existing translations, add new keys from the POT with empty msgstr,
	/// and mark PO keys not in the POT as obsolete.
	/// </summary>
	public static class PoMergeEngine
	{
		/// <summary>
		/// Merges <paramref name="_pot"/> (template) into <paramref name="_existingPo"/> (current translation).
		/// </summary>
		/// <param name="_existingPo">The existing translated PO file.</param>
		/// <param name="_pot">The POT template file (typically has empty msgstr values).</param>
		/// <param name="_markObsolete">
		/// When true (default), keys in <paramref name="_existingPo"/> that are not in <paramref name="_pot"/>
		/// are appended as obsolete entries. When false they are simply dropped.
		/// </param>
		/// <returns>
		/// A tuple of the merged <see cref="PoFile"/> and a <see cref="PoMergeResult"/> with merge statistics.
		/// </returns>
		public static (PoFile merged, PoMergeResult result) Merge(PoFile _existingPo, PoFile _pot, bool _markObsolete = true)
		{
			var result = new PoMergeResult();

			var merged = new PoFile
			{
				HasHeader    = _existingPo.HasHeader,
				HeaderLines  = new List<string>(_existingPo.HeaderLines),
				HeaderMsgStr = _existingPo.HeaderMsgStr,
				Entries      = new List<PoEntry>()
			};

			var existingLookup = _existingPo.BuildLookup();
			var potLookup      = _pot.BuildLookup();

			// Active entries in POT order
			foreach (var potEntry in _pot.Entries)
			{
				if (potEntry.IsObsolete)
					continue;

				string key = potEntry.ComposedKey;
				var newEntry = CloneEntry(potEntry);

				if (existingLookup.TryGetValue(key, out var existing))
				{
					newEntry.MsgStr      = existing.MsgStr;
					newEntry.MsgStrForms = existing.MsgStrForms != null ? (string[])existing.MsgStrForms.Clone() : null;
					newEntry.IsFuzzy     = existing.IsFuzzy;
					result.PreservedKeys++;
					if (existing.IsFuzzy)
						result.FuzzyKeys++;
				}
				else
				{
					result.AddedKeys++;
					result.AddedKeysList.Add(key);
				}

				merged.Entries.Add(newEntry);
			}

			// Keys in PO not in POT become obsolete (or dropped)
			foreach (var existingEntry in _existingPo.Entries)
			{
				if (existingEntry.IsObsolete)
				{
					merged.Entries.Add(CloneEntry(existingEntry));
					continue;
				}

				string key = existingEntry.ComposedKey;
				if (!potLookup.ContainsKey(key))
				{
					result.ObsoleteKeys++;
					result.ObsoleteKeysList.Add(key);

					if (_markObsolete)
					{
						var obsoleteEntry = CloneEntry(existingEntry);
						obsoleteEntry.IsObsolete = true;
						merged.Entries.Add(obsoleteEntry);
					}
				}
			}

			return (merged, result);
		}

		private static PoEntry CloneEntry(PoEntry _entry)
		{
			return new PoEntry
			{
				TranslatorComments = new List<string>(_entry.TranslatorComments),
				SourceReferences   = new List<string>(_entry.SourceReferences),
				IsFuzzy            = _entry.IsFuzzy,
				IsObsolete         = _entry.IsObsolete,
				Context            = _entry.Context,
				MsgId              = _entry.MsgId,
				MsgIdPlural        = _entry.MsgIdPlural,
				MsgStr             = _entry.MsgStr,
				MsgStrForms        = _entry.MsgStrForms != null ? (string[])_entry.MsgStrForms.Clone() : null,
			};
		}
	}
}
