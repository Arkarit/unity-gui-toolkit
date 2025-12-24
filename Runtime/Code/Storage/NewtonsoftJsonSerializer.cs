using System;
using Newtonsoft.Json;
using System.Text;

namespace GuiToolkit.Storage
{
	public sealed class NewtonsoftJsonSerializer : ISerializer
	{
		private readonly JsonSerializerSettings m_settings;

		public NewtonsoftJsonSerializer( JsonSerializerSettings? _settings = null )
		{
			m_settings = _settings ?? new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.None
			};
		}

		public byte[] Serialize<T>( T _value )
		{
			string json = JsonConvert.SerializeObject(_value, m_settings);
			return Encoding.UTF8.GetBytes(json);
		}

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
