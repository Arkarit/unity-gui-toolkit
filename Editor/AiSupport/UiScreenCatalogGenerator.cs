using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UGUI = UnityEngine.UI;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Milestone 1 of the AI screen-authoring effort: reflects the toolkit's <c>Ui*</c> component
	/// family into a machine-readable JSON catalog (components, authorable props, available styles,
	/// nesting rules). An external agent reads this "vocabulary" to author screen descriptions.
	///
	/// Opt-out inclusion: every non-abstract component in the runtime assembly whose name starts
	/// with "Ui" is catalogued automatically. Use <see cref="UiNotAuthorableAttribute"/> to exclude
	/// a component and <see cref="UiAuthorableAttribute"/> to enrich/force-include one.
	///
	/// Reflection only — no components are instantiated, so [ExecuteAlways] side effects never run.
	/// </summary>
	[EditorAware]
	public static class UiScreenCatalogGenerator
	{
		private const int CatalogVersion = 1;
		private const string OutputFileName = "screen-catalog.json";

		// Written into the currently-open (client) project. Assets/ always maps to that project.
		private const string OutputDir = "Assets/AiSupport";

		/// <summary>Project-relative path of the generated catalog file.</summary>
		public static string CatalogPath => $"{OutputDir}/{OutputFileName}";

		// The toolkit's own assembly (where UiThing lives); the base of the "authorable" universe.
		private static Assembly s_toolkitAssembly;

		// Force-excluded infrastructure/helper components (name-exact or by prefix below).
		private static readonly HashSet<string> s_denyExactNames = new()
		{
			"UiThing",   // concrete but a base class, not a usable widget
			"UiMain",
			"UiCanvasScalerReference",
		};

		// Force-excluded by name prefix (pooling internals, style appliers/definitions).
		private static readonly string[] s_denyPrefixes =
		{
			"UiPool",
			"UiApplyStyle",
			"UiStyle",
		};

		// Client (non-toolkit) types with these name prefixes are demo/sample/test content, not
		// production screen elements. Toolkit types can't match (they must start with "Ui").
		// [UiAuthorable] overrides this (checked earlier in IsAuthorable).
		private static readonly string[] s_denyClientNamePrefixes =
		{
			"Demo",
			"Example",
			"Sample",
			"Test",
		};

		// Assembly name segments that mark a test/playmode/editmode assembly (never authorable).
		private static readonly string[] s_testAssemblySegments =
		{
			"Test",
			"Tests",
			"PlayMode",
			"EditMode",
		};

		// Loaded once per Generate() run so style lookups don't re-scan the AssetDatabase per component.
		private static List<UiStyleConfig> s_styleConfigCache;

		// FullName -> class /// <summary> text, harvested once per Generate() run (see BuildDocSummaryMap).
		private static Dictionary<string, string> s_docSummaries;

		[MenuItem(StringConstants.AI_GENERATE_SCREEN_CATALOG_MENU_NAME)]
		public static void GenerateMenu()
		{
			var path = Generate();
			if (!string.IsNullOrEmpty(path))
				UiLog.LogInternal($"AI screen catalog written to '{path}'.");
		}

		/// <summary>
		/// Builds the catalog and writes it to disk. Returns the output path, or null on failure.
		/// </summary>
		public static string Generate()
		{
			try
			{
				var catalog = BuildCatalog();

				EditorFileUtility.EnsureUnityFolderExists(OutputDir);
				string path = CatalogPath;

				string json = JsonUtility.ToJson(catalog, true);
				File.WriteAllText(path, json);
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

				UiLog.LogInternal($"AI screen catalog: {catalog.components.Count} components, " +
				                  $"{catalog.styleGroups.Count} styled types, {catalog.skins.Count} skins.");
				return path;
			}
			catch (Exception e)
			{
				UiLog.LogError($"Could not generate AI screen catalog: {e.Message}\n{e.StackTrace}");
				return null;
			}
		}

		#region Build

		private static UiScreenCatalog BuildCatalog()
		{
			s_toolkitAssembly = typeof(UiThing).Assembly;
			string toolkitName = s_toolkitAssembly.GetName().Name;

			var catalog = new UiScreenCatalog
			{
				version = CatalogVersion,
				generatedAtUtc = DateTime.UtcNow.ToString("o"),
				toolkitAssembly = toolkitName,
			};

			s_styleConfigCache = LoadAllStyleConfigs();
			try
			{
				CollectStyles(catalog);

				// Scan the toolkit assembly plus every assembly that references it (client asmdefs,
				// Assembly-CSharp), minus test/playmode assemblies. Client types are only kept if
				// they subclass a toolkit component.
				var assemblies = AppDomain.CurrentDomain.GetAssemblies()
					.Where(ReferencesToolkit)
					.Where(a => !IsTestAssembly(a));

				var types = assemblies
					.SelectMany(SafeGetTypes)
					.Where(IsAuthorable)
					.OrderBy(t => t.FullName, StringComparer.Ordinal)
					.ToList();

				// Harvest class descriptions from /// <summary> doc comments (single source of truth,
				// shared with Doxygen/IntelliSense). Restricted to the types we actually catalogue.
				s_docSummaries = BuildDocSummaryMap(new HashSet<string>(types.Select(t => t.FullName)));

				foreach (var type in types)
					catalog.components.Add(BuildComponent(type));

				// Raw UGUI/Unity building blocks (Image, ScrollRect, CanvasGroup, ...) via the allow-list —
				// they don't start with "Ui" so the reflection scan above never sees them.
				CollectUnityTypes(catalog);

				catalog.components = catalog.components
					.OrderBy(c => c.category, StringComparer.Ordinal)
					.ThenBy(c => c.type, StringComparer.Ordinal)
					.ToList();

				WarnMissingDescriptions(catalog);

				// Standard elements first: the resolved registry winners are part of the authoring palette
				// (a registry key is always a valid "template"), so the palette needs them to be complete.
				var standardElements = CollectStandardElements(catalog);

				CollectPalette(catalog, standardElements);

				WarnMissingPaletteDescriptions(catalog);
			}
			finally
			{
				s_styleConfigCache = null;
				s_docSummaries = null;
			}

			return catalog;
		}

		private static bool ReferencesToolkit( Assembly _assembly )
		{
			try
			{
				if (_assembly == s_toolkitAssembly)
					return true;

				string toolkitName = s_toolkitAssembly.GetName().Name;
				return _assembly.GetReferencedAssemblies().Any(r => r.Name == toolkitName);
			}
			catch
			{
				return false;
			}
		}

		// The toolkit's own test assembly (…​.Test.PlayMode) references the toolkit and its test
		// components subclass real widgets, so they'd otherwise be catalogued. Match by dotted-name
		// segment so a legitimately-named client assembly isn't caught by a substring.
		private static bool IsTestAssembly( Assembly _assembly )
		{
			if (_assembly == s_toolkitAssembly)
				return false;

			string name;
			try { name = _assembly.GetName().Name; }
			catch { return false; }

			var segments = name.Split('.');
			return segments.Any(s => s_testAssemblySegments.Any(t => string.Equals(s, t, StringComparison.OrdinalIgnoreCase)));
		}

		private static IEnumerable<Type> SafeGetTypes( Assembly _assembly )
		{
			try
			{
				return _assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException e)
			{
				return e.Types.Where(t => t != null);
			}
			catch
			{
				return Array.Empty<Type>();
			}
		}

		private static bool IsAuthorable( Type _type )
		{
			if (_type.IsAbstract || _type.IsInterface || _type.IsGenericTypeDefinition)
				return false;
			if (!typeof(Component).IsAssignableFrom(_type))
				return false;

			if (_type.GetCustomAttribute<UiNotAuthorableAttribute>(false) != null)
				return false;
			if (_type.GetCustomAttribute<UiAuthorableAttribute>(false) != null)
				return true;

			// Toolkit-owned types are filtered by the naming/denylist heuristics (they carry the
			// base classes and infrastructure we don't want listed as usable widgets).
			if (_type.Assembly == s_toolkitAssembly)
				return IsAuthorableToolkitType(_type);

			// Client (or other referencing) types named like demo/sample/test content are not
			// production screen elements. [UiAuthorable] (checked above) overrides this.
			if (s_denyClientNamePrefixes.Any(p => _type.Name.StartsWith(p, StringComparison.Ordinal)))
				return false;

			// Client (or other referencing) types are authorable iff they derive from an
			// authorable toolkit component — the naming rules above are toolkit-only and must
			// NOT reject client subclasses like "SettingsScreen : UiView".
			return HasAuthorableToolkitAncestor(_type);
		}

		private static bool IsAuthorableToolkitType( Type _type )
		{
			if (!_type.Name.StartsWith("Ui", StringComparison.Ordinal))
				return false;
			// Concrete-but-base classes (e.g. UiButtonBase, UiProgressBarBase) are meant to be
			// subclassed, not placed directly. Add [UiAuthorable] to force-include if ever needed.
			if (_type.Name.EndsWith("Base", StringComparison.Ordinal))
				return false;
			if (s_denyExactNames.Contains(_type.Name))
				return false;
			if (s_denyPrefixes.Any(p => _type.Name.StartsWith(p, StringComparison.Ordinal)))
				return false;

			return true;
		}

		private static bool HasAuthorableToolkitAncestor( Type _type )
		{
			for (var baseType = _type.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
			{
				if (baseType.Assembly != s_toolkitAssembly)
					continue;

				// The nearest toolkit ancestor decides: deriving from infrastructure (pooling,
				// style appliers, UiMain) is not an authorable screen element. Deriving from a
				// real UI base (UiThing/UiView/UiButton/...) is — including UiThing itself.
				if (baseType.Name == "UiMain" || baseType.Name == "UiCanvasScalerReference")
					return false;
				if (s_denyPrefixes.Any(p => baseType.Name.StartsWith(p, StringComparison.Ordinal)))
					return false;

				return true;
			}

			return false;
		}

		private static UiCatalogComponent BuildComponent( Type _type )
		{
			var authorable = _type.GetCustomAttribute<UiAuthorableAttribute>(false);

			var component = new UiCatalogComponent
			{
				type = _type.Name,
				fullName = _type.FullName,
				assembly = _type.Assembly.GetName().Name,
				category = !string.IsNullOrEmpty(authorable?.Category) ? authorable.Category : ClassifyCategory(_type),
				description = s_docSummaries != null && s_docSummaries.TryGetValue(_type.FullName, out var summary) ? summary : "",
				isRoot = typeof(UiView).IsAssignableFrom(_type),
				requiresComponents = CollectRequiredComponents(_type),
				styles = SafeStyleNames(_type),
			};

			CollectFields(_type, component);
			ResolveContentField(_type, component);

			return component;
		}

		#region Doc-comment harvesting

		/// <summary>
		/// Builds a FullName → class-summary map for the given types by locating each type's source
		/// file via its <see cref="MonoScript"/> and extracting the <c>/// &lt;summary&gt;</c> block
		/// above the class declaration. Deliberately Roslyn-free (a plain text scan) so it runs on
		/// every Unity version, including old ones where the Roslyn bridge (Dll2022Hack) is absent.
		/// </summary>
		private static Dictionary<string, string> BuildDocSummaryMap( HashSet<string> _wantedFullNames )
		{
			var map = new Dictionary<string, string>();
			if (_wantedFullNames.Count == 0)
				return map;

			foreach (var guid in AssetDatabase.FindAssets("t:MonoScript"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				var type = script != null ? script.GetClass() : null;
				if (type?.FullName == null || map.ContainsKey(type.FullName) || !_wantedFullNames.Contains(type.FullName))
					continue;

				try
				{
					string summary = ExtractClassSummary(File.ReadAllText(path), type.Name);
					if (!string.IsNullOrEmpty(summary))
						map[type.FullName] = summary;
				}
				catch (Exception e)
				{
					UiLog.LogWarning($"AI catalog: could not read doc comment from '{path}': {e.Message}");
				}
			}

			return map;
		}

		/// <summary>
		/// Extracts the plain text of the <c>/// &lt;summary&gt;</c> doc comment immediately preceding
		/// a declaration of <paramref name="_className"/>. Skips attribute/blank lines between the
		/// comment and the class, and ignores comment lines that merely mention the name. Handles
		/// partial classes by taking the first declaration that actually carries a summary. Null if none.
		/// </summary>
		private static string ExtractClassSummary( string _source, string _className )
		{
			var lines = _source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
			var declRegex = new Regex($@"\b(class|struct)\s+{Regex.Escape(_className)}\b");

			for (int i = 0; i < lines.Length; i++)
			{
				string lead = lines[i].TrimStart();
				if (lead.StartsWith("//"))            // a comment merely mentioning the name — not a declaration
					continue;
				if (!declRegex.IsMatch(lines[i]))
					continue;

				// Walk upward past attributes/blank lines, then collect the contiguous /// block.
				int j = i - 1;
				while (j >= 0)
				{
					string t = lines[j].Trim();
					if (t.Length == 0 || t.StartsWith("["))
					{
						j--;
						continue;
					}
					break;
				}

				var doc = new List<string>();
				while (j >= 0 && lines[j].TrimStart().StartsWith("///"))
				{
					doc.Add(lines[j].TrimStart().Substring(3));
					j--;
				}
				if (doc.Count == 0)
					continue;                          // this declaration has no doc — try another (partial classes)
				doc.Reverse();

				string xml = string.Join("\n", doc);
				var m = Regex.Match(xml, @"<summary>(.*?)</summary>", RegexOptions.Singleline);
				string text = CleanDocText(m.Success ? m.Groups[1].Value : xml);
				if (!string.IsNullOrEmpty(text))
					return text;
			}

			return null;
		}

		/// <summary>Strips inner XML doc tags, unescapes entities, collapses whitespace to single spaces.</summary>
		private static string CleanDocText( string _raw )
		{
			if (string.IsNullOrEmpty(_raw))
				return "";
			// Keep the referenced short name from <see cref="X.Y"/>, then drop all remaining tags.
			string s = Regex.Replace(_raw, "<see\\s+cref=\"[^\"]*?([A-Za-z0-9_]+)\"\\s*/>", "$1");
			s = Regex.Replace(s, "<[^>]+>", " ");
			s = s.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
			s = Regex.Replace(s, "\\s+", " ").Trim();
			return s;
		}

		/// <summary>Logs which authorable components still lack a <c>/// &lt;summary&gt;</c>, to nudge documentation.</summary>
		private static void WarnMissingDescriptions( UiScreenCatalog _catalog )
		{
			// Raw UGUI/Unity types (unityType set) never have a toolkit /// <summary>, so don't nag about them.
			var toolkitComponents = _catalog.components.Where(c => string.IsNullOrEmpty(c.unityType)).ToList();
			var missing = toolkitComponents
				.Where(c => string.IsNullOrEmpty(c.description))
				.Select(c => c.type)
				.OrderBy(t => t, StringComparer.Ordinal)
				.ToList();

			if (missing.Count == 0)
				return;

			UiLog.LogWarning($"AI catalog: {missing.Count}/{toolkitComponents.Count} authorable components have no " +
			                 $"/// <summary> doc comment (no description for the authoring AI):\n  {string.Join(", ", missing)}");
		}

		/// <summary>Logs which palette prefabs still lack a root <see cref="UiComment"/> (their flavor description).</summary>
		private static void WarnMissingPaletteDescriptions( UiScreenCatalog _catalog )
		{
			var missing = _catalog.palette
				// Internal sub-parts are listed for completeness, not offered for composition — an author
				// never picks one, so a missing flavor description is not worth reporting.
				.Where(e => string.IsNullOrEmpty(e.description) && !e.isInternal)
				.Select(e => e.name)
				.OrderBy(n => n, StringComparer.Ordinal)
				.ToList();

			if (missing.Count == 0)
				return;

			UiLog.LogWarning($"AI catalog: {missing.Count}/{_catalog.palette.Count} palette prefabs have no root " +
			                 $"UiComment (no flavor description for the authoring AI):\n  {string.Join(", ", missing)}");
		}

		#endregion

		private static List<string> CollectRequiredComponents( Type _type )
		{
			var result = new List<string>();
			foreach (var req in _type.GetCustomAttributes<RequireComponent>(true))
			{
				AddTypeName(result, req.m_Type0);
				AddTypeName(result, req.m_Type1);
				AddTypeName(result, req.m_Type2);
			}
			return result;

			static void AddTypeName( List<string> _list, Type _t )
			{
				if (_t != null && !_list.Contains(_t.Name))
					_list.Add(_t.Name);
			}
		}

		private static void CollectFields( Type _type, UiCatalogComponent _component )
		{
			// Walk the whole hierarchy but only keep fields declared in the toolkit assembly or in
			// the component's own (client) assembly. Unity-internal serialized fields (Graphic,
			// BaseMeshEffect, ...) are skipped — those are covered by the styling system, not by
			// direct authoring.
			for (var t = _type; t != null && t != typeof(object); t = t.BaseType)
			{
				if (t.Assembly != s_toolkitAssembly && t.Assembly != _type.Assembly)
					continue;

				var declared = t.GetFields(BindingFlags.Instance | BindingFlags.Public |
				                           BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

				foreach (var field in declared)
				{
					if (!IsSerializedField(field))
						continue;
					if (field.GetCustomAttribute<HideInInspector>() != null)
						continue;

					if (IsEventField(field.FieldType))
					{
						_component.events.Add(new UiCatalogEvent
						{
							name = AuthoringName(field.Name),
							field = field.Name,
							type = field.FieldType.Name,
						});
						continue;
					}

					_component.props.Add(BuildProp(field));
				}
			}
		}

		private static UiCatalogProp BuildProp( FieldInfo _field )
		{
			var prop = new UiCatalogProp
			{
				name = AuthoringName(_field.Name),
				field = _field.Name,
				optional = _field.GetCustomAttribute<OptionalAttribute>() != null,
				mandatory = _field.GetCustomAttribute<MandatoryAttribute>() != null,
				mandatoryExternal = _field.GetCustomAttribute<MandatoryExternalAttribute>() != null,
				tooltip = _field.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "",
			};

			var range = _field.GetCustomAttribute<RangeAttribute>();
			if (range != null)
			{
				prop.hasRange = true;
				prop.rangeMin = range.min;
				prop.rangeMax = range.max;
			}

			ClassifyValue(_field.FieldType, prop);
			return prop;
		}

		#endregion

		#region Classification

		private static void ClassifyValue( Type _type, UiCatalogProp _prop )
		{
			_prop.valueType = _type.FullName;

			// Lists / arrays.
			Type elementType = GetEnumerableElementType(_type);
			if (elementType != null)
			{
				_prop.kind = "list";
				var elementProbe = new UiCatalogProp();
				ClassifyValue(elementType, elementProbe);
				_prop.elementKind = elementProbe.kind;
				_prop.refType = elementProbe.refType;
				if (elementProbe.enumValues.Count > 0)
					_prop.enumValues = elementProbe.enumValues;
				return;
			}

			if (_type == typeof(string)) { _prop.kind = "string"; return; }
			if (_type == typeof(bool)) { _prop.kind = "bool"; return; }

			if (_type.IsEnum)
			{
				_prop.kind = "enum";
				_prop.enumValues = Enum.GetNames(_type).ToList();
				return;
			}

			if (_type == typeof(float) || _type == typeof(double)) { _prop.kind = "float"; return; }
			if (IsIntegerType(_type)) { _prop.kind = "int"; return; }

			if (_type == typeof(Color) || _type == typeof(Color32)) { _prop.kind = "color"; return; }
			if (_type == typeof(Vector2) || _type == typeof(Vector2Int)) { _prop.kind = "vector2"; return; }
			if (_type == typeof(Vector3) || _type == typeof(Vector3Int)) { _prop.kind = "vector3"; return; }
			if (_type == typeof(Vector4)) { _prop.kind = "vector4"; return; }

			if (typeof(Sprite).IsAssignableFrom(_type)) { _prop.kind = "sprite"; return; }

			if (typeof(Component).IsAssignableFrom(_type))
			{
				_prop.kind = "componentRef";
				_prop.refType = _type.Name;
				return;
			}

			if (typeof(UnityEngine.Object).IsAssignableFrom(_type))
			{
				_prop.kind = "objectRef";
				_prop.refType = _type.Name;
				return;
			}

			if (_type.GetCustomAttribute<SerializableAttribute>() != null && !_type.IsPrimitive)
			{
				_prop.kind = "struct";
				return;
			}

			_prop.kind = "unknown";
		}

		private static string ClassifyCategory( Type _type )
		{
			string name = _type.Name;

			if (typeof(UiView).IsAssignableFrom(_type))
				return "Root";
			if (typeof(UGUI.LayoutGroup).IsAssignableFrom(_type)
			    || name.Contains("LayoutGroup") || name.Contains("LayoutElement"))
				return "Layout";
			if (typeof(UGUI.BaseMeshEffect).IsAssignableFrom(_type))
				return "Modifier";
			if (typeof(UGUI.Graphic).IsAssignableFrom(_type))
				return "Graphic";

			if (ContainsAny(name, "Button", "Toggle", "Slider", "Dropdown", "Tab",
				    "Picker", "Radio", "InputField", "Select"))
				return "Input";
			if (ContainsAny(name, "Text", "Label"))
				return "Text";
			if (ContainsAny(name, "Image", "Sprite", "Icon", "Circle", "Star", "Shape"))
				return "Graphic";
			if (ContainsAny(name, "Panel", "View", "Dialog", "Requester", "Popup", "Container", "Modal"))
				return "Container";
			if (name.Contains("Animation"))
				return "Animation";
			if (ContainsAny(name, "Loca", "Localize", "Localized", "Language"))
				return "Loca";

			return "Widget";
		}

		// Best-effort detection of an explicit content-container field (the transform under which
		// children are placed). NOTE: a boolean "acceptsChildren" was intentionally dropped — in UGUI
		// anything can parent anything, so it added no information the category+description don't already
		// convey, and a hard "false" was simply wrong. contentField is a different, non-redundant hint
		// (WHERE children go) kept for a future baker use.
		private static void ResolveContentField( Type _type, UiCatalogComponent _component )
		{
			foreach (var prop in _component.props)
			{
				bool looksLikeContainer =
					(prop.kind == "componentRef" || prop.kind == "objectRef")
					&& (prop.refType == "RectTransform" || prop.refType == "Transform" || prop.refType == "GameObject")
					&& (prop.name.IndexOf("content", StringComparison.OrdinalIgnoreCase) >= 0
					    || prop.name.IndexOf("container", StringComparison.OrdinalIgnoreCase) >= 0);

				if (looksLikeContainer)
				{
					_component.contentField = prop.field;
					break;
				}
			}
		}

		#endregion

		#region UnityTypes

		/// <summary>
		/// Adds the raw UGUI/Unity building blocks from the allow-list (<see cref="UiAuthorableUnityTypesConfig"/>)
		/// as authorable components. These don't start with "Ui" so the reflection scan skips them, yet the baker
		/// can build them (element type → AddComponent). Each carries a <c>unityType</c> and an optional
		/// <c>prefer</c> wrapper hint.
		/// </summary>
		private static void CollectUnityTypes( UiScreenCatalog _catalog )
		{
			var seen = new HashSet<string>(_catalog.components.Select(c => c.fullName), StringComparer.Ordinal);

			foreach (var entry in UiAuthorableUnityTypesConfig.EffectiveEntries())
			{
				if (entry == null || entry.hidden || string.IsNullOrEmpty(entry.unityType))
					continue;

				var type = ResolveUnityType(entry.unityType);
				if (type == null)
				{
					UiLog.LogWarning($"AI catalog: authorable Unity type '{entry.unityType}' could not be resolved; skipped.");
					continue;
				}
				if (!typeof(Component).IsAssignableFrom(type))
				{
					UiLog.LogWarning($"AI catalog: authorable Unity type '{entry.unityType}' ({type.FullName}) is not a Component; skipped.");
					continue;
				}
				if (!seen.Add(type.FullName))
					continue; // already catalogued — don't duplicate

				_catalog.components.Add(BuildUnityTypeComponent(type, entry));
			}
		}

		// Resolves a config entry's type name — full name preferred (e.g. "UnityEngine.UI.Image"),
		// with a short-name fallback ("Image") for convenience.
		private static Type ResolveUnityType( string _name )
		{
			if (string.IsNullOrEmpty(_name))
				return null;

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				Type t;
				try { t = asm.GetType(_name, false); }
				catch { t = null; }
				if (t != null)
					return t;
			}

			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
				foreach (var t in SafeGetTypes(asm))
					if (t.Name == _name)
						return t;

			return null;
		}

		private static UiCatalogComponent BuildUnityTypeComponent( Type _type, UiAuthorableUnityTypesConfig.Entry _entry )
		{
			var component = new UiCatalogComponent
			{
				type = _type.Name,
				fullName = _type.FullName,
				assembly = _type.Assembly.GetName().Name,
				category = !string.IsNullOrEmpty(_entry.category) ? _entry.category : ClassifyCategory(_type),
				unityType = _type.FullName,
				prefer = _entry.prefer ?? "",
				isRoot = false,
				requiresComponents = CollectRequiredComponents(_type),
				styles = SafeStyleNames(_type),
			};

			CollectFields(_type, component);

			// Native Unity components (CanvasGroup, ...) have no reflectable serialized fields — their
			// authorable data lives on C# properties. Fall back to public read/write properties so they
			// still expose a usable vocabulary (props are tagged member="property" for the baker).
			if (component.props.Count == 0 && component.events.Count == 0)
				CollectProperties(_type, component);

			ResolveContentField(_type, component);
			return component;
		}

		// Public read/write instance properties declared on the type (up to, but excluding, the Unity
		// framework base types). Used only as the field-reflection fallback for native components.
		private static void CollectProperties( Type _type, UiCatalogComponent _component )
		{
			for (var t = _type; !IsFrameworkType(t); t = t.BaseType)
			{
				var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
				foreach (var p in properties)
				{
					if (!p.CanRead || !p.CanWrite || p.GetIndexParameters().Length > 0)
						continue;
					if (p.GetCustomAttribute<ObsoleteAttribute>() != null)
						continue;

					var prop = new UiCatalogProp
					{
						name = p.Name,
						field = p.Name,
						member = "property",
						tooltip = p.GetCustomAttribute<TooltipAttribute>()?.tooltip ?? "",
					};
					ClassifyValue(p.PropertyType, prop);
					_component.props.Add(prop);
				}
			}
		}

		private static bool IsFrameworkType( Type _t )
		{
			return _t == null
			    || _t == typeof(object)
			    || _t == typeof(Component)
			    || _t == typeof(Behaviour)
			    || _t == typeof(MonoBehaviour)
			    || _t == typeof(UnityEngine.Object);
		}

		#endregion

		#region Palette

		// Built-in scan root: every prefab whose asset path contains this segment is a palette template.
		private const string StandardElementsSegment = "/Prefabs/StandardElements/";

		// The prefab GUIDs that make up the authoring palette (and the scan scope for standard-element
		// markers): the built-in StandardElements folder plus any extra folders / individual prefabs
		// configured on the override asset. Deduped, order-preserving.
		private static List<string> CollectCandidatePrefabGuids( UiAuthorablePaletteConfig _config )
		{
			var guids = new List<string>();
			void AddGuid( string _guid )
			{
				if (!string.IsNullOrEmpty(_guid) && !guids.Contains(_guid))
					guids.Add(_guid);
			}

			foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.Replace('\\', '/').Contains(StandardElementsSegment))
					AddGuid(guid);
			}

			if (_config != null)
			{
				foreach (var folder in _config.ExtraFolderPaths())
					foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
						AddGuid(guid);

				foreach (var prefab in _config.ExtraPrefabs)
				{
					if (prefab == null)
						continue;
					string path = AssetDatabase.GetAssetPath(prefab);
					AddGuid(AssetDatabase.AssetPathToGUID(path));
				}
			}

			return guids;
		}

		// The scan scope for standard-element MARKERS is wider than the palette: markers live on prefabs
		// across the whole toolkit /Prefabs/ tree (buttons, dialogs, player-settings, pickers, text, …),
		// not just the StandardElements folder. Bounded to the toolkit root (plus client folders/prefabs),
		// so it stays cheap even in a large host project — never a whole-project prefab load.
		private static List<string> CollectStandardElementCandidateGuids( UiAuthorablePaletteConfig _config )
		{
			var guids = new List<string>();
			void AddGuid( string _guid )
			{
				if (!string.IsNullOrEmpty(_guid) && !guids.Contains(_guid))
					guids.Add(_guid);
			}

			foreach (var folder in StandardElementScanFolders(_config, _warnOnMissingVariantsPath: true))
				foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
					AddGuid(guid);

			if (_config != null)
			{
				foreach (var prefab in _config.ExtraPrefabs)
				{
					if (prefab == null)
						continue;
					AddGuid(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab)));
				}
			}

			return guids;
		}

		/// <summary>
		/// The folders scanned for standard-element markers, in scan order. Existing folders only.
		/// </summary>
		/// <param name="_warnOnMissingVariantsPath">
		/// Whether a configured but non-existent <c>PrefabVariantsPath</c> is worth a warning. True for the
		/// catalog run, false for callers that merely ASK about the scope and would repeat the warning.
		/// </param>
		public static List<string> StandardElementScanFolders(
			UiAuthorablePaletteConfig _config, bool _warnOnMissingVariantsPath = false )
		{
			var folders = new List<string>();
			void AddFolder( string _folder )
			{
				if (!string.IsNullOrEmpty(_folder) && !folders.Contains(_folder))
					folders.Add(_folder);
			}

			string toolkitRoot = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir()?.TrimEnd('/');
			if (!string.IsNullOrEmpty(toolkitRoot) && AssetDatabase.IsValidFolder(toolkitRoot))
				AddFolder(toolkitRoot);

			// The canonical client prefab-variants folder: client variants of tagged standard elements live
			// here (the variant-creation tool writes here), so they are discovered and out-rank the library
			// defaults. Client-writable and works whether the toolkit is symlinked or in read-only Packages.
			string variantsPath = UiToolkitConfiguration.Instance.PrefabVariantsPath?.TrimEnd('/');
			if (!string.IsNullOrEmpty(variantsPath))
			{
				if (AssetDatabase.IsValidFolder(variantsPath))
				{
					AddFolder(variantsPath);
				}
				else if (_warnOnMissingVariantsPath)
				{
					UiLog.LogWarning($"AI catalog: prefabVariantsPath '{variantsPath}' does not exist — client " +
						"prefab variants placed there won't be discovered, so standard elements (and thus UiMain " +
						"and authored screens) resolve to the LIBRARY defaults. Create the folder or fix the path " +
						"in Ui Toolkit Configuration.");
				}
			}

			if (_config != null)
			{
				foreach (var folder in _config.ExtraFolderPaths())
					AddFolder(folder?.TrimEnd('/'));
			}

			return folders;
		}

		/// <summary>
		/// Whether a standard-element marker on this prefab would ever be SEEN by the catalog scan.
		/// </summary>
		/// <remarks>
		/// The scan is deliberately bounded — toolkit root, <c>PrefabVariantsPath</c>, the palette config's
		/// extra folders and prefabs — so that it stays cheap in a large host project and never loads every
		/// prefab. The cost of that bound is that a marker outside it is written and then never read: the
		/// prefab carries a perfectly valid identity, the palette count does not move, and nothing says why.
		/// Callers that CREATE markers use this to say so at the moment the marker is placed.
		/// </remarks>
		/// <param name="_prefabPath">Project-relative prefab path.</param>
		/// <param name="_scanFolders">The folders that were checked, for the caller's message.</param>
		public static bool IsInStandardElementScanScope( string _prefabPath, out List<string> _scanFolders )
		{
			var config = UiAuthorablePaletteConfig.FindFirst();
			_scanFolders = StandardElementScanFolders(config);

			string path = _prefabPath?.Replace("\\", "/");
			if (string.IsNullOrEmpty(path))
				return false;

			foreach (var folder in _scanFolders)
			{
				if (path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
					return true;
			}

			if (config != null)
			{
				// Individually listed prefabs are in scope wherever they live.
				foreach (var prefab in config.ExtraPrefabs)
				{
					if (prefab == null)
						continue;

					string extraPath = AssetDatabase.GetAssetPath(prefab)?.Replace("\\", "/");
					if (string.Equals(extraPath, path, StringComparison.OrdinalIgnoreCase))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Builds the authoring palette: the prefabs under the built-in StandardElements folder plus the
		/// configured extra folders/prefabs, UNION every standard-element identity the registry resolved.
		/// The union is what makes the palette match what a screen may actually write into "template" —
		/// a registry key resolves regardless of where its prefab lives, and shipped screens use keys
		/// (StandardHeadline, StandardBackButton, ...) whose prefabs sit outside the StandardElements
		/// folder. Listing only the folder scan made those look non-existent, so screens were authored by
		/// hand-building what the palette already composes.
		/// </summary>
		private static void CollectPalette( UiScreenCatalog _catalog, List<ResolvedStandardElement> _standardElements )
		{
			var config = UiAuthorablePaletteConfig.FindFirst();
			var guids = CollectCandidatePrefabGuids(config);

			var entries = new List<UiPaletteEntry>();
			foreach (var guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				var entry = BuildPaletteEntry(prefab, path, guid, prefab != null ? prefab.name : null, config);
				if (entry != null)
					entries.Add(entry);
			}

			// A folder-scan entry whose name IS a standard-element identity is authored through the registry,
			// so it must advertise the prefab the registry actually resolves that identity to. Otherwise the
			// catalog points at the scanned library prefab while a bake instantiates the client variant that
			// out-ranks it — the entry would describe something other than what you get.
			var winnerByKey = new Dictionary<string, GameObject>(StringComparer.Ordinal);
			foreach (var resolved in _standardElements ?? new List<ResolvedStandardElement>())
			{
				if (!string.IsNullOrEmpty(resolved.key) && resolved.prefab != null)
					winnerByKey[resolved.key] = resolved.prefab;
			}

			foreach (var entry in entries)
			{
				if (!winnerByKey.TryGetValue(entry.name, out var winner))
					continue;

				string winnerPath = AssetDatabase.GetAssetPath(winner);
				if (string.IsNullOrEmpty(winnerPath) || winnerPath == entry.prefabPath)
					continue;

				entry.prefabPath = winnerPath;
				entry.prefabGuid = AssetDatabase.AssetPathToGUID(winnerPath);
			}

			// Deduped by AUTHORING NAME, not by prefab: the folder scan lists a prefab under its own name,
			// while a registry key is authored under the key. Both can point at the same asset (the key's
			// winner may be a client variant of the scanned library prefab) — the folder entry wins, since
			// naming a specific prefab and naming an identity are both legal and the entry already exists.
			var byName = new HashSet<string>(entries.Select(e => e.name), StringComparer.Ordinal);
			foreach (var resolved in _standardElements ?? new List<ResolvedStandardElement>())
			{
				if (string.IsNullOrEmpty(resolved.key) || byName.Contains(resolved.key))
					continue;

				string path = AssetDatabase.GetAssetPath(resolved.prefab);
				var entry = BuildPaletteEntry(resolved.prefab, path, AssetDatabase.AssetPathToGUID(path),
					resolved.key, config);
				if (entry == null)
					continue;

				entries.Add(entry);
				byName.Add(resolved.key);
			}

			_catalog.palette = entries
				.OrderBy(e => e.category, StringComparer.Ordinal)
				.ThenBy(e => e.name, StringComparer.Ordinal)
				.ToList();
		}

		// _authoringName is the value a screen writes into "template": the prefab's own name for a folder-scan
		// entry, the registry key for a resolved standard element.
		private static UiPaletteEntry BuildPaletteEntry( GameObject _prefab, string _path, string _guid,
			string _authoringName, UiAuthorablePaletteConfig _config )
		{
			if (_prefab == null || string.IsNullOrEmpty(_authoringName))
				return null;

			string name = _authoringName;
			if (_config != null && (_config.IsHidden(name) || _config.IsHidden(_prefab.name)))
				return null;

			// Internal sub-parts of a composed element: bakeable, but only meaningful inside their parent.
			// Listed with the flag rather than dropped — an author needs to tell "not for me" apart from
			// "does not exist", and a silent omission is what sends them off hand-building a replacement.
			var marker = _prefab.GetComponent<UiStandardElement>();

			Type primary = PrimaryComponentType(_prefab);

			var entry = new UiPaletteEntry
			{
				name = name,
				prefabPath = _path,
				prefabGuid = _guid,
				isInternal = marker != null && marker.IsInternal,
				kind = primary?.Name ?? "",
				category = primary != null ? ClassifyCategory(primary) : "Container",
				// Instance/"flavor" description: a UiComment on the prefab root. Curated per prefab (and
				// travels with a client variant), so OkButton and CancelButton can read differently even
				// though both are UiButtons. The per-type description lives on the component entry.
				description = RootCommentText(_prefab),
				standardElement = StandardElementKey(_prefab),
				slots = DerivePaletteSlots(_prefab, primary),
				parts = DerivePaletteParts(_prefab, out bool partsTruncated),
			};
			entry.partsTruncated = partsTruncated;

			var over = _config?.FindOverride(name);
			if (over != null)
			{
				if (!string.IsNullOrEmpty(over.category)) entry.category = over.category;
				if (over.slots != null && over.slots.Count > 0) entry.slots = over.slots;
			}

			return entry;
		}

		/// <summary>Text of a <see cref="UiComment"/> on the prefab root (whitespace-collapsed), or "" if none.</summary>
		private static string RootCommentText( GameObject _prefab )
		{
			var comment = _prefab.GetComponent<UiComment>();
			if (comment == null || string.IsNullOrWhiteSpace(comment.Text))
				return "";
			return Regex.Replace(comment.Text, "\\s+", " ").Trim();
		}

		// The most-derived toolkit UiThing on the prefab root — the component that "is" the widget.
		private static Type PrimaryComponentType( GameObject _root )
		{
			Type best = null;
			foreach (var component in _root.GetComponents<Component>())
			{
				if (component == null)
					continue;
				var type = component.GetType();
				if (!typeof(UiThing).IsAssignableFrom(type))
					continue;
				if (best == null || best.IsAssignableFrom(type))
					best = type;
			}
			return best;
		}

		/// <summary>Cap on <see cref="UiPaletteEntry.parts"/> per element, so a large dialog cannot swamp the catalog.</summary>
		private const int MaxPaletteParts = 20;

		/// <summary>
		/// The text component a node-level <c>"text"</c> actually writes to, and how many candidates there are.
		/// </summary>
		/// <remarks>
		/// Mirrors <c>UiScreenBaker.ApplyText</c> deliberately — localized component first, plain TMP as the
		/// fallback, first in hierarchy order either way. If the two ever disagree the catalog would document
		/// a target the baker does not use, which is worse than documenting nothing.
		/// </remarks>
		private static TMPro.TMP_Text NodeTextTarget( GameObject _root, out int _candidates )
		{
			var localized = _root.GetComponentsInChildren<UiLocalizedTextMeshProUGUI>(true);
			if (localized.Length > 0)
			{
				_candidates = localized.Length;
				return localized[0];
			}

			var plain = _root.GetComponentsInChildren<TMPro.TMP_Text>(true);
			_candidates = plain.Length;
			return plain.Length > 0 ? plain[0] : null;
		}

		/// <summary>Slash-separated path of <paramref name="_child"/> below <paramref name="_root"/>, "" if it IS the root.</summary>
		private static string PathRelativeTo( Transform _root, Transform _child )
		{
			var names = new List<string>();
			for (var t = _child; t != null && t != _root; t = t.parent)
				names.Insert(0, t.name);
			return string.Join("/", names);
		}

		/// <summary>
		/// The child paths an <c>"overrides"</c> entry on this element may be keyed by, in hierarchy order.
		/// </summary>
		/// <remarks>
		/// Every emitted path is verified against <c>Transform.Find</c> before it goes in — the list is a
		/// promise the baker has to be able to keep, not a rendering of the hierarchy. Three cases are
		/// therefore dropped rather than described:
		/// <list type="bullet">
		/// <item>a name containing '/', which Find would read as a separator (subtree skipped too);</item>
		/// <item>a second sibling with the same name, because Find returns the first and the later one is
		/// unreachable no matter what we write here (subtree skipped as well);</item>
		/// <item>anything Find does not return, as a belt-and-braces check.</item>
		/// </list>
		/// Parts without an addressable component are not listed but ARE descended into, so a pure layout
		/// container costs nothing and its children still show up — with the container's name inside their
		/// path, which is all an author needs to reach it.
		/// </remarks>
		private static List<UiPalettePart> DerivePaletteParts( GameObject _root, out bool _truncated )
		{
			var parts = new List<UiPalettePart>();
			var emittedPaths = new HashSet<string>(StringComparer.Ordinal);

			// A local, not the out parameter: a local function cannot touch one.
			bool truncated = false;

			void Walk( Transform _t, string _prefix )
			{
				foreach (Transform child in _t)
				{
					if (parts.Count >= MaxPaletteParts)
					{
						truncated = true;
						return;
					}

					string name = child.name;
					if (string.IsNullOrEmpty(name) || name.Contains("/"))
						continue;

					string path = string.IsNullOrEmpty(_prefix) ? name : _prefix + "/" + name;

					// A duplicate path is unreachable for every sibling after the first, so neither it nor
					// its children can be addressed.
					if (!emittedPaths.Add(path))
						continue;

					var type = PartComponentType(child.gameObject);
					string element = StandardElementKey(child.gameObject);
					if (type != null && _root.transform.Find(path) == child)
					{
						parts.Add(new UiPalettePart
						{
							path = path,
							type = type.Name,
							element = element,
							text = child.GetComponent<TMPro.TMP_Text>() != null,
							shipsInactive = !child.gameObject.activeSelf,
						});
					}

					// A part that is itself a palette element documents its own internals under its own
					// entry. Descending anyway would repeat them here at paths long enough to bury the
					// structure the author is composing with.
					//
					// Except an animation wrapper: it is tagged so it can be instantiated, but compositionally
					// it only passes through, and stopping there hid the one thing worth reaching — the glyph
					// inside a CloseButton sat behind a WiggleAnimation and vanished from the list entirely.
					bool stopsHere = !string.IsNullOrEmpty(element)
						&& !typeof(UiSimpleAnimationBase).IsAssignableFrom(type);

					if (!stopsHere)
						Walk(child, path);
				}
			}

			Walk(_root.transform, "");
			_truncated = truncated;
			return parts;
		}

		/// <summary>
		/// The component that best identifies a part, or null when the part carries nothing worth addressing.
		/// </summary>
		/// <remarks>
		/// Broader than <see cref="PrimaryComponentType"/>, which only looks at <see cref="UiThing"/> and
		/// would therefore call most internals of a composed element uninteresting — a plain Image doing the
		/// work of a tab underline is exactly what an author wants to reach.
		///
		/// Animations are ranked last on purpose. They are the most-derived UiThing on many nodes, so taking
		/// the toolkit component first labelled an image "UiSimpleAnimation": true, and useless. The name has
		/// to say what the part IS, not what happens to it.
		/// </remarks>
		private static Type PartComponentType( GameObject _go )
		{
			var toolkit = PrimaryComponentType(_go);
			if (toolkit != null && !typeof(UiSimpleAnimationBase).IsAssignableFrom(toolkit))
				return toolkit;

			// Order by how much it tells the author, not by class hierarchy: an interactive part first, then
			// text, then a plain graphic, then the fade handle. Layout-only components are deliberately absent
			// — naming them would list every container in the tree without helping anyone.
			if (_go.GetComponent<UGUI.Selectable>() is { } selectable)
				return selectable.GetType();
			if (_go.GetComponent<TMPro.TMP_Text>() is { } text)
				return text.GetType();
			if (_go.GetComponent<UGUI.Graphic>() is { } graphic)
				return graphic.GetType();
			if (_go.GetComponent<CanvasGroup>() != null)
				return typeof(CanvasGroup);

			// Nothing visual, but an animation host is still worth an "active" or "rect" override, and
			// dropping it would leave a hole in the middle of the paths below it.
			return toolkit;
		}

		private static List<UiPaletteSlot> DerivePaletteSlots( GameObject _root, Type _primary )
		{
			var slots = new List<UiPaletteSlot>();

			var textTarget = NodeTextTarget(_root, out int textCandidates);
			if (textTarget != null)
			{
				bool localized = textTarget is UiLocalizedTextMeshProUGUI;
				string note = localized
					? "Set a loca key (prefix a literal with '@text:' to bypass localization)."
					: "Set the display text.";

				// With several texts in the tree, "text" on the node is NOT the obvious one — it is whichever
				// comes first in the hierarchy. On a composed dialog that is easily a close button's glyph
				// while the headline sits further down, and the slot list said none of it. Naming the target
				// is the difference between a vocabulary and a trap.
				if (textCandidates > 1)
				{
					note += $" This element has {textCandidates} texts; the node-level slot writes to "
						+ $"'{PathRelativeTo(_root.transform, textTarget.transform)}'. Address the others "
						+ "through \"overrides\" — see \"parts\".";
				}

				slots.Add(new UiPaletteSlot
				{
					name = "text",
					kind = localized ? "loca" : "text",
					note = note,
				});
			}

			if (_primary != null)
			{
				if (typeof(UiButtonBase).IsAssignableFrom(_primary) || _root.GetComponent<UGUI.Button>() != null)
					slots.Add(new UiPaletteSlot { name = "onClick", kind = "event", note = "Click handler (wired later)." });

				if (typeof(UiToggle).IsAssignableFrom(_primary))
					slots.Add(new UiPaletteSlot { name = "onValueChanged", kind = "event", note = "Toggle change handler (wired later)." });
			}

			return slots;
		}

		/// <summary>The standard-element key from a root <see cref="UiStandardElement"/> marker, or "" if untagged.</summary>
		private static string StandardElementKey( GameObject _prefab )
		{
			var marker = _prefab.GetComponent<UiStandardElement>();
			if (marker == null || marker.Element == EStandardElement.None)
				return "";
			return marker.Key ?? "";
		}

		#endregion

		#region Standard-element registry

		private class StandardElementCandidate
		{
			public EStandardElement element;
			public string customId;
			public GameObject prefab;
			public string path;
			public bool fromLibrary;
			public bool isInternal;
		}

		/// <summary>A standard-element identity and the prefab it resolved to — a valid "template" value.</summary>
		private class ResolvedStandardElement
		{
			public string key;
			public GameObject prefab;
		}

		/// <summary>
		/// Scans the palette candidate prefabs for <see cref="UiStandardElement"/> markers, resolves the
		/// winning prefab per identity (client prefabs/variants out-rank toolkit-library defaults; a
		/// same-rank tie is an error), and writes the runtime <see cref="UiStandardElementRegistry"/>,
		/// pointing <see cref="UiToolkitConfiguration"/> at it.
		/// </summary>
		/// <returns>The resolved identities, so <see cref="CollectPalette"/> can list them as templates.</returns>
		private static List<ResolvedStandardElement> CollectStandardElements( UiScreenCatalog _catalog )
		{
			var config = UiAuthorablePaletteConfig.FindFirst();
			var byKey = new Dictionary<string, List<StandardElementCandidate>>(StringComparer.Ordinal);

			foreach (var guid in CollectStandardElementCandidateGuids(config))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				var marker = prefab != null ? prefab.GetComponent<UiStandardElement>() : null;
				if (marker == null || marker.Element == EStandardElement.None)
					continue;

				string key = marker.Key;
				if (string.IsNullOrEmpty(key))
				{
					UiLog.LogWarning($"AI catalog: UiStandardElement on '{path}' is Custom with an empty Custom Id; skipped.");
					continue;
				}

				if (!byKey.TryGetValue(key, out var list))
					byKey[key] = list = new List<StandardElementCandidate>();

				list.Add(new StandardElementCandidate
				{
					element = marker.Element,
					customId = marker.CustomId,
					prefab = prefab,
					path = path,
					fromLibrary = EditorAssetUtility.IsPackagesOrInternalAsset(prefab),
					isInternal = marker.IsInternal,
				});
			}

			var entries = new List<UiStandardElementRegistry.Entry>();
			var resolved = new List<ResolvedStandardElement>();
			foreach (var kv in byKey.OrderBy(k => k.Key, StringComparer.Ordinal))
			{
				// Client prefabs/variants out-rank library defaults; a tie within the winning rank is ambiguous.
				var client = kv.Value.Where(c => !c.fromLibrary).OrderBy(c => c.path, StringComparer.Ordinal).ToList();
				var winners = client.Count > 0
					? client
					: kv.Value.OrderBy(c => c.path, StringComparer.Ordinal).ToList();

				if (winners.Count > 1)
				{
					UiLog.LogError($"AI catalog: standard element '{kv.Key}' is claimed by {winners.Count} " +
					               $"{(client.Count > 0 ? "client" : "library")} prefabs — ambiguous:\n  " +
					               $"{string.Join("\n  ", winners.Select(c => c.path))}\nUsing '{winners[0].path}'.");

					// Also persist it: the console is not reachable over MCP, so setup_status is the only
					// place an external agent can learn that a key silently resolved to the wrong prefab.
					_catalog.standardElementAmbiguities.Add(new UiCatalogStandardElementAmbiguity
					{
						key = kv.Key,
						candidates = winners.Select(c => c.path).ToList(),
						winner = winners[0].path,
						client = client.Count > 0,
					});
				}

				var winner = winners[0];
				entries.Add(new UiStandardElementRegistry.Entry
				{
					element = winner.element,
					customId = winner.customId,
					prefab = winner.prefab,
					fromLibrary = winner.fromLibrary,
					isInternal = winner.isInternal,
				});
				resolved.Add(new ResolvedStandardElement { key = kv.Key, prefab = winner.prefab });
			}

			WarnMissingStandardElements(byKey);
			WriteStandardElementRegistry(entries);

			return resolved;
		}

		/// <summary>Warns which built-in <see cref="EStandardElement"/> values have no tagged prefab yet.</summary>
		private static void WarnMissingStandardElements( Dictionary<string, List<StandardElementCandidate>> _byKey )
		{
			var missing = new List<string>();
			int builtinCount = 0;
			foreach (EStandardElement e in Enum.GetValues(typeof(EStandardElement)))
			{
				if (e == EStandardElement.None || e == EStandardElement.Custom)
					continue;
				builtinCount++;
				if (!_byKey.ContainsKey(e.ToString()))
					missing.Add(e.ToString());
			}

			if (missing.Count > 0)
				UiLog.LogWarning($"AI catalog: {missing.Count}/{builtinCount} built-in standard elements have no prefab " +
				                 $"tagged with a UiStandardElement marker yet (UiMain falls back to its inline/config prefabs " +
				                 $"for these):\n  {string.Join(", ", missing)}");
		}

		private static void WriteStandardElementRegistry( List<UiStandardElementRegistry.Entry> _entries )
		{
			var config = UiToolkitConfiguration.Instance;

			var registry = config != null ? config.StandardElementRegistry : null;
			if (registry == null)
				registry = FindExistingRegistry();

			if (registry == null)
			{
				string dir = config != null ? config.GeneratedAssetsDir : "Assets/";
				if (!dir.EndsWith("/"))
					dir += "/";
				string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}UiStandardElementRegistry.asset");
				registry = ScriptableObject.CreateInstance<UiStandardElementRegistry>();
				AssetDatabase.CreateAsset(registry, path);
				UiLog.LogInternal($"AI catalog: created standard-element registry at '{path}'.");
			}

			registry.EditorSetEntries(_entries);
			EditorUtility.SetDirty(registry);

			if (config != null)
				config.EditorSetStandardElementRegistry(registry);

			AssetDatabase.SaveAssets();
			UiLog.LogInternal($"AI catalog: standard-element registry holds {_entries.Count} " +
			                  $"entr{(_entries.Count == 1 ? "y" : "ies")}.");
		}

		private static UiStandardElementRegistry FindExistingRegistry()
		{
			foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(UiStandardElementRegistry)}"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var registry = AssetDatabase.LoadAssetAtPath<UiStandardElementRegistry>(path);
				if (registry != null)
					return registry;
			}
			return null;
		}

		#endregion

		#region Styles

		private static void CollectStyles( UiScreenCatalog _catalog )
		{
			var skinNames = new List<string>();
			var stylesByType = new Dictionary<string, List<string>>();

			foreach (var config in s_styleConfigCache)
			{
				foreach (var skinName in config.SkinNames)
					if (!skinNames.Contains(skinName))
						skinNames.Add(skinName);

				var skins = config.Skins;
				if (skins == null || skins.Count == 0)
					continue;

				// The first skin is the canonical style set (mirrors the toolkit's own convention).
				foreach (var style in skins[0].Styles)
				{
					if (style?.SupportedComponentType == null)
						continue;

					string typeName = style.SupportedComponentType.Name;
					if (!stylesByType.TryGetValue(typeName, out var names))
					{
						names = new List<string>();
						stylesByType[typeName] = names;
					}

					if (!names.Contains(style.Name))
						names.Add(style.Name);
				}
			}

			_catalog.skins = skinNames;
			_catalog.styleGroups = stylesByType
				.OrderBy(kv => kv.Key, StringComparer.Ordinal)
				.Select(kv => new UiCatalogStyleGroup { componentType = kv.Key, styleNames = kv.Value })
				.ToList();
		}

		private static List<UiStyleConfig> LoadAllStyleConfigs()
		{
			var result = new List<UiStyleConfig>();
			foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(UiStyleConfig)}"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var config = AssetDatabase.LoadAssetAtPath<UiStyleConfig>(path);
				if (config != null)
					result.Add(config);
			}
			return result;
		}

		// Styles keyed directly to this Ui* type (usually none — styles target the underlying
		// Unity components). Returns empty gracefully if no config is available.
		private static List<string> SafeStyleNames( Type _type )
		{
			var names = new List<string>();
			foreach (var config in s_styleConfigCache)
			{
				foreach (var n in config.GetEffectiveStyleNamesByMonoBehaviourType(_type))
					if (!names.Contains(n))
						names.Add(n);
			}
			return names;
		}

		#endregion

		#region Helpers

		private static bool IsSerializedField( FieldInfo _field )
		{
			if (_field.IsStatic || _field.IsLiteral || _field.IsInitOnly)
				return false;
			if (_field.GetCustomAttribute<NonSerializedAttribute>() != null)
				return false;

			return _field.IsPublic || _field.GetCustomAttribute<SerializeField>() != null;
		}

		private static bool IsEventField( Type _type )
		{
			if (typeof(UnityEventBase).IsAssignableFrom(_type))
				return true;
			// The toolkit's own CEvent<...> family (see EventOverrides.cs).
			return _type.Name.Contains("CEvent");
		}

		private static Type GetEnumerableElementType( Type _type )
		{
			if (_type.IsArray)
				return _type.GetElementType();
			if (_type.IsGenericType && _type.GetGenericTypeDefinition() == typeof(List<>))
				return _type.GetGenericArguments()[0];
			return null;
		}

		private static bool IsIntegerType( Type _type )
		{
			return _type == typeof(int) || _type == typeof(uint)
			    || _type == typeof(long) || _type == typeof(ulong)
			    || _type == typeof(short) || _type == typeof(ushort)
			    || _type == typeof(byte) || _type == typeof(sbyte);
		}

		private static string AuthoringName( string _fieldName )
		{
			return _fieldName.StartsWith("m_", StringComparison.Ordinal)
				? _fieldName.Substring(2)
				: _fieldName;
		}

		private static bool ContainsAny( string _name, params string[] _needles )
		{
			foreach (var n in _needles)
				if (_name.IndexOf(n, StringComparison.Ordinal) >= 0)
					return true;
			return false;
		}

		#endregion
	}
}
