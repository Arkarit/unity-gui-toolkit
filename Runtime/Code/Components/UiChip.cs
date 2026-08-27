using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A compact, colour-coded label with an optional leading icon - a division name, a tag, a state word.
	///
	/// The chip owns no look of its own: size and colour role come from the style, so the same prefab is a
	/// list-row chip or an inline one depending on which style it carries ("Chip/Default", "Chip/Small").
	/// Nor does it care what its background IS - any Graphic will do, rounded or nine-sliced or plain.
	/// Display only unless it is made clickable, and then it says so: a chip that takes no clicks does not
	/// raycast either, so the tap reaches the card or row underneath instead of dying on the chip.
	/// </summary>
	public class UiChip : UiTextContainer, IPointerClickHandler
	{
		[Header("Chip")]

		[Tooltip("The chip's background graphic. Carries its colour role through its style, and is the "
			+ "click target when the chip is clickable.")]
		// Graphic, not Image and not UiRoundedImage: raycastTarget is all this class ever touches, and that
		// is declared on Graphic. Asking for more would rule out backgrounds that would do perfectly well -
		// a plain Image for a nine-sliced chip, or anything else that draws.
		[SerializeField][Mandatory] protected Graphic m_background;

		[Tooltip("Optional icon in front of the label. Without a sprite its GameObject is switched off, so "
			+ "a layout group does not reserve the space for it.")]
		[SerializeField][Optional] protected Image m_icon;

		[Tooltip("Whether a tap on the chip is taken. Off means the chip does not raycast at all, so the "
			+ "tap reaches whatever lies behind it. AddClickListener() switches this on by itself, and so "
			+ "does wiring OnClick in the inspector.")]
		[SerializeField] protected bool m_clickable;

		[Tooltip("Cut an overlong label with an ellipsis instead of letting it push the row apart. Switch "
			+ "off only if the style already sets an overflow mode of its own.")]
		[SerializeField] protected bool m_truncateOverlongText = true;

		[Tooltip("Invoked on a tap, if the chip is clickable.")]
		public CEvent OnClick = new();

		/// <summary>
		/// The label in full, even when the chip is too narrow to show all of it.
		///
		/// Not the same thing as <see cref="UiTextContainer.Text"/>: that one round-trips the localization
		/// KEY for a localized label, which is what an author needs. This is the string a reader would see
		/// if there were room - truncation is the text component's doing and never shortens the string.
		/// </summary>
		public string FullText => m_tmpText != null ? m_tmpText.text
			: m_text != null ? m_text.text
			: "";

		/// <summary>
		/// Icon in front of the label, or null for none. Setting null switches the icon object off rather
		/// than leaving an empty slot behind, so the label moves over.
		/// </summary>
		public Sprite Icon
		{
			get => m_icon != null ? m_icon.sprite : null;
			set
			{
				if (m_icon == null)
				{
					if (value != null)
					{
						UiLog.LogError($"'{gameObject.name}' has no icon image, cannot set an icon sprite.",
							this);
					}
					return;
				}

				m_icon.sprite = value;
				m_icon.gameObject.SetActive(value != null);
			}
		}

		/// <summary>
		/// Whether a tap on the chip is taken. See the field tooltip for what "no" means for the tap.
		/// </summary>
		public bool Clickable
		{
			get => m_clickable;
			set
			{
				m_clickable = value;
				ApplyClickable();
			}
		}

		public Graphic Background => m_background;

		/// <summary>
		/// Adds a click listener and makes the chip clickable in the same breath.
		///
		/// The reason to prefer this over OnClick.AddListener(): a chip only raycasts while it is clickable,
		/// so a listener added the other way would never be called and the cause would be invisible.
		/// </summary>
		public void AddClickListener( UnityAction _call )
		{
			if (_call == null)
				return;

			OnClick.AddListener(_call);
			Clickable = true;
		}

		public void RemoveClickListener( UnityAction _call )
		{
			if (_call == null)
				return;

			OnClick.RemoveListener(_call);
		}

		public void OnPointerClick( PointerEventData _eventData )
		{
			// The raycast target is already off when the chip is not clickable; this catches the case where
			// something else inside it - an icon, a stray graphic - is the one that was hit.
			if (!m_clickable)
				return;

			OnClick.Invoke();
		}

		protected override void Init()
		{
			base.Init();
			ApplyClickable();
			ApplyTruncation();

			if (m_icon != null && m_icon.sprite == null)
				m_icon.gameObject.SetActive(false);
		}

		private void ApplyClickable()
		{
			if (m_background == null)
				return;

			m_background.raycastTarget = m_clickable;
		}

		private void ApplyTruncation()
		{
			if (!m_truncateOverlongText || m_tmpText == null)
				return;

			// Only the one mode that means "grow past the rect" is replaced. Anything an author or a style
			// chose deliberately - Truncate, Masking, a linked overflow - is left alone.
			if (m_tmpText.overflowMode == TextOverflowModes.Overflow)
				m_tmpText.overflowMode = TextOverflowModes.Ellipsis;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			// Wiring OnClick in the inspector is a statement of intent; a chip that then swallowed nothing
			// because a checkbox was left unticked would read as a broken chip.
			if (OnClick != null && OnClick.GetPersistentEventCount() > 0)
				m_clickable = true;

			ApplyClickable();
		}
#endif
	}
}
