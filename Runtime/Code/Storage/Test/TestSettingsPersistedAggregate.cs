using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GuiToolkit.Settings;
using NUnit.Framework;

namespace GuiToolkit.Storage.Tests
{
	public sealed class TestSettingsPersistedAggregate
	{
		[Test]
		public async Task ManyMutations_SaveOnce_ReloadRestoresAllValues()
		{
			MemoryByteStore byteStore = new MemoryByteStore();
			ISerializer serializer = new SettingsAggregateTestSerializer();

			IDocumentStore innerStore = new DocumentStore(byteStore, serializer);
			CountingDocumentStore countingStore = new CountingDocumentStore(innerStore);

			SettingsPersistedAggregate settings = new SettingsPersistedAggregate(countingStore, "test", "user");

			await settings.LoadAsync();

			for (int i = 0; i < 100; i++)
			{
				settings.SetInt($"k{i}", i);
			}

			await settings.SaveAsync();

			Assert.That(countingStore.saveCount, Is.EqualTo(1));

			// Saving again without changes must do nothing.
			await settings.SaveAsync();
			Assert.That(countingStore.saveCount, Is.EqualTo(1));

			// Reload via a new instance, proving persistence.
			SettingsPersistedAggregate settingsReloaded = new SettingsPersistedAggregate(innerStore, "test", "user");
			await settingsReloaded.LoadAsync();

			Assert.That(settingsReloaded.GetInt("k0", -1), Is.EqualTo(0));
			Assert.That(settingsReloaded.GetInt("k1", -1), Is.EqualTo(1));
			Assert.That(settingsReloaded.GetInt("k42", -1), Is.EqualTo(42));
			Assert.That(settingsReloaded.GetInt("k99", -1), Is.EqualTo(99));
		}

		private sealed class CountingDocumentStore : IDocumentStore
		{
			private readonly IDocumentStore m_inner;

			public int saveCount;

			public CountingDocumentStore( IDocumentStore _inner )
			{
				m_inner = _inner;
			}

			public Task<bool> ExistsAsync(
				string _collection,
				string _id,
				System.Threading.CancellationToken _cancellationToken = default )
			{
				return m_inner.ExistsAsync(_collection, _id, _cancellationToken);
			}

			public Task<T?> LoadAsync<T>(
				string _collection,
				string _id,
				System.Threading.CancellationToken _cancellationToken = default )
			{
				return m_inner.LoadAsync<T>(_collection, _id, _cancellationToken);
			}

			public Task SaveAsync<T>(
				string _collection,
				string _id,
				T _document,
				System.Threading.CancellationToken _cancellationToken = default )
			{
				// Count only the aggregate document itself (not other collections).
				if (_collection == "settings" && _id == "user")
				{
					saveCount++;
				}

				return m_inner.SaveAsync(_collection, _id, _document, _cancellationToken);
			}

			public Task DeleteAsync(
				string _collection,
				string _id,
				System.Threading.CancellationToken _cancellationToken = default )
			{
				return m_inner.DeleteAsync(_collection, _id, _cancellationToken);
			}

			public Task<IReadOnlyList<string>> ListIdsAsync(
				string _collection,
				System.Threading.CancellationToken _cancellationToken = default )
			{
				return m_inner.ListIdsAsync(_collection, _cancellationToken);
			}
		}

		private sealed class SettingsAggregateTestSerializer : ISerializer
		{
			public byte[] Serialize<T>( T _value )
			{
				if (_value is SettingsData settings)
				{
					return SerializeSettings(settings);
				}

				if (_value is CollectionIndex index)
				{
					return SerializeIndex(index);
				}

				throw new NotSupportedException($"SettingsAggregateTestSerializer does not support type: {typeof(T).FullName}");
			}

			public T Deserialize<T>( byte[] _data )
			{
				if (typeof(T) == typeof(SettingsData))
				{
					SettingsData settings = DeserializeSettings(_data);
					return (T)(object)settings;
				}

				if (typeof(T) == typeof(CollectionIndex))
				{
					CollectionIndex index = DeserializeIndex(_data);
					return (T)(object)index;
				}

				throw new NotSupportedException($"SettingsAggregateTestSerializer does not support type: {typeof(T).FullName}");
			}

			private static byte[] SerializeSettings( SettingsData _settings )
			{
				using MemoryStream ms = new MemoryStream();
				using BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

				WriteIntDict(bw, _settings.ints);
				WriteFloatDict(bw, _settings.floats);
				WriteStringDict(bw, _settings.strings);
				WriteBoolDict(bw, _settings.bools);

				bw.Flush();
				return ms.ToArray();
			}

			private static SettingsData DeserializeSettings( byte[] _data )
			{
				using MemoryStream ms = new MemoryStream(_data);
				using BinaryReader br = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);

				SettingsData settings = new SettingsData();

				ReadIntDict(br, settings.ints);
				ReadFloatDict(br, settings.floats);
				ReadStringDict(br, settings.strings);
				ReadBoolDict(br, settings.bools);

				return settings;
			}

			private static void WriteIntDict( BinaryWriter _bw, Dictionary<string, int> _dict )
			{
				_bw.Write(_dict.Count);
				foreach (var kvp in _dict)
				{
					_bw.Write(kvp.Key ?? string.Empty);
					_bw.Write(kvp.Value);
				}
			}

			private static void ReadIntDict( BinaryReader _br, Dictionary<string, int> _dict )
			{
				int count = _br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					string key = _br.ReadString();
					int value = _br.ReadInt32();
					_dict[key] = value;
				}
			}

			private static void WriteFloatDict( BinaryWriter _bw, Dictionary<string, float> _dict )
			{
				_bw.Write(_dict.Count);
				foreach (var kvp in _dict)
				{
					_bw.Write(kvp.Key ?? string.Empty);
					_bw.Write(kvp.Value);
				}
			}

			private static void ReadFloatDict( BinaryReader _br, Dictionary<string, float> _dict )
			{
				int count = _br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					string key = _br.ReadString();
					float value = _br.ReadSingle();
					_dict[key] = value;
				}
			}

			private static void WriteStringDict( BinaryWriter _bw, Dictionary<string, string> _dict )
			{
				_bw.Write(_dict.Count);
				foreach (var kvp in _dict)
				{
					_bw.Write(kvp.Key ?? string.Empty);
					_bw.Write(kvp.Value ?? string.Empty);
				}
			}

			private static void ReadStringDict( BinaryReader _br, Dictionary<string, string> _dict )
			{
				int count = _br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					string key = _br.ReadString();
					string value = _br.ReadString();
					_dict[key] = value;
				}
			}

			private static void WriteBoolDict( BinaryWriter _bw, Dictionary<string, bool> _dict )
			{
				_bw.Write(_dict.Count);
				foreach (var kvp in _dict)
				{
					_bw.Write(kvp.Key ?? string.Empty);
					_bw.Write(kvp.Value);
				}
			}

			private static void ReadBoolDict( BinaryReader _br, Dictionary<string, bool> _dict )
			{
				int count = _br.ReadInt32();
				for (int i = 0; i < count; i++)
				{
					string key = _br.ReadString();
					bool value = _br.ReadBoolean();
					_dict[key] = value;
				}
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
				index.entries = new List<CollectionIndexEntry>(count);

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
