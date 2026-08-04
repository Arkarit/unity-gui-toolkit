using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Captures every serialized value of every component in a baked prefab into a temporary intermediate
	/// format, keyed by node path and Unity's own <c>propertyPath</c> strings.
	///
	/// Why this exists: the screen description is a curated vocabulary, so a human edit it cannot express is
	/// invisible to <c>read_screen</c> and to edit preservation — a font size and a gradient direction were both
	/// lost that way. A snapshot is complete by construction instead, which decouples "nothing gets lost" from
	/// "the description can say it".
	///
	/// This is a CLIPBOARD, not a format: it belongs under Library/ (never version-controlled), because a second
	/// place that describes a screen would compete with the description for being the source of truth.
	///
	/// Object references are recorded descriptively (asset path, or the node path they point at inside this
	/// prefab) so they can be ANALYSED, but they are marked and must never be written back blindly: a re-bake
	/// rebuilds the prefab with fresh object ids, and wiring belongs to the description via "#id".
	/// </summary>
	public static class UiPrefabValueSnapshot
	{
		private const string SnapshotDir = "Library/UiToolkit/ValueSnapshots";

		/// <summary>Marks a captured value as an object reference rather than plain data.</summary>
		public const string RefKey = "__ref";

		// Identity and bookkeeping: never a human's edit, and meaningless once the prefab is rebuilt.
		private static readonly HashSet<string> s_skippedPaths = new()
		{
			"m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
			"m_GameObject", "m_Script", "m_EditorClassIdentifier", "m_EditorHideFlags", "m_Name",
			"m_Father", "m_Children", "m_RootOrder", "m_LocalEulerAnglesHint",
		};

		// Derived data a component recomputes for itself. TMP's text info alone holds per-character arrays, so
		// capturing it would bury the real edits in noise and bloat the snapshot for nothing.
		private static readonly string[] s_skippedPrefixes =
		{
			"m_textInfo", "m_mesh", "m_RenderedWidth", "m_RenderedHeight", "m_PreferredValues",
			"m_TextComponent.", "m_SubTextObjects", "m_MaterialReferences", "m_FontFeatures",
		};

		public static string SnapshotPathFor( string _prefabPath ) =>
			$"{SnapshotDir}/{Path.GetFileNameWithoutExtension(_prefabPath)}.values.json";

		/// <summary>
		/// Captures <paramref name="_prefabPath"/> and writes the snapshot next to the others under Library/.
		/// Returns a summary: where it went plus what it holds, so a caller can judge the result without
		/// reading the file.
		/// </summary>
		public static JObject Capture( string _prefabPath )
		{
			var snapshot = BuildSnapshot(_prefabPath, out var summary);

			string snapshotPath = SnapshotPathFor(_prefabPath);
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(snapshotPath)));
			File.WriteAllText(Path.GetFullPath(snapshotPath), snapshot.ToString(Newtonsoft.Json.Formatting.Indented));

			summary["prefab"] = _prefabPath;
			summary["snapshotPath"] = snapshotPath;
			summary["byteSize"] = new FileInfo(Path.GetFullPath(snapshotPath)).Length;
			return summary;
		}

		/// <summary>
		/// Builds a snapshot in memory without writing it. The restore side needs the prefab's CURRENT state as
		/// its comparison baseline, and it must be expressed in exactly the same terms as the stored snapshot —
		/// so both sides come from this one method rather than from two readers that could drift apart.
		/// </summary>
		internal static JObject BuildSnapshot( string _prefabPath, out JObject _summary )
		{
			if (AssetDatabase.LoadAssetAtPath<GameObject>(_prefabPath) == null)
				throw new ArgumentException($"No prefab at '{_prefabPath}'.");

			var nodes = new JArray();
			int componentCount = 0, valueCount = 0, refCount = 0, emptyComponentCount = 0;
			var missingScripts = new JArray();

			// LoadPrefabContents, not the asset object: loading the asset directly did not expose every component
			// (a UiSimpleAnimation was simply absent from the walk), and this is the API the reader already uses to
			// inspect a prefab. It realises the prefab in a temporary scene, so nested instances behave like
			// ordinary objects — hence the finally.
			GameObject prefab = PrefabUtility.LoadPrefabContents(_prefabPath);
			try
			{
				foreach (var (path, gameObject) in Walk(prefab.transform, ""))
				{
					var components = new JArray();
					foreach (var component in gameObject.GetComponents<Component>())
					{
						if (component == null)
						{
							// A null entry means the script behind that component could not be loaded. Reported rather
							// than skipped in silence: a snapshot that quietly omits components is exactly how a
							// UiSimpleAnimation went missing without anyone noticing.
							missingScripts.Add(string.IsNullOrEmpty(path) ? "<root>" : path);
							continue;
						}

						// Listed even when nothing was captured: that a component IS here is information of its own,
						// and hiding the empty ones once made a whole UiSimpleAnimation look absent.
						var values = CaptureComponent(component, ref refCount);
						components.Add(new JObject
						{
							["type"] = component.GetType().Name,
							["values"] = values,
						});
						componentCount++;
						valueCount += values.Count;
						if (values.Count == 0)
							emptyComponentCount++;
					}

					nodes.Add(new JObject { ["path"] = path, ["components"] = components });
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(prefab);
			}

			_summary = new JObject
			{
				["nodes"] = nodes.Count,
				["components"] = componentCount,
				["values"] = valueCount,
				["objectReferences"] = refCount,
				// A component nothing could be read from is worth seeing, not hiding: it means the capture does
				// not understand a type yet.
				["componentsWithoutValues"] = emptyComponentCount,
				["nodesWithUnloadableScripts"] = missingScripts,
			};

			return new JObject
			{
				["prefab"] = _prefabPath,
				["capturedAtUtc"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
				["nodes"] = nodes,
			};
		}

		/// <summary>Node paths relative to the prefab root, the root itself being "".</summary>
		private static IEnumerable<(string path, GameObject gameObject)> Walk( Transform _transform, string _path )
		{
			yield return (_path, _transform.gameObject);

			foreach (Transform child in _transform)
			{
				string childPath = string.IsNullOrEmpty(_path) ? child.name : $"{_path}/{child.name}";
				foreach (var entry in Walk(child, childPath))
					yield return entry;
			}
		}

		/// <summary>
		/// One component's serialized values, in the same shapes the snapshot uses. Exposed so the motion
		/// harvester reads animations through exactly this reader — a second one would drift, and then a
		/// harvested preset would not compare equal to what a snapshot sees.
		/// </summary>
		internal static JObject CaptureComponentValues( Component _component )
		{
			int ignored = 0;
			return CaptureComponent(_component, ref ignored);
		}

		private static JObject CaptureComponent( Component _component, ref int _refCount )
		{
			var values = new JObject();
			var serialized = new SerializedObject(_component);
			var property = serialized.GetIterator();

			// Next(), not NextVisible(): the latter is tied to what the Inspector would show, which silently
			// emptied whole components (a UiSimpleAnimation came back with nothing at all). A snapshot has to be
			// complete regardless of drawers, foldout state or [HideInInspector]. Descending yields nested values
			// as their own propertyPath entries ("m_gradient.key0.r"), so containers need no special handling.
			bool enterChildren = true;
			while (property.Next(enterChildren))
			{
				enterChildren = true;
				if (IsSkipped(property.propertyPath))
				{
					// Do not descend into a skipped container either; its children are just as derived.
					enterChildren = false;
					continue;
				}

				if (!TryReadValue(property, out var token, out bool isReference))
					continue;

				// A value that could be read is complete, so do not also descend into how Unity happens to
				// represent it. Descending captured every colour a second time as .r/.g/.b/.a and every vector as
				// .x/.y/.z/.w — but above all it turned each object reference into a bare .m_FileID integer, which
				// looks like ordinary data to anything downstream and would let raw object ids be written back.
				enterChildren = false;

				values[property.propertyPath] = token;
				if (isReference)
					_refCount++;
			}

			return values;
		}

		private static bool IsSkipped( string _propertyPath )
		{
			if (s_skippedPaths.Contains(_propertyPath))
				return true;
			foreach (string prefix in s_skippedPrefixes)
			{
				if (_propertyPath.StartsWith(prefix, StringComparison.Ordinal))
					return true;
			}
			return false;
		}

		private static bool TryReadValue( SerializedProperty _property, out JToken _token, out bool _isReference )
		{
			_token = null;
			_isReference = false;

			switch (_property.propertyType)
			{
				case SerializedPropertyType.Integer: _token = _property.longValue; return true;
				case SerializedPropertyType.Boolean: _token = _property.boolValue; return true;
				case SerializedPropertyType.Float: _token = _property.doubleValue; return true;
				case SerializedPropertyType.String: _token = _property.stringValue; return true;
				case SerializedPropertyType.Character: _token = _property.intValue; return true;
				case SerializedPropertyType.ArraySize: _token = _property.intValue; return true;
				case SerializedPropertyType.LayerMask: _token = _property.intValue; return true;

				// The name, not the index: an enum's numbering is an implementation detail, and a name survives
				// a reordered enum whereas a 3 silently becomes something else.
				case SerializedPropertyType.Enum:
				{
					// Resolved through the real field type first, because a [Flags] COMBINATION has no single
					// index — enumValueIndex gives nothing usable and the value came out as a bare 69632, which
					// is both unreadable and not something the baker would accept back. Enum.ToString renders it
					// as "ScaleX, Alpha", which is exactly the form authored descriptions use.
					var enumType = ResolveFieldType(_property);
					if (enumType != null && enumType.IsEnum)
					{
						_token = Enum.ToObject(enumType, _property.intValue).ToString();
						return true;
					}

					_token = _property.enumValueIndex >= 0 && _property.enumValueIndex < _property.enumNames.Length
						? _property.enumNames[_property.enumValueIndex]
						: (JToken)_property.intValue;
					return true;
				}

				case SerializedPropertyType.Color:
					_token = ColorToJson(_property.colorValue);
					return true;

				case SerializedPropertyType.Vector2: { var v = _property.vector2Value; _token = new JArray { v.x, v.y }; return true; }
				case SerializedPropertyType.Vector3: { var v = _property.vector3Value; _token = new JArray { v.x, v.y, v.z }; return true; }
				case SerializedPropertyType.Vector4: { var v = _property.vector4Value; _token = new JArray { v.x, v.y, v.z, v.w }; return true; }
				case SerializedPropertyType.Vector2Int: { var v = _property.vector2IntValue; _token = new JArray { v.x, v.y }; return true; }
				case SerializedPropertyType.Vector3Int: { var v = _property.vector3IntValue; _token = new JArray { v.x, v.y, v.z }; return true; }
				case SerializedPropertyType.Rect: { var r = _property.rectValue; _token = new JArray { r.x, r.y, r.width, r.height }; return true; }

				case SerializedPropertyType.Quaternion: { var q = _property.quaternionValue; _token = new JArray { q.x, q.y, q.z, q.w }; return true; }
				case SerializedPropertyType.Bounds: { var b = _property.boundsValue; _token = new JArray { b.center.x, b.center.y, b.center.z, b.size.x, b.size.y, b.size.z }; return true; }

				// Curves and gradients are LEAF properties as far as the iterator is concerned: NextVisible never
				// descends into them, so without an explicit case they vanish from the snapshot entirely — and
				// they are among the main reasons it exists, being awkward to express in a description.
				case SerializedPropertyType.AnimationCurve:
					_token = CurveToJson(_property.animationCurveValue);
					return _token != null;

				case SerializedPropertyType.Gradient:
					_token = GradientToJson(ReadGradient(_property));
					return _token != null;

				case SerializedPropertyType.ObjectReference:
					_isReference = true;
					_token = DescribeReference(_property.objectReferenceValue);
					return true;

				// Everything else (Generic containers, ManagedReference, gradients as a whole, ...) is either
				// descended into by the iterator or not restorable as a value anyway.
				default:
					return false;
			}
		}

		/// <summary>
		/// The declared type behind a property path, walked field by field. Needed because SerializedProperty
		/// exposes an enum's NAMES but not its values, so a flags combination cannot be decoded from it alone.
		/// Returns null for anything it cannot follow, and every caller has a fallback for that.
		/// </summary>
		private static Type ResolveFieldType( SerializedProperty _property )
		{
			Type type = _property.serializedObject.targetObject?.GetType();
			if (type == null)
				return null;

			foreach (string segment in _property.propertyPath.Split('.'))
			{
				// Array plumbing ("Array", "data[3]") is not worth following: no enum of interest sits behind it.
				if (segment == "Array" || segment.StartsWith("data[", StringComparison.Ordinal))
					return null;

				var field = FindFieldInHierarchy(type, segment);
				if (field == null)
					return null;
				type = field.FieldType;
			}
			return type;
		}

		private static System.Reflection.FieldInfo FindFieldInHierarchy( Type _type, string _name )
		{
			for (var t = _type; t != null && t != typeof(object); t = t.BaseType)
			{
				var field = t.GetField(_name, System.Reflection.BindingFlags.Instance |
					System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
					System.Reflection.BindingFlags.DeclaredOnly);
				if (field != null)
					return field;
			}
			return null;
		}

		// SerializedProperty exposes gradientValue only internally, so it has to be reached by reflection. If a
		// future Unity renames it, the gradient is simply left out of the snapshot rather than the capture failing.
		// Internal because the restore side needs the very same handle to write one back — one place to break.
		internal static readonly System.Reflection.PropertyInfo GradientValueProperty =
			typeof(SerializedProperty).GetProperty("gradientValue",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
				System.Reflection.BindingFlags.NonPublic);

		private static Gradient ReadGradient( SerializedProperty _property )
		{
			if (GradientValueProperty == null)
				return null;
			try { return GradientValueProperty.GetValue(_property) as Gradient; }
			catch { return null; }
		}

		/// <summary>
		/// Hex while hex is exact, four floats when it is not. Hex reads well in a diff and covers virtually every
		/// colour a human picks, but it is eight bits per channel — and since the walk no longer descends into a
		/// colour's float components, hex alone would quietly round whatever it cannot represent.
		/// </summary>
		private static JToken ColorToJson( Color _color )
		{
			string hex = "#" + ColorUtility.ToHtmlStringRGBA(_color);
			if (ColorUtility.TryParseHtmlString(hex, out var roundTripped) && roundTripped == _color)
				return hex;

			return new JArray { _color.r, _color.g, _color.b, _color.a };
		}

		private static JToken CurveToJson( AnimationCurve _curve )
		{
			if (_curve == null)
				return null;

			var keys = new JArray();
			foreach (var key in _curve.keys)
			{
				keys.Add(new JObject
				{
					["time"] = key.time, ["value"] = key.value,
					["inTangent"] = key.inTangent, ["outTangent"] = key.outTangent,
				});
			}
			return new JObject
			{
				["keys"] = keys,
				["preWrapMode"] = _curve.preWrapMode.ToString(),
				["postWrapMode"] = _curve.postWrapMode.ToString(),
			};
		}

		private static JToken GradientToJson( Gradient _gradient )
		{
			if (_gradient == null)
				return null;

			var colorKeys = new JArray();
			foreach (var key in _gradient.colorKeys)
				colorKeys.Add(new JObject { ["time"] = key.time, ["color"] = "#" + ColorUtility.ToHtmlStringRGB(key.color) });

			var alphaKeys = new JArray();
			foreach (var key in _gradient.alphaKeys)
				alphaKeys.Add(new JObject { ["time"] = key.time, ["alpha"] = key.alpha });

			return new JObject
			{
				["colorKeys"] = colorKeys,
				["alphaKeys"] = alphaKeys,
				["mode"] = _gradient.mode.ToString(),
				["colorSpace"] = _gradient.colorSpace.ToString(),
			};
		}

		/// <summary>
		/// Describes what a reference points at, for analysis only: an asset by path, an object inside the same
		/// prefab by its node path, or the type alone when neither applies.
		/// </summary>
		private static JToken DescribeReference( UnityEngine.Object _value )
		{
			if (_value == null)
				return new JObject { [RefKey] = null };

			string assetPath = AssetDatabase.GetAssetPath(_value);
			var asComponent = _value as Component;
			var asGameObject = _value as GameObject ?? (asComponent != null ? asComponent.gameObject : null);

			var result = new JObject { [RefKey] = _value.GetType().Name };
			if (!string.IsNullOrEmpty(assetPath))
				result["asset"] = assetPath;
			if (asGameObject != null)
				result["node"] = NodePathOf(asGameObject.transform);
			return result;
		}

		private static string NodePathOf( Transform _transform )
		{
			var parts = new List<string>();
			for (var t = _transform; t != null && t.parent != null; t = t.parent)
				parts.Insert(0, t.name);
			return string.Join("/", parts);
		}
	}
}
