using GuiToolkit.Style;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	// General Ui event definitions.
	public static class UiEventDefinitions
	{
		#region Loca
			/// \brief Event invoked on language change.
			/// <param name="string">Language token, e.g. "de"</param>
			/// Note that this event is also spawned once on startup.
			public static CEvent<string>										EvLanguageChanged = new(true);
		#endregion

		#region Screen
			/// \brief Invoked if the screen resolution has changed
			/// <param name="ScreenResolution 0">Screen resolution before change</param>
			/// <param name="ScreenResolution 1">Screen resolution after change</param>
			public static CEvent<ScreenResolution,ScreenResolution>				EvScreenResolutionChange = new(true, new ScreenResolution(), new ScreenResolution(Screen.width, Screen.height));
		#endregion

		#region Player Settings
			/// \brief Invoked if a player setting has changed.
			/// <param name="PlayerSetting">Changed player setting class instance</param>
			public static CEvent<PlayerSetting>									EvPlayerSettingChanged = new();
		#endregion

		#region Views and Panels
			/// \brief Invoked before a full screen view is opened or closed.
			/// UiView: The UiView to open/closed
			/// bool: opened
			public static CEvent<UiView, bool>									EvFullScreenView = new();
			
			/// <summary>Raised right before the show transition starts.</summary>
			public static CEvent<UiPanel>										EvOnPanelBeginShow = new();
	
			/// <summary>Raised after the show transition completes (or instant show is done).</summary>
			public static CEvent<UiPanel>										EvOnPanelEndShow = new();
	
			/// <summary>Raised right before the hide transition starts.</summary>
			public static CEvent<UiPanel>										EvOnPanelBeginHide = new();
	
			/// <summary>Raised after the hide transition completes (or instant hide is done).</summary>
			public static CEvent<UiPanel>										EvOnPanelEndHide = new();
	
			/// <summary>Raised when the panel is about to be destroyed (or returned to pool).</summary>
			public static CEvent<UiPanel>										EvOnPanelDestroyed = new();

		#endregion

		#region Style System
			/// \brief Invoked if skin changes
			/// float: duration
			public static CEvent<float>											EvSkinChanged = new();
			/// \brief Invoked if skin values have changed - the skin itself stays the same
			/// float: normalized amount of change
			public static CEvent<float>											EvSkinValuesChanged = new();
	
			// Style system
			/// \brief Invoked if applicableness of style has changed.
			/// Used to synchronize styles of same name but members of different skins
			public static CEvent<UiStyleConfig, UiAbstractStyleBase> 			EvStyleApplicableChanged = new();
			public static CEvent<UiStyleConfig, UiAbstractStyleBase>			EvDeleteStyle = new();
			public static CEvent<UiStyleConfig, UiSkin>							EvAddSkin = new();
			public static CEvent<UiStyleConfig, string>							EvDeleteSkin = new();
			public static CEvent<UiStyleConfig, UiSkin, string>					EvSetSkinAlias = new();
			public static CEvent<UiStyleConfig, UiAbstractStyleBase, string>	EvSetStyleAlias = new();
			public static CEvent<UiAbstractApplyStyleBase>						EvStyleApplierCreated = new();	
			public static CEvent<UiAbstractApplyStyleBase>						EvStyleApplierChangedParent = new();	
			public static CEvent<UiAbstractApplyStyleBase>						EvStyleApplierDestroyed = new();
		#endregion


		#region Camera
			public static CEvent<Camera, Camera>								EvMainCameraChanged = new(true);
			public static CEvent<float, float>									EvMainCameraFovChanged = new(true);
		#endregion

		#region Time
			// Not yet implemented: unscaled time. Implement/test on demand please.
	
			// This is called every frame. Use this if you need an update for standalone classes or disabled game objects.
			// For regular and active game objects consider using Update() instead (less overhead)
			public static readonly CEvent<int> OnTickPerFrame = new(true);
			public static readonly CEvent<int> OnTickPerSecond = new(true);
			public static readonly CEvent<int> OnTickPerMinute = new(true);
		#endregion
		
#if UNITY_EDITOR
		// Fix redraw issues when changing style in Unity >= 6
		// Scene view didn't update when skin was changed
		[InitializeOnLoadMethod]
		private static void Init()
		{
			EvSkinChanged.RemoveListener(OnSkinChanged);
			EvSkinChanged.AddListener(OnSkinChanged);
		}

		private static void OnSkinChanged(float _)
		{
			if (!Application.isPlaying)
			{
				EditorApplication.delayCall += () =>
				{
					if (Selection.activeGameObject)
						EditorGeneralUtility.SetDirty(Selection.activeGameObject);
					
					SceneView.RepaintAll();
				};
			}
		}
#endif
	}
}
