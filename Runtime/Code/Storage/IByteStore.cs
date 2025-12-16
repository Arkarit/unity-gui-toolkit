using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public interface IByteStore
	{
		Task<bool> ExistsAsync( string _key, CancellationToken _cancellationToken = default );
		Task<byte[]?> LoadAsync( string _key, CancellationToken _cancellationToken = default );
		Task SaveAsync( string _key, byte[] _data, CancellationToken _cancellationToken = default );
		Task DeleteAsync( string _key, CancellationToken _cancellationToken = default );

		Task<IReadOnlyList<string>> ListKeysAsync(
			string _prefix,
			CancellationToken _cancellationToken = default
		);
	}
}
