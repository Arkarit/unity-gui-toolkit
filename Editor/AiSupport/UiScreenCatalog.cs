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

		/// <summary>
		/// Ready-made prefab building blocks (StandardButton, StandardCheckbox, panel backgrounds, ...).
		/// The toolkit's widgets are not self-contained — a "button" is a hand-built prefab (background,
		/// label, animation). The baker composes screens from these templates; an author references one
		/// by <see cref="UiPaletteEntry.name"/> in a screen node's <c>"template"</c> field.
		/// </summary>
		public List<UiPaletteEntry> palette = new();

		/// <summary>
		/// Standard-element keys claimed by more than one prefab of the winning rank. The generator picks
		/// the alphabetically first candidate and logs an error, but the Unity console is not reachable
		/// over MCP — so the collisions are persisted here and surfaced by <c>setup_status</c>. Silently
		/// resolving to the wrong prefab is otherwise invisible until someone diffs the registry by hand.
		/// </summary>
		public List<UiCatalogStandardElementAmbiguity> standardElementAmbiguities = new();
	}

	[Serializable]
	public class UiCatalogStandardElementAmbiguity
	{
		/// <summary>The contested standard-element key (EStandardElement name or custom id).</summary>
		public string key = "";

		/// <summary>Asset paths of all candidates of the winning rank, in the generator's tie-break order.</summary>
		public List<string> candidates = new();

		/// <summary>The candidate that won — <c>candidates[0]</c>, i.e. alphabetically first.</summary>
		public string winner = "";

		/// <summary>True when the contest was between client prefabs (rather than library defaults).</summary>
		public bool client;
	}

	[Serializable]
	public class UiPaletteEntry
	{
		/// <summary>Authoring key — the value that goes into a screen node's "template".</summary>
		public string name = "";

		/// <summary>Project-relative asset path of the source prefab.</summary>
		public string prefabPath = "";

		/// <summary>Stable asset GUID (survives moves/renames; the baker resolves the prefab by this).</summary>
		public string prefabGuid = "";

		/// <summary>Short name of the primary Ui* component on the prefab root (e.g. "UiButton"); may be empty.</summary>
		public string kind = "";

		/// <summary>Heuristic category (Button/Toggle/Slider/Panel/Text/Container/...).</summary>
		public string category = "";

		/// <summary>Instance/"flavor" description harvested from a UiComment on the prefab root; may be empty.</summary>
		public string description = "";

		/// <summary>Standard-element identity from a root UiStandardElement marker (enum name or Custom id); "" if untagged.</summary>
		public string standardElement = "";

		/// <summary>
		/// True for an internal sub-part of a composed element (a marker with <c>IsInternal</c>): bakeable, but
		/// not a building block a screen author composes — it only makes sense inside its parent. Listed rather
		/// than hidden so an author can tell "not meant for me" apart from "does not exist".
		/// </summary>
		public bool isInternal;

		/// <summary>Authorable slots this template exposes (text, style, onClick, icon, ...).</summary>
		public List<UiPaletteSlot> slots = new();

		/// <summary>
		/// The addressable internals of this element: every child path an <c>"overrides"</c> entry may be
		/// keyed by, in hierarchy order.
		/// </summary>
		/// <remarks>
		/// Without this, "what is this element made of, and which parts can I adjust?" has no answer in the
		/// vocabulary an author already reads — <c>slots</c> describes only the node itself, and read_screen
		/// stops at a template's boundary by design. An author who cannot see the parts rebuilds the element
		/// by hand instead of varianting it, and a hand-built copy carries literal values instead of styles,
		/// so it drops out of the skin. Listing the parts is what makes overriding the cheaper option.
		/// </remarks>
		public List<UiPalettePart> parts = new();

		/// <summary>
		/// True when <see cref="parts"/> was cut at the cap, so an author knows the list is not exhaustive
		/// and can fall back to capture_prefab_values on the prefab.
		/// </summary>
		public bool partsTruncated;
	}

	/// <summary>
	/// One addressable internal part of a palette element.
	/// </summary>
	/// <remarks>
	/// <see cref="path"/> is not a description of the hierarchy — it is the literal key an <c>"overrides"</c>
	/// entry uses, and the generator only emits paths it has verified resolve. That is the whole value of the
	/// list: an author can copy a path straight into a screen instead of guessing at one.
	/// </remarks>
	[Serializable]
	public class UiPalettePart
	{
		/// <summary>Child transform path relative to the element root, e.g. "Header/Title".</summary>
		public string path = "";

		/// <summary>Short name of the most telling component on that part (e.g. "Image", "UiButton").</summary>
		public string type = "";

		/// <summary>
		/// Set when this part is itself a tagged standard element — its palette entry, where its own parts
		/// are listed. Empty otherwise.
		/// </summary>
		/// <remarks>
		/// The reason the list stops here rather than descending: a composed dialog would otherwise repeat
		/// every internal of every element it contains, at paths so long they obscure the structure the
		/// author is actually composing with. Naming the element instead is both shorter and more useful —
		/// it says "look this one up" and the entry is right there in the same palette.
		/// </remarks>
		public string element = "";

		/// <summary>True when the part carries a text component, so an override here may set "text".</summary>
		public bool text;

		/// <summary>
		/// True when the element ships this part switched OFF — an override with <c>"active": true</c> turns
		/// it on for one instance. Worth naming: a part that is invisible by default is invisible in a
		/// screenshot too, so nothing else would reveal that it is there at all.
		/// </summary>
		public bool shipsInactive;
	}

	[Serializable]
	public class UiPaletteSlot
	{
		/// <summary>Slot key used in a screen node (e.g. "text", "style", "onClick").</summary>
		public string name = "";

		/// <summary>Slot kind: text, loca, style, event, sprite.</summary>
		public string kind = "";

		/// <summary>Optional hint about what this slot controls.</summary>
		public string note = "";
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

		/// <summary>
		/// For raw UGUI/Unity building blocks pulled in via the allow-list (Image, ScrollRect, CanvasGroup, ...):
		/// the underlying Unity type's full name. Empty for the toolkit's own <c>Ui*</c> components.
		/// </summary>
		public string unityType = "";

		/// <summary>
		/// Optional steering hint: the name of a toolkit wrapper the author should usually prefer over this
		/// raw type (e.g. <c>ScrollRect</c> → <c>UiScrollRect</c>). Advisory only — the raw type stays fully
		/// authorable. Empty when there is no wrapper.
		/// </summary>
		public string prefer = "";

		/// <summary>Heuristic category (Root/Container/Layout/Input/Text/Graphic/Modifier/Loca/Widget/...).</summary>
		public string category = "";

		/// <summary>Human-readable description, harvested from the class /// &lt;summary&gt; doc comment; may be empty.</summary>
		public string description = "";

		/// <summary>True if this component can be the top-level node of a screen (a UiView).</summary>
		public bool isRoot;

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

		/// <summary>Actual serialized field / property name the baker writes to.</summary>
		public string field = "";

		/// <summary>
		/// How the baker writes this prop: "field" (a serialized field, the default and overwhelming majority)
		/// or "property" (a C# property setter — used for native Unity components like CanvasGroup that have no
		/// reflectable serialized fields, only properties).
		/// </summary>
		public string member = "field";

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
