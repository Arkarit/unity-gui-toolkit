using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class AbstractSingletonScriptableObject<T> : ScriptableObject where T : AbstractSingletonScriptableObject<T>
	{
		public const string AssetDir = "Assets/Resources/";
		public static string ClassName => typeof(T).Name;
		public static string AssetPath => AssetDir + ClassName + ".asset";

		private static T s_instance;

		protected virtual void OnEnable() { }

		public static T Instance
		{
			get
			{
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				if (s_instance == null)
				{
					s_instance = (T)AssetReadyGate.LoadOrCreateScriptableObject(ClassName, AssetPath, typeof(T), out bool wasCreated);
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
