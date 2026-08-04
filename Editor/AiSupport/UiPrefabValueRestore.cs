using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Writes values from a <see cref="UiPrefabValueSnapshot"/> back into a re-baked prefab — the second half of
	/// the capture → adapt the description → re-bake → restore workflow.
	///
	/// The point is to decouple "nothing gets lost" from "the description can say it": whatever a human edited
	/// that the authoring vocabulary cannot express survives a re-bake, without the baker having to grow a
	/// feature for it first.
	///
	/// Four rules shape this, and each one is a decision rather than an implementation detail:
	///
	/// 1. VALUES, NOT REFERENCES. Wiring belongs to the description via "#id", which is the single source of
	///    truth for what points at what. Differing references are therefore REPORTED, never written — see
	///    <c>references</c> in the report. (Unity does keep object ids stable across a re-bake for nodes that
	///    keep their identity, so restoring them is not impossible — but it would put a second authority next
	///    to the description, so it stays a report until we decide otherwise.)
	/// 2. COMPARE AGAINST THE STATE AFTER THE BAKE, not against the description. Anything the bake already
	///    reproduced compares equal and is left alone, so the description keeps its authority and style-set
	///    values are not baked in as if a human had typed them. Only the residue is a candidate.
	/// 3. LEAVE DERIVED DATA ALONE. Inherited from the capture's skip lists, so there is one definition of
	///    "not a human's edit" rather than two.
	/// 4. REPORT EVERYTHING. Every restored path is something the description could not express, which makes
	///    the report a roadmap of the gaps worth closing in the baker — <c>propertyHistogram</c> exists for
	///    exactly that reading.
	///
	/// Dry run is the DEFAULT. A tool that writes into prefabs should be readable before it is trusted, and the
	/// residue cannot be told apart from a deliberate description change by comparison alone: both show up as
	/// "snapshot differs from current". The caller sitting between capture and restore knows which is which, so
	/// the plan is made reviewable and <c>include</c> allows applying only the agreed part.
	/// </summary>
	public static class UiPrefabValueRestore
	{
		/// <summary>How many plan entries travel back inline; the full plan always goes to the report file.</summary>
		private const int MaxInlineEntries = 40;

		private sealed class PlanEntry
		{
			public string Node;
			public string ComponentType;
			public int Ordinal;
			public string Property;
			public JToken From;
			public JToken To;

			public JObject ToJson() => new()
			{
				["node"] = string.IsNullOrEmpty(Node) ? "<root>" : Node,
				["component"] = ComponentType,
				["ordinal"] = Ordinal,
				["property"] = Property,
				["from"] = From,
				["to"] = To,
			};
		}

		private sealed class ComponentValues
		{
			public string Type;
			public int Ordinal;
			public JObject Values;
			public bool Matched;
		}

		/// <summary>
		/// Compares <paramref name="_prefabPath"/> against its snapshot and reports — or, when
		/// <paramref name="_dryRun"/> is false, performs — the value restore.
		/// </summary>
		/// <param name="_snapshotPath">Defaults to the snapshot the capture wrote for this prefab.</param>
		/// <param name="_include">
		/// When given, only entries whose node path, component type or property path contains one of these
		/// substrings are considered. This is how a reviewed plan gets applied in part.
		/// </param>
		public static JObject Apply( string _prefabPath, string _snapshotPath, bool _dryRun, string[] _include )
		{
			string snapshotPath = string.IsNullOrWhiteSpace(_snapshotPath)
				? UiPrefabValueSnapshot.SnapshotPathFor(_prefabPath)
				: _snapshotPath;

			string fullSnapshotPath = Path.GetFullPath(snapshotPath);
			if (!File.Exists(fullSnapshotPath))
			{
				throw new ArgumentException(
					$"No snapshot at '{snapshotPath}'. Run capture_prefab_values on the prefab BEFORE re-baking it — " +
					"a snapshot taken after the bake has already lost whatever the bake overwrote.");
			}

			var snapshot = JObject.Parse(File.ReadAllText(fullSnapshotPath));
			var warnings = new JArray();

			string capturedFrom = (string)snapshot["prefab"];
			if (!string.IsNullOrEmpty(capturedFrom) && capturedFrom != _prefabPath)
			{
				warnings.Add($"The snapshot was captured from '{capturedFrom}', which is not '{_prefabPath}'. " +
					"Node paths may not line up.");
			}

			// The comparison baseline is the prefab as it stands NOW (rule 2), expressed by the very same builder
			// the snapshot came from so that the two sides cannot drift apart in their vocabulary.
			var current = UiPrefabValueSnapshot.BuildSnapshot(_prefabPath, out var currentStats);

			// A prefab whose scripts will not load must not be written to: saving it makes Unity drop the
			// components behind the missing scripts for good. Reading it is fine, so the dry run still works.
			var unloadable = (JArray)currentStats["nodesWithUnloadableScripts"];
			if (unloadable is { Count: > 0 } && !_dryRun)
			{
				return new JObject
				{
					["prefab"] = _prefabPath,
					["snapshotPath"] = snapshotPath,
					["blocked"] = "Refusing to write: the prefab has components whose scripts cannot be loaded, and " +
						"saving it would delete them permanently. Fix the project state first (restart Unity, then " +
						"delete Library/PackageCache/<the package>, then a full reimport), and verify with a dry run.",
					["nodesWithUnloadableScripts"] = unloadable,
				};
			}

			var currentIndex = IndexNodes(current);
			var snapshotIndex = IndexNodes(snapshot);

			var plan = new List<PlanEntry>();
			var references = new JArray();
			var missingNodes = new JArray();
			var missingComponents = new JArray();
			var missingProperties = new JArray();
			int identical = 0, filteredOut = 0;

			foreach (var (nodePath, snapshotComponents) in snapshotIndex)
			{
				if (!currentIndex.TryGetValue(nodePath, out var currentComponents))
				{
					// Renaming, retyping or moving a node gives it a new identity, so the snapshot cannot find it.
					// This is the one case where a re-bake genuinely breaks references from outside the prefab too.
					missingNodes.Add(string.IsNullOrEmpty(nodePath) ? "<root>" : nodePath);
					continue;
				}

				foreach (var snapshotComponent in snapshotComponents)
				{
					var currentComponent = currentComponents.FirstOrDefault(
						_c => _c.Type == snapshotComponent.Type && _c.Ordinal == snapshotComponent.Ordinal);

					if (currentComponent == null)
					{
						missingComponents.Add($"{Describe(nodePath)} :: {snapshotComponent.Type}" +
							(snapshotComponent.Ordinal > 0 ? $" #{snapshotComponent.Ordinal}" : ""));
						continue;
					}

					currentComponent.Matched = true;

					foreach (var property in snapshotComponent.Values)
					{
						string propertyPath = property.Key;
						JToken want = property.Value;

						if (!Matches(_include, nodePath, snapshotComponent.Type, propertyPath))
						{
							filteredOut++;
							continue;
						}

						var have = currentComponent.Values[propertyPath];
						if (have == null)
						{
							missingProperties.Add($"{Describe(nodePath)} :: {snapshotComponent.Type}.{propertyPath}");
							continue;
						}

						if (ValuesEqual(have, want))
						{
							identical++;
							continue;
						}

						// Rule 1: a differing reference is information, not a candidate.
						if (IsReference(want) || IsReference(have) || IsObjectIdFragment(propertyPath))
						{
							references.Add(new JObject
							{
								["node"] = Describe(nodePath),
								["component"] = snapshotComponent.Type,
								["property"] = propertyPath,
								["from"] = have,
								["to"] = want,
							});
							continue;
						}

						plan.Add(new PlanEntry
						{
							Node = nodePath,
							ComponentType = snapshotComponent.Type,
							Ordinal = snapshotComponent.Ordinal,
							Property = propertyPath,
							From = have,
							To = want,
						});
					}
				}
			}

			var addedNodes = new JArray();
			foreach (var (nodePath, components) in currentIndex)
			{
				if (!snapshotIndex.ContainsKey(nodePath))
					addedNodes.Add(Describe(nodePath));
			}

			var restored = new JArray();
			var failed = new JArray();
			if (!_dryRun && plan.Count > 0)
				Write(_prefabPath, plan, restored, failed);

			var report = new JObject
			{
				["prefab"] = _prefabPath,
				["snapshotPath"] = snapshotPath,
				["dryRun"] = _dryRun,
				["plan"] = new JArray(plan.Select(_e => _e.ToJson())),
				["restored"] = restored,
				["failed"] = failed,
				["references"] = references,
				["missingNodes"] = missingNodes,
				["missingComponents"] = missingComponents,
				["missingProperties"] = missingProperties,
				["addedNodes"] = addedNodes,
				["warnings"] = warnings,
			};

			string reportPath = ReportPathFor(_prefabPath);
			Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
			File.WriteAllText(Path.GetFullPath(reportPath), report.ToString(Newtonsoft.Json.Formatting.Indented));

			return Summarise(_prefabPath, snapshotPath, reportPath, _dryRun, plan, restored, failed, references,
				missingNodes, missingComponents, missingProperties, addedNodes, warnings, identical, filteredOut,
				RenameCandidates(missingNodes, currentIndex, snapshotIndex));
		}

		public static string ReportPathFor( string _prefabPath ) =>
			$"{Path.GetDirectoryName(UiPrefabValueSnapshot.SnapshotPathFor(_prefabPath)).Replace('\\', '/')}/" +
			$"{Path.GetFileNameWithoutExtension(_prefabPath)}.restore.json";

		private static JObject Summarise( string _prefabPath, string _snapshotPath, string _reportPath, bool _dryRun,
			List<PlanEntry> _plan, JArray _restored, JArray _failed, JArray _references, JArray _missingNodes,
			JArray _missingComponents, JArray _missingProperties, JArray _addedNodes, JArray _warnings,
			int _identical, int _filteredOut, JArray _renameCandidates )
		{
			// The histogram is the roadmap reading of rule 4: a property that keeps coming back is a gap in the
			// authoring vocabulary, not a one-off edit.
			var histogram = new JObject();
			foreach (var group in _plan.GroupBy(_e => $"{_e.ComponentType}.{_e.Property}")
				.OrderByDescending(_g => _g.Count()))
			{
				histogram[group.Key] = group.Count();
			}

			var summary = new JObject
			{
				["prefab"] = _prefabPath,
				["snapshotPath"] = _snapshotPath,
				["reportPath"] = _reportPath,
				["dryRun"] = _dryRun,
				["identical"] = _identical,
				[_dryRun ? "wouldRestore" : "restoredCount"] = _dryRun ? _plan.Count : _restored.Count,
				["propertyHistogram"] = histogram,
				["entries"] = new JArray(_plan.Take(MaxInlineEntries).Select(_e => _e.ToJson())),
				["entriesTruncated"] = _plan.Count > MaxInlineEntries ? _plan.Count - MaxInlineEntries : 0,
				["differingReferences"] = _references.Count,
				["referenceSample"] = new JArray(_references.Take(MaxInlineEntries)),
				["missingNodes"] = _missingNodes,
				["renameCandidates"] = _renameCandidates,
				["missingComponents"] = _missingComponents,
				["missingProperties"] = _missingProperties,
				["addedNodes"] = _addedNodes,
				["filteredOut"] = _filteredOut,
				["warnings"] = _warnings,
			};

			if (!_dryRun)
				summary["failed"] = _failed;

			return summary;
		}

		/// <summary>
		/// A guess at where a missing node went. Since identity is the node's name, a rename looks exactly like a
		/// removal plus an addition — and the pair is usually obvious from the same parent and the same set of
		/// components. Labelled as a candidate because it IS a guess: nothing acts on it automatically.
		/// </summary>
		private static JArray RenameCandidates( JArray _missingNodes,
			Dictionary<string, List<ComponentValues>> _current, Dictionary<string, List<ComponentValues>> _snapshot )
		{
			var result = new JArray();
			var unmatched = _current.Where(_kv => !_snapshot.ContainsKey(_kv.Key)).ToList();
			if (unmatched.Count == 0)
				return result;

			foreach (var missing in _missingNodes)
			{
				string missingPath = (string)missing == "<root>" ? "" : (string)missing;
				if (!_snapshot.TryGetValue(missingPath, out var wanted))
					continue;

				string parent = ParentOf(missingPath);
				var wantedTypes = wanted.Select(_c => _c.Type).OrderBy(_t => _t).ToList();

				foreach (var (candidatePath, candidate) in unmatched)
				{
					if (ParentOf(candidatePath) != parent)
						continue;
					if (!candidate.Select(_c => _c.Type).OrderBy(_t => _t).SequenceEqual(wantedTypes))
						continue;

					result.Add(new JObject
					{
						["snapshotNode"] = Describe(missingPath),
						["nowProbably"] = Describe(candidatePath),
						["sharedComponents"] = new JArray(wantedTypes),
					});
				}
			}
			return result;
		}

		private static string ParentOf( string _path )
		{
			int slash = _path.LastIndexOf('/');
			return slash < 0 ? "" : _path.Substring(0, slash);
		}

		private static void Write( string _prefabPath, List<PlanEntry> _plan, JArray _restored, JArray _failed )
		{
			GameObject prefab = PrefabUtility.LoadPrefabContents(_prefabPath);
			try
			{
				// Grouped per component so each SerializedObject is opened, filled and applied exactly once.
				foreach (var group in _plan.GroupBy(_e => (_e.Node, _e.ComponentType, _e.Ordinal)))
				{
					var component = FindComponent(prefab, group.Key.Node, group.Key.ComponentType, group.Key.Ordinal);
					if (component == null)
					{
						foreach (var entry in group)
						{
							_failed.Add(Fail(entry, "the component was no longer there when writing"));
						}
						continue;
					}

					var serialized = new SerializedObject(component);
					bool touched = false;

					// In snapshot order on purpose: an array's ".Array.size" precedes its ".Array.data[i]" entries,
					// so growing a list happens before its elements are written.
					foreach (var entry in group)
					{
						var property = serialized.FindProperty(entry.Property);
						if (property == null)
						{
							_failed.Add(Fail(entry, "no such serialized property on the rebuilt component"));
							continue;
						}

						if (!TryWriteValue(property, entry.To, out string error))
						{
							_failed.Add(Fail(entry, error));
							continue;
						}

						_restored.Add(entry.ToJson());
						touched = true;
					}

					if (touched)
						serialized.ApplyModifiedPropertiesWithoutUndo();
				}

				PrefabUtility.SaveAsPrefabAsset(prefab, _prefabPath, out bool success);
				if (!success)
					throw new Exception($"PrefabUtility.SaveAsPrefabAsset failed for '{_prefabPath}'.");
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(prefab);
			}

			AssetDatabase.ImportAsset(_prefabPath, ImportAssetOptions.ForceUpdate);
		}

		private static JObject Fail( PlanEntry _entry, string _reason )
		{
			var json = _entry.ToJson();
			json["reason"] = _reason;
			return json;
		}

		private static Component FindComponent( GameObject _prefab, string _nodePath, string _type, int _ordinal )
		{
			Transform transform = string.IsNullOrEmpty(_nodePath)
				? _prefab.transform
				: _prefab.transform.Find(_nodePath);
			if (transform == null)
				return null;

			int ordinal = 0;
			foreach (var component in transform.GetComponents<Component>())
			{
				// Nulls are skipped without counting, exactly as the capture does — otherwise a node with an
				// unloadable script would shift every ordinal after it and values would land on the wrong component.
				if (component == null)
					continue;
				if (component.GetType().Name != _type)
					continue;
				if (ordinal++ == _ordinal)
					return component;
			}
			return null;
		}

		private static Dictionary<string, List<ComponentValues>> IndexNodes( JObject _snapshot )
		{
			var result = new Dictionary<string, List<ComponentValues>>();
			var nodes = _snapshot["nodes"] as JArray ?? new JArray();

			foreach (var node in nodes)
			{
				string path = (string)node["path"] ?? "";
				var components = new List<ComponentValues>();
				var perType = new Dictionary<string, int>();

				foreach (var component in node["components"] as JArray ?? new JArray())
				{
					string type = (string)component["type"] ?? "";
					perType.TryGetValue(type, out int ordinal);
					perType[type] = ordinal + 1;

					components.Add(new ComponentValues
					{
						Type = type,
						Ordinal = ordinal,
						Values = component["values"] as JObject ?? new JObject(),
					});
				}

				result[path] = components;
			}
			return result;
		}

		private static string Describe( string _nodePath ) => string.IsNullOrEmpty(_nodePath) ? "<root>" : _nodePath;

		/// <summary>
		/// Equality in the precision Unity actually stores, which is what makes the comparison meaningful.
		///
		/// <see cref="JToken.DeepEquals"/> is not usable here: the snapshot has been through text, where a float
		/// becomes the short literal "12.67" and parses back as the double 12.67 — while the live side still holds
		/// the same float widened to 12.670000076293945. Those are the same number in a prefab and different
		/// numbers to a double comparison, which reported 69 differences whose "from" and "to" printed identically.
		/// Narrowing both sides to float makes them agree exactly, with no epsilon to tune.
		/// </summary>
		private static bool ValuesEqual( JToken _a, JToken _b )
		{
			if (_a == null || _b == null)
				return _a == null && _b == null;

			// Integers stay integers: ids and sizes need their full width, and narrowing those to float would be
			// the opposite mistake.
			if (_a.Type == JTokenType.Integer && _b.Type == JTokenType.Integer)
				return (long)_a == (long)_b;

			if (IsNumeric(_a) && IsNumeric(_b))
				return (float)(double)_a == (float)(double)_b;

			if (_a is JArray arrayA && _b is JArray arrayB)
			{
				if (arrayA.Count != arrayB.Count)
					return false;
				for (int i = 0; i < arrayA.Count; i++)
				{
					if (!ValuesEqual(arrayA[i], arrayB[i]))
						return false;
				}
				return true;
			}

			if (_a is JObject objectA && _b is JObject objectB)
			{
				if (objectA.Count != objectB.Count)
					return false;
				foreach (var property in objectA)
				{
					if (!objectB.TryGetValue(property.Key, out var other) || !ValuesEqual(property.Value, other))
						return false;
				}
				return true;
			}

			return JToken.DeepEquals(_a, _b);
		}

		private static bool IsNumeric( JToken _token ) =>
			_token.Type is JTokenType.Float or JTokenType.Integer;

		private static bool IsReference( JToken _token ) =>
			_token is JObject obj && obj.ContainsKey(UiPrefabValueSnapshot.RefKey);

		/// <summary>
		/// The halves Unity stores an object reference in. The capture no longer descends into a reference, so
		/// these should not appear at all — but a snapshot taken before that fix still holds them, and they are
		/// plain integers that nothing else would recognise as references. Writing one would put a raw object id
		/// into a prefab, which is the exact failure rule 1 exists to prevent, so this stays as a second line.
		/// </summary>
		private static bool IsObjectIdFragment( string _propertyPath ) =>
			_propertyPath.EndsWith(".m_FileID", StringComparison.Ordinal)
			|| _propertyPath.EndsWith(".m_PathID", StringComparison.Ordinal);

		private static bool Matches( string[] _include, string _node, string _type, string _property )
		{
			if (_include == null || _include.Length == 0)
				return true;

			foreach (string filter in _include)
			{
				if (string.IsNullOrEmpty(filter))
					continue;
				if (_node.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
					|| _type.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
					|| _property.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// The exact counterpart of the capture's reader. Anything the capture can express has to be writable here,
		/// or a value would be captured, reported as a difference, and then quietly fail to be restored.
		/// </summary>
		private static bool TryWriteValue( SerializedProperty _property, JToken _token, out string _error )
		{
			_error = null;
			try
			{
				switch (_property.propertyType)
				{
					case SerializedPropertyType.Integer: _property.longValue = (long)_token; return true;
					case SerializedPropertyType.Boolean: _property.boolValue = (bool)_token; return true;
					case SerializedPropertyType.Float: _property.doubleValue = (double)_token; return true;
					case SerializedPropertyType.String: _property.stringValue = (string)_token ?? ""; return true;
					case SerializedPropertyType.Character: _property.intValue = (int)_token; return true;
					case SerializedPropertyType.ArraySize: _property.intValue = (int)_token; return true;
					case SerializedPropertyType.LayerMask: _property.intValue = (int)_token; return true;

					case SerializedPropertyType.Enum: return TryWriteEnum(_property, _token, out _error);

					case SerializedPropertyType.Color:
					{
						// Both forms the capture can produce: hex while hex is exact, four floats when it is not.
						if (_token is JArray)
						{
							if (!TryFloats(_token, 4, out var f, out _error)) return false;
							_property.colorValue = new Color(f[0], f[1], f[2], f[3]);
							return true;
						}

						if (!ColorUtility.TryParseHtmlString((string)_token, out var color))
						{
							_error = $"'{_token}' is not a colour";
							return false;
						}
						_property.colorValue = color;
						return true;
					}

					case SerializedPropertyType.Vector2:
					{
						if (!TryFloats(_token, 2, out var f, out _error)) return false;
						_property.vector2Value = new Vector2(f[0], f[1]); return true;
					}
					case SerializedPropertyType.Vector3:
					{
						if (!TryFloats(_token, 3, out var f, out _error)) return false;
						_property.vector3Value = new Vector3(f[0], f[1], f[2]); return true;
					}
					case SerializedPropertyType.Vector4:
					{
						if (!TryFloats(_token, 4, out var f, out _error)) return false;
						_property.vector4Value = new Vector4(f[0], f[1], f[2], f[3]); return true;
					}
					case SerializedPropertyType.Vector2Int:
					{
						if (!TryFloats(_token, 2, out var f, out _error)) return false;
						_property.vector2IntValue = new Vector2Int((int)f[0], (int)f[1]); return true;
					}
					case SerializedPropertyType.Vector3Int:
					{
						if (!TryFloats(_token, 3, out var f, out _error)) return false;
						_property.vector3IntValue = new Vector3Int((int)f[0], (int)f[1], (int)f[2]); return true;
					}
					case SerializedPropertyType.Rect:
					{
						if (!TryFloats(_token, 4, out var f, out _error)) return false;
						_property.rectValue = new Rect(f[0], f[1], f[2], f[3]); return true;
					}
					case SerializedPropertyType.Quaternion:
					{
						if (!TryFloats(_token, 4, out var f, out _error)) return false;
						_property.quaternionValue = new Quaternion(f[0], f[1], f[2], f[3]); return true;
					}
					case SerializedPropertyType.Bounds:
					{
						if (!TryFloats(_token, 6, out var f, out _error)) return false;
						_property.boundsValue = new Bounds(
							new Vector3(f[0], f[1], f[2]), new Vector3(f[3], f[4], f[5]));
						return true;
					}

					case SerializedPropertyType.AnimationCurve:
						_property.animationCurveValue = CurveFromJson(_token);
						return true;

					case SerializedPropertyType.Gradient: return TryWriteGradient(_property, _token, out _error);

					// Rule 1. Reaching here would be a bug in the caller, so it says so rather than writing.
					case SerializedPropertyType.ObjectReference:
						_error = "object references are reported, never restored (wiring belongs to the description)";
						return false;

					default:
						_error = $"cannot write a {_property.propertyType}";
						return false;
				}
			}
			catch (Exception e)
			{
				_error = $"{e.GetType().Name}: {e.Message}";
				return false;
			}
		}

		private static bool TryWriteEnum( SerializedProperty _property, JToken _token, out string _error )
		{
			_error = null;

			// Written back by NAME, mirroring the capture: an index would silently mean something else after the
			// enum is reordered. A number is still accepted for the flag masks the capture cannot name.
			if (_token.Type == JTokenType.String)
			{
				string name = (string)_token;
				int index = Array.IndexOf(_property.enumNames, name);
				if (index < 0)
				{
					_error = $"'{name}' is not one of {string.Join("/", _property.enumNames)}";
					return false;
				}
				_property.enumValueIndex = index;
				return true;
			}

			_property.intValue = (int)_token;
			return true;
		}

		private static bool TryWriteGradient( SerializedProperty _property, JToken _token, out string _error )
		{
			_error = null;
			if (UiPrefabValueSnapshot.GradientValueProperty == null)
			{
				_error = "SerializedProperty.gradientValue is not reachable in this Unity version";
				return false;
			}

			if (_token is not JObject json)
			{
				_error = "a gradient needs an object with colorKeys/alphaKeys";
				return false;
			}

			var gradient = new Gradient();

			var colorKeys = new List<GradientColorKey>();
			foreach (var key in json["colorKeys"] as JArray ?? new JArray())
			{
				if (!ColorUtility.TryParseHtmlString((string)key["color"], out var color))
				{
					_error = $"'{key["color"]}' is not a colour";
					return false;
				}
				colorKeys.Add(new GradientColorKey(color, (float)key["time"]));
			}

			var alphaKeys = new List<GradientAlphaKey>();
			foreach (var key in json["alphaKeys"] as JArray ?? new JArray())
				alphaKeys.Add(new GradientAlphaKey((float)key["alpha"], (float)key["time"]));

			if (colorKeys.Count > 0 || alphaKeys.Count > 0)
				gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());

			if (Enum.TryParse((string)json["mode"], out GradientMode mode))
				gradient.mode = mode;
			if (Enum.TryParse((string)json["colorSpace"], out ColorSpace colorSpace))
				gradient.colorSpace = colorSpace;

			UiPrefabValueSnapshot.GradientValueProperty.SetValue(_property, gradient);
			return true;
		}

		private static AnimationCurve CurveFromJson( JToken _token )
		{
			var curve = new AnimationCurve();
			if (_token is not JObject json)
				return curve;

			var keys = new List<Keyframe>();
			foreach (var key in json["keys"] as JArray ?? new JArray())
			{
				keys.Add(new Keyframe(
					(float)key["time"], (float)key["value"], (float)key["inTangent"], (float)key["outTangent"]));
			}
			curve.keys = keys.ToArray();

			if (Enum.TryParse((string)json["preWrapMode"], out WrapMode preWrap))
				curve.preWrapMode = preWrap;
			if (Enum.TryParse((string)json["postWrapMode"], out WrapMode postWrap))
				curve.postWrapMode = postWrap;

			return curve;
		}

		private static bool TryFloats( JToken _token, int _count, out float[] _values, out string _error )
		{
			_error = null;
			_values = null;

			if (_token is not JArray array || array.Count < _count)
			{
				_error = $"expected {_count} numbers, got '{_token}'";
				return false;
			}

			_values = new float[_count];
			for (int i = 0; i < _count; i++)
				_values[i] = Convert.ToSingle((double)array[i], CultureInfo.InvariantCulture);
			return true;
		}
	}
}
