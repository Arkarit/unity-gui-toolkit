using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GuiToolkit.Exceptions;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public class AssetLoader
	{
		public static AssetLoader s_instance;
		private class LoadedPrefab
		{
			public GameObject Prefab;
			public IInstanceHandle Handle;
		}

		private readonly HashSet<string> m_BeingLoaded = new();
		private readonly Dictionary<string, LoadedPrefab> m_loadedPrefabsByClassName = new();


		public static AssetLoader Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new AssetLoader();
				return s_instance;
			}
		}

		public void LoadAsync( UiPanelLoadInfo _loadInfo )
		{
			if (_loadInfo == null)
				throw new ArgumentNullException(nameof(_loadInfo));

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

			if (_loadInfo.InstantiationType == UiPanelLoadInfo.EInstantiationType.Pool)
			{
				LoadAsyncFromPool(_loadInfo);
				return;
			}

			LoadAsyncImpl(_loadInfo);
		}

		private async Task LoadAsyncImpl( UiPanelLoadInfo _loadInfo )
		{
			try
			{
				var key = new CanonicalAssetKey(_loadInfo.AssetProvider, _loadInfo.CanonicalId, _loadInfo.PanelType);

				var instHandle = await AssetManager.InstantiateAsync(key, _loadInfo.Parent);
				var go = instHandle.Instance;
				var panel = go != null ? go.GetComponent(_loadInfo.PanelType) as UiPanel : null;

				if (panel == null)
				{
					instHandle.Release();
					throw new AssetLoadFailedException(key,
						$"Instance does not contain requested panel type '{_loadInfo.PanelType.Name}'.");
				}

				if (_loadInfo.InitPanelData != null)
					panel.Init(_loadInfo.InitPanelData, instHandle);

				_loadInfo.OnSuccess?.Invoke(panel);
			}
			catch (OperationCanceledException ex)
			{
				_loadInfo.OnFail?.Invoke(_loadInfo, ex);
			}
			catch (Exception ex)
			{
				_loadInfo.OnFail?.Invoke(_loadInfo, ex);
			}
		}

		private async Task LoadAsyncFromPool( UiPanelLoadInfo _loadInfo )
		{
			try
			{
				var key = new CanonicalAssetKey(_loadInfo.AssetProvider, _loadInfo.CanonicalId, _loadInfo.PanelType);

				var go = await UiPool.Instance.GetAsync(key);
				var panel = go != null ? go.GetComponent(_loadInfo.PanelType) as UiPanel : null;

				if (panel == null)
				{
					UiPool.Instance.Release(go);
					throw new AssetLoadFailedException(key,
						$"Pooled instance does not contain requested panel type '{_loadInfo.PanelType.Name}'.");
				}

				if (_loadInfo.InitPanelData != null)
					panel.Init(_loadInfo.InitPanelData, null);

				_loadInfo.OnSuccess?.Invoke(panel);
			}
			catch (OperationCanceledException ex)
			{
				_loadInfo.OnFail?.Invoke(_loadInfo, ex);
			}
			catch (Exception ex)
			{
				_loadInfo.OnFail?.Invoke(_loadInfo, ex);
			}
		}
	}
}