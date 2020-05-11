using UnityEngine;
using UnityEngine.UI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// \brief Clone and cache materials
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
	/// It uses ClonedMaterialsCache to share the same material between multiple clones and thus protects batching.
	/// Also, if both example components rely on the same UiMaterialCloner instead of cloning it themselves, they will always use the same cloned material.
	/// 
	/// MaterialCloner needs either a Graphic or a Renderer on the same object. This however can not be enforced by [RequireComponent] by MaterialCloner, since it is either-or.

	[ExecuteAlways]
	public sealed class MaterialCloner : MonoBehaviour
	{
		[SerializeField]
		private bool m_useMaterialCache;

		[SerializeField]
		private string m_materialCacheKey;

		[SerializeField]
		private Material m_originalMaterial;

		[SerializeField]
		private Material m_clonedMaterial;

		[SerializeField]
		private int m_instanceId;

		private bool m_insertedIntoCache;

		/// Get cloned material
		public Material Material
		{
			get
			{
				InitIfNecessary();
				return m_clonedMaterial;
			}
		}

		/// Get original material
		public Material OriginalMaterial
		{
			get
			{
				InitIfNecessary();
				return m_originalMaterial;
			}
		}

		/// Get Graphic on same game object (may be null)
		public Graphic Graphic { get; private set; }

		/// Get Renderer on same game object (may be null)
		public Renderer Renderer { get; private set; }

		/// Switch between shared usage of UiMaterialCloner and unique cloned instance
		/// true: UiMaterialCloner uses ClonedMaterialsCache. This means the material will be shared between multiple UiMaterialCloner's using the same MaterialCacheKey.<BR>
		/// false: Each UiMaterialCloner maintains its own cloned material.<BR>
		/// Note that switching from non-use cache to use cache will most likely destroy the current material (also visible, if it differs in visual outcome)
		public bool UseClonedMaterialsCache
		{
			get => m_useMaterialCache;
			set
			{
				bool oldUseMaterialCache = m_useMaterialCache;
				m_useMaterialCache = value;
				PostChange(m_useMaterialCache, oldUseMaterialCache, m_materialCacheKey, m_materialCacheKey);
			}
		}

		/// Key for ClonedMaterialsCache. Use it to separate between groups of shared materials.
		/// Only valid if UseClonedMaterialsCache == true
		public string MaterialCacheKey
		{
			get => m_materialCacheKey;
			set
			{
				string oldMaterialCacheKey = m_materialCacheKey;
				m_materialCacheKey = value;
				PostChange(UseClonedMaterialsCache, UseClonedMaterialsCache, m_materialCacheKey, MaterialCacheKey);
			}
		}

		/// Is this material cloner currently valid?
		public bool Valid => m_clonedMaterial != null;

		private void OnEnable()
		{
			InitIfNecessary();
			ClonedMaterialsCache.Instance.EvMaterialReplaced.AddListener(OnMaterialReplaced);
		}

		private void OnDisable()
		{
			ClonedMaterialsCache.Instance.EvMaterialReplaced.RemoveListener(OnMaterialReplaced);
		}

		private void OnDestroy()
		{
			m_instanceId = 0;
		}

		private void OnMaterialReplaced( Material _oldOriginalMaterial, Material _newOriginalMaterial, Material _oldClonedMaterial, Material _newClonedMaterial )
		{
//Debug.Log($"m_useMaterialCache: {m_useMaterialCache} _oldOriginalMaterial: {_oldOriginalMaterial.name} m_originalMaterial:{m_originalMaterial.name} _oldClonedMaterial:{_oldClonedMaterial.name} m_clonedMaterial: {m_clonedMaterial.name}");
			if (!m_useMaterialCache || _oldOriginalMaterial != m_originalMaterial || _oldClonedMaterial != m_clonedMaterial)
				return;

			m_originalMaterial = _newOriginalMaterial;
			m_clonedMaterial = _newClonedMaterial;

			SetMaterial(_newClonedMaterial);
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
			if (Renderer)
				Renderer.sharedMaterial = _material;
			if (Graphic)
				Graphic.material = _material;
		}

		private Material GetMaterial()
		{
			if (Renderer)
				return Renderer.sharedMaterial;
			if (Graphic)
				return Graphic.material;
			return null;
		}

		private void Init()
		{
			Graphic = GetComponent<Graphic>();
			Renderer = GetComponent<Renderer>();

			bool gameObjectWasCloned = m_instanceId != 0 && m_instanceId != gameObject.GetInstanceID() && !m_useMaterialCache;
			m_instanceId = gameObject.GetInstanceID();

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

			if (materialHasChangedExternally || !m_originalMaterial)
			{
				ReleaseClonedMaterial(m_clonedMaterial);
				m_clonedMaterial = null;
				m_originalMaterial = GetMaterial();
			}

			if (m_clonedMaterial != null)
			{
				if (gameObjectWasCloned)
					m_clonedMaterial = Instantiate(m_clonedMaterial);

				SetMaterial(m_clonedMaterial);
				if (m_useMaterialCache && !m_insertedIntoCache)
				{
					ClonedMaterialsCache.Instance.HingeClonedMaterial(m_originalMaterial, m_clonedMaterial, m_materialCacheKey);
					m_insertedIntoCache = true;
				}
				return;
			}

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

			Material result = m_useMaterialCache ? ClonedMaterialsCache.Instance.AcquireClonedMaterial(_material, m_materialCacheKey) : Instantiate(_material);

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
				ClonedMaterialsCache.Instance.ReleaseClonedMaterial(_material, m_materialCacheKey);

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

			if (Renderer)
			{
				if (Renderer.sharedMaterial == m_clonedMaterial)
					return;
			}

			if (Graphic)
			{
				if (Graphic.material == m_clonedMaterial)
					return;
			}

			Init();
		}

		/// Note: this is only public because C# programmers dont have friends. UiMaterialClonerEditor needs access.
		public void PostChange(bool _currentUseMaterialCache, bool _previousUseMaterialCache, string _currentKey, string _previousKey)
		{
			if (!m_clonedMaterial || !m_originalMaterial)
				return;

			Debug.Assert (!(_currentUseMaterialCache != _previousUseMaterialCache && _currentKey != _previousKey));

			if (_currentUseMaterialCache != _previousUseMaterialCache)
			{
				if (_currentUseMaterialCache)
				{
					m_clonedMaterial.Destroy();
					m_clonedMaterial = ClonedMaterialsCache.Instance.AcquireClonedMaterial(m_originalMaterial, _currentKey);
				}
				else
				{
					Material freshlyClonedMaterial = Instantiate(m_clonedMaterial);
					ClonedMaterialsCache.Instance.ReleaseClonedMaterial(m_clonedMaterial, _currentKey);
					m_clonedMaterial = freshlyClonedMaterial;
				}
				SetMaterial(m_clonedMaterial);
			}

			if (_currentUseMaterialCache && _currentKey != _previousKey)
			{
				ClonedMaterialsCache.Instance.UnhingeClonedMaterial(m_clonedMaterial, _previousKey);
				ClonedMaterialsCache.Instance.HingeClonedMaterial(m_originalMaterial, m_clonedMaterial, _currentKey);
			}
		}

#if UNITY_EDITOR
		/// \addtogroup Editor Code
		public string GetDebugInfoStr()
		{
			return $"Original Material:{m_originalMaterial.name}    Cloned Material:{m_clonedMaterial.name}";
		}
#endif
	}

