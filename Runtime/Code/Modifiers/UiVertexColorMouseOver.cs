using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	public class UiVertexColorMouseOver : UiGradientBase, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] protected Color m_color;
		[SerializeField] protected Color m_hoverColor;
		
		private bool m_hover;

		protected override Color GetColor(Vector2 _) => m_hover ? m_hoverColor : m_color;
		protected override bool NeedsMeshBounds => false;

		public void OnPointerEnter(PointerEventData eventData)
		{
			m_hover = true;
			SetDirty();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			m_hover = false;
			SetDirty();
		}
	}
}
