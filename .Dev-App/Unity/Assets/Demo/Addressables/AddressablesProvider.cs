using GuiToolkit.AssetHandling;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

public sealed class AddressablesProvider : IAssetProvider
{
    public async Task<IInstanceHandle> InstantiateAsync(object key, Transform parent = null, CancellationToken ct = default)
    {
        UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> h;
        if (key is UnityEngine.AddressableAssets.AssetReferenceGameObject ar)
            h = ar.InstantiateAsync(parent);
        else
            h = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(key, parent);

        await h.Task;
        return new AddrInstanceHandle(h);
    }

    public async Task<IAssetHandle<GameObject>> LoadPrefabAsync(object key, CancellationToken ct = default)
    {
        var h = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(key);
        await h.Task;
        return new AddrAssetHandle(h, key);
    }

    public void Release(IAssetHandle<GameObject> handle)
    {
        var a = (AddrAssetHandle)handle;
        UnityEngine.AddressableAssets.Addressables.Release(a.Handle);
    }

    public async Task PreloadAsync(IEnumerable<object> keysOrLabels, CancellationToken ct = default)
    {
        foreach (var k in keysOrLabels) { var h = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(k); await h.Task; UnityEngine.AddressableAssets.Addressables.Release(h); }
    }

    public void ReleaseUnused() { /* optional trimming */ }

    private sealed class AddrInstanceHandle : IInstanceHandle
    {
        public GameObject Instance { get; }
        private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> _h;
        public AddrInstanceHandle(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> h) { _h = h; Instance = h.Result; }
        public void Release() { UnityEngine.AddressableAssets.Addressables.ReleaseInstance(Instance); }
    }

    private sealed class AddrAssetHandle : IAssetHandle<GameObject>
    {
        public GameObject Asset => Handle.Result;
        public object Key { get; }
        public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> Handle;
        public AddrAssetHandle(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> handle, object key) { Handle = handle; Key = key; }
    }
}
