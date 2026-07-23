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

				catalog.components = catalog.components
					.OrderBy(c => c.category, StringComparer.Ordinal)
					.ThenBy(c => c.type, StringComparer.Ordinal)
					.ToList();

				WarnMissingDescriptions(catalog);

				CollectPalette(catalog);
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
			ResolveChildrenCapability(_type, component);

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
			var missing = _catalog.components
				.Where(c => string.IsNullOrEmpty(c.description))
				.Select(c => c.type)
				.OrderBy(t => t, StringComparer.Ordinal)
				.ToList();

			if (missing.Count == 0)
				return;

			UiLog.LogWarning($"AI catalog: {missing.Count}/{_catalog.components.Count} authorable components have no " +
			                 $"/// <summary> doc comment (no description for the authoring AI):\n  {string.Join(", ", missing)}");
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

		private static void ResolveChildrenCapability( Type _type, UiCatalogComponent _component )
		{
			if (typeof(UiView).IsAssignableFrom(_type)
			    || typeof(UiPanel).IsAssignableFrom(_type)
			    || typeof(UGUI.LayoutGroup).IsAssignableFrom(_type))
			{
				_component.acceptsChildren = true;
			}

			// Best-effort detection of an explicit content container field.
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
					_component.acceptsChildren = true;
					break;
				}
			}
		}

		#endregion

		#region Palette

		// Built-in scan root: every prefab whose asset path contains this segment is a palette template.
		private const string StandardElementsSegment = "/Prefabs/StandardElements/";

		private static void CollectPalette( UiScreenCatalog _catalog )
		{
			var config = UiAuthorablePaletteConfig.FindFirst();

			// Collect candidate prefab GUIDs: the built-in StandardElements scan plus any extra folders
			// / individual prefabs configured on the override asset.
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

			if (config != null)
			{
				foreach (var folder in config.ExtraFolderPaths())
					foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
						AddGuid(guid);

				foreach (var prefab in config.ExtraPrefabs)
				{
					if (prefab == null)
						continue;
					string path = AssetDatabase.GetAssetPath(prefab);
					AddGuid(AssetDatabase.AssetPathToGUID(path));
				}
			}

			var entries = new List<UiPaletteEntry>();
			foreach (var guid in guids)
			{
				var entry = BuildPaletteEntry(guid, config);
				if (entry != null)
					entries.Add(entry);
			}

			_catalog.palette = entries
				.OrderBy(e => e.category, StringComparer.Ordinal)
				.ThenBy(e => e.name, StringComparer.Ordinal)
				.ToList();
		}

		private static UiPaletteEntry BuildPaletteEntry( string _guid, UiAuthorablePaletteConfig _config )
		{
			string path = AssetDatabase.GUIDToAssetPath(_guid);
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab == null)
				return null;

			string name = prefab.name;
			if (_config != null && _config.IsHidden(name))
				return null;

			Type primary = PrimaryComponentType(prefab);

			var entry = new UiPaletteEntry
			{
				name = name,
				prefabPath = path,
				prefabGuid = _guid,
				kind = primary?.Name ?? "",
				category = primary != null ? ClassifyCategory(primary) : "Container",
				acceptsChildren = PaletteAcceptsChildren(prefab, primary),
				slots = DerivePaletteSlots(prefab, primary),
			};

			var over = _config?.FindOverride(name);
			if (over != null)
			{
				if (!string.IsNullOrEmpty(over.category)) entry.category = over.category;
				if (!string.IsNullOrEmpty(over.description)) entry.description = over.description;
				if (over.slots != null && over.slots.Count > 0) entry.slots = over.slots;
			}

			return entry;
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

		private static bool AcceptsChildren( Type _type )
		{
			return typeof(UiView).IsAssignableFrom(_type)
			    || typeof(UiPanel).IsAssignableFrom(_type)
			    || typeof(UGUI.LayoutGroup).IsAssignableFrom(_type);
		}

		// Palette templates are prefabs, so the primary-type test alone misses containers whose root
		// carries no UiThing (e.g. StandardButtonBar = a bare RectTransform + HorizontalLayoutGroup).
		// Such a root still arranges children, so it IS an authoring container. Matches how the baker
		// nests children (it parents them straight under the node root — see UiScreenBaker.BuildNode).
		private static bool PaletteAcceptsChildren( GameObject _root, Type _primary )
		{
			if (_primary != null && AcceptsChildren(_primary))
				return true;
			return _root.GetComponent(typeof(UGUI.LayoutGroup)) != null;
		}

		private static List<UiPaletteSlot> DerivePaletteSlots( GameObject _root, Type _primary )
		{
			var slots = new List<UiPaletteSlot>();

			if (_root.GetComponentInChildren<TMPro.TMP_Text>(true) != null)
			{
				bool localized = _root.GetComponentInChildren<UiLocalizedTextMeshProUGUI>(true) != null;
				slots.Add(new UiPaletteSlot
				{
					name = "text",
					kind = localized ? "loca" : "text",
					note = localized
						? "Set a loca key (prefix a literal with '@text:' to bypass localization)."
						: "Set the display text.",
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
				foreach (var n in config.GetStyleNamesByMonoBehaviourType(_type))
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
