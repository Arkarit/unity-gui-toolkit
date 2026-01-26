using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[CreateAssetMenu(fileName = nameof(UiMainStyleConfig), menuName = StringConstants.CREATE_MAIN_STYLE_CONFIG)]
	public class UiMainStyleConfig : UiStyleConfig
	{
	}
}
