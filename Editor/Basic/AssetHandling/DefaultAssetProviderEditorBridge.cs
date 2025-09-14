using GuiToolkit.AssetHandling;
using System;
using UnityEditor;

namespace GuiToolkit.Editor
{
    /// <summary>
    /// Editor bridge for the <see cref="DefaultAssetProvider"/>.
    /// Provides conversion from <see cref="UnityEngine.Object"/> references
    /// to normalized resource identifiers based on <c>Resources/</c> folder paths.
    /// </summary>
    public class DefaultAssetProviderEditorBridge : IAssetProviderEditorBridge
    {
        /// <summary>
        /// Try to convert a Unity Object reference to a resource identifier.
        /// </summary>
        /// <param name="_obj">The Unity object to analyze.</param>
        /// <param name="_resId">
        /// Output: resource id string prefixed with <c>res:</c>,
        /// or null if the object cannot be resolved.
        /// </param>
        /// <returns>
        /// True if a valid id was generated, false otherwise.
        /// </returns>
        public bool TryMakeId(UnityEngine.Object _obj, out string _resId)
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
            if (dot >= 0)
                sub = sub.Substring(0, dot);

            _resId = sub.StartsWith("res:", StringComparison.Ordinal) ? sub : "res:" + sub;
            return true;
        }

        /// <summary>
        /// Bootstrap class that automatically assigns this bridge
        /// to <see cref="DefaultAssetProvider.s_editorBridge"/> on editor load.
        /// </summary>
        [InitializeOnLoad]
        internal static class DefaultAssetProviderEditorBridgeBootstrap
        {
            /// <summary>
            /// Static constructor: installs the bridge when Unity loads the editor domain.
            /// </summary>
            static DefaultAssetProviderEditorBridgeBootstrap()
            {
                DefaultAssetProvider.s_editorBridge = new DefaultAssetProviderEditorBridge();
            }
        }
    }
}
