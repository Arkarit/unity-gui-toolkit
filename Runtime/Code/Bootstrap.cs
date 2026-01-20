using GuiToolkit.Exceptions;
using GuiToolkit.Settings;
using GuiToolkit.Storage;
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[EditorAware]
	public static class Bootstrap
	{
		private static bool s_isInitialized = false;
		public static bool IsInitialized => s_isInitialized;

#if UNITY_EDITOR
		static Bootstrap()
		{
			EditorApplication.playModeStateChanged -= HandlePlayMode;
			EditorApplication.playModeStateChanged += HandlePlayMode;
		}

		private static void HandlePlayMode(PlayModeStateChange _playMode)
		{
			s_isInitialized = false;
			if (_playMode == PlayModeStateChange.EnteredEditMode)
				AssetReadyGate.WhenReady(() => Initialize());
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
				return;
			
			// We need to set this early, because otherwise some modules will complain/throw because not initialized toolkit
			s_isInitialized = true;

			UiToolkitConfiguration.Initialize();

			if (Application.isPlaying)
				InitializeRuntime();
		}


		private static void InitializeRuntime()
		{
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

			Storage.Storage.Initialize(routingConfigs);

			SettingsPersistedAggregate aggregate = new(Storage.Storage.Documents, StringConstants.PLAYER_SETTINGS_COLLECTION, StringConstants.PLAYER_SETTINGS_ID);
			PlayerSettings.Instance = new PlayerSettings();
			PlayerSettings.Instance.Initialize(aggregate);
		}
	}
}