﻿using UnityEngine;
using UnityEngine.UI;
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
		[FormerlySerializedAs("m_materialCacheKey")]
		private string m_materialInstanceKey;

		[SerializeField]
		private Material m_originalMaterial;

		[SerializeField]
		private Material m_clonedMaterial;

		[SerializeField]
		private int m_instanceId;

		private static readonly HashSet<MaterialCloner> s_instances = new HashSet<MaterialCloner>();

		/// Get cloned material
		public Material ClonedMaterial
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
			set
			{
				InitIfNecessary();
				if (m_originalMaterial == value)
					return;
				Clear();
				SetMaterialToRenderer(value);
				Init();
			}
		}

		/// Get Graphic on same game object (may be null)
		public Graphic Graphic { get; private set; }

		/// Get Renderer on same game object (may be null)
		public Renderer Renderer { get; private set; }

		/// Switch between shared usage of UiMaterialCloner and unique cloned instance
		/// Note that switching from non-use cache to use cache will most likely destroy the current material (also visible, if it differs in visual outcome)
		public bool IsSharedMaterial
		{
			get => m_isSharedMaterial;
			set
			{
				bool oldIsSharedMaterial = m_isSharedMaterial;
				m_isSharedMaterial = value;
				PostChangeMaterial(m_isSharedMaterial, oldIsSharedMaterial, s_instances);
			}
		}

		/// Key for shared material instances. Use it to separate between groups of shared materials.
		/// Only valid if IsSharedMaterial == true
		public string MaterialInstanceKey
		{
			get => m_materialInstanceKey;
			set
			{
				string oldMaterialInstanceKey = m_materialInstanceKey;
				m_materialInstanceKey = value;
				PostChangeKey(m_materialInstanceKey, oldMaterialInstanceKey, s_instances);
			}
		}

		/// Is this material cloner currently valid?
		public bool Valid => m_clonedMaterial != null;

		private void Awake()
		{
			s_instances.Add(this);
		}

#if UNITY_EDITOR
		/// Workaround against Unity forgetting the static hash set on every recompile
		private void Update()
		{
			if (!s_instances.Contains(this))
				s_instances.Add(this);
		}
