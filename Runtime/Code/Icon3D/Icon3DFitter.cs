using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Frames an object for a 3D icon: from its world bounds to camera position, projection and clip planes.
	/// </summary>
	public static class Icon3DFitter
	{
		private static readonly List<Renderer> s_renderers = new();
		private static readonly Vector3[] s_corners = new Vector3[8];

		/// <summary>
		/// World bounds of all renderers below _root. Particle, trail and line renderers are ignored by default:
		/// their bounds depend on simulation state and make the framing jump.
		/// Renderer bounds are only valid while the renderers are active.
		/// </summary>
		public static bool TryGetBounds( GameObject _root, out Bounds _bounds, bool _includeEffects = false )
		{
			_bounds = default;
			bool found = false;

			_root.GetComponentsInChildren(false, s_renderers);
			foreach (var rend in s_renderers)
			{
				if (!rend.enabled)
					continue;

				if (!_includeEffects && (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer))
					continue;

				if (!found)
				{
					_bounds = rend.bounds;
					found = true;
				}
				else
				{
					_bounds.Encapsulate(rend.bounds);
				}
			}

			s_renderers.Clear();
			return found;
		}

		/// <summary>
		/// Position, rotate and configure _camera so that _bounds fills the image, leaving _padding
		/// (fraction of the image size) free on every side.
		/// </summary>
		public static void Fit
		(
			Camera _camera,
			Bounds _bounds,
			Quaternion _viewRotation,
			UiIcon3DPreset.EProjection _projection,
			float _fieldOfView,
			UiIcon3DPreset.EFitMode _fitMode,
			float _padding,
			float _aspect
		)
		{
			float fill = Mathf.Clamp(1 - 2 * _padding, 0.01f, 1);
			_aspect = Mathf.Max(_aspect, 0.0001f);
			var center = _bounds.center;
			var forward = _viewRotation * Vector3.forward;

			_camera.aspect = _aspect;
			_camera.orthographic = _projection == UiIcon3DPreset.EProjection.Orthographic;
			_camera.fieldOfView = _fieldOfView;

			float distance, near, far;

			if (_fitMode == UiIcon3DPreset.EFitMode.Sphere)
			{
				float radius = Mathf.Max(_bounds.extents.magnitude, 0.0001f);
				if (_camera.orthographic)
				{
					_camera.orthographicSize = radius / Mathf.Min(1, _aspect) / fill;
					distance = radius * 2;
				}
				else
				{
					float tanMin = Mathf.Tan(_fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Min(1, _aspect) * fill;
					distance = radius / Mathf.Sin(Mathf.Atan(tanMin));
				}

				near = distance - radius;
				far = distance + radius;
			}
			else
			{
				// Corners in camera space relative to the bounds center (camera at the center, before moving back)
				GetCorners(_bounds, s_corners);
				var toCamera = Quaternion.Inverse(_viewRotation);
				for (int i = 0; i < s_corners.Length; i++)
					s_corners[i] = toCamera * (s_corners[i] - center);

				if (_camera.orthographic)
				{
					float maxX = 0, maxY = 0, minZ = float.MaxValue, maxZ = float.MinValue;
					foreach (var c in s_corners)
					{
						maxX = Mathf.Max(maxX, Mathf.Abs(c.x));
						maxY = Mathf.Max(maxY, Mathf.Abs(c.y));
						minZ = Mathf.Min(minZ, c.z);
						maxZ = Mathf.Max(maxZ, c.z);
					}

					_camera.orthographicSize = Mathf.Max(Mathf.Max(maxY, maxX / _aspect) / fill, 0.0001f);
					distance = -minZ + Mathf.Max(maxZ - minZ, 0.0001f);
					near = distance + minZ;
					far = distance + maxZ;
				}
				else
				{
					float tanV = Mathf.Tan(_fieldOfView * 0.5f * Mathf.Deg2Rad) * fill;
					float tanH = tanV * _aspect;
					distance = 0;
					foreach (var c in s_corners)
					{
						distance = Mathf.Max(distance, Mathf.Abs(c.x) / tanH - c.z);
						distance = Mathf.Max(distance, Mathf.Abs(c.y) / tanV - c.z);
						distance = Mathf.Max(distance, -c.z + 0.0001f);
					}

					near = float.MaxValue;
					far = float.MinValue;
					foreach (var c in s_corners)
					{
						near = Mathf.Min(near, distance + c.z);
						far = Mathf.Max(far, distance + c.z);
					}
				}
			}

			// Some slack so the object is never clipped by rounding; near must stay positive for perspective
			float depth = Mathf.Max(far - near, 0.0001f);
			near = Mathf.Max(near - depth * 0.05f, _camera.orthographic ? 0 : distance * 0.001f);
			far += depth * 0.05f;

			_camera.transform.SetPositionAndRotation(center - forward * distance, _viewRotation);
			_camera.nearClipPlane = Mathf.Max(near, 0.0001f);
			_camera.farClipPlane = Mathf.Max(far, _camera.nearClipPlane + 0.001f);
		}

		private static void GetCorners( Bounds _bounds, Vector3[] _corners )
		{
			var min = _bounds.min;
			var max = _bounds.max;
			_corners[0] = new Vector3(min.x, min.y, min.z);
			_corners[1] = new Vector3(max.x, min.y, min.z);
			_corners[2] = new Vector3(min.x, max.y, min.z);
			_corners[3] = new Vector3(max.x, max.y, min.z);
			_corners[4] = new Vector3(min.x, min.y, max.z);
			_corners[5] = new Vector3(max.x, min.y, max.z);
			_corners[6] = new Vector3(min.x, max.y, max.z);
			_corners[7] = new Vector3(max.x, max.y, max.z);
		}
	}
}
