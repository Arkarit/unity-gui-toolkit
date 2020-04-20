using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiKeyBindingsList : UiPanel
	{
		[SerializeField]
		protected UiKeyBindingsEntry m_entryPrefab;

		[SerializeField]
		protected Transform m_entriesParent;

		protected override void OnEnable()
		{
			base.OnEnable();

UiMain.Instance.KeyBindings.Initialize(new List<KeyValuePair<string, KeyCode>>()
{
new KeyValuePair<string, KeyCode>("Forwards", KeyCode.W),
new KeyValuePair<string, KeyCode>("Left", KeyCode.A),
new KeyValuePair<string, KeyCode>("Right", KeyCode.S),
new KeyValuePair<string, KeyCode>("Backwards", KeyCode.D)
});

			FillList();
		}

		protected override void OnDisable()
		{
			ClearList();
			base.OnDisable();
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