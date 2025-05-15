using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Create rounded and antialiased images
	///
	/// In nearly every project, there's a need for rounded images and frames.
	/// This class handles this by creating an image with rounded corners of an arbitrary radius, and optional frame (hole) functionality and antialiasing.
	/// It works nearly like the original UnityEngine.UI.Image, where it's based on.
	/// You can add a sprite and set a color for the image.
	/// UV coordinates however are always 0/1 and there is no support for sliced, tiled, preserve aspect etc.
	///
	/// It also has some other improvements compared to UnityEngine.UI.Image; it can be disabled etc.
	/// 
	/// Unfortunately we can not make it an UiThing in C#, which would be a very simple task in a real programming language: just inherit from UiThing and Image.
	/// We also can't handle the improvements via composition. Thus this class is a bit outside of the common UiThing class hierarchy.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : Image, IEnableableInHierarchy
	{
		public const int MinCornerSegments = 2;
		public const int MaxCornerSegments = 30;

		public const float MinRadius = 0;
		public const float MaxRadius = 200;

		public const float MinFrameSize = 0;
		public const float MaxFrameSize = 200;

		public const float MinFadeSize = 0;
		public const float MaxFadeSize = 30;

		[Flags]
		protected enum MaterialFlags
		{
			Enabled		= 0x01,
			InvertMask	= 0x02,
			Maskable	= 0x04,
		}
		
		private enum Corner
		{
			TopLeft,
			TopRight,
			BottomRight,
			BottomLeft,
		}

		private enum Fade
		{
			None,
			Inner,
			Outer,
		}

		private enum QuadFade
		{
			None,
			Left,
			Right,
			Top,
			Bottom,
		}

		private class Vertex
		{
			public Vector2 Position;
			public Vector2 Uv;
			public Color Color;
		}

		[SerializeField] protected bool m_enabledInHierarchy;
		
		[SerializeField] protected Material m_disabledMaterial;
		
		[Tooltip("Corner segments. The more, the rounder. But keep an eye on performance; "
				 + "more corner segments mean more triangles and longer creation time. "
				 + "Between 5 and 10 should be sufficient for most tasks.")]
		[UnityEngine.Range(MinCornerSegments, MaxCornerSegments)]
		[SerializeField] protected int m_cornerSegments = 5;

		[Tooltip("Corner radius. To work properly, this should always be greater than frame size (when used with frame)")]
		[UnityEngine.Range(MinRadius, MaxRadius)]
		[SerializeField] protected float m_radius = 10;

		[Tooltip("Frame size. When set to 0, the image is completely filled. To work properly, this should always be less than corner radius (when used with frame)")]
		[UnityEngine.Range(MinFrameSize, MaxFrameSize)]
		[SerializeField] protected float m_frameSize = 0;

		[Tooltip("Fades out the edges of the image. Very useful for antialiasing, but can also be used for other purposes (e.g. soft shadow)")]
		[UnityEngine.Range(MinFadeSize, MaxFadeSize)]
		[SerializeField] protected float m_fadeSize = 0;

		[Tooltip("Invert stencil mask. Useful for cutouts.")]
		[SerializeField] protected bool m_invertMask;

		[Tooltip("Use padding. This is most useful for adjusting stretchable masks.")]
		[SerializeField] protected bool m_usePadding;

		[Tooltip("Padding")]
		[SerializeField] protected RectOffset m_padding;

		[Tooltip("Assign a fixed size instead of using rect transform boundaries. The fixed size is centered to the pivot. This is most useful for adjusting stretchable masks. X and y are offsets.")]
		[SerializeField] protected bool m_useFixedSize;

		[Tooltip("Fixed size")]
		[SerializeField] protected Rect m_fixedSize = new Rect(-10, -10, 20, 20);
		
		private static readonly List<Vertex> s_vertices = new();
		private static readonly List<int[]> s_triangles = new();
		
		protected Material m_clonedMaterial;
		protected MaterialFlags m_materialFlags = MaterialFlags.Enabled;
		
		public int CornerSegments
		{
			get => m_cornerSegments;
			set
			{
				CheckSetterRange(nameof(CornerSegments), value, MinCornerSegments, MaxCornerSegments);
				m_cornerSegments = value;
				SetVerticesDirty();
			}
		}

		public float Radius
		{
			get => m_radius;
			set
			{
				CheckSetterRange(nameof(Radius), value, MinRadius, MaxRadius);
				m_radius = value;
				SetVerticesDirty();
			}
		}

		public float FrameSize
		{
			get => m_frameSize;
			set
			{
				CheckSetterRange(nameof(FrameSize), value, MinFrameSize, MaxFrameSize);
				m_frameSize = value;
				SetVerticesDirty();
			}
		}

		public float FadeSize
		{
			get => m_fadeSize;
			set
			{
				CheckSetterRange(nameof(FadeSize), value, MinFadeSize, MaxFadeSize);
				m_fadeSize = value;
				SetVerticesDirty();
			}
		}

		public bool InvertMask
		{
			get => m_invertMask;
			set
			{
				if (m_invertMask == value)
					return;
				
				m_invertMask = value;
				SetMaterialDirty();
			}
		}

		public RectOffset Padding
		{
			get => m_padding;
			set => m_padding = value;
		}

		public Rect Rect
		{
			get
			{
				var result = rectTransform.rect;

				if (m_useFixedSize)
				{
					var pivot = rectTransform.pivot;
					result.x += result.width * pivot.x + m_fixedSize.x - m_fixedSize.width / 2;
					result.y += result.height * pivot.y + m_fixedSize.y - m_fixedSize.height / 2;
					result.width = m_fixedSize.width;
					result.height = m_fixedSize.height;
				}

				if (m_usePadding)
				{
					result.x += m_padding.left;
					result.width -= m_padding.horizontal;
					result.y += m_padding.bottom;
					result.height -= m_padding.vertical;
				}

				return result;
			}
		}

		#region IEnableableInHierarchy
		public bool IsEnableableInHierarchy => m_disabledMaterial;
		bool IEnableableInHierarchy.StoreEnabledInHierarchy 
		{ 
			get => m_enabledInHierarchy; 
			set => m_enabledInHierarchy = value; 
		}
		public bool EnabledInHierarchy
		{ 
			get => EnableableInHierarchyUtility.GetEnabledInHierarchy(this);
			set => EnableableInHierarchyUtility.SetEnabledInHierarchy(this, value); 
		}
		public IEnableableInHierarchy[] Children => GetComponentsInChildren<IEnableableInHierarchy>();
		public void OnEnabledInHierarchyChanged(bool _enabled) => SetMaterialDirty();
		#endregion

		protected override void OnDisable()
		{
			base.OnDisable();
			m_clonedMaterial.SafeDestroyDelayed();
			m_clonedMaterial = null;
		}

		private Material m_lastMaterial;
		
		public override Material materialForRendering
		{
			get
			{
				var currentMaterialFlags = CurrentMaterialFlags;
				var actualMaterial = (m_disabledMaterial && (currentMaterialFlags & MaterialFlags.Enabled) == 0) ?
					m_disabledMaterial :
					material;
				
				if (m_lastMaterial != actualMaterial)
				{
					m_clonedMaterial.SafeDestroyDelayed();
					m_clonedMaterial = null;
					m_lastMaterial = actualMaterial;
				}
				
				Material result;
				
				bool needsClone = (currentMaterialFlags & MaterialFlags.InvertMask) != 0;
				if (needsClone)
				{
					if (!m_clonedMaterial)
						m_clonedMaterial = Instantiate(actualMaterial);
					actualMaterial = m_clonedMaterial;
				}
				
				var savedMaterial = m_Material;
				m_Material = actualMaterial;
				result = base.materialForRendering;
				m_Material = savedMaterial;
				
				if (result == null)
					result = material;
				
				if (!maskable)
					result.SetInt("_StencilComp", (int)CompareFunction.Always);
				else if (m_invertMask)
					result.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
				else
					result.SetInt("_StencilComp", (int)CompareFunction.Equal);
				
				return result;
			}
		}


		protected override void OnPopulateMesh( Mesh _mesh )
		{
			if (m_frameSize > 0)
			{
				GenerateFrame();
				ApplyToMesh(_mesh);
				return;
			}

			GenerateFilled();
			ApplyToMesh(_mesh);
		}

		protected override void OnPopulateMesh( VertexHelper _vh )
		{
			if (m_frameSize > 0)
			{
				GenerateFrame();
				ApplyToVertexHelper(_vh);
				return;
			}

			GenerateFilled();
			ApplyToVertexHelper(_vh);
		}

		protected override void UpdateGeometry()
		{
			workerMesh.Clear(false);
			if (rectTransform == null || Rect.width < 0 || Rect.height < 0)
				return;

			var components = GetComponents<IMeshModifier>();
			bool hasComponents = components.Length > 0;

			if (hasComponents)
			{
				using (VertexHelper vertexHelper = new VertexHelper())
				{
					OnPopulateMesh(vertexHelper);
					foreach (var component in components)
						component.ModifyMesh(vertexHelper);
					vertexHelper.FillMesh(workerMesh);
				}

				canvasRenderer.SetMesh(workerMesh);
				return;
			}

			OnPopulateMesh(workerMesh);
			canvasRenderer.SetMesh(workerMesh);
		}

		private MaterialFlags CurrentMaterialFlags
		{
			get
			{
				return 
					(EnabledInHierarchy ? MaterialFlags.Enabled : 0) | 
					(InvertMask ? MaterialFlags.InvertMask : 0) | 
					(maskable ? MaterialFlags.Maskable : 0);
			}
		}
		
		private void ApplyToMesh( Mesh _mesh )
		{
			var vertexCount = s_vertices.Count;

			var vertices = new Vector3[vertexCount];
			var colors = new Color[vertexCount];
			var uv = new Vector2[vertexCount];

			for (int i = 0; i < vertexCount; i++)
			{
				var vertex = s_vertices[i];
				vertices[i] = vertex.Position;
				colors[i] = vertex.Color;
				uv[i] = vertex.Uv;
			}

			var triangleCount = s_triangles.Count;
			var triangles = new int[triangleCount * 3];
			int it = 0;
			for (int i = 0; i < triangleCount; i++)
			{
				var triangle = s_triangles[i];
				triangles[it++] = triangle[0];
				triangles[it++] = triangle[1];
				triangles[it++] = triangle[2];
			}

			_mesh.vertices = vertices;
			_mesh.colors = colors;
			_mesh.uv = uv;
			_mesh.triangles = triangles;

			s_vertices.Clear();
			s_triangles.Clear();
		}

		private static void ApplyToVertexHelper( VertexHelper _vh )
		{
			_vh.Clear();

			foreach (var vertex in s_vertices)
				_vh.AddVert(vertex.Position, vertex.Color, vertex.Uv);

			for (int i = 0; i < s_triangles.Count; i++)
			{
				var tri = s_triangles[i];
				_vh.AddTriangle(tri[0], tri[1], tri[2]);
			}

			s_vertices.Clear();
			s_triangles.Clear();
		}

		private void GenerateFrame()
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFrameRect();
				return;
			}

			GenerateFrameRounded();
		}

		private void GenerateFilled()
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFilledRect();
				return;
			}

			GenerateFilledRounded();
		}

		private void GenerateFrameRect() => GenerateFrameRect(Rect, m_frameSize);

		private void GenerateFrameRect( Rect _rect, float _frameSize )
		{
			if (!Mathf.Approximately(0, m_fadeSize))
			{
				GenerateFrameRectSimple(_rect, m_fadeSize);
				FadeFrameRect(_rect, Fade.Outer);
				_rect.x += m_fadeSize;
				_rect.y += m_fadeSize;
				_rect.width -= m_fadeSize * 2;
				_rect.height -= m_fadeSize * 2;
				GenerateFrameRectSimple(_rect, _frameSize - m_fadeSize * 2);
				_rect.x += _frameSize - m_fadeSize * 2;
				_rect.y += _frameSize - m_fadeSize * 2;
				_rect.width -= (_frameSize - m_fadeSize * 2) * 2;
				_rect.height -= (_frameSize - m_fadeSize * 2) * 2;
				GenerateFrameRectSimple(_rect, m_fadeSize);
				FadeFrameRect(_rect, Fade.Inner);

				return;
			}

			GenerateFrameRectSimple(_rect, _frameSize);
		}

		private void GenerateFrameRectSimple( Rect _rect, float _frameWidth )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			Rect bl = new Rect(x, y, _frameWidth, _frameWidth);
			Rect br = new Rect(w + x - _frameWidth, y, _frameWidth, _frameWidth);
			Rect tl = new Rect(x, h + y - _frameWidth, _frameWidth, _frameWidth);
			Rect tr = new Rect(w + x - _frameWidth, h + y - _frameWidth, _frameWidth, _frameWidth);

			AddQuad(bl);
			AddQuad(br, QuadFade.None, true);
			AddQuad(tl, QuadFade.None, true);
			AddQuad(tr);

			Rect l = new Rect(x, y + _frameWidth, _frameWidth, h - _frameWidth * 2);
			Rect r = new Rect(w + x - _frameWidth, y + _frameWidth, _frameWidth, h - _frameWidth * 2);
			Rect t = new Rect(x + _frameWidth, h + y - _frameWidth, w - _frameWidth * 2, _frameWidth);
			Rect b = new Rect(x + _frameWidth, y, w - _frameWidth * 2, _frameWidth);

			AddQuad(l);
			AddQuad(r);
			AddQuad(t);
			AddQuad(b);
		}

		private void GenerateFrameRounded()
		{
			if (Mathf.Approximately(0, m_fadeSize))
			{
				GenerateFrameRounded(Rect, m_radius, m_frameSize, Fade.None);
				return;
			}

			var rect = Rect;
			var radius = m_radius;
			GenerateFrameRounded(ref rect, ref radius, m_fadeSize, Fade.Outer);
			GenerateFrameRounded(ref rect, ref radius, m_frameSize - m_fadeSize * 2, Fade.None);
			GenerateFrameRounded(rect, radius, m_fadeSize, Fade.Inner);
		}

		private void GenerateFrameRounded( ref Rect _rect, ref float _radius, float _frameSize, Fade _fade )
		{
			GenerateFrameRounded(_rect, _radius, _frameSize, _fade);
			_rect.x += _frameSize;
			_rect.y += _frameSize;
			_rect.width -= _frameSize * 2;
			_rect.height -= _frameSize * 2;
			_radius -= _frameSize;
		}

		private void GenerateFrameRounded( Rect _rect, float _radius, float _frameSize, Fade _fade )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			Rect l = new Rect(x, y + _radius, _frameSize, h - _radius * 2);
			Rect r = new Rect(w + x - _frameSize, y + _radius, _frameSize, h - _radius * 2);
			Rect t = new Rect(x + _radius, h + y - _frameSize, w - _radius * 2, _frameSize);
			Rect b = new Rect(x + _radius, y, w - _radius * 2, _frameSize);

			switch (_fade)
			{
				case Fade.None:
					AddQuad(l);
					AddQuad(r);
					AddQuad(t);
					AddQuad(b);
					break;
				case Fade.Inner:
					AddQuad(l, QuadFade.Right);
					AddQuad(r, QuadFade.Left);
					AddQuad(t, QuadFade.Bottom);
					AddQuad(b, QuadFade.Top);
					break;
				case Fade.Outer:
					AddQuad(l, QuadFade.Left);
					AddQuad(r, QuadFade.Right);
					AddQuad(t, QuadFade.Top);
					AddQuad(b, QuadFade.Bottom);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_fade), _fade, null);
			}

			AddFrameSegment(_rect, Corner.TopLeft, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.TopRight, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.BottomLeft, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.BottomRight, _frameSize, _radius, _fade);
		}

		private void GenerateFilledRect()
		{
			var rect = Rect;
			if (Mathf.Approximately(0, m_fadeSize))
			{
				AddQuad(rect);
				return;
			}

			GenerateFrameRectSimple(rect, m_fadeSize);
			FadeFrameRect(rect, Fade.Outer);
			rect.x += m_fadeSize;
			rect.y += m_fadeSize;
			rect.width -= m_fadeSize * 2;
			rect.height -= m_fadeSize * 2;
			AddQuad(rect);
		}

		private void FadeFrameRect( Rect _rect, Fade _fade )
		{
			if (_fade == Fade.None)
				return;

			var fadeColor = color;
			fadeColor.a = 0;
			float top = _rect.yMin;
			float bottom = _rect.yMax;
			float left = _rect.xMin;
			float right = _rect.xMax;

			// frame is 8 quads, 16 tris, 32 verts
			for (int i = s_vertices.Count - 32; i < s_vertices.Count; i++)
			{
				var vertex = s_vertices[i];
				var position = vertex.Position;

				bool condition =
					Mathf.Approximately(left, position.x) ||
					Mathf.Approximately(right, position.x) ||
					Mathf.Approximately(top, position.y) ||
					Mathf.Approximately(bottom, position.y);

				if (_fade == Fade.Inner)
					condition = !condition;

				if (condition)
				{
					vertex.Color = fadeColor;
				}
			}
		}

		private void GenerateFilledRounded()
		{
			Rect rect = Rect;
			float radius = m_radius;
			if (!Mathf.Approximately(0, m_fadeSize))
				GenerateFrameRounded(ref rect, ref radius, m_fadeSize, Fade.Outer);

			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			var cex = rect.center.x;
			var cey = rect.center.y;

			AddTriangle
			(
				x, y + radius,
				cex, cey,
				x, y + h - radius
			);
			AddTriangle
			(
				x + radius, y + h,
				cex, cey,
				x + w - radius, y + h
			);
			AddTriangle
			(
				x + w, y + radius,
				cex, cey,
				x + w, y + h - radius
			);
			AddTriangle
			(
				x + radius, y,
				cex, cey,
				x + w - radius, y
			);

			AddSector(rect, Corner.TopLeft, radius);
			AddSector(rect, Corner.TopRight, radius);
			AddSector(rect, Corner.BottomLeft, radius);
			AddSector(rect, Corner.BottomRight, radius);
		}

		private void AddFrameSegment( Rect _rect, Corner _corner, float _frameSize, float _radius, Fade _fade )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			float angle = ((int)_corner + 3) * 90 * Mathf.Deg2Rad;
			float angleIncrement = 90f / m_cornerSegments * Mathf.Deg2Rad;

			float ox, oy;
			switch (_corner)
			{
				case Corner.TopLeft:
					ox = x + _radius;
					oy = y + h - _radius;
					break;
				case Corner.TopRight:
					ox = x + w - _radius;
					oy = y + h - _radius;
					break;
				case Corner.BottomRight:
					ox = x + w - _radius;
					oy = y + _radius;
					break;
				case Corner.BottomLeft:
					ox = x + _radius;
					oy = y + _radius;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null);
			}

			float radiusInner = _radius - _frameSize;
			for (int i = 0; i < m_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * _radius + ox;
				float y1 = Mathf.Cos(angle) * _radius + oy;
				float x3 = Mathf.Sin(angle) * radiusInner + ox;
				float y3 = Mathf.Cos(angle) * radiusInner + oy;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * _radius + ox;
				float y0 = Mathf.Cos(angle) * _radius + oy;
				float x2 = Mathf.Sin(angle) * radiusInner + ox;
				float y2 = Mathf.Cos(angle) * radiusInner + oy;
				AddIrregularQuad(x0, y0, x1, y1, x2, y2, x3, y3, _fade);
			}

		}

		private void AddSector( Rect _rect, Corner _corner, float _radius )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;
			var cex = _rect.center.x;
			var cey = _rect.center.y;

			float angle = ((int)_corner + 3) * 90 * Mathf.Deg2Rad;
			float angleIncrement = 90f / m_cornerSegments * Mathf.Deg2Rad;

			float ox, oy;
			switch (_corner)
			{
				case Corner.TopLeft:
					ox = x + _radius;
					oy = y + h - _radius;
					break;
				case Corner.TopRight:
					ox = x + w - _radius;
					oy = y + h - _radius;
					break;
				case Corner.BottomRight:
					ox = x + w - _radius;
					oy = y + _radius;
					break;
				case Corner.BottomLeft:
					ox = x + _radius;
					oy = y + _radius;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null);
			}

			for (int i = 0; i < m_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * _radius + ox;
				float y1 = Mathf.Cos(angle) * _radius + oy;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * _radius + ox;
				float y0 = Mathf.Cos(angle) * _radius + oy;
				AddTriangle(x0, y0, cex, cey, x1, y1);
			}
		}

		private void AddTriangle( float _ax, float _ay, float _bx, float _by, float _cx, float _cy )
		{
			int startIndex = s_vertices.Count;

			AddVert(_ax, _ay, color);
			AddVert(_bx, _by, color);
			AddVert(_cx, _cy, color);

			s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 2 });
		}

		private void AddQuad( Rect _rect, QuadFade _fade = QuadFade.None, bool _left = false ) => AddQuad(_rect.min, _rect.max, _fade, _left);

		private void AddQuad( Vector2 _posMin, Vector2 _posMax, QuadFade _fade = QuadFade.None, bool _left = false )
		{
			int startIndex = s_vertices.Count;
			var fadeColor = color;
			fadeColor.a = 0;

			switch (_fade)
			{
				case QuadFade.None:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Left:
					AddVert(_posMin.x, _posMin.y, fadeColor);
					AddVert(_posMin.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Right:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMin.y, fadeColor);
					break;
				case QuadFade.Top:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Bottom:
					AddVert(_posMin.x, _posMin.y, fadeColor);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, fadeColor);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_fade), _fade, null);
			}

			if (_left)
			{
				s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 3 });
				s_triangles.Add(new[] { startIndex + 2, startIndex + 3, startIndex + 1 });
				return;
			}

			s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 2 });
			s_triangles.Add(new[] { startIndex + 2, startIndex + 3, startIndex });
		}

		private void AddIrregularQuad( float _ax, float _ay, float _bx, float _by, float _cx, float _cy, float _dx, float _dy, Fade _fade )
		{
			int startIndex = s_vertices.Count;
			var fadeColor = color;
			fadeColor.a = 0;

			var effectiveColor = _fade == Fade.Outer ? fadeColor : color;

			AddVert(_ax, _ay, effectiveColor);
			AddVert(_bx, _by, effectiveColor);

			effectiveColor = _fade == Fade.Inner ? fadeColor : color;

			AddVert(_cx, _cy, effectiveColor);
			AddVert(_dx, _dy, effectiveColor);

			s_triangles.Add(new[] { startIndex + 3, startIndex + 1, startIndex });
			s_triangles.Add(new[] { startIndex + 2, startIndex + 3, startIndex });
		}

		private void AddVert( float _x, float _y, Color32 _color )
		{
			s_vertices.Add(new Vertex() { Position = new Vector2(_x, _y), Uv = GetUv(_x, _y), Color = _color });
		}

		private Vector2 GetUv( float _x, float _y )
		{
			Rect r = rectTransform.rect;
			Vector2 pivot = rectTransform.pivot;

			var normalizedX = _x / r.width + pivot.x;
			var normalizedY = _y / r.height + pivot.y;

			return new Vector2(normalizedX, normalizedY);
		}
		
		private void CheckSetterRange( string _name, float _value, float _min, float _max )
		{
			if (_value < _min || _value > _max)
				throw new ArgumentOutOfRangeException($"{_name} is out of range; should be in the range of {_min}..{_max}, but is {_value}");
		}
	}
}