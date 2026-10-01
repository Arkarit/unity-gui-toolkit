using GuiToolkit;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Demo: the icon animates while the pointer is over it and freezes on its current frame when the pointer leaves -
/// "animate only the hovered item". The RawImage needs raycastTarget on.
/// </summary>
[RequireComponent(typeof(UiIcon3D))]
public class Icon3DHoverAnimate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public void OnPointerEnter( PointerEventData _eventData ) => GetComponent<UiIcon3D>().Mode = UiIcon3D.EMode.Animated;
	public void OnPointerExit( PointerEventData _eventData ) => GetComponent<UiIcon3D>().Mode = UiIcon3D.EMode.Static;
}
