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

		[SerializeField]
		private Material m_originalMaterial;

		[SerializeField]
		private Material m_clonedMaterial;

		private Graphic m_graphics;
		private Renderer m_renderer;
		private bool m_insertedIntoCache;

		public Material Material
		{
			get
			{
				InitIfNecessary();
				return m_clonedMaterial;
			}
		}

		public bool Valid => m_clonedMaterial != null;

		private void OnEnable()
		{
			InitIfNecessary();
		}

		private void ReleaseCurrentClonedMaterial()
		{
			if (!m_clonedMaterial)
				return;

			ReleaseClonedMaterial(m_clonedMaterial);
			m_clonedMaterial = null;

			SetMaterial(m_originalMaterial);
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
			m_graphics = GetComponent<Graphic>();
			m_renderer = GetComponent<Renderer>();

			// This happens on Undo. Workaround.
			if (GetMaterial() == null)
			{
				if (m_clonedMaterial != null)
					SetMaterial(m_clonedMaterial);
				else
					SetMaterial(m_originalMaterial);
			}

			Material currentRendererMaterial = GetMaterial();
			bool materialHasChangedExternally = currentRendererMaterial != m_originalMaterial && currentRendererMaterial != m_clonedMaterial;

			if (materialHasChangedExternally)
			{
				ReleaseClonedMaterial(m_clonedMaterial);
				m_clonedMaterial = null;
				m_originalMaterial = GetMaterial();
			}

			if (m_clonedMaterial != null)
			{
				SetMaterial(m_clonedMaterial);
				if (m_useMaterialCache && !m_insertedIntoCache)
				{
					UiMaterialCache.Instance.InsertClonedMaterial(m_originalMaterial, m_clonedMaterial, m_materialCacheKey);
					m_insertedIntoCache = true;
				}
				return;
			}

			if (!m_originalMaterial)
				m_originalMaterial = GetMaterial();

			m_clonedMaterial = AcquireClonedMaterial(m_originalMaterial);
			SetMaterial(m_clonedMaterial);
		}

		private Material AcquireClonedMaterial(Material _material)
		{
			if (IsOriginal(_material))
			{
				m_originalMaterial = _material;
			}
			else
			{
				return m_clonedMaterial;
			}

			// We need to make a temporary copy since the cloned material must be released before acquiring a new one
			Material previousCloned = m_clonedMaterial != null ? Instantiate(m_clonedMaterial) : null; 

			ReleaseClonedMaterial(m_clonedMaterial);
			m_clonedMaterial = null;

			Material result = m_useMaterialCache ? UiMaterialCache.Instance.AcquireClonedMaterial(_material, m_materialCacheKey) : Instantiate(_material);

			if (previousCloned)
			{
				result.CopyPropertiesFromMaterial(previousCloned);
				previousCloned.Destroy();
			}

			return result;
		}

		private void ReleaseClonedMaterial(Material _material)
		{
			if (_material == null)
				return;

			if (m_useMaterialCache)
				UiMaterialCache.Instance.ReleaseClonedMaterial(_material, m_materialCacheKey);

			_material.Destroy();
		}

		private bool IsOriginal(Material _material)
		{
			if (_material == null || m_clonedMaterial == null)
				return true;

			return m_clonedMaterial != GetMaterial();
		}

		private void InitIfNecessary()
		{
			if (m_clonedMaterial == null)
			{
				Init();
				return;
			}

			if (m_renderer)
			{
				if (m_renderer.sharedMaterial == m_clonedMaterial)
					return;
			}

			if (m_graphics)
			{
				if (m_graphics.material == m_clonedMaterial)
					return;
			}

			Init();
		}

#if UNITY_EDITOR

		private Material m_previousMat;

		/// \addtogroup Editor Code
		public void PreChange()
		{
			if (!m_clonedMaterial)
				return;

			m_previousMat = Instantiate(m_clonedMaterial);
			ReleaseCurrentClonedMaterial();
			m_clonedMaterial = null;
		}

		/// \addtogroup Editor Code
		public void PostChange()
		{
			if (!m_previousMat)
				return;
			Init();
			if (m_clonedMaterial)
				m_clonedMaterial.CopyPropertiesFromMaterial(m_previousMat);
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
