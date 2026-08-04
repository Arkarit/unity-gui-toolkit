using System;

namespace GuiToolkit
{
	/// <summary>
	/// Optional marker used by the AI screen-authoring catalog generator.
	///
	/// The catalog generator works opt-out: every non-abstract <c>Ui*</c> component in the
	/// runtime assembly is included automatically. This attribute is therefore NOT required.
	/// Add it only to <b>enrich</b> a component's catalog entry (give it an explicit category)
	/// or to <b>force-include</b> a component that the default name/denylist heuristics would
	/// otherwise skip.
	///
	/// The catalog's human-readable description is NOT set here: it is harvested from the
	/// component's <c>/// &lt;summary&gt;</c> XML doc comment, so the same comment serves Doxygen,
	/// IntelliSense and the authoring AI (one source of truth). Document the class to describe it.
	///
	/// To force-<i>exclude</i> a component, use <see cref="UiNotAuthorableAttribute"/> instead.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class UiAuthorableAttribute : Attribute
	{
		/// <summary>
		/// Overrides the auto-detected category (e.g. "Widget", "Layout", "Input", "Text",
		/// "Graphic", "Container", "Root", "Modifier", "Loca"). Null keeps the auto-detected value.
		/// </summary>
		public readonly string Category;

		public UiAuthorableAttribute( string _category = null )
		{
			Category = _category;
		}
	}

	/// <summary>
	/// Force-excludes a component from the AI screen-authoring catalog, overriding the
	/// opt-out default inclusion. Use on infrastructure/helper components that should never
	/// appear as an authorable screen element.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class UiNotAuthorableAttribute : Attribute
	{
	}
}
