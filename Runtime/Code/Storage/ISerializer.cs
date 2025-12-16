namespace GuiToolkit.Storage
{
	public interface ISerializer
	{
		byte[] Serialize<T>( T _value );
		T Deserialize<T>( byte[] _data );
	}
}
