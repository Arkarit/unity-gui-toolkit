using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public sealed class DocumentStore : IDocumentStore
	{
		private readonly IByteStore m_byteStore;
		private readonly ISerializer m_serializer;

		public DocumentStore( IByteStore _byteStore, ISerializer _serializer )
		{
			m_byteStore = _byteStore;
			m_serializer = _serializer;
		}

		public Task<bool> ExistsAsync( string _collection, string _id, CancellationToken _cancellationToken = default )
		{
			return ExistsAsync(_collection, _id, StorageRequestContext.Default, _cancellationToken);
		}

		public Task<T?> LoadAsync<T>( string _collection, string _id, CancellationToken _cancellationToken = default )
		{
			return LoadAsync<T>(_collection, _id, StorageRequestContext.Default, _cancellationToken);
		}

		public Task SaveAsync<T>( string _collection, string _id, T _document, CancellationToken _cancellationToken = default )
		{
			return SaveAsync(_collection, _id, _document, StorageRequestContext.Default, _cancellationToken);
		}

		public Task DeleteAsync( string _collection, string _id, CancellationToken _cancellationToken = default )
		{
			return DeleteAsync(_collection, _id, StorageRequestContext.Default, _cancellationToken);
		}

		public Task<IReadOnlyList<string>> ListIdsAsync( string _collection, CancellationToken _cancellationToken = default )
		{
			return ListIdsAsync(_collection, StorageRequestContext.Default, _cancellationToken);
		}

		public async Task<bool> ExistsAsync(
			string _collection,
			string _id,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default )
		{
			string key = BuildDocKey(_collection, _id);
			return await ExistsBytesAsync(key, _context, _cancellationToken);
		}

		public async Task<T?> LoadAsync<T>(
			string _collection,
			string _id,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default )
		{
			string key = BuildDocKey(_collection, _id);

			byte[]? data = await LoadBytesAsync(key, _context, _cancellationToken);
			if (data == null)
				return default;

			return m_serializer.Deserialize<T>(data);
		}

		public async Task SaveAsync<T>(
			string _collection,
			string _id,
			T _document,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default )
		{
			string docKey = BuildDocKey(_collection, _id);
			byte[] data = m_serializer.Serialize(_document);

			await SaveBytesAsync(docKey, data, _context, _cancellationToken);
			await UpsertIndexAsync(_collection, _id, _context, _cancellationToken);
		}

		public async Task DeleteAsync(
			string _collection,
			string _id,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default )
		{
			string docKey = BuildDocKey(_collection, _id);

			await DeleteBytesAsync(docKey, _context, _cancellationToken);
			await RemoveFromIndexAsync(_collection, _id, _context, _cancellationToken);
		}

		public async Task<IReadOnlyList<string>> ListIdsAsync(
			string _collection,
			StorageRequestContext _context,
			CancellationToken _cancellationToken = default )
		{
			CollectionIndex? index = await LoadIndexAsync(_collection, _context, _cancellationToken);
			if (index != null)
			{
				List<string> ids = new List<string>(index.entries.Count);
				for (int i = 0; i < index.entries.Count; i++)
					ids.Add(index.entries[i].id);

				return ids;
			}

			string prefix = BuildCollectionPrefix(_collection);
			IReadOnlyList<string> keys = await ListKeysAsync(prefix, _context, _cancellationToken);

			List<string> fallbackIds = new List<string>();
			for (int i = 0; i < keys.Count; i++)
			{
				string? id = TryExtractIdFromDocKey(_collection, keys[i]);
				if (id != null)
					fallbackIds.Add(id);
			}

			return fallbackIds;
		}

		private Task<bool> ExistsBytesAsync( string _key, StorageRequestContext _context, CancellationToken _ct )
		{
			if (m_byteStore is IContextualByteStore contextual)
				return contextual.ExistsAsync(_key, _context, _ct);

			return m_byteStore.ExistsAsync(_key, _ct);
		}

		private Task<byte[]?> LoadBytesAsync( string _key, StorageRequestContext _context, CancellationToken _ct )
		{
			if (m_byteStore is IContextualByteStore contextual)
				return contextual.LoadAsync(_key, _context, _ct);

			return m_byteStore.LoadAsync(_key, _ct);
		}

		private Task SaveBytesAsync( string _key, byte[] _data, StorageRequestContext _context, CancellationToken _ct )
		{
			if (m_byteStore is IContextualByteStore contextual)
				return contextual.SaveAsync(_key, _data, _context, _ct);

			return m_byteStore.SaveAsync(_key, _data, _ct);
		}

		private Task DeleteBytesAsync( string _key, StorageRequestContext _context, CancellationToken _ct )
		{
			if (m_byteStore is IContextualByteStore contextual)
				return contextual.DeleteAsync(_key, _context, _ct);

			return m_byteStore.DeleteAsync(_key, _ct);
		}

		private Task<IReadOnlyList<string>> ListKeysAsync( string _prefix, StorageRequestContext _context, CancellationToken _ct )
		{
			if (m_byteStore is IContextualByteStore contextual)
				return contextual.ListKeysAsync(_prefix, _context, _ct);

			return m_byteStore.ListKeysAsync(_prefix, _ct);
		}

		private static string BuildCollectionPrefix( string _collection )
		{
			return $"doc/{_collection}/";
		}

		private static string BuildDocKey( string _collection, string _id )
		{
			return $"doc/{_collection}/{_id}";
		}

		private static string BuildIndexKey( string _collection )
		{
			return $"doc/{_collection}/_index";
		}

		private static string? TryExtractIdFromDocKey( string _collection, string _key )
		{
			string prefix = BuildCollectionPrefix(_collection);
			if (_key.StartsWith(prefix, StringComparison.Ordinal) == false)
			{
				return null;
			}

			string tail = _key.Substring(prefix.Length);
			if (tail == "_index")
			{
				return null;
			}

			return tail;
		}

		private async Task<CollectionIndex?> LoadIndexAsync(
			string _collection,
			StorageRequestContext _context,
			CancellationToken _cancellationToken )
		{
			string indexKey = BuildIndexKey(_collection);
			byte[]? data = await LoadBytesAsync(indexKey, _context, _cancellationToken);
			if (data == null)
				return null;

			return m_serializer.Deserialize<CollectionIndex>(data);
		}

		private async Task SaveIndexAsync(
			string _collection,
			CollectionIndex _index,
			StorageRequestContext _context,
			CancellationToken _cancellationToken )
		{
			string indexKey = BuildIndexKey(_collection);
			byte[] data = m_serializer.Serialize(_index);
			await SaveBytesAsync(indexKey, data, _context, _cancellationToken);
		}

		private async Task UpsertIndexAsync(
			string _collection,
			string _id,
			StorageRequestContext _context,
			CancellationToken _cancellationToken )
		{
			CollectionIndex index =
				await LoadIndexAsync(_collection, _context, _cancellationToken) ??
				new CollectionIndex();

			long nowUnixMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			for (int i = 0; i < index.entries.Count; i++)
			{
				if (index.entries[i].id == _id)
				{
					index.entries[i].updatedUnixMs = nowUnixMs;
					await SaveIndexAsync(_collection, index, _context, _cancellationToken);
					return;
				}
			}

			index.entries.Add(new CollectionIndexEntry
			{
				id = _id,
				updatedUnixMs = nowUnixMs
			});

			await SaveIndexAsync(_collection, index, _context, _cancellationToken);
		}

		private async Task RemoveFromIndexAsync(
			string _collection,
			string _id,
			StorageRequestContext _context,
			CancellationToken _cancellationToken )
		{
			CollectionIndex? index = await LoadIndexAsync(_collection, _context, _cancellationToken);
			if (index == null)
				return;

			for (int i = index.entries.Count - 1; i >= 0; i--)
			{
				if (index.entries[i].id == _id)
					index.entries.RemoveAt(i);
			}

			await SaveIndexAsync(_collection, index, _context, _cancellationToken);
		}
	}
}