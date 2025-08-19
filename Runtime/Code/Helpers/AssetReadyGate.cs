using GuiToolkit.Debugging;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public static partial class AssetReadyGate
	{
		public static void SetInstanceRuntime<T>( ref T _instance, string _name ) where T : ScriptableObject
		{
			ThrowIfNotPlaying(_name);
			if (!_instance)
				_instance = RuntimeLoad<T>(_name);
		}

		public static T RuntimeLoad<T>( string _name ) where T : ScriptableObject
		{
			ThrowIfNotPlaying(_name);
			var result = Resources.Load<T>(_name);
			if (!result)
			{
				Debug.LogError($"Scriptable object could not be loaded from path '{_name}'");
				return ScriptableObject.CreateInstance<T>();
			}

			return result;
		}

#if UNITY_EDITOR
		public static bool TrySetInstance<T>( ref T _instance, string _name, string _assetPath ) where T : ScriptableObject
		{
			if (Application.isPlaying)
			{
				if (_instance == null)
					_instance = RuntimeLoad<T>(_name);
				return _instance != null;
			}

			if (ImportBusy() || ImporterPending(_assetPath))
				return false;

			_instance = EditorLoadOrCreate<T>(_name, _assetPath);
			return _instance != null;
		}

		// Gate for specific ScriptableObject assets (type+path), e.g. to ensure importer finished.
		// With getter and setter for members
		public static void WhenReady<T>
		(
			Func<T> get,
			Action<T> set,
			Func<bool> _additionalReady,
			Action<T> callback,
			string name,
			string _assetPath,
			int extraFrameTries = 5
		)
			where T : ScriptableObject
		{
			if (callback == null)
				return;

			if (Application.isPlaying)
			{
				if (get() != null)
				{
					callback(get());
					return;
				}

				var inst = RuntimeLoad<T>(name);
				set.Invoke(inst);
				callback(inst);
				return;
			}

			T current;

			// First try - only if not 
			if (Ready(_assetPath) && _additionalReady())
			{
				current = get();
				if (current != null)
				{
					callback(current);
					return;
				}
			}

			int countdown = extraFrameTries;
			EditorApplication.update += Tick;
			return;

			void Tick()
			{
				if (!Ready(_assetPath) || !_additionalReady())
					return;

				current = get();
				if (current != null)
				{
					EditorApplication.update -= Tick;
					callback(current);
					return;
				}

				if (countdown-- < 0)
				{
					EditorApplication.update -= Tick;
					Debug.LogError($"Could not load '{_assetPath}' after {extraFrameTries + 1} frames delay. Consider increasing the delay. ");
					return;
				}

				var inst = EditorLoadOrCreate<T>(name, _assetPath);
				if (inst != null)
				{
					set(inst);
					EditorApplication.update -= Tick;
					callback(inst);
				}
			}
		}

		// Gate for specific ScriptableObject assets (type+path), e.g. to ensure importer finished.
		public static void WhenReady(
			Action callback,
			(Type type, string assetPath)[] assets,
			int quietFrames = 0,
			int maxFrames = 0 // 0 = no timeout
		)
		{
			WhenReady(callback, null, assets, quietFrames, maxFrames);
		}

		// Gate for specific ScriptableObject assets (type+path), e.g. to ensure importer finished.
		public static void WhenReady(
			Action callback,
			Func<bool> conditionIfNotPlaying,
			(Type type, string assetPath)[] assets,
			int quietFrames = 0,
			int maxFrames = 0 // 0 = no timeout
		)
		{
			if (callback == null)
				return;

			assets ??= Array.Empty<(Type, string)>();

			// Validate types
			foreach (var asset in assets)
			{
				if (asset.type == null)
					throw new ArgumentException("Type must not be null.", nameof(assets));

				if (!typeof(ScriptableObject).IsAssignableFrom(asset.type))
					throw new ArgumentException(
						$"{nameof(WhenReady)} only supports ScriptableObject types, but got '{asset.type.Name}'.");
			}

			if (Application.isPlaying)
			{
				callback();
				return;
			}

			// Immediate fast path
			if (CompletelyLoaded())
			{
				callback();
				return;
			}

			int countdown = quietFrames;
			int frames = 0;

			EditorApplication.update += Tick;
			return;

			bool CompletelyLoaded()
			{
				if (conditionIfNotPlaying != null && !conditionIfNotPlaying())
					return false;

				if (ImportBusy())
					return false;

				foreach (var asset in assets)
				{
					if (ImporterPending(asset.assetPath))
						return false;

					foreach (var dep in AssetDatabase.GetDependencies(asset.assetPath, recursive: true))
					{
						if (ImporterPending(dep))
							return false;
					}
				}

				return true;
			}

			void Tick()
			{
				if (maxFrames > 0 && ++frames > maxFrames)
				{
					EditorApplication.update -= Tick;
					Debug.LogError($"WhenReady(assets) timeout after {maxFrames} frames. Caller: {DebugUtility.GetCallingClassAndMethod()}");
					return;
				}

				if (!CompletelyLoaded())
				{
					countdown = quietFrames;
					return;
				}

				if (countdown-- > 0)
					return;

				EditorApplication.update -= Tick;
				try { callback(); }
				catch (Exception ex) { Debug.LogException(ex); }
			}
		}

		// Generic gate: wait until isReady() becomes true (or immediately in play mode).
		public static void WhenReady(
			Func<bool> isReady,
			Action callback,
			int quietFrames = 0,
			int maxFrames = 0 // 0 = no timeout
		)
		{
			if (callback == null) return;
			if (isReady == null) throw new ArgumentNullException(nameof(isReady));

			// In play mode: assume ready (runtime should not depend on editor-import state).
			if (Application.isPlaying || isReady())
			{
				callback();
				return;
			}

			int countdown = quietFrames;
			int frames = 0;

			void Tick()
			{
				// optional timeout (useful in tests)
				if (maxFrames > 0 && ++frames > maxFrames)
				{
					EditorApplication.update -= Tick;
					Debug.LogError($"WhenReady timeout after {maxFrames} frames. Caller: {DebugUtility.GetCallingClassAndMethod()}");
					return;
				}

				if (!isReady())
				{
					// reset quiet-frames while not ready
					countdown = quietFrames;
					return;
				}

				// stay quiet for a few frames to avoid flapping right after isUpdating flips
				if (countdown-- > 0)
					return;

				EditorApplication.update -= Tick;
				try { callback(); }
				catch (Exception ex) { Debug.LogException(ex); }
			}

			// Fast path: try again right now to avoid one-frame delay
			if (isReady() && quietFrames <= 0)
			{
				callback();
				return;
			}

			EditorApplication.update += Tick;
		}

		// Convenience overload: only paths (no type check needed).
		public static void WhenReady( Action callback, params string[] assetPaths )
		{
			var items = new (Type, string)[assetPaths?.Length ?? 0];
			for (int i = 0; i < items.Length; i++)
				items[i] = (typeof(ScriptableObject), assetPaths[i]);
			WhenReady(callback, items);
		}

		// Convenience overload: only paths (no type check needed).
		public static void WhenReady( Action callback, Func<bool> conditionWhenNotRunning, params string[] assetPaths )
		{
			var items = new (Type, string)[assetPaths?.Length ?? 0];
			for (int i = 0; i < items.Length; i++)
				items[i] = (typeof(ScriptableObject), assetPaths[i]);
			WhenReady(callback, conditionWhenNotRunning, items);
		}

		public static void Save<T>( T _instance, string _name, string _assetPath ) where T : ScriptableObject
		{
			ThrowIfNotReady(_assetPath);

			if (_instance == null)
				_instance = EditorLoadOrCreate<T>(_name, _assetPath);

			EditorGeneralUtility.SetDirty(_instance);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}

		public static bool ImportBusy()
			=> EditorApplication.isCompiling || EditorApplication.isUpdating;

		public static bool ImporterPending( string assetPath )
			=> !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath))
			   && AssetDatabase.GetMainAssetTypeAtPath(assetPath) == null;

		public static bool Ready( string assetPath )
			=> Application.isPlaying || !(ImportBusy() || ImporterPending(assetPath));

		public static void ThrowIfNotReady( string assetPath, int _extraStackFrames = 0 )
		{
			if (!Ready(assetPath))
				throw new InvalidOperationException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} is not allowed during import/compile. " +
					$"Wrap with WhenReady(...). Asset: {assetPath}");
		}

		public static void ThrowIfNotPlaying( string _name, int _extraStackFrames = 0 )
		{
			if (!Application.isPlaying)
				throw new InvalidOperationException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} " +
					 $"(with '{_name}') is not allowed in Editor while not playing. Please use WhenReady(...) or InstanceOrNull.");
		}

		public static T EditorLoadOrCreate<T>( string _name, string _assetPath ) where T : ScriptableObject
		{
			if (string.IsNullOrEmpty(_assetPath))
				throw new System.InvalidOperationException("AssetPath not set for " + typeof(T).FullName);

			ThrowIfNotReady(_assetPath);

			var asset = AssetDatabase.LoadAssetAtPath<T>(_assetPath);
			if (asset)
				return asset;

			EditorFileUtility.EnsureUnityFolderExists(System.IO.Path.GetDirectoryName(_assetPath).Replace('\\', '/'));
			var inst = ScriptableObject.CreateInstance<T>();
			inst.name = _name;
			AssetDatabase.CreateAsset(inst, _assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			return AssetDatabase.LoadAssetAtPath<T>(_assetPath);
		}

#else
		public static bool TrySetInstance<T>( ref T _instance, string _name, string _assetPath ) where T : ScriptableObject
		{
			if (_instance == null)
				_instance = RuntimeLoad<T>(_name);
			return _instance != null;
		}

		public static void WhenReady<T>
		(
			Func<T> get,
			Action<T> set,
			Func<bool> _0,
			Action<T> callback,
			string name,
			string _1,
			int _2 = 5
		)
			where T : ScriptableObject
		{
			if (callback == null)
				return;

			if (get() != null)
			{
				callback(get());
				return;
			}

			var inst = RuntimeLoad<T>(name);
			set.Invoke(inst);
			callback(inst);
		}

		public static void WhenReady( Action callback, Func<bool> _0, params string[] _1 ) => callback?.Invoke();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady( Action callback, (Type type, string assetPath)[] _0, int _1 = 0, int _2 = 0 ) => callback?.Invoke();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady( Func<bool> _0, Action callback, int _1 = 0, int _2 = 0 ) => callback();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady( Action callback, params string[] assetPaths ) => callback?.Invoke();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Save<T>( T _0, string _1, string _2 ) where T : ScriptableObject {}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ImportBusy() => false;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ImporterPending( string assetPath ) => false;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Ready( string assetPath ) => true;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotReady( string assetPath ) { }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotPlaying( string _name, int _extraStackFrames = 0 ) { }
#endif
	}
}
