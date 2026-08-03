using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Reads a project's animations back out of its prefabs and groups identical configurations, so the motion
	/// a project ALREADY uses can be named and reused instead of re-invented per screen.
	///
	/// Why this is worth a tool: an animation is eighteen serialized values, and getting them right is craft —
	/// curve shapes and tangents decide whether motion feels snappy or rubbery, which is a judgement made by
	/// someone watching it. A configuration that appears on many prefabs has already had that judgement applied.
	/// Harvesting turns that into a vocabulary; inventing one from scratch throws it away.
	///
	/// Wiring is deliberately excluded from the grouping key: which transform an animation drives, and which
	/// slaves it commands, is per-instance and says nothing about how the motion looks. Two animations that
	/// differ only in their target are the same motion.
	/// </summary>
	public static class UiMotionHarvester
	{
		/// <summary>Per-instance wiring, plus bookkeeping — none of it describes how a motion looks.</summary>
		private static readonly HashSet<string> s_ignoredForShape = new()
		{
			"m_target", "m_alphaGraphic", "m_alphaCanvasGroup", "m_backwardsAnimation",
			"m_behavioursToDisableWhilePlaying", "m_container", "m_childAnimations",
			"m_onFinish", "m_onFinishOnce", "m_enabledInHierarchy", "m_Enabled",
		};

		/// <summary>Prefixes of the same, for the array entries the reader emits one by one.</summary>
		private static readonly string[] s_ignoredPrefixes =
		{
			"m_slaveAnimations", "m_childAnimations", "m_behavioursToDisableWhilePlaying",
			"m_onFinish", "m_onFinishOnce",
		};

		public static JObject Harvest( string[] _folders, int _minOccurrences, int _maxExamples )
		{
			string[] folders = _folders is { Length: > 0 } ? _folders : new[] { "Assets" };
			folders = folders.Where(AssetDatabase.IsValidFolder).ToArray();
			if (folders.Length == 0)
				throw new ArgumentException("None of the given folders exist in this project.");

			string[] guids = AssetDatabase.FindAssets("t:Prefab", folders);

			var groups = new Dictionary<string, Group>(StringComparer.Ordinal);
			int prefabsWithMotion = 0, animationCount = 0;

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null)
					continue;

				// The asset object is enough here: only serialized values are read, nothing is instantiated or
				// modified, and loading prefab contents for thousands of prefabs would be needlessly slow.
				var animations = prefab.GetComponentsInChildren<UiSimpleAnimationBase>(true);
				if (animations.Length == 0)
					continue;

				prefabsWithMotion++;
				foreach (var animation in animations)
				{
					if (animation == null)
						continue;

					animationCount++;
					var values = UiPrefabValueSnapshot.CaptureComponentValues(animation);
					var shape = ShapeOf(animation.GetType().Name, values);
					string key = shape.ToString(Newtonsoft.Json.Formatting.None);

					if (!groups.TryGetValue(key, out var group))
					{
						group = new Group { Type = animation.GetType().Name, Shape = shape };
						groups[key] = group;
					}

					group.Count++;
					if (group.Examples.Count < _maxExamples)
						group.Examples.Add($"{path}:{PathOf(prefab.transform, animation.transform)}");
				}
			}

			int minOccurrences = Mathf.Max(1, _minOccurrences);
			var ordered = groups.Values
				.Where(g => g.Count >= minOccurrences)
				.OrderByDescending(g => g.Count)
				.ToList();

			var result = new JArray();
			foreach (var group in ordered)
			{
				result.Add(new JObject
				{
					["type"] = group.Type,
					["count"] = group.Count,
					["summary"] = Summarise(group.Shape),
					["values"] = group.Shape,
					["examples"] = new JArray(group.Examples),
				});
			}

			return new JObject
			{
				["folders"] = new JArray(folders),
				["prefabsScanned"] = guids.Length,
				["prefabsWithMotion"] = prefabsWithMotion,
				["animationsFound"] = animationCount,
				["distinctShapes"] = groups.Count,
				["reported"] = ordered.Count,
				["minOccurrences"] = minOccurrences,
				["note"] = "Grouped by how the motion LOOKS: targets, slaves and callbacks are excluded, so two " +
					"animations differing only in what they drive count as one. 'count' is how often a project " +
					"committed to that shape — the high counts are its vocabulary.",
				["shapes"] = result,
			};
		}

		private sealed class Group
		{
			public string Type;
			public JObject Shape;
			public int Count;
			public readonly List<string> Examples = new();
		}

		private static JObject ShapeOf( string _typeName, JObject _values )
		{
			var shape = new JObject { ["__type"] = _typeName };

			foreach (var property in _values)
			{
				if (s_ignoredForShape.Contains(property.Key))
					continue;
				if (s_ignoredPrefixes.Any(p => property.Key.StartsWith(p, StringComparison.Ordinal)))
					continue;

				// An empty curve is the reader's way of saying "this channel is unused"; keeping them would split
				// otherwise identical shapes over which unused channels happen to carry a stub.
				if (IsEmptyCurve(property.Value))
					continue;

				shape[property.Key] = property.Value;
			}

			return shape;
		}

		private static bool IsEmptyCurve( JToken _token ) =>
			_token is JObject o && o["keys"] is JArray keys && keys.Count == 0 && o["preWrapMode"] != null;

		/// <summary>
		/// A one-line reading of a shape, so a list of candidates can be skimmed without opening each one.
		/// </summary>
		private static string Summarise( JObject _shape )
		{
			var parts = new List<string>();

			string support = (string)_shape["m_support"];
			if (!string.IsNullOrEmpty(support) && support != "None")
				parts.Add(support);

			if (_shape["m_scaleLocked"]?.Value<bool>() == true)
				parts.Add("uniform");

			var duration = _shape["m_duration"];
			if (duration != null)
				parts.Add($"{Num(duration)}s");

			var delay = _shape["m_delay"];
			if (delay != null && delay.Value<double>() > 0)
				parts.Add($"delay {Num(delay)}s");

			var perChild = _shape["m_delayPerChild"];
			if (perChild != null && Math.Abs(perChild.Value<double>()) > 0.0001)
				parts.Add($"stagger {Num(perChild)}s/child");

			// Ranges included because without them distinct shapes summarised identically — three separate
			// entries all read "ScaleX, uniform, 0.15s, scaleX:2k" while differing in what they scale between.
			AddRange(parts, _shape, "scale", "m_scaleXStart", "m_scaleXEnd");
			AddRange(parts, _shape, "posX", "m_posXStart", "m_posXEnd");
			AddRange(parts, _shape, "posY", "m_posYStart", "m_posYEnd");
			AddRange(parts, _shape, "rotZ", "m_rotZStart", "m_rotZEnd");

			foreach (var channel in new[] { "m_scaleXCurve", "m_scaleYCurve", "m_alphaCurve",
			                                "m_posXCurve", "m_posYCurve", "m_rotZCurve" })
			{
				if (_shape[channel] is JObject curve && curve["keys"] is JArray keys && keys.Count > 0)
					parts.Add($"{channel.Substring(2).Replace("Curve", "")}:{DescribeCurve(keys)}");
			}

			if (_shape["m_autoOnEnable"]?.Value<bool>() == true)
				parts.Add("auto on enable");

			return parts.Count > 0 ? string.Join(", ", parts) : "(no motion channels)";
		}

		private static void AddRange( List<string> _parts, JObject _shape, string _label, string _from, string _to )
		{
			var from = _shape[_from];
			var to = _shape[_to];
			if (from == null || to == null)
				return;

			double a = from.Value<double>();
			double b = to.Value<double>();
			if (Math.Abs(a - b) < 0.0001)
				return;

			_parts.Add($"{_label} {Num(from)}→{Num(to)}");
		}

		/// <summary>Invariant formatting: the harvest is read by tools, and a decimal comma is not a number.</summary>
		private static string Num( JToken _token ) =>
			Math.Round(_token.Value<double>(), 3).ToString(System.Globalization.CultureInfo.InvariantCulture);

		/// <summary>
		/// A curve rendered so two shapes that differ only in their curve can be told apart — key values, "↗" for
		/// leaving the 0..1 band, and "~" for hand-set tangents.
		///
		/// The tangent mark is the interesting one. Keyframes are computable; the slope between them is the part
		/// that decides whether motion feels snappy or rubbery, and it is set by someone watching it. So "~"
		/// flags the shapes worth reusing over the ones that merely have the right numbers.
		/// </summary>
		private static string DescribeCurve( JArray _keys )
		{
			var values = new List<string>();
			bool overshoots = false;
			bool eased = false;

			foreach (var key in _keys)
			{
				double value = key["value"]?.Value<double>() ?? 0;
				values.Add(Math.Round(value, 3).ToString(System.Globalization.CultureInfo.InvariantCulture));

				if (value > 1.0001 || value < -0.0001)
					overshoots = true;

				double inTangent = key["inTangent"]?.Value<double>() ?? 0;
				double outTangent = key["outTangent"]?.Value<double>() ?? 0;
				if (Math.Abs(inTangent) > 0.0001 || Math.Abs(outTangent) > 0.0001)
					eased = true;
			}

			// Long curves are summarised by their extremes rather than every key, to keep a line readable.
			string body = values.Count <= 5
				? string.Join("/", values)
				: $"{values[0]}/…{values.Count - 2} keys…/{values[^1]}";

			return body + (overshoots ? "↗" : "") + (eased ? "~" : "");
		}

		private static string PathOf( Transform _root, Transform _transform )
		{
			if (_transform == _root)
				return "<root>";

			var parts = new List<string>();
			for (var t = _transform; t != null && t != _root; t = t.parent)
				parts.Insert(0, t.name);
			return string.Join("/", parts);
		}
	}
}
