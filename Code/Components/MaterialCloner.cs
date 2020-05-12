using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System.Collections.Generic;

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
		[FormerlySerializedAs("m_useMaterialCache")]
		private bool m_isSharedMaterial;

		[SerializeField]
		private string m_materialCacheKey;

		[SerializeField]
		private Material m_originalMaterial;

		[SerializeField]
		private Material m_clonedMaterial;

		[SerializeField]
		private int m_instanceId;

		private readonly HashSet<MaterialCloner> s_instances = new HashSet<MaterialCloner>();

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
			get => m_isSharedMaterial;
			set
			{
				bool oldUseMaterialCache = m_isSharedMaterial;
				m_isSharedMaterial = value;
				PostChange(m_isSharedMaterial, oldUseMaterialCache, m_materialCacheKey, m_materialCacheKey, s_instances);
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
				PostChange(UseClonedMaterialsCache, UseClonedMaterialsCache, m_materialCacheKey, MaterialCacheKey, s_instances);
			}
		}

		/// Is this material cloner currently valid?
		public bool Valid => m_clonedMaterial != null;

		private void Awake()
		{
			s_instances.Add(this);
		}

		private void OnEnable()
		{
			InitIfNecessary();
		}

		private void OnDestroy()
		{
			m_instanceId = 0;
			s_instances.Remove(this);
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

			bool gameObjectWasCloned = m_instanceId != 0 && m_instanceId != gameObject.GetInstanceID() && !m_isSharedMaterial;
			m_instanceId = gameObject.GetInstanceID();

			// This happens on Undo. Workaround.
			if (GetMaterial() == null)
			{
				if (m_clonedMaterial != null)
					SetMaterial(m_clonedMaterial);
				else
					SetMaterial(m_originalMaterial);
			}

			if (m_clonedMaterial != null)
			{
				if (gameObjectWasCloned && !m_isSharedMaterial)
					m_clonedMaterial = Instantiate(m_clonedMaterial);

				SetMaterial(m_clonedMaterial);
				return;
			}

			m_clonedMaterial = Instantiate(m_originalMaterial);
			SetMaterial(m_clonedMaterial);
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

		/// Note: this is only public because C# programmers don't have friends. UiMaterialClonerEditor needs access.
		public void OnMaterialReplaced( string _key, Material _oldOriginalMaterial, Material _newOriginalMaterial, Material _newClonedMaterial )
		{
			if (!m_isSharedMaterial || _key != m_materialCacheKey || _oldOriginalMaterial != m_originalMaterial)
				return;

			m_originalMaterial = _newOriginalMaterial;
			m_clonedMaterial = _newClonedMaterial;

			SetMaterial(_newClonedMaterial);
		}

		/// Note: this is only public because C# programmers don't have friends. UiMaterialClonerEditor needs access.
		public void PostChange(bool _currentShareMaterial, bool _previousShareMaterial, string _currentKey, string _previousKey, IEnumerable<MaterialCloner> _instances)
		{
			if (!m_clonedMaterial || !m_originalMaterial)
				return;

			Debug.Assert (!(_currentShareMaterial != _previousShareMaterial && _currentKey != _previousKey));

			if (_currentShareMaterial != _previousShareMaterial)
			{
				if (_currentShareMaterial)
				{
					Material clonedMaterial = FindClonedMaterialInOtherInstances(_instances);
					if (clonedMaterial)
					{
						m_clonedMaterial.Destroy();
						m_clonedMaterial = clonedMaterial;
						SetMaterial(clonedMaterial);
					}
					return;
				}
				else
				{
					// we have to ensure that we are not the last instance holding this material, otherwise leak
					Material clonedMaterial = FindClonedMaterialInOtherInstances(_instances);

					// there's another instance holding the same material, we can safely clone
					if (clonedMaterial)
					{
						m_clonedMaterial = Instantiate(clonedMaterial);
						SetMaterial(m_clonedMaterial);
						return;
					}

					// nothing to do, we're the only instance holding this material
				}
				SetMaterial(m_clonedMaterial);
			}

			if (_currentShareMaterial && _currentKey != _previousKey)
			{
				m_materialCacheKey = _previousKey;
				Material oldSharedMaterial = FindClonedMaterialInOtherInstances(_instances);
				m_materialCacheKey = _currentKey;
				Material newSharedMaterial = FindClonedMaterialInOtherInstances(_instances);

				if (oldSharedMaterial == null)
				{
					if (newSharedMaterial == null)
					{
						// we have sharing enabled, but no one else has this material. Nothing to do.
						return;
					}
					else
					{
						// we previously were holding the material solitary, but for the new material, there's already an instance using it.
						// we have to destroy the old material and use the new one.
						m_clonedMaterial.Destroy();
						m_clonedMaterial = newSharedMaterial;
					}
				}
				else
				{
					if (newSharedMaterial == null)
					{
						// we previously were sharing the material, but now holding solitary.
						// we mustn't destroy the old material, since it is still in use, we need to clone the previous material instead.
						m_clonedMaterial = Instantiate(m_clonedMaterial);
					}
					else
					{
						// we shared before and after. We just need to switch to the new material. No cloning, no destroy.
						m_clonedMaterial = newSharedMaterial;
					}
				}

				SetMaterial(m_clonedMaterial);
			}
		}

		private Material FindClonedMaterialInOtherInstances(IEnumerable<MaterialCloner> _instances)
		{
			foreach (var instance in _instances)
			{
				if (instance == this)
					continue;

				if (instance.m_isSharedMaterial
					&& instance.m_originalMaterial == m_originalMaterial
					&& instance.m_materialCacheKey == m_materialCacheKey)
				{
					return instance.m_clonedMaterial;
				}
			}

			return null;
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
	public class MaterialClonerEditor : Editor
	{
		protected SerializedProperty m_isSharedMaterialProp;
		protected SerializedProperty m_materialCacheKeyProp;
		protected SerializedProperty m_originalMaterialProp;

		public virtual void OnEnable()
		{
			m_isSharedMaterialProp = serializedObject.FindProperty("m_isSharedMaterial");
			m_materialCacheKeyProp = serializedObject.FindProperty("m_materialCacheKey");
			m_originalMaterialProp = serializedObject.FindProperty("m_originalMaterial");
		}

		public override void OnInspectorGUI()
		{
			MaterialCloner thisMaterialCloner = (MaterialCloner)target;
			bool previousSharedMaterial = m_isSharedMaterialProp.boolValue;
			string previousMaterialCacheKey = m_materialCacheKeyProp.stringValue;

			EditorGUILayout.PropertyField(m_isSharedMaterialProp, new GUIContent("Share Material between instances"));
			if (m_isSharedMaterialProp.boolValue)
				EditorGUILayout.PropertyField(m_materialCacheKeyProp, new GUIContent("Sharing Key"));

			EditorGUILayout.PropertyField(m_originalMaterialProp, new GUIContent("Original Material"));

			Material prevOriginalMaterial = thisMaterialCloner.OriginalMaterial;
			bool cachedMaterialHasChanged = m_isSharedMaterialProp.boolValue && (Material) m_originalMaterialProp.objectReferenceValue != thisMaterialCloner.OriginalMaterial;

			EditorGUILayout.LabelField(thisMaterialCloner.GetDebugInfoStr());

			serializedObject.ApplyModifiedProperties();

			if (m_isSharedMaterialProp.boolValue != previousSharedMaterial || m_materialCacheKeyProp.stringValue != previousMaterialCacheKey)
			{

				MaterialCloner[] instances = FindObjectsOfType<MaterialCloner>();
				thisMaterialCloner.PostChange( m_isSharedMaterialProp.boolValue, previousSharedMaterial, m_materialCacheKeyProp.stringValue, previousMaterialCacheKey, instances );
			}

			if (GUILayout.Button("Force reset material") || cachedMaterialHasChanged)
			{
				Material oldClonedMaterial = thisMaterialCloner.Material;
				Material clonedMaterial = Instantiate(thisMaterialCloner.OriginalMaterial);
				MaterialCloner[] instances = FindObjectsOfType<MaterialCloner>();
				foreach (var instance in instances)
					instance.OnMaterialReplaced(m_materialCacheKeyProp.stringValue, prevOriginalMaterial, thisMaterialCloner.OriginalMaterial, clonedMaterial);
				oldClonedMaterial.Destroy();
			}
		}

	}
#endif


}
