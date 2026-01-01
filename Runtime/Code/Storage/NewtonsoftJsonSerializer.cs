using System;
using Newtonsoft.Json;
using System.Text;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// JSON serializer implementation based on Newtonsoft.Json.
	/// </summary>
	/// <remarks>
	/// Uses UTF-8 encoded JSON.
	/// TypeNameHandling is disabled by default to avoid persisting CLR type information.
	/// </remarks>
	public sealed class NewtonsoftJsonSerializer : ISerializer
	{
		private readonly JsonSerializerSettings m_settings;

		/// <summary>
		/// Creates a serializer with optional custom settings.
		/// </summary>
		/// <param name="_settings">Optional JsonSerializerSettings. If null, a safe default configuration is used.</param>
		public NewtonsoftJsonSerializer( JsonSerializerSettings? _settings = null )
		{
			m_settings = _settings ?? new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.None,
				Formatting = Formatting.Indented
			};
		}

		/// <summary>
		/// Serializes a value to UTF-8 encoded JSON bytes.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="_value">Value to serialize.</param>
		/// <returns>UTF-8 JSON bytes.</returns>
		public byte[] Serialize<T>( T _value )
		{
			string json = JsonConvert.SerializeObject(_value, m_settings);
			return Encoding.UTF8.GetBytes(json);
		}

		/// <summary>
		/// Deserializes a value from UTF-8 encoded JSON bytes.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="_data">UTF-8 JSON bytes.</param>
		/// <returns>Deserialized value.</returns>
		/// <exception cref="System.InvalidOperationException">Thrown if deserialization returns null.</exception>
		public T Deserialize<T>( byte[] _data )
		{
			if (_data == null)
			{
				throw new ArgumentNullException(nameof(_data));
			}

			string json = Encoding.UTF8.GetString(_data);
			T? value = JsonConvert.DeserializeObject<T>(json, m_settings);

			if (value == null)
			{
				throw new InvalidOperationException("Deserialization returned null.");
			}

			return value;
		}
	}
}
