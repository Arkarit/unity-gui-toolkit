using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit
{
	/// <summary>
	/// BaseMeshEffectTMP is a BaseMeshEffect with support for m_textMeshPro.
	/// 
	/// m_textMeshPro has a huge flaw: It does not support mesh modifiers at all, ModifyMesh() is simply not called :(
	/// BaseMeshEffectTMP provides a workaround for this by calling ModifyMesh() each time a m_textMeshPro text changes.
	/// The technique/idea for this is taken from https://unitylist.com/p/i24/Mesh-Effect-For-Text-Mesh-Pro (MIT license)
	/// The beforementioned project however is improved by allowing modifiers to also change the mesh topology, which requires some
	/// additional tricks. The whole thing is quite a hack, but otherwise mesh modifiers weren't possible with text mesh pro at all.
	/// 
	/// How it works: The first active BaseMeshEffectTMP on a game object listens in a callback for TMP text changes.
	/// It then gets the mesh from TMP, calls ModifyMesh() for all modifiers for this mesh, and eventually sets the canvasrenderer to this mesh.
	/// If any of the modifiers changes the mesh topology (adds or removes vertices or triangles), the mesh has to be cloned to avoid an error in
	/// TextMeshProUGUI.GenerateTextMesh(), which tries to set the vertices without previously adjusting the triangles, which fails if the topology
	/// has changed.
	/// </summary>
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

		private TMP_Text m_textMeshPro;
		private CanvasRenderer m_canvasRenderer;
		private RectTransform m_rectTransform;
		private Graphic m_graphic;

		private bool m_anyTopologyChangingMod;

		protected virtual bool ChangesTopology { get {return false;} }

		/// <summary>
		/// Callback when any TextMeshPro changes.
		/// Only installed/called when we actually reside on a TMP game object, and only for the first active BaseMeshEffectTMP on this game object,
		/// The first active BaseMeshEffectTMP does the mesh handling and subsequent ModifyMesh() for all modifiers.
		/// </summary>
		/// <param name="_obj">We have to check if this parameter means the actual TMPro on our game object</param>
		private void OnTMProTextChanged( Object _obj )
		{
			// return if the call does not apply to our TextMeshPro instance (or null or inactive)
			if ( m_textMeshPro != _obj || this == null || !IsActive() )
				return;

			// If no text at all, return
			var textInfo = m_textMeshPro.textInfo;
			if (textInfo.characterCount - textInfo.spaceCount <= 0)
			{
				return;
			}

			// Determine, if any modifier changes the mesh topology
			BaseMeshEffectTMP[] mods = GetComponents<BaseMeshEffectTMP>();
			bool anyTopologyChangingMod = false;
			foreach(var mod in mods)
			{
				if (mod.IsActive() && mod.ChangesTopology)
				{
					anyTopologyChangingMod = true;
					break;
				}
			}

			// Get meshes from GenerateTextMesh()
			s_meshes.Clear();
			foreach (var info in textInfo.meshInfo)
			{
				if (info.mesh == null)
					return;

				// clone mesh if any modifier changes the topology
				Mesh mesh = anyTopologyChangingMod ? Mesh.Instantiate(info.mesh) : info.mesh;
				s_meshes.Add(mesh);
			}

			// Fill the vertex helper, then call ModifyMesh() for all modifiers
			foreach (var m in s_meshes)
			{
				FillVertexHelper(s_vertexHelper, m);

				// We don't check 'enabled' for the mods here - BaseMeshEffect itself also doesn't.
				// Modifiers itself are responsible to return when inactive
				foreach (var mod in mods)
					mod.ModifyMesh(s_vertexHelper);

				s_vertexHelper.FillMesh(m);
			}

			// Assign the mesh to the canvas renderer
			if (m_canvasRenderer)
			{
				m_canvasRenderer.SetMesh(s_meshes[0]);
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

			// Inefficiency, your name is Unity.
			// This whole VertexHelper rubbish is complete crap.
			// Giant amount of completely unnecessary copying.
			// And even the crap is crappy: Why is there a VertexHelper.FillMesh(), but not a method, which fills the vertex helper BY a mesh (except the ctor)?
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

		protected override void Start()
		{
			base.Start();

			m_graphic = GetComponent<Graphic>();
			m_canvasRenderer = GetComponent<CanvasRenderer>();
			m_rectTransform = GetComponent<RectTransform>();
			m_textMeshPro = GetComponent<TMP_Text>();
		}

		protected override void OnEnable()
		{
			UpdateTMPCallback();
			MakeAllModsDirty();

#if UNITY_EDITOR
			if (graphic && m_textMeshPro)
			{
				GraphicRebuildTracker.TrackGraphic (graphic);
			}
#endif
		}

		protected override void OnDisable()
		{
			UpdateTMPCallback();
			MakeAllModsDirty();


#if UNITY_EDITOR
			if (graphic && m_textMeshPro)
			{
				GraphicRebuildTracker.UnTrackGraphic(graphic);
			}
#endif
		}

		private void UpdateTMPCallback()
		{
			if (!m_textMeshPro)
				return;

			BaseMeshEffectTMP[] mods = GetComponents<BaseMeshEffectTMP>();

			foreach (BaseMeshEffectTMP mod in mods)
				mod.RemoveTMPCallback();

			for (int i=0; i<mods.Length; i++)
			{
				BaseMeshEffectTMP mod = mods[i];
				if (mod.enabled)
				{
					mod.InstallTMPCallback();
					return;
				}
			}
		}

		private bool m_TMPCallbackInstalled;
		private void InstallTMPCallback()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMProTextChanged);
			m_TMPCallbackInstalled = true;
		}

		private void RemoveTMPCallback()
		{
			if (!m_TMPCallbackInstalled)
				return;
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMProTextChanged);
			m_TMPCallbackInstalled = false;
		}

		private void UpdateTMPMeshes()
		{
			foreach (var info in m_textMeshPro.textInfo.meshInfo)
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
			if (m_textMeshPro)
			{
				UpdateTMPMeshes();

				if (m_canvasRenderer)
				{
					m_canvasRenderer.SetMesh (m_textMeshPro.mesh);

					GetComponentsInChildren (false, s_subMeshUIs);
					foreach (var sm in s_subMeshUIs)
					{
						sm.canvasRenderer.SetMesh (sm.mesh);
					}
					s_subMeshUIs.Clear ();
				}
				m_textMeshPro.havePropertiesChanged = true;
			}
			else if (graphic)
			{
				graphic.SetVerticesDirty ();
			}
		}

		private void MakeAllModsDirty()
		{
			BaseMeshEffectTMP[] mods = GetComponents<BaseMeshEffectTMP>();
			foreach (var mod in mods)
				mod.SetDirty();
		}

// 		private void Debugprint(string _s)
// 		{
// 			BaseMeshEffectTMP[] mods = GetComponents<BaseMeshEffectTMP>();
//			for (int i=0; i<mods.Length; i++)
// 			{
// 				if (mods[i] == this)
// 				{
// 					Debug.Log($"{this} Mod {i} {_s} enabled: {enabled} m_initialized: {m_initialized}");
// 				}
// 			}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			SetDirty();
		}
#endif

	}
}