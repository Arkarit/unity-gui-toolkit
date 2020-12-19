using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class UiSettingsEntryBase : UiThing
	{
		[SerializeField]
		protected TMP_Text m_text;
	}
}