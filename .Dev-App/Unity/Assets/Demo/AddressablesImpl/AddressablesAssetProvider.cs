using System;
using GuiToolkit.AssetHandling;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using GuiToolkit.Exceptions;
using NUnit.Framework.Internal.Filters;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using Addressables = UnityEngine.AddressableAssets.Addressables;
using AssetReference = UnityEngine.AddressableAssets.AssetReference;
using System.Runtime.CompilerServices;
using GuiToolkit.Debugging;

/// <summary>
/// Instance handle for Addressables-based instantiation.
/// Manages the lifetime of an instantiated GameObject via Addressables.
/// </summary>
public sealed class AddressableInstanceHandle : IInstanceHandle
{
    private bool m_isReleased;
    private AsyncOperationHandle<GameObject> m_handle;

    /// <summary>
    /// The instantiated GameObject (result of the Addressables operation).
    /// </summary>
    public GameObject Instance { get; private set; }

    /// <summary>
    /// Create a new instance handle from an Addressables instantiate handle.
    /// </summary>
    /// <param name="_handle">The completed instantiate handle.</param>
    public AddressableInstanceHandle(AsyncOperationHandle<GameObject> _handle)
    {
        m_handle = _handle;
        Instance = _handle.Result;
    }

    /// <summary>
    /// Release this instance. Calls <c>Addressables.ReleaseInstance</c> if the GameObject is alive;
    /// otherwise releases the underlying handle if still valid.
    /// </summary>
    public void Release()
    {
        if (m_isReleased)
            return;

        m_isReleased = true;

        if (Instance)
        {
            Addressables.ReleaseInstance(Instance);
            Instance = null;
            return;
        }

        if (m_handle.IsValid())
            Addressables.Release(m_handle);
    }
}

/// <summary>
/// Asset handle for Addressables-based asset loads.
/// Wraps an <see cref="AsyncOperationHandle{TObject}"/> and exposes diagnostics.
/// </summary>
/// <typeparam name="T">Unity Object type of the asset.</typeparam>
public sealed class AddressableAssetHandle<T> : IAssetHandle<T> where T : Object
{
    private bool m_isReleased;

    /// <summary>
    /// Canonical key used for loading (for diagnostics).
    /// </summary>
    public CanonicalAssetKey Key { get; }

    /// <summary>
    /// The underlying Addressables async operation handle.
    /// </summary>
    public AsyncOperationHandle<T> Handle;

    /// <summary>
    /// Create a new Addressables asset handle.
    /// </summary>
    /// <param name="_handle">The async handle returned by Addressables.</param>
    /// <param name="_key">The canonical key associated with this load.</param>
    public AddressableAssetHandle(AsyncOperationHandle<T> _handle, CanonicalAssetKey _key)
    {
        Handle = _handle;
        Key = _key;
    }

    /// <summary>
    /// True if the handle is valid and succeeded.
    /// </summary>
    public bool IsLoaded => Handle.IsValid() && Handle.Status == AsyncOperationStatus.Succeeded;

    /// <summary>
    /// The loaded asset if available and not released; otherwise null.
    /// </summary>
    public T Asset => (IsLoaded && !m_isReleased) ? Handle.Result : null;

    /// <summary>
    /// Release this asset (calls <c>Addressables.Release</c> if the handle is valid).
    /// </summary>
    public void Release()
    {
        if (m_isReleased)
            return;

        m_isReleased = true;

        if (Handle.IsValid())
            Addressables.Release(Handle);
    }
}

/// <summary>
/// Addressables-backed asset provider.
/// Requires Addressables to be initialized before use.
/// </summary>
/// <remarks>
/// This demo provider triggers Addressables initialization in <see cref="Init"/>.
/// If your project initializes elsewhere, provide a no-op implementation:
/// <code>
/// public bool IsInitialized =&gt; true;
/// public void Init() {}
/// </code>
/// </remarks>
public sealed class AddressablesAssetProvider : IAssetProvider
{
    private static Task s_initTask;

    /// <summary>
    /// Reset cached init state on domain reload (per Unity runtime init).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeOnLoad() => s_initTask = null;

    /// <summary>
    /// Optional editor bridge for converting object references to ids.
    /// </summary>
    public static IAssetProviderEditorBridge s_editorBridge;

    /// <inheritdoc/>
    public string Name => "Addressables Asset Provider";

    /// <inheritdoc/>
    public string ResName => "Addressable";

    /// <inheritdoc/>
    public IAssetProviderEditorBridge EditorBridge => s_editorBridge;

