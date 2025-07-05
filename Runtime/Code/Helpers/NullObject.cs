// Originally from https://stackoverflow.com/a/22261282/2328447
using System.ComponentModel;

namespace GuiToolkit
{
	public struct NullObject<T>
	{
		[DefaultValue(true)]
		private bool m_isNull;// default property initializers are not supported for structs

		private NullObject( T _item, bool _isNull ) : this()
		{
			m_isNull = _isNull;
			Item = _item;
		}

		public NullObject( T _item ) : this(_item, _item == null)
		{
		}

		public static NullObject<T> Null()
		{
			return new NullObject<T>();
		}

		public T Item { get; private set; }

		public bool IsNull => m_isNull;

		public static implicit operator T( NullObject<T> nullObject ) => nullObject.Item;

		public static implicit operator NullObject<T>( T item ) => new NullObject<T>(item);

		public override string ToString() => Item != null ? Item.ToString() : "<null>";

		public override bool Equals( object _other )
		{
			if (_other == null)
				return IsNull;

			if (!(_other is NullObject<T>))
				return false;

			var otherNullObject = (NullObject<T>)_other;

			if (IsNull)
				return otherNullObject.IsNull;

			if (otherNullObject.IsNull)
				return false;

			return Item.Equals(otherNullObject.Item);
		}

		public override int GetHashCode()
		{
			if (m_isNull)
				return 0;

			var result = Item.GetHashCode();

			if (result >= 0)
				result++;

			return result;
		}
	}
}