using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class DateDisplay : UiPanel
	{
		[SerializeField] protected TMP_Text m_text;
		[SerializeField] protected SerializableDateTime m_dateTime;

		protected override void OnEnable()
		{
			base.OnEnable();
			UpdateText();
		}

		protected virtual void UpdateText()
		{
		}
	}
}