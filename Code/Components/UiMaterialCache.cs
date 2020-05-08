using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiMaterialCache : MonoBehaviour
	{
		public enum EMaterialCacheType
		{
			NoCache,
			ByString,
			Global,
		}

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

		public Material AcquireClonedMaterial( Material _originalMaterial, EMaterialCacheType _cacheType = EMaterialCacheType.Global, string _key = "" )
		{
			if (_originalMaterial == null)
				return null;

			if (_cacheType == EMaterialCacheType.NoCache)
				return _originalMaterial;

			if (_cacheType == EMaterialCacheType.Global)
				_key = "";
			else
				Debug.Assert( !string.IsNullOrEmpty(_key), "'ByString' cached material needs a string key" );

			if (!m_cacheGroups.TryGetValue(_key, out CacheGroup cacheGroup))
			{
				cacheGroup = new CacheGroup();
				m_cacheGroups.Add(_key, cacheGroup);
			}

			ClonedMaterialRecord clonedMaterialRecord;

			if (cacheGroup.m_clonedMaterialRecordByClonedMaterial.TryGetValue(_originalMaterial, out clonedMaterialRecord))
			{
				clonedMaterialRecord.m_usageCount++;
				return clonedMaterialRecord.m_clonedMaterial;
			}

			if (cacheGroup.m_clonedMaterialRecordByOriginalMaterial.TryGetValue(_originalMaterial, out clonedMaterialRecord))
			{
				clonedMaterialRecord.m_usageCount++;
				return clonedMaterialRecord.m_clonedMaterial;
			}

			clonedMaterialRecord = new ClonedMaterialRecord();
			clonedMaterialRecord.m_originalMaterial = _originalMaterial;
			clonedMaterialRecord.m_clonedMaterial = Instantiate(_originalMaterial);
			clonedMaterialRecord.m_usageCount = 1;
			cacheGroup.m_clonedMaterialRecordByOriginalMaterial.Add(_originalMaterial, clonedMaterialRecord);
			cacheGroup.m_clonedMaterialRecordByClonedMaterial.Add(clonedMaterialRecord.m_clonedMaterial, clonedMaterialRecord);
			return clonedMaterialRecord.m_clonedMaterial;
		}

		public Material ReleaseClonedMaterial( Material _clonedMaterial, EMaterialCacheType _cacheType = EMaterialCacheType.Global, string _key = "" )
		{
			if (_clonedMaterial == null)
				return null;

			if (_cacheType == EMaterialCacheType.NoCache)
				return _clonedMaterial;

			if (_cacheType == EMaterialCacheType.Global)
				_key = "";
			else
				Debug.Assert( !string.IsNullOrEmpty(_key), "'ByString' cached material needs a string key" );

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
	}
}