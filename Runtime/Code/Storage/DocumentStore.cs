using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public sealed class DocumentStore : IDocumentStore
	{
		private readonly IByteStore _byteStore;
		private readonly ISerializer _serializer;

		public DocumentStore( IByteStore _byteStore, ISerializer _serializer )
		{
			this._byteStore = _byteStore;
			this._serializer = _serializer;
		}

		public async Task<bool> ExistsAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default )
		{
			string key = BuildDocKey(_collection, _id);
			return await _byteStore.ExistsAsync(key, _cancellationToken);
		}

		public async Task<T?> LoadAsync<T>(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default )
		{
			string key = BuildDocKey(_collection, _id);

			byte[]? data = await _byteStore.LoadAsync(key, _cancellationToken);
			if (data == null)
			{
				return default;
			}

			return _serializer.Deserialize<T>(data);
		}

		public async Task SaveAsync<T>(
			string _collection,
			string _id,
			T _document,
			CancellationToken _cancellationToken = default )
		{
			string docKey = BuildDocKey(_collection, _id);
			byte[] data = _serializer.Serialize(_document);

			await _byteStore.SaveAsync(docKey, data, _cancellationToken);

			await UpsertIndexAsync(_collection, _id, _cancellationToken);
		}

		public async Task DeleteAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default )
		{
			string docKey = BuildDocKey(_collection, _id);

			await _byteStore.DeleteAsync(docKey, _cancellationToken);
			await RemoveFromIndexAsync(_collection, _id, _cancellationToken);
		}

		public async Task<IReadOnlyList<string>> ListIdsAsync(
			string _collection,
			CancellationToken _cancellationToken = default )
		{
			CollectionIndex? index = await LoadIndexAsync(_collection, _cancellationToken);
			if (index != null)
			{
				List<string> ids = new List<string>(index.entries.Count);
				for (int i = 0; i < index.entries.Count; i++)
				{
					ids.Add(index.entries[i].id);
				}
				return ids;
			}

			// Fallback: list keys by prefix (works fine for local file store, may be slow for backend).
			string prefix = BuildCollectionPrefix(_collection);
			IReadOnlyList<string> keys = await _byteStore.ListKeysAsync(prefix, _cancellationToken);

			List<string> fallbackIds = new List<string>();
			for (int i = 0; i < keys.Count; i++)
			{
				string? id = TryExtractIdFromDocKey(_collection, keys[i]);
				if (id != null)
				{
					fallbackIds.Add(id);
				}
			}

			return fallbackIds;
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
			CancellationToken _cancellationToken )
		{
			string indexKey = BuildIndexKey(_collection);
			byte[]? data = await _byteStore.LoadAsync(indexKey, _cancellationToken);
			if (data == null)
			{
				return null;
			}

			return _serializer.Deserialize<CollectionIndex>(data);
		}

		private async Task SaveIndexAsync(
			string _collection,
			CollectionIndex _index,
			CancellationToken _cancellationToken )
		{
			string indexKey = BuildIndexKey(_collection);
			byte[] data = _serializer.Serialize(_index);
			await _byteStore.SaveAsync(indexKey, data, _cancellationToken);
		}

		private async Task UpsertIndexAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken )
		{
			CollectionIndex index =
				await LoadIndexAsync(_collection, _cancellationToken) ??
				new CollectionIndex();

			long nowUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			for (int i = 0; i < index.entries.Count; i++)
			{
				if (index.entries[i].id == _id)
				{
					index.entries[i].updatedUnixMs = nowUnixMs;
					await SaveIndexAsync(_collection, index, _cancellationToken);
					return;
				}
			}

			index.entries.Add(new CollectionIndexEntry
			{
				id = _id,
				updatedUnixMs = nowUnixMs
			});

			await SaveIndexAsync(_collection, index, _cancellationToken);
		}

		private async Task RemoveFromIndexAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken )
		{
			CollectionIndex? index = await LoadIndexAsync(_collection, _cancellationToken);
			if (index == null)
			{
				return;
			}

			for (int i = index.entries.Count - 1; i >= 0; i--)
			{
				if (index.entries[i].id == _id)
				{
					index.entries.RemoveAt(i);
				}
			}

			await SaveIndexAsync(_collection, index, _cancellationToken);
		}
	}
}