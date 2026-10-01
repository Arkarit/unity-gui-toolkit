using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Optional, on an object shown as a 3D icon: overrides the automatic framing where it fails - skinned meshes
	/// whose bounds include the whole bind pose, particles, odd pivots, or objects that should be framed around
	/// one detail only. Can also define the view the object prefers (e.g. a sword seen from the side).
	///
	/// The renderer uses the first enabled hint in the object's hierarchy.
	/// </summary>
	[DisallowMultipleComponent]
	public class UiIcon3DBoundsHint : MonoBehaviour
	{
		[Tooltip("Bounds to frame, in this GameObject's local space")]
		[SerializeField] private Bounds m_bounds = new(Vector3.zero, Vector3.one);
		[Tooltip("Use the view rotation below instead of the preset's (an icon's own override still wins)")]
		[SerializeField] private bool m_overrideViewRotation;
		[Tooltip("Camera rotation in this GameObject's local space (euler angles); (0,180,0) looks at the local +Z side")]
		[SerializeField] private Vector3 m_viewRotation = new(15, 150, 0);

		public Bounds LocalBounds
		{
			get => m_bounds;
			set => m_bounds = value;
		}

		public bool OverrideViewRotation
		{
			get => m_overrideViewRotation;
			set => m_overrideViewRotation = value;
		}

		public Quaternion LocalViewRotation
		{
			get => Quaternion.Euler(m_viewRotation);
			set => m_viewRotation = value.eulerAngles;
		}

		public Quaternion WorldViewRotation => transform.rotation * LocalViewRotation;

		/// <summary>Axis aligned world bounds of the local bounds box.</summary>
		public Bounds WorldBounds
		{
			get
			{
				var center = m_bounds.center;
				var extents = m_bounds.extents;
				var result = new Bounds(transform.TransformPoint(center), Vector3.zero);
				for (int i = 0; i < 8; i++)
				{
					var corner = center + new Vector3(
						(i & 1) == 0 ? -extents.x : extents.x,
						(i & 2) == 0 ? -extents.y : extents.y,
						(i & 4) == 0 ? -extents.z : extents.z);
					result.Encapsulate(transform.TransformPoint(corner));
				}

				return result;
			}
		}

		/// <summary>Set the bounds to what the renderers below this GameObject currently cover.</summary>
		public bool FitToRenderers( bool _includeEffects = false )
		{
			if (!Icon3DFitter.TryGetBounds(gameObject, out var world, _includeEffects))
				return false;

			// Back into local space: encapsulate the world box corners
			var min = world.min;
			var max = world.max;
			var local = new Bounds(transform.InverseTransformPoint(world.center), Vector3.zero);
			for (int i = 0; i < 8; i++)
			{
				var corner = new Vector3(
					(i & 1) == 0 ? min.x : max.x,
					(i & 2) == 0 ? min.y : max.y,
					(i & 4) == 0 ? min.z : max.z);
				local.Encapsulate(transform.InverseTransformPoint(corner));
			}

			m_bounds = local;
			return true;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(m_bounds.center, m_bounds.size);

			if (!m_overrideViewRotation)
				return;

			// View direction: from where the camera looks
			var direction = LocalViewRotation * Vector3.forward;
			float length = m_bounds.extents.magnitude * 2;
			Gizmos.DrawLine(m_bounds.center - direction * length, m_bounds.center);
		}
	}
}
