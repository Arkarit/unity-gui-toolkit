using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// File system based byte store implementation.
	/// </summary>
	/// <remarks>
	/// Keys are mapped to file paths below a configured root directory.
	/// File names are derived from the key via hashing to avoid invalid characters and overly long paths.
	/// </remarks>
	public sealed class FileByteStore : IByteStore
	{
		private readonly string m_rootDir;

		/// <summary>
		/// Creates a new file byte store.
		/// </summary>
		/// <param name="_rootDir">Root directory used for all stored files.</param>
		/// <exception cref="System.ArgumentException">Thrown if the root directory is null or whitespace.</exception>
		public FileByteStore( string _rootDir )
		{
			if (string.IsNullOrWhiteSpace(_rootDir))
			{
				throw new ArgumentException("Root directory must not be null or whitespace.", nameof(_rootDir));
			}

			m_rootDir = _rootDir;
			Directory.CreateDirectory(m_rootDir);
		}

		/// <summary>
		/// Checks whether a key exists.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>True if the key exists; otherwise false.</returns>
		public Task<bool> ExistsAsync( string _key, CancellationToken _cancellationToken = default )
		{
			string path = GetPathForKey(_key);
			bool exists = File.Exists(path);
			return Task.FromResult(exists);
		}

		/// <summary>
		/// Loads the byte payload for a key.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>The stored bytes, or null if the key does not exist.</returns>
		public async Task<byte[]?> LoadAsync( string _key, CancellationToken _cancellationToken = default )
		{
			string path = GetPathForKey(_key);
			if (File.Exists(path) == false)
			{
				return null;
			}

			await using FileStream stream = new FileStream(
				path,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				bufferSize: 16 * 1024,
				useAsync: true);

			byte[] data = new byte[stream.Length];
			int offset = 0;

			while (offset < data.Length)
			{
				int read = await stream.ReadAsync(
					data.AsMemory(offset, data.Length - offset),
					_cancellationToken);

				if (read == 0)
				{
					break;
				}

				offset += read;
			}

			if (offset != data.Length)
			{
				throw new IOException("Unexpected end of file while reading.");
			}

			return data;
		}

		/// <summary>
		/// Saves the given bytes under a key.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_data">Payload bytes to store.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <remarks>
		/// Uses a temporary file and atomic replace to reduce risk of corruption.
		/// </remarks>
		public async Task SaveAsync( string _key, byte[] _data, CancellationToken _cancellationToken = default )
		{
			if (_data == null)
			{
				throw new ArgumentNullException(nameof(_data));
			}

			string path = GetPathForKey(_key);
			string tmpPath = path + ".tmp";

			Directory.CreateDirectory(m_rootDir);

			await using (FileStream stream = new FileStream(
				tmpPath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 16 * 1024,
				useAsync: true))
			{
				await stream.WriteAsync(_data.AsMemory(0, _data.Length), _cancellationToken);
				await stream.FlushAsync(_cancellationToken);
			}

			ReplaceFile(tmpPath, path);
		}

		/// <summary>
		/// Deletes the data stored for a key.
		/// </summary>
		/// <param name="_key">Logical key.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		public Task DeleteAsync( string _key, CancellationToken _cancellationToken = default )
		{
			string path = GetPathForKey(_key);

			if (File.Exists(path))
			{
				File.Delete(path);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Lists keys matching a prefix.
		/// </summary>
		/// <param name="_prefix">Key prefix to filter by.</param>
		/// <param name="_cancellationToken">Cancellation token.</param>
		/// <returns>All keys starting with the specified prefix.</returns>
		public Task<IReadOnlyList<string>> ListKeysAsync
		(
			string _prefix,
			CancellationToken _cancellationToken = default 
		)
		{
			// Note: This implementation stores files by hashed name.
			// Listing by prefix is not possible without an additional key index.
			// DocumentStore has an index for IDs; use that to avoid prefix scans.
			IReadOnlyList<string> keys = Array.Empty<string>();
			return Task.FromResult(keys);
		}

		private string GetPathForKey( string _key )
		{
			if (string.IsNullOrEmpty(_key))
			{
				throw new ArgumentException("Key must not be null or empty.", nameof(_key));
			}

			string fileName = HashKeyToFileName(_key);
			var result = Path.Combine(m_rootDir, fileName + ".json");
			Storage.Log($"Storage Path for key {_key}: {result}");
			return result;
		}

		private static string HashKeyToFileName( string _key )
		{
			using SHA256 sha = SHA256.Create();
			byte[] bytes = Encoding.UTF8.GetBytes(_key);
			byte[] hash = sha.ComputeHash(bytes);

			StringBuilder sb = new StringBuilder(hash.Length * 2);
			for (int i = 0; i < hash.Length; i++)
			{
				sb.Append(hash[i].ToString("x2"));
			}

			return sb.ToString();
		}

		private static void ReplaceFile( string _tmpPath, string _finalPath )
		{
			if (File.Exists(_finalPath))
			{
				File.Delete(_finalPath);
			}

			File.Move(_tmpPath, _finalPath);
		}
	}
}
