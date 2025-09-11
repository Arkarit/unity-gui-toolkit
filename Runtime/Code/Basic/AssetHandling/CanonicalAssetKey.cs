using System;

namespace GuiToolkit.AssetHandling
{
	/// <summary>
	/// Canonical identifier for an asset across different providers.
	/// Immutable, comparable, safe to use as dictionary key.
	/// </summary>
	public readonly struct CanonicalAssetKey : IEquatable<CanonicalAssetKey>
	{
		public readonly IAssetProvider Provider;
		public readonly string Id; // normalized identifier string
		public readonly Type Type;

		public CanonicalAssetKey( IAssetProvider _provider, string _id, Type _type )
		{
			Provider = _provider;
			Id = _id ?? string.Empty;
			Type = _type;
		}

		public bool TryGetValue(string _type, out string _val )
		{
			_val = null;
			if (Id.StartsWith(_type, StringComparison.Ordinal))
			{
				_val = Id.Substring(_type.Length);
				return true;
			}
			
			return false;
		}
		
		public bool Equals( CanonicalAssetKey _other ) =>
			   Provider == _other.Provider 
			&& string.Equals(Id, _other.Id, StringComparison.Ordinal)
			&& Type == _other.Type;

		public override bool Equals( object _obj ) => _obj is CanonicalAssetKey other && Equals(other);

		public override int GetHashCode() => HashCode.Combine(Provider, Type, Id);

		public override string ToString() => $"{Provider}:{Id}";
	}
}
