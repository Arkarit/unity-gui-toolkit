using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiAbstractStyleBase), true)]
	public class UiAbstractStyleBaseDrawer : AbstractPropertyDrawer<UiAbstractStyleBase>
	{
		private const float LineEndGapHor = 20;
		private const float LineGapVert = 4;
		private const float EndGap = 10;

		protected override void OnInspectorGUI()
		{
			var currentStyle = Property.boxedValue as UiAbstractStyleBase;
			if (currentStyle != null)
			{
				LabelField(UiStyleUtility.GetName(currentStyle.SupportedMonoBehaviourType, currentStyle.Name));
				Line(LineGapVert, EditorGUIUtility.labelWidth - LineEndGapHor);
			}

			Indent(() =>
			{
				foreach (var childProperty in ChildProperties)
				{
					if (childProperty.name == "m_key")
						continue;

					PropertyField(childProperty);
				}
			});

			Space(EndGap);
		}
	}
}