using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GuiToolkit.Storage.Tests
{
	internal sealed class TestSerializer : ISerializer
	{
		public byte[] Serialize<T>( T _value )
		{
			if (_value is string s)
			{
				return SerializeString(s);
			}

			if (_value is CollectionIndex index)
			{
				return SerializeIndex(index);
			}

			throw new NotSupportedException($"TestSerializer does not support type: {typeof(T).FullName}");
		}

		public T Deserialize<T>( byte[] _data )
		{
			if (typeof(T) == typeof(string))
			{
				string s = DeserializeString(_data);
				return (T)(object)s;
			}

			if (typeof(T) == typeof(CollectionIndex))
			{
				CollectionIndex index = DeserializeIndex(_data);
				return (T)(object)index;
			}

			throw new NotSupportedException($"TestSerializer does not support type: {typeof(T).FullName}");
		}

		private static byte[] SerializeString( string _s )
		{
			return Encoding.UTF8.GetBytes(_s);
		}

		private static string DeserializeString( byte[] _data )
		{
			return Encoding.UTF8.GetString(_data);
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
