namespace GuiToolkit.Storage
{
	/// <summary>
	/// Per-request context passed through storage layers.
	/// </summary>
	/// <remarks>
	/// Currently only carries an opaque Payload value.
	/// Backends may interpret this payload to provide authentication, routing, tracing, etc.
	/// </remarks>
	public readonly struct StorageRequestContext
	{
		/// <summary>
		/// Default context with a null payload.
		/// </summary>
		/// <returns>Default request context.</returns>
		public static readonly StorageRequestContext Default = new StorageRequestContext(null);

		/// <summary>
		/// Opaque per-request payload.
		/// </summary>
		/// <returns>Payload instance, or null.</returns>
		public object? Payload { get; }

		/// <summary>
		/// Creates a new request context.
		/// </summary>
		/// <param name="_payload">Opaque payload object, interpreted by the backend if needed.</param>
		public StorageRequestContext( object? _payload )
		{
			Payload = _payload;
		}
	}
}
