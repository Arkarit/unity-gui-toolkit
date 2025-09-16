using System;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	/// <summary>
	/// Data container that describes how a UiPanel should be loaded.
	/// It does not perform any loading itself, but provides all
	/// necessary information for the AssetLoader / AssetManager.
	/// </summary>
	public class UiPanelLoadInfo
	{
		/// <summary>
		/// Defines how the panel instance is managed after creation.
		/// </summary>
		public enum EInstantiationType
		{
			/// <summary>
			/// Create new instance, destroy after close.
			/// </summary>
			Instantiate,

			/// <summary>
			/// Reuse pooled instance.
			/// </summary>
			Pool,

			/// <summary>
			/// Create new instance, keep alive after close.
			/// </summary>
			InstantiateAndKeep,
		}

		// --------------------------------------------------------------------
		// Note: The attributes [Mandatory] / [Optional] are only markers for
		//       overview / documentation purposes. They do not enforce rules
		//       at runtime or compile-time.
		// --------------------------------------------------------------------

		/// <summary>
		/// The panel type to be loaded. Must derive from UiPanel.
		/// </summary>
		[Mandatory] public Type PanelType;

		/// <summary>
		/// Optional override for the canonical prefab identifier.
		/// If null, defaults to the PanelType name.
		/// </summary>
		[Optional] public string CanonicalId;

		/// <summary>
		/// Maximum number of allowed instances. Values &lt;= 0 mean unlimited.
		/// </summary>
		[Optional] public int MaxInstances = 0;

		/// <summary>
		/// Defines how this panel is instantiated (pooled, kept, destroyed).
		/// Defaults to Pool.
		/// </summary>
		[Optional] public EInstantiationType InstantiationType = EInstantiationType.Pool;

		/// <summary>
		/// Optional parent transform where the panel instance will be attached.
		/// </summary>
		[Optional] public Transform Parent = null;

		/// <summary>
		/// Optional initialization payload passed to the panel on creation.
		/// </summary>
		[Optional] public IInitPanelData InitPanelData = null;

		/// <summary>
		/// Callback invoked on successful load.
		/// </summary>
		[Optional] public Action<UiPanel> OnSuccess = null;

		/// <summary>
		/// Callback invoked when loading fails with an exception.
		/// </summary>
		[Optional] public Action<UiPanelLoadInfo, Exception> OnFail = null;

		/// <summary>
		/// Optional explicit asset provider. If null, the AssetManager will choose one.
		/// </summary>
		[Optional] public IAssetProvider AssetProvider = null;

		/// <summary>
		/// Returns a compact single-line summary of the load info.
		/// Useful for logs and debugging.
		/// </summary>
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

		/// <summary>
		/// Returns a multi-line formatted string version of the summary.
		/// Easier to read in log output when many fields are involved.
		/// </summary>
		public string ToMultilineString()
		{
			return ToString()
				.Replace(",", ",\n", StringComparison.Ordinal)
				.Replace("(", "(\n ", StringComparison.Ordinal)
				.Replace(")", "\n)\n");
		}
		
		public UiPanelLoadInfo() {}
		
		public UiPanelLoadInfo(CanonicalAssetKey _key)
		{
			PanelType = _key.Type;
			CanonicalId = _key.Id;
			AssetProvider = _key.Provider;
		}
		
		public UiPanelLoadInfo(CanonicalAssetRef _ref)
		{
//			PanelType = _ref.Type;
			CanonicalId = _ref.Id;
		}
		
	}
}
