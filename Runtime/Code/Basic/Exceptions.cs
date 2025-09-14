using System;
using GuiToolkit.AssetHandling;

namespace GuiToolkit.Exceptions
{
    /// <summary>
    /// Exception thrown when a class or subsystem is used before it has been initialized.
    /// Typically indicates a missing call to an initialization routine in the correct order.
    /// </summary>
    public sealed class NotInitializedException : InvalidOperationException
    {
        /// <summary>
        /// Creates a new exception indicating that the given type has not yet been initialized.
        /// </summary>
        /// <param name="_type">The type that was not initialized.</param>
        public NotInitializedException(Type _type)
            : base($"A class instance of type '{_type.Name}' is not yet initialized. Please ensure call order")
        { }

        /// <summary>
        /// Creates a new exception indicating that the given type has not yet been initialized,
        /// including a custom message with additional context.
        /// </summary>
        /// <param name="_type">The type that was not initialized.</param>
        /// <param name="_msg">Additional context or details.</param>
        public NotInitializedException(Type _type, string _msg)
            : base($"A class instance of type '{_type.Name}' is not yet initialized. Please ensure call order.\n{_msg}")
        { }
    }

    /// <summary>
    /// Exception thrown when loading an asset fails.
    /// Typically indicates an invalid key, missing asset, or provider failure.
    /// </summary>
    public sealed class AssetLoadFailedException : InvalidOperationException
    {
        /// <summary>
        /// Creates a new exception for a failed asset load with only the key information.
        /// </summary>
        /// <param name="_key">The canonical asset key of the failed asset.</param>
        public AssetLoadFailedException(CanonicalAssetKey _key)
            : base($"Asset load failed for '{_key.ToString()}'")
        { }

        /// <summary>
        /// Creates a new exception for a failed asset load, including a custom message.
        /// </summary>
        /// <param name="_key">The canonical asset key of the failed asset.</param>
        /// <param name="_msg">Additional context or details.</param>
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
