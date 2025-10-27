using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiAspectRatioDependentStyleConfig), menuName = StringConstants.CREATE_ASPECT_RATIO_DEPENDENT_STYLE_CONFIG)]
	[EditorAware]
	public class UiAspectRatioDependentStyleConfig : UiStyleConfig
	{
		public const string Landscape = "Landscape";
		public const string Portrait = "Portrait";

		protected static UiAspectRatioDependentStyleConfig s_instance;
		public static string ClassName => typeof(UiAspectRatioDependentStyleConfig).Name;
		public static void ResetInstance() => s_instance = null;

		protected override void OnEnable()
		{
			AssetReadyGate.WhenReady(() =>
			{
				var instance = Instance;
				if (instance.NumSkins == 0)
				{
					var skins = s_instance.Skins;
					skins.Add(new UiSkin(s_instance, Landscape, 1));
					skins.Add(new UiSkin(s_instance, Portrait, 0));
					s_instance.Skins = skins;
				}

				Skins.Sort((a, b) =>
				{
					if (a.AspectRatioGreaterEqual > b.AspectRatioGreaterEqual)
						return -1;
					if (b.AspectRatioGreaterEqual > a.AspectRatioGreaterEqual) 
						return 1;
					return 0;
				});

				base.OnEnable();
				UiEventDefinitions.EvScreenResolutionChange.AddListener(OnScreenResolutionChange, true);
			});
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UiEventDefinitions.EvScreenResolutionChange.RemoveListener(OnScreenResolutionChange);
		}

		private void OnScreenResolutionChange( ScreenResolution _before, ScreenResolution _after )
		{
			var bestMatchingSkin = FindBestMatchingSkin(_after);
			if ( bestMatchingSkin == null )
				return;

			if (bestMatchingSkin != Instance.CurrentSkin )
				CurrentSkinName = bestMatchingSkin.Name;
		}

		private UiSkin FindBestMatchingSkin(ScreenResolution _screenResolution)
		{
			if (_screenResolution.AspectRatio == 0)
				return null;

			foreach (var uiSkin in Skins)
			{
				if (uiSkin.AspectRatioGreaterEqual < _screenResolution.AspectRatio)
					return uiSkin;
			}

			return Skins[0];
		}

		public static UiAspectRatioDependentStyleConfig Instance
		{
			get
			{
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				if (s_instance == null)
				{
					s_instance = (UiAspectRatioDependentStyleConfig)AssetReadyGate.LoadOrCreateScriptableObject
					(
						typeof(UiAspectRatioDependentStyleConfig),
						out bool wasCreated
					);
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
