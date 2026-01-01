using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Low-level abstraction for storing and retrieving raw bytes by key.
	/// </summary>
	/// <remarks>
	/// This interface is intentionally backend-agnostic.
	/// Higher-level APIs such as IDocumentStore build on top of it.
	/// </remarks>
	public interface IByteStore
	{
		/// <summary>
		/// Checks whether data exists for the given key.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>True if the key exists; otherwise false.</returns>
		Task<bool> ExistsAsync( string _key, CancellationToken _cancellationToken = default );
		/// <summary>
		/// Loads data for the given key.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>The stored bytes, or null if the key does not exist.</returns>
		Task<byte[]?> LoadAsync( string _key, CancellationToken _cancellationToken = default );
		/// <summary>
		/// Saves data under the given key.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_data">Payload bytes to store.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		Task SaveAsync( string _key, byte[] _data, CancellationToken _cancellationToken = default );
		/// <summary>
		/// Deletes data for the given key.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		Task DeleteAsync( string _key, CancellationToken _cancellationToken = default );

		/// <summary>
		/// Lists all keys that start with the given prefix.
		/// </summary>
		/// <param name="_prefix">Key prefix to match.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>Matching keys.</returns>
		Task<IReadOnlyList<string>> ListKeysAsync(
			string _prefix,
			CancellationToken _cancellationToken = default
		);
	}
}
