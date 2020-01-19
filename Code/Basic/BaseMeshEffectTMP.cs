using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit
{
	public abstract class BaseMeshEffectTMP : BaseMeshEffect
	{
		static private readonly List<Vector2> s_uv0 = new List<Vector2>();
		static private readonly List<Vector2> s_uv1 = new List<Vector2>();
		static private readonly List<Vector3> s_vertices = new List<Vector3>();
		static private readonly List<int> s_indices = new List<int>();
		static private readonly List<Vector3> s_normals = new List<Vector3>();
		static private readonly List<Vector4> s_tangents = new List<Vector4>();
		static private readonly List<Color32> s_colors = new List<Color32>();
		static private readonly VertexHelper s_vertexHelper = new VertexHelper();
		static private readonly List<TMP_SubMeshUI> s_subMeshUIs = new List<TMP_SubMeshUI>();
		static private readonly List<Mesh> s_meshes = new List<Mesh>();

		protected TMP_Text TextMeshPro { get { Init(); return m_textMeshPro; } }
		protected CanvasRenderer CanvasRenderer { get { Init (); return m_canvasRenderer; } }

		protected bool m_isTextMeshProActive;
		protected TMP_Text m_textMeshPro;
		protected bool m_initialized;
		protected CanvasRenderer m_canvasRenderer;
		protected RectTransform m_rectTransform;
		protected Graphic m_graphic;

		private void OnTMProTextChanged( Object _obj )
		{
			var textInfo = TextMeshPro.textInfo;
			if (TextMeshPro != _obj || textInfo.characterCount - textInfo.spaceCount <= 0)
			{
				return;
			}

			s_meshes.Clear();
			foreach (var info in textInfo.meshInfo)
			{
				s_meshes.Add(info.mesh);
			}

			foreach (var m in s_meshes)
			{
				FillVertexHelper(s_vertexHelper, m);
				ModifyMesh(s_vertexHelper);
				s_vertexHelper.FillMesh(m);
			}

			if (CanvasRenderer)
			{
				CanvasRenderer.SetMesh(TextMeshPro.mesh);
				GetComponentsInChildren(false, s_subMeshUIs);
				foreach (var sm in s_subMeshUIs)
				{
					sm.canvasRenderer.SetMesh(sm.mesh);
				}
				s_subMeshUIs.Clear();
			}

			s_meshes.Clear();
		}

		void FillVertexHelper( VertexHelper _vh, Mesh _mesh )
		{
			_vh.Clear();

			_mesh.GetVertices(s_vertices);
			_mesh.GetColors(s_colors);
			_mesh.GetUVs(0, s_uv0);
			_mesh.GetUVs(1, s_uv1);
			_mesh.GetNormals(s_normals);
			_mesh.GetTangents(s_tangents);
			_mesh.GetIndices(s_indices, 0);

			for (int i = 0; i < s_vertices.Count; i++)
			{
				s_vertexHelper.AddVert(s_vertices[i], s_colors[i], s_uv0[i], s_uv1[i], s_normals[i], s_tangents[i]);
			}

			for (int i = 0; i < s_indices.Count; i += 3)
			{
				_vh.AddTriangle(s_indices[i], s_indices[i + 1], s_indices[i + 2]);
			}
		}

		protected virtual void Init()
		{
			if (!m_initialized)
			{
				m_initialized = true;
				m_graphic = m_graphic ?? GetComponent<Graphic>();
				m_canvasRenderer = m_canvasRenderer ?? GetComponent<CanvasRenderer>();
				m_rectTransform = m_rectTransform ?? GetComponent<RectTransform>();
				m_textMeshPro = m_textMeshPro ?? GetComponent<TMP_Text>();
			}
		}

		/// <summary>
		/// This function is called when the object becomes enabled and active.
		/// </summary>
		protected override void OnEnable()
		{
			m_initialized = false;
			SetDirty ();
			if (TextMeshPro)
			{
				TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMProTextChanged);
			}

#if UNITY_EDITOR
			if (graphic && TextMeshPro)
			{
				GraphicRebuildTracker.TrackGraphic (graphic);
			}
#endif
		}

		/// <summary>
		/// This function is called when the behaviour becomes disabled () or inactive.
		/// </summary>
		protected override void OnDisable()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMProTextChanged);
			SetDirty ();

#if UNITY_EDITOR
			if (graphic && TextMeshPro)
			{
				GraphicRebuildTracker.UnTrackGraphic(graphic);
			}
#endif
		}
		/// <summary>
		/// Mark the vertices as dirty.
		/// </summary>
		public virtual void SetDirty()
		{
			if (TextMeshPro)
			{
				foreach (var info in TextMeshPro.textInfo.meshInfo)
				{
					var mesh = info.mesh;
					if (mesh)
					{
						mesh.Clear ();
						mesh.vertices = info.vertices;
						mesh.uv = info.uvs0;
						mesh.uv2 = info.uvs2;
						mesh.colors32 = info.colors32;
						mesh.normals = info.normals;
						mesh.tangents = info.tangents;
						mesh.triangles = info.triangles;
					}
				}

				if (CanvasRenderer)
				{
					CanvasRenderer.SetMesh (TextMeshPro.mesh);

					GetComponentsInChildren (false, s_subMeshUIs);
					foreach (var sm in s_subMeshUIs)
					{
						sm.canvasRenderer.SetMesh (sm.mesh);
					}
					s_subMeshUIs.Clear ();
				}
				TextMeshPro.havePropertiesChanged = true;
			}
			else if (graphic)
			{
				graphic.SetVerticesDirty ();
			}
		}
	}
}