using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiKeyBindingsList : UiPanel
	{
		[SerializeField]
		protected UiKeyBindingsEntry m_entryPrefab;

		[SerializeField]
		protected RectTransform m_entriesParent;

		[SerializeField]
		protected ScrollRect m_scrollRect;

		[System.Serializable]
		public class CEvRefreshList : UnityEvent {}
		public static CEvRefreshList  EvRefreshList = new CEvRefreshList();


		protected override void OnEnable()
		{
			base.OnEnable();

			FillList();
			if (m_scrollRect)
				m_scrollRect.ScrollToTop(this);
		}

		protected override void OnDisable()
		{
			ClearList();
			base.OnDisable();
		}

		protected override void AddEventListeners()
		{
			base.AddEventListeners();
			EvRefreshList.AddListener(FillList);
		}

		protected override void RemoveEventListeners()
		{
			EvRefreshList.RemoveListener(FillList);
			base.RemoveEventListeners();
		}

		private void ClearList()
		{
			m_entriesParent.PoolDestroyChildren<UiKeyBindingsEntry>();
		}

		private void FillList()
		{
			ClearList();

			KeyBindings keyBindings = UiMain.Instance.KeyBindings;
			foreach( var kv in keyBindings)
			{
				var entry = m_entryPrefab.PoolInstantiate();
				entry.transform.SetParent(m_entriesParent, false);
				entry.Initialize(kv);
			}
		}
	}
}