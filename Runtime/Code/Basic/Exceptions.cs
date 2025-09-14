using System;
using GuiToolkit.AssetHandling;

namespace GuiToolkit.Exceptions
{
	public sealed class NotInitializedException : InvalidOperationException
	{
		/// <summary>
		/// Not yet initialized
		/// </summary>
		public NotInitializedException(Type _type)
			: base($"A class instance of type '{_type.Name}' is not yet initialized. Please ensure call order")
		{ }
		public NotInitializedException(Type _type, string _msg)
			: base($"A class instance of type '{_type.Name}' is not yet initialized. Please ensure call order.\n{_msg}")
		{ }
	}

	public sealed class AssetLoadFailedException : InvalidOperationException
	{
		public AssetLoadFailedException(CanonicalAssetKey _key)
			: base($"Asset load failed for '{_key.ToString()}'")
		{ }
		
		public AssetLoadFailedException(CanonicalAssetKey _key, string _msg)
			: base($"Asset load failed for '{_key.ToString()}'\n{_msg}")
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