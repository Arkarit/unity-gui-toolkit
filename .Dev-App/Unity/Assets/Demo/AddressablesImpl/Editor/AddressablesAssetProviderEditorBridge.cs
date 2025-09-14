using GuiToolkit.AssetHandling;
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace GuiToolkit.Editor
{
    /// <summary>
    /// Editor bridge for the Addressables provider.
    /// Converts a <see cref="UnityEngine.Object"/> into a normalized
    /// addressable identifier based on the Addressables settings.
    /// </summary>
    public class AddressablesAssetProviderEditorBridge : IAssetProviderEditorBridge
    {
        /// <summary>
        /// Try to create an addressable id for a given Unity object.
        /// </summary>
        /// <param name="_obj">Unity object to resolve.</param>
        /// <param name="_addrId">
        /// Output: Identifier prefixed with <c>addr:</c> if the object is part of Addressables;
        /// otherwise null.
        /// </param>
        /// <returns>True if an id could be created, otherwise false.</returns>
        public bool TryMakeId(UnityEngine.Object _obj, out string _addrId)
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

        /// <summary>
        /// Bootstrapper that registers this bridge on editor domain load.
        /// </summary>
        [InitializeOnLoad]
        internal static class AddressablesAssetProviderEditorBridgeBootstrap
        {
            /// <summary>
            /// Assign this bridge to the Addressables provider's editor hook.
            /// </summary>
            static AddressablesAssetProviderEditorBridgeBootstrap()
            {
                // NOTE: fixed type name here (was: AddressablesAssetProvider)
                AddressablesAssetProvider.s_editorBridge = new AddressablesAssetProviderEditorBridge();
            }
        }
    }
}
