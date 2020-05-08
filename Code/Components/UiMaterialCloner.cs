using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// /brief Clone and cache materials
	/// 
	/// Very often, it is necessary to clone a material.
	/// The naive approach to just clone and store a material leads to
	/// broken batching. <BR>
	/// Also, it gets problematic if two components need to clone a material:
	/// The first component clones the material from a mesh renderer and sets the cloned material.
	/// The second component also clones the material from a mesh renderer (which already has the cloned material applied) and sets it.
	/// If now the first component does something with the material, this won't become visible, because the mesh renderer already has the cloned material from the second component.
	/// 
	/// These problems can be avoided by applying an UiMaterialCloner.
	/// It uses UiMaterialCache to share the same material between multiple clones and thus protects batching.
	/// Also, if both example components rely on the same UiMaterialCloner instead of cloning it themselves, they will always use the same cloned material.

	[ExecuteAlways]
	public sealed class UiMaterialCloner : MonoBehaviour
	{
		[SerializeField]
		private bool m_useMaterialCache;

		[SerializeField]
		private string m_materialCacheKey;
		private Material m_material;
		private Graphic m_graphics;
		private Renderer m_renderer;

#if UNITY_EDITOR
		//Workaround: sharedMaterial is null after an undo
		private Material m_originalMaterial;
#endif

		public Material Material
		{
			get
			{
				InitIfNecessary();
				return m_material;
			}
		}

		public bool Valid => m_material != null;

		private void OnEnable()
		{
			if (m_material == null)
				Init();
		}

		private void OnDisable()
		{
			if (!m_material)
				return;

			SetMaterial( UiMaterialCache.Instance.ReleaseClonedMaterial(m_material) );

#if UNITY_EDITOR
			//Workaround: m_meshRenderer.sharedMaterial is null after an undo
			m_originalMaterial = GetMaterial();
#endif
			m_material = null;
		}

		private void SetMaterial(Material _material)
		{
			if (m_renderer)
				m_renderer.sharedMaterial = _material;
			if (m_graphics)
				m_graphics.material = _material;
		}

		private Material GetMaterial()
		{
			if (m_renderer)
				return m_renderer.sharedMaterial;
			if (m_graphics)
				return m_graphics.material;
			return null;
		}

		private void Init()
		{
			if (m_material)
			{
				UiMaterialCache.Instance.ReleaseClonedMaterial(m_material);
				m_material = null;
			}

			m_graphics = GetComponent<Graphic>();
			m_renderer = GetComponent<Renderer>();

#if UNITY_EDITOR
			if (GetMaterial() == null)
			{
				SetMaterial(m_originalMaterial);
			}
#endif

			Material originalMaterial = GetMaterial();

			if (originalMaterial == null)
				return;

			m_material = UiMaterialCache.Instance.AcquireClonedMaterial(originalMaterial);
			SetMaterial(m_material);
		}
/*
		private Material AcquireClonedMaterial(Material _material)
		{
			if (m_useMaterialCache)
				return UiMaterialCache.Instance.AcquireClonedMaterial(_material);

			Material clonedMaterial = Instantiate(_material);

		}

		private Material ReleaseClonedMaterial(Material _material)
		{
		}
*/

		private void InitIfNecessary()
		{
			if (m_material == null)
			{
				Init();
				return;
			}

			if (m_renderer)
			{
				if (m_renderer.sharedMaterial == m_material)
					return;
			}

			if (m_graphics)
			{
				if (m_graphics.material == m_material)
					return;
			}

			Init();
		}

	}
}
