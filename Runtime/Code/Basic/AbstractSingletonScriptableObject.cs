using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class AbstractSingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
	{
		protected const string EditorDir = "Assets/Resources/";
		protected static string ClassName => typeof(T).Name;
		protected static string EditorPath => EditorDir + ClassName + ".asset";

		protected static T s_instance;

		protected virtual void OnEnable() { }

		public static T Instance
		{
			get
			{
				if (s_instance == null)
					SingletonScriptableObjectHelper.SetInstanceRuntime(ref s_instance, ClassName);
				
				return s_instance;
			}
		}

		// Editor-safe: return null while not ready
		public static T InstanceOrNull
		{
			get
			{
				if (s_instance == null)
					SingletonScriptableObjectHelper.TrySetInstance(ref s_instance, ClassName, EditorPath);
				
				return s_instance;
			}
		}

#if UNITY_EDITOR
		// Deferred, safe in Editor. Immediate call if already ready.
		public static void WhenReady( Action<T> _callback, int _extraFrameTries = 5 )
		{
			SingletonScriptableObjectHelper.WhenReady
			(
				() => s_instance,
				inst => s_instance = inst,
				_callback,
				ClassName,
				EditorPath, 
				_extraFrameTries
			);
		}
#endif


#if UNITY_EDITOR

		public virtual void OnEditorInitialize() { }

		private static void CreateAsset( Object _obj, string _path )
		{
			string directory = EditorFileUtility.GetDirectoryName(_path);
			EditorFileUtility.EnsureUnityFolderExists(directory);
			AssetDatabase.CreateAsset(_obj, _path);
		}

		public static void EditorSave( T _instance )
		{
			if (!AssetDatabase.Contains(_instance))
				CreateAsset(_instance, EditorPath);

			EditorGeneralUtility.SetDirty(_instance);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}

		public static void EditorSave() => EditorSave(Instance);

		public static bool Initialized => AssetDatabase.LoadAssetAtPath<T>(EditorPath) != null;

		public static void Initialize()
		{
			if (Initialized)
				return;

			T instance = CreateInstance<T>();
			s_instance = instance;
			var abstractSingletonScriptableObject = instance as AbstractSingletonScriptableObject<T>;
			abstractSingletonScriptableObject.OnEditorInitialize();

			EditorSave(instance);
		}
#endif
	}
}
