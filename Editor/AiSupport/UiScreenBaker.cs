using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using GuiToolkit.Style;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Milestone 2 of the AI screen-authoring effort: turns a JSON screen description (authored against
	/// the <see cref="UiScreenCatalog"/> vocabulary) into a real <c>.prefab</c> asset that the team can
	/// hand-edit afterward.
	///
	/// The toolkit's widgets are not self-contained, so screens are composed from two node kinds:
	/// <list type="bullet">
	/// <item><b>template</b> — instantiates a ready-made palette prefab (StandardButton, panel background,
	/// ...) as a nested prefab instance, keeping the link so template edits propagate.</item>
	/// <item><b>element</b> — creates a bare GameObject and adds a catalogued component (a UiView root,
	/// a layout group, a plain panel).</item>
	/// </list>
	///
	/// Runs entirely in Edit Mode (no Play Mode → no <see cref="EditorApplication.isPlaying"/> side
	/// effects, and the baked prefab can be screenshotted for the AI preview loop).
	///
	/// Marked <c>[EditorAware]</c>: baking touches toolkit singletons (e.g.
	/// <see cref="UiToolkitConfiguration"/>) which are gated behind editor-awareness. The baker is only
	/// ever entered from a menu item or an MCP request on the main thread, i.e. when assets are ready.
	/// </summary>
	[EditorAware]
	public static class UiScreenBaker
	{
		private const string OutputDir = "Assets/AiSupport/Generated";

		// Literal-text escape: bypasses localization on an otherwise-localized text component.
		private const string LiteralTextPrefix = "@text:";
		// Optional, purely cosmetic prefix an author may put on a loca key.
		private const string LocaKeyPrefix = "@loca:";

		private static Dictionary<string, Type> s_componentTypesByName;
		private static Dictionary<Type, Type> s_applierByTargetType;

		/// <summary>Project-relative folder the baked prefabs are written to.</summary>
		public static string GeneratedDir => OutputDir;

		#region Public API

		/// <summary>
		/// Bakes a screen described by <paramref name="_screenJson"/> into a prefab asset and returns
		/// its project-relative path. Throws on malformed input so the caller (menu / MCP bridge) can
		/// surface a precise message.
		/// </summary>
		public static string Bake( string _screenJson )
		{
			if (string.IsNullOrWhiteSpace(_screenJson))
				throw new ArgumentException("Empty screen JSON.");

			JObject screen;
			try
			{
				screen = JObject.Parse(_screenJson);
			}
			catch (Exception e)
			{
				throw new ArgumentException($"Screen JSON is not valid JSON: {e.Message}");
			}

			string name = (string)screen["name"];
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Screen JSON must have a non-empty \"name\".");

			var rootNode = screen["root"] as JObject;
			if (rootNode == null)
				throw new ArgumentException("Screen JSON must have a \"root\" node object.");

			ResetCaches();

			GameObject rootGo = null;
			try
			{
				rootGo = BuildNode(rootNode, null);

				EditorFileUtility.EnsureUnityFolderExists(OutputDir);
				string safeName = EditorFileUtility.GetSafeFileName(name);
				string path = $"{OutputDir}/{safeName}.prefab";

				var saved = PrefabUtility.SaveAsPrefabAsset(rootGo, path, out bool success);
				if (!success || saved == null)
					throw new Exception($"PrefabUtility.SaveAsPrefabAsset failed for '{path}'.");

				AssetDatabase.Refresh();
				UiLog.LogInternal($"Baked screen '{name}' → '{path}'.");
				return path;
			}
			finally
			{
				if (rootGo != null)
					UnityEngine.Object.DestroyImmediate(rootGo);
			}
		}

		[MenuItem(StringConstants.AI_BAKE_TEST_DIALOG_MENU_NAME)]
		private static void BakeTestDialogMenu()
		{
			try
			{
				string path = Bake(TestDialogJson);
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				EditorGUIUtility.PingObject(asset);
				Selection.activeObject = asset;
			}
			catch (Exception e)
			{
				UiLog.LogError($"Bake Test Dialog failed: {e.Message}\n{e.StackTrace}");
			}
		}

		#endregion

		#region Node building

		private static GameObject BuildNode( JObject _node, Transform _parent )
		{
			string template = (string)_node["template"];
			string type = (string)_node["type"];

			if (!string.IsNullOrEmpty(template) && !string.IsNullOrEmpty(type))
				throw new ArgumentException($"Node declares both \"template\" ('{template}') and \"type\" ('{type}'); pick one.");
			if (string.IsNullOrEmpty(template) && string.IsNullOrEmpty(type))
				throw new ArgumentException("Node must declare either \"template\" or \"type\".");

			GameObject go = !string.IsNullOrEmpty(template)
				? CreateTemplateNode(template)
				: CreateElementNode(type);

			string id = (string)_node["id"];
			string displayName = (string)_node["name"] ?? id ?? go.name;
			go.name = displayName;

			// Parent before configuring so [ExecuteAlways] style appliers resolve against the hierarchy.
			if (_parent != null)
			{
				var rt = go.transform as RectTransform;
				if (rt != null)
					rt.SetParent(_parent, false);
				else
					go.transform.SetParent(_parent, false);
			}

			// A UiView carries a Canvas + CanvasScaler that the toolkit configures at runtime (render
			// mode, reference resolution via the global template). That never runs while baking, so do
			// it here — otherwise the view keeps Unity's defaults (WorldSpace + constant-pixel scaler)
			// and every child renders at the wrong size.
			ConfigureViewCanvasIfPresent(go);

			if (_node["props"] is JObject props)
				ApplyProps(go, props);

			string style = (string)_node["style"];
			if (!string.IsNullOrEmpty(style))
				ApplyStyle(go, style);

			string text = (string)_node["text"];
			if (text != null)
				ApplyText(go, text);

			// Layout: an explicit "rect" wins; otherwise a root gets a sane full-stretch default so it
			// isn't left at the 100x100 centered default of a fresh RectTransform.
			if (_node["rect"] is JObject rect)
				ApplyRect(go, rect);
			else if (_parent == null)
				ApplyFullStretch(go);

			if (_node["children"] is JArray children)
			{
				foreach (var child in children.OfType<JObject>())
					BuildNode(child, go.transform);
			}

			return go;
		}

		private static GameObject CreateTemplateNode( string _templateName )
		{
			var prefab = ResolveTemplatePrefab(_templateName);
			if (prefab == null)
				throw new ArgumentException($"Unknown template '{_templateName}'. It must be a palette entry " +
				                            $"(run Generate Screen Catalog to see available templates).");

			var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			if (instance == null)
				throw new Exception($"Could not instantiate template prefab '{_templateName}'.");

			return instance;
		}

		private static GameObject CreateElementNode( string _typeName )
		{
			Type type = ResolveComponentType(_typeName);
			if (type == null)
				throw new ArgumentException($"Unknown component type '{_typeName}'. It must be a catalogued " +
				                            $"component (run Generate Screen Catalog to see available types).");

			// Start with a RectTransform (UI object); AddComponent auto-adds [RequireComponent]s.
			var go = new GameObject(_typeName, typeof(RectTransform));
			go.AddComponent(type);
			return go;
		}

		#endregion

		#region Canvas / Layout

		private static void ConfigureViewCanvasIfPresent( GameObject _go )
		{
			var canvas = _go.GetComponent<Canvas>();
			var scaler = _go.GetComponent<CanvasScaler>();
			if (canvas == null || scaler == null)
				return;

			// Overlay renders correctly in the prefab stage without needing a camera reference; UiMain
			// re-inits the render mode when the view is actually shown at runtime.
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;

			var config = UiToolkitConfiguration.Instance;
			var template = config != null ? config.GlobalCanvasScalerTemplate : null;
			if (template != null)
			{
				template.CopyTo(scaler);
			}
			else
			{
				// Fallback mirrors the toolkit's usual authoring resolution.
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
				scaler.matchWidthOrHeight = 0.5f;
			}

			EditorGeneralUtility.SetDirty(canvas);
			EditorGeneralUtility.SetDirty(scaler);
		}

		private static void ApplyFullStretch( GameObject _go )
		{
			if (_go.transform is RectTransform rt)
			{
				rt.anchorMin = Vector2.zero;
				rt.anchorMax = Vector2.one;
				rt.pivot = new Vector2(0.5f, 0.5f);
				rt.offsetMin = Vector2.zero;
				rt.offsetMax = Vector2.zero;
				EditorGeneralUtility.SetDirty(rt);
			}
		}

		private static void ApplyRect( GameObject _go, JObject _rect )
		{
			if (_go.transform is not RectTransform rt)
			{
				UiLog.LogWarning($"'rect' set on '{_go.name}' but it has no RectTransform; skipped.");
				return;
			}

			Vector2 min = rt.anchorMin, max = rt.anchorMax, pivot = rt.pivot;

			string preset = (string)_rect["anchor"];
			if (!string.IsNullOrEmpty(preset) && TryAnchorPreset(preset, out var pMin, out var pMax, out var pPivot))
			{
				min = pMin; max = pMax; pivot = pPivot;
			}

			if (_rect["anchorMin"] is JArray aMin) min = Vec2(aMin, min);
			if (_rect["anchorMax"] is JArray aMax) max = Vec2(aMax, max);
			if (_rect["pivot"] is JArray piv) pivot = Vec2(piv, pivot);

			rt.anchorMin = min;
			rt.anchorMax = max;
			rt.pivot = pivot;

			// sizeDelta / anchoredPosition first, then explicit stretch offsets win if given.
			if (_rect["size"] is JArray size) rt.sizeDelta = Vec2(size, rt.sizeDelta);
			if (_rect["position"] is JArray pos) rt.anchoredPosition = Vec2(pos, rt.anchoredPosition);
			if (_rect["offsetMin"] is JArray oMin) rt.offsetMin = Vec2(oMin, rt.offsetMin);
			if (_rect["offsetMax"] is JArray oMax) rt.offsetMax = Vec2(oMax, rt.offsetMax);

			EditorGeneralUtility.SetDirty(rt);
		}

		// Unity's anchor-preset grid, plus "stretch"/"fill". Returns anchorMin/Max and a matching pivot.
		private static bool TryAnchorPreset( string _name, out Vector2 _min, out Vector2 _max, out Vector2 _pivot )
		{
			(Vector2 min, Vector2 max, Vector2 pivot)? preset = _name.Trim().ToLowerInvariant() switch
			{
				"stretch" or "fill"   => (new Vector2(0, 0),    new Vector2(1, 1),    new Vector2(0.5f, 0.5f)),
				"center"              => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),
				"top"                 => (new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1)),
				"bottom"              => (new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0)),
				"left"                => (new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f)),
				"right"               => (new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f)),
				"top-left"            => (new Vector2(0, 1),    new Vector2(0, 1),    new Vector2(0, 1)),
				"top-right"           => (new Vector2(1, 1),    new Vector2(1, 1),    new Vector2(1, 1)),
				"bottom-left"         => (new Vector2(0, 0),    new Vector2(0, 0),    new Vector2(0, 0)),
				"bottom-right"        => (new Vector2(1, 0),    new Vector2(1, 0),    new Vector2(1, 0)),
				"top-stretch"         => (new Vector2(0, 1),    new Vector2(1, 1),    new Vector2(0.5f, 1)),
				"bottom-stretch"      => (new Vector2(0, 0),    new Vector2(1, 0),    new Vector2(0.5f, 0)),
				"left-stretch"        => (new Vector2(0, 0),    new Vector2(0, 1),    new Vector2(0, 0.5f)),
				"right-stretch"       => (new Vector2(1, 0),    new Vector2(1, 1),    new Vector2(1, 0.5f)),
				"stretch-horizontal"  => (new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f)),
				"stretch-vertical"    => (new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f)),
				_ => ((Vector2, Vector2, Vector2)?)null,
			};

			if (preset == null)
			{
				_min = _max = _pivot = new Vector2(0.5f, 0.5f);
				UiLog.LogWarning($"Unknown anchor preset '{_name}'; ignored.");
				return false;
			}

			(_min, _max, _pivot) = preset.Value;
			return true;
		}

		private static Vector2 Vec2( JArray _arr, Vector2 _fallback )
		{
			var v = _fallback;
			if (_arr.Count > 0) v.x = (float)_arr[0];
			if (_arr.Count > 1) v.y = (float)_arr[1];
			return v;
		}

		#endregion

		#region Props

		private static void ApplyProps( GameObject _go, JObject _props )
		{
			foreach (var pair in _props)
			{
				string key = pair.Key;
				JToken value = pair.Value;

				var (component, field) = FindSerializedField(_go, key);
				if (field == null)
				{
					UiLog.LogWarning($"Prop '{key}' not found on '{_go.name}'; skipped.");
					continue;
				}

				if (!TryConvert(value, field.FieldType, out object converted))
				{
					UiLog.LogWarning($"Prop '{key}' on '{_go.name}': cannot convert value to {field.FieldType.Name}; skipped.");
					continue;
				}

				field.SetValue(component, converted);
				EditorGeneralUtility.SetDirty(component);
			}
		}

		// Resolves an authoring name ("layer") or raw field name ("m_layer") to a serialized field on
		// any component of the GameObject, searching the most-derived declarations first.
		private static (Component, FieldInfo) FindSerializedField( GameObject _go, string _key )
		{
			string mKey = _key.StartsWith("m_", StringComparison.Ordinal) ? _key : "m_" + _key;

			foreach (var component in _go.GetComponents<Component>())
			{
				if (component == null)
					continue;

				for (var t = component.GetType(); t != null && t != typeof(object); t = t.BaseType)
				{
					var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public |
					                         BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
					foreach (var f in fields)
					{
						if (f.Name == _key || f.Name == mKey)
							return (component, f);
					}
				}
			}
			return (null, null);
		}

		private static bool TryConvert( JToken _token, Type _type, out object _result )
		{
			_result = null;
			try
			{
				if (_type == typeof(string)) { _result = (string)_token; return true; }
				if (_type == typeof(bool)) { _result = (bool)_token; return true; }
				if (_type.IsEnum) { _result = Enum.Parse(_type, (string)_token, true); return true; }

				if (_type == typeof(int) || _type == typeof(short) || _type == typeof(byte)
				    || _type == typeof(sbyte) || _type == typeof(ushort))
				{ _result = Convert.ChangeType((int)_token, _type, CultureInfo.InvariantCulture); return true; }
				if (_type == typeof(long) || _type == typeof(uint) || _type == typeof(ulong))
				{ _result = Convert.ChangeType((long)_token, _type, CultureInfo.InvariantCulture); return true; }
				if (_type == typeof(float)) { _result = (float)_token; return true; }
				if (_type == typeof(double)) { _result = (double)_token; return true; }

				if (_type == typeof(Color) || _type == typeof(Color32))
				{
					var c = ParseColor(_token);
					_result = _type == typeof(Color32) ? (object)(Color32)c : c;
					return true;
				}

				if (_type == typeof(Vector2)) { var v = Floats(_token, 2); _result = new Vector2(v[0], v[1]); return true; }
				if (_type == typeof(Vector3)) { var v = Floats(_token, 3); _result = new Vector3(v[0], v[1], v[2]); return true; }
				if (_type == typeof(Vector4)) { var v = Floats(_token, 4); _result = new Vector4(v[0], v[1], v[2], v[3]); return true; }

				if (typeof(Sprite).IsAssignableFrom(_type))
				{
					_result = AssetDatabase.LoadAssetAtPath<Sprite>((string)_token);
					return _result != null;
				}

				// Component / object references (id-based wiring) and nested structs are not yet
				// supported by the baker — handled in a later "logic wiring" milestone.
				return false;
			}
			catch
			{
				return false;
			}
		}

		private static Color ParseColor( JToken _token )
		{
			if (_token.Type == JTokenType.String)
			{
				string s = (string)_token;
				if (ColorUtility.TryParseHtmlString(s, out var c))
					return c;
				throw new FormatException($"'{s}' is not a valid HTML color.");
			}
			var f = Floats(_token, 4, defaultAlpha: 1f);
			return new Color(f[0], f[1], f[2], f[3]);
		}

		private static float[] Floats( JToken _token, int _count, float defaultAlpha = 0f )
		{
			var result = new float[_count];
			if (_count == 4)
				result[3] = defaultAlpha;

			if (_token is JArray arr)
			{
				for (int i = 0; i < _count && i < arr.Count; i++)
					result[i] = (float)arr[i];
				return result;
			}
			throw new FormatException("Expected a JSON array of numbers.");
		}

		#endregion

		#region Style

		private static void ApplyStyle( GameObject _go, string _styleName )
		{
			BuildApplierMap();

			bool applied = false;
			foreach (var kv in s_applierByTargetType)
			{
				Type targetType = kv.Key;
				Type applierType = kv.Value;

				if (_go.GetComponent(targetType) == null)
					continue;

				var applier = (UiAbstractApplyStyleBase)_go.AddComponent(applierType);
				applier.Name = _styleName; // setter resolves + applies

				if (applier.Style != null)
				{
					applier.Apply();
					EditorGeneralUtility.SetDirty(applier);
					applied = true;
				}
				else
				{
					UnityEngine.Object.DestroyImmediate(applier);
				}
			}

			if (!applied)
				UiLog.LogWarning($"Style '{_styleName}' matched no component on '{_go.name}'; skipped.");
		}

		#endregion

		#region Text

		private static void ApplyText( GameObject _go, string _text )
		{
			var localized = _go.GetComponentInChildren<UiLocalizedTextMeshProUGUI>(true);
			if (localized != null)
			{
				if (_text.StartsWith(LiteralTextPrefix, StringComparison.Ordinal))
				{
					string literal = _text.Substring(LiteralTextPrefix.Length);
					SetPrivateField(localized, "m_isTranslated", false);
					localized.text = literal;
				}
				else
				{
					string key = _text.StartsWith(LocaKeyPrefix, StringComparison.Ordinal)
						? _text.Substring(LocaKeyPrefix.Length)
						: _text;
					SetPrivateField(localized, "m_isTranslated", true);
					SetPrivateField(localized, "m_locaKey", key);
				}
				EditorGeneralUtility.SetDirty(localized);
				return;
			}

			var tmp = _go.GetComponentInChildren<TMPro.TMP_Text>(true);
			if (tmp != null)
			{
				tmp.text = StripPrefix(_text, LiteralTextPrefix);
				EditorGeneralUtility.SetDirty(tmp);
				return;
			}

			UiLog.LogWarning($"Text set requested on '{_go.name}' but no TMP text component was found; skipped.");
		}

		private static string StripPrefix( string _value, string _prefix )
			=> _value.StartsWith(_prefix, StringComparison.Ordinal) ? _value.Substring(_prefix.Length) : _value;

		private static void SetPrivateField( object _target, string _fieldName, object _value )
		{
			for (var t = _target.GetType(); t != null && t != typeof(object); t = t.BaseType)
			{
				var f = t.GetField(_fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				if (f != null)
				{
					f.SetValue(_target, _value);
					return;
				}
			}
		}

		#endregion

		#region Type resolution

		private static void ResetCaches()
		{
			s_componentTypesByName = null;
			s_applierByTargetType = null;
		}

		private static GameObject ResolveTemplatePrefab( string _name )
		{
			// Prefer the catalog's palette (authoritative, honors the override asset). Fall back to a
			// direct StandardElements name match so baking works even before the catalog is regenerated.
			foreach (var guid in AssetDatabase.FindAssets($"{_name} t:Prefab"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab != null && prefab.name == _name &&
				    path.Replace('\\', '/').Contains("/Prefabs/StandardElements/"))
					return prefab;
			}

			// Second pass: any prefab with that exact name anywhere (client widgets / extra folders).
			foreach (var guid in AssetDatabase.FindAssets($"{_name} t:Prefab"))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab != null && prefab.name == _name)
					return prefab;
			}
			return null;
		}

		private static Type ResolveComponentType( string _shortName )
		{
			if (s_componentTypesByName == null)
			{
				s_componentTypesByName = new Dictionary<string, Type>(StringComparer.Ordinal);
				Assembly toolkit = typeof(UiThing).Assembly;

				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
				{
					Type[] types;
					try { types = asm.GetTypes(); }
					catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
					catch { continue; }

					foreach (var t in types)
					{
						if (t == null || t.IsAbstract || !typeof(Component).IsAssignableFrom(t))
							continue;

						// On a short-name collision, prefer the toolkit's own type.
						if (s_componentTypesByName.TryGetValue(t.Name, out var existing))
						{
							if (existing.Assembly == toolkit)
								continue;
						}
						s_componentTypesByName[t.Name] = t;
					}
				}
			}

			s_componentTypesByName.TryGetValue(_shortName, out var result);
			return result;
		}

		// target Unity component type -> concrete UiApplyStyle* applier type.
		private static void BuildApplierMap()
		{
			if (s_applierByTargetType != null)
				return;

			s_applierByTargetType = new Dictionary<Type, Type>();
			Assembly toolkit = typeof(UiThing).Assembly;

			foreach (var t in SafeTypes(toolkit))
			{
				if (t.IsAbstract || !typeof(UiAbstractApplyStyleBase).IsAssignableFrom(t))
					continue;

				Type target = ApplierTargetType(t);
				if (target != null)
					s_applierByTargetType[target] = t;
			}
		}

		private static Type ApplierTargetType( Type _applierType )
		{
			for (var b = _applierType.BaseType; b != null; b = b.BaseType)
			{
				if (b.IsGenericType && b.GetGenericTypeDefinition().Name.StartsWith("UiAbstractApplyStyle", StringComparison.Ordinal))
				{
					var args = b.GetGenericArguments();
					if (args.Length >= 1)
						return args[0];
				}
			}
			return null;
		}

		private static IEnumerable<Type> SafeTypes( Assembly _asm )
		{
			try { return _asm.GetTypes(); }
			catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
			catch { return Array.Empty<Type>(); }
		}

		#endregion

		#region Test fixture

		// The Milestone-2 proof: a dialog composed from templates + a UiView element root.
		private const string TestDialogJson = @"
{
  ""name"": ""AiTestDialog"",
  ""root"": {
    ""type"": ""UiView"",
    ""id"": ""root"",
    ""props"": { ""layer"": ""Dialog"", ""isFullScreen"": false },
    ""children"": [
      {
        ""template"": ""StandardPanelBackgroundWithHeadline"",
        ""id"": ""panel"",
        ""rect"": { ""anchor"": ""center"", ""size"": [900, 600] },
        ""children"": [
          {
            ""template"": ""StandardButtonBar"",
            ""id"": ""buttons"",
            ""rect"": { ""anchor"": ""bottom-stretch"", ""size"": [0, 140], ""position"": [0, 40] },
            ""children"": [
              { ""template"": ""OkButton"", ""id"": ""okButton"", ""text"": ""@text:OK"" },
              { ""template"": ""CancelButton"", ""id"": ""cancelButton"", ""text"": ""@text:Cancel"" }
            ]
          }
        ]
      }
    ]
  }
}";

		#endregion
	}
}
