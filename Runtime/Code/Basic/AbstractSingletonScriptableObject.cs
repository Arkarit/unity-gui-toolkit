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

		public static T Instance
		{
			get
			{
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				if (s_instance == null)
				{
					s_instance = (T)AssetReadyGate.LoadOrCreateScriptableObject(typeof(T), out bool wasCreated);
					string s = $"Loaded Singleton Scriptable Object {ClassName}";
#if UNITY_EDITOR
					s += $" asset path:{AssetDatabase.GetAssetPath(s_instance)}, wasCreated:{wasCreated}";
#endif
					UiLog.Log(s);
#if UNITY_EDITOR
					if (wasCreated)
						s_instance.OnEditorCreatedAsset();
#endif
				}
				
				return s_instance;
			}
		}

#if UNITY_EDITOR

		public virtual void OnEditorCreatedAsset() { }

#endif
	}
}
