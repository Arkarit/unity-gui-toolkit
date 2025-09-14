using GuiToolkit.AssetHandling;
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace GuiToolkit.Editor
{
	public class AddressablesProviderEditorBridge : IAssetProviderEditorBridge
	{
		public bool TryMakeId( UnityEngine.Object _obj, out string _addrId )
		{
			_addrId = null;
			if (_obj == null) 
				return false;

			var path = AssetDatabase.GetAssetPath(_obj);
			if (string.IsNullOrEmpty(path)) 
				return false;

			var guid = AssetDatabase.AssetPathToGUID(path);
			if (string.IsNullOrEmpty(guid)) 
				return false;

			var settings = AddressableAssetSettingsDefaultObject.Settings;
			var entry = settings ? settings.FindAssetEntry(guid) : null;
			if (entry == null) 
				return false;

			var addr = entry.address;
			_addrId = addr.StartsWith("addr:", StringComparison.Ordinal) ? addr : "addr:" + addr;
			return true;
		}

		[InitializeOnLoad]
		internal static class AddressablesProviderEditorBridgeBootstrap
		{
			static AddressablesProviderEditorBridgeBootstrap()
			{
				AddressablesAssetProvider.s_editorBridge = new AddressablesProviderEditorBridge();
			}
		}
	}
}
