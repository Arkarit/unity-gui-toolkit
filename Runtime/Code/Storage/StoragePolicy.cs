namespace GuiToolkit.Storage
{
	/// <summary>
	/// Storage routing policy for a collection.
	/// </summary>
	/// <remarks>
	/// Policies are resolved when building a routed byte store.
	/// Some values are reserved for future use.
	/// </remarks>
	public enum StoragePolicy
	{
		LocalOnly,
		BackendOnly,

		// Reserved for later:
		MirrorWrite,
		CacheReadThrough
	}
}
