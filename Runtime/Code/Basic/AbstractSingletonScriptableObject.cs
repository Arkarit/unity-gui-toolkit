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
		protected static string AssetPath => EditorDir + ClassName + ".asset";

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
					SingletonScriptableObjectHelper.TrySetInstance(ref s_instance, ClassName, AssetPath);
				
				return s_instance;
			}
		}
		// Deferred, safe in Editor. Immediate call if already ready.
		public static void WhenReady( Action<T> _callback, int _extraFrameTries = 5 )
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
			{
				_callback.Invoke(Instance);
				return;
			}
			
			SingletonScriptableObjectHelper.WhenReady
			(
				() => s_instance,
				inst => s_instance = inst,
				_callback,
				ClassName,
				AssetPath, 
				_extraFrameTries
			);
#else
			_callback.Invoke(Instance);
#endif
		}

#if UNITY_EDITOR
		public static T EditorLoadOrCreate<T>() where T : ScriptableObject => SingletonScriptableObjectHelper.EditorLoadOrCreate<T>(ClassName, AssetPath);

		public virtual void OnEditorInitialize() { }

		public static void EditorSave() => SingletonScriptableObjectHelper.EditorSave(s_instance, ClassName, AssetPath);
#endif
	}
}
