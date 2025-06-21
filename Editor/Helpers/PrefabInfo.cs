using UnityEditor;
using UnityEditor.SceneManagement;              // ObjectOverride + PrefabStageUtility
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Provides a complete, editor-only snapshot of a prefab-related <see cref="GameObject"/>.
	/// All relevant prefab data are resolved eagerly (except expensive override lists, which are
	/// fetched lazily).  The class is intended for editor tooling where clarity and robustness
	/// outweigh raw performance.
	/// </summary>
	[Serializable]
	public class PrefabInfo
	{
		// -------------------------------------------------------------------
		// Serialized core data
		// -------------------------------------------------------------------

		public GameObject m_gameObject;
		public bool m_isPrefab;
		public bool m_isInstanceRoot;
		public bool m_isPartOfPrefab;
		public PrefabAssetType m_assetType;
		public PrefabInstanceStatus m_instanceStatus;

		// -------------------------------------------------------------------
		// Private, non-serialized caches
		// -------------------------------------------------------------------

		[NonSerialized] private string m_assetPath;
		[NonSerialized] private string m_assetGuid;
		[NonSerialized] private List<ObjectOverride> m_overrides;

		// -------------------------------------------------------------------
		// Quick checks
		// -------------------------------------------------------------------

		public bool IsValid => m_gameObject != null;
		public bool IsVariantAsset => m_assetType == PrefabAssetType.Variant;
		public bool IsModelAsset => m_assetType == PrefabAssetType.Model;

		public bool HasOverrides => m_instanceStatus == PrefabInstanceStatus.Connected
		                            && PrefabUtility.HasPrefabInstanceAnyOverrides(m_gameObject, false);

		public bool IsDisconnected => m_instanceStatus == PrefabInstanceStatus.Disconnected;

		public bool IsMissingAsset => m_assetType == PrefabAssetType.MissingAsset
		                              || m_instanceStatus == PrefabInstanceStatus.MissingAsset;

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

		public IReadOnlyList<ObjectOverride> Overrides
		{
			get
			{
				if (m_overrides == null && HasOverrides)
					m_overrides = PrefabUtility.GetObjectOverrides(m_gameObject);
				return m_overrides;
			}
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
				m_isInstanceRoot = PrefabUtility.IsAnyPrefabInstanceRoot(_go),
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