using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuiToolkit.Style;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Read and write access to the styling system's actual VALUES — the colours, fonts, sizes and sprites
	/// that decide what a project looks like. list_styles names the vocabulary; this is what changes it.
	///
	/// Without it a project's look is reachable only through the Inspector, which means an agent can author
	/// a screen but never theme it, and every colour ends up hand-copied into individual prefabs instead of
	/// living in the one place the toolkit provides for it.
	///
	/// Values are addressed the same way the screen-authoring JSON addresses props (colours as "#RRGGBBAA",
	/// assets as project-relative paths, enums by name), because a second convention for the same job is a
	/// second thing to get wrong.
	/// </summary>
	public static class UiStyleWriter
	{
		#region Clone

		/// <summary>
		/// Payload: <c>{ "which": "main"|"aspectRatio", "path": "Assets/.../MyStyleConfig.asset" }</c>.
		///
		/// A fresh project uses the config that ships INSIDE the package. Editing that is not a smaller
		/// version of theming, it is a mistake with a delay on it: it lives in the immutable package copy,
		/// so the change is either refused or silently thrown away on the next version bump. Cloning into
		/// the project first is the step that makes everything after it stick.
		/// </summary>
		public static JObject CloneConfig( JObject _request )
		{
			bool aspectRatio = IsAspectRatio(_request);
			var configuration = UiToolkitConfiguration.Instance;
			UiStyleConfig current = aspectRatio
				? configuration.UiAspectRatioDependentStyleConfig
				: configuration.UiMainStyleConfig;

			if (current == null)
				throw new Exception($"The UiToolkitConfiguration has no {(aspectRatio ? "aspect ratio " : "")}"
					+ "style config assigned at all — assign one in Gui Toolkit → Configuration first.");

			string currentPath = AssetDatabase.GetAssetPath(current);
			if (!IsPackageOwned(currentPath))
				return new JObject
				{
					["cloned"] = false,
					["path"] = currentPath,
					["reason"] = "Already project-local — edit it directly with write_skin.",
				};

			string targetPath = (string)_request["path"];
			if (string.IsNullOrWhiteSpace(targetPath))
				targetPath = "Assets/Resources/" + (aspectRatio
					? nameof(UiAspectRatioDependentStyleConfig)
					: "UiMainStyleConfig") + ".asset";

			if (!targetPath.StartsWith("Assets/", StringComparison.Ordinal))
				throw new Exception($"'{targetPath}' is not inside Assets/ — a project-local config has to be.");
			if (!targetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
				throw new Exception($"'{targetPath}' needs a .asset extension.");
			if (File.Exists(targetPath))
				throw new Exception($"'{targetPath}' already exists. Delete it first, or clone to another path — "
					+ "overwriting a style config silently would throw away whatever theming it already holds.");

			EnsureFolder(Path.GetDirectoryName(targetPath).Replace('\\', '/'));

			var clone = UnityEngine.Object.Instantiate(current);
			clone.name = Path.GetFileNameWithoutExtension(targetPath);
			AssetDatabase.CreateAsset(clone, targetPath);

			int repaired = RepairBackReferences(clone);

			var configurationObject = new SerializedObject(configuration);
			var property = configurationObject.FindProperty(aspectRatio
				? "m_uiAspectRatioDependentStyleConfig"
				: "m_uiMainStyleConfig");
			property.objectReferenceValue = clone;
			configurationObject.ApplyModifiedPropertiesWithoutUndo();

			EditorUtility.SetDirty(configuration);
			AssetDatabase.SaveAssets();
			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);

			return new JObject
			{
				["cloned"] = true,
				["path"] = targetPath,
				["clonedFrom"] = currentPath,
				["skins"] = new JArray(clone.SkinNames.Cast<object>().ToArray()),
				["repairedBackReferences"] = repaired,
				["hint"] = "The UiToolkitConfiguration now points at the clone. Regenerate the catalog if you "
					+ "rename skins or styles; changing values needs no regeneration.",
			};
		}

		/// <summary>
		/// Instantiate() copies serialized fields verbatim, and among them are the skins' and styles' own
		/// references BACK to the config they belong to — which still name the original. Left alone, the
		/// clone's styles believe they live in the package asset, and the editor's cross-style synchronisation
		/// then reacts to the wrong document. Cheap to repair here, invisible and confusing later.
		/// </summary>
		internal static int RepairBackReferences( UiStyleConfig _config )
		{
			int repaired = 0;
			var serialized = new SerializedObject(_config);
			var skins = serialized.FindProperty("m_skins");
			if (skins == null)
				return 0;

			for (int i = 0; i < skins.arraySize; i++)
			{
				var skin = skins.GetArrayElementAtIndex(i);
				var skinConfig = skin.FindPropertyRelative("m_config");
				if (skinConfig != null && skinConfig.objectReferenceValue != (UnityEngine.Object)_config)
				{
					skinConfig.objectReferenceValue = _config;
					repaired++;
				}

				var styles = skin.FindPropertyRelative("m_styles");
				if (styles == null)
					continue;

				for (int j = 0; j < styles.arraySize; j++)
				{
					var styleConfig = styles.GetArrayElementAtIndex(j).FindPropertyRelative("m_styleConfig");
					if (styleConfig == null || styleConfig.objectReferenceValue == (UnityEngine.Object)_config)
						continue;

					styleConfig.objectReferenceValue = _config;
					repaired++;
				}
			}

			if (repaired > 0)
				serialized.ApplyModifiedPropertiesWithoutUndo();

			return repaired;
		}

		#endregion

		#region Read

		/// <summary>
		/// Payload: <c>{ "which", "skin", "styles": [...], "componentType": "TMP_Text", "applicableOnly": true }</c>.
		/// Defaults to the applicable values only — a style carries every serialized field of its target
		/// component, and the handful that are switched on are the ones that actually define the look.
		/// </summary>
		public static JObject ReadSkin( JObject _request )
		{
			var config = ResolveConfig(_request, out bool aspectRatio);
			var skin = ResolveSkin(config, (string)_request["skin"]);
			bool applicableOnly = (bool?)_request["applicableOnly"] ?? true;

			var nameFilter = StringSet(_request["styles"]);
			string typeFilter = (string)_request["componentType"];

			var styles = new JArray();
			// The effective set, so an inherited style is visible to a caller instead of looking absent.
			// Each entry says where it comes from, because that decides what writing to it does: an
			// inherited style is copied into this config first (see write_skin).
			foreach (var style in skin.EffectiveStyles)
			{
				if (style == null)
					continue;
				if (nameFilter != null && !nameFilter.Contains(style.Name))
					continue;
				if (!string.IsNullOrEmpty(typeFilter) && !TypeMatches(style, typeFilter))
					continue;

				var values = new JObject();
				foreach (var pair in ValueProperties(style))
				{
					var applicable = (ApplicableValueBase)pair.Value.GetValue(style);
					if (applicable == null || (applicableOnly && !applicable.IsApplicable))
						continue;

					values[pair.Key] = applicableOnly
						? ValueToken(applicable.RawValueObj, ValueType(pair.Value))
						: new JObject
						{
							["value"] = ValueToken(applicable.RawValueObj, ValueType(pair.Value)),
							["applicable"] = applicable.IsApplicable,
						};
				}

				styles.Add(new JObject
				{
					["name"] = style.Name,
					["componentType"] = style.SupportedComponentType?.Name,
					["inherited"] = !skin.OwnsStyle(style.Key),
					["values"] = values,
				});
			}

			return new JObject
			{
				["configPath"] = AssetDatabase.GetAssetPath(config),
				["projectLocal"] = !IsPackageOwned(AssetDatabase.GetAssetPath(config)),
				["which"] = aspectRatio ? "aspectRatio" : "main",
				["skin"] = skin.Name,
				["skins"] = new JArray(config.SkinNames.Cast<object>().ToArray()),
				["applicableOnly"] = applicableOnly,
				["styles"] = styles,
			};
		}

		#endregion

		#region Write

		/// <summary>
		/// Payload:
		/// <c>{ "which", "skin", "dryRun": false, "styles": [ { "name": "Text/Headline",
		/// "componentType": "TMP_Text", "values": { "Color": "#FFDB00", "FontSize": 40 } } ] }</c>.
		///
		/// A bare value means "use this, and switch the value on"; <c>{ "value": …, "applicable": false }</c>
		/// hands a value back to whatever the component itself carries. Returns before/after per value, so a
		/// caller can verify the change rather than assume it.
		/// </summary>
		public static JObject WriteSkin( JObject _request )
		{
			var config = ResolveConfig(_request, out bool aspectRatio);
			var skin = ResolveSkin(config, (string)_request["skin"]);
			bool dryRun = (bool?)_request["dryRun"] ?? false;

			if (!dryRun && IsPackageOwned(AssetDatabase.GetAssetPath(config)))
				throw new Exception("Refusing to write the style config that ships with the package "
					+ $"('{AssetDatabase.GetAssetPath(config)}'): it lives in the immutable package copy and the "
					+ "change would be lost on the next version bump. Run clone_style_config first.");

			if (_request["styles"] is not JArray requestedStyles || requestedStyles.Count == 0)
				throw new Exception("write_skin requires a non-empty 'styles' array.");

			var changes = new JArray();
			var warnings = new JArray();
			int changed = 0;

			// Everything is resolved and converted BEFORE anything is written. The first version wrote as it
			// went and threw on the first unreadable value — leaving the earlier styles changed and the later
			// ones untouched, which is the one outcome a caller cannot reason about. A rejected call now
			// leaves the config exactly as it found it.
			var planned = new List<Planned>();

			foreach (var entry in requestedStyles.OfType<JObject>())
			{
				string styleName = (string)entry["name"]
					?? throw new Exception("Every entry in 'styles' needs a 'name'.");
				var style = ResolveStyle(skin, styleName, (string)entry["componentType"], !dryRun, out bool materialized);
				if (materialized)
				{
					warnings.Add($"Style '{styleName}' was inherited and has been copied into "
						+ $"'{AssetDatabase.GetAssetPath(config)}' so it can be written to. It no longer follows "
						+ "the config it came from.");
				}

				if (entry["values"] is not JObject requestedValues)
					throw new Exception($"Style '{styleName}' has no 'values' object.");

				var properties = ValueProperties(style);
				var applied = new JObject();

				foreach (var value in requestedValues)
				{
					if (!properties.TryGetValue(value.Key, out var property))
					{
						string known = string.Join(", ", properties.Keys.OrderBy(_k => _k));
						throw new Exception($"Style '{styleName}' ({style.SupportedComponentType?.Name}) has no "
							+ $"value called '{value.Key}'. It has: {known}.");
					}

					var target = (ApplicableValueBase)property.GetValue(style);
					var valueType = ValueType(property);

					JToken wanted = value.Value;
					bool? applicable = null;
					if (wanted is JObject wrapper && (wrapper["value"] != null || wrapper["applicable"] != null))
					{
						applicable = (bool?)wrapper["applicable"];
						wanted = wrapper["value"];
					}

					var before = new JObject
					{
						["value"] = ValueToken(target.RawValueObj, valueType),
						["applicable"] = target.IsApplicable,
					};

					bool hasValue = wanted != null && wanted.Type != JTokenType.Null;
					object converted = null;
					if (hasValue && !UiScreenBaker.TryConvert(wanted, valueType, out converted))
						throw new Exception($"Cannot read {wanted.ToString(Newtonsoft.Json.Formatting.None)} "
							+ $"as {valueType.Name} for '{styleName}.{value.Key}'. Colours are \"#RRGGBBAA\", "
							+ "assets are project-relative paths, enums are their name.");

					// A value that is written but not switched on changes nothing on screen, which is the most
					// confusing possible outcome — so writing one switches it on unless told otherwise.
					bool wantApplicable = applicable ?? hasValue;

					planned.Add(new Planned(target, hasValue, converted, wantApplicable, valueType, applied,
						value.Key, before, wanted));
					changed++;
				}

				changes.Add(new JObject
				{
					["name"] = style.Name,
					["componentType"] = style.SupportedComponentType?.Name,
					["values"] = applied,
				});
			}

			// Second pass: nothing above threw, so every planned change is applicable and the write can run to
			// completion.
			foreach (var change in planned)
			{
				if (!dryRun)
				{
					if (change.HasValue)
						change.Target.RawValueObj = change.Value;
					change.Target.IsApplicable = change.Applicable;
				}

				change.Report[change.Key] = new JObject
				{
					["before"] = change.Before,
					["after"] = new JObject
					{
						["value"] = dryRun
							? (change.Wanted ?? change.Before["value"])
							: ValueToken(change.Target.RawValueObj, change.ValueType),
						["applicable"] = change.Applicable,
					},
				};
			}

			if (!dryRun)
			{
				EditorUtility.SetDirty(config);
				AssetDatabase.SaveAssets();

				// Appliers listen for this, and they run in Edit Mode too — so an open scene, an open prefab
				// stage and the next screenshot all show the new values without a reimport or a re-bake.
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			}

			return new JObject
			{
				["configPath"] = AssetDatabase.GetAssetPath(config),
				["which"] = aspectRatio ? "aspectRatio" : "main",
				["skin"] = skin.Name,
				["dryRun"] = dryRun,
				["valuesChanged"] = changed,
				["styles"] = changes,
				["warnings"] = warnings,
			};
		}

		/// <summary>One resolved, converted, not-yet-written value change plus where to report it.</summary>
		private readonly struct Planned
		{
			public readonly ApplicableValueBase Target;
			public readonly bool HasValue;
			public readonly object Value;
			public readonly bool Applicable;
			public readonly Type ValueType;
			public readonly JObject Report;
			public readonly string Key;
			public readonly JObject Before;
			public readonly JToken Wanted;

			public Planned( ApplicableValueBase _target, bool _hasValue, object _value, bool _applicable,
				Type _valueType, JObject _report, string _key, JObject _before, JToken _wanted )
			{
				Target = _target;
				HasValue = _hasValue;
				Value = _value;
				Applicable = _applicable;
				ValueType = _valueType;
				Report = _report;
				Key = _key;
				Before = _before;
				Wanted = _wanted;
			}
		}

		#endregion

		#region Helpers

		private static bool IsAspectRatio( JObject _request )
		{
			string which = ((string)_request["which"] ?? "main").ToLowerInvariant();
			return which switch
			{
				"main" => false,
				"aspectratio" => true,
				_ => throw new Exception($"'which' is 'main' or 'aspectRatio', not '{which}'."),
			};
		}

		private static UiStyleConfig ResolveConfig( JObject _request, out bool _aspectRatio )
		{
			_aspectRatio = IsAspectRatio(_request);
			var configuration = UiToolkitConfiguration.Instance;
			UiStyleConfig config = _aspectRatio
				? configuration.UiAspectRatioDependentStyleConfig
				: configuration.UiMainStyleConfig;

			if (config == null)
				throw new Exception("The UiToolkitConfiguration has no style config assigned — "
					+ "open Gui Toolkit → Configuration and assign one.");

			return config;
		}

		private static UiSkin ResolveSkin( UiStyleConfig _config, string _name )
		{
			if (_config.NumSkins == 0)
				throw new Exception($"'{AssetDatabase.GetAssetPath(_config)}' holds no skins at all.");

			if (string.IsNullOrWhiteSpace(_name))
				return _config.CurrentSkin ?? _config.Skins[0];

			foreach (var skin in _config.Skins)
				if (string.Equals(skin.Name, _name, StringComparison.Ordinal) ||
				    string.Equals(skin.Alias, _name, StringComparison.Ordinal))
					return skin;

			throw new Exception($"No skin '{_name}'. Available: {string.Join(", ", _config.SkinNames)}.");
		}

		/// <summary>
		/// A style is identified by its name AND the component type it targets — "Buttons/Standard/Background"
		/// exists five times over, once per component that makes up a button's background. Naming only the
		/// name is fine while it is unique and an error the moment it is not, rather than a coin flip.
		/// </summary>
		private static UiAbstractStyleBase ResolveStyle
		(
			UiSkin _skin,
			string _name,
			string _componentType,
			bool _materialize,
			out bool _materialized
		)
		{
			_materialized = false;

			// The effective set, so an inherited style can be written to at all. Writing to the instance
			// that comes back from inheritance would edit the config it belongs to, so it is copied into
			// this one first - unless this is a dry run, which must leave everything exactly as it was.
			var matches = _skin.EffectiveStyles
				.Where(_s => _s != null && string.Equals(_s.Name, _name, StringComparison.Ordinal))
				.Where(_s => string.IsNullOrEmpty(_componentType) || TypeMatches(_s, _componentType))
				.ToList();

			if (matches.Count == 1)
			{
				var match = matches[0];
				if (!_materialize || _skin.OwnsStyle(match.Key))
					return match;

				var own = _skin.MaterializeStyle(match.Key);
				_materialized = own != null && !ReferenceEquals(own, match);
				return own ?? match;
			}

			if (matches.Count == 0)
				throw new Exception($"No style '{_name}'"
					+ (string.IsNullOrEmpty(_componentType) ? "" : $" for component type '{_componentType}'")
					+ $" in skin '{_skin.Name}'.");

			string types = string.Join(", ", matches.Select(_m => _m.SupportedComponentType?.Name));
			throw new Exception($"'{_name}' exists for several component types ({types}) — "
				+ "say which one you mean with 'componentType'.");
		}

		private static bool TypeMatches( UiAbstractStyleBase _style, string _componentType )
		{
			var type = _style.SupportedComponentType;
			if (type == null)
				return false;

			return string.Equals(type.Name, _componentType, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(type.FullName, _componentType, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// The generated style classes expose one public property per value, typed ApplicableValue&lt;T&gt;.
		/// That property name is the name a caller uses, so the vocabulary needs no separate table to drift
		/// from the code.
		/// </summary>
		private static Dictionary<string, PropertyInfo> ValueProperties( UiAbstractStyleBase _style )
		{
			var result = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
			foreach (var property in _style.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				if (!typeof(ApplicableValueBase).IsAssignableFrom(property.PropertyType))
					continue;
				if (property.GetIndexParameters().Length > 0)
					continue;

				result[property.Name] = property;
			}

			return result;
		}

		/// <summary>The T of the property's ApplicableValue&lt;T&gt;.</summary>
		private static Type ValueType( PropertyInfo _property )
		{
			var type = _property.PropertyType;
			while (type != null)
			{
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApplicableValue<>))
					return type.GetGenericArguments()[0];
				type = type.BaseType;
			}

			throw new Exception($"'{_property.Name}' is not an ApplicableValue<T>.");
		}

		private static JToken ValueToken( object _value, Type _type )
		{
			if (_value == null)
				return JValue.CreateNull();

			if (UiScreenReader.TryEmitSimple(_value, _type, out JToken token))
				return token;

			if (_value is TMPro.VertexGradient gradient)
				return new JObject
				{
					["topLeft"] = "#" + ColorUtility.ToHtmlStringRGBA(gradient.topLeft),
					["topRight"] = "#" + ColorUtility.ToHtmlStringRGBA(gradient.topRight),
					["bottomLeft"] = "#" + ColorUtility.ToHtmlStringRGBA(gradient.bottomLeft),
					["bottomRight"] = "#" + ColorUtility.ToHtmlStringRGBA(gradient.bottomRight),
				};

			// Fonts, materials and every other asset a style can point at: the path is what identifies it,
			// and it is also what write_skin accepts back.
			if (_value is UnityEngine.Object unityObject)
			{
				// A destroyed or never-assigned Unity object is not `null` to C# — the boxed reference is a
				// real object whose overloaded operator reports it as null. Reaching for .name on one of
				// those throws, which is how reading a whole skin died on a single stale reference.
				if (!unityObject)
					return JValue.CreateNull();

				string path = AssetDatabase.GetAssetPath(unityObject);
				return string.IsNullOrEmpty(path) ? unityObject.name : path;
			}

			return _value.ToString();
		}

		private static HashSet<string> StringSet( JToken _token )
		{
			if (_token is not JArray array || array.Count == 0)
				return null;

			return new HashSet<string>(array.Select(_t => (string)_t), StringComparer.Ordinal);
		}

		private static bool IsPackageOwned( string _assetPath )
		{
			if (string.IsNullOrEmpty(_assetPath))
				return false;

			if (_assetPath.StartsWith("Packages/", StringComparison.Ordinal))
				return true;

			// The toolkit's own dev app has the package symlinked into Assets/, where the Packages/ test says
			// nothing — there the root dir is what separates "ours to edit" from "the shipped default".
			string root = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir();
			return !string.IsNullOrEmpty(root) && _assetPath.StartsWith(root, StringComparison.Ordinal);
		}

		private static void EnsureFolder( string _folder )
		{
			if (string.IsNullOrEmpty(_folder) || AssetDatabase.IsValidFolder(_folder))
				return;

			string parent = Path.GetDirectoryName(_folder)?.Replace('\\', '/');
			EnsureFolder(parent);
			AssetDatabase.CreateFolder(parent, Path.GetFileName(_folder));
		}

		#endregion
	}
}
