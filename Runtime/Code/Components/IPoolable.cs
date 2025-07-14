namespace GuiToolkit
{
	public interface IPoolable
	{
		void OnPoolCreated();
		void OnPoolReleased();
	}
}
