using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Circular UI image (variable segment count, optional frame and fade).
	///
	/// The circle is inscribed in the (padded / fixed-sized / size-offset) RectTransform's rect:
	/// its radius is min(rect.width, rect.height) / 2 and it is centered on rect.center.
	/// It is approximated by a regular N-gon with N = m_segments.
	///
	/// Because the underlying polygon is regular and convex, mitered and uniform-scale inset are
	/// equivalent here, so a single radial shrink is used for frame and fade rings (no strategy
	/// selection needed).
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiCircle : UiShapeImage
	{
		public const int MinSegments = 4;
		public const int MaxSegments = 300;

		[Tooltip("Number of segments approximating the circle. "
		         + "Higher = rounder, but more triangles. "
		         + "At low values (4, 5, 6, 8) you effectively get a regular polygon.")]
		[UnityEngine.Range(MinSegments, MaxSegments)]
		[SerializeField] protected int m_segments = 32;

		public int Segments
		{
			get => m_segments;
			set
			{
				CheckSetterRange(nameof(Segments), value, MinSegments, MaxSegments);
				m_segments = value;
				SetVerticesDirty();
			}
		}

		protected override void GenerateFilled()
		{
			var rect = Rect;
			Vector2 center = rect.center;
			float r = Mathf.Min(rect.width, rect.height) * 0.5f;

			BuildRing(center, r, s_perimA);

			if (Mathf.Approximately(0, m_fadeSize))
			{
				EmitFilledFromPerimeter(center, s_perimA, color);
				return;
			}

			BuildRing(center, Mathf.Max(0f, r - m_fadeSize), s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, m_fadeColor, color);
			EmitFilledFromPerimeter(center, s_perimB, color);
		}

		protected override void GenerateFrame()
		{
			var rect = Rect;
			Vector2 center = rect.center;
			float r = Mathf.Min(rect.width, rect.height) * 0.5f;

			BuildRing(center, r, s_perimA);

			if (Mathf.Approximately(0, m_fadeSize))
			{
				BuildRing(center, Mathf.Max(0f, r - m_frameSize), s_perimB);
				EmitFrameStripFromPerimeters(s_perimA, s_perimB, color, color);
				return;
			}

			// 3-ring sandwich: outer-fade band, solid frame band, inner-fade band.
			float rOuter = r;
			float r1 = Mathf.Max(0f, rOuter - m_fadeSize);
			float r2 = Mathf.Max(0f, rOuter - (m_frameSize - m_fadeSize));
			float r3 = Mathf.Max(0f, rOuter - m_frameSize);

			BuildRing(center, r1, s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, m_fadeColor, color);

			CopyPerimeter(s_perimB, s_perimA);
			BuildRing(center, r2, s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, color, color);

			CopyPerimeter(s_perimB, s_perimA);
			BuildRing(center, r3, s_perimB);
			EmitFrameStripFromPerimeters(s_perimA, s_perimB, color, m_fadeColor);
		}

		private void BuildRing( Vector2 _center, float _radius, List<Vector2> _out )
		{
			_out.Clear();
			int n = m_segments;
			float step = 2f * Mathf.PI / n;
			for (int i = 0; i < n; i++)
			{
				float a = i * step;
				_out.Add(new Vector2(
					_center.x + Mathf.Sin(a) * _radius,
					_center.y + Mathf.Cos(a) * _radius));
			}
		}
	}
}
