using System;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GuiToolkit.Storage.Tests
{
	/// <summary>
	/// Unit tests for storage routing behavior.
	/// </summary>
	/// <remarks>
	/// This file is part of the storage unit test suite.
	/// </remarks>
	public sealed class TestStorageRouting
	{
		[Test]
		public async Task RoutingByteStore_RoutesByPrefix()
		{
			MemoryByteStore local = new MemoryByteStore();
			MemoryByteStore backend = new MemoryByteStore();

			RoutingByteStore routing = new RoutingByteStore(local)
				.AddRoute("doc/settings/", local)
				.AddRoute("doc/profile/", backend);

			byte[] a = Encoding.UTF8.GetBytes("A");
			byte[] b = Encoding.UTF8.GetBytes("B");

			await routing.SaveAsync("doc/settings/user", a);
			await routing.SaveAsync("doc/profile/user", b);

			Assert.That(await local.ExistsAsync("doc/settings/user"), Is.True);
			Assert.That(await local.ExistsAsync("doc/profile/user"), Is.False);

			Assert.That(await backend.ExistsAsync("doc/profile/user"), Is.True);
			Assert.That(await backend.ExistsAsync("doc/settings/user"), Is.False);
		}

		[Test]
		public async Task RoutingByteStore_LongestPrefixWins()
		{
			MemoryByteStore aStore = new MemoryByteStore();
			MemoryByteStore bStore = new MemoryByteStore();

			RoutingByteStore routing = new RoutingByteStore(aStore)
				.AddRoute("doc/", aStore)
				.AddRoute("doc/settings/", bStore);

			byte[] data = Encoding.UTF8.GetBytes("X");

			await routing.SaveAsync("doc/settings/user", data);

			Assert.That(await bStore.ExistsAsync("doc/settings/user"), Is.True);
			Assert.That(await aStore.ExistsAsync("doc/settings/user"), Is.False);
		}

		[Test]
		public void StorageFactory_BackendOnlyWithoutBackend_Throws()
		{
			MemoryByteStore local = new MemoryByteStore();
			ISerializer serializer = new DummySerializer();

			StorageRoutingConfig config = new StorageRoutingConfig(local, serializer)
				.SetPolicy("profile", StoragePolicy.BackendOnly);

			Assert.Throws<InvalidOperationException>(() =>
			{
				StaticStorageFactory.CreateDocumentStore(config);
			});
		}

		private sealed class DummySerializer : ISerializer
		{
			public byte[] Serialize<T>( T _value )
			{
				throw new NotSupportedException();
			}

			public T Deserialize<T>( byte[] _data )
			{
				throw new NotSupportedException();
			}
		}
	}
}