#endif

		private void OnEnable()
		{
			InitIfNecessary();
		}

		private void OnDestroy()
		{
			m_instanceId = 0;
			s_instances.Remove(this);
		}

		private void SetMaterialToRenderer(Material _material)
		{
			if (Renderer)
				Renderer.sharedMaterial = _material;
			if (Graphic)
				Graphic.material = _material;
		}

		private Material GetMaterialFromRenderer()
		{
			if (Renderer)
				return Renderer.sharedMaterial;
			if (Graphic)
				return Graphic.material;
			return null;
		}

		private void Clear()
		{
			SetMaterialToRenderer(m_originalMaterial);
			if (m_isSharedMaterial)
			{
				Material foundMaterial = FindClonedMaterialInOtherInstances(s_instances, m_materialInstanceKey);
				if (foundMaterial == null)
					m_clonedMaterial.SafeDestroy();
			}
			else
			{
				m_clonedMaterial.SafeDestroy();
			}
			m_clonedMaterial = null;
			m_originalMaterial = null;
		}

		private void Init()
		{
			Graphic = GetComponent<Graphic>();
			Renderer = GetComponent<Renderer>();

			bool gameObjectWasCloned = m_instanceId != 0 && m_instanceId != gameObject.GetInstanceID() && !m_isSharedMaterial;
			m_instanceId = gameObject.GetInstanceID();

			if (m_originalMaterial == null)
			{
				m_originalMaterial = GetMaterialFromRenderer();
				if (m_originalMaterial == null)
					return;
			}

			// Material in renderer/graphic was forgotten. This happens on Undo. Workaround.
			if (GetMaterialFromRenderer() == null)
			{
				if (m_clonedMaterial != null)
					SetMaterialToRenderer(m_clonedMaterial);
				else
					SetMaterialToRenderer(m_originalMaterial);
			}

			// We already have a cloned material.
			if (m_clonedMaterial != null)
			{
				// Duplicate material on game object clone for non-shared materials.
				if (gameObjectWasCloned && !m_isSharedMaterial)
					m_clonedMaterial = Instantiate(m_clonedMaterial);

				SetMaterialToRenderer(m_clonedMaterial);
				return;
			}

			// First, try to find already existing shared material
			if (m_isSharedMaterial)
				m_clonedMaterial = FindClonedMaterialInOtherInstances(s_instances, m_materialInstanceKey);

			// If not found, create a new one.
			if (m_clonedMaterial == null)
				m_clonedMaterial = Instantiate(m_originalMaterial);

			SetMaterialToRenderer(m_clonedMaterial);
		}

		private void InitIfNecessary()
		{
#if UNITY_EDITOR
			// never init if we're a prefab
			if (EditorGameObjectUtility.IsPrefab(gameObject))
				return;
#endif

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

		private void PostChangeMaterial(bool _currentShareMaterial, bool _previousShareMaterial, IEnumerable<MaterialCloner> _instances)
		{
			if (!m_clonedMaterial || !m_originalMaterial)
				return;

			if (_currentShareMaterial != _previousShareMaterial)
			{
				if (_currentShareMaterial)
				{
					Material clonedMaterial = FindClonedMaterialInOtherInstances(_instances);
					if (clonedMaterial)
					{
						m_clonedMaterial.SafeDestroy();
						m_clonedMaterial = clonedMaterial;
						SetMaterialToRenderer(clonedMaterial);
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
						SetMaterialToRenderer(m_clonedMaterial);
						return;
					}

					// nothing to do, we're the only instance holding this material
				}
				SetMaterialToRenderer(m_clonedMaterial);
			}
		}

		private void PostChangeKey(string _currentKey, string _previousKey, IEnumerable<MaterialCloner> _instances)
		{
			if (!m_clonedMaterial || !m_originalMaterial)
				return;

			if (m_isSharedMaterial && _currentKey != _previousKey)
			{
				Material oldSharedMaterial = FindClonedMaterialInOtherInstances(_instances, _previousKey);
				Material newSharedMaterial = FindClonedMaterialInOtherInstances(_instances, _currentKey);

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
						m_clonedMaterial.SafeDestroy();
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

				SetMaterialToRenderer(m_clonedMaterial);
			}
		}

		/// Note: This is quite inefficient. A dictionary to track cloned materials by int key made out of original material and explicit key would be better.
		/// This is however quite some change, so I leave it as it is, until it becomes a real issue (hundreds of material cloners)
		/// Only public because C# programmers don't have friends - MaterialClonerEditor needs access
		public Material FindClonedMaterialInOtherInstances(IEnumerable<MaterialCloner> _instances, string _key = null)
		{
			if (string.IsNullOrEmpty(_key))
				_key = m_materialInstanceKey;

			foreach (var instance in _instances)
			{
				if (instance == this)
					continue;

				if (instance.m_isSharedMaterial
					&& instance.m_originalMaterial == m_originalMaterial
					&& instance.m_materialInstanceKey == _key)
				{
					return instance.m_clonedMaterial;
				}
			}

			return null;
		}
	}