    /// <summary>
    /// True once <see cref="Addressables.InitializeAsync"/> has completed (successfully or faulted).
    /// </summary>
    public bool IsInitialized => s_initTask != null && s_initTask.IsCompleted;

    /// <summary>
    /// Initialize Addressables if not already started or if the previous attempt faulted/canceled.
    /// </summary>
    public void Init()
    {
        if (s_initTask == null || s_initTask.IsFaulted || s_initTask.IsCanceled)
            s_initTask = Addressables.InitializeAsync().Task;
    }

    /// <summary>
    /// Instantiate an addressable prefab and return an instance handle.
    /// </summary>
    /// <param name="_key">Raw key (string, CanonicalAssetKey, or AssetReference).</param>
    /// <param name="_parent">Optional parent transform for the instance.</param>
    /// <param name="_cancellationToken">Cancellation token.</param>
    /// <returns>Instance handle managing the instantiated GameObject.</returns>
    /// <exception cref="NotInitializedException">Thrown if Addressables is not initialized.</exception>
    /// <exception cref="Exception">Thrown if the instantiate operation fails.</exception>
    public async Task<IInstanceHandle> InstantiateAsync
    (
        object _key,
        Transform _parent = null,
        CancellationToken _cancellationToken = default
    )
    {
        CheckInitialized();
        var assetKey = NormalizeKey<GameObject>(_key);

        if (!assetKey.TryGetValue("addr:", out string addr))
            throw new Exception($"AddressablesProvider.InstantiateAsync: Unsupported key '{assetKey.Id}' (expected 'addr:...').");

        AsyncOperationHandle<GameObject> handle = default;

        try
        {
            handle = Addressables.InstantiateAsync(addr, _parent);

            await handle.Task;

            if (_cancellationToken.IsCancellationRequested)
            {
                if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded && handle.Result)
                    Addressables.ReleaseInstance(handle.Result);
                else if (handle.IsValid())
                    Addressables.Release(handle);

                _cancellationToken.ThrowIfCancellationRequested();
            }

            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded || !handle.Result)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                throw new Exception($"InstantiateAsync failed for '{assetKey.Id}' (status {handle.Status}).");
            }

            return new AddressableInstanceHandle(handle);
        }
        catch
        {
            if (handle.IsValid())
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result)
                    Addressables.ReleaseInstance(handle.Result);
                else
                    Addressables.Release(handle);
            }
            throw;
        }
    }

    /// <summary>
    /// Load an addressable asset and return a handle.
    /// </summary>
    /// <typeparam name="T">Unity Object type to load.</typeparam>
    /// <param name="_key">Raw key (string, CanonicalAssetKey, or AssetReference).</param>
    /// <param name="_cancellationToken">Cancellation token.</param>
    /// <returns>Asset handle wrapping the Addressables operation.</returns>
    /// <exception cref="NotInitializedException">Thrown if Addressables is not initialized.</exception>
    /// <exception cref="Exception">Thrown if the load operation fails.</exception>
    public async Task<IAssetHandle<T>> LoadAssetAsync<T>
    (
        object _key,
        CancellationToken _cancellationToken = default
    ) where T : Object
    {
        CheckInitialized();

        var assetKey = NormalizeKey<T>(_key);

        if (!assetKey.TryGetValue("addr:", out string addr))
            throw new Exception($"AddressablesProvider.LoadAssetAsync: Unsupported key '{assetKey.Id}' (expected 'addr:...').");

#if UNITY_EDITOR
        // In the editor, synchronous completion is acceptable for dev workflows (e.g., D&D).
        if (!Application.isPlaying)
        {
            var h = Addressables.LoadAssetAsync<T>(addr);
            h.WaitForCompletion(); // safe in Editor
            if (!h.IsValid() || h.Status != AsyncOperationStatus.Succeeded)
            {
                if (h.IsValid()) Addressables.Release(h);
                throw new Exception($"LoadAssetAsync<{typeof(T).Name}> failed for '{assetKey.Id}' (status {h.Status}).");
            }

            return new AddressableAssetHandle<T>(h, assetKey);
        }
#endif

        AsyncOperationHandle<T> handle = default;

        try
        {
            handle = Addressables.LoadAssetAsync<T>(addr);

            await handle.Task;

            if (_cancellationToken.IsCancellationRequested)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                _cancellationToken.ThrowIfCancellationRequested();
            }

            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);

                throw new Exception($"LoadAssetAsync<{typeof(T).Name}> failed for '{assetKey.Id}' (status {handle.Status}).");
            }

            return new AddressableAssetHandle<T>(handle, assetKey);
        }
        catch
        {
            if (handle.IsValid())
                Addressables.Release(handle);
            throw;
        }
    }

    /// <inheritdoc/>
    public void Release<T>(IAssetHandle<T> _handle) where T : Object
    {
        _handle?.Release();
    }

    /// <inheritdoc/>
    public void Release(IInstanceHandle _handle)
    {
        _handle?.Release();
    }

    /// <inheritdoc/>
    public void ReleaseUnused()
    {
        // optional trimming
    }

    /// <inheritdoc/>
    public CanonicalAssetKey NormalizeKey<T>(object _key) where T : Object
    {
        CheckInitialized();
        return NormalizeKey(_key, typeof(T));
    }

    /// <summary>
    /// Normalize a raw key to a canonical key for Addressables.
    /// Accepts CanonicalAssetKey, AssetReference, string (adds 'addr:' if missing),
    /// and (Editor-only) Object via EditorBridge.
    /// </summary>
    /// <param name="_key">Raw key object.</param>
    /// <param name="_type">Expected Unity Object type.</param>
    /// <returns>Canonical key with this provider set.</returns>
    /// <exception cref="InvalidOperationException">When a key belonging to another provider is used.</exception>
    public CanonicalAssetKey NormalizeKey(object _key, Type _type)
    {
        CheckInitialized();

        if (_key is CanonicalAssetKey assetKey)
        {
            if (!ReferenceEquals(assetKey.Provider, this))
                throw new InvalidOperationException
                (
                    $"Attempt to use Key designed for Asset provider '{assetKey.Provider?.GetType().Name}' with '{GetType().Name}'"
                );

            return assetKey;
        }

        if (_key is AssetReference assetReference)
        {
            var runtimeKey = assetReference.RuntimeKey.ToString();
            if (!runtimeKey.StartsWith("addr:", StringComparison.Ordinal))
                runtimeKey = $"addr:{runtimeKey}";

            return new CanonicalAssetKey(this, runtimeKey, _type);
        }

        if (_key is string s)
        {
            var id = s.StartsWith("addr:", StringComparison.Ordinal) ? s : $"addr:{s}";
            return new CanonicalAssetKey(this, id, _type);
        }

#if UNITY_EDITOR
        if (_key is Object obj)
            if (EditorBridge != null && EditorBridge.TryMakeId(obj, out string id))
                return new CanonicalAssetKey(this, id, obj.GetType());
#else
        if (_key is Object obj)
            throw new InvalidOperationException("Object keys are not supported. Use an 'addr:' path instead.");
#endif

        return new CanonicalAssetKey(this, $"unknown:{_key}", _type);
    }

    /// <inheritdoc/>
    public bool Supports(CanonicalAssetKey _key)
    {
        CheckInitialized();
        return _key.Provider == this;
    }

    /// <inheritdoc/>
    public bool Supports(string _id)
    {
        CheckInitialized();

        if (_id.StartsWith("addr:", StringComparison.Ordinal))
            return true;

        return Exists(_id);
    }

    /// <inheritdoc/>
    public bool Supports(object _obj)
    {
        if (_obj is CanonicalAssetKey key)
            return Supports(key);

        if (_obj is string id)
            return Supports(id);

        CheckInitialized();
        if (_obj is AssetReference)
            return true;

#if UNITY_EDITOR
        if (_obj is Object obj)
            return EditorBridge != null
                && EditorBridge.TryMakeId(obj, out _);
#endif

        return false;
    }

    /// <summary>
    /// Lightweight existence check using current Addressables resource locators.
    /// Returns true if any locator can resolve the key to resource locations.
    /// </summary>
    private bool Exists(string _key)
    {
        var locators = Addressables.ResourceLocators;
        foreach (var loc in locators)
            if (loc.Locate(_key, typeof(object), out var _))
                return true;

        return false;
    }

    /// <summary>
    /// Guard to ensure Addressables was initialized before using the provider.
    /// </summary>
    /// <exception cref="NotInitializedException">Thrown when <see cref="IsInitialized"/> is false.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckInitialized()
    {
        if (!IsInitialized)
        {
            var caller = DebugUtility.GetCallingClassAndMethod(false, true, 1);
            throw new NotInitializedException(typeof(AddressablesAssetProvider),
                $"Please ensure Addressables to be initialized before using {caller}()");
        }
    }
}
