using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiOrientationDependentStyleConfig), menuName = StringConstants.CREATE_ORIENTATION_DEPENDENT_STYLE_CONFIG)]
	public class UiOrientationDependentStyleConfig : UiStyleConfig
	{
		public const string Landscape = "Landscape";
		public const string Portrait = "Portrait";

		protected static UiOrientationDependentStyleConfig s_instance;
		
		protected const string EditorDir = "Assets/Resources/";
		protected static string ClassName => typeof(UiOrientationDependentStyleConfig).Name;
		protected static string EditorPath => EditorDir + ClassName + ".asset";
		
		public static void ResetInstance() => s_instance = null;

		protected override void OnEnable()
		{
			base.OnEnable();
			
#if UNITY_EDITOR
			if (EditorAssetUtility.IsBeingImportedFirstTime(EditorPath))
				return;
#endif
			
			var instance = Instance;
			if (instance.NumSkins == 0)
			{
				var skins = s_instance.Skins;
				skins.Add(new UiSkin(s_instance, Landscape));
				skins.Add(new UiSkin(s_instance, Portrait));
				s_instance.Skins = skins;
#if UNITY_EDITOR
				EditorSave(s_instance);
#endif
			}

			UiEventDefinitions.EvScreenOrientationChange.AddListener(OnScreenOrientationChange);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UiEventDefinitions.EvScreenOrientationChange.RemoveListener(OnScreenOrientationChange);
		}

		private void OnScreenOrientationChange(EScreenOrientation _before, EScreenOrientation _after)
		{
			Instance.CurrentSkinName = _after == EScreenOrientation.Landscape ? Landscape : Portrait;
//			if (Application.isPlaying)
//				LayoutRebuilder.ForceRebuildLayoutImmediate(UiMain.Instance.transform as RectTransform);
		}

		public static UiOrientationDependentStyleConfig Instance
		{
			get
			{
				if (s_instance == null)
				{
					var styleConfig = UiToolkitConfiguration.Instance.UiOrientationDependentStyleConfig;
					if (styleConfig)
						s_instance = styleConfig;
					else
						s_instance = Resources.Load<UiOrientationDependentStyleConfig>(ClassName);
					
					if (s_instance == null)
					{
#if !UNITY_EDITOR
						Debug.LogError($"Scriptable object could not be loaded from path '{ClassName}'");
#endif
						s_instance = CreateInstance<UiOrientationDependentStyleConfig>();
					}
				}

				return s_instance;
			}
		}
		
#if UNITY_EDITOR

		public virtual void OnEditorInitialize() {}

		public static void EditorSave(UiOrientationDependentStyleConfig _instance)
		{
			if (!AssetDatabase.Contains(_instance))
			{
				EditorAssetUtility.CreateAsset(_instance, EditorPath);
			}

			EditorUtility.SetDirty(_instance);
			AssetDatabase.SaveAssetIfDirty(_instance);
		}

		public static bool Initialized => AssetDatabase.LoadAssetAtPath<UiOrientationDependentStyleConfig>(EditorPath) != null;

		public static void Initialize()
		{
			if (Initialized)
				return;

			UiOrientationDependentStyleConfig instance = CreateInstance<UiOrientationDependentStyleConfig>();
			s_instance = instance;
			instance.OnEditorInitialize();

			EditorSave(instance);
		}


#endif

	}
}
