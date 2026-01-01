using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Byte store operations that accept a StorageRequestContext.
	/// </summary>
	/// <remarks>
	/// This is used when a backend requires per-request metadata (for example auth, routing or diagnostics payloads).
	/// </remarks>
	/// <seealso cref="StorageRequestContext"/>
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
