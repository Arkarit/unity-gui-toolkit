using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public interface IContextualByteStore : IByteStore
	{
		Task<bool> ExistsAsync(
			string _key,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default );

		Task<byte[]?> LoadAsync(
			string _key,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default );

		Task SaveAsync(
			string _key,
			byte[] _data,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default );

		Task DeleteAsync(
			string _key,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default );

		Task<IReadOnlyList<string>> ListKeysAsync(
			string _prefix,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default );
	}
}
