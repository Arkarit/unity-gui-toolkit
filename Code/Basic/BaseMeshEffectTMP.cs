using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if !NOT_USE_TMPRO
using TMPro;
#endif

namespace GuiToolkit
{
	public abstract class BaseMeshEffectTMP : BaseMeshEffect
	{
#if !NOT_USE_TMPRO
		static readonly List<Vector2> s_Uv0 = new List<Vector2>();
		static readonly List<Vector2> s_Uv1 = new List<Vector2>();
		static readonly List<Vector3> s_Vertices = new List<Vector3>();
		static readonly List<int> s_Indices = new List<int>();
		static readonly List<Vector3> s_Normals = new List<Vector3>();
		static readonly List<Vector4> s_Tangents = new List<Vector4>();
		static readonly List<Color32> s_Colors = new List<Color32>();
		static readonly VertexHelper s_VertexHelper = new VertexHelper();
		static readonly List<TMP_SubMeshUI> s_SubMeshUIs = new List<TMP_SubMeshUI>();
		static readonly List<Mesh> s_Meshes = new List<Mesh>();

		public TMP_Text textMeshPro { get { Initialize(); return _textMeshPro; } }
		public CanvasRenderer canvasRenderer { get { Initialize (); return _canvasRenderer; } }

		bool _isTextMeshProActive;
		TMP_Text _textMeshPro;
		bool _initialized;
		CanvasRenderer _canvasRenderer;
		RectTransform _rectTransform;
		Graphic _graphic;

		/// <summary>
		/// Called when any TextMeshPro generated the mesh.
		/// </summary>
		/// <param name="obj">TextMeshPro object.</param>
		void OnTextChanged( Object obj )
		{
			// Skip if the object is different from the current object or the text is empty.
			var textInfo = textMeshPro.textInfo;
			if (textMeshPro != obj || textInfo.characterCount - textInfo.spaceCount <= 0)
			{
				return;
			}

			// Collect the meshes.
			s_Meshes.Clear();
			foreach (var info in textInfo.meshInfo)
			{
				s_Meshes.Add(info.mesh);
			}

			// Convert meshes to VertexHelpers and modify them.
			foreach (var m in s_Meshes)
			{
				FillVertexHelper(s_VertexHelper, m);
				ModifyMesh(s_VertexHelper);
				s_VertexHelper.FillMesh(m);
			}

			// Set the modified meshes to the CanvasRenderers (for UI only).
			if (canvasRenderer)
			{
				canvasRenderer.SetMesh(textMeshPro.mesh);
				GetComponentsInChildren(false, s_SubMeshUIs);
				foreach (var sm in s_SubMeshUIs)
				{
					sm.canvasRenderer.SetMesh(sm.mesh);
				}
				s_SubMeshUIs.Clear();
			}

			// Clear.
			s_Meshes.Clear();
		}

		void FillVertexHelper( VertexHelper vh, Mesh mesh )
		{
			vh.Clear();

			mesh.GetVertices(s_Vertices);
			mesh.GetColors(s_Colors);
			mesh.GetUVs(0, s_Uv0);
			mesh.GetUVs(1, s_Uv1);
			mesh.GetNormals(s_Normals);
			mesh.GetTangents(s_Tangents);
			mesh.GetIndices(s_Indices, 0);

			for (int i = 0; i < s_Vertices.Count; i++)
			{
				s_VertexHelper.AddVert(s_Vertices[i], s_Colors[i], s_Uv0[i], s_Uv1[i], s_Normals[i], s_Tangents[i]);
			}

			for (int i = 0; i < s_Indices.Count; i += 3)
			{
				vh.AddTriangle(s_Indices[i], s_Indices[i + 1], s_Indices[i + 2]);
			}
		}

		protected virtual void Initialize()
		{
			if (!_initialized)
			{
				_initialized = true;
				_graphic = _graphic ?? GetComponent<Graphic>();
				_canvasRenderer = _canvasRenderer ?? GetComponent<CanvasRenderer>();
				_rectTransform = _rectTransform ?? GetComponent<RectTransform>();
				_textMeshPro = _textMeshPro ?? GetComponent<TMP_Text>();
			}
		}

		/// <summary>
		/// This function is called when the object becomes enabled and active.
		/// </summary>
		protected override void OnEnable ()
		{
			_initialized = false;
			SetDirty ();
			if (textMeshPro)
			{
				TMPro_EventManager.TEXT_CHANGED_EVENT.Add (OnTextChanged);
			}

#if UNITY_EDITOR
			if (graphic && textMeshPro)
			{
				GraphicRebuildTracker.TrackGraphic (graphic);
			}
#endif
		}

		/// <summary>
		/// This function is called when the behaviour becomes disabled () or inactive.
		/// </summary>
		protected override void OnDisable ()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove (OnTextChanged);
			SetDirty ();

#if UNITY_EDITOR
			if (graphic && textMeshPro)
			{
				GraphicRebuildTracker.UnTrackGraphic (graphic);
			}
#endif
		}
		/// <summary>
		/// Mark the vertices as dirty.
		/// </summary>
		public virtual void SetDirty ()
		{
			if (textMeshPro)
			{
				foreach (var info in textMeshPro.textInfo.meshInfo)
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

				if (canvasRenderer)
				{
					canvasRenderer.SetMesh (textMeshPro.mesh);

					GetComponentsInChildren (false, s_SubMeshUIs);
					foreach (var sm in s_SubMeshUIs)
					{
						sm.canvasRenderer.SetMesh (sm.mesh);
					}
					s_SubMeshUIs.Clear ();
				}
				textMeshPro.havePropertiesChanged = true;
			}
			else
			if (graphic)
			{
				graphic.SetVerticesDirty ();
			}
		}

#endif

	}
}