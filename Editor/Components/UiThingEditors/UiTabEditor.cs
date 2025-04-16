using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiTab))]
	public class UiTabEditor : UiThingEditor
	{
		private static readonly HashSet<string> m_excludedProperties = new()
		{
			"m_uiImage"
		};

		protected override HashSet<string> excludedProperties => m_excludedProperties;
	}
}