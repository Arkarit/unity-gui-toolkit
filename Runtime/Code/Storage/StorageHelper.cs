using System.Collections.Generic;

namespace GuiToolkit.Storage
{
	public sealed class CollectionIndex
	{
		public int schemaVersion = 1;
		public List<CollectionIndexEntry> entries = new List<CollectionIndexEntry>();
	}

	public sealed class CollectionIndexEntry
	{
		public string id = string.Empty;
		public long updatedUnixMs;
	}
}
