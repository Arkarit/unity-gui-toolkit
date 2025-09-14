using System;

namespace GuiToolkit.AssetHandling
{
    /// <summary>
    /// Canonical identifier for an asset across different providers.
    /// Immutable, comparable, and safe to use as dictionary key.
    /// </summary>
    public readonly struct CanonicalAssetKey : IEquatable<CanonicalAssetKey>
    {
        /// <summary>
        /// The asset provider that owns this key.
        /// May be null if not resolved yet.
        /// </summary>
        public readonly IAssetProvider Provider;

        /// <summary>
        /// The normalized identifier string used by the provider.
        /// Guaranteed to be non-null and stable.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// The expected Unity type (e.g. GameObject, ScriptableObject, Component).
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// Create a canonical asset key from provider, id, and type.
        /// If the provider is null, AssetManager is queried to find one.
        /// If id is null or empty, defaults to <c>_type.Name</c>.
        /// </summary>
        /// <param name="_provider">The asset provider, or null to auto-detect.</param>
        /// <param name="_id">Optional raw identifier string. If null/empty, type name is used.</param>
        /// <param name="_type">The Unity object type. Must not be null.</param>
        public CanonicalAssetKey(IAssetProvider _provider, string _id, Type _type)
        {
            var id = string.IsNullOrEmpty(_id) ? _type.Name : _id;
            if (_provider != null)
            {
                Provider = _provider;
                Id = id;
            }
            else
            {
                Provider = AssetManager.GetAssetProvider(id);
                Id = Provider != null ? Provider.NormalizeKey(id, _type).Id : id;
            }

            Type = _type;
        }

        /// <summary>
        /// Create a canonical asset key from provider and type.
        /// Identifier defaults to the type name.
        /// </summary>
        /// <param name="_provider">The asset provider, or null to auto-detect.</param>
        /// <param name="_type">The Unity object type. Must not be null.</param>
        public CanonicalAssetKey(IAssetProvider _provider, Type _type)
            : this(_provider, null, _type)
        {
        }

        /// <summary>
        /// Try to extract a substring value from the identifier,
        /// assuming it starts with a given prefix.
        /// </summary>
        /// <param name="_type">The prefix to check.</param>
        /// <param name="_val">Output: the remaining substring, or null if prefix not found.</param>
        /// <returns>True if prefix matched and substring was extracted, false otherwise.</returns>
        public bool TryGetValue(string _type, out string _val)
        {
            _val = null;
            if (Id.StartsWith(_type, StringComparison.Ordinal))
            {
                _val = Id.Substring(_type.Length);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Equals(CanonicalAssetKey _other) =>
               Provider == _other.Provider
            && string.Equals(Id, _other.Id, StringComparison.Ordinal)
            && Type == _other.Type;

        /// <inheritdoc/>
        public override bool Equals(object _obj) => _obj is CanonicalAssetKey other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Provider, Type, Id);

        /// <summary>
        /// Returns a human-readable string for debugging,
        /// including provider, id, and type.
        /// </summary>
        public override string ToString() =>
            $"Provider:'{Provider}' Id:'{Id}' Type:'{Type.Name}'";
    }
}
