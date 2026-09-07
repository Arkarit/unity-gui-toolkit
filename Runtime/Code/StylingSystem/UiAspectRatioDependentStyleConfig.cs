using UnityEngine;

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiAspectRatioDependentStyleConfig), menuName = StringConstants.CREATE_ASPECT_RATIO_DEPENDENT_STYLE_CONFIG)]
	[EditorAware]
	public class UiAspectRatioDependentStyleConfig : UiStyleConfig
	{
		public const string Landscape = "Landscape";
		public const string Portrait = "Portrait";

		protected static UiAspectRatioDependentStyleConfig s_instance;
		public static string ClassName => nameof(UiAspectRatioDependentStyleConfig);

		protected override void OnEnable()
		{
			AssetReadyGate.WhenReady(() =>
			{
				// THIS config, not s_instance: that field is declared and never assigned, so the branch
				// meant to give a new config its two skins threw a NullReferenceException instead - out of
				// OnEnable, where Unity logs it and carries on, which is why it went unnoticed. The config
				// then stayed empty, and an empty aspect-ratio config resolves nothing at all.
				if (NumSkins == 0)
				{
					var skins = Skins;
					skins.Add(new UiSkin(this, Landscape, 1));
					skins.Add(new UiSkin(this, Portrait, 0));
					Skins = skins;
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
	
			if (bestMatchingSkin != CurrentSkin )
				CurrentSkinName = bestMatchingSkin.Name;
		}

		private UiSkin FindBestMatchingSkin(ScreenResolution _screenResolution)
		{
			if (_screenResolution.AspectRatio == 0 || Skins == null || Skins.Count == 0)
				return null;

			foreach (var uiSkin in Skins)
			{
				if (uiSkin.AspectRatioGreaterEqual < _screenResolution.AspectRatio)
					return uiSkin;
			}

			return Skins[0];
		}

		public new static UiAspectRatioDependentStyleConfig Instance
		{
			get
			{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				EditorCallerGate.ThrowIfNotEditorAware(ClassName);
				Bootstrap.ThrowIfNotInitialized();
#endif				
				return UiToolkitConfiguration.Instance.UiAspectRatioDependentStyleConfig;
			}
		}
	}
}
