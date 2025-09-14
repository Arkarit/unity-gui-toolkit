using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	/// <summary>
	/// Handle for an instantiated asset (GameObject).
	/// Allows access to the instance and defines how to release it.
	/// </summary>
	public interface IInstanceHandle
	{
		/// <summary>
		/// The instantiated GameObject.
		/// </summary>
		GameObject Instance { get; }

		/// <summary>
		/// Release this instance.
		/// <note>
		/// Addressables: calls <c>ReleaseInstance</c>.  
		/// Direct/Resources: destroys the GameObject.
		/// </note>
		/// </summary>
		void Release();
	}

	/// <summary>
	/// Handle for a loaded asset (without instantiation).
	/// Provides access to the asset and diagnostics information.
	/// </summary>
	/// <typeparam name="T">The Unity Object type of the loaded asset.</typeparam>
	public interface IAssetHandle<T> where T : Object
	{
		/// <summary>
		/// The loaded asset reference.
		/// </summary>
		T Asset { get; }

		/// <summary>
		/// Canonical key of the loaded asset, useful for diagnostics or debugging.
		/// </summary>
		CanonicalAssetKey Key { get; }

		/// <summary>
		/// Indicates whether the asset is currently loaded and valid.
		/// </summary>
		bool IsLoaded { get; }

		/// <summary>
		/// Release this asset handle.
		/// <note>
		/// Addressables: calls <c>Release</c> on the handle.  
		/// Direct/Resources: unloads or does nothing, depending on provider.
		/// </note>
		/// </summary>
		void Release();
	}
	/// <summary>
	/// Editor-only bridge for asset providers.
	/// Provides helper functionality that is only required in the Unity Editor,
	/// e.g. creating identifiers for assets from Object references.
	/// </summary>
	public interface IAssetProviderEditorBridge
	{
		/// <summary>
		/// Try to generate a resource identifier string for a given Unity Object.
		/// Returns true if successful.
		/// </summary>
		/// <param name="_obj">The Unity Object to identify.</param>
		/// <param name="_resId">The resulting identifier string.</param>
		/// <returns>True if an identifier could be created, otherwise false.</returns>
		bool TryMakeId( Object _obj, out string _resId );
	}

	/// <summary>
	/// Base interface for asset providers. 
	/// An asset provider defines how assets are located, loaded, instantiated,
	/// and released. Examples include a Resources-based provider (part of the library itself)
	/// or an Addressables provider (in the demo-app, since the library is Addressables-agnostic)
	/// </summary>
	public interface IAssetProvider
	{
		/// <summary>
		/// Human-readable name of the provider, e.g. "Default Asset Provider".
		/// Useful for debugging or Editor GUI.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Human-readable name of the loading backend, e.g. "Resources" or "Addressables".
		/// </summary>
		string ResName { get; }

		/// <summary>
		/// Loading Prefix ending with : , e.g. "res:"
		/// </summary>
		string Prefix { get; }

		/// <summary>
		/// Optional editor-only bridge for editor-specific functionality.
		/// Can be null in runtime contexts.
		/// </summary>
		IAssetProviderEditorBridge EditorBridge { get; }

		/// <summary>
		/// Indicates whether the provider has been initialized successfully.
		/// </summary>
		/// <note>
		/// Since Init() is allowed to work async, it might take some time after Init() until it's actually initialized.
		/// </note>
		bool IsInitialized { get; }

		/// <summary>
		/// Initialize the provider. 
		/// Some providers may require explicit initialization (e.g. Addressables).
		/// Others may leave this method empty.
		/// </summary>
		/// <note>
		/// Since Init() is allowed to work async, it might take some time after Init() until it's actually initialized.
		/// </note>
		void Init();

		/// <summary>
		/// Load an asset of type T without instantiating it.
		/// Returns a handle that can later be released. 
		/// May be a no-op for providers that only support instantiation.
		/// </summary>
		/// <typeparam name="T">The Unity Object type to load.</typeparam>
		/// <param name="_key">The asset key (string, CanonicalAssetKey, etc.).</param>
		/// <param name="_cancellationToken">Optional cancellation token.</param>
		/// <returns>A handle to the loaded asset.</returns>
		Task<IAssetHandle<T>> LoadAssetAsync<T>(
			object _key,
			CancellationToken _cancellationToken = default
		) where T : Object;

		/// <summary>
		/// Instantiate an asset and return a handle that knows how to free itself.
		/// </summary>
		/// <param name="_key">The asset key (string, CanonicalAssetKey, etc.).</param>
		/// <param name="_parent">Optional parent transform for the instance.</param>
		/// <param name="_cancellationToken">Optional cancellation token.</param>
		/// <returns>A handle to the instantiated object.</returns>
		Task<IInstanceHandle> InstantiateAsync(
			object _key,
			Transform _parent = null,
			CancellationToken _cancellationToken = default
		);

		/// <summary>
		/// Release a previously loaded asset handle (not an instance).
		/// </summary>
		/// <typeparam name="T">The Unity Object type.</typeparam>
		/// <param name="_handle">The handle to release.</param>
		void Release<T>( IAssetHandle<T> _handle ) where T : Object;

		/// <summary>
		/// Release a previously instantiated instance handle.
		/// </summary>
		/// <param name="_handle">The instance handle to release.</param>
		void Release( IInstanceHandle _handle );

		/// <summary>
		/// Housekeeping hook. 
		/// Called to allow the provider to release unused assets or cleanup caches.
		/// </summary>
		void ReleaseUnused();

		/// <summary>
		/// Normalize a raw key into a canonical asset key with type information.
		/// </summary>
		/// <typeparam name="T">The expected Unity Object type.</typeparam>
		/// <param name="_key">The raw key object.</param>
		/// <returns>A canonical asset key.</returns>
		CanonicalAssetKey NormalizeKey<T>( object _key ) where T : Object;

		/// <summary>
		/// Normalize a raw key into a canonical asset key with explicit type information.
		/// </summary>
		/// <param name="_key">The raw key object.</param>
		/// <param name="_type">The expected Unity Object type.</param>
		/// <returns>A canonical asset key.</returns>
		CanonicalAssetKey NormalizeKey( object _key, Type _type );

		/// <summary>
		/// Determine if this provider supports the given canonical key.
		/// </summary>
		/// <param name="_key">The canonical asset key to check.</param>
		/// <returns>True if supported, otherwise false.</returns>
		bool Supports( CanonicalAssetKey _key );

		/// <summary>
		/// Determine if this provider supports the given identifier string.
		/// For example, "res:MyPrefab" or "addr:SomeAddress".
		/// </summary>
		/// <param name="_id">The identifier string.</param>
		/// <returns>True if supported, otherwise false.</returns>
		bool Supports( string _id );

		/// <summary>
		/// Determine if this provider supports the given raw object key.
		/// For example, a string, a CanonicalAssetKey, or another object reference.
		/// </summary>
		/// <param name="_obj">The raw object key to check.</param>
		/// <returns>True if supported, otherwise false.</returns>
		bool Supports( object _obj );
	}
}
