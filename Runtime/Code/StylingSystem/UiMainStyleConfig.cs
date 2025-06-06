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
		
		protected const string EditorDir = "Assets/Resources/";
		protected static string ClassName => typeof(UiMainStyleConfig).Name;
		protected static string EditorPath => EditorDir + ClassName + ".asset";
		
		public static void ResetInstance() => s_instance = null;

		public static UiMainStyleConfig Instance
		{
			get
			{
				if (s_instance == null)
				{
					var styleConfig = UiToolkitConfiguration.Instance.UiMainStyleConfig;
					if (styleConfig)
						s_instance = styleConfig;
					else
						s_instance = Resources.Load<UiMainStyleConfig>(ClassName);
					
					if (s_instance == null)
					{
#if !UNITY_EDITOR
						Debug.LogError($"Scriptable object could not be loaded from path '{ClassName}'");
#endif
						s_instance = CreateInstance<UiMainStyleConfig>();
#if UNITY_EDITOR
						EditorSave(s_instance);
#endif
					}
				}

				return s_instance;
			}
		}
		
#if UNITY_EDITOR

		public virtual void OnEditorInitialize() {}

		public static void EditorSave(UiMainStyleConfig _instance)
		{
			if (!AssetDatabase.Contains(_instance))
			{
				EditorGeneralUtility.CreateAsset(_instance, EditorPath);
			}

			EditorGeneralUtility.SetDirty(_instance);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}

		public static bool Initialized => AssetDatabase.LoadAssetAtPath<UiMainStyleConfig>(EditorPath) != null;

		public static void Initialize()
		{
			if (Initialized)
				return;

			UiMainStyleConfig instance = CreateInstance<UiMainStyleConfig>();
			s_instance = instance;
			instance.OnEditorInitialize();

			EditorSave(instance);
		}


#endif

	}
}
