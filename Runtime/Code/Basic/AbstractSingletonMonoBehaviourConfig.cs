using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace GuiToolkit
{
	/// <summary>
	/// A base class to store configs on game object prefabs.
	/// Doing so has one huge advantage compared to common Scriptable objects:
	/// You can very simply override default configs by creating a prefab variant.
	/// That's why it is used for the style system.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[ExecuteAlways]
	public abstract class AbstractSingletonMonoBehaviourConfig<T> : MonoBehaviour where T : MonoBehaviour
	{
		protected const string EditorDir = "Assets/Resources/";
		protected static string ClassName => typeof(T).Name;
		protected static string EditorPath => EditorDir + ClassName + ".prefab";

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
						GameObject go = Resources.Load<GameObject>(ClassName);
						if (go == null)
						{
							Debug.LogError($"Config could not be loaded from path '{ClassName}'");
							go = new GameObject($"ErrorCouldNotLoad_{ClassName}");
							s_instance = go.AddComponent<T>();
							return s_instance;
						}

						s_instance = go.GetComponent<T>();
						if (s_instance == null)
						{
							Debug.LogError($"Config was found at '{ClassName}', but it doesn't contain a matching component");
							s_instance = go.AddComponent<T>();
							return s_instance;
						}

						return s_instance;
#if UNITY_EDITOR
					}


#endif
				}

				return s_instance;
			}
		}

	}
}
