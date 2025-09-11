using System;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	/// <summary>
	/// Editor-only constraints for CanonicalAssetRef targets.
	/// Enforced by the CanonicalAssetRefDrawer.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public sealed class CanonicalAssetRefAttribute : PropertyAttribute
	{
		public Type[] RequiredTypes { get; }
		public Type[] RequiredBaseClasses { get; }

		public CanonicalAssetRefAttribute(Type[] _requiredTypes = null, Type[] _requiredBaseClasses = null)
		{
			RequiredTypes = _requiredTypes ?? Array.Empty<Type>();
			RequiredBaseClasses = _requiredBaseClasses ?? Array.Empty<Type>();
		}
	}
}
