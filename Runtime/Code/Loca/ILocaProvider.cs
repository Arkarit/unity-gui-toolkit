using System;
using System.Collections.Generic;

namespace GuiToolkit
{
	[Serializable]
	public sealed class ProcessedLoca
	{
		public string Group = string.Empty;
		public List<ProcessedLocaEntry> Entries = new();
		
		public ProcessedLoca() {}
		public ProcessedLoca(string _group, List<ProcessedLocaEntry>  _entries)
		{
			Group = _group;
			Entries = _entries;
			SortByKey();
		}

		private void SortByKey()
		{
			if (Entries == null || Entries.Count <= 1)
				return;

			Entries.Sort(static ( a, b ) =>
			{
				// null checks first
				if (a == null && b == null) return 0;
				if (a == null) return 1;
				if (b == null) return -1;

				// empty keys after non-empty
				bool aEmpty = string.IsNullOrEmpty(a.Key);
				bool bEmpty = string.IsNullOrEmpty(b.Key);
				if (aEmpty && bEmpty) return 0;
				if (aEmpty) return 1;
				if (bEmpty) return -1;

				int keyCompare = string.Compare(a.Key, b.Key, StringComparison.Ordinal);
				if (keyCompare != 0)
					return keyCompare;

				// secondary: LanguageId
				return string.Compare(a.LanguageId, b.LanguageId, StringComparison.Ordinal);
			});
		}
	}

	[Serializable]
	public sealed class ProcessedLocaEntry
	{
		public string Key;
		public string LanguageId;
		public string Text;       // optional
		public string[] Forms;    // optional, length up to 6
	}

	public interface ILocaProvider
	{
		public ProcessedLoca Localization { get; }

#if UNITY_EDITOR
		public void CollectData();
#endif
	}
}
