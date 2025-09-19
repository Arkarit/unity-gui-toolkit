using GuiToolkit.Debugging;
using System;
using UnityEngine;

// Do not remove
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using GuiToolkit.Exceptions;
using Object = UnityEngine.Object;
using System.Runtime.InteropServices;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public static class AssetReadyGate
	{
		private static readonly HashSet<string> s_pathsDone = new();

		public static T RuntimeLoad<T>( string _name ) where T : ScriptableObject
		{
			ThrowIfNotPlaying(_name);
			var result = Resources.Load<T>(_name);
			if (!result)
			{
				Debug.LogError($"Scriptable object could not be loaded from path '{_name}' ");
				return ScriptableObject.CreateInstance<T>();
			}

			return result;
		}

#if UNITY_EDITOR
		public static void Clear() => s_pathsDone.Clear();

		private static bool AllAssetsDone( (Type type, string assetPath)[] _assets )
		{
			foreach (var asset in _assets)
			{
				if (!s_pathsDone.Contains(asset.assetPath))
					return false;
			}

			return true;
		}

		public static void WhenReady(
			Action _callback,
			Func<bool> _conditionIfNotPlaying,
			(Type type, string assetPath)[] _assets,
			int _quietFrames = 2,
			int _maxFrames = 30 // 0 = no timeout
		)
		{
			if (_callback == null)
				return;

			_assets ??= Array.Empty<(Type, string)>();

			// Validate types
			foreach (var asset in _assets)
			{
				if (asset.type == null)
					throw new ArgumentException("Type must not be null.", nameof(_assets));

				if (!typeof(ScriptableObject).IsAssignableFrom(asset.type))
					throw new ArgumentException(
						$"WhenReady() only supports ScriptableObject types, but got '{asset.type.Name}'.");
			}

			if (Application.isPlaying)
			{
				_callback();
				return;
			}

			// Immediate fast path
			if (AllAssetsDone(_assets))
			{
				_callback();
				return;
			}

			int countdown = _quietFrames;
			int frames = 0;

			EditorApplication.update += Tick;
			return;

			bool CompletelyLoaded()
			{
				if (_conditionIfNotPlaying != null && !_conditionIfNotPlaying())
					return false;

				if (ImportBusy())
					return false;

				foreach (var asset in _assets)
				{
					if (string.IsNullOrEmpty(asset.assetPath))
						continue;

					if (s_pathsDone.Contains(asset.assetPath))
						continue;

					if (ImporterPending(asset.assetPath))
						return false;

					foreach (var dep in AssetDatabase.GetDependencies(asset.assetPath, recursive: true))
					{
						if (ImporterPending(dep))
							return false;
					}

					s_pathsDone.Add(asset.assetPath);
				}

				return true;
			}

			void Tick()
			{
				if (_maxFrames > 0 && ++frames > _maxFrames)
				{
					EditorApplication.update -= Tick;
					Debug.LogError($"WhenReady(assets) timeout after {_maxFrames} frames. Caller: {DebugUtility.GetCallingClassAndMethod()}");
					return;
				}

				if (!CompletelyLoaded())
				{
					countdown = _quietFrames;
					return;
				}

				if (countdown-- > 0)
					return;

				EditorApplication.update -= Tick;
				try { _callback(); }
				catch (Exception ex) { Debug.LogException(ex); }
			}
		}

		// Convenience overload: only paths (no type check needed).
		public static void WhenReady( Action _callback, Func<bool> _conditionWhenNotRunning, params string[] _assetPaths )
		{
			var items = new (Type, string)[_assetPaths?.Length ?? 0];
			for (int i = 0; i < items.Length; i++)
				items[i] = (typeof(ScriptableObject), _assetPaths[i]);

			WhenReady(_callback, _conditionWhenNotRunning, items);
		}

		public static void WhenReady( Action _callback, params string[] _assetPaths ) => WhenReady(_callback, null, _assetPaths);

		public static bool ImportBusy()
			=> EditorApplication.isCompiling || EditorApplication.isUpdating;

		public static bool ImporterPending( string _assetPath )
			=> !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(_assetPath))
			   && AssetDatabase.GetMainAssetTypeAtPath(_assetPath) == null;

		public static bool Ready( params string[] _assetPaths )
		{
			if (Application.isPlaying)
				return true;
			
			if (ImportBusy())
				return false;

			foreach (var assetPath in _assetPaths)
			{
				if (ImporterPending(assetPath))
					return false;
			}
			
			return true;
		}

		public static void ThrowIfNotReady( string _assetPath, int _extraStackFrames = 0 )
		{
			if (!Ready(_assetPath))
				throw new NotInitializedException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} is not allowed during import/compile. " +
					$"Wrap with WhenReady(...). Asset: {_assetPath}");
		}

		public static void ThrowIfNotPlaying( string _name, int _extraStackFrames = 0 )
		{
			if (!Application.isPlaying)
				throw new InvalidOperationException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} " +
					 $"(with '{_name}') is not allowed in Editor while not playing. Please use WhenReady(...) or InstanceOrNull.");
		}

		public static T EditorLoad<T>( string _name, string _assetPath ) where T : ScriptableObject
		{
			return (T)EditorLoad(_name, _assetPath, typeof(T));
		}

		public static T EditorLoadOrCreate<T>( string _name, string _assetPath, out bool _wasCreated ) where T : ScriptableObject
		{
			return (T)EditorLoadOrCreate(_name, _assetPath, typeof(T), out _wasCreated);
		}

		public static T EditorLoadOrCreate<T>( string _name, string _assetPath ) where T : ScriptableObject => (T)EditorLoadOrCreate(_name, _assetPath, typeof(T), out _);

		public static ScriptableObject EditorLoad( string _name, string _assetPath, Type _type )
		{
			if (string.IsNullOrEmpty(_assetPath))
				throw new InvalidOperationException("AssetPath not set for " + _type.FullName);

			ThrowIfNotReady(_assetPath);
			return AssetDatabase.LoadAssetAtPath<ScriptableObject>(_assetPath);
		}

		public static ScriptableObject EditorLoadOrCreate( string _name, string _assetPath, Type _type, out bool _wasCreated )
		{
			_wasCreated = false;
			// We don't need a ThrowIfNotReady(), since EditorLoad() checks this.
			var asset = EditorLoad<ScriptableObject>(_name, _assetPath);
			if (asset)
				return asset;

			_wasCreated = true;
			EditorFileUtility.EnsureUnityFolderExists(System.IO.Path.GetDirectoryName(_assetPath).Replace('\\', '/'));
			var inst = ScriptableObject.CreateInstance(_type);
			inst.name = _name;
			Debug.Log($"Create scriptable object instance '{_name}' of type '{_type.Name}' at '{_assetPath}'");
			AssetDatabase.CreateAsset(inst, _assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(_assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			return AssetDatabase.LoadAssetAtPath<ScriptableObject>(_assetPath);
		}

		public static ScriptableObject EditorLoadOrCreate( string _name, string _assetPath, Type _type ) => EditorLoadOrCreate(_name, _assetPath, _type, out _);

		public static bool AssetExists( string _assetPath )
		{
			ThrowIfNotReady(_assetPath);
			return AssetDatabase.LoadAssetAtPath<Object>(_assetPath) != null;
		}

		// Gate for specific ScriptableObject assets (type+path), e.g. to ensure importer finished.


#else
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Clear() {}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady(Action _callback, Func<bool> _0, params string[] _1) => _callback.Invoke();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady( Action _callback, Func<bool> _0, (Type type, string assetPath)[] _1, int _2 = 0, int _3 = 0 ) => _callback.Invoke();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady( Action _callback, params string[] _1 ) => _callback?.Invoke();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Save<T>( T _0, string _1, string _2 ) where T : ScriptableObject {}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ImportBusy() => false;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ImporterPending( string _ ) => false;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Ready( string _ ) => true;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotReady( string _ ) { }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotPlaying( string _0, int _1 = 0 ) { }

#endif

		public static ScriptableObject LoadOrCreateScriptableObject( string _className, string _assetPath, Type _type, out bool _wasCreated )
		{
			_wasCreated = false;
			ScriptableObject result = null;

#if UNITY_EDITOR
			if (Application.isPlaying)
			{
#endif
				result = Resources.Load<ScriptableObject>(_className);
				if (result == null)
				{
					Debug.LogError($"Scriptable object could not be loaded from path '{_className}'");
					result = ScriptableObject.CreateInstance(_type);
				}
#if UNITY_EDITOR
			}
			else
			{
				EditorCallerGate.ThrowIfNotEditorAware(_className);
				ThrowIfNotReady(_assetPath);
				result = EditorLoadOrCreate(_className, _assetPath, _type, out _wasCreated);
			}
#endif

			if (result == null)
			{
				result = ScriptableObject.CreateInstance(_type);
				_wasCreated = true;
			}

			return result;
		}

		public static ScriptableObject LoadOrCreateScriptableObject( string _className, string _assetPath, Type _type )
		{
			return LoadOrCreateScriptableObject(_className, _assetPath, _type, out _);
		}

	}


#if UNITY_EDITOR
	[InitializeOnLoad]
	static class AssetReadyGateReset
	{
		static AssetReadyGateReset()
		{
			AssemblyReloadEvents.beforeAssemblyReload += Clear;
			EditorApplication.playModeStateChanged += _ => Clear();
		}

		private static void Clear()
		{
			AssetReadyGate.Clear();
		}
	}
#endif

}
