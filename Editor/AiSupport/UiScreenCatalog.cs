using System;
using System.Collections.Generic;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Serializable data model for the AI screen-authoring catalog. This is the machine-readable
	/// "vocabulary" an external agent reads to know which toolkit components exist, what properties
	/// and styles they accept, and how they may be nested. It is emitted as JSON via
	/// <see cref="UiScreenCatalogGenerator"/> using <c>UnityEngine.JsonUtility</c>, so every type
	/// here must be a plain <c>[Serializable]</c> class with public fields only (no properties,
	/// no dictionaries).
	/// </summary>
	[Serializable]
	public class UiScreenCatalog
	{
		/// <summary>Schema version of this catalog format. Bump on breaking shape changes.</summary>
		public int version = 1;

		/// <summary>ISO-8601 UTC timestamp of generation (round-trip "o" format).</summary>
		public string generatedAtUtc = "";

		/// <summary>Assembly the catalogued components were reflected from.</summary>
		public string toolkitAssembly = "";

		/// <summary>All skin names discovered across the project's style configs.</summary>
		public List<string> skins = new();

		/// <summary>
		/// Style names grouped by the component type they target (Image, TMP_Text, ...).
		/// Styles are keyed by the underlying Unity component, not by the Ui* wrapper, so this
		/// top-level map is where the full style vocabulary lives.
		/// </summary>
		public List<UiCatalogStyleGroup> styleGroups = new();

		/// <summary>All authorable components.</summary>
		public List<UiCatalogComponent> components = new();
	}

	[Serializable]
	public class UiCatalogStyleGroup
	{
		/// <summary>Short type name of the styled component (e.g. "Image", "TMP_Text").</summary>
		public string componentType = "";

		/// <summary>Available style names for that component type.</summary>
		public List<string> styleNames = new();
	}

	[Serializable]
	public class UiCatalogComponent
	{
		/// <summary>Short class name — the value that goes into a screen JSON node's "type".</summary>
		public string type = "";

		/// <summary>Namespace-qualified type name.</summary>
		public string fullName = "";

		/// <summary>Assembly the component is declared in — lets the agent tell toolkit from client types.</summary>
		public string assembly = "";

		/// <summary>Heuristic category (Root/Container/Layout/Input/Text/Graphic/Modifier/Loca/Widget/...).</summary>
		public string category = "";

		/// <summary>Human-readable description (from [UiAuthorable]); may be empty.</summary>
		public string description = "";

		/// <summary>True if this component can be the top-level node of a screen (a UiView).</summary>
		public bool isRoot;

		/// <summary>True if this component may contain child element nodes.</summary>
		public bool acceptsChildren;

		/// <summary>
		/// When known, the serialized field / transform under which children are placed
		/// (best-effort heuristic; empty means "the component's own transform").
		/// </summary>
		public string contentField = "";

		/// <summary>Components implicitly added via [RequireComponent].</summary>
		public List<string> requiresComponents = new();

		/// <summary>Style names that directly target this component type (usually empty for Ui* wrappers).</summary>
		public List<string> styles = new();

		/// <summary>Authorable serialized properties.</summary>
		public List<UiCatalogProp> props = new();

		/// <summary>Serialized event fields (CEvent / UnityEvent). Listed for later logic binding.</summary>
		public List<UiCatalogEvent> events = new();
	}

	[Serializable]
	public class UiCatalogProp
	{
		/// <summary>Authoring name (serialized field with a leading "m_" stripped).</summary>
		public string name = "";

		/// <summary>Actual serialized field name the baker writes to.</summary>
		public string field = "";

		/// <summary>
		/// Value kind: string, bool, int, float, enum, color, vector2, vector3, vector4,
		/// sprite, componentRef, objectRef, list, struct, unknown.
		/// </summary>
		public string kind = "";

		/// <summary>Full type name of the underlying value.</summary>
		public string valueType = "";

		/// <summary>For kind == "list": the kind of each element.</summary>
		public string elementKind = "";

		/// <summary>For componentRef/objectRef (and list thereof): the referenced type's short name.</summary>
		public string refType = "";

		/// <summary>For kind == "enum": the allowed value names.</summary>
		public List<string> enumValues = new();

		public bool optional;
		public bool mandatory;
		public bool mandatoryExternal;

		public bool hasRange;
		public float rangeMin;
		public float rangeMax;

		/// <summary>Tooltip text, if the field carries a [Tooltip] attribute.</summary>
		public string tooltip = "";
	}

	[Serializable]
	public class UiCatalogEvent
	{
		public string name = "";
		public string field = "";
		public string type = "";
	}
}