#if UNITY_EDITOR
	/// \addtogroup Editor Code
	/// UiMaterialCloner is quite fragile regarding its options and thus needs a special
	/// treatment in the editor
	[CustomEditor(typeof(MaterialCloner))]
	public class MaterialClonerEditor : UnityEditor.Editor
	{
		protected SerializedProperty m_isSharedMaterialProp;
		protected SerializedProperty m_materialInstanceKeyProp;
		protected SerializedProperty m_originalMaterialProp;
		protected SerializedProperty m_clonedMaterialProp;

		private void OnEnable()
		{
			m_isSharedMaterialProp = serializedObject.FindProperty("m_isSharedMaterial");
			m_materialInstanceKeyProp = serializedObject.FindProperty("m_materialInstanceKey");
			m_originalMaterialProp = serializedObject.FindProperty("m_originalMaterial");
			m_clonedMaterialProp = serializedObject.FindProperty("m_clonedMaterial");
		}

		public override void OnInspectorGUI()
		{
			MaterialCloner thisMaterialCloner = (MaterialCloner)target;
			if (EditorGameObjectUtility.InfoBoxIfPrefab(thisMaterialCloner.gameObject))
			{
				Renderer renderer = thisMaterialCloner.GetComponent<Renderer>();
				if ( renderer != null && renderer.sharedMaterial == null && thisMaterialCloner.OriginalMaterial != null )
					renderer.sharedMaterial = thisMaterialCloner.OriginalMaterial;

				Graphic graphic = thisMaterialCloner.GetComponent<Graphic>();
				if ( graphic != null && graphic.material == null && thisMaterialCloner.OriginalMaterial != null )
					graphic.material = thisMaterialCloner.OriginalMaterial;

				return;
			}

			bool previousSharedMaterial = m_isSharedMaterialProp.boolValue;
			string previousMaterialCacheKey = m_materialInstanceKeyProp.stringValue;

			EditorGUILayout.PropertyField(m_isSharedMaterialProp, new GUIContent("Share Material between instances"));
			if (m_isSharedMaterialProp.boolValue)
				EditorGUILayout.PropertyField(m_materialInstanceKeyProp, new GUIContent("Sharing Key"));

			EditorGUILayout.PropertyField(m_originalMaterialProp, new GUIContent("Original Material"));

			Material prevOriginalMaterial = thisMaterialCloner.OriginalMaterial;
			bool materialHasChanged = (Material) m_originalMaterialProp.objectReferenceValue != thisMaterialCloner.OriginalMaterial;

			serializedObject.ApplyModifiedProperties();

			if (m_isSharedMaterialProp.boolValue != previousSharedMaterial)
			{
				MaterialCloner[] instances = FindObjectsOfType<MaterialCloner>();
				PostChangeShareMaterial( thisMaterialCloner, m_isSharedMaterialProp.boolValue, previousSharedMaterial, instances );
			}

			if (m_materialInstanceKeyProp.stringValue != previousMaterialCacheKey)
			{
				MaterialCloner[] instances = FindObjectsOfType<MaterialCloner>();
				PostChangeKey( thisMaterialCloner, m_materialInstanceKeyProp.stringValue, previousMaterialCacheKey, instances );
			}

			if (GUILayout.Button("Force reset material") || materialHasChanged)
			{
				Undo.SetCurrentGroupName(materialHasChanged ? "Change Material" : "Force reset material");

				Material oldClonedMaterial = thisMaterialCloner.ClonedMaterial;
				Material clonedMaterial = Instantiate(thisMaterialCloner.OriginalMaterial);
				Undo.RegisterCreatedObjectUndo(clonedMaterial, "");

				if (thisMaterialCloner.IsSharedMaterial)
				{
					MaterialCloner[] instances = FindObjectsOfType<MaterialCloner>();
					Material newOriginalMaterial = thisMaterialCloner.OriginalMaterial;
				
					// we have to reset the original material of the current MaterialCloner;
					// otherwise it will return false in WillMaterialBeReplaced() in the replacement loop below.
					m_originalMaterialProp.objectReferenceValue = prevOriginalMaterial;
					serializedObject.ApplyModifiedProperties();

					foreach (var instance in instances)
					{
						if (WouldMaterialBeReplaced(instance, m_materialInstanceKeyProp.stringValue, prevOriginalMaterial))
						{
							SetMaterial(instance, newOriginalMaterial, clonedMaterial);
						}
					}
				}
				else
				{
					m_clonedMaterialProp.objectReferenceValue = clonedMaterial;
					serializedObject.ApplyModifiedProperties();
				}

				oldClonedMaterial.SafeDestroy();

				Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
			}

			EditorGUILayout.LabelField(GetDebugInfoStr(thisMaterialCloner));
		}

		private static bool WouldMaterialBeReplaced( MaterialCloner _materialCloner, string _key, Material _oldOriginalMaterial )
		{
			return _materialCloner.IsSharedMaterial && _key == _materialCloner.MaterialInstanceKey && _oldOriginalMaterial == _materialCloner.OriginalMaterial;
		}

		private void SetMaterial( MaterialCloner _materialCloner, Material _originalMaterial, Material _clonedMaterial )
		{
			SerializedObject serObj = new SerializedObject(_materialCloner);
			serObj.FindProperty("m_originalMaterial").objectReferenceValue = _originalMaterial;
			serObj.FindProperty("m_clonedMaterial").objectReferenceValue = _clonedMaterial;
			serObj.ApplyModifiedProperties();
			if (_materialCloner.Renderer)
			{
				Undo.RegisterCompleteObjectUndo(_materialCloner.Renderer, "");
				_materialCloner.Renderer.material = _clonedMaterial;
			}
			else if (_materialCloner.Graphic)
			{
				Undo.RegisterCompleteObjectUndo(_materialCloner.Graphic, "");
				_materialCloner.Graphic.material = _clonedMaterial;
			}
		}


		private void DestroySharedMaterial( MaterialCloner _materialCloner )
		{
			Material moribund = _materialCloner.ClonedMaterial;

			SerializedObject serObj = new SerializedObject(_materialCloner);
			serObj.FindProperty("m_clonedMaterial").objectReferenceValue = null;
			serObj.ApplyModifiedProperties();

			moribund.SafeDestroy();
		}

		private void PostChangeShareMaterial( MaterialCloner _materialCloner, bool _currentShareMaterial, bool _previousShareMaterial, IEnumerable<MaterialCloner> _instances)
		{
			if (_currentShareMaterial != _previousShareMaterial)
			{
				if (_currentShareMaterial)
				{
					Material clonedMaterial = _materialCloner.FindClonedMaterialInOtherInstances(_instances);
					if (clonedMaterial)
					{
						DestroySharedMaterial(_materialCloner);

						SetMaterial(_materialCloner, _materialCloner.OriginalMaterial, clonedMaterial);
					}
					return;
				}
				else
				{
					// we have to ensure that we are not the last instance holding this material, otherwise leak
					Material clonedMaterial = _materialCloner.FindClonedMaterialInOtherInstances(_instances);

					// there's another instance holding the same material, we can safely clone
					if (clonedMaterial)
					{
						clonedMaterial = Instantiate(clonedMaterial);
						Undo.RegisterCreatedObjectUndo(clonedMaterial, "");

						SetMaterial(_materialCloner, _materialCloner.OriginalMaterial, clonedMaterial);
						return;
					}

					// nothing to do, we're the only instance holding this material
				}
			}

		}

		private void PostChangeKey( MaterialCloner _materialCloner, string _currentKey, string _previousKey, IEnumerable<MaterialCloner> _instances)
		{
			if (!_materialCloner.IsSharedMaterial)
				return;

			if (_currentKey != _previousKey)
			{
				Material oldSharedMaterial = _materialCloner.FindClonedMaterialInOtherInstances(_instances, _previousKey);
				Material newSharedMaterial = _materialCloner.FindClonedMaterialInOtherInstances(_instances, _currentKey);

				Material clonedMaterial = _materialCloner.ClonedMaterial;

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
						DestroySharedMaterial(_materialCloner);
						clonedMaterial = newSharedMaterial;
					}
				}
				else
				{
					if (newSharedMaterial == null)
					{
						// we previously were sharing the material, but now holding solitary.
						// we mustn't destroy the old material, since it is still in use, we need to clone the previous material instead.
						clonedMaterial = Instantiate(clonedMaterial);
						Undo.RegisterCreatedObjectUndo(clonedMaterial, "");
					}
					else
					{
						// we shared before and after. We just need to switch to the new material. No cloning, no destroy.
						clonedMaterial = newSharedMaterial;
					}
				}

				SetMaterial(_materialCloner, _materialCloner.OriginalMaterial, clonedMaterial);
			}
		}

		private string GetDebugInfoStr(MaterialCloner _materialCloner)
		{
			MaterialCloner[] instances = FindObjectsOfType<MaterialCloner>();

			int numInstances = 0;
			foreach (var instance in instances)
			{
				if (instance.ClonedMaterial == _materialCloner.ClonedMaterial)
					numInstances++;
			}

			if (_materialCloner.OriginalMaterial == null)
				return "Original Material is null!";

			if (_materialCloner.ClonedMaterial == null)
				return "Cloned Material is null!";

			return $"Original Material:{_materialCloner.OriginalMaterial.name}    Cloned Material:{_materialCloner.ClonedMaterial.name} ClonedMaterialInstances: {numInstances}";
		}

	}
#endif


}
