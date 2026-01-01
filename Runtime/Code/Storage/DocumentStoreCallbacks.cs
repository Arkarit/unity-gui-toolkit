using System;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Callback-based convenience wrappers for IDocumentStore operations.
	/// </summary>
	/// <remarks>
	/// These helpers translate async/await calls into success/failure callbacks.
	/// Callbacks are posted back to the main thread via Storage.PostToMainThread().
	/// </remarks>
	/// <seealso cref="Storage.PostToMainThread"/>
	public static class DocumentStoreCallbacks
	{
		/// <summary>
		/// Loads a document and reports the result via callbacks.
		/// </summary>
		/// <typeparam name="T">Document type.</typeparam>
		/// <param name="_store">Document store instance.</param>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_id">Document id.</param>
		/// <param name="_onSuccess">Called on the main thread with the loaded document (or null if missing).</param>
		/// <param name="_onFail">Called on the main thread if an exception occurs.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		public static void Load<T>(
			this GuiToolkit.Storage.IDocumentStore _store,
			string _collection,
			string _id,
			Action<T?> _onSuccess,
			Action<StorageError> _onFail,
			CancellationToken _cancellationToken = default )
		{
			_ = LoadInternal();

			async Task LoadInternal()
			{
				try
				{
					T? result = await _store.LoadAsync<T>(_collection, _id, _cancellationToken);
					Storage.PostToMainThread(() => _onSuccess(result));
				}
				catch (Exception ex)
				{
					Storage.PostToMainThread(() => _onFail(new StorageError(ex)));
				}
			}
		}

		/// <summary>
		/// Saves a document and reports completion via callbacks.
		/// </summary>
		/// <typeparam name="T">Document type.</typeparam>
		/// <param name="_store">Document store instance.</param>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_id">Document id.</param>
		/// <param name="_document">Document instance to save.</param>
		/// <param name="_onSuccess">Called on the main thread after a successful save.</param>
		/// <param name="_onFail">Called on the main thread if an exception occurs.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		public static void Save<T>(
			this GuiToolkit.Storage.IDocumentStore _store,
			string _collection,
			string _id,
			T _document,
			Action _onSuccess,
			Action<StorageError> _onFail,
			CancellationToken _cancellationToken = default )
		{
			_ = SaveInternal();

			async Task SaveInternal()
			{
				try
				{
					await _store.SaveAsync(_collection, _id, _document, _cancellationToken);
					Storage.PostToMainThread(_onSuccess);
				}
				catch (Exception ex)
				{
					Storage.PostToMainThread(() => _onFail(new StorageError(ex)));
				}
			}
		}
	}
}