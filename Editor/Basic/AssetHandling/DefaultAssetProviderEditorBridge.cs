using GuiToolkit.AssetHandling;
using System;
using UnityEditor;

namespace GuiToolkit.Editor
{
	public class DefaultAssetProviderEditorBridge : IAssetProviderEditorBridge
	{
		public bool TryMakeId( UnityEngine.Object _obj, out string _resId )
		{
			_resId = null;
			if (_obj == null) 
				return false;

			var path = AssetDatabase.GetAssetPath(_obj);
			if (string.IsNullOrEmpty(path)) 
				return false;

			int idx = path.IndexOf("/Resources/", StringComparison.Ordinal);
			if (idx < 0) 
				return false;

			var sub = path.Substring(idx + "/Resources/".Length);
			int dot = sub.LastIndexOf('.');
			if (dot >= 0) sub = sub.Substring(0, dot);

			_resId = sub.StartsWith("res:", StringComparison.Ordinal) ? sub : "res:" + sub;
			return true;
		}
		
		[InitializeOnLoad]
		internal static class DefaultAssetProviderEditorBridgeBootstrap
		{
			static DefaultAssetProviderEditorBridgeBootstrap()
			{
				DefaultAssetProvider.s_editorBridge = new DefaultAssetProviderEditorBridge();
			}
		}
	}
}
