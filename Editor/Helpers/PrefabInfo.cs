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
		public class COverrideInfo
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

		// -------------------------------------------------------------------
		// Info getters
		// -------------------------------------------------------------------

		public bool IsValid => m_gameObject != null;
		public bool IsVariantAsset => m_assetType == PrefabAssetType.Variant;
		public bool IsModelAsset => m_assetType == PrefabAssetType.Model;

		public bool IsDirty
		{
			get
			{
				if (m_gameObject == null)
					return false;

				return EditorUtility.IsDirty(m_gameObject);
			}
		}

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

		public COverrideInfo OverrideInfo
		{
			get
			{
				bool isPrefab = IsValid && PrefabUtility.IsPartOfAnyPrefab(m_gameObject);
				var outermostRoot = isPrefab ? PrefabUtility.GetOutermostPrefabInstanceRoot(m_gameObject) : null;

				return new COverrideInfo()
				{
					IsPrefab = isPrefab,

					PropertyModifications =
						isPrefab
							? PrefabUtility.GetPropertyModifications(outermostRoot).ToList()
							: new List<PropertyModification>(),
					AddedGameObjects =
						isPrefab ? PrefabUtility.GetAddedGameObjects(outermostRoot) : new List<AddedGameObject>(),
					RemovedGameObjects = isPrefab
						? PrefabUtility.GetRemovedGameObjects(outermostRoot)
						: new List<RemovedGameObject>(),
					AddedComponents =
						isPrefab ? PrefabUtility.GetAddedComponents(outermostRoot) : new List<AddedComponent>(),
					RemovedComponents =
						isPrefab ? PrefabUtility.GetRemovedComponents(outermostRoot) : new List<RemovedComponent>(),
					ObjectOverrides =
						isPrefab ? PrefabUtility.GetObjectOverrides(outermostRoot) : new List<ObjectOverride>()
				};
			}
		}

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

		public void Modify<T>( T _target, Action<T> _edit, string _label = "Prefab Change" ) where T : UnityEngine.Object
		{
			if (!IsValid)
				throw new NullReferenceException($"{nameof(IsValid)} is false ({nameof(GameObject)} is null); Only existing objects can be modified.");

			if (_target == null) 
				throw new ArgumentNullException(nameof(_target));
			if (_edit == null) 
				throw new ArgumentNullException(nameof(_edit));

			Undo.RecordObject(_target, _label);
			_edit(_target);
			EditorUtility.SetDirty(_target);
		}

		public void Modify<T>(Action<T> _edit, bool _inChildren = true, string _label = "Prefab Change" ) where T : UnityEngine.Component
		{
			if (!IsValid)
				throw new NullReferenceException($"{nameof(IsValid)} is false ({nameof(GameObject)} is null); Only existing objects can be modified.");

			if (_edit == null) 
				throw new ArgumentNullException(nameof(_edit));

			T target = _inChildren ? GameObject.GetComponentInChildren<T>() : GameObject.GetComponent<T>();
			if (target == null)
				throw new InvalidOperationException($"Missing required component '{typeof(T).Name}' on GameObject '{GameObject.name}'.");

			Undo.RecordObject(target, _label);
			_edit(target);
			EditorUtility.SetDirty(target);
//			EditorUtility.SetDirty(GameObject);
		}

		public override string ToString()
		{
			if (!IsValid)
				return "<Invalid PrefabInfo>";

			var parts = new List<string>
			{
				$"{GameObject.name} ({GameObject.GetInstanceID()})",
				$"IsPrefab={IsPrefab}",
				$"IsInstanceRoot={IsInstanceRoot}",
				$"IsPartOfPrefab={IsPartOfPrefab}",
				$"IsConnected={IsConnected}",
				$"IsDisconnected={IsDisconnected}",
				$"AssetType={AssetType}",
				$"InstanceStatus={InstanceStatus}",
				$"IsVariantAsset={IsVariantAsset}",
				$"IsModelAsset={IsModelAsset}",
				$"IsMissingAsset={IsMissingAsset}",
				$"IsDirty={IsDirty}",
				$"HasUnsavedChanges={HasUnsavedChanges}",
				$"AssetPath='{AssetPath}'",
				$"AssetGuid='{AssetGuid}'"
			};

			return string.Join("\n", parts);
		}

	}
}