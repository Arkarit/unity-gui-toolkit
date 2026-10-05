using GuiToolkit;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Demo: the icon animates while the pointer is over it - "animate only the hovered item". What happens when the pointer
/// leaves is a choice:
/// <list type="bullet">
/// <item><b>Freeze</b>: it stands still on the frame it has reached.</item>
/// <item><b>Rewind</b>: it runs back to the first frame of its loop, faster than it played (<see cref="UiIcon3D.RewindSpeed"/>),
/// and stands still there. Hovering in again on the way carries on forward from where it is, so a nervous pointer makes
/// it swing instead of jump.</item>
/// </list>
/// The RawImage needs raycastTarget on.
///
/// Hover is only one trigger. <see cref="Animate"/> is the whole interface: set it from anywhere - a selection, a "new" badge,
/// a tab being the active one - and the icon does the same.
/// </summary>
[RequireComponent(typeof(UiIcon3D))]
public class Icon3DHoverAnimate : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public enum EOnExit
	{
		Freeze,
		Rewind,
	}

	[Tooltip("What the icon does when Animate turns false")]
	[SerializeField] private EOnExit m_onExit = EOnExit.Freeze;

	public EOnExit OnExit
	{
		get => m_onExit;
		set => m_onExit = value;
	}

	/// <summary>True: the animation plays. False: it freezes or rewinds, depending on <see cref="OnExit"/>.</summary>
	public bool Animate
	{
		get => GetComponent<UiIcon3D>().Mode == UiIcon3D.EMode.Animated;
		set
		{
			var icon = GetComponent<UiIcon3D>();
			icon.Mode = value
				? UiIcon3D.EMode.Animated
				: m_onExit == EOnExit.Rewind ? UiIcon3D.EMode.Rewinding : UiIcon3D.EMode.Static;
		}
	}

	public void OnPointerEnter( PointerEventData _eventData ) => Animate = true;
	public void OnPointerExit( PointerEventData _eventData ) => Animate = false;
}
