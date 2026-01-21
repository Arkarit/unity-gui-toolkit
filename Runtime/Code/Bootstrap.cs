using GuiToolkit.Exceptions;
using GuiToolkit.Settings;
using GuiToolkit.Storage;
using System;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[EditorAware]
	public static class Bootstrap
	{
		private static bool s_isInitialized;
		private static bool s_isInitializing;
		public static bool IsInitialized => s_isInitialized || s_isInitializing;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void RuntimeInitialize()
		{
			Initialize();
		}


#if UNITY_EDITOR
		static Bootstrap()
		{
			UiLog.LogInternal("Static Ctor");
			EditorApplication.playModeStateChanged -= HandlePlayMode;
			EditorApplication.playModeStateChanged += HandlePlayMode;
			EditorApplication.delayCall += Delayed;
		}

		private static void Delayed()
		{
			if (!Application.isPlaying)
				HandlePlayMode(PlayModeStateChange.EnteredEditMode);
		}

		private static void HandlePlayMode(PlayModeStateChange _playMode)
		{
			if (_playMode == PlayModeStateChange.EnteredEditMode)
			{
				UiLog.LogInternal("Entered Edit Mode");
				s_isInitialized = false;
				AssetReadyGate.WhenReady(() => Initialize(), false, 5, 300);
			}
		}

#endif
		public static void ThrowIfNotInitialized()
		{
			if (!IsInitialized)
				throw new ToolkitNotInitializedException();
		}

		public static void Initialize()
		{
			if (IsInitialized)
			{
				Log("Attempt to initialize, but is already initialized");
				return;
			}

			Log("Starting initialization");
			s_isInitializing = true;
			// Config needs to be initialized first, because some other modules will need it for values.
			Log("Initialize UiToolkitConfiguration");
			UiToolkitConfiguration.Initialize();
			Log("Initialize UiMainStyleConfig");
			UiMainStyleConfig.Initialize();
			Log("Initialize UiAspectRatioDependentStyleConfig");
			UiAspectRatioDependentStyleConfig.Initialize();

			if (Application.isPlaying)
				InitializeRuntime();
			
			Log("Mark as initialized");
			s_isInitialized = true;
			s_isInitializing = false;
		}
		
		private static void InitializeRuntime()
		{
			Log("Initialize Runtime Parts");
			
			Log("Create Routing configs");
			IReadOnlyList<StorageRoutingConfig> routingConfigs = UiToolkitConfiguration.Instance.StorageFactory.CreateRoutingConfigs();

			bool hasPlayerSettingsCollection = false;

			foreach (StorageRoutingConfig config in routingConfigs)
			{
				if (config.HasCollection(StringConstants.PLAYER_SETTINGS_COLLECTION))
				{
					hasPlayerSettingsCollection = true;
					break;
				}
			}

			if (!hasPlayerSettingsCollection)
				throw new InvalidOperationException(
					$"Custom storage factory needs to route '{StringConstants.PLAYER_SETTINGS_COLLECTION}'.");

			Log("Initializing storage");
			Storage.Storage.Initialize(routingConfigs);

			Log("Initialize settings aggregate");
			SettingsPersistedAggregate aggregate = new(Storage.Storage.Documents, StringConstants.PLAYER_SETTINGS_COLLECTION, StringConstants.PLAYER_SETTINGS_ID);
			Log("Initialize player settings");
			PlayerSettings.Instance = new PlayerSettings();
			PlayerSettings.Instance.Initialize(aggregate);
		}
		
		private static void Log(string _s) => UiLog.LogInternal(_s, "Bootstrap");
	}
}