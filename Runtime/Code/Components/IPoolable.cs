namespace GuiToolkit
{
	/// <summary>
	/// Optional interface for objects that are managed by UiPool.
	/// Allows custom initialization and cleanup logic.
	/// </summary>
	public interface IPoolable
	{
		/// <summary>
		/// Called after the object has been retrieved from the pool.
		/// </summary>
		void OnPoolCreated();

		/// <summary>
		/// Called before the object is returned to the pool.
		/// </summary>
		void OnPoolReleased();
	}
}
