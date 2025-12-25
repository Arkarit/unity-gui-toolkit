namespace GuiToolkit.Storage
{
	public enum StoragePolicy
	{
		LocalOnly,
		BackendOnly,

		// Reserved for later:
		MirrorWrite,
		CacheReadThrough
	}
}
