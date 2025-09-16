using System;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	[Serializable]
	public class CanonicalAssetRef
	{
		public string Type;
		public string Id;
		
		public Type GetType() => System.Type.GetType(Type);
	}
}