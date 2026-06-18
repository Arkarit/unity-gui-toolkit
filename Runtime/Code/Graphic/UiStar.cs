using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Star-shaped UI image (variable spike count, configurable notch depth, optional frame and fade).
	///
	/// The star is inscribed in the (padded / fixed-sized) RectTransform's rect: its outer radius
	/// is min(rect.width, rect.height) / 2 and it is centered on rect.center. The shape is a
	/// 2N-vertex polygon alternating between outer-radius spike tips and inner-radius notches.
	///
	/// Frame and fade are produced by computing inset copies of the perimeter. Two strategies are
	/// available (see InsetStrategy): Miter (constant perpendicular frame thickness, clamped by
	/// m_miterLimit at sharp tips) and UniformScale (always robust, but frame appears thinner at
	/// notches than at tips). Miter is the default and matches what users typically expect.
	///
	/// UV mapping uses the inherited GetUv: each vertex's position is linearly mapped to the
	/// rect-aligned sprite UV. A rotated star therefore keeps its sprite oriented to the rect, not
	/// to the star — i.e. the spikes move under a fixed sprite. To rotate sprite-with-star, rotate
	/// the RectTransform instead.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiStar : UiShapeImage
	{
		public const int MinSpikeCount = 3;
		public const int MaxSpikeCount = 50;

		public const float MinInnerRadiusRatio = 0.05f;
		public const float MaxInnerRadiusRatio = 0.95f;

		public const float MinRotation = -180f;
		public const float MaxRotation = 180f;

		public const float MinMiterLimit = 1f;
		public const float MaxMiterLimit = 20f;

		public enum InsetStrategy
		{
			/// <summary>
			/// Mitered inset: each perimeter edge is offset inward along its normal by the inset distance,
			/// and new vertices sit at the intersections. Produces a constant perpendicular frame thickness.
			/// Miter overshoot at sharp spike tips is clamped by m_miterLimit (the maximum ratio of
			/// miter length to inset).
			/// </summary>
			Miter,

			/// <summary>
			/// Inner perimeter is the outer perimeter uniformly scaled toward the rect center. Always
			/// robust and never self-intersects, but the frame appears thinner at notches than at spike tips.
			/// </summary>
			UniformScale,
		}

		[Tooltip("Number of star spikes.")]
		[UnityEngine.Range(MinSpikeCount, MaxSpikeCount)]
		[SerializeField] protected int m_spikeCount = 5;

		[Tooltip("Inner radius as a fraction of the outer radius (0 = notches at center, 1 = no notch). "
		         + "Typical pointy stars: 0.4-0.55. Sunburst / shallow notches: 0.85-0.95.")]
		[UnityEngine.Range(MinInnerRadiusRatio, MaxInnerRadiusRatio)]
		[SerializeField] protected float m_innerRadiusRatio = 0.5f;

		[Tooltip("Rotation around the rect center, in degrees. 0 = first spike points straight up.")]
		[UnityEngine.Range(MinRotation, MaxRotation)]
		[SerializeField] protected float m_rotation = 0f;

		[Tooltip("How the inner perimeter is computed when a frame or fade is active.")]
		[SerializeField] protected InsetStrategy m_insetStrategy = InsetStrategy.Miter;

		[Tooltip("Miter limit (only used by InsetStrategy.Miter). Maximum ratio of miter length to inset; "
		         + "at sharper tips, the miter is clamped along the bisector. Higher = sharper tips preserved, "
		         + "lower = tips blunted earlier.")]
		[UnityEngine.Range(MinMiterLimit, MaxMiterLimit)]
		[SerializeField] protected float m_miterLimit = 4f;

		public int SpikeCount
		{
			get => m_spikeCount;
			set
			{
				CheckSetterRange(nameof(SpikeCount), value, MinSpikeCount, MaxSpikeCount);
				m_spikeCount = value;
				SetVerticesDirty();
			}
		}

		public float InnerRadiusRatio
		{
			get => m_innerRadiusRatio;
			set
			{
				CheckSetterRange(nameof(InnerRadiusRatio), value, MinInnerRadiusRatio, MaxInnerRadiusRatio);
				m_innerRadiusRatio = value;
				SetVerticesDirty();
			}
		}

		public float Rotation
		{
			get => m_rotation;
			set
			{
				CheckSetterRange(nameof(Rotation), value, MinRotation, MaxRotation);
				m_rotation = value;
				SetVerticesDirty();
			}
		}

		public InsetStrategy Inset
		{
			get => m_insetStrategy;
			set
			{
				if (m_insetStrategy == value)
					return;

				m_insetStrategy = value;
				SetVerticesDirty();
			}
		}

		public float MiterLimit
		{
			get => m_miterLimit;
			set
			{
				CheckSetterRange(nameof(MiterLimit), value, MinMiterLimit, MaxMiterLimit);
				m_miterLimit = value;
				SetVerticesDirty();
			}
		}

		protected override void GenerateFilled()
		{
			var rect = Rect;
			Vector2 center = rect.center;
			float r = Mathf.Min(rect.width, rect.height) * 0.5f;

			BuildPerimeter(center, r, s_perimA);

			if (Mathf.Approximately(0, m_fadeSize))
			{
				EmitFilledFromPerimeter(center, s_perimA, color);
				return;
			}

			BuildInsetPerimeter(s_perimA, center, m_fadeSize, s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, m_fadeColor, color);
			EmitFilledFromPerimeter(center, s_perimB, color);
		}

		protected override void GenerateFrame()
		{
			var rect = Rect;
			Vector2 center = rect.center;
			float r = Mathf.Min(rect.width, rect.height) * 0.5f;

			BuildPerimeter(center, r, s_perimA);

			if (Mathf.Approximately(0, m_fadeSize))
			{
				BuildInsetPerimeter(s_perimA, center, m_frameSize, s_perimB);
				EmitFrameStripFromPerimeters(s_perimA, s_perimB, color, color);
				return;
			}

			// 3-ring sandwich: outer-fade band, solid frame band, inner-fade band.
			// Each band's inner perimeter becomes the next band's outer perimeter.
			BuildInsetPerimeter(s_perimA, center, m_fadeSize, s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, m_fadeColor, color);

			CopyPerimeter(s_perimB, s_perimA);
			BuildInsetPerimeter(s_perimA, center, m_frameSize - m_fadeSize * 2, s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, color, color);

			CopyPerimeter(s_perimB, s_perimA);
			BuildInsetPerimeter(s_perimA, center, m_fadeSize, s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, color, m_fadeColor);
		}

		private void BuildPerimeter( Vector2 _center, float _outerRadius, List<Vector2> _out )
		{
			_out.Clear();
			int n = m_spikeCount;
			float innerR = _outerRadius * m_innerRadiusRatio;
			float baseAngle = m_rotation * Mathf.Deg2Rad;
			float step = Mathf.PI / n;

			// 2N vertices alternating spike/notch, traversed clockwise (with sin/cos convention,
			// increasing angle goes top -> right -> bottom -> left -> top).
			for (int i = 0; i < 2 * n; i++)
			{
				float a = baseAngle + i * step;
				float r = (i % 2 == 0) ? _outerRadius : innerR;
				_out.Add(new Vector2(_center.x + Mathf.Sin(a) * r, _center.y + Mathf.Cos(a) * r));
			}
		}

		private void BuildInsetPerimeter( List<Vector2> _src, Vector2 _center, float _inset, List<Vector2> _dst )
		{
			_dst.Clear();
			int count = _src.Count;
			if (count < 3 || _inset <= 0)
			{
				for (int i = 0; i < count; i++)
					_dst.Add(_src[i]);
				return;
			}

			switch (m_insetStrategy)
			{
				case InsetStrategy.UniformScale:
					BuildInsetUniformScale(_src, _center, _inset, _dst);
					return;
				case InsetStrategy.Miter:
				default:
					BuildInsetMiter(_src, _inset, _dst);
					return;
			}
		}

		private static void BuildInsetUniformScale( List<Vector2> _src, Vector2 _center, float _inset, List<Vector2> _dst )
		{
			float maxR = 0f;
			for (int i = 0; i < _src.Count; i++)
			{
				float d = (_src[i] - _center).magnitude;
				if (d > maxR)
					maxR = d;
			}

			if (maxR <= 0f)
			{
				for (int i = 0; i < _src.Count; i++)
					_dst.Add(_center);
				return;
			}

			float scale = (maxR - _inset) / maxR;
			if (scale <= 0f)
			{
				for (int i = 0; i < _src.Count; i++)
					_dst.Add(_center);
				return;
			}

			for (int i = 0; i < _src.Count; i++)
			{
				var p = _src[i];
				_dst.Add(new Vector2(
					_center.x + (p.x - _center.x) * scale,
					_center.y + (p.y - _center.y) * scale));
			}
		}

		private void BuildInsetMiter( List<Vector2> _src, float _inset, List<Vector2> _dst )
		{
			int n = _src.Count;
			float maxMiterLength = m_miterLimit * _inset;

			for (int i = 0; i < n; i++)
			{
				Vector2 prev = _src[(i - 1 + n) % n];
				Vector2 cur = _src[i];
				Vector2 next = _src[(i + 1) % n];

				Vector2 e1 = (cur - prev).normalized;
				Vector2 e2 = (next - cur).normalized;

				// Perimeter is generated clockwise in screen coords (+Y up), so the inward
				// normal is the edge direction rotated 90 degrees clockwise: (dx,dy) -> (dy,-dx).
				Vector2 n1 = new Vector2(e1.y, -e1.x);
				Vector2 n2 = new Vector2(e2.y, -e2.x);

				Vector2 bisRaw = n1 + n2;
				float bisLen = bisRaw.magnitude;
				if (bisLen < 1e-6f)
				{
					// Edges anti-parallel (180-degree turn). Inset purely along incoming normal.
					_dst.Add(cur + n1 * _inset);
					continue;
				}

				Vector2 bisector = bisRaw / bisLen;
				float cosHalf = Vector2.Dot(bisector, n1);

				float miterLength;
				if (cosHalf <= 1e-4f)
					miterLength = maxMiterLength;
				else
					miterLength = Mathf.Min(_inset / cosHalf, maxMiterLength);

				_dst.Add(cur + bisector * miterLength);
			}
		}

	}
}
