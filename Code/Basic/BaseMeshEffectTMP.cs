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
		protected CanvasRenderer CanvasRenderer { get { Init(); return m_canvasRenderer; } }

		protected bool m_isTextMeshProActive;
		protected TMP_Text m_textMeshPro;
		protected bool m_initialized;
		protected CanvasRenderer m_canvasRenderer;
		protected RectTransform m_rectTransform;
		protected Graphic m_graphic;

		private bool m_amIFirstModifier;
		private readonly List<BaseMeshEffectTMP> m_mods = new List<BaseMeshEffectTMP>();
		private bool m_anyTopologyChangingMod;
		private bool m_aboutToBeDisabled;

		protected virtual bool ChangesTopology { get {return false;} }

		private void OnTMProTextChanged( Object _obj )
		{
			if (this == null || !enabled)
				return;

			Init();
			if (!m_amIFirstModifier)
				return;

			var textInfo = TextMeshPro.textInfo;
			if (TextMeshPro != _obj || textInfo.characterCount - textInfo.spaceCount <= 0)
			{
				return;
			}

			s_meshes.Clear();
			foreach (var info in textInfo.meshInfo)
			{
				if (info.mesh == null)
					return;

				Mesh mesh = m_anyTopologyChangingMod ? Mesh.Instantiate(info.mesh) : info.mesh;
				s_meshes.Add(mesh);
			}

			foreach (var m in s_meshes)
			{
				foreach (var mod in m_mods)
				{
					FillVertexHelper(s_vertexHelper, m);
					mod.ModifyMesh(s_vertexHelper);
					s_vertexHelper.FillMesh(m);
				}
			}

			if (CanvasRenderer)
			{
				CanvasRenderer.SetMesh(s_meshes[0]);
				GetComponentsInChildren(false, s_subMeshUIs);
				int numModifiedMeshes = s_meshes.Count;
				int numSubMeshes = s_subMeshUIs.Count;
				Debug.Assert(numModifiedMeshes == numSubMeshes+1);
				if (numModifiedMeshes == numSubMeshes+1)
				{
					for (int i=0; i<numSubMeshes; i++)
					{
						TMP_SubMeshUI subMeshUI = s_subMeshUIs[i];
						subMeshUI.canvasRenderer.SetMesh(s_meshes[i+1]);
					}
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
				if (!enabled)
					return;

				m_initialized = true;
				m_graphic = m_graphic ?? GetComponent<Graphic>();
				m_canvasRenderer = m_canvasRenderer ?? GetComponent<CanvasRenderer>();
				m_rectTransform = m_rectTransform ?? GetComponent<RectTransform>();
				m_textMeshPro = m_textMeshPro ?? GetComponent<TMP_Text>();

				m_mods.Clear();
				var mods = GetComponents<BaseMeshEffectTMP>();
				foreach (var mod in mods)
				{
					if (mod.enabled && !mod.m_aboutToBeDisabled)
						m_mods.Add(mod);
				}

				if (m_mods.Count == 0)
				{
					m_initialized = false;
					return;
				}

				m_amIFirstModifier = m_mods[0] == this;
				m_anyTopologyChangingMod = false;
				foreach (var mod in m_mods)
				{
					if (mod.ChangesTopology)
					{
						m_anyTopologyChangingMod = true;
						break;
					}
				}
				if (TextMeshPro && m_amIFirstModifier)
				{
					TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMProTextChanged);
				}
			}
		}

		protected override void OnEnable()
		{
			m_aboutToBeDisabled = false;
			MakeAllModsDirty();

			Init();

#if UNITY_EDITOR
			if (graphic && TextMeshPro)
			{
				GraphicRebuildTracker.TrackGraphic (graphic);
			}
#endif
		}

		protected override void OnDisable()
		{
			m_aboutToBeDisabled = true;
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMProTextChanged);
			MakeAllModsDirty();

#if UNITY_EDITOR
			if (graphic && TextMeshPro)
			{
				GraphicRebuildTracker.UnTrackGraphic(graphic);
			}
#endif
		}

		private void MakeAllModsDirty()
		{
			BaseMeshEffectTMP[] mods = GetComponents<BaseMeshEffectTMP>();
			foreach (var mod in mods)
			{
				mod.m_initialized = false;
				mod.SetDirty();
			}
		}


		private void UpdateTMPMeshes()
		{
			foreach (var info in TextMeshPro.textInfo.meshInfo)
			{
				var mesh = info.mesh;
				if (mesh)
				{
					mesh.Clear();
					mesh.vertices = info.vertices;
					mesh.uv = info.uvs0;
					mesh.uv2 = info.uvs2;
					mesh.colors32 = info.colors32;
					mesh.normals = info.normals;
					mesh.tangents = info.tangents;
					mesh.triangles = info.triangles;
				}
			}
		}

		/// <summary>
		/// Mark the vertices as dirty.
		/// </summary>
		public virtual void SetDirty()
		{
			if (TextMeshPro)
			{
				UpdateTMPMeshes();

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

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			SetDirty();
		}
#endif

	}
}