using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit
{
	/// <summary>
	/// Links render texture producers (cameras) and consumers (RawImages, renderers) by keyword and
	/// creates the render textures on demand, so a project does not need a static .renderTexture asset
	/// for every camera that renders into the UI.
	///
	/// Rules:
	/// <list type="bullet">
	/// <item>A texture exists for a keyword as long as there is at least one producer and a size is known:
	/// either the producer's <see cref="RenderTextureSpec.FixedSize"/> or the largest size requested by a consumer.</item>
	/// <item>Registration order does not matter. Whoever comes later gets the texture as soon as it exists.</item>
	/// <item>The texture object for a keyword stays the same while it exists. Resizing releases and recreates it
	/// in place; all users are notified nevertheless, so they can refresh.</item>
	/// <item>Consumer sizes are polled once per frame, right before rendering, after the canvas layout ran.</item>
	/// </list>
	/// </summary>
	public static class RenderTextureManager
	{
		private sealed class Entry
		{
			public readonly string Key;
			public readonly List<IRenderTextureProducer> Producers = new();
			public readonly List<IRenderTextureConsumer> Consumers = new();
			public RenderTexture Texture;
			// What the texture was last configured with. Compared instead of the texture's own properties,
			// because a platform may substitute values (e.g. a 32 bit depth buffer for 24 requested).
			public Vector2Int AppliedSize;
			public RenderTextureSpec AppliedSpec;
			public bool Dirty = true;

			public Entry( string _key ) => Key = _key;
			public bool IsEmpty => Producers.Count == 0 && Consumers.Count == 0;
		}

		private static readonly Dictionary<string, Entry> s_entries = new();
		private static readonly List<Entry> s_flushList = new();
		private static Texture2D s_placeholder;
		private static int s_uniqueKeyCounter;
		private static bool s_playerLoopInstalled;

		/// <summary>
		/// A 1x1 fully transparent texture. Consumers show it while there is no render texture,
		/// because a RawImage without texture renders as a white rectangle.
		/// </summary>
		public static Texture2D Placeholder
		{
			get
			{
				if (s_placeholder == null)
				{
					s_placeholder = new Texture2D(1, 1, TextureFormat.RGBA32, false)
					{
						name = "RenderTextureManager.Placeholder",
						hideFlags = HideFlags.HideAndDontSave
					};
					s_placeholder.SetPixel(0, 0, Color.clear);
					s_placeholder.Apply(false, true);
				}

				return s_placeholder;
			}
		}

		/// <summary>Number of keywords currently known (with or without texture).</summary>
		public static int Count => s_entries.Count;

		/// <summary>One line per keyword for diagnostics: size, format, memory, who uses it. Replaces the contents of _result.</summary>
		public static void CollectInfo( List<RenderTextureInfo> _result )
		{
			_result.Clear();
			foreach (var entry in s_entries.Values)
			{
				var texture = entry.Texture;
				_result.Add(new RenderTextureInfo
				{
					Key = entry.Key,
					HasTexture = texture != null,
					IsCreated = texture != null && texture.IsCreated(),
					Width = texture != null ? texture.width : 0,
					Height = texture != null ? texture.height : 0,
					Format = texture != null ? texture.format : RenderTextureFormat.Default,
					DepthBits = entry.AppliedSpec.SanitizedDepthBits,
					MsaaSamples = entry.AppliedSpec.SanitizedMsaaSamples,
					MipMaps = entry.AppliedSpec.UseMipMaps,
					Producers = entry.Producers.Count,
					Consumers = entry.Consumers.Count,
					Bytes = texture != null ? UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture) : 0,
				});
			}
		}

		/// <summary>Creates a keyword nobody else uses, e.g. for one 3D icon among many.</summary>
		public static string CreateUniqueKey( string _prefix = "rt" ) => $"{_prefix}#{++s_uniqueKeyCounter}";

		public static void RegisterProducer( string _key, IRenderTextureProducer _producer )
		{
			if (!CheckArgs(_key, _producer))
				return;

			var entry = GetOrCreateEntry(_key);
			if (entry.Producers.Contains(_producer))
				return;

			entry.Producers.Add(_producer);
			if (entry.Producers.Count > 1 && !entry.Producers[0].RenderTextureSpec.Equals(_producer.RenderTextureSpec))
				UiLog.LogWarning($"Producers for render texture '{_key}' have different specs; the first one is used", _producer as Object);

			entry.Dirty = true;
			Flush(entry);
		}

		public static void UnregisterProducer( string _key, IRenderTextureProducer _producer )
		{
			if (!TryGetEntry(_key, out var entry) || !entry.Producers.Remove(_producer))
				return;

			entry.Dirty = true;
			Flush(entry);
		}

		public static void RegisterConsumer( string _key, IRenderTextureConsumer _consumer )
		{
			if (!CheckArgs(_key, _consumer))
				return;

			var entry = GetOrCreateEntry(_key);
			if (entry.Consumers.Contains(_consumer))
				return;

			entry.Consumers.Add(_consumer);
			entry.Dirty = true;
			Flush(entry);
		}

		public static void UnregisterConsumer( string _key, IRenderTextureConsumer _consumer )
		{
			if (!TryGetEntry(_key, out var entry) || !entry.Consumers.Remove(_consumer))
				return;

			entry.Dirty = true;
			Flush(entry);
		}

		/// <summary>
		/// Re-evaluate a keyword at the end of this frame, e.g. after a producer changed its spec.
		/// Size changes of consumers are detected automatically and do not need this.
		/// </summary>
		public static void SetDirty( string _key )
		{
			if (TryGetEntry(_key, out var entry))
				entry.Dirty = true;
		}

		/// <summary>
		/// Current texture for a keyword, or null. Pending changes for that keyword are applied first,
		/// so the result is up to date even within the frame that changed it.
		/// </summary>
		public static RenderTexture GetTexture( string _key )
		{
			if (!TryGetEntry(_key, out var entry))
				return null;

			Flush(entry);
			return entry.Texture;
		}

		/// <summary>
		/// Applies all pending changes now. Called automatically once per frame right before rendering
		/// (player loop in play mode, editor update in edit mode).
		/// </summary>
		public static void FlushAll()
		{
			if (s_entries.Count == 0)
				return;

			// Flush may remove entries, so iterate a copy
			s_flushList.Clear();
			s_flushList.AddRange(s_entries.Values);
			foreach (var entry in s_flushList)
				Flush(entry);

			s_flushList.Clear();
		}

		/// <summary>
		/// Releases all textures and forgets all registrations. Users are notified with null.
		/// </summary>
		public static void ReleaseAll()
		{
			s_flushList.Clear();
			s_flushList.AddRange(s_entries.Values);
			s_entries.Clear();

			foreach (var entry in s_flushList)
			{
				if (entry.Texture == null)
					continue;

				var texture = entry.Texture;
				entry.Texture = null;
				Notify(entry);
				DestroyTexture(texture);
			}

			s_flushList.Clear();
		}

		private static void Flush( Entry _entry )
		{
			RemoveDestroyedUsers(_entry);

			if (_entry.IsEmpty)
			{
				if (_entry.Texture != null)
					DestroyTexture(_entry.Texture);

				s_entries.Remove(_entry.Key);
				return;
			}

			var spec = _entry.Producers.Count > 0 ? _entry.Producers[0].RenderTextureSpec : default;
			var size = _entry.Producers.Count > 0 ? ResolveSize(_entry, spec) : Vector2Int.zero;
			bool wanted = size.x > 0 && size.y > 0;

			if (!wanted)
			{
				if (_entry.Texture != null)
				{
					// Detach everyone first: Unity complains about releasing a camera's target texture
					var texture = _entry.Texture;
					_entry.Texture = null;
					Notify(_entry);
					DestroyTexture(texture);
				}
				else if (_entry.Dirty)
				{
					// newly registered users need to learn that there is no texture yet
					Notify(_entry);
				}

				_entry.Dirty = false;
				return;
			}

			bool changed = false;
			if (_entry.Texture == null)
			{
				_entry.Texture = CreateTexture(_entry.Key, size, spec);
				changed = true;
			}
			else if (size != _entry.AppliedSize || !IsSameTextureConfig(spec, _entry.AppliedSpec))
			{
				// Cameras must not target the texture while it is released
				for (int i = 0; i < _entry.Producers.Count; i++)
					SafeNotify(_entry.Producers[i], null);

				Reconfigure(_entry.Texture, size, spec);
				changed = true;
			}

			_entry.AppliedSize = size;
			_entry.AppliedSpec = spec;

			if (changed || _entry.Dirty)
				Notify(_entry);

			_entry.Dirty = false;
		}

		private static Vector2Int ResolveSize( Entry _entry, RenderTextureSpec _spec )
		{
			if (_spec.HasFixedSize)
				return Clamp(_spec.FixedSize);

			var size = Vector2Int.zero;
			foreach (var consumer in _entry.Consumers)
				size = Vector2Int.Max(size, consumer.RequestedSize);

			if (size.x <= 0 || size.y <= 0)
				return Vector2Int.zero;

			int granularity = Mathf.Max(1, _spec.SizeGranularity);
			size.x = (size.x + granularity - 1) / granularity * granularity;
			size.y = (size.y + granularity - 1) / granularity * granularity;
			return Clamp(size);
		}

		private static Vector2Int Clamp( Vector2Int _size )
		{
			int max = SystemInfo.maxTextureSize;
			return new Vector2Int(Mathf.Clamp(_size.x, 1, max), Mathf.Clamp(_size.y, 1, max));
		}

		private static RenderTexture CreateTexture( string _key, Vector2Int _size, RenderTextureSpec _spec )
		{
			var result = new RenderTexture(_size.x, _size.y, _spec.SanitizedDepthBits, _spec.ColorFormat, RenderTextureReadWrite.Default)
			{
				name = $"RenderTextureManager '{_key}'",
				hideFlags = HideFlags.HideAndDontSave,
				antiAliasing = _spec.SanitizedMsaaSamples,
				useMipMap = _spec.UseMipMaps,
				autoGenerateMips = _spec.UseMipMaps,
				filterMode = _spec.FilterMode,
				wrapMode = TextureWrapMode.Clamp,
			};

			result.Create();
			return result;
		}

		// Size related fields (fixed size, granularity) are already covered by the resolved size
		private static bool IsSameTextureConfig( RenderTextureSpec _a, RenderTextureSpec _b ) =>
			_a.ColorFormat == _b.ColorFormat
			&& _a.SanitizedDepthBits == _b.SanitizedDepthBits
			&& _a.SanitizedMsaaSamples == _b.SanitizedMsaaSamples
			&& _a.UseMipMaps == _b.UseMipMaps
			&& _a.FilterMode == _b.FilterMode;

		private static void Reconfigure( RenderTexture _texture, Vector2Int _size, RenderTextureSpec _spec )
		{
			// Everything but the filter mode can only be changed while the texture is released
			_texture.Release();
			_texture.width = _size.x;
			_texture.height = _size.y;
			_texture.format = _spec.ColorFormat;
			_texture.depth = _spec.SanitizedDepthBits;
			_texture.antiAliasing = _spec.SanitizedMsaaSamples;
			_texture.useMipMap = _spec.UseMipMaps;
			_texture.autoGenerateMips = _spec.UseMipMaps;
			_texture.filterMode = _spec.FilterMode;
			_texture.Create();
		}

		private static void DestroyTexture( RenderTexture _texture )
		{
			if (_texture == null)
				return;

			_texture.Release();
			if (Application.isPlaying)
				Object.Destroy(_texture);
			else
				Object.DestroyImmediate(_texture);
		}

		private static void Notify( Entry _entry )
		{
			// Producers first, so a camera targets the texture before anyone shows it
			for (int i = 0; i < _entry.Producers.Count; i++)
				SafeNotify(_entry.Producers[i], _entry.Texture);
			for (int i = 0; i < _entry.Consumers.Count; i++)
				SafeNotify(_entry.Consumers[i], _entry.Texture);
		}

		private static void SafeNotify( IRenderTextureUser _user, RenderTexture _texture )
		{
			try
			{
				_user.OnRenderTextureChanged(_texture);
			}
			catch (Exception e)
			{
				Debug.LogException(e, _user as Object);
			}
		}

		private static void RemoveDestroyedUsers( Entry _entry )
		{
			int removed = _entry.Producers.RemoveAll(IsDestroyed) + _entry.Consumers.RemoveAll(IsDestroyed);
			if (removed > 0)
				_entry.Dirty = true;
		}

		private static bool IsDestroyed( IRenderTextureUser _user ) => _user == null || _user is Object obj && obj == null;

		private static bool CheckArgs( string _key, IRenderTextureUser _user )
		{
			if (string.IsNullOrEmpty(_key))
			{
				UiLog.LogError("Render texture keyword must not be empty", _user as Object);
				return false;
			}

			if (_user == null)
			{
				UiLog.LogError($"Render texture user for '{_key}' is null");
				return false;
			}

			EnsureUpdateHook();
			return true;
		}

		private static Entry GetOrCreateEntry( string _key )
		{
			if (!s_entries.TryGetValue(_key, out var entry))
			{
				entry = new Entry(_key);
				s_entries.Add(_key, entry);
			}

			return entry;
		}

		private static bool TryGetEntry( string _key, out Entry _entry )
		{
			if (string.IsNullOrEmpty(_key))
			{
				_entry = null;
				return false;
			}

			return s_entries.TryGetValue(_key, out _entry);
		}

		#region Frame hook

		// Marker type for our player loop system
		private struct RenderTextureManagerFlush { }

		private static void EnsureUpdateHook()
		{
			if (Application.isPlaying)
				InstallPlayerLoopSystem();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Works with domain reload disabled: nothing can be registered this early in play mode
			ReleaseAll();
			s_uniqueKeyCounter = 0;
			s_playerLoopInstalled = false;
		}

		// Runs after the canvas layout and before the cameras render, so size changes from layout
		// are picked up within the same frame.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void InstallPlayerLoopSystem()
		{
			if (s_playerLoopInstalled)
				return;

			s_playerLoopInstalled = PlayerLoopUtility.InsertBeforeFrameRendering(typeof(RenderTextureManagerFlush), FlushAll);
		}

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		private static void InitEditor()
		{
			// The player loop does not run in edit mode; editor update ticks regardless of focus
			UnityEditor.EditorApplication.update += () =>
			{
				if (!Application.isPlaying)
					FlushAll();
			};

			// Textures are HideAndDontSave and would leak across a domain reload
			UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ReleaseAll;
		}
#endif

		#endregion
	}
}
