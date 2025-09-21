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
	/// <summary>
	/// Gatekeeper for safe ScriptableObject access and creation.
	/// Prevents saving or creating assets while the Editor is importing/compiling
	/// or while a player build is in progress. Provides simple runtime loading
	/// via Resources in Play Mode.
	/// </summary>
	public static class AssetReadyGate
	{
		/// <summary>
		/// Loads a ScriptableObject from Resources at runtime. Throws in Editor if not playing.
		/// </summary>
		/// <typeparam name="T">ScriptableObject type to load.</typeparam>
		/// <param name="_name">Resources path (without extension).</param>
		/// <returns>Loaded instance or a transient ScriptableObject if not found.</returns>
		public static T RuntimeLoad<T>( string _name ) where T : ScriptableObject
		{
			ThrowIfNotPlaying(_name);
			var result = Resources.Load<T>(_name);
			if (!result)
			{
				UiLog.LogError($"Scriptable object could not be loaded from path '{_name}' ");
				return ScriptableObject.CreateInstance<T>();
			}

			return result;
		}

#if UNITY_EDITOR
		private const string AssetDir = "Assets/Resources/";

		/// <summary>
		/// True while the Editor is compiling scripts or updating assets.
		/// </summary>
		public static bool ImportBusy => EditorApplication.isCompiling || EditorApplication.isUpdating;

		/// <summary>
		/// Shorthand for AllScriptableObjectsReady.
		/// </summary>
		public static bool Ready => AllScriptableObjectsReady;

		private static string[] s_scriptableObjectGuids;

		/// <summary>
		/// Cached list of all ScriptableObject GUIDs in Assets and Packages.
		/// This is intentionally broad because the library may live in Packages
		/// and consumers may have ScriptableObjects in both trees.
		/// </summary>
		private static string[] ScriptableObjectGuids
		{
			get
			{
				if (s_scriptableObjectGuids == null)
					s_scriptableObjectGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets", "Packages" });
				return s_scriptableObjectGuids;
			}
		}

		/// <summary>
		/// Clears the cached GUID list. Called on relevant Editor events.
		/// </summary>
		public static void Clear() => s_scriptableObjectGuids = null;

		/// <summary>
		/// Returns true if Editor is idle (no compile/update) and all ScriptableObjects
		/// in Assets/Packages have a resolvable main type (i.e., importer finished).
		/// Also returns false if a player build is in progress.
		/// In Play Mode this always returns true.
		/// </summary>
		public static bool AllScriptableObjectsReady
		{
			get
			{
				if (Application.isPlaying)
					return true;

				if (BuildPipeline.isBuildingPlayer)
					return false;

				// Editor must be calm first.
				if (ImportBusy)
					return false;

				for (int i = 0; i < ScriptableObjectGuids.Length; i++)
				{
					string guid = ScriptableObjectGuids[i];
					string path = AssetDatabase.GUIDToAssetPath(guid);

					// If path cannot be resolved (and editor is calm), treat as missing, not pending.
					if (string.IsNullOrEmpty(path))
						continue;

					// Folders are not assets to wait for.
					if (AssetDatabase.IsValidFolder(path))
						continue;

					// If file is not on disk (and editor is calm), treat as missing, not pending.
					if (!File.Exists(path))
						continue;

					// Importer is done when the main type is known.
					Type mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
					if (mainType == null)
						return false; // Still pending.
				}

				// All checked paths are ready (or irrelevant).
				return true;
			}
		}

		/// <summary>
		/// Executes a callback after the Editor becomes quiet and all ScriptableObjects are ready.
		/// Uses a "quiet frame" countdown to avoid firing on the very first ready frame.
		/// Aborts if a build starts or a timeout is reached.
		/// </summary>
		/// <param name="_callback">Action to invoke when ready.</param>
		/// <param name="_quietFrames">Number of consecutive ready frames required before invoking.</param>
		/// <param name="_maxFrames">Hard timeout in frames (0 disables timeout).</param>
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

			UiLog.LogOnce("Begin waiting for Scriptable Objects to be available");
			EditorApplication.update += Tick;
			return;

			void Tick()
			{
				// Abort if a build begins after scheduling.
				if (BuildPipeline.isBuildingPlayer)
				{
					EditorApplication.update -= Tick;
					UiLog.LogWarning("WhenReady aborted: build started.");
					return;
				}

				// Timeout guard to avoid dangling subscriptions.
				if (_maxFrames > 0 && ++frames > _maxFrames)
				{
					EditorApplication.update -= Tick;
					UiLog.LogError($"WhenReady(assets) timeout after {_maxFrames} frames. Caller: {DebugUtility.GetCallingClassAndMethod()}");
					return;
				}

				// Not ready yet; reset quiet countdown.
				if (!AllScriptableObjectsReady)
				{
					countdown = _quietFrames;
					return;
				}

				// Wait for the required number of consecutive quiet frames.
				if (countdown-- > 0)
					return;

				EditorApplication.update -= Tick;
				if (UiLog.LogOnce($"All scriptable objects became available after {frames} frames plus {_quietFrames} extra frames."))
				{
					if (s_scriptableObjectGuids != null)
					{
						string s = "Scriptable Objects ready:";
						
						foreach (var guid in s_scriptableObjectGuids)
						{
							if (guid == null)
								continue;
							
							s += $"\n\t{AssetDatabase.GUIDToAssetPath(guid)}";
						}
						
						UiLog.LogVerbose(s);
					}
				}

				try
				{
					_callback();
				}
				catch (Exception ex)
				{
					UiLog.LogError($"Exception in Callback: {ex}");
				}
			}
		}

		/// <summary>
		/// Throws if the asset system is not ready (import/compile/build in progress).
		/// Use this to guard any Editor-time asset IO that must not run during import/build.
		/// </summary>
		/// <param name="_extraStackFrames">Extra frames to skip for caller extraction in logs.</param>
		/// <exception cref="NotInitializedException">Thrown if assets are not ready.</exception>
		public static void ThrowIfNotReady( int _extraStackFrames = 0 )
		{
			if (!Ready)
				throw new NotInitializedException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} is not allowed during import/compile. " +
					$"Wrap with WhenReady(...).");
		}

		/// <summary>
		/// Throws if called in the Editor while not in Play Mode.
		/// Use this to enforce that runtime-only accessors are not used in Edit Mode.
		/// </summary>
		/// <param name="_name">Logical object name for diagnostics.</param>
		/// <param name="_extraStackFrames">Extra frames to skip for caller extraction in logs.</param>
		/// <exception cref="InvalidOperationException">Thrown if not in Play Mode.</exception>
		public static void ThrowIfNotPlaying( string _name, int _extraStackFrames = 0 )
		{
			if (!Application.isPlaying)
				throw new InvalidOperationException(
					$"{DebugUtility.GetCallingClassAndMethod(false, true, 1 + _extraStackFrames)} " +
					 $"(with '{_name}') is not allowed in Editor while not playing. Please use WhenReady(...) or InstanceOrNull.");
		}

		/// <summary>
		/// Loads a ScriptableObject of type T from the project (Editor-only).
		/// Prefers assets under Assets/ if multiple matches exist.
		/// Returns null if none found.
		/// </summary>
		public static T EditorLoad<T>() where T : ScriptableObject => (T)EditorLoad(typeof(T));

		/// <summary>
		/// Loads or creates a ScriptableObject of type T (Editor-only).
		/// Outputs whether a new instance had to be created.
		/// </summary>
		public static T EditorLoadOrCreate<T>( out bool _wasCreated ) where T : ScriptableObject => (T)EditorLoadOrCreate(typeof(T), out _wasCreated);

		/// <summary>
		/// Loads or creates a ScriptableObject of type T (Editor-only).
		/// </summary>
		public static T EditorLoadOrCreate<T>() where T : ScriptableObject => (T)EditorLoadOrCreate(typeof(T), out _);

		/// <summary>
		/// Loads a ScriptableObject of the given type from the project (Editor-only).
		/// Uses type name filter (t:TypeName). Prefers Assets/ over Packages/ if multiple matches exist.
		/// Returns null if none found.
		/// </summary>
		/// <param name="_type">Concrete ScriptableObject type to load.</param>
		/// <returns>Loaded asset or null.</returns>
		public static ScriptableObject EditorLoad( Type _type )
		{
			ThrowIfNotReady();
			string path;
			var foundGuids = AssetDatabase.FindAssets($"t:{_type.Name}");
			if (foundGuids == null || foundGuids.Length == 0)
				return null;

			// Assets in user dirs are preferred
			foreach (var guid in foundGuids)
			{
				path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.StartsWith("Assets/", StringComparison.Ordinal))
					return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
			}

			path = AssetDatabase.GUIDToAssetPath(foundGuids[0]);
			return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
		}

		/// <summary>
		/// Loads a ScriptableObject of the given type or creates it if missing (Editor-only).
		/// Creation is blocked during player build to avoid writing while building.
		/// New assets are created at Assets/Resources/{TypeName}.asset.
		/// </summary>
		/// <param name="_type">Concrete ScriptableObject type.</param>
		/// <param name="_wasCreated">True if a new asset was created.</param>
		/// <returns>Existing or newly created asset, or null if creation was blocked.</returns>
		public static ScriptableObject EditorLoadOrCreate( Type _type, out bool _wasCreated )
		{
			_wasCreated = false;
			// We do not need a ThrowIfNotReady() here; EditorLoad() already enforces it.
			var asset = EditorLoad(_type);
			if (asset)
				return asset;

			// Hard guard: never create assets while building the player.
			if (BuildPipeline.isBuildingPlayer)
			{
				UiLog.LogError($"Attempt to create scriptable object '{_type.Name}' during build process");
				return null;
			}

			_wasCreated = true;
			var assetPath = $"{AssetDir}{_type.Name}.asset";
			EditorFileUtility.EnsureUnityFolderExists(System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/'));
			var inst = ScriptableObject.CreateInstance(_type);
			inst.name = _type.Name;
			UiLog.Log($"Create scriptable object instance '{inst.name}' of type '{_type.Name}' at '{assetPath}'");
			AssetDatabase.CreateAsset(inst, assetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			return AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
		}

		/// <summary>
		/// Shorthand for EditorLoadOrCreate(Type, out _).
		/// </summary>
		public static ScriptableObject EditorLoadOrCreate( Type _type ) => EditorLoadOrCreate(_type, out _);

		/// <summary>
		/// Returns true if there is at least one ScriptableObject of type T in the project (Editor-only).
		/// </summary>
		public static bool ScriptableObjectExists<T>() where T : ScriptableObject
		{
			var found = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
			return found != null && found.Length > 0;
		}

#else

		/// <summary>
		/// Always false in player builds.
		/// </summary>
		public static bool ImportBusy => false;

		/// <summary>
		/// Always true in player builds.
		/// </summary>
		public static bool Ready => true;

		/// <summary>
		/// Always true in player builds.
		/// </summary>
		public static bool AllScriptableObjectsReady => true;

		/// <summary>
		/// In player builds, invoke immediately.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void WhenReady(Action _callback, Func<bool> _0 = null, int _1 = 0, int _2 = 0) => _callback?.Invoke();

		/// <summary>
		/// No-op in player builds.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotReady( int _ = 0 ) {}

		/// <summary>
		/// No-op in player builds.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ThrowIfNotPlaying( string _0, int __1 = 0 ) {}
#endif

		/// <summary>
		/// Loads or creates a ScriptableObject regardless of context.
		/// In Play Mode, loads from Resources and falls back to transient instance.
		/// In Editor (not playing), uses EditorLoadOrCreate with readiness checks.
		/// </summary>
		/// <param name="_type">Concrete ScriptableObject type.</param>
		/// <param name="_wasCreated">True if a new instance was created (asset or transient).</param>
		/// <returns>ScriptableObject instance (asset or transient).</returns>
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
					UiLog.LogError($"Scriptable object could not be loaded from path '{className}'");
					result = ScriptableObject.CreateInstance(_type);
				}
#if UNITY_EDITOR
			}
			else
			{
				// Enforce that the caller is Editor-aware, then ensure asset readiness.
				EditorCallerGate.ThrowIfNotEditorAware(className);
				ThrowIfNotReady();
				result = EditorLoadOrCreate(_type, out _wasCreated);
			}
#endif

			// Fallback: transient instance to avoid returning null to callers.
			if (result == null)
			{
				result = ScriptableObject.CreateInstance(_type);
				_wasCreated = true;
			}

			return result;
		}

		/// <summary>
		/// Shorthand for LoadOrCreateScriptableObject(Type, out _).
		/// </summary>
		public static ScriptableObject LoadOrCreateScriptableObject( Type _type )
		{
		 return LoadOrCreateScriptableObject(_type, out _);
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// Resets the cached GUID list on significant Editor lifecycle events
	/// to keep the readiness evaluation correct as the project changes.
	/// </summary>
	[InitializeOnLoad]
	static class AssetReadyGateReset
	{
		static AssetReadyGateReset()
		{
			AssemblyReloadEvents.beforeAssemblyReload += Clear;
			EditorApplication.playModeStateChanged += _ => Clear();
			EditorApplication.projectChanged += Clear;
		}

		private static void Clear()
		{
			AssetReadyGate.Clear();
		}
	}
#endif
}
