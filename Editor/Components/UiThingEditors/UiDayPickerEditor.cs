using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiDayPicker))]
	public class UiDayPickerEditor : UiThingEditor
	{
		private static readonly HashSet<string> m_excludedProperties = new()
		{
			"m_uiImage"
		};

		protected override HashSet<string> excludedProperties => m_excludedProperties;
	}
}