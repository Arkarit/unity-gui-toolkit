using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace GuiToolkit.Storage
{

	public interface IDocumentStore
	{
		Task<bool> ExistsAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default
		);

		Task<T?> LoadAsync<T>(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default
		);

		Task SaveAsync<T>(
			string _collection,
			string _id,
			T _document,
			CancellationToken _cancellationToken = default
		);

		Task DeleteAsync(
			string _collection,
			string _id,
			CancellationToken _cancellationToken = default
		);

		Task<IReadOnlyList<string>> ListIdsAsync(
			string _collection,
			CancellationToken _cancellationToken = default
		);
	}
}
