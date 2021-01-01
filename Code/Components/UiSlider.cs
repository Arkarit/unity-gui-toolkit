using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Slider;

namespace GuiToolkit
{
	public class UiSlider : UiThing
	{
		private const string LAYOUT_TOOLTIP = "The first icon can be set small with FirstIconSmall setter (Useful e.g. for volume sliders.) Set these fields if you want to use this.";
		[Header("Mandatory members")]
		[Tooltip("Unity slider component (mandatory)")]
		[SerializeField] protected Slider m_slider;

		[Header("Buttons/toggles to set the slider to 0/1 on click")]
		[Tooltip("This toggle can set the slider to 0 and disable slider elements")]
		[SerializeField] protected UiToggle m_optionalOnOffToggle;
		[Tooltip("Slider elements to disable when toggled off")]
		[SerializeField] protected UiImage[] m_optionalUiImagesToDisableWhenOff;

		[Tooltip("Button to set the slider to 0")]
		[SerializeField] protected UiButton m_optionalNoVolumeButton;
		[Tooltip("Button to set the slider to 1")]
		[SerializeField] protected UiButton m_optionalFullVolumeButton;

		[Header("Icons")]
		[Tooltip("Icon images. Needs to be set if 'Icons' getter/setter is used.")]
		[SerializeField] protected Image[] m_optionalIconImages;

		[Space]
		[Tooltip(LAYOUT_TOOLTIP)]
		[SerializeField] protected LayoutElement m_optionalFirstIconLayoutElement;
		[Tooltip(LAYOUT_TOOLTIP)]
		[SerializeField] protected LayoutElement m_optionalFirstSpacerLayoutElement;
		[Tooltip(LAYOUT_TOOLTIP)]
		[SerializeField] protected float m_optionalFirstIconSizeWhenSmall = 35;
		[Tooltip(LAYOUT_TOOLTIP)]
		[SerializeField] protected float m_optionalFirstIconSizeWhenBig = 80;
		[Tooltip(LAYOUT_TOOLTIP)]
		[SerializeField] protected float m_optionalFirstSpacerSizeWhenSmall = 40;
		[Tooltip(LAYOUT_TOOLTIP)]
		[SerializeField] protected float m_optionalFirstSpacerSizeWhenBig = 30;
		[Tooltip(LAYOUT_TOOLTIP)]
		[SerializeField] protected bool m_firstIconSmall;


		private float m_savedSliderVal;
		private List<string> m_icons;

		public SliderEvent OnValueChanged
		{
			get => m_slider.onValueChanged;
			set => m_slider.onValueChanged = value;
		}

		public float Value
		{
			get => m_slider.value;
			set => m_slider.value = value;
		}

		public bool FirstIconSmall
		{
			get => m_firstIconSmall;
			set
			{
				m_firstIconSmall = value;
				if (m_optionalFirstIconLayoutElement == null || m_optionalFirstSpacerLayoutElement == null)
				{
					Debug.LogError("Attempt to set first icon big/small, but necessary members were not set");
					return;
				}
				if (m_slider.direction == Direction.LeftToRight || m_slider.direction == Direction.RightToLeft)
				{
					m_optionalFirstIconLayoutElement.minWidth = m_firstIconSmall ? m_optionalFirstIconSizeWhenSmall : m_optionalFirstIconSizeWhenBig;
					m_optionalFirstSpacerLayoutElement.minWidth = m_firstIconSmall ? m_optionalFirstSpacerSizeWhenSmall : m_optionalFirstSpacerSizeWhenBig;
				}
				else
				{
					m_optionalFirstIconLayoutElement.minHeight = m_firstIconSmall ? m_optionalFirstIconSizeWhenSmall : m_optionalFirstIconSizeWhenBig;
					m_optionalFirstSpacerLayoutElement.minHeight = m_firstIconSmall ? m_optionalFirstSpacerSizeWhenSmall : m_optionalFirstSpacerSizeWhenBig;
				}
			}
		}

		public List<string> Icons
		{
			get => m_icons;
			set
			{
				if (m_optionalIconImages == null)
					m_optionalIconImages = new Image[0];

				foreach (var image in m_optionalIconImages)
					image.gameObject.SetActive(false);

				m_icons = value;
				if (m_icons == null)
					return;

				for (int i=0; i<m_icons.Count; i++)
				{
					if (i >= m_optionalIconImages.Length)
					{
						Debug.LogWarning($"Attempting to set icon '{m_icons[i]}', but only {m_optionalIconImages.Length} icon images have been set");
						return;
					}
					if (m_icons[i] == null)
						continue;

					Sprite sprite = LoadIcon(m_icons[i]);
					if (sprite != null)
					{
						m_optionalIconImages[i].sprite = sprite;
						m_optionalIconImages[i].gameObject.SetActive(true);
					}
				}
			}
		}

		private Sprite LoadIcon(string _assetPath)
		{
			if (string.IsNullOrEmpty(_assetPath))
				return null;

			Sprite result = Resources.Load<Sprite>(_assetPath);
			if (result == null)
			{
				Debug.LogError($"Sprite '{_assetPath}' not found!");
				return null;
			}
			return result;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
#if (UNITY_EDITOR)
			Validate();
#endif

			m_savedSliderVal = m_slider.value;

			if (m_optionalOnOffToggle != null)
				m_optionalOnOffToggle.OnValueChanged.AddListener(OnOnOffValueChanged);
			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.OnClick.AddListener(OnFullVolumeClick);
		}

		protected override void OnDisable()
		{
			if (m_optionalOnOffToggle != null)
				m_optionalOnOffToggle.OnValueChanged.RemoveListener(OnOnOffValueChanged);
			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.OnClick.RemoveListener(OnFullVolumeClick);
			base.OnDisable();
		}

		protected virtual void OnOnOffValueChanged( bool _value )
		{
			// The on/off toggle is inverted - when it's on, the slider is off and vice versa
			_value = !_value;

			m_slider.interactable = _value;
			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.Enabled = _value;

			if (m_optionalUiImagesToDisableWhenOff != null)
			{
				foreach (var uiImage in m_optionalUiImagesToDisableWhenOff)
					uiImage.Enabled = _value;
			}

			if (_value)
			{
				m_slider.value = m_savedSliderVal;
				return;
			}

			m_savedSliderVal = m_slider.value;
			m_slider.value = 0;
		}

		protected virtual void OnFullVolumeClick()
		{
			m_slider.value = 1;
		}

		private void Validate()
		{
			if (m_optionalNoVolumeButton != null && m_optionalOnOffToggle != null)
				throw new Exception("Only one of optional full volume button or optional on off toggle may be set (or none at all)");
		}

	}
}