using GuiToolkit;
using System;
using UnityEngine;


namespace GuiToolkit.AssetHandling
{

	public class UiPanelLoadInfo
	{
		public enum EInstantiationType
		{
			Instantiate,        // Automatically destroyed after close
			Pool,               // Pooled
			InstantiateAndKeep, // Kept in memory after close
		}

		public Type PanelType;
		public int MaxInstances = 0; // maximum number of allowed instances for this panel. <= 0 means unlimited.
		public EInstantiationType InstantiationType = EInstantiationType.Pool;
		public Transform Parent = null;
		public IInitPanelData InitPanelData = null;
		public Action<UiPanel> OnSuccess = null;
		public Action<UiPanelLoadInfo, Exception> OnFail = null;
		public IAssetProvider AssetProvider = null;

		public override string ToString()
		{
			string parentPath = Parent != null ? Parent.GetPath() : "<null>";
			string initData = InitPanelData != null ? InitPanelData.GetType().Name : "<null>";
			string provider = AssetProvider != null ? AssetProvider.Name : "<null>";

			return $"UiPanelLoadInfo(" +
				   $"PanelType={PanelType?.Name ?? "<null>"}, " +
				   $"InstantiationType={InstantiationType}, " +
				   $"MaxInstances={(MaxInstances <= 0 ? "unlimited" : MaxInstances.ToString())}, " +
				   $"Parent={parentPath}, " +
				   $"InitPanelData={initData}, " +
				   $"OnSuccess={(OnSuccess != null ? "yes" : "no")}, " +
				   $"OnFail={(OnFail != null ? "yes" : "no")}, " +
				   $"AssetProvider={provider})";
		}

	}

}