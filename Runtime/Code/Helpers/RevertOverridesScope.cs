using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// <summary>
	/// When writing editor code which modify game objects and components in the currently edited scene,
	/// it often comes to unwanted property overrides of prefabs. This scope class can avoid this.
	/// 
	/// Consider in an editor code you want to render the currently selected camera in isometric mode into a temporary render texture.
	/// Simple to do: create a temporary render texture and assign it to the camera, change camera projection, render, and afterwards
	/// restore the previous camera settings / render texture.
	/// Now if the camera is part of a prefab instance, you have created some unwanted overrides, especially the render texture and the camera projection.
	///
	/// RevertOverridesScope helps to avoid this; it saves all property modifications for one or more specific objects at beginning of scope
	/// and restores it at end of scope, omitting all changes in between.
	/// </summary>
	public class RevertOverridesScope : IDisposable
	{
#if UNITY_EDITOR
		private static readonly Stack<List<ModificationsRecord>> s_poolListModificationsRecord = new();
		private static readonly Stack<ModificationsRecord> s_poolModificationsRecord = new();

		private List<ModificationsRecord> m_recordList = null;

		private class ModificationsRecord
		{
			public Object Object;
			public GameObject PrefabInstanceRoot;
			public PropertyModification[] Modifications;
			public bool IsDirty;
		}
#endif

		public RevertOverridesScope(Object _obj)
		{
#if UNITY_EDITOR
			m_recordList = AddRecordIfNecessary(_obj);
#endif
		}

		public RevertOverridesScope(IEnumerable<Object> _objects)
		{
#if UNITY_EDITOR
			foreach (Object obj in _objects)
				m_recordList = AddRecordIfNecessary(obj, m_recordList);
#endif
		}

		public RevertOverridesScope(params Object[] _objects)
		{
#if UNITY_EDITOR
			foreach (Object obj in _objects)
				m_recordList = AddRecordIfNecessary(obj, m_recordList);
#endif
		}

		public void Dispose()
		{
#if UNITY_EDITOR
			if (m_recordList == null)
				return;

			foreach (var record in m_recordList)
			{
				PrefabUtility.SetPropertyModifications(record.PrefabInstanceRoot, record.Modifications);
				if (!record.IsDirty)
					EditorUtility.ClearDirty(record.Object);
			}

			ReleaseModificationsRecordList(m_recordList);
			m_recordList = null;
#endif
		}

#if UNITY_EDITOR
		private List<ModificationsRecord> AddRecordIfNecessary(Object _obj, List<ModificationsRecord> _list = null)
		{
			if (Application.isPlaying)
				return null;

			if (_obj == null)
				return null;

			var prefabInstanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(_obj);
			if (prefabInstanceRoot == null)
				return null;

			if (_list == null)
				_list = GetModificationsRecordList();

			var record = GetModificationsRecord();
			record.Object = _obj;
			record.Modifications = PrefabUtility.GetPropertyModifications(prefabInstanceRoot);
			record.IsDirty = EditorUtility.IsDirty(_obj);
			record.PrefabInstanceRoot = prefabInstanceRoot;
			_list.Add(record);

			return _list;
		}

		private ModificationsRecord GetModificationsRecord()
		{
			if (s_poolModificationsRecord.Count == 0)
				return new ModificationsRecord();

			return s_poolModificationsRecord.Pop();
		}

		private void ReleaseModificationsRecord(ModificationsRecord _record)
		{
			s_poolModificationsRecord.Push(_record);
		}

		private List<ModificationsRecord> GetModificationsRecordList()
		{
			if (s_poolListModificationsRecord.Count == 0)
				return new List<ModificationsRecord>();

			return s_poolListModificationsRecord.Pop();
		}

		private void ReleaseModificationsRecordList(List<ModificationsRecord> _list)
		{
			foreach (var record in _list)
			{
				ReleaseModificationsRecord(record);
			}

			_list.Clear();
			s_poolListModificationsRecord.Push(_list);
		}
#endif
	}
}