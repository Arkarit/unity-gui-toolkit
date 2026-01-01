using System;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Wraps an exception that occurred during a storage operation.
	/// </summary>
	/// <remarks>
	/// Used by callback-based APIs to report failures without throwing on the calling thread.
	/// </remarks>
	public sealed class StorageError
	{
		/// <summary>
		/// The captured exception.
		/// </summary>
		/// <returns>Exception instance.</returns>
		public Exception Exception { get; }

		/// <summary>
		/// Creates a new error wrapper.
		/// </summary>
		/// <param name="_exception">Exception to wrap.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if exception is null.</exception>
		public StorageError( Exception _exception )
		{
			Exception = _exception;
		}
	}
}