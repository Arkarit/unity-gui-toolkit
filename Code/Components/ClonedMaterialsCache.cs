using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	/// \brief maintain cloned materials for sharing them between multiple graphic's / renderer's
	/// 
	/// Very often, it is necessary to clone a material.
	/// The naive approach to just clone and store a material leads to
	/// broken batching. <BR>
	/// ClonedMaterialsCache maintains a cache of cloned materials, indexed by a) original material and b) a string key.
	/// Since its main purpose is to be globally shared, it is implemented as a singleton.
	/// It is implemented as a non-serialized class; thus components using it need to update it manually on load
	/// via HingeClonedMaterial(). See MaterialCloner for usage.
	/// \attention If you are actively working on a class that uses ClonedMaterialsCache, be aware that a recompile invalidates the cache, which might lead to unpredictable results. To overcome this, reload the scene.

	public class ClonedMaterialsCache : MonoBehaviour
	{
		[System.Serializable]
		/// Event when cloned material is about to be replaced:
		/// 1. replaced original material, about to be removed
		/// 2. replacement original material
		/// 3. replaced material, about to be destroyed
		/// 4. replacement material
		public class CEvMaterialReplaced : UnityEvent<Material,Material,Material, Material> {}
		public CEvMaterialReplaced EvMaterialReplaced = new CEvMaterialReplaced();

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

		/// Instance getter
		public static ClonedMaterialsCache Instance => UiMain.Instance.ClonedMaterialsCache;

		/// \brief Get a cloned material from an original material.
		/// Either a new cloned material is created, or an already previously cloned material is returned.
		/// Note that it's mandatory to call ReleaseClonedMaterial(), if you want to get rid of the cloned material.
		/// \param[in] _originalMaterial The original material
		/// \param[in] _key optional key to separate material sharing ("" is global)
		/// \return Cloned Material
		public Material AcquireClonedMaterial( Material _originalMaterial, string _key = "" )
		{
			return InsertMaterial(_originalMaterial, null, _key);
		}

		/// \brief Release a cloned material
		/// Releases a cloned material. ClonedMaterialsCache holds an usage counter; if the cloned material is not used<BR>
		/// by any instance anymore, it is destroyed.
		/// \param[in] _clonedMaterial The cloned material
		/// \param[in] _key optional key to separate material sharing ("" is global)
		/// \return Original Material
		public Material ReleaseClonedMaterial( Material _clonedMaterial, string _key = "" )
		{
			return ReleaseMaterial(_clonedMaterial, _key, true);
		}

		/// \brief Insert an already existing cloned material.
		/// The provided cloned material is inserted into the cache. <BR> 
		/// Note that it has to be the same cloned material instance,
		/// if multiple callers use the same _originalMaterial and _key!
		/// \param[in] _originalMaterial The original material
		/// \param[in] _clonedMaterial The cloned material
		/// \param[in] _key optional key to separate material sharing ("" is global)
		/// \return Cloned Material
		public Material HingeClonedMaterial( Material _originalMaterial, Material _clonedMaterial, string _key = "" )
		{
			return InsertMaterial(_originalMaterial, _clonedMaterial, _key);
		}

		/// \brief Unhinges a cloned material
		/// Releases a cloned material. This is quite similar to ReleaseClonedMaterial();<BR>
		/// the difference is, that the material is not destroyed if the usage counter goes to zero.
		/// \param[in] _clonedMaterial The cloned material
		/// \param[in] _key optional key to separate material sharing ("" is global)
		/// \return Original Material
		public Material UnhingeClonedMaterial( Material _clonedMaterial, string _key = "" )
		{
			return ReleaseMaterial(_clonedMaterial, _key, false);
		}

		public bool ReplaceMaterial( Material _oldOriginalMaterial, Material _newOriginalMaterial, string _key = "" )
		{
			if (_oldOriginalMaterial == null || _newOriginalMaterial == null)
				return false;

			if (!m_cacheGroups.TryGetValue(_key, out CacheGroup cacheGroup))
				return false;

			if (!cacheGroup.m_clonedMaterialRecordByOriginalMaterial.TryGetValue(_oldOriginalMaterial, out ClonedMaterialRecord clonedMaterialRecord))
				return false;

			Material newClonedMaterial = Instantiate(_newOriginalMaterial);
			EvMaterialReplaced.Invoke( _oldOriginalMaterial, _newOriginalMaterial, clonedMaterialRecord.m_clonedMaterial, newClonedMaterial);

			Debug.Assert(cacheGroup.m_clonedMaterialRecordByClonedMaterial.ContainsKey(clonedMaterialRecord.m_clonedMaterial));
			Debug.Assert(cacheGroup.m_clonedMaterialRecordByOriginalMaterial.ContainsKey(clonedMaterialRecord.m_originalMaterial));


			cacheGroup.m_clonedMaterialRecordByOriginalMaterial.Remove(clonedMaterialRecord.m_originalMaterial);
			cacheGroup.m_clonedMaterialRecordByClonedMaterial.Remove(clonedMaterialRecord.m_clonedMaterial);


			clonedMaterialRecord.m_clonedMaterial.Destroy();
			clonedMaterialRecord.m_clonedMaterial = newClonedMaterial;
			clonedMaterialRecord.m_originalMaterial = _newOriginalMaterial;

			cacheGroup.m_clonedMaterialRecordByOriginalMaterial.Add(_newOriginalMaterial, clonedMaterialRecord);
			cacheGroup.m_clonedMaterialRecordByClonedMaterial.Add(newClonedMaterial, clonedMaterialRecord);

			return true;
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

		private Material ReleaseMaterial( Material _clonedMaterial, string _key, bool _destroyOnUsageCountZero )
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
					if (_destroyOnUsageCountZero)
						clonedMaterialRecord.m_clonedMaterial.Destroy();
				}

				return clonedMaterialRecord.m_originalMaterial;
			}

			return _clonedMaterial;
		}


	}
}