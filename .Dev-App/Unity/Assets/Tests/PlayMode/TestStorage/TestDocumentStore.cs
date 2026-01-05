using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GuiToolkit.Storage.Tests
{
	/// <summary>
	/// Unit tests for DocumentStore.
	/// </summary>
	/// <remarks>
	/// This file is part of the storage unit test suite.
	/// </remarks>
	public sealed class TestDocumentStore
	{
		[Test]
		public async Task SaveLoadExistsDelete_Works()
		{
			MemoryByteStore byteStore = new MemoryByteStore();
			ISerializer serializer = new TestSerializer();

			DocumentStore store = new DocumentStore(byteStore, serializer);

			string collection = "settings";
			string id = "user";

			Assert.That(await store.ExistsAsync(collection, id), Is.False);

			await store.SaveAsync(collection, id, "Hello");

			Assert.That(await store.ExistsAsync(collection, id), Is.True);

			string loaded = await store.LoadAsync<string>(collection, id);
			Assert.That(loaded, Is.EqualTo("Hello"));

			await store.DeleteAsync(collection, id);

			Assert.That(await store.ExistsAsync(collection, id), Is.False);

			string? loadedAfterDelete = await store.LoadAsync<string>(collection, id);
			Assert.That(loadedAfterDelete, Is.Null);
		}

		[Test]
		public async Task ListIdsAsync_UsesIndexAndReturnsIds()
		{
			MemoryByteStore byteStore = new MemoryByteStore();
			ISerializer serializer = new TestSerializer();

			DocumentStore store = new DocumentStore(byteStore, serializer);

			string collection = "diary";

			await store.SaveAsync(collection, "a", "A");
			await store.SaveAsync(collection, "b", "B");
			await store.SaveAsync(collection, "c", "C");

			var ids = await store.ListIdsAsync(collection);

			Assert.That(ids, Does.Contain("a"));
			Assert.That(ids, Does.Contain("b"));
			Assert.That(ids, Does.Contain("c"));
			Assert.That(ids.Count, Is.EqualTo(3));
		}

		[Test]
		public async Task SaveSameIdTwice_DoesNotDuplicateIndexEntry()
		{
			MemoryByteStore byteStore = new MemoryByteStore();
			ISerializer serializer = new TestSerializer();

			DocumentStore store = new DocumentStore(byteStore, serializer);

			string collection = "diary";
			string id = "a";

			await store.SaveAsync(collection, id, "First");
			await store.SaveAsync(collection, id, "Second");

			var ids = await store.ListIdsAsync(collection);

			Assert.That(ids, Does.Contain(id));
			Assert.That(ids.Count, Is.EqualTo(1));
			Assert.That(ids.Count(_ => _ == id), Is.EqualTo(1));

			string loaded = await store.LoadAsync<string>(collection, id);
			Assert.That(loaded, Is.EqualTo("Second"));
		}
	}
}
