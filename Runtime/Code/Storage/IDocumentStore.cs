using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace GuiToolkit.Storage
{

	/// <summary>
	/// High-level abstraction for storing and retrieving typed documents.
	/// </summary>
	/// <remarks>
	/// Documents are grouped into collections and identified by an id.
	/// Implementations handle serialization and delegate persistence to an IByteStore.
	/// </remarks>
	/// <seealso cref="IByteStore"/>
	public interface IDocumentStore
	{
		/// <summary>
		/// Checks whether a document exists.
		/// </summary>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_id">Document id.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>True if the document exists; otherwise false.</returns>
		Task<bool> ExistsAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default
		);

		/// <summary>
		/// Loads a document.
		/// </summary>
		/// <typeparam name="T">Document type.</typeparam>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_id">Document id.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>The loaded document, or null if it does not exist.</returns>
		Task<T?> LoadAsync<T>(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default
		);

		/// <summary>
		/// Saves a document.
		/// </summary>
		/// <typeparam name="T">Document type.</typeparam>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_id">Document id.</param>
		/// <param name="_document">Document instance to save.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		Task SaveAsync<T>(
			string _collection,
			string _id,
			T _document,
			CancellationToken _cancellationToken = default
		);

		/// <summary>
		/// Deletes a document.
		/// </summary>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_id">Document id.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		Task DeleteAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default
		);

		/// <summary>
		/// Lists document ids in a collection.
		/// </summary>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>A list of document ids.</returns>
		Task<IReadOnlyList<string>> ListIdsAsync(
			string _collection,
			CancellationToken _cancellationToken = default
		);
	}
}
