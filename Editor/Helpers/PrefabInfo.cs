using UnityEditor;
using UnityEditor.SceneManagement;              // ObjectOverride + PrefabStageUtility
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Provides a complete, editor-only snapshot of a prefab-related <see cref="GameObject"/>.
	/// All relevant prefab data are resolved eagerly (except expensive override lists, which are
	/// fetched lazily).  The class is intended for editor tooling where clarity and robustness
	/// outweigh raw performance.
	/// One other goal for this class was to find a sane naming for Unity's wildly weird naming in PrefabUtility().
	/// E.G. PrefabUtility.HasPrefabInstanceAnyOverrides is simply called IsDirty here (which exactly is the purpose of this PrefabUtility method)
	/// </summary>
	public class PrefabInfo
	{
		public class OverrideInfo
		{
			public bool IsPrefab;
			public List<PropertyModification> PropertyModifications;
			public List<AddedGameObject> AddedGameObjects;
			public List<RemovedGameObject> RemovedGameObjects;
			public List<AddedComponent> AddedComponents;
			public List<RemovedComponent> RemovedComponents;
			public List<ObjectOverride> ObjectOverrides;
		}
		
		private GameObject m_gameObject;
		private bool m_isPrefab;
		private bool m_isInstanceRoot;
		private bool m_isPartOfPrefab;
		private PrefabAssetType m_assetType;
		private PrefabInstanceStatus m_instanceStatus;
		private string m_assetPath;
		private string m_assetGuid;
		private OverrideInfo m_overrideInfo;

		// -------------------------------------------------------------------
		// Info getters
		// -------------------------------------------------------------------

		public bool IsValid => m_gameObject != null;
		public bool IsVariantAsset => m_assetType == PrefabAssetType.Variant;
		public bool IsModelAsset => m_assetType == PrefabAssetType.Model;

		public bool IsDirty => m_instanceStatus == PrefabInstanceStatus.Connected
		                       && PrefabUtility.HasPrefabInstanceAnyOverrides(m_gameObject, false);
		public bool HasUnsavedChanges => IsDirty;




		public bool IsConnected => m_instanceStatus == PrefabInstanceStatus.Connected;
		public bool IsDisconnected => m_instanceStatus == PrefabInstanceStatus.Disconnected;

		public bool IsMissingAsset => m_assetType == PrefabAssetType.MissingAsset
		                              || m_instanceStatus == PrefabInstanceStatus.MissingAsset;

		public GameObject GameObject           => m_gameObject;
		public bool IsPrefab                   => m_isPrefab;
		public bool IsInstanceRoot             => m_isInstanceRoot;
		public bool IsPartOfPrefab             => m_isPartOfPrefab;
		public PrefabAssetType AssetType       => m_assetType;
		public PrefabInstanceStatus InstanceStatus => m_instanceStatus;


		// -------------------------------------------------------------------
		// Lazy data
		// -------------------------------------------------------------------

		public string AssetPath
		{
			get
			{
				if (m_assetPath == null && m_isPartOfPrefab)
					m_assetPath = AssetDatabase.GetAssetPath(
						PrefabUtility.GetCorrespondingObjectFromSource(m_gameObject));
				return m_assetPath;
			}
		}

		public string AssetGuid
		{
			get
			{
				if (m_assetGuid == null && !string.IsNullOrEmpty(AssetPath))
					m_assetGuid = AssetDatabase.AssetPathToGUID(AssetPath);
				return m_assetGuid;
			}
		}

		public OverrideInfo GetOverrideInfo()
		{
			if (!IsValid)
				throw new ArgumentNullException("GameObject mustn't be null");

			bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(m_gameObject);
			var outermostRoot = isPrefab ? PrefabUtility.GetOutermostPrefabInstanceRoot(m_gameObject) : null;
			
			return new OverrideInfo()
			{
				IsPrefab = isPrefab,

				PropertyModifications = isPrefab ? PrefabUtility.GetPropertyModifications(outermostRoot).ToList() : new List<PropertyModification>(),
				AddedGameObjects = isPrefab ? PrefabUtility.GetAddedGameObjects(outermostRoot) : new List<AddedGameObject>(),
				RemovedGameObjects = isPrefab ? PrefabUtility.GetRemovedGameObjects(outermostRoot) : new List<RemovedGameObject>(),
				AddedComponents = isPrefab ? PrefabUtility.GetAddedComponents(outermostRoot) : new List<AddedComponent>(),
				RemovedComponents = isPrefab ? PrefabUtility.GetRemovedComponents(outermostRoot) : new List<RemovedComponent>(),
				ObjectOverrides = isPrefab ? PrefabUtility.GetObjectOverrides(outermostRoot) : new List<ObjectOverride>()
			};
		}
		


		// -------------------------------------------------------------------
		// Factory
		// -------------------------------------------------------------------

		public static PrefabInfo Create(GameObject _go)
		{
			if (_go == null) return new PrefabInfo { m_gameObject = null };

			return new PrefabInfo
			{
				m_gameObject = _go,
				m_isPrefab = PrefabUtility.IsPartOfPrefabAsset(_go),
				m_isInstanceRoot = PrefabUtility.IsAnyPrefabInstanceRoot(_go) && !PrefabUtility.IsPartOfPrefabAsset(_go),
				m_isPartOfPrefab = PrefabUtility.IsPartOfPrefabInstance(_go)
				                   || PrefabUtility.IsPartOfPrefabAsset(_go),
				m_assetType = PrefabUtility.GetPrefabAssetType(_go),
				m_instanceStatus = PrefabUtility.GetPrefabInstanceStatus(_go)
			};
		}

		// -------------------------------------------------------------------
		// Convenience / validation
		// -------------------------------------------------------------------

		public void AssertIsVariantAsset(string _msg = null)
		{
			if (!IsVariantAsset)
				throw new InvalidOperationException(_msg ?? $"{m_gameObject.name} is not a variant prefab.");
		}

		public bool IsFromSameSourceAs(PrefabInfo _other) =>
			_other != null && AssetGuid == _other.AssetGuid;

		public void PingAssetInProject()
		{
			if (!string.IsNullOrEmpty(AssetPath))
				EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(AssetPath));
		}

		public void FocusInPrefabStage()
		{
			if (m_isPrefab)
			{
				AssetDatabase.OpenAsset(m_gameObject); // opens in Prefab Mode
			}
			else if (m_isInstanceRoot && !string.IsNullOrEmpty(AssetPath))
			{
				PrefabStageUtility.OpenPrefab(AssetPath); // Unity 6 API
			}
		}
	}
}