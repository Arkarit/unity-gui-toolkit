namespace GuiToolkit.Storage
{
	/// <summary>
	/// Serialization abstraction used by the storage system.
	/// </summary>
	/// <remarks>
	/// Serializers must be deterministic and stable for persisted data.
	/// Implementations may choose any encoding (e.g. JSON, binary).
	/// </remarks>
	public interface ISerializer
	{
		/// <summary>
		/// Serializes a value into a byte array.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="_value">Value to serialize.</param>
		/// <returns>Serialized bytes.</returns>
		byte[] Serialize<T>( T _value );
		/// <summary>
		/// Deserializes a value from a byte array.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="_data">Serialized bytes.</param>
		/// <returns>Deserialized value.</returns>
		/// <exception cref="System.InvalidOperationException">Thrown if the data cannot be deserialized to the requested type.</exception>
		T Deserialize<T>( byte[] _data );
	}
}
