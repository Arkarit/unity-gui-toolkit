using System;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	[Serializable]
	public class CanonicalAssetRef
	{
		public string Type;
		public string Id;
		
		public Type GetContentType()
		{
			if (string.IsNullOrEmpty(Type))
				return null;
			
			return System.Type.GetType(Type);
		}
	}
}