using System.Collections.Generic;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Index document used to track documents within a collection.
	/// </summary>
	/// <remarks>
	/// The index can be used to list ids and to record last update timestamps.
	/// It is stored as a regular document next to the collection content.
	/// </remarks>
	public sealed class CollectionIndex
	{
		/// <summary>
		/// Schema version of the index document.
		/// </summary>
		/// <returns>Schema version.</returns>
		public int schemaVersion = 1;
		/// <summary>
		/// Collection entries.
		/// </summary>
		/// <returns>List of entries.</returns>
		public List<CollectionIndexEntry> entries = new List<CollectionIndexEntry>();
	}

	/// <summary>
	/// Index entry describing a single document.
	/// </summary>
	/// <remarks>
	/// Stores the document id and the last updated timestamp in Unix milliseconds.
	/// </remarks>
	public sealed class CollectionIndexEntry
	{
		/// <summary>
		/// Document id.
		/// </summary>
		/// <returns>Id string.</returns>
		public string id = string.Empty;
		/// <summary>
		/// Last update timestamp in Unix milliseconds.
		/// </summary>
		/// <returns>Unix time in milliseconds.</returns>
		public long updatedUnixMs;
	}
}
