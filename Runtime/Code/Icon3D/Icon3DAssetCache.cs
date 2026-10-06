using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GuiToolkit.AssetHandling;
using GuiToolkit.Exceptions;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A caller's share of an object loaded for 3D icons. Poll <see cref="IsLoaded"/> / <see cref="HasFailed"/>;
	/// release when no longer needed.
	/// </summary>
	public sealed class Icon3DAssetLease
	{
		private Icon3DAssetCache.Entry m_entry;

		internal Icon3DAssetLease( Icon3DAssetCache.Entry _entry ) => m_entry = _entry;

		/// <summary>Canonical id the lease was taken for; empty after release.</summary>
		public string Id => m_entry?.Id ?? string.Empty;

		public bool IsReleased => m_entry == null;

		public bool IsLoaded
		{
			get
			{
				if (m_entry == null)
					return false;

				Icon3DAssetCache.Poll(m_entry);
				return m_entry.Asset != null;
			}
		}

		public bool HasFailed
		{
			get
			{
				if (m_entry == null)
					return false;

				Icon3DAssetCache.Poll(m_entry);
				return m_entry.HasFailed;
			}
		}

		/// <summary>The loaded prefab, or null while loading / after failure / after release.</summary>
		public GameObject Asset => IsLoaded ? m_entry.Asset : null;

		public void Release()
		{
			if (m_entry == null)
				return;

			Icon3DAssetCache.Release(m_entry);
			m_entry = null;
		}
	}

	/// <summary>
	/// Loads objects for 3D icons through <see cref="AssetManager"/>: one load per canonical id, shared by all
	/// icons showing it, released when the last one lets go.
	///
	/// Results are polled, never delivered by callback. An icon only ever looks at its current lease, so a load that
	/// completes after the icon moved on (pooled, reassigned, disabled) can not end up in the wrong icon. Loads given
	/// up before they completed are released as soon as they complete.
	/// </summary>
	public static class Icon3DAssetCache
	{
		internal sealed class Entry
		{
			public string Id;
			public int RefCount;
			public Task<IAssetHandle<GameObject>> Task;
			public CancellationTokenSource Cancellation;
			public IAssetHandle<GameObject> Handle;
			public GameObject Asset;
			public bool HasFailed;
		}

		private static readonly Dictionary<string, Entry> s_entries = new();
		private static readonly List<Entry> s_abandoned = new();

		/// <summary>Number of distinct ids currently loaded or loading.</summary>
		public static int Count => s_entries.Count;

		/// <summary>Start loading _id (or join a running/finished load) and return a lease on it.</summary>
		public static Icon3DAssetLease Acquire( string _id )
		{
			if (string.IsNullOrEmpty(_id))
				throw new ArgumentException("Id must not be empty", nameof(_id));

			if (!s_entries.TryGetValue(_id, out var entry))
			{
				entry = new Entry { Id = _id };
				s_entries.Add(_id, entry);
				Start(entry);
			}

			entry.RefCount++;
			return new Icon3DAssetLease(entry);
		}

		/// <summary>Release loads that were given up before they completed. Called once per renderer tick.</summary>
		internal static void Update()
		{
			for (int i = s_abandoned.Count - 1; i >= 0; i--)
			{
				var entry = s_abandoned[i];
				if (entry.Task != null && !entry.Task.IsCompleted)
					continue;

				ReleaseHandle(entry);
				s_abandoned.RemoveAt(i);
			}
		}

		internal static void Poll( Entry _entry )
		{
			if (_entry.Asset != null || _entry.HasFailed)
				return;

			// AssetManager was not initialized when the load was requested: try again
			if (_entry.Task == null)
			{
				Start(_entry);
				if (_entry.Task == null)
					return;
			}

			if (!_entry.Task.IsCompleted)
				return;

			if (_entry.Task.IsCanceled || _entry.Task.IsFaulted || _entry.Task.Result == null || _entry.Task.Result.Asset == null)
			{
				_entry.HasFailed = true;
				var reason = _entry.Task.Exception?.GetBaseException().Message ?? "no asset";
				UiLog.LogError($"3D icon object '{_entry.Id}' could not be loaded: {reason}");
				return;
			}

			_entry.Handle = _entry.Task.Result;
			_entry.Asset = _entry.Handle.Asset;
		}

		internal static void Release( Entry _entry )
		{
			if (--_entry.RefCount > 0)
				return;

			if (s_entries.TryGetValue(_entry.Id, out var registered) && registered == _entry)
				s_entries.Remove(_entry.Id);

			if (_entry.Task != null && !_entry.Task.IsCompleted)
			{
				// Can't release what is not there yet; finish the job when it arrives
				_entry.Cancellation?.Cancel();
				s_abandoned.Add(_entry);
				return;
			}

			ReleaseHandle(_entry);
		}

		/// <summary>Release everything, e.g. on shutdown. Leases still held become empty.</summary>
		public static void ReleaseAll()
		{
			foreach (var entry in s_entries.Values)
			{
				if (entry.Task != null && !entry.Task.IsCompleted)
				{
					entry.Cancellation?.Cancel();
					s_abandoned.Add(entry);
					continue;
				}

				ReleaseHandle(entry);
			}

			s_entries.Clear();
		}

		private static void Start( Entry _entry )
		{
			try
			{
				_entry.Cancellation = new CancellationTokenSource();
				_entry.Task = AssetManager.LoadAssetAsync(_entry.Id, _entry.Cancellation.Token);
			}
			catch (NotInitializedException)
			{
				_entry.Task = null;   // retried on the next poll
			}
			catch (Exception e)
			{
				_entry.HasFailed = true;
				UiLog.LogError($"3D icon object '{_entry.Id}' could not be loaded: {e.Message}");
			}
		}

		private static void ReleaseHandle( Entry _entry )
		{
			var task = _entry.Task;
			var handle = _entry.Handle;
			if (handle == null && task != null && task.Status == TaskStatus.RanToCompletion)
				handle = task.Result;

			try
			{
				handle?.Release();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			_entry.Handle = null;
			_entry.Asset = null;
			_entry.Cancellation?.Dispose();
			_entry.Cancellation = null;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			s_entries.Clear();
			s_abandoned.Clear();
		}
	}
}
