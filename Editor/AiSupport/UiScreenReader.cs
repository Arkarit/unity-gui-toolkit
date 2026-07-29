using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GuiToolkit.Style;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// The inverse of <see cref="UiScreenBaker"/>: reads a baked (or hand-built) <c>.prefab</c> back into a
	/// screen-JSON description in the same shape <c>bake_screen</c> consumes, so an author can inspect what
	/// is in a prefab, tweak it, and re-bake — and so the edit-preserving re-bake has a "current state" to
	/// diff against.
	///
	/// Read-back is BEST-EFFORT and structural, not a byte-perfect inverse:
	/// <list type="bullet">
	/// <item>Template (nested-prefab-instance) nodes emit <c>template</c> = the source's standard-element
	/// key (so the registry re-resolves the client variant) or its prefab name; their props are the
	/// instance's property-override set (exactly what the author changed).</item>
	/// <item>Element nodes emit <c>type</c> = the primary catalogued component; their props are the fields
	/// that differ from a fresh default of that component.</item>
	/// <item>Cross-node references are re-expressed as <c>"#id"</c> using synthesized, stable ids; a
	/// reference into a template's internal part (not an authored node) cannot be named and is dropped
	/// with a warning.</item>
	/// </list>
	///
	/// Marked <c>[EditorAware]</c> for the same reason as the baker (touches gated toolkit singletons).
	/// </summary>
	[EditorAware]
	public static class UiScreenReader
	{
		private static List<string> s_warnings;

		// Reference props deferred until every node has an id (a ref can point forward to a later node).
		private class DeferredRef
		{
			public JObject propsBag;     // the node's "props" object the resolved "#id" is written into
			public string key;           // the authoring prop name
			public List<UnityEngine.Object> targets = new(); // 1 (single ref) or N (list/array)
			public bool isList;
		}

		private static List<DeferredRef> s_deferredRefs;
		private static Dictionary<GameObject, string> s_idByAuthoredGo;
		private static readonly Dictionary<Type, Component> s_defaultComponents = new();
		private static GameObject s_defaultsHost;

		/// <summary>Result of a read-back: the screen JSON plus any non-fatal warnings.</summary>
		public class ReadResult
		{
			public JObject screen;
			public List<string> warnings = new();
		}

		private static void Warn( string _message )
		{
			s_warnings?.Add(_message);
			UiLog.LogWarning(_message);
		}

		#region Public API

		/// <summary>
		/// Reads a prefab into a screen-JSON object (+ warnings). <paramref name="_source"/> selects where the
		/// JSON comes from:
		/// <list type="bullet">
		/// <item><c>auto</c> (default) — the source-JSON sidecar written at bake time if present (a perfect,
		/// clean authoring round-trip), else a best-effort structural read-back of the prefab.</item>
		/// <item><c>sidecar</c> — the sidecar only; errors if there is none.</item>
		/// <item><c>structural</c> — always read the prefab's current state (includes later hand edits); this
		/// is what the edit-preserving re-bake diffs against.</item>
		/// </list>
		/// </summary>
		public static ReadResult Read( string _prefabPath, string _source = "auto" )
		{
			if (string.IsNullOrWhiteSpace(_prefabPath))
				throw new ArgumentException("Empty prefab path.");

			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath);
			if (asset == null)
				throw new ArgumentException($"No prefab asset at '{_prefabPath}'.");

			string source = (_source ?? "auto").Trim().ToLowerInvariant();
			string sidecarPath = UiScreenBaker.SidecarPathFor(_prefabPath);
			bool sidecarExists = System.IO.File.Exists(System.IO.Path.GetFullPath(sidecarPath));

			if (source == "sidecar" && !sidecarExists)
				throw new ArgumentException($"No source sidecar '{sidecarPath}' (this prefab was not baked, or predates sidecars). Use source 'structural'.");

			if (source != "structural" && sidecarExists)
				return ReadFromSidecar(sidecarPath);

			s_warnings = new List<string>();
			s_deferredRefs = new List<DeferredRef>();
			s_idByAuthoredGo = new Dictionary<GameObject, string>();
			var usedIds = new HashSet<string>(StringComparer.Ordinal);

			GameObject root = PrefabUtility.LoadPrefabContents(_prefabPath);
			try
			{
				var rootNode = BuildNode(root, usedIds);
				ResolveDeferredRefs();

				var screen = new JObject
				{
					["name"] = System.IO.Path.GetFileNameWithoutExtension(_prefabPath),
					["root"] = rootNode,
				};
				return new ReadResult { screen = screen, warnings = s_warnings };
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
				ClearDefaults();
			}
		}

		/// <summary>Reads a prefab and returns the pretty-printed screen JSON string.</summary>
		public static string ReadToJson( string _prefabPath )
			=> Read(_prefabPath).screen.ToString(Formatting.Indented);

		// Returns the clean authoring JSON that was baked, straight from the sidecar — no structural guessing.
		private static ReadResult ReadFromSidecar( string _sidecarPath )
		{
			string json = System.IO.File.ReadAllText(System.IO.Path.GetFullPath(_sidecarPath));
			JObject screen;
			try { screen = JObject.Parse(json); }
			catch (Exception e) { throw new Exception($"Source sidecar '{_sidecarPath}' is not valid JSON: {e.Message}"); }
			return new ReadResult { screen = screen, warnings = new List<string>() };
		}

		[MenuItem(StringConstants.AI_READ_SELECTED_PREFAB_MENU_NAME)]
		private static void ReadSelectedMenu()
		{
			var go = Selection.activeObject as GameObject;
			string path = go != null ? AssetDatabase.GetAssetPath(go) : null;
			if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
			{
				UiLog.LogError("Select a prefab asset to read back.");
				return;
			}

			try
			{
				var result = Read(path);
				UiLog.LogInternal($"read_screen '{path}':\n{result.screen.ToString(Formatting.Indented)}");
			}
			catch (Exception e)
			{
				UiLog.LogError($"Read Selected Prefab failed: {e.Message}\n{e.StackTrace}");
			}
		}

		#endregion

		#region Node building

		private static JObject BuildNode( GameObject _go, HashSet<string> _usedIds )
		{
			var node = new JObject();
			string id = AssignId(_go, _usedIds);
			node["id"] = id;

			GameObject templateSource = TemplateSourceOf(_go);
			bool isTemplate = templateSource != null;

			var authorableComponents = new List<Component>();

			if (isTemplate)
			{
				node["template"] = TemplateName(templateSource);
			}
			else
			{
				var primary = PickPrimaryAndExtras(_go, out var extras);
				if (primary == null)
				{
					// A pure text node has no catalogued component of its own (TMP_Text is deliberately not
					// authorable — text travels in the "text" field). Emit the text component as the type
					// anyway, so a re-bake recreates something the "text" below can actually be applied to.
					var tmpText = _go.GetComponent<TMPro.TMP_Text>();
					if (tmpText != null)
					{
						node["type"] = tmpText.GetType().Name;
					}
					else
					{
						// Nothing catalogable — still emit a bare node so structure is not lost.
						Warn($"Node '{_go.name}': no catalogued component found; emitting a bare node.");
						node["type"] = "RectTransform";
					}
				}
				else
				{
					node["type"] = primary.GetType().Name;
					authorableComponents.Add(primary);
					if (extras.Count > 0)
					{
						node["components"] = new JArray(extras.Select(c => (JToken)c.GetType().Name));
						authorableComponents.AddRange(extras);
					}
				}
			}

			// A friendly name only when it adds information (differs from the id and the identity).
			string identityName = isTemplate ? (string)node["template"] : (string)node["type"];
			if (!string.IsNullOrEmpty(_go.name) && _go.name != id && _go.name != identityName)
				node["name"] = _go.name;

			var rect = ReadRect(_go);
			if (rect != null)
				node["rect"] = rect;

			var props = ReadProps(_go, isTemplate, templateSource, authorableComponents);
			if (props != null && props.Count > 0)
				node["props"] = props;

			string style = ReadStyle(_go, isTemplate, templateSource);
			if (style != null)
				node["style"] = style;

			string text = ReadText(_go, isTemplate, templateSource);
			if (text != null)
				node["text"] = text;

			var scroll = ReadScroll(_go);
			if (scroll != null)
				node["scroll"] = scroll;

			var children = ReadChildren(_go, isTemplate);
			if (children.Count > 0)
				node["children"] = new JArray(children.Select(c => BuildNode(c, _usedIds)));

			return node;
		}

		// A stable, unique id derived from the GameObject name (or its identity), so re-expressed "#id"
		// references stay readable and the JSON re-bakes deterministically.
		private static string AssignId( GameObject _go, HashSet<string> _usedIds )
		{
			string basis = string.IsNullOrEmpty(_go.name) ? "node" : _go.name;
			basis = new string(basis.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray()).Trim('_');
			if (string.IsNullOrEmpty(basis))
				basis = "node";

			string id = basis;
			int n = 2;
			while (!_usedIds.Add(id))
				id = $"{basis}_{n++}";

			s_idByAuthoredGo[_go] = id;
			return id;
		}

		#endregion

		#region Template / element classification

		// The source prefab this GameObject is the instance-root of (i.e. it is a template node), or null if
		// it is a plain authored GameObject. Only the instance ROOT counts — internal parts of a nested
		// prefab return the outer instance and are never visited as nodes (see ReadChildren).
		private static GameObject TemplateSourceOf( GameObject _go )
		{
			if (!PrefabUtility.IsAnyPrefabInstanceRoot(_go))
				return null;
			var src = PrefabUtility.GetCorrespondingObjectFromSource(_go);
			return src;
		}

		private static string TemplateName( GameObject _source )
		{
			var marker = _source.GetComponent<UiStandardElement>();
			if (marker != null && marker.Element != EStandardElement.None)
				return marker.Key; // standard-element identity → registry re-resolves the client variant
			return _source.name;
		}

		// The authored children to recurse into. For a plain element every transform child is authored. For
		// a template only the ADDED GameObjects are authored (its internal hierarchy belongs to the source);
		// we return the top-most added roots, in hierarchy order.
		private static List<GameObject> ReadChildren( GameObject _go, bool _isTemplate )
		{
			if (!_isTemplate)
			{
				var result = new List<GameObject>();
				foreach (Transform child in _go.transform)
					result.Add(child.gameObject);
				return result;
			}

			var added = PrefabUtility.GetAddedGameObjects(_go)
				.Select(a => a.instanceGameObject)
				.Where(g => g != null)
				.ToList();

			// Keep only the top-most added roots (an added subtree lists its inner GOs too), then order them
			// by their position in the hierarchy for stable, readable output.
			var addedSet = new HashSet<GameObject>(added);
			var tops = added.Where(g => g.transform.parent == null || !addedSet.Contains(g.transform.parent.gameObject)).ToList();
			tops.Sort(( a, b ) => HierarchyOrder(a).CompareTo(HierarchyOrder(b)));
			return tops;
		}

		private static int HierarchyOrder( GameObject _go )
		{
			// Cheap stable-ish ordering by depth then sibling index.
			int order = 0, depth = 0;
			for (var t = _go.transform; t != null; t = t.parent)
			{
				order += t.GetSiblingIndex();
				depth++;
			}
			return order + depth * 1000;
		}

		// Picks the primary catalogued component of an element node plus any extra stacked components,
		// skipping infrastructure (Transform, the view Canvas trio, style appliers, text components) and
		// RequireComponent dependencies of another candidate.
		private static Component PickPrimaryAndExtras( GameObject _go, out List<Component> _extras )
		{
			_extras = new List<Component>();

			var candidates = _go.GetComponents<Component>()
				.Where(c => c != null && IsAuthorableComponent(c))
				.ToList();
			if (candidates.Count == 0)
				return null;

			// Drop components that are RequireComponent-required by another candidate (they are dependencies,
			// re-added automatically on bake).
			var required = new HashSet<Type>();
			foreach (var c in candidates)
				foreach (var req in RequiredTypes(c.GetType()))
					required.Add(req);

			var top = candidates.Where(c => !required.Contains(c.GetType())).ToList();
			if (top.Count == 0)
				top = candidates; // all mutually required — keep them all rather than lose the node

			// Annotations describe a node, they are never its identity: they must be read back (a screen has to
			// author its own standard-element marker, or a re-bake drops it), but if one won the primary pick the
			// node would come back typed as the annotation instead of as the widget it actually is.
			var primaryCandidates = top.Where(c => !IsAnnotationComponent(c)).ToList();
			if (primaryCandidates.Count == 0)
				return null; // annotations only — nothing here that makes a node

			var primary = primaryCandidates[0];
			_extras = top.Where(c => c != primary).ToList();
			return primary;
		}

		// Components that annotate a node rather than make one. Authorable and read back, but never a node's "type".
		private static bool IsAnnotationComponent( Component _c ) => _c is UiStandardElement || _c is UiComment;

		private static bool IsAuthorableComponent( Component _c )
		{
			if (_c is Transform) return false;
			if (_c is Canvas || _c is CanvasScaler || _c is GraphicRaycaster) return false;
			// Pure render infrastructure, auto-added via Graphic's RequireComponent. Without this it becomes
			// the sole candidate on a text node (TMP_Text is filtered below) and wins the primary pick.
			if (_c is CanvasRenderer) return false;
			if (_c is UiAbstractApplyStyleBase) return false;
			if (_c is TMPro.TMP_Text) return false;
			return true;
		}

		private static IEnumerable<Type> RequiredTypes( Type _type )
		{
			foreach (var attr in _type.GetCustomAttributes(typeof(RequireComponent), true).Cast<RequireComponent>())
			{
				if (attr.m_Type0 != null) yield return attr.m_Type0;
				if (attr.m_Type1 != null) yield return attr.m_Type1;
				if (attr.m_Type2 != null) yield return attr.m_Type2;
			}
		}

		#endregion

		#region Rect

		private static JObject ReadRect( GameObject _go )
		{
			if (_go.transform is not RectTransform rt)
				return null;

			var rect = new JObject
			{
				["anchorMin"] = Vec(rt.anchorMin),
				["anchorMax"] = Vec(rt.anchorMax),
				["pivot"] = Vec(rt.pivot),
				["size"] = Vec(rt.sizeDelta),
				["position"] = Vec(rt.anchoredPosition),
			};
			return rect;
		}

		private static JArray Vec( Vector2 _v ) => new() { Round(_v.x), Round(_v.y) };

		private static float Round( float _f ) => Mathf.Round(_f * 1000f) / 1000f;

		#endregion

		#region Props

		private static JObject ReadProps( GameObject _go, bool _isTemplate, GameObject _templateSource, List<Component> _authorable )
		{
			var props = new JObject();

			// Authored props = fields whose value differs from a REFERENCE. For a template that reference is
			// the instance's source component (so overrides = exactly what the author changed — robust, no
			// dependence on PrefabUtility's modification-target matching); for an element it is a fresh default
			// of the component type. Only the node's own root-GO components are considered (deep overrides are
			// not re-bakeable as node props). Object-ref props are read only for elements — a template's
			// internal refs are neither authored intent nor re-bakeable.
			var components = _isTemplate
				? _go.GetComponents<Component>().Where(c => c != null && IsAuthorableComponent(c)).ToList()
				: _authorable;

			foreach (var component in components)
			{
				Component reference = _isTemplate
					? PrefabUtility.GetCorrespondingObjectFromSource(component) as Component
					: GetDefaultComponent(component.GetType());

				foreach (var field in SerializedFields(component.GetType()))
				{
					if (_isTemplate && IsObjectRefType(field.FieldType))
						continue;
					if (reference != null && ValuesEqual(field.GetValue(component), field.GetValue(reference)))
						continue;
					TryEmitField(component, field.Name, props);
				}
			}

			return props;
		}

		// Reads a component field's live value and writes it into the props bag under a friendly key
		// ("m_foo" → "foo"), deferring object references to "#id". Unsupported types are skipped silently
		// (they round-trip via the prefab itself, not the JSON).
		private static void TryEmitField( Component _component, string _fieldName, JObject _props )
		{
			var field = FindField(_component.GetType(), _fieldName);
			if (field == null)
				return;
			if (ShouldSkipField(_component, field))
				return;

			string key = _fieldName.StartsWith("m_", StringComparison.Ordinal) ? _fieldName.Substring(2) : _fieldName;
			key = char.ToLowerInvariant(key[0]) + key.Substring(1);
			if (_props.ContainsKey(key))
				return;

			object value = field.GetValue(_component);
			Type type = field.FieldType;

			// Object references (single or list/array) → deferred "#id".
			if (IsObjectRefType(type))
			{
				DeferReference(_props, key, value, type);
				return;
			}

			if (TryEmitSimple(value, type, out JToken token))
				_props[key] = token;
		}

		private static bool ShouldSkipField( Component _component, FieldInfo _field )
		{
			// Text / loca fields are represented by "text"; the style applier name by "style".
			if (_component is UiLocalizedTextMeshProUGUI &&
			    (_field.Name is "m_locaKey" or "m_text" or "m_isTranslated"))
				return true;
			return false;
		}

		private static bool TryEmitSimple( object _value, Type _type, out JToken _token )
		{
			_token = null;

			if (_type == typeof(string)) { _token = (string)_value; return true; }
			if (_type == typeof(bool)) { _token = (bool)_value; return true; }
			if (_type.IsEnum) { _token = _value.ToString(); return true; }

			if (_type == typeof(int) || _type == typeof(short) || _type == typeof(byte) || _type == typeof(sbyte)
			    || _type == typeof(ushort) || _type == typeof(long) || _type == typeof(uint) || _type == typeof(ulong))
			{ _token = Convert.ToInt64(_value, CultureInfo.InvariantCulture); return true; }
			if (_type == typeof(float)) { _token = Round((float)_value); return true; }
			if (_type == typeof(double)) { _token = (double)_value; return true; }

			if (_type == typeof(Color)) { _token = "#" + ColorUtility.ToHtmlStringRGBA((Color)_value); return true; }
			if (_type == typeof(Color32)) { _token = "#" + ColorUtility.ToHtmlStringRGBA((Color32)_value); return true; }

			if (_type == typeof(Vector2)) { _token = Vec((Vector2)_value); return true; }
			if (_type == typeof(Vector3)) { var v = (Vector3)_value; _token = new JArray { Round(v.x), Round(v.y), Round(v.z) }; return true; }
			if (_type == typeof(Vector4)) { var v = (Vector4)_value; _token = new JArray { Round(v.x), Round(v.y), Round(v.z), Round(v.w) }; return true; }

			if (_type == typeof(AnimationCurve)) { _token = CurveToJson((AnimationCurve)_value); return true; }
			if (_type == typeof(Gradient)) { _token = GradientToJson((Gradient)_value); return true; }
			if (_type == typeof(RectOffset))
			{
				var p = (RectOffset)_value;
				_token = new JArray { p.left, p.right, p.top, p.bottom };
				return true;
			}

			if (typeof(Sprite).IsAssignableFrom(_type))
			{
				var sprite = _value as Sprite;
				if (sprite == null) return false;
				_token = AssetDatabase.GetAssetPath(sprite);
				return _token != null;
			}

			return false;
		}

		private static JToken CurveToJson( AnimationCurve _curve )
		{
			if (_curve == null)
				return null;
			var keys = new JArray();
			foreach (var k in _curve.keys)
				keys.Add(new JObject
				{
					["time"] = Round(k.time),
					["value"] = Round(k.value),
					["inTangent"] = Round(k.inTangent),
					["outTangent"] = Round(k.outTangent),
				});
			return new JObject { ["keys"] = keys };
		}

		// Colour and alpha keys are separate in a Gradient, so they stay separate here — collapsing them would
		// lose stops that only one of the two tracks has.
		private static JToken GradientToJson( Gradient _gradient )
		{
			if (_gradient == null)
				return null;

			var colorKeys = new JArray();
			foreach (var k in _gradient.colorKeys)
				colorKeys.Add(new JObject { ["time"] = Round(k.time), ["color"] = "#" + ColorUtility.ToHtmlStringRGB(k.color) });

			var alphaKeys = new JArray();
			foreach (var k in _gradient.alphaKeys)
				alphaKeys.Add(new JObject { ["time"] = Round(k.time), ["alpha"] = Round(k.alpha) });

			return new JObject
			{
				["colorKeys"] = colorKeys,
				["alphaKeys"] = alphaKeys,
				["mode"] = _gradient.mode.ToString(),
			};
		}

		#endregion

		#region References

		private static bool IsObjectRefType( Type _type )
		{
			if (typeof(UnityEngine.Object).IsAssignableFrom(_type) && !typeof(Sprite).IsAssignableFrom(_type))
				return true;
			Type element = ElementType(_type);
			return element != null && typeof(UnityEngine.Object).IsAssignableFrom(element) && !typeof(Sprite).IsAssignableFrom(element);
		}

		private static Type ElementType( Type _type )
		{
			if (_type.IsArray)
				return _type.GetElementType();
			if (_type.IsGenericType && _type.GetGenericTypeDefinition() == typeof(List<>))
				return _type.GetGenericArguments()[0];
			return null;
		}

		private static void DeferReference( JObject _props, string _key, object _value, Type _type )
		{
			var deferred = new DeferredRef { propsBag = _props, key = _key };

			if (ElementType(_type) != null)
			{
				deferred.isList = true;
				if (_value is IEnumerable list)
					foreach (var item in list)
						deferred.targets.Add(item as UnityEngine.Object);
			}
			else
			{
				deferred.targets.Add(_value as UnityEngine.Object);
			}

			// Nothing to reference (all null) → skip entirely.
			if (deferred.targets.All(t => t == null))
				return;

			s_deferredRefs.Add(deferred);
		}

		private static void ResolveDeferredRefs()
		{
			foreach (var deferred in s_deferredRefs)
			{
				var ids = new List<string>();
				foreach (var target in deferred.targets)
				{
					string id = IdForObject(target);
					if (id != null)
						ids.Add("#" + id);
					else if (target != null)
						Warn($"Reference '{deferred.key}' → '{target}': target is not an authored node; dropped.");
				}

				if (ids.Count == 0)
					continue;

				deferred.propsBag[deferred.key] = deferred.isList ? new JArray(ids.Cast<object>().ToArray()) : (JToken)ids[0];
			}
		}

		private static string IdForObject( UnityEngine.Object _obj )
		{
			GameObject go = _obj switch
			{
				GameObject g => g,
				Component c => c.gameObject,
				_ => null,
			};
			if (go == null)
				return null;
			return s_idByAuthoredGo.TryGetValue(go, out var id) ? id : null;
		}

		#endregion

		#region Style / Text / Scroll

		private static string ReadStyle( GameObject _go, bool _isTemplate, GameObject _templateSource )
		{
			var applier = _go.GetComponent<UiAbstractApplyStyleBase>();
			if (applier == null)
				return null;

			// On a template node only report the style if it was overridden (else it is the template's own).
			if (_isTemplate)
			{
				var src = PrefabUtility.GetCorrespondingObjectFromSource(applier) as UiAbstractApplyStyleBase;
				if (src != null && src.Name == applier.Name)
					return null;
			}

			return string.IsNullOrEmpty(applier.Name) ? null : applier.Name;
		}

		private static string ReadText( GameObject _go, bool _isTemplate, GameObject _templateSource )
		{
			var localized = FindScopedText(_go, _isTemplate);
			if (localized == null)
				return null;

			// On a template only report text that was overridden (else it is the template default). Detect the
			// override by comparing the instance's text/loca fields to its source component — robust across
			// nested prefabs, unlike matching PrefabUtility modification targets by reference.
			if (_isTemplate && !TextIsOverridden(localized))
				return null;

			bool isTranslated = GetPrivate<bool>(localized, "m_isTranslated");
			if (!isTranslated)
				return "@text:" + localized.text;

			string key = GetPrivate<string>(localized, "m_locaKey");
			return string.IsNullOrEmpty(key) ? null : "@loca:" + key;
		}

		private static bool TextIsOverridden( UiLocalizedTextMeshProUGUI _instance )
		{
			var src = PrefabUtility.GetCorrespondingObjectFromSource(_instance) as UiLocalizedTextMeshProUGUI;
			if (src == null)
				return true; // not from a template source → authored

			return GetPrivate<bool>(_instance, "m_isTranslated") != GetPrivate<bool>(src, "m_isTranslated")
			    || GetPrivate<string>(_instance, "m_locaKey") != GetPrivate<string>(src, "m_locaKey")
			    || (_instance.text ?? "") != (src.text ?? "");
		}

		// The text component that belongs to THIS node's own scope — i.e. not inside a descendant authored
		// child node (whose text is that node's business). Prevents a parent from stealing a child button's
		// label. For an element node every transform child is an authored node, so this collapses to the
		// node's own GameObject; for a template it reaches into the template's internal parts.
		private static UiLocalizedTextMeshProUGUI FindScopedText( GameObject _go, bool _isTemplate )
		{
			var authoredChildren = new HashSet<Transform>(
				ReadChildren(_go, _isTemplate).Select(g => g.transform));

			foreach (var candidate in _go.GetComponentsInChildren<UiLocalizedTextMeshProUGUI>(true))
			{
				if (!IsUnderAny(candidate.transform, authoredChildren, _go.transform))
					return candidate;
			}
			return null;
		}

		// True if _t is, or is a descendant of, any transform in _boundary (searching up to and including
		// _stopAt).
		private static bool IsUnderAny( Transform _t, HashSet<Transform> _boundary, Transform _stopAt )
		{
			for (var t = _t; t != null; t = t.parent)
			{
				if (_boundary.Contains(t))
					return true;
				if (t == _stopAt)
					break;
			}
			return false;
		}

		private static JObject ReadScroll( GameObject _go )
		{
			var scrollRect = _go.GetComponent<ScrollRect>();
			if (scrollRect == null || scrollRect.content == null)
				return null;

			var content = scrollRect.content;
			string direction =
				scrollRect.horizontal && scrollRect.vertical ? "both" :
				scrollRect.horizontal ? "horizontal" : "vertical";

			var scroll = new JObject { ["direction"] = direction };

			if (content.GetComponent<GridLayoutGroup>() != null) scroll["layout"] = "grid";
			else if (content.GetComponent<VerticalLayoutGroup>() != null) scroll["layout"] = "vertical";
			else if (content.GetComponent<HorizontalLayoutGroup>() != null) scroll["layout"] = "horizontal";
			else scroll["layout"] = "none";

			scroll["fit"] = content.GetComponent<ContentSizeFitter>() != null;
			return scroll;
		}

		#endregion

		#region Reflection helpers

		private static IEnumerable<FieldInfo> SerializedFields( Type _type )
		{
			for (var t = _type; t != null && t != typeof(object); t = t.BaseType)
			{
				var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public |
				                         BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				foreach (var f in fields)
				{
					bool serialized = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null;
					bool ignored = f.GetCustomAttribute<System.NonSerializedAttribute>() != null;
					if (serialized && !ignored && !f.IsStatic)
						yield return f;
				}
			}
		}

		private static FieldInfo FindField( Type _type, string _name )
		{
			for (var t = _type; t != null && t != typeof(object); t = t.BaseType)
			{
				var f = t.GetField(_name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (f != null)
					return f;
			}
			return null;
		}

		private static T GetPrivate<T>( object _target, string _fieldName )
		{
			var f = FindField(_target.GetType(), _fieldName);
			return f != null && f.GetValue(_target) is T value ? value : default;
		}

		private static bool ValuesEqual( object _a, object _b )
		{
			if (ReferenceEquals(_a, _b)) return true;
			// Treat a null string and "" as equal — a fresh AddComponent leaves strings null while a
			// serialized prefab stores "", which would otherwise read as a spurious authored prop.
			if (_a == null) return _b is string sb && sb.Length == 0;
			if (_b == null) return _a is string sa && sa.Length == 0;
			return _a.Equals(_b);
		}

		// A throwaway default instance of a component type, used to tell which element-node fields the author
		// changed. Cached on a shared hidden host and torn down after the read.
		private static Component GetDefaultComponent( Type _type )
		{
			if (s_defaultComponents.TryGetValue(_type, out var cached))
				return cached;

			Component component = null;
			try
			{
				if (s_defaultsHost == null)
				{
					s_defaultsHost = new GameObject("~UiScreenReaderDefaults", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
				}
				var host = new GameObject("~default", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
				host.transform.SetParent(s_defaultsHost.transform, false);
				component = host.GetComponent(_type) ?? host.AddComponent(_type);
			}
			catch (Exception e)
			{
				Warn($"Could not build a default '{_type.Name}' for prop diffing ({e.Message}); its props may be over-reported.");
			}

			s_defaultComponents[_type] = component;
			return component;
		}

		private static void ClearDefaults()
		{
			s_defaultComponents.Clear();
			if (s_defaultsHost != null)
			{
				UnityEngine.Object.DestroyImmediate(s_defaultsHost);
				s_defaultsHost = null;
			}
		}

		#endregion
	}
}
