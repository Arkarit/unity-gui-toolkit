using GuiToolkit.Exceptions;
using GuiToolkit.Settings;
using GuiToolkit.Storage;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using GuiToolkit.Editor;
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

			// NOT EditorApplication.delayCall. It promises "some later tick" and in a background editor that
			// tick may never come: measured in an unfocused editor, a delayCall scheduled from outside had
			// still not run after 20 seconds, while EditorApplication.update was ticking the whole time
			// (the MCP bridge is pumped from it and answered every request in milliseconds). Since the
			// entire editor-side initialisation of the toolkit hangs off this one call, an unfocused editor
			// answered "GuiToolkit is not initialized" to everything until a human clicked into the window.
			// The update path fires on the next tick regardless of focus; one shot, unsubscribed immediately.
			EditorApplication.update += InitializeOnNextTick;
		}

		private static void InitializeOnNextTick()
		{
			EditorApplication.update -= InitializeOnNextTick;
			Delayed();
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
				AssetReadyGate.WhenReady(() => Initialize(), false);
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
			
#if UNITY_EDITOR
			DoxygenConfig.Initialize();
			ImageMagickConfig.Initialize();
#endif

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