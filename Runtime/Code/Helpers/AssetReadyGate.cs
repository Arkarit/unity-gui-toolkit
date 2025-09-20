using GuiToolkit.Debugging;
using System;
using UnityEngine;

// Do not remove
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.IO;
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
		public static bool ImportBusy => EditorApplication.isCompiling || EditorApplication.isUpdating;

		public static bool Ready => AllScriptableObjectsReady;

		/// <summary>
		/// Returns true if the editor is calm (no compile/update) and
		/// all ScriptableObjects under the given folders are fully imported.
		/// Defaults to "Assets" to avoid scanning Packages.
		/// </summary>
		public static bool AllScriptableObjectsReady
		{
			get
			{
				if (Application.isPlaying)
					return true;

				// Editor must be calm first.
				if (ImportBusy)
					return false;

				string[] folders = new[] { "Assets", "Packages" };

				// GUID-level search is safe even while importer churns.
				string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", folders);

				for (int i = 0; i < guids.Length; i++)
				{
					string guid = guids[i];
					string path = AssetDatabase.GUIDToAssetPath(guid);

					// If path cannot be resolved (and editor is calm), treat as missing, not pending.
					if (string.IsNullOrEmpty(path))
					{
						continue;
					}

					// Folders are not assets to wait for.
					if (AssetDatabase.IsValidFolder(path))
					{
						continue;
					}

					// If file is not on disk (and editor is calm), treat as missing, not pending.
					if (!File.Exists(path))
					{
						continue;
					}

					// Importer is done when the main type is known.
					Type mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
					if (mainType == null)
					{
						// Still pending.
						return false;
					}
				}

				// All checked paths are ready (or irrelevant).
				return true;
			}
		}


		public static void WhenReady(
			Action _callback,
			int _quietFrames = 2,
			int _maxFrames = 60 // 0 = no timeout
		)
		{
			if (_callback == null)
				return;

			if (Application.isPlaying)
			{
				_callback();
				return;
			}

			// Immediate fast path
			if (AllScriptableObjectsReady)
			{
				_callback();
				return;
			}

			int countdown = _quietFrames;
			int frames = 0;

			EditorApplication.update += Tick;
			return;

			void Tick()
			{
				if (_maxFrames > 0 && ++frames > _maxFrames)
				{
					EditorApplication.update -= Tick;
					Debug.LogError($"WhenReady(assets) timeout after {_maxFrames} frames. Caller: {DebugUtility.GetCallingClassAndMethod()}");
					return;
				}

				if (!AllScriptableObjectsReady)
				{
					countdown = _quietFrames;
					return;
				}

				if (countdown-- > 0)
					return;

				EditorApplication.update -= Tick;

				try
				{
					_callback();
				}
				catch (Exception ex)
				{
					Debug.LogError($"Exception in Callback: {ex}");
				}
			}
		}

		public static void ThrowIfNotReady( int _extraStackFrames = 0 )
		{
			if (!Ready)
				throw new NotInitializedException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} is not allowed during import/compile. " +
					$"Wrap with WhenReady(...).");
		}

		public static void ThrowIfNotPlaying( string _name, int _extraStackFrames = 0 )
		{
			if (!Application.isPlaying)
				throw new InvalidOperationException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} " +
					 $"(with '{_name}') is not allowed in Editor while not playing. Please use WhenReady(...) or InstanceOrNull.");
		}

		public static T EditorLoad<T>() where T : ScriptableObject => (T)EditorLoad(typeof(T));

		public static T EditorLoadOrCreate<T>( out bool _wasCreated ) where T : ScriptableObject => (T)EditorLoadOrCreate(typeof(T), out _wasCreated);

		public static T EditorLoadOrCreate<T>() where T : ScriptableObject => (T)EditorLoadOrCreate(typeof(T), out _);

		public static ScriptableObject EditorLoad( Type _type )
		{
			ThrowIfNotReady();
			string path;
			var foundGuids = AssetDatabase.FindAssets($"t:{_type}");
			if (foundGuids == null || foundGuids.Length == 0)
				return null;

			// Assets in user dirs are preferred
			foreach (var guid in foundGuids)
			{
				path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.StartsWith("Assets", StringComparison.Ordinal))
					return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
			}

			path = AssetDatabase.GUIDToAssetPath(foundGuids[0]);
			return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
		}

		public static ScriptableObject EditorLoadOrCreate( Type _type, out bool _wasCreated )
		{
			_wasCreated = false;
			// We don't need a ThrowIfNotReady(), since EditorLoad() checks this.
			var asset = EditorLoad(_type);
			if (asset)
				return asset;

			_wasCreated = true;
			var assetPath = $"Assets/Resources/{_type.Name}.asset";
			EditorFileUtility.EnsureUnityFolderExists(System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/'));
			var inst = ScriptableObject.CreateInstance(_type);
			inst.name = _type.Name;
			Debug.Log($"Create scriptable object instance '{inst.name}' of type '{_type.Name}' at '{assetPath}'");
			AssetDatabase.CreateAsset(inst, assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			return AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
		}

		public static ScriptableObject EditorLoadOrCreate( Type _type ) => EditorLoadOrCreate(_type, out _);

		public static bool ScriptableObjectExists<T>() where T : ScriptableObject
		{
			var found = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
			return found != null && found.Length > 0;
		}

#else

		public static bool ImportBusy => false;
		public static bool Ready => true;
		public static bool AllScriptableObjectsReady => true;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady(Action _callback, Func<bool> _0 = null, int _1 = 0, int _2 = 0) => _callback?.Invoke();
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotReady( int _ = 0 ) {}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotPlaying( string _0, int __1 = 0 ) {}
#endif

		public static ScriptableObject LoadOrCreateScriptableObject( Type _type, out bool _wasCreated )
		{
			_wasCreated = false;
			ScriptableObject result = null;
			string className = _type.Name;

#if UNITY_EDITOR
			if (Application.isPlaying)
			{
#endif
				result = Resources.Load<ScriptableObject>(className);
				if (result == null)
				{
					Debug.LogError($"Scriptable object could not be loaded from path '{className}'");
					result = ScriptableObject.CreateInstance(_type);
				}
#if UNITY_EDITOR
			}
			else
			{
				EditorCallerGate.ThrowIfNotEditorAware(className);
				ThrowIfNotReady();
				result = EditorLoadOrCreate(_type, out _wasCreated);
			}
#endif

			if (result == null)
			{
				result = ScriptableObject.CreateInstance(_type);
				_wasCreated = true;
			}

			return result;
		}

		public static ScriptableObject LoadOrCreateScriptableObject( Type _type )
		{
			return LoadOrCreateScriptableObject(_type, out _);
		}

	}
}
