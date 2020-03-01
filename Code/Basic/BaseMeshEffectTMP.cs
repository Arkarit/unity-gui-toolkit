using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace GuiToolkit
{
	/// <summary>
	/// BaseMeshEffectTMP is a BaseMeshEffect with support for TextMeshPro.
	/// It's completely transparent for the sub class inheriting this, if it inherits BaseMeshEffect or BaseMeshEffectTMP;
	/// both behave exactly the same.
	/// 
	/// TextMeshPro has a huge flaw: It does not support mesh modifiers at all, ModifyMesh() is simply not called :(
	/// BaseMeshEffectTMP provides a workaround for this by calling ModifyMesh() each time a m_textMeshPro text changes.
	/// The technique/idea for this is taken from https://unitylist.com/p/i24/Mesh-Effect-For-Text-Mesh-Pro (MIT license)
	/// The beforementioned project however is improved here by allowing modifiers to also change the mesh topology, which requires some
	/// additional tricks. The whole thing is quite a hack, but otherwise mesh modifiers weren't possible with text mesh pro at all.
	/// 
	/// How it works: The first active BaseMeshEffectTMP on a game object listens in a callback for TMP text changes.
	/// It then gets the mesh from TMP, calls ModifyMesh() for all modifiers for this mesh, and eventually sets the canvasrenderer to this mesh.
	/// If any of the modifiers changes the mesh topology (adds or removes vertices or triangles), the mesh has to be cloned to avoid an error in
	/// TextMeshProUGUI.GenerateTextMesh(), which tries to set the vertices without previously adjusting the triangles, which fails if the topology
	/// has changed.
	/// </summary>

	[RequireComponent(typeof(RectTransform))]
	public abstract class BaseMeshEffectTMP : BaseMeshEffect, ICanvasListener
	{
		static private readonly List<TMP_SubMeshUI> s_subMeshUIs = new List<TMP_SubMeshUI>();
		static private readonly List<Mesh> s_meshes = new List<Mesh>();

		protected RectTransform m_rectTransform;
		protected TMP_Text m_textMeshPro;
		protected CanvasRenderer m_canvasRenderer;
		protected Graphic m_graphic;
		protected Canvas m_canvas;

		private bool m_anyTopologyChangingMod;
		private bool m_TMPCallbackInstalled;
		private bool m_aboutToBeDisabled;

		/// <summary>
		/// Return true if your modifier adds or removes any elements to/from the mesh
		/// </summary>
		protected virtual bool ChangesTopology { get {return false;} }

		/// <summary>
		/// Callback when any TextMeshPro changes.
		/// Only installed/called when we actually reside on a TMP game object, and only for the first active BaseMeshEffectTMP on this game object,
		/// The first active BaseMeshEffectTMP does the mesh handling and subsequent ModifyMesh() for all modifiers.
		/// </summary>
		/// <param name="_obj">We have to check if this parameter means the actual TMPro on our game object</param>
		private void OnTMProTextChanged( UnityEngine.Object _obj )
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

			// Get meshes from TextMeshPro
			s_meshes.Clear();
			foreach (var info in textInfo.meshInfo)
			{
				if (info.mesh == null)
					return;

				// clone mesh if any modifier changes the topology (see "How it works")
				Mesh mesh = anyTopologyChangingMod ? Mesh.Instantiate(info.mesh) : info.mesh;
				s_meshes.Add(mesh);
			}

			// Fill the vertex helper, then call ModifyMesh() for all modifiers
			int numMeshes = s_meshes.Count;
			for(int i=0; i<numMeshes; i++)
			{
				Mesh m = s_meshes[i];
				FixMissingChannelsIfNecessary(ref m);
				VertexHelper vertexHelper = new VertexHelper(m);

				// We don't check 'enabled' for the mods here - BaseMeshEffect itself also doesn't.
				// Modifiers itself are responsible to return when inactive.
				foreach (var mod in mods)
					mod.ModifyMesh(vertexHelper);

				vertexHelper.FillMesh(m);
				vertexHelper.Dispose();
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

		private void InitCanvas()
		{
			if (m_canvas != null || m_canvas.isRootCanvas)
				return;

			m_canvas = GetComponentInParent<Canvas>();
			m_canvas = m_canvas.rootCanvas;
		}

		/// <summary>
		/// Set the cached components.
		/// </summary>
		protected override void Awake()
		{
			base.Awake();

			m_rectTransform = GetComponent<RectTransform>();
			m_graphic = GetComponent<Graphic>();
			m_canvasRenderer = GetComponent<CanvasRenderer>();
			m_textMeshPro = GetComponent<TMP_Text>();
			InitCanvas();
		}

		/// <summary>
		/// Every time the activeness of a modifier changes,
		/// the callback needs to re-evaluated, and all mods have to be set dirty
		/// </summary>
		protected override void OnEnable()
		{
			InitCanvas();
			m_aboutToBeDisabled = false;
			UpdateTMPCallback();
			SetDirty(true);

			if (m_textMeshPro != null)
				CanvasTracker.Instance.AddListener(m_canvas, this);

#if UNITY_EDITOR
			if (graphic && m_textMeshPro)
			{
				GraphicRebuildTracker.TrackGraphic (graphic);
			}
#endif
		}

		/// <summary>
		/// Every time the activeness of a modifier changes,
		/// the callback needs to re-evaluated, and all mods have to be set dirty
		/// </summary>
		protected override void OnDisable()
		{
			// Stupid Unity sometimes calls OnDisable() with enabled == true, sometimes false.
			// Simply setting enabled = false here leads to all BaseMeshEffectTMP's disabled when script reloading.
			// So mark that we are about to be disabled.
			m_aboutToBeDisabled = true;

			UpdateTMPCallback();
			SetDirty(true);

			if (m_textMeshPro != null)
			{
				CanvasTracker.Instance.RemoveListener(m_canvas, this);
			}


#if UNITY_EDITOR
			if (graphic && m_textMeshPro)
			{
				GraphicRebuildTracker.UnTrackGraphic(graphic);
			}
#endif
		}

		/// <summary>
		/// TMP does not use uv channels 2 and 3 (_mesh.uv3 and .uv4 - stupid 1-based names)
		/// VertexHelper however forces us to use it, so we need to add it if its missing
		/// </summary>
		/// <param name="_mesh"></param>
		private void FixMissingChannelsIfNecessary( ref Mesh _mesh )
		{
			int numVerts = _mesh.vertices.Length;
			if ( numVerts == 0)
				return;

			if (_mesh.uv3 == null || _mesh.uv3.Length != numVerts)
				_mesh.uv3 = new Vector2[numVerts];
			if (_mesh.uv4 == null || _mesh.uv4.Length != numVerts)
				_mesh.uv4 = new Vector2[numVerts];
		}

		/// <summary>
		/// Remove current callback, and then set callback to first active mod on game object
		/// </summary>
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
				if (mod.enabled && !mod.m_aboutToBeDisabled)
				{
					mod.InstallTMPCallback();
					return;
				}
			}
		}

		/// <summary>
		/// Install callback for this mod (doesn't check if mod is first active mod)
		/// </summary>
		private void InstallTMPCallback()
		{
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMProTextChanged);
			m_TMPCallbackInstalled = true;
		}

		/// <summary>
		/// Remove callback, if it was set to this mod
		/// </summary>
		private void RemoveTMPCallback()
		{
			if (!m_TMPCallbackInstalled)
				return;
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMProTextChanged);
			m_TMPCallbackInstalled = false;
		}

		/// <summary>
		/// Set dirty
		/// </summary>
		public virtual void SetDirty()
		{
			// TextMeshPro requires a special way.
			if (m_textMeshPro)
			{
				// Restore canvas renderer meshes
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
				// mark TextMeshPro dirty by telling that the properties have changed
				m_textMeshPro.havePropertiesChanged = true;
			}
			else if (graphic)
			{
				graphic.SetVerticesDirty ();
			}
		}

		/// <summary>
		/// Set dirty for all mods on this game object
		/// </summary>
		/// <param name="_allMods">true for all mods, false for only this mod</param>
		public void SetDirty( bool _allMods )
		{
			if (_allMods)
			{
				BaseMeshEffectTMP[] mods = GetComponents<BaseMeshEffectTMP>();
				foreach (var mod in mods)
					mod.SetDirty(false);
				return;
			}

			SetDirty();
		}

		public void OnCanvasChanged()
		{
			SetDirty();
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
			base.OnValidate();
			SetDirty();
		}

#endif

	}
}