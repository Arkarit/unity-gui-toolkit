using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Abstract base for shape-based UI images (rounded rectangle, star, ...).
	///
	/// Holds all shape-agnostic infrastructure:
	/// - Serialized fields for frame size, fade size, padding, fixed size, disabled material,
	///   stencil-mask inversion, simple-gradient reference and the IEnableableInHierarchy flag.
	/// - Material handling (disabledMaterial swap, invert-mask material clone, stencil compare).
	/// - Sprite -> UV mapping based on world position inside the rect.
	/// - OnPopulateMesh dispatch (Mesh and VertexHelper overloads) to abstract Generate* methods.
	/// - Static vertex/triangle buffers and ApplyTo* writers.
	///
	/// Concrete subclasses (UiRoundedImage, UiStar, ...) implement GenerateFilled() and
	/// GenerateFrame() to populate s_vertices / s_triangles with their shape-specific
	/// geometry. They may use AddVert / AddTriangle as building blocks and mutate
	/// s_vertices entries directly (e.g. to set per-vertex fade colors).
	///
	/// Field names (m_*) are intentionally preserved from the original UiRoundedImage
	/// so existing serialized scenes / prefabs continue to load against the moved fields.
	/// </summary>
	[RequireComponent(typeof(CanvasRenderer))]
	public abstract class UiShapeImage : Image, IEnableableInHierarchy
	{
		public const float MinFrameSize = 0;
		public const float MaxFrameSize = 200;

		public const float MinFadeSize = 0;
		public const float MaxFadeSize = 200;

		[Flags]
		protected enum MaterialFlags
		{
			Enabled = 0x01,
			InvertMask = 0x02,
			Maskable = 0x04,
		}

		protected enum Fade
		{
			None,
			Inner,
			Outer,
		}

		protected class Vertex
		{
			public Vector2 Position;
			public Vector2 Uv;
			public Color Color;
		}

		[SerializeField] protected bool m_enabledInHierarchy;

		[SerializeField] protected Material m_disabledMaterial;

		[Tooltip("Frame size. When set to 0, the image is completely filled. "
		         + "To work properly, this should always be less than the shape's primary radius/size when used with frame.")]
		[UnityEngine.Range(MinFrameSize, MaxFrameSize)]
		[SerializeField] protected float m_frameSize = 0;

		[Tooltip("Fades out the edges of the image. Very useful for antialiasing, but can also be used for other purposes (e.g. soft shadow)")]
		[UnityEngine.Range(MinFadeSize, MaxFadeSize)]
		[SerializeField] protected float m_fadeSize = 0;

		[Tooltip("The Transparent color")]
		[SerializeField] protected Color m_fadeColor = Color.clear;

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

		[Tooltip("Ui Simple Gradient component. Mandatory if you want to use the 'SimpleGradientColors' getters+setters.")]
		[SerializeField] protected UiGradientSimple m_gradientSimple;

		[Tooltip("If true, a single size offset value is applied to both width and height. "
		         + "If false, X and Y are independent.")]
		[SerializeField] protected bool m_uniformSizeOffset = true;

		[Tooltip("Offset added to the rect's width / height, applied symmetrically around the rect center. "
		         + "Positive values grow the shape, negative values shrink it. "
		         + "When m_uniformSizeOffset is true, only the X component is used for both axes.")]
		[SerializeField] protected Vector2 m_sizeOffset;

		protected static readonly List<Vertex> s_vertices = new();
		protected static readonly List<int[]> s_triangles = new();

		// Scratch buffers for perimeter-based shape subclasses (UiStar, UiCircle, ...).
		// Two are provided so a subclass can ping-pong between "current outer" and "next inner"
		// when building the fade-ring sandwich.
		protected static readonly List<Vector2> s_perimA = new();
		protected static readonly List<Vector2> s_perimB = new();

		protected Material m_clonedMaterial;
		private Material m_lastMaterial;

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

		public Color FadeColor
		{
			get => m_fadeColor;
			set
			{
				if (m_fadeColor == value)
					return;

				m_fadeColor = value;
				SetVerticesDirty();
			}
		}

		public RectOffset Padding
		{
			get => m_padding;
			set => m_padding = value;
		}

		public bool UniformSizeOffset
		{
			get => m_uniformSizeOffset;
			set
			{
				if (m_uniformSizeOffset == value)
					return;

				m_uniformSizeOffset = value;
				SetVerticesDirty();
			}
		}

		/// <summary>
		/// Raw size offset as stored. When UniformSizeOffset is true, only the X component is used
		/// (it is applied to both axes); the Y component is preserved but ignored.
		/// </summary>
		public Vector2 SizeOffset
		{
			get => m_sizeOffset;
			set
			{
				if (m_sizeOffset == value)
					return;

				m_sizeOffset = value;
				SetVerticesDirty();
			}
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

				float offX = m_sizeOffset.x;
				float offY = m_uniformSizeOffset ? m_sizeOffset.x : m_sizeOffset.y;
				result.x -= offX * 0.5f;
				result.y -= offY * 0.5f;
				result.width += offX;
				result.height += offY;

				return result;
			}
		}

		public void SetSimpleGradientColors( Color _leftOrTop, Color _rightOrBottom )
		{
			if (m_gradientSimple == null)
			{
				UiLog.LogError("Attempt to set simple gradient colors, but simple gradient was not set");
				return;
			}
			m_gradientSimple.SetColors(_leftOrTop, _rightOrBottom);
		}

		public (Color leftOrTop, Color rightOrBottom) GetSimpleGradientColors()
		{
			if (m_gradientSimple == null)
				return (leftOrTop: Color.white, rightOrBottom: Color.white);
			return m_gradientSimple.GetColors();
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
		public void OnEnabledInHierarchyChanged( bool _enabled ) => SetMaterialDirty();
		#endregion

		protected override void OnDisable()
		{
			base.OnDisable();
			if (GeneralUtility.IsQuitting)
				return;

			m_clonedMaterial.SafeDestroyDelayed();
			m_clonedMaterial = null;
		}

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
				GenerateFrame();
			else
				GenerateFilled();
			ApplyToMesh(_mesh);
		}

		protected override void OnPopulateMesh( VertexHelper _vh )
		{
			if (m_frameSize > 0)
				GenerateFrame();
			else
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

		/// <summary>
		/// Generate the filled (no-frame) mesh of the shape. Must populate s_vertices and s_triangles.
		/// </summary>
		protected abstract void GenerateFilled();

		/// <summary>
		/// Generate the frame (outline-only) mesh of the shape. Called only when m_frameSize > 0.
		/// Must populate s_vertices and s_triangles.
		/// </summary>
		protected abstract void GenerateFrame();

		protected void AddTriangle( float _ax, float _ay, float _bx, float _by, float _cx, float _cy )
		{
			int startIndex = s_vertices.Count;

			AddVert(_ax, _ay, color);
			AddVert(_bx, _by, color);
			AddVert(_cx, _cy, color);

			s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 2 });
		}

		protected void AddVert( float _x, float _y, Color32 _color )
		{
			s_vertices.Add(new Vertex() { Position = new Vector2(_x, _y), Uv = GetUv(_x, _y), Color = _color });
		}

		protected Vector2 GetUv( float _x, float _y )
		{
			if (sprite == null)
				return Vector2.zero;

			Rect r = Rect;

			Rect uvRect = sprite.textureRect;
			Rect fullTex = sprite.textureRect;

			if (sprite.packed && sprite.associatedAlphaSplitTexture == null)
				fullTex = new Rect(0, 0, sprite.texture.width, sprite.texture.height);

			float u = Mathf.Lerp(
				uvRect.x / fullTex.width,
				(uvRect.x + uvRect.width) / fullTex.width,
				(_x - r.xMin) / r.width
			);

			float v = Mathf.Lerp(
				uvRect.y / fullTex.height,
				(uvRect.y + uvRect.height) / fullTex.height,
				(_y - r.yMin) / r.height
			);

			return new Vector2(u, v);
		}

		protected static void CheckSetterRange( string _name, float _value, float _min, float _max )
		{
			if (_value < _min || _value > _max)
				throw new ArgumentOutOfRangeException($"{_name} is out of range; should be in the range of {_min}..{_max}, but is {_value}");
		}

		/// <summary>
		/// Fan-triangulate a perimeter ring from a center vertex. Triangle winding matches
		/// UiRoundedImage.AddSector (later, center, earlier).
		/// </summary>
		protected void EmitFilledFromPerimeter( Vector2 _center, List<Vector2> _perim, Color _color )
		{
			int n = _perim.Count;
			if (n < 3)
				return;

			int centerIdx = s_vertices.Count;
			AddVert(_center.x, _center.y, _color);

			int firstPerim = s_vertices.Count;
			for (int i = 0; i < n; i++)
				AddVert(_perim[i].x, _perim[i].y, _color);

			for (int i = 0; i < n; i++)
			{
				int a = firstPerim + i;
				int b = firstPerim + (i + 1) % n;
				s_triangles.Add(new[] { b, centerIdx, a });
			}
		}

		/// <summary>
		/// Emit a quad strip between two paired perimeter rings (outer + inner, same vertex count).
		/// Triangle winding matches UiRoundedImage.AddIrregularQuad.
		/// </summary>
		protected void EmitFrameStripFromPerimeters( List<Vector2> _outer, List<Vector2> _inner, Color _outerColor, Color _innerColor )
		{
			int n = _outer.Count;
			if (n < 3 || _inner.Count != n)
				return;

			int outerStart = s_vertices.Count;
			for (int i = 0; i < n; i++)
				AddVert(_outer[i].x, _outer[i].y, _outerColor);

			int innerStart = s_vertices.Count;
			for (int i = 0; i < n; i++)
				AddVert(_inner[i].x, _inner[i].y, _innerColor);

			for (int i = 0; i < n; i++)
			{
				int oE = outerStart + i;
				int oL = outerStart + (i + 1) % n;
				int iE = innerStart + i;
				int iL = innerStart + (i + 1) % n;

				s_triangles.Add(new[] { iE, oE, oL });
				s_triangles.Add(new[] { iL, iE, oL });
			}
		}

		protected static void CopyPerimeter( List<Vector2> _src, List<Vector2> _dst )
		{
			_dst.Clear();
			for (int i = 0; i < _src.Count; i++)
				_dst.Add(_src[i]);
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
	}
}
