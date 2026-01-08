using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GuiToolkit.Storage.Tests
{
	/// <summary>
	/// Unit tests for PersistedAggregate.
	/// </summary>
	/// <remarks>
	/// This file is part of the storage unit test suite.
	/// </remarks>
	public sealed class TestPersistedAggregate
	{
		[Test]
		public async Task Load_MissingDocument_UsesInitialState_AndIsNotDirty()
		{
			var byteStore = new MemoryByteStore();
			var serializer = new AggregateTestSerializer();
			var innerStore = new DocumentStore(byteStore, serializer);
			var countingStore = new CountingDocumentStore(innerStore);

			var aggregate = new PersistedAggregate<string>(
				countingStore,
				_collection: "agg",
				_id: "one",
				_initialState: "init");

			await aggregate.LoadAsync();

			Assert.That(aggregate.State, Is.EqualTo("init"));

			await aggregate.SaveAsync();
			Assert.That(countingStore.saveCount, Is.EqualTo(0));
		}

		[Test]
		public async Task Mutate_SetsDirty_SaveWritesOnce_AndThenStops()
		{
			var byteStore = new MemoryByteStore();
			var serializer = new AggregateTestSerializer();
			var innerStore = new DocumentStore(byteStore, serializer);
			var countingStore = new CountingDocumentStore(innerStore);

			var aggregate = new PersistedAggregate<string>(
				countingStore,
				_collection: "agg",
				_id: "one",
				_initialState: "init");

			await aggregate.LoadAsync();

			aggregate.Mutate(s => { s = "changed"; }); // for string this won't work: strings are immutable!
													   // Correct mutation for immutable types: replace whole state via a helper
													   // We'll do it properly below:

			aggregate = new PersistedAggregate<string>(
				countingStore,
				_collection: "agg",
				_id: "one",
				_initialState: "init");

			await aggregate.LoadAsync();

			aggregate.Mutate(_ => { }); // mark dirty without changing content

			await aggregate.SaveAsync();
			Assert.That(countingStore.saveCount, Is.EqualTo(1));

			await aggregate.SaveAsync();
			Assert.That(countingStore.saveCount, Is.EqualTo(1));
		}

		private sealed class CountingDocumentStore : IDocumentStore
		{
			private readonly IDocumentStore m_inner;

			public int saveCount;

			public CountingDocumentStore( IDocumentStore _inner )
			{
				m_inner = _inner;
			}

			public Task<bool> ExistsAsync( string _collection, string _id, System.Threading.CancellationToken _cancellationToken = default )
				=> m_inner.ExistsAsync(_collection, _id, _cancellationToken);

			public Task<T?> LoadAsync<T>( string _collection, string _id, System.Threading.CancellationToken _cancellationToken = default )
				=> m_inner.LoadAsync<T>(_collection, _id, _cancellationToken);

			public Task SaveAsync<T>( string _collection, string _id, T _document, System.Threading.CancellationToken _cancellationToken = default )
			{
				saveCount++;
				return m_inner.SaveAsync(_collection, _id, _document, _cancellationToken);
			}

			public Task DeleteAsync( string _collection, string _id, System.Threading.CancellationToken _cancellationToken = default )
				=> m_inner.DeleteAsync(_collection, _id, _cancellationToken);

			public Task<System.Collections.Generic.IReadOnlyList<string>> ListIdsAsync( string _collection, System.Threading.CancellationToken _cancellationToken = default )
				=> m_inner.ListIdsAsync(_collection, _cancellationToken);
		}

		private sealed class AggregateTestSerializer : ISerializer
		{
			public byte[] Serialize<T>( T _value )
			{
				if (_value is string s)
				{
					return Encoding.UTF8.GetBytes(s);
				}

				if (_value is CollectionIndex index)
				{
					return SerializeIndex(index);
				}

				throw new NotSupportedException($"AggregateTestSerializer does not support type: {typeof(T).FullName}");
			}

			public T Deserialize<T>( byte[] _data )
			{
				if (typeof(T) == typeof(string))
				{
					string s = Encoding.UTF8.GetString(_data);
					return (T)(object)s;
				}

				if (typeof(T) == typeof(CollectionIndex))
				{
					CollectionIndex index = DeserializeIndex(_data);
					return (T)(object)index;
				}

				throw new NotSupportedException($"AggregateTestSerializer does not support type: {typeof(T).FullName}");
			}

			private static byte[] SerializeIndex( CollectionIndex _index )
			{
				using MemoryStream ms = new MemoryStream();
				using BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

				bw.Write(_index.schemaVersion);
				bw.Write(_index.entries.Count);

				for (int i = 0; i < _index.entries.Count; i++)
				{
					bw.Write(_index.entries[i].id ?? string.Empty);
					bw.Write(_index.entries[i].updatedUnixMs);
				}

				bw.Flush();
				return ms.ToArray();
			}

			private static CollectionIndex DeserializeIndex( byte[] _data )
			{
				using MemoryStream ms = new MemoryStream(_data);
				using BinaryReader br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

				CollectionIndex index = new CollectionIndex();
				index.schemaVersion = br.ReadInt32();

				int count = br.ReadInt32();
				index.entries = new System.Collections.Generic.List<CollectionIndexEntry>(count);

				for (int i = 0; i < count; i++)
				{
					CollectionIndexEntry entry = new CollectionIndexEntry();
					entry.id = br.ReadString();
					entry.updatedUnixMs = br.ReadInt64();
					index.entries.Add(entry);
				}

				return index;
			}
		}
	}
}