#if UNITY_EDITOR
	/// \addtogroup Editor Code
	/// UiMaterialCloner is quite fragile regarding its options and thus needs a special
	/// treatment in the editor
	[CustomEditor(typeof(MaterialCloner))]
	public class UiMaterialClonerEditor : Editor
	{
		protected SerializedProperty m_useMaterialCacheProp;
		protected SerializedProperty m_materialCacheKeyProp;
		protected SerializedProperty m_originalMaterialProp;

		public virtual void OnEnable()
		{
			m_useMaterialCacheProp = serializedObject.FindProperty("m_useMaterialCache");
			m_materialCacheKeyProp = serializedObject.FindProperty("m_materialCacheKey");
			m_originalMaterialProp = serializedObject.FindProperty("m_originalMaterial");
		}

		public override void OnInspectorGUI()
		{
			MaterialCloner thisMaterialCloner = (MaterialCloner)target;
			bool previousUseMaterialCache = m_useMaterialCacheProp.boolValue;
			string previousMaterialCacheKey = m_materialCacheKeyProp.stringValue;

			EditorGUILayout.PropertyField(m_useMaterialCacheProp, new GUIContent("Share Material between instances"));
			if (m_useMaterialCacheProp.boolValue)
				EditorGUILayout.PropertyField(m_materialCacheKeyProp, new GUIContent("Sharing Key"));

			EditorGUILayout.PropertyField(m_originalMaterialProp, new GUIContent("Original Material"));

			Material prevOriginalMaterial = thisMaterialCloner.OriginalMaterial;
			bool cachedMaterialHasChanged = m_useMaterialCacheProp.boolValue && (Material) m_originalMaterialProp.objectReferenceValue != thisMaterialCloner.OriginalMaterial;

			EditorGUILayout.LabelField(thisMaterialCloner.GetDebugInfoStr());

			serializedObject.ApplyModifiedProperties();

			thisMaterialCloner.PostChange( m_useMaterialCacheProp.boolValue, previousUseMaterialCache, m_materialCacheKeyProp.stringValue, previousMaterialCacheKey );

			if (GUILayout.Button("Force reset material") || cachedMaterialHasChanged)
			{
				ClonedMaterialsCache.Instance.ReplaceMaterial(prevOriginalMaterial, (Material) m_originalMaterialProp.objectReferenceValue, m_materialCacheKeyProp.stringValue);
			}

		}

	}
#endif


}
