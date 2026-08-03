using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor.AiSupport
{
	/// <summary>
	/// Asks the running app what a tap would actually hit, and optionally performs it.
	///
	/// This answers a question no screenshot can. "Is this button clickable" is decided by the raycast, and a
	/// full-rect frame overlay with raycastTarget left on will swallow every click underneath it while looking
	/// completely correct — that exact defect sat on the reward screen's tab bar and could only be reasoned
	/// about, not shown.
	///
	/// So a click here goes through the raycast rather than around it: naming a target computes its centre and
	/// raycasts there, and the answer includes the whole hit stack. If the top of that stack is not the thing
	/// that was named, that IS the finding — and invoking the button's event directly would have hidden it.
	/// </summary>
	public static class UiRuntimeProbe
	{
		public static JObject Probe( string _payload )
		{
			var request = string.IsNullOrWhiteSpace(_payload) ? new JObject() : JObject.Parse(_payload);

			if (!EditorApplication.isPlaying)
			{
				throw new Exception("Not in Play Mode, so there is no running UI to probe. " +
				                    "Use playMode/enter, or ask the human to bring the app to the right state.");
			}

			var eventSystem = EventSystem.current;
			if (eventSystem == null)
				throw new Exception("No EventSystem in the running scene, so nothing can be raycast or clicked.");

			string target = (string)request["target"];
			bool click = (bool?)request["click"] ?? false;

			Vector2 screenPoint;
			GameObject targetObject = null;

			if (!string.IsNullOrEmpty(target))
			{
				targetObject = FindTarget(target);
				var rect = targetObject.transform as RectTransform;
				if (rect == null)
					throw new Exception($"'{target}' has no RectTransform, so it has no screen position.");

				screenPoint = ScreenCentreOf(rect);
			}
			else
			{
				var x = request["x"];
				var y = request["y"];
				if (x == null || y == null)
					throw new Exception("Give either a 'target' node to aim at, or an 'x'/'y' screen position.");

				// Top-left origin, because that is how a captured image reads. Unity's own screen space is
				// bottom-left, so a position taken off a screenshot would otherwise land mirrored.
				string origin = ((string)request["origin"] ?? "topLeft").ToLowerInvariant();
				float py = (float)y;
				screenPoint = new Vector2((float)x, origin == "bottomleft" ? py : Screen.height - py);
			}

			var pointer = new PointerEventData(eventSystem)
			{
				position = screenPoint,
				button = PointerEventData.InputButton.Left,
			};

			var hits = new List<RaycastResult>();
			eventSystem.RaycastAll(pointer, hits);

			var stack = new JArray();
			foreach (var hit in hits)
			{
				stack.Add(new JObject
				{
					["node"] = PathOf(hit.gameObject),
					["module"] = hit.module != null ? hit.module.GetType().Name : null,
					["sortingOrder"] = hit.sortingOrder,
					["depth"] = hit.depth,
					["distance"] = hit.distance,
				});
			}

			var result = new JObject
			{
				["screenPoint"] = new JArray(screenPoint.x, screenPoint.y),
				["screenSize"] = new JArray(Screen.width, Screen.height),
				["target"] = targetObject != null ? PathOf(targetObject) : null,
				["hits"] = stack,
				["topmost"] = hits.Count > 0 ? PathOf(hits[0].gameObject) : null,
			};

			if (targetObject != null)
			{
				// The whole point: does a tap aimed at this thing actually reach it, or does something on top
				// take it? "Blocked by" names the culprit, which is what a raycastTarget left on looks like.
				bool reaches = hits.Count > 0 && IsSelfOrDescendant(hits[0].gameObject, targetObject);
				result["targetReceivesInput"] = reaches;
				if (!reaches && hits.Count > 0)
					result["blockedBy"] = PathOf(hits[0].gameObject);
			}

			if (click)
			{
				if (hits.Count == 0)
				{
					result["clicked"] = null;
					result["clickNote"] = "Nothing under that position, so nothing was clicked.";
				}
				else
				{
					var top = hits[0].gameObject;
					pointer.pointerCurrentRaycast = hits[0];
					pointer.pointerPressRaycast = hits[0];

					// Down, up, click — the sequence a real tap produces, so anything listening for the parts
					// rather than the whole behaves as it would for a user.
					var pressed = ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerDownHandler);
					pointer.pointerPress = pressed;
					ExecuteEvents.Execute(top, pointer, ExecuteEvents.pointerUpHandler);
					var handler = ExecuteEvents.ExecuteHierarchy(top, pointer, ExecuteEvents.pointerClickHandler);

					result["clicked"] = PathOf(top);
					result["handledBy"] = handler != null ? PathOf(handler) : null;
					if (handler == null)
					{
						result["clickNote"] = "The click reached this node but nothing in its parents handles a " +
							"pointer click, so it had no effect.";
					}
				}
			}

			return result;
		}

		/// <summary>
		/// The centre of a rect in screen coordinates. Overlay canvases have no camera, and passing one anyway
		/// yields a position that is subtly wrong rather than obviously wrong.
		/// </summary>
		private static Vector2 ScreenCentreOf( RectTransform _rect )
		{
			var canvas = _rect.GetComponentInParent<Canvas>();
			Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
				? canvas.worldCamera
				: null;

			var corners = new Vector3[4];
			_rect.GetWorldCorners(corners);
			Vector3 centre = (corners[0] + corners[2]) * 0.5f;
			return RectTransformUtility.WorldToScreenPoint(camera, centre);
		}

		private static GameObject FindTarget( string _target )
		{
			var candidates = new List<GameObject>();

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded)
					continue;

				foreach (var root in scene.GetRootGameObjects())
					Collect(root.transform, _target, candidates);
			}

			if (candidates.Count == 1)
				return candidates[0];

			if (candidates.Count == 0)
				throw new Exception($"No active object matching '{_target}'. Pass a node name, or a path ending " +
				                    "in one, exactly as it appears in the hierarchy.");

			throw new Exception($"'{_target}' matches {candidates.Count} objects: " +
			                    string.Join(", ", candidates.Take(8).Select(PathOf)) +
			                    ". Pass more of the path to disambiguate.");
		}

		private static void Collect( Transform _transform, string _target, List<GameObject> _into )
		{
			string path = PathOf(_transform.gameObject);
			if (_transform.name == _target || path == _target || path.EndsWith("/" + _target, StringComparison.Ordinal))
				_into.Add(_transform.gameObject);

			foreach (Transform child in _transform)
				Collect(child, _target, _into);
		}

		private static bool IsSelfOrDescendant( GameObject _candidate, GameObject _of )
		{
			for (var t = _candidate.transform; t != null; t = t.parent)
			{
				if (t.gameObject == _of)
					return true;
			}
			return false;
		}

		private static string PathOf( GameObject _go )
		{
			var parts = new List<string>();
			for (var t = _go.transform; t != null; t = t.parent)
				parts.Insert(0, t.name);
			return string.Join("/", parts);
		}
	}
}
