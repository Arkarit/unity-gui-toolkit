using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class UiResolutionDependentSwitcher : MonoBehaviour
	{
		[Header("Landscape")]
		[SerializeField] protected UiHorizontalOrVerticalLayoutGroup[] m_horizontalInLandscape = new UiHorizontalOrVerticalLayoutGroup[0];

		[Header("Portrait")]
		[SerializeField] protected UiHorizontalOrVerticalLayoutGroup[] m_verticalInLandscape = new UiHorizontalOrVerticalLayoutGroup[0];

		protected virtual void Start()
		{
			UpdateElements();
		}

		protected virtual void OnRectTransformDimensionsChange()
		{
			UpdateElements();
		}

		protected virtual void UpdateElements()
		{
			bool isLandscape = Screen.width >= Screen.height;

			foreach( var layout in m_horizontalInLandscape )
				if (layout != null)
					layout.Vertical = !isLandscape;

			foreach( var layout in m_verticalInLandscape )
				if (layout != null)
					layout.Vertical = isLandscape;
		}

	}
}