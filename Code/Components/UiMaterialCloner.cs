using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

		private Material m_originalMaterial;
		private Material m_material;
		private Graphic m_graphics;
		private Renderer m_renderer;

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
			ReleaseCurrentClonedMaterial();
			return;
		}

		private void ReleaseCurrentClonedMaterial()
		{
			if (!m_material)
				return;

			SetMaterial(ReleaseClonedMaterial(m_material));
			m_originalMaterial = GetMaterial();

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

			if (GetMaterial() == null)
				SetMaterial(m_originalMaterial);

			m_material = AcquireClonedMaterial(GetMaterial());
			SetMaterial(m_material);
		}

		private Material AcquireClonedMaterial(Material _material)
		{
			if (IsOriginal(_material))
			{
				m_originalMaterial = _material;
			}
			else
			{
				return m_material;
			}

			// We need to make a temporary copy since the cloned material must be released before acquiring a new one
			Material previousCloned = m_material != null ? Instantiate(m_material) : null; 

			ReleaseClonedMaterial(m_material);
			m_material = null;

			Material result = m_useMaterialCache ? UiMaterialCache.Instance.AcquireClonedMaterial(_material, m_materialCacheKey) : Instantiate(_material);

			if (previousCloned)
			{
				result.CopyPropertiesFromMaterial(previousCloned);
				previousCloned.Destroy();
			}

			return result;
		}

		private Material ReleaseClonedMaterial(Material _material)
		{
			if (_material == null)
				return null;

			if (m_useMaterialCache)
				return UiMaterialCache.Instance.ReleaseClonedMaterial(_material, m_materialCacheKey);

			_material.Destroy();
			return m_originalMaterial;
		}

		private bool IsOriginal(Material _material)
		{
			if (_material == null || m_material == null)
				return true;

			return m_material != GetMaterial();
		}

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

#if UNITY_EDITOR

		private Material m_previousMat;

		/// \addtogroup Editor Code
		public void PreChange()
		{
			if (!m_material)
				return;

			m_previousMat = Instantiate(m_material);
			ReleaseCurrentClonedMaterial();
			m_material = null;
		}

		/// \addtogroup Editor Code
		public void PostChange()
		{
			if (!m_previousMat)
				return;
			Init();
			if (m_material)
				m_material.CopyPropertiesFromMaterial(m_previousMat);
			m_previousMat.Destroy();
			m_previousMat = null;
		}
#endif
	}

#if UNITY_EDITOR
	/// \addtogroup Editor Code
	/// UiMaterialCloner is quite fragile regarding its options and thus needs a special
	/// treatment in the editor
	[CustomEditor(typeof(UiMaterialCloner))]
	public class UiMaterialClonerEditor : Editor
	{
		protected SerializedProperty m_useMaterialCacheProp;
		protected SerializedProperty m_materialCacheKeyProp;

		public virtual void OnEnable()
		{
			m_useMaterialCacheProp = serializedObject.FindProperty("m_useMaterialCache");
			m_materialCacheKeyProp = serializedObject.FindProperty("m_materialCacheKey");
		}

		public override void OnInspectorGUI()
		{
			UiMaterialCloner thisMaterialCloner = (UiMaterialCloner)target;
			bool previousUseMaterialCache = m_useMaterialCacheProp.boolValue;
			string previousMaterialCacheKey = m_materialCacheKeyProp.stringValue;

			EditorGUILayout.PropertyField(m_useMaterialCacheProp);
			EditorGUILayout.PropertyField(m_materialCacheKeyProp);

			bool changed = previousUseMaterialCache != m_useMaterialCacheProp.boolValue || previousMaterialCacheKey != m_materialCacheKeyProp.stringValue;

			if (changed)
			{
				Undo.RecordObject(thisMaterialCloner, "Value change");
				thisMaterialCloner.PreChange();
			}

			serializedObject.ApplyModifiedProperties();

			if (changed)
				thisMaterialCloner.PostChange();
		}

	}
#endif


}
