using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public sealed class MemoryByteStore : IByteStore
	{
		private readonly Dictionary<string, byte[]> m_data = new Dictionary<string, byte[]>();

		public Task<bool> ExistsAsync( string _key, CancellationToken _cancellationToken = default )
		{
			bool exists = m_data.ContainsKey(_key);
			return Task.FromResult(exists);
		}

		public Task<byte[]?> LoadAsync( string _key, CancellationToken _cancellationToken = default )
		{
			if (m_data.TryGetValue(_key, out byte[] data) == false)
			{
				return Task.FromResult<byte[]?>(null);
			}

			byte[] copy = new byte[data.Length];
			Buffer.BlockCopy(data, 0, copy, 0, data.Length);

			return Task.FromResult<byte[]?>(copy);
		}

		public Task SaveAsync( string _key, byte[] _data, CancellationToken _cancellationToken = default )
		{
			if (_data == null)
			{
				throw new ArgumentNullException(nameof(_data));
			}

			byte[] copy = new byte[_data.Length];
			Buffer.BlockCopy(_data, 0, copy, 0, _data.Length);

			m_data[_key] = copy;

			return Task.CompletedTask;
		}

		public Task DeleteAsync( string _key, CancellationToken _cancellationToken = default )
		{
			m_data.Remove(_key);
			return Task.CompletedTask;
		}

		public Task<IReadOnlyList<string>> ListKeysAsync(
			string _prefix,
			CancellationToken _cancellationToken = default )
		{
			if (string.IsNullOrEmpty(_prefix))
			{
				return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
			}

			List<string> keys = new List<string>();

			foreach (KeyValuePair<string, byte[]> kvp in m_data)
			{
				if (kvp.Key.StartsWith(_prefix, StringComparison.Ordinal))
				{
					keys.Add(kvp.Key);
				}
			}

			return Task.FromResult<IReadOnlyList<string>>(keys);
		}
	}
}
