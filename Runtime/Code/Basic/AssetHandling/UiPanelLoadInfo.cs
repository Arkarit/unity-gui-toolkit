using Codice.Client.BaseCommands.CheckIn;
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

		// Note: The Attributes used here are solely for overview which parts are mandatory

		[Mandatory] public Type PanelType;
		[Optional] public string CanonicalId;
		[Optional] public int MaxInstances = 0;
		[Optional] public EInstantiationType InstantiationType = EInstantiationType.Pool;
		[Optional] public Transform Parent = null;
		[Optional] public IInitPanelData InitPanelData = null;
		[Optional] public Action<UiPanel> OnSuccess = null;
		[Optional] public Action<UiPanelLoadInfo, Exception> OnFail = null;
		[Optional] public IAssetProvider AssetProvider = null;

		public override string ToString()
		{
			string parentPath = Parent != null ? Parent.GetPath() : "<null>";
			string initData = InitPanelData != null ? InitPanelData.GetType().Name : "<null>";
			string provider = AssetProvider != null ? AssetProvider.Name : "<null>";

			return $"UiPanelLoadInfo(" +
				   $"PanelType={PanelType?.Name ?? "<null>"}, " +
				   $"CanonicalKey={CanonicalId ?? "<null>"}, " +
				   $"InstantiationType={InstantiationType}, " +
				   $"MaxInstances={(MaxInstances <= 0 ? "unlimited" : MaxInstances.ToString())}, " +
				   $"Parent={parentPath}, " +
				   $"InitPanelData={initData}, " +
				   $"OnSuccess={(OnSuccess != null ? "yes" : "no")}, " +
				   $"OnFail={(OnFail != null ? "yes" : "no")}, " +
				   $"AssetProvider={provider})";
		}

		public string ToMultilineString()
		{
			return 
				ToString()
				.Replace(",", ",\n", StringComparison.Ordinal)
				.Replace("(", "(\n ", StringComparison.Ordinal)
				.Replace(")", "\n)\n");
		}

	}

}