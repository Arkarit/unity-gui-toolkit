namespace GuiToolkit.Storage
{
	public readonly struct StorageRequestContext
	{
		public static readonly StorageRequestContext Default = new StorageRequestContext(null);

		public object? Payload { get; }

		public StorageRequestContext( object? _payload )
		{
			Payload = _payload;
		}
	}
}
