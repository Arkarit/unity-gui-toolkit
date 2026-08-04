using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A topology-changing mesh-effect modifier (UiGradientBase) that applies a Unity Gradient across the mesh of the
	/// Graphic or TextMeshPro text on the same GameObject, horizontally or vertically, optionally subdividing the mesh
	/// at the gradient's keys (including hard steps for Fixed-mode gradients).
	/// </summary>
	[ExecuteAlways]
	public class UiGradientEx : UiGradientBase
	{
		protected enum Type
		{
			Horizontal,
			Vertical,
		}

		[SerializeField]
		protected Gradient m_gradient = new Gradient();
		[SerializeField]
		protected Type m_type = Type.Vertical;
		[SerializeField]
		protected bool m_splitAtKeys = true;

		// Width of the duplicated-vertex sliver used to render hard color steps in GradientMode.Fixed.
		// Each Fixed-mode key time t becomes two iso-lines at (t - eps) and (t + eps) so that
		// vertex-position-based color sampling produces the desired step (instead of GPU-smoothing
		// across the key). Picked to be well below sub-pixel size at any plausible UI resolution.
		private const float FIXED_MODE_EPSILON = 1e-4f;

		private static readonly SortedSet<float> s_KeyTimesSet = new();
		private static readonly List<float> s_KeyTimesList = new();

		public Gradient Gradient
		{
			get => m_gradient;
			set => m_gradient = value;
		}

		protected override bool ChangesTopology { get {return true;} }

		protected override Color GetColor( Vector2 _normVal )
		{
			return m_gradient.Evaluate( m_type == Type.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}

		protected override void Prepare( VertexHelper _vh )
		{
			if (!m_splitAtKeys)
				return;

			try
			{
				bool isFixed = m_gradient.mode == GradientMode.Fixed;
				foreach (var key in m_gradient.colorKeys)
					AddKey(key.time, isFixed);
				foreach (var key in m_gradient.alphaKeys)
					AddKey(key.time, isFixed);

				if (s_KeyTimesSet.Count == 0)
					return;

				s_KeyTimesSet.ToList(s_KeyTimesList);

				if (m_type == Type.Horizontal)
					UiMeshModifierUtility.Subdivide(_vh, s_KeyTimesList, null);
				else
					UiMeshModifierUtility.Subdivide(_vh, null, s_KeyTimesList);
			}
			finally
			{
				s_KeyTimesSet.Clear();
				s_KeyTimesList.Clear();
			}
		}

		private void AddKey(float _keyTime, bool _isFixed)
		{
			float t = m_type == Type.Horizontal ? _keyTime : 1.0f - _keyTime;
			if (_isFixed)
			{
				float a = t - FIXED_MODE_EPSILON;
				float b = t + FIXED_MODE_EPSILON;
				if (a > 0f && a < 1f)
					s_KeyTimesSet.Add(a);
				if (b > 0f && b < 1f)
					s_KeyTimesSet.Add(b);
			}
			else if (t > 0f && t < 1f)
			{
				s_KeyTimesSet.Add(t);
			}
		}

	}
}
