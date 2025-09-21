using System;
using System.Threading.Tasks;
using GuiToolkit.Exceptions;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	/// <summary>
	/// High-level loader that creates UiPanels either directly or via pool.
	/// Delegates key resolution and instantiation to AssetManager / UiPool.
	/// </summary>
	public class AssetLoader
	{
		public static AssetLoader s_instance;

		private class LoadedPrefab
		{
			public GameObject Prefab;
			public IInstanceHandle Handle;
		}

		private readonly System.Collections.Generic.HashSet<string> m_BeingLoaded = new();
		private readonly System.Collections.Generic.Dictionary<string, LoadedPrefab> m_loadedPrefabsByClassName = new();

		/// <summary>
		/// Singleton instance of the loader.
		/// </summary>
		public static AssetLoader Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new AssetLoader();

				return s_instance;
			}
		}

		/// <summary>
		/// Load a panel according to the provided load info.
		/// Decides between direct instantiate and pool path.
		/// </summary>
		/// <param name="_loadInfo">Panel load parameters.</param>
		public void LoadAsync( UiPanelLoadInfo _loadInfo )
		{
			if (_loadInfo == null)
				throw new ArgumentNullException(nameof(_loadInfo));

			if (_loadInfo.MaxInstances > 0)
			{
				int openInstances = UiPanel.GetNumOpenDialogs(_loadInfo.PanelType);
				UiPanel.DebugLogStatic(
					$"Checking max instances {_loadInfo.PanelType.Name}: openInstances:{openInstances} loadInfo.MaxInstances:{_loadInfo.MaxInstances}"
				);

				if (openInstances >= _loadInfo.MaxInstances)
				{
					UiLog.LogWarning(
						$"Attempt to load panel '{_loadInfo.PanelType.Name}', " +
						$"but open instances ({openInstances}) is >= allowed instances in load info {_loadInfo.MaxInstances}, cancelling load without callbacks"
					);
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

		// =====================================================================
		// private helpers
		// =====================================================================

		private async Task LoadAsyncImpl( UiPanelLoadInfo _loadInfo )
		{
			UiPanel panel = null;
			try
			{
				var key = new CanonicalAssetKey(_loadInfo.AssetProvider, _loadInfo.CanonicalId, _loadInfo.PanelType);

				var instHandle = await AssetManager.InstantiateAsync(key, _loadInfo.Parent);
				var go = instHandle.Instance;
				panel = go != null ? go.GetComponent(_loadInfo.PanelType) as UiPanel : null;

				if (panel == null)
				{
					instHandle.Release();
					throw new AssetLoadFailedException(
						key,
						$"Instance does not contain requested panel type '{_loadInfo.PanelType.Name}'."
					);
				}

				panel.SetHandle(instHandle);

				// We need to try/catch possible exceptions in panel.Init() separately, since these interfere with the loading exceptions
				try
				{
					if (_loadInfo.InitPanelData != null)
						panel.Init(_loadInfo.InitPanelData);
				}
				catch (Exception ex)
				{
					SafeInvokeFail(_loadInfo, panel, ex);
					// at this point, panel is already loaded and releases the handle itself, so we don't need to keep track
					panel.SafeDestroy();
					return;
				}

				SafeInvokeSuccess(_loadInfo, panel);
			}
			catch (OperationCanceledException ex)
			{
				SafeInvokeFail(_loadInfo, panel, ex);
			}
			catch (Exception ex)
			{
				SafeInvokeFail(_loadInfo, panel, ex);
			}
		}

		private async Task LoadAsyncFromPool( UiPanelLoadInfo _loadInfo )
		{
			UiPanel panel = null;
			GameObject go = null;

			try
			{
				var key = new CanonicalAssetKey(_loadInfo.AssetProvider, _loadInfo.CanonicalId, _loadInfo.PanelType);

				go = await UiPool.Instance.GetAsync(key);
				panel = go ? go.GetComponent(_loadInfo.PanelType) as UiPanel : null;

				if (panel == null)
				{
					UiPool.Instance.Release(go);
					go = null; // prevent double release in catch/finally
					throw new AssetLoadFailedException(
						key,
						$"Pooled instance does not contain requested panel type '{_loadInfo.PanelType.Name}'."
					);
				}

				// Handle is managed by pool; set it to null for the panel to be sure
				panel.SetHandle(null);

				// We need to try/catch possible exceptions in panel.Init() separately, since these interfere with the loading exceptions
				try
				{
					if (_loadInfo.InitPanelData != null)
						panel.Init(_loadInfo.InitPanelData);
				}
				catch (Exception ex)
				{
					SafeInvokeFail(_loadInfo, panel, ex);
					UiPool.Instance.Release(go);
					go = null;
					return;
				}

				SafeInvokeSuccess(_loadInfo, panel);
			}
			catch (OperationCanceledException ex)
			{
				if (panel == null && go != null)
				{
					UiPool.Instance.Release(go);
					go = null;
				}
				
				SafeInvokeFail(_loadInfo, panel, ex);
			}
			catch (Exception ex)
			{
				if (panel == null && go != null)
				{
					UiPool.Instance.Release(go);
					go = null;
				}
				
				SafeInvokeFail(_loadInfo, panel, ex);
			}
		}
		
		private static void SafeInvokeSuccess( UiPanelLoadInfo _loadInfo, UiPanel _panel )
		{
			try
			{
				_loadInfo.OnSuccess?.Invoke(_panel);
			}
			catch (Exception cbEx)
			{
				UiLog.LogError($"Exception in OnSuccess():{cbEx}");
			}
		}

		private static void SafeInvokeFail( UiPanelLoadInfo _loadInfo, UiPanel _panel, Exception _ex )
		{
			try
			{
				if (_panel)
					_panel.OnAssetLoadFailedInternal(_ex);

				_loadInfo.OnFail?.Invoke(_loadInfo, _ex);
			}
			catch (Exception cbEx)
			{
				UiLog.LogError($"Exception in OnFail():{cbEx}");
			}

			UiLog.LogError($"Panel load failed, exception:{_ex}");
		}
	}
}
