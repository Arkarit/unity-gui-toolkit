using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiMaterialCache : MonoBehaviour
	{
		private class ClonedMaterialRecord
		{
			public Material m_originalMaterial;
			public Material m_clonedMaterial;
			public int m_usageCount;
		}

		private class CacheGroup
		{
			public readonly Dictionary<Material, ClonedMaterialRecord> m_clonedMaterialRecordByOriginalMaterial = new Dictionary<Material, ClonedMaterialRecord>();
			public readonly Dictionary<Material, ClonedMaterialRecord> m_clonedMaterialRecordByClonedMaterial = new Dictionary<Material, ClonedMaterialRecord>();
		}

		private readonly Dictionary<string, CacheGroup> m_cacheGroups = new Dictionary<string, CacheGroup>();

		public static UiMaterialCache Instance => UiMain.Instance.UiMaterialCache;

		public Material AcquireClonedMaterial( Material _originalMaterial, string _key = "" )
		{
			return InsertMaterial(_originalMaterial, null, _key);
		}


		public Material InsertClonedMaterial( Material _originalMaterial, Material _clonedMaterial, string _key = null )
		{
			return InsertMaterial(_originalMaterial, _clonedMaterial, _key);
		}

		public Material ReleaseClonedMaterial( Material _clonedMaterial, string _key = "" )
		{
			if (_clonedMaterial == null)
				return null;

			if (!m_cacheGroups.TryGetValue(_key, out CacheGroup cacheGroup))
			{
				return _clonedMaterial;
			}

			if (cacheGroup.m_clonedMaterialRecordByClonedMaterial.TryGetValue(_clonedMaterial, out ClonedMaterialRecord clonedMaterialRecord))
			{
				clonedMaterialRecord.m_usageCount--;

				if (clonedMaterialRecord.m_usageCount == 0)
				{
					cacheGroup.m_clonedMaterialRecordByClonedMaterial.Remove(clonedMaterialRecord.m_clonedMaterial);
					cacheGroup.m_clonedMaterialRecordByOriginalMaterial.Remove(clonedMaterialRecord.m_originalMaterial);
					clonedMaterialRecord.m_clonedMaterial.Destroy();
				}

				return clonedMaterialRecord.m_originalMaterial;
			}

			return _clonedMaterial;
		}

		private Material InsertMaterial( Material _originalMaterial, Material _clonedMaterial, string _key )
		{
			if (_originalMaterial == null)
				return null;

			if (!m_cacheGroups.TryGetValue(_key, out CacheGroup cacheGroup))
			{
				cacheGroup = new CacheGroup();
				m_cacheGroups.Add(_key, cacheGroup);
			}

			ClonedMaterialRecord clonedMaterialRecord;

			if (cacheGroup.m_clonedMaterialRecordByClonedMaterial.TryGetValue(_originalMaterial, out clonedMaterialRecord))
			{
				clonedMaterialRecord.m_usageCount++;
				Debug.Assert(_clonedMaterial == null || _clonedMaterial == clonedMaterialRecord.m_clonedMaterial);
				return clonedMaterialRecord.m_clonedMaterial;
			}

			if (cacheGroup.m_clonedMaterialRecordByOriginalMaterial.TryGetValue(_originalMaterial, out clonedMaterialRecord))
			{
				clonedMaterialRecord.m_usageCount++;
				Debug.Assert(_clonedMaterial == null || _clonedMaterial == clonedMaterialRecord.m_clonedMaterial);
				return clonedMaterialRecord.m_clonedMaterial;
			}

			clonedMaterialRecord = new ClonedMaterialRecord();
			clonedMaterialRecord.m_originalMaterial = _originalMaterial;
			clonedMaterialRecord.m_clonedMaterial = _clonedMaterial != null ? _clonedMaterial : Instantiate(_originalMaterial);
			clonedMaterialRecord.m_usageCount = 1;
			cacheGroup.m_clonedMaterialRecordByOriginalMaterial.Add(_originalMaterial, clonedMaterialRecord);
			cacheGroup.m_clonedMaterialRecordByClonedMaterial.Add(clonedMaterialRecord.m_clonedMaterial, clonedMaterialRecord);
			return clonedMaterialRecord.m_clonedMaterial;
		}

	}
}