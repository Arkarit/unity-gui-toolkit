using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GuiToolkit.Storage.Tests
{
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
	}
}
