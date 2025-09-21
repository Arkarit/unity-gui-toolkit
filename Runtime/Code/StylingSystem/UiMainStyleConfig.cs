using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiMainStyleConfig), menuName = StringConstants.CREATE_MAIN_STYLE_CONFIG)]
	public class UiMainStyleConfig : UiStyleConfig
	{
		protected static UiMainStyleConfig s_instance;
		public static string ClassName => typeof(UiMainStyleConfig).Name;

		public static void ResetInstance() => s_instance = null;

		public static UiMainStyleConfig Instance
		{
			get
			{
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				if (s_instance == null)
				{
					s_instance = (UiMainStyleConfig)AssetReadyGate.LoadOrCreateScriptableObject(typeof(UiMainStyleConfig), out bool wasCreated);
					string s = $"Loaded {ClassName}";
#if UNITY_EDITOR
					s += $", asset path:{AssetDatabase.GetAssetPath(s_instance)}, wasCreated:{wasCreated}";
#endif
					UiLog.Log(s);
#if UNITY_EDITOR
					if (wasCreated)
						s_instance.OnEditorCreatedAsset();
#endif
				}
				
				return s_instance;
			}
			
			internal set
			{
#if UNITY_EDITOR
				if (s_instance != null)
					UiLog.Log($"Replacing {ClassName} instance '{AssetDatabase.GetAssetPath(s_instance)}' with '{AssetDatabase.GetAssetPath(value)}'");
				else
					UiLog.Log($"Setting {ClassName} instance to '{AssetDatabase.GetAssetPath(value)}'");
#endif
				s_instance = value;
			}
		}

#if UNITY_EDITOR

		public virtual void OnEditorCreatedAsset() { }

#endif

	}
}
