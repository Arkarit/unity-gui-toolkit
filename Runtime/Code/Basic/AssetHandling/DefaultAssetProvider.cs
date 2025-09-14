using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

using Object = UnityEngine.Object;
using GuiToolkit.Exceptions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.AssetHandling
{
    /// <summary>
    /// Default instance handle for non-Addressables providers.
    /// Implements release semantics by destroying the instantiated GameObject.
    /// </summary>
    public sealed class DefaultInstanceHandle : IInstanceHandle
    {
        /// <summary>
        /// The instantiated GameObject.
        /// </summary>
        public GameObject Instance { get; private set; }

        /// <summary>
        /// Create an instance handle for a given GameObject.
        /// </summary>
        /// <param name="_object">The instantiated GameObject.</param>
        public DefaultInstanceHandle(GameObject _object)
        {
            Instance = _object;
        }

        /// <summary>
        /// Release the instance (destroy the GameObject).
        /// </summary>
        public void Release()
        {
            if (!Instance)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying)
                Object.Destroy(Instance);
            else
                Object.DestroyImmediate(Instance);
#else
            Object.Destroy(Instance);
#endif
            Instance = null;
        }
    }

    /// <summary>
    /// Default asset handle for synchronous Resources loading.
    /// </summary>
    /// <typeparam name="T">Unity Object type.</typeparam>
    public sealed class DefaultAssetHandle<T> : IAssetHandle<T> where T : Object
    {
        /// <summary>
        /// The loaded asset reference.
        /// </summary>
        public T Asset { get; }

        /// <summary>
        /// Canonical key used for loading (for diagnostics).
        /// </summary>
        public CanonicalAssetKey Key { get; }

        /// <summary>
        /// Whether the asset is currently loaded (non-null).
        /// </summary>
        public bool IsLoaded => Asset != null;

        /// <summary>
        /// Release the loaded asset.
        /// <note>
        /// Resources-based loading does not require an explicit release,
        /// so this is a no-op in the default provider.
        /// </note>
        /// </summary>
        public void Release() { }

        /// <summary>
        /// Create a default asset handle.
        /// </summary>
        /// <param name="_asset">The loaded asset.</param>
        /// <param name="_key">The canonical key used to load it.</param>
        public DefaultAssetHandle(T _asset, CanonicalAssetKey _key)
        {
            Asset = _asset;
            Key = _key;
        }
    }

    /// <summary>
    /// Default asset provider backed by <c>Resources</c>.
    /// Accepts canonical ids with prefix <c>res:</c> and, as a fallback,
    /// unprefixed strings which are treated as Resources paths.
    /// </summary>
    public sealed class DefaultAssetProvider : IAssetProvider
    {
        /// <summary>
        /// Optional editor bridge for converting object references to ids.
        /// </summary>
        public static IAssetProviderEditorBridge s_editorBridge;

        /// <inheritdoc/>
        public string Name => "Default Asset Provider";

        /// <inheritdoc/>
        public string ResName => "Resource";

        /// <inheritdoc/>
        public bool IsInitialized => true;

        /// <inheritdoc/>
        public void Init() { }

        /// <inheritdoc/>
        public IAssetProviderEditorBridge EditorBridge => s_editorBridge;

        /// <summary>
        /// Instantiate a prefab by key and return an instance handle.
        /// </summary>
        /// <param name="_key">Raw key (string, CanonicalAssetKey, or Object in Editor).</param>
        /// <param name="_parent">Optional parent transform.</param>
        /// <param name="_cancellationToken">Cancellation token.</param>
        public async Task<IInstanceHandle> InstantiateAsync
        (
            object _key,
            Transform _parent = null,
            CancellationToken _cancellationToken = default
        )
        {
            var assetKey = NormalizeKey<GameObject>(_key);
            var prefab = (GameObject)Load(assetKey, _cancellationToken);

            // Yield once to keep behavior consistent with async call sites.
            await Task.Yield();
            _cancellationToken.ThrowIfCancellationRequested();

            var instance = Object.Instantiate(prefab, _parent);
            return new DefaultInstanceHandle(instance);
        }

        /// <summary>
        /// Load an asset (synchronously via Resources) and return a handle.
        /// </summary>
        /// <typeparam name="T">Unity Object type to load.</typeparam>
        /// <param name="_key">Raw key (string, CanonicalAssetKey, or Object in Editor).</param>
        /// <param name="_cancellationToken">Cancellation token.</param>
        public Task<IAssetHandle<T>> LoadAssetAsync<T>
        (
            object _key,
            CancellationToken _cancellationToken = default
        ) where T : Object
        {
            var assetKey = NormalizeKey<T>(_key);
            var obj = (T)Load(assetKey, _cancellationToken);

            // Actually loaded synchronously; returning a completed handle.
            return Task.FromResult<IAssetHandle<T>>(new DefaultAssetHandle<T>(obj, assetKey));
        }

        /// <inheritdoc/>
        public void Release<T>(IAssetHandle<T> _handle) where T : Object
        {
            _handle?.Release(); // no-op for Resources
        }

        /// <inheritdoc/>
        public void Release(IInstanceHandle _handle)
        {
            _handle?.Release();
        }

        /// <inheritdoc/>
        public void ReleaseUnused() { }

        /// <inheritdoc/>
        public CanonicalAssetKey NormalizeKey<T>(object _key) where T : Object => NormalizeKey(_key, typeof(T));

        /// <summary>
        /// Normalize a raw key to a canonical key for Resources.
        /// </summary>
        /// <param name="_key">String id, CanonicalAssetKey, or (Editor-only) Object.</param>
        /// <param name="_type">Expected Unity type.</param>
        /// <returns>Canonical key with provider set to this.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when trying to use a key that belongs to another provider.
        /// </exception>
        public CanonicalAssetKey NormalizeKey(object _key, Type _type)
        {
            if (_key is CanonicalAssetKey assetKey)
            {
                if (!ReferenceEquals(assetKey.Provider, this))
                    throw new InvalidOperationException
                    (
                        $"Attempt to use Key designed for Asset provider '{assetKey.Provider?.GetType().Name}' with '{GetType().Name}'"
                    );

                return assetKey;
            }

#if UNITY_EDITOR
            if (_key is Object obj)
                if (EditorBridge != null && EditorBridge.TryMakeId(obj, out string id))
                    return new CanonicalAssetKey(this, id, obj.GetType());
#else
            if (_key is Object obj)
                throw new InvalidOperationException("Object keys are not supported. Use a 'res:' path instead.");
#endif

            if (_key is string pathStr)
                return new CanonicalAssetKey(this,
                    pathStr.StartsWith("res:", StringComparison.Ordinal) ? pathStr : $"res:{pathStr}",
                    _type);

            return new CanonicalAssetKey(this, $"unknown:{_key}", _type);
        }

        /// <inheritdoc/>
        public bool Supports(CanonicalAssetKey _key) => _key.Provider == this;

        /// <inheritdoc/>
        public bool Supports(string _id)
        {
            if (string.IsNullOrEmpty(_id)) return false;
            return _id.StartsWith("res:", StringComparison.Ordinal)
                   || !_id.Contains(":"); // fallback for unprefixed strings
        }

        /// <inheritdoc/>
        public bool Supports(object _obj)
        {
            if (_obj is CanonicalAssetKey key)
                return Supports(key);

            if (_obj is string id)
                return Supports(id);

#if UNITY_EDITOR
            if (_obj is Object obj)
                return EditorBridge != null
                    && EditorBridge.TryMakeId(obj, out _);
#endif

            return false;
        }

        /// <summary>
        /// Load a Unity Object via Resources based on a canonical key.
        /// </summary>
        /// <param name="_assetKey">Canonical key (must be <c>res:</c>-based for this provider).</param>
        /// <param name="_cancellationToken">Cancellation token.</param>
        /// <returns>The loaded object (never null if successful).</returns>
        /// <exception cref="AssetLoadFailedException">
        /// Thrown when the asset does not exist or when a requested component
        /// type does not exist on a loaded GameObject.
        /// </exception>
        public Object Load(CanonicalAssetKey _assetKey, CancellationToken _cancellationToken)
        {
            Object result = null;

            if (_assetKey.TryGetValue("res:", out string resourcePath))
            {
                // Components cannot be loaded directly via Resources.
                // We load a GameObject and (optionally) verify the required component type exists.
                if (typeof(Component).IsAssignableFrom(_assetKey.Type))
                {
                    var go = Resources.Load(resourcePath, typeof(GameObject)) as GameObject;
                    if (!go)
                        throw new AssetLoadFailedException(_assetKey);

                    var component = go.GetComponent(_assetKey.Type);
                    if (!component)
                    {
                        go.SafeDestroy();
                        throw new AssetLoadFailedException(_assetKey,
                            $"Asset was found, but didn't carry a Component of type '{_assetKey.Type}'");
                    }

                    result = go;
                }
                else
                {
                    result = Resources.Load(resourcePath, _assetKey.Type);
                }
            }

            _cancellationToken.ThrowIfCancellationRequested();

            if (!result)
                throw new AssetLoadFailedException(_assetKey);

            return result;
        }
    }
}
