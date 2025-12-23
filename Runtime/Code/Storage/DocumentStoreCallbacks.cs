using System;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public static class DocumentStoreCallbacks
	{
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