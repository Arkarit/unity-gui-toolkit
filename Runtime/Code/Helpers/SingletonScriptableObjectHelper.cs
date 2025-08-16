using Microsoft.CodeAnalysis.CSharp.Syntax;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public static class SingletonScriptableObjectHelper
	{
		public static void SetInstanceRuntime<T>( ref T _instance, string _name ) where T : ScriptableObject
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				throw new System.InvalidOperationException(
					typeof(T).Name + ".Instance is not allowed in Editor while not playing. Please use WhenReady(...) or InstanceOrNull.");
#endif
			if (!_instance)
				_instance = RuntimeLoad<T>(_name);

			//TODO? Instance is assumed to be loaded? Should we warn, error or throw?
		}

		public static bool TrySetInstance<T>( ref T _instance, string _name, string _assetPath ) where T : ScriptableObject
		{
#if UNITY_EDITOR
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
#else
			return GetInstanceRuntime<T>(ref _instance, _name);
#endif
		}


#if UNITY_EDITOR
		// Deferred, safe in Editor. Immediate call if already ready.
		public static void WhenReady<T>
		(
			System.Func<T> get,
			System.Action<T> set,
			System.Action<T> callback,
			string name,
			string editorPath,
			int extraFrameTries = 5
		)
			where T : ScriptableObject
		{
			if (callback == null) return;

			var current = get();
			if (current != null) { callback(current); return; }

			int countdown = extraFrameTries;

			void Tick()
			{
				if (ImportBusy() || ImporterPending(editorPath))
					return;

				if (countdown-- < 0)
				{
					EditorApplication.update -= Tick;
					Debug.LogError($"Could not load '{editorPath}' after {extraFrameTries + 1} frames delay. Consider increasing the delay.");
					return;
				}

				var inst = EditorLoadOrCreate<T>(name, editorPath);
				if (inst != null)
				{
					set(inst);
					EditorApplication.update -= Tick;
					callback(inst);
				}
			}

			EditorApplication.update += Tick;
		}

		public static T EditorLoadOrCreate<T>( string _name, string _assetPath ) where T : ScriptableObject
		{
			if (string.IsNullOrEmpty(_assetPath))
				throw new System.InvalidOperationException("AssetPath not set for " + typeof(T).FullName);

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

		private static void CreateAsset( Object _obj, string _path )
		{
			string directory = EditorFileUtility.GetDirectoryName(_path);
			EditorFileUtility.EnsureUnityFolderExists(directory);
			AssetDatabase.CreateAsset(_obj, _path);
		}

		public static void EditorSave<T>( T _instance, string _name, string _assetPath, int _extraFrameTries ) where T : ScriptableObject
		{
			WhenReady
			(
				() => _instance,
				inst => _instance = inst,
				inst =>
				{
					EditorGeneralUtility.SetDirty(inst);
					AssetDatabase.SaveAssetIfDirty(inst);
				},
				_name,
				_assetPath,
				_extraFrameTries
			);
		}
		
		public static bool ImportBusy() => EditorApplication.isCompiling || EditorApplication.isUpdating;

		public static bool ImporterPending( string path ) =>
			!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path))
			&& AssetDatabase.GetMainAssetTypeAtPath(path) == null;
#endif

		private static T RuntimeLoad<T>( string _name ) where T : ScriptableObject
		{
			var result = Resources.Load<T>(_name);
			if (!result)
			{
				Debug.LogError($"Scriptable object could not be loaded from path '{_name}'");
				return ScriptableObject.CreateInstance<T>();
			}

			return result;
		}

	}
}