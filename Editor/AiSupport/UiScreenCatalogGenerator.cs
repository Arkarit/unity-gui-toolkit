using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

		// Loaded once per Generate() run so style lookups don't re-scan the AssetDatabase per component.
		private static List<UiStyleConfig> s_styleConfigCache;

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

				string outputDir = ResolveOutputDir();
				EditorFileUtility.EnsureUnityFolderExists(outputDir);
				string path = $"{outputDir}/{OutputFileName}";

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
			var runtimeAssembly = typeof(UiThing).Assembly;

			var catalog = new UiScreenCatalog
			{
				version = CatalogVersion,
				generatedAtUtc = DateTime.UtcNow.ToString("o"),
				toolkitAssembly = runtimeAssembly.GetName().Name,
			};

			s_styleConfigCache = LoadAllStyleConfigs();
			try
			{
				CollectStyles(catalog);

				var types = runtimeAssembly.GetTypes()
					.Where(IsAuthorable)
					.OrderBy(t => t.FullName, StringComparer.Ordinal);

				foreach (var type in types)
					catalog.components.Add(BuildComponent(type));

				catalog.components = catalog.components
					.OrderBy(c => c.category, StringComparer.Ordinal)
					.ThenBy(c => c.type, StringComparer.Ordinal)
					.ToList();
			}
			finally
			{
				s_styleConfigCache = null;
			}

			return catalog;
		}

		private static bool IsAuthorable( Type _type )
		{
			if (_type.IsAbstract || _type.IsInterface || _type.IsGenericTypeDefinition)
				return false;
			if (!typeof(Component).IsAssignableFrom(_type))
				return false;

			if (_type.GetCustomAttribute<UiNotAuthorableAttribute>(false) != null)
				return false;

			bool forced = _type.GetCustomAttribute<UiAuthorableAttribute>(false) != null;
			if (forced)
				return true;

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

		private static UiCatalogComponent BuildComponent( Type _type )
		{
			var authorable = _type.GetCustomAttribute<UiAuthorableAttribute>(false);

			var component = new UiCatalogComponent
			{
				type = _type.Name,
				fullName = _type.FullName,
				category = !string.IsNullOrEmpty(authorable?.Category) ? authorable.Category : ClassifyCategory(_type),
				description = authorable?.Description ?? "",
				isRoot = typeof(UiView).IsAssignableFrom(_type),
				requiresComponents = CollectRequiredComponents(_type),
				styles = SafeStyleNames(_type),
			};

			CollectFields(_type, component);
			ResolveChildrenCapability(_type, component);

			return component;
		}

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
			var runtimeAssembly = typeof(UiThing).Assembly;

			// Walk the whole hierarchy but only keep fields declared inside the toolkit assembly,
			// so Unity-internal serialized fields (Graphic, BaseMeshEffect, ...) are skipped —
			// those are covered by the styling system, not by direct authoring.
			for (var t = _type; t != null && t != typeof(object); t = t.BaseType)
			{
				if (t.Assembly != runtimeAssembly)
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

		private static string ResolveOutputDir()
		{
			// Locate this generator's own script so the catalog lands next to the package's
			// Editor code regardless of package/symlink layout.
			foreach (var guid in AssetDatabase.FindAssets($"{nameof(UiScreenCatalogGenerator)} t:MonoScript"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.EndsWith($"/{nameof(UiScreenCatalogGenerator)}.cs", StringComparison.Ordinal))
					continue;

				const string editorSegment = "/Editor/";
				int idx = path.IndexOf(editorSegment, StringComparison.Ordinal);
				if (idx >= 0)
					return path.Substring(0, idx + editorSegment.Length) + "Generated/AiSupport";
			}

			return "Assets/Editor/Generated/AiSupport";
		}

		#endregion
	}
}
