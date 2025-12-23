using System;

namespace GuiToolkit.Storage
{
	public sealed class StorageError
	{
		public Exception Exception { get; }

		public StorageError( Exception _exception )
		{
			Exception = _exception;
		}
	}
}