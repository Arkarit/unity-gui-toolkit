using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Renders an animation as a CONTACT SHEET: the same prefab sampled at several points along its timeline,
	/// composed into one image.
	///
	/// Why this exists: a single screenshot cannot show motion, so an agent authoring an animation could only
	/// verify the serialized numbers and had to ask a human whether it looked right. This does not convey feel —
	/// timing and easing are judgements — but it does catch everything gross: nothing moves at all, the wrong
	/// target moves, an overshoot leaves the panel clipped, or a curve ends somewhere other than its end value
	/// and leaves the element permanently wrong.
	///
	/// It works in Edit Mode with no Play Mode and no library changes to the animation system: the animation is
	/// driven through <c>EditorPlay</c> and <c>UpdateInEditor</c>, which already exist for the Inspector's own
	/// test buttons. Duration is measured rather than assumed, because the real length includes slave animations
	/// and their delays and is not exposed.
	/// </summary>
	public static class UiScreenMotionPreview
	{
		/// <summary>The step the animation is advanced by. Small enough that sampling lands close to the mark.</summary>
		private const float StepSeconds = 1f / 60f;

		/// <summary>A runaway guard: 20 s of animation at 60 Hz. UI motion is a fraction of a second.</summary>
		private const int MaxSteps = 1200;

		private const int MaxColumns = 4;
		private const int CellGap = 6;

		/// <summary>Height of the per-cell progress strip that says WHERE in the timeline a frame sits.</summary>
		private const int TimeBarHeight = 5;

		public static JObject Capture( string _prefabPath, string _animationNode, int _frames, int _width,
			int _height, bool _backwards, JObject _populate )
		{
			int frames = Mathf.Clamp(_frames > 0 ? _frames : 5, 2, 12);

			// The session holds the instance between frames — the whole reason the single-shot path could not be
			// reused: rebuilding the scene per frame would reset whatever is being animated.
			using var session = UiScreenPreview.BeginSession(_prefabPath, _width, _height);

			var populated = Populate(session.Instance, _populate);

			var animation = FindAnimation(session.Instance, _animationNode, out string resolvedNode,
				out var available);
			if (animation == null)
			{
				throw new ArgumentException(
					$"No UiSimpleAnimationBase found{(string.IsNullOrEmpty(_animationNode) ? "" : $" at node '{_animationNode}'")} " +
					$"in '{_prefabPath}'. Animations in this prefab: " +
					(available.Count > 0 ? string.Join(", ", available) : "(none)") + ".");
			}

			if (_backwards && !animation.HasBackwardsAnimation && !animation.IsBackwards)
			{
				// Not fatal — playing backwards still runs the same curves in reverse — but worth saying, since
				// a dedicated backwards animation would look different from a reversed forward one.
				Debug.Log($"[UiScreenMotionPreview] '{resolvedNode}' has no separate backwards animation; " +
					"reversing the forward one.");
			}

			// Reset first: a children animation fills its slave list from the container only when it collects,
			// which Reset/Play do. Collecting before that found an empty list and filmed the orchestrator alone.
			animation.Reset();

			// The master and everything it drives. Each animation advances through its OWN Update, which Unity
			// calls in Play Mode but not here — so stepping only the master would show the panel popping while
			// the click catcher never fades, and the filmstrip would quietly report half the motion as all of it.
			var driven = CollectDriven(animation);
			float duration = MeasureDuration(driven, _backwards, out int measuredSteps);

			var textures = new List<Texture2D>();
			var times = new JArray();
			try
			{
				// Second pass: the same stepping again, rendering when a sample point is reached. Stepping is
				// repeated rather than reused so every frame comes from the same integration the measurement saw.
				animation.Reset();
				animation.EditorPlay(_backwards);

				int nextSample = 0;
				for (int step = 0; step <= measuredSteps && nextSample < frames; step++)
				{
					// Sample points are spread evenly over the measured length, first frame at 0, last at the end.
					int sampleStep = frames == 1 ? 0 : (int)Math.Round(measuredSteps * (double)nextSample / (frames - 1));
					if (step == sampleStep)
					{
						textures.Add(session.RenderFrame());
						times.Add(measuredSteps == 0 ? 0f : (float)Math.Round(step * StepSeconds, 4));
						nextSample++;
						// Several sample points can fall on the same step for a very short animation.
						while (nextSample < frames &&
						       (int)Math.Round(measuredSteps * (double)nextSample / (frames - 1)) == step)
						{
							textures.Add(session.RenderFrame());
							times.Add((float)Math.Round(step * StepSeconds, 4));
							nextSample++;
						}
					}

					if (step < measuredSteps)
						Step(driven);
				}

				// If the animation finished early, pad with the final state so the sheet always has `frames` cells.
				while (textures.Count < frames)
				{
					textures.Add(session.RenderFrame());
					times.Add((float)Math.Round(duration, 4));
				}

				var sheet = Compose(textures, out int columns, out int rows);
				try
				{
					return new JObject
					{
						["png"] = Convert.ToBase64String(sheet.EncodeToPNG()),
						["width"] = sheet.width,
						["height"] = sheet.height,
						["frames"] = textures.Count,
						["columns"] = columns,
						["rows"] = rows,
						["cellWidth"] = textures[0].width,
						["cellHeight"] = textures[0].height,
						["animationNode"] = resolvedNode,
						["animationType"] = animation.GetType().Name,
						["backwards"] = _backwards,
						["measuredDuration"] = (float)Math.Round(duration, 4),
						// What actually moved. If a slave you expected is absent here, it is not wired as a slave —
						// which is the difference between "the animation looks wrong" and "it was never driven".
						["drivenAnimations"] = new JArray(
							driven.Select(_a => (JToken)PathOf(session.Instance.transform, _a.transform))),
						["populated"] = populated,
						["times"] = times,
						["readingOrder"] = "left to right, top to bottom; each cell carries a progress strip at " +
							"its bottom edge showing where in the timeline it sits",
						["animationsInPrefab"] = new JArray(available),
					};
				}
				finally
				{
					UnityEngine.Object.DestroyImmediate(sheet);
				}
			}
			finally
			{
				foreach (var texture in textures)
				{
					if (texture != null)
						UnityEngine.Object.DestroyImmediate(texture);
				}
				// Leave the instance in a defined state; it is about to be thrown away, but a half-played
				// animation on a shared asset would be a nasty surprise if that ever changed.
				animation.Reset();
			}
		}

		/// <summary>
		/// Plays without rendering, counting steps until nothing is playing any more. The real length includes
		/// slave animations and their delays, and nothing exposes it — so measuring is cheaper than reimplementing
		/// the recursive sum and risking disagreement with it.
		/// </summary>
		private static float MeasureDuration( List<UiSimpleAnimationBase> _driven, bool _backwards, out int _steps )
		{
			var master = _driven[0];
			master.Reset();
			master.EditorPlay(_backwards);

			int steps = 0;
			while (steps < MaxSteps && _driven.Any(_a => _a.IsPlaying))
			{
				Step(_driven);
				steps++;
			}

			master.Reset();
			_steps = steps;
			return steps * StepSeconds;
		}

		private static void Step( List<UiSimpleAnimationBase> _driven )
		{
			foreach (var animation in _driven)
				animation.UpdateInEditor(StepSeconds);
		}

		/// <summary>
		/// The master first, then every animation it drives: slaves transitively, plus any dedicated backwards
		/// animation, since that is what plays instead of the forward one when a panel closes.
		/// </summary>
		private static List<UiSimpleAnimationBase> CollectDriven( UiSimpleAnimationBase _master )
		{
			var ordered = new List<UiSimpleAnimationBase>();
			var seen = new HashSet<UiSimpleAnimationBase>();

			void Visit( UiSimpleAnimationBase _animation )
			{
				if (_animation == null || !seen.Add(_animation))
					return;

				ordered.Add(_animation);

				if (_animation.BackwardsAnimation != null)
					Visit(_animation.BackwardsAnimation);

				var slaves = _animation.SlaveAnimations;
				if (slaves == null)
					return;
				foreach (var slave in slaves)
					Visit(slave);
			}

			Visit(_master);
			return ordered;
		}

		/// <summary>
		/// Fills a container with instances of a row prefab before filming.
		///
		/// Without this the tool is blind to the project's most common list pattern: a container plus a row
		/// prefab spawned per item at runtime. Such a screen holds NO rows as an asset, so a staggered entrance
		/// has nothing to collect at edit time and films as an empty strip — the animation would look absent
		/// when it is merely unpopulated. The rows live in the throw-away preview scene only; the asset is
		/// never touched.
		/// </summary>
		private static JObject Populate( GameObject _root, JObject _populate )
		{
			if (_populate == null)
				return null;

			string containerPath = (string)_populate["container"];
			string rowPrefabPath = (string)_populate["prefab"];
			int count = Mathf.Clamp((int?)_populate["count"] ?? 3, 1, 32);

			if (string.IsNullOrEmpty(containerPath) || string.IsNullOrEmpty(rowPrefabPath))
				throw new ArgumentException("'populate' needs both a 'container' node path and a 'prefab' path.");

			var container = containerPath == "<root>" ? _root.transform : _root.transform.Find(containerPath);
			if (container == null)
				throw new ArgumentException($"populate: no node '{containerPath}' in the prefab.");

			var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rowPrefabPath);
			if (rowPrefab == null)
				throw new ArgumentException($"populate: no prefab at '{rowPrefabPath}'.");

			for (int i = 0; i < count; i++)
			{
				var row = (GameObject)PrefabUtility.InstantiatePrefab(rowPrefab, container);
				row.name = $"{rowPrefab.name} ({i})";
			}

			return new JObject
			{
				["container"] = containerPath,
				["prefab"] = rowPrefabPath,
				["count"] = count,
			};
		}

		private static UiSimpleAnimationBase FindAnimation( GameObject _root, string _nodePath,
			out string _resolvedNode, out List<string> _available )
		{
			_available = new List<string>();
			foreach (var candidate in _root.GetComponentsInChildren<UiSimpleAnimationBase>(true))
				_available.Add(PathOf(_root.transform, candidate.transform));

			if (!string.IsNullOrEmpty(_nodePath))
			{
				var transform = _nodePath == "<root>" ? _root.transform : _root.transform.Find(_nodePath);
				var found = transform != null ? transform.GetComponent<UiSimpleAnimationBase>() : null;
				_resolvedNode = _nodePath;
				return found;
			}

			// Default to the animation that drives the whole screen: the one on the root, which is what UiPanel
			// itself picks up as the show/hide animation.
			var onRoot = _root.GetComponent<UiSimpleAnimationBase>();
			if (onRoot != null)
			{
				_resolvedNode = "<root>";
				return onRoot;
			}

			var first = _root.GetComponentInChildren<UiSimpleAnimationBase>(true);
			_resolvedNode = first != null ? PathOf(_root.transform, first.transform) : null;
			return first;
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

		/// <summary>
		/// Lays the frames out in a grid, first frame top-left. One image rather than N: it reads as a filmstrip,
		/// and it costs a fraction of the tokens that separate images would.
		/// </summary>
		private static Texture2D Compose( List<Texture2D> _frames, out int _columns, out int _rows )
		{
			int cellWidth = _frames[0].width;
			int cellHeight = _frames[0].height;

			_columns = Mathf.Min(_frames.Count, MaxColumns);
			_rows = (_frames.Count + _columns - 1) / _columns;

			int sheetWidth = _columns * cellWidth + (_columns - 1) * CellGap;
			int sheetHeight = _rows * cellHeight + (_rows - 1) * CellGap;

			var sheet = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGBA32, false, false);

			var background = new Color32(10, 10, 12, 255);
			var backgroundPixels = new Color32[sheetWidth * sheetHeight];
			for (int i = 0; i < backgroundPixels.Length; i++)
				backgroundPixels[i] = background;
			sheet.SetPixels32(backgroundPixels);

			for (int index = 0; index < _frames.Count; index++)
			{
				int column = index % _columns;
				int row = index / _columns;

				int x = column * (cellWidth + CellGap);
				// Texture rows run bottom-up, so the first row has to land at the TOP of the sheet.
				int y = sheetHeight - (row + 1) * cellHeight - row * CellGap;

				var pixels = _frames[index].GetPixels32();
				StampTimeBar(pixels, cellWidth, cellHeight,
					_frames.Count == 1 ? 1f : index / (float)(_frames.Count - 1));
				sheet.SetPixels32(x, y, cellWidth, cellHeight, pixels);
			}

			sheet.Apply(false, false);
			return sheet;
		}

		/// <summary>
		/// Burns a progress strip into the bottom of a cell. Without it, frames of a subtle animation are hard to
		/// tell apart and impossible to order at a glance — and drawing a bar needs no font.
		/// </summary>
		private static void StampTimeBar( Color32[] _pixels, int _width, int _height, float _normalizedTime )
		{
			var filled = new Color32(80, 200, 255, 255);
			var empty = new Color32(40, 40, 48, 255);
			int filledWidth = Mathf.RoundToInt(Mathf.Clamp01(_normalizedTime) * _width);

			for (int row = 0; row < TimeBarHeight && row < _height; row++)
			{
				int rowStart = row * _width;   // row 0 is the bottom edge
				for (int column = 0; column < _width; column++)
					_pixels[rowStart + column] = column < filledWidth ? filled : empty;
			}
		}
	}
}
