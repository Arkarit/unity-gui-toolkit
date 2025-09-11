using System;

namespace GuiToolkit.Exceptions
{
	public sealed class NotInitializedException : Exception
	{
		/// <summary>
		/// Not yet initialized
		/// </summary>
		public NotInitializedException(Type _type)
			: base($"The class instance '{_type.Name}' is not yet initialized. Please ensure call order or postpone")
		{ }
	}

	/// <summary>
	/// Exception thrown when Roslyn-based parsing or rewriting is not available
	/// for the current Unity version or environment.
	/// </summary>
	public sealed class RoslynUnavailableException : NotSupportedException
	{
		/// <summary>
		/// Creates a new instance that explains how to enable Roslyn support.
		/// </summary>
		public RoslynUnavailableException()
			: base($"Roslyn-based parsing is not available in this Unity version.\n" +
				   $"Install Roslyn via menu '{StringConstants.ROSLYN_INSTALL_HACK}' " +
					"or run this on Unity 6+ where Roslyn in package is supported.")
		{ }
	}

}