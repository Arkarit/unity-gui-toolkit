using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public class UiPanelLoader
	{
		public static UiPanelLoader s_instance;
		private class LoadedPrefab
		{
			public GameObject Prefab;
			public IInstanceHandle Handle;
		}
		
		private readonly HashSet<string> m_BeingLoaded = new();
		private readonly Dictionary<string, LoadedPrefab> m_loadedPrefabsByClassName = new();

		
		public static UiPanelLoader Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new UiPanelLoader();
				return s_instance; 
			}
		}
		
		public void LoadAsync(UiPanelLoadInfo _loadInfo)
		{
			if (_loadInfo == null)
				throw new ArgumentNullException($"{nameof(UiPanelLoadInfo)} is null!");
			
			if (_loadInfo.MaxInstances > 0)
			{
				int openInstances = UiPanel.GetNumOpenDialogs(_loadInfo.PanelType);
				UiPanel.DebugLogStatic($"Checking max instances {_loadInfo.PanelType.Name}: openInstances:{openInstances} loadInfo.MaxInstances:{_loadInfo.MaxInstances}");
				if (openInstances >= _loadInfo.MaxInstances)
				{
					Debug.LogWarning($"Attempt to load panel '{_loadInfo.PanelType.Name}', " + 
					                 $"but open instances ({openInstances}) is >= allowed instances in load info {_loadInfo.MaxInstances}, cancelling load without callbacks");
					return;
				}
			}
			
			
		}
		
#if false		
		public void LoadAsync(UiPanelLoadInfo _loadInfo)
		{
			if (_loadInfo == null)
				throw new ArgumentNullException($"{nameof(UiPanelLoadInfo)} is null!");
			
			
			if (_loadInfo.MaxInstances > 0)
			{
				int openInstances = UiPanel.GetNumOpenDialogs(_loadInfo.PanelType);
				UiPanel.DebugLogStatic($"Checking max instances {_loadInfo.PanelType.Name}: openInstances:{openInstances} loadInfo.MaxInstances:{_loadInfo.MaxInstances}");
				if (openInstances >= _loadInfo.MaxInstances)
				{
					Debug.LogWarning($"Attempt to load panel '{_loadInfo.PanelType.Name}', " + 
					                 $"but open instances ({openInstances}) is >= allowed instances in load info {_loadInfo.MaxInstances}, cancelling load without callbacks");
					return;
				}
			}
			
			if (_loadInfo.InstantiationType == UiPanelLoadInfo.EInstantiationType.Pool && PrefabPools.instance.HasPrefab(_loadInfo.PanelType.Name))
			{
				GetPanelFromPool(_loadInfo);
				return;
			}
			
			// Check + load config. Loaded sync, since it only contains a few asset references and strings.
			if (!m_PanelConfig)
				LoadConfig();
			
			UiPanel.DebugLogStatic($"Attempt to find already loaded {_loadInfo.PanelType.Name}");
			if (m_loadedPrefabsByClassName.TryGetValue(_loadInfo.PanelType.Name, out LoadedPrefab loadedPrefab))
			{
				UiPanel.DebugLogStatic($"Success.");
				DoInstantiate(_loadInfo, loadedPrefab.Prefab);
				return;
			}
			
			UiPanel.DebugLogStatic($"Fail.");

			var assetReference = m_PanelConfig.GetAssetReferenceByType(_loadInfo.PanelType);
			if (assetReference == null)
			{
				if (_loadInfo.OnFail != null)
				{
					_loadInfo.OnFail.Invoke(_loadInfo);
					return;
				}

				// An error is only issued if there is no dedicated fail handler
				Dbg.Error(this, $"Could not load panel of type '{_loadInfo.PanelType.Name}'. Please check your {nameof(UiPanelConfig)}");
				return;
			}
			
			// Panel is already being loaded, just wait for it
			if (m_BeingLoaded.Contains(_loadInfo.PanelType.Name))
			{
				UiPanel.DebugLogStatic($"Already being loaded, waiting.");
				ServiceLocator.CoRoutineRunner.StartCoroutine(WaitForPanelLoadedCoroutine(_loadInfo));
				return;
			}
			
			m_BeingLoaded.Add(_loadInfo.PanelType.Name);
			ServiceLocator.CoRoutineRunner.StartCoroutine(LoadPanelCoroutine(assetReference, _loadInfo));
		}

		IEnumerator WaitForPanelLoadedCoroutine(UiPanelLoadInfo loadInfo)
		{
			while (m_BeingLoaded.Contains(loadInfo.PanelType.Name))
				yield return 0;
			
			UiPanel.DebugLogStatic($"Loading finished in other coroutine, now instanciating panel.");
			// We needn't re-implement anything; OnLoadPanel() now should find the panel in m_LoadedPrefabsByClassName
			// and do the rest of the work.
			OnLoadPanel(loadInfo);
		}
		
		IEnumerator LoadPanelCoroutine(AssetReferenceUiPanel assetReference, UiPanelLoadInfo loadInfo)
		{
			UiPanel.DebugLogStatic($"Loading panel coroutine {loadInfo.PanelType.Name}");
			var handle = Addressables.LoadAssetAsync<GameObject>(assetReference);
			yield return handle;
			
			m_BeingLoaded.Remove(loadInfo.PanelType.Name);
			
			if (!handle.IsValid())
			{
				if (loadInfo.OnFail != null)
				{
					loadInfo.OnFail.Invoke(loadInfo);
					yield break;
				}

				// An error is only issued if there is no dedicated fail handler
				Dbg.Error(this, $"'{loadInfo.PanelType.Name}' has {nameof(UiPanelConfig)} entry, handle is invalid. Please check your {nameof(UiPanelConfig)}");
				yield break;
			}
			
			if (handle.Result == null)
			{
				handle.SafeRelease();
				if (loadInfo.OnFail != null)
				{
					loadInfo.OnFail.Invoke(loadInfo);
					yield break;
				}

				// An error is only issued if there is no dedicated fail handler
				Dbg.Error(this, $"'{loadInfo.PanelType.Name}' has entry in {nameof(UiPanelConfig)}, but asset loading failed. Please check your {nameof(UiPanelConfig)}");
				yield break;
			}

			UiPanel.DebugLogStatic($"Adding panel {loadInfo.PanelType.Name}");
			var go = handle.Result;
			m_loadedPrefabsByClassName.Add(loadInfo.PanelType.Name, new LoadedPrefab()
			{
				Prefab = go,
				Handle = handle
			});
			
			DoInstantiate(loadInfo, go);
		}
#endif
	}
}