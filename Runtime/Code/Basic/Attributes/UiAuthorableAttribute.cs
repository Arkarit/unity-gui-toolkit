using System;

namespace GuiToolkit
{
	/// <summary>
	/// Optional marker used by the AI screen-authoring catalog generator.
	///
	/// The catalog generator works opt-out: every non-abstract <c>Ui*</c> component in the
	/// runtime assembly is included automatically. This attribute is therefore NOT required.
	/// Add it only to <b>enrich</b> a component's catalog entry (give it an explicit category
	/// or a human-readable description) or to <b>force-include</b> a component that the default
	/// name/denylist heuristics would otherwise skip.
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

		/// <summary>
		/// Human-readable description surfaced to the authoring AI. Null/empty leaves it blank.
		/// </summary>
		public readonly string Description;

		public UiAuthorableAttribute( string _category = null, string _description = null )
		{
			Category = _category;
			Description = _description;
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
