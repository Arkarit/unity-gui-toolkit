using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiAbstractStyleBase), true)]
	public class UiAbstractStyleBaseDrawer : AbstractPropertyDrawer
	{
		private const float GapAfterTitle = 8;
		private const float GapAfterDisplay = 8;
		private const float LineEndGapHor = 20;

		protected override void OnInspectorGUI()
		{
			var currentStyle = Property.boxedValue as UiAbstractStyleBase;
			if (currentStyle != null)
			{
				LabelField(UiStyleUtility.GetName(currentStyle.SupportedMonoBehaviourType, currentStyle.Name));
/*
				var lineRect = new Rect(
					currentRect.x, 
					currentRect.y - 7,
					EditorGUIUtility.labelWidth - LineEndGapHor,
					1
				);

				EditorGUI.DrawRect(lineRect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
*/
			}

			foreach (var childProperty in ChildProperties)
			{
				if (childProperty.name == "m_key")
					continue;

				PropertyField(childProperty);
			}
		}
	}
}