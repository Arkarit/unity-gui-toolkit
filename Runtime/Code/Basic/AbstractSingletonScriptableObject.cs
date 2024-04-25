using UnityEngine;

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

		public static T Instance
		{
			get
			{
				if (s_instance == null)
				{
#if UNITY_EDITOR
					if (Application.isPlaying)
					{
#endif
						s_instance = Resources.Load<T>(ClassName);
						if (s_instance == null)
						{
							Debug.LogError($"UiToolkitMainSettings could not be loaded from path '{ClassName}'");
							s_instance = CreateInstance<T>();
						}
#if UNITY_EDITOR
					}
					else
					{
						s_instance = EditorLoad();
					}
#endif
				}

				return s_instance;
			}
		}

#if UNITY_EDITOR

		public virtual void OnInitialize() {}

		protected static T EditorLoad()
		{
			T result = AssetDatabase.LoadAssetAtPath<T>(EditorPath);
			if (result == null)
			{
				result = CreateInstance<T>();
				EditorSave(result);
			}

			return result;
		}

		public static void EditorSave(T instance)
		{
			if (!AssetDatabase.Contains(instance))
			{
				EditorFileUtility.CreateAsset(instance, EditorPath);
			}

			AssetDatabase.SaveAssets();
		}

		public static bool Initialized => AssetDatabase.LoadAssetAtPath<T>(EditorPath) != null;

		public static void Initialize()
		{
			if (Initialized)
				return;

			T instance = CreateInstance<T>();
			s_instance = instance;
			var abstractSingletonScriptableObject = instance as AbstractSingletonScriptableObject<T>;
			abstractSingletonScriptableObject.OnInitialize();

			EditorSave(instance);
		}


#endif
	}
}
