using System;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	[Serializable]
	public class CanonicalAssetRef
	{
		public string Type;     // logical panel type name (e.g., "ShopPanel")
		public string PanelId;  // canonical id with prefix: "res:/...", "guid:...", "addr:..."
	}
}