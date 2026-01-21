using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class AbstractSingletonScriptableObject<T> : ScriptableObject where T : AbstractSingletonScriptableObject<T>
	{
		public static string ClassName => typeof(T).Name;

		private static T s_instance;

		protected virtual void OnEnable() { }
		protected virtual void OnInitialized() { }
		public static bool IsInitialized => s_instance != null;

		public static void Initialize()
		{
			if (IsInitialized)
				return;
			
			s_instance = (T)AssetReadyGate.LoadOrCreateScriptableObject(typeof(T), out bool wasCreated);
			string s = $"Loaded Singleton Scriptable Object {ClassName}";
			
#if UNITY_EDITOR
			s += $" asset path:{AssetDatabase.GetAssetPath(s_instance)}, wasCreated:{wasCreated}";
#endif
			UiLog.Log(s);
			
#if UNITY_EDITOR
			if (wasCreated)
				s_instance.OnEditorCreatedAsset();
			
			s_instance.OnInitialized();
#endif
		}

		public static T Instance
		{
			get
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				Bootstrap.ThrowIfNotInitialized();
				if (s_instance == null)
					throw new InvalidOperationException($"Class {typeof(T).Name} should be initialized by Bootstrap.Initialize() or another place, but isn't.");
#endif				
				return s_instance;
			}
		}

#if UNITY_EDITOR

		public virtual void OnEditorCreatedAsset() { }

#endif
	}
}
