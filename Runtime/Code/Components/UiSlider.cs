using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Slider;

namespace GuiToolkit
{
	public class UiSlider : UiThing
	{
		public enum ETextMode
		{
			NoText,
			Always,
			OnMove
		}
		
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
		
		[Header("Text")]
		[Tooltip("Show text when slider is moved.")]
		[SerializeField] protected ETextMode m_textMode;
		[Tooltip("Optional text component")]
		[SerializeField] protected TMP_Text m_optionalValueText;
		[Tooltip("Optional animation for show/hide text")]
		[SerializeField] protected UiSimpleAnimation m_optionalShowTextAnimation;
		[SerializeField] protected float m_textBaseValue = 0;
		[SerializeField] protected float m_textMultiplier = 1;
		[SerializeField] protected bool m_textIsInt = false;
		[SerializeField] protected float m_hideTextDelaySeconds = 1;

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

		public Func<UiSlider, string> ValueToStringFn = DefaultValueToStringFn;
		public float TextMultiplier => m_textMultiplier;
		public float TextBaseValue => m_textBaseValue;
		
		public bool TextIsInt => m_textIsInt;
		public ETextMode TextMode
		{
			get => m_textMode;
			set => m_textMode = value;
		}

		public override bool IsEnableableInHierarchy => true;

		private float m_savedSliderVal;
		private List<string> m_icons;
		private Coroutine m_hideTextCoroutine = null;
		

		public SliderEvent OnValueChanged => m_slider.onValueChanged;

		// This value is always normalized.
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
					UiLog.LogError("Attempt to set first icon big/small, but necessary members were not set");
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
						UiLog.LogWarning($"Attempting to set icon '{m_icons[i]}', but only {m_optionalIconImages.Length} icon images have been set");
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

		public static string DefaultValueToStringFn(UiSlider _slider)
		{
			var val = _slider.TextBaseValue + _slider.Value * _slider.TextMultiplier;
			if (_slider.TextIsInt)
			{
				int intVal = (int) Mathf.Round(val);
				return intVal.ToString();
			}

			return string.Format(CultureInfo.InvariantCulture, "{0:F2}", val);
		}

		private Sprite LoadIcon(string _assetPath)
		{
			if (string.IsNullOrEmpty(_assetPath))
				return null;

			Sprite result = Resources.Load<Sprite>(_assetPath);
			if (result == null)
			{
				UiLog.LogError($"Sprite '{_assetPath}' not found!");
				return null;
			}
			return result;
		}

		public override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			m_slider.interactable = _enabled;
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
			else if (m_optionalNoVolumeButton != null)
				m_optionalNoVolumeButton.OnClick.AddListener(OnNoVolumeButton);

			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.OnClick.AddListener(OnFullVolumeClick);
			
			bool hasText = m_optionalValueText != null;
			
			if (!hasText)
				return;
			
			m_optionalValueText.gameObject.SetActive(m_textMode == ETextMode.Always || m_optionalShowTextAnimation != null);
			if (m_optionalShowTextAnimation != null)
				m_optionalShowTextAnimation.Reset();
			
			if (m_textMode != ETextMode.NoText)
				OnValueChanged.AddListener(ChangeText);
		}

		protected override void OnDisable()
		{
			if (m_textMode != ETextMode.NoText)
				OnValueChanged.RemoveListener(ChangeText);
			
			if (m_optionalOnOffToggle != null)
				m_optionalOnOffToggle.OnValueChanged.RemoveListener(OnOnOffValueChanged);
			else if (m_optionalNoVolumeButton != null)
				m_optionalNoVolumeButton.OnClick.RemoveListener(OnNoVolumeButton);

			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.OnClick.RemoveListener(OnFullVolumeClick);
			
			m_hideTextCoroutine = null;
			base.OnDisable();
		}

		private void ChangeText(float _value)
		{
			if ( m_optionalValueText == null || TextMode == ETextMode.NoText)
				return;
			
			m_optionalValueText.text = ValueToStringFn(this);
			
			if (TextMode == ETextMode.Always)
				return;
			
			if (m_hideTextCoroutine != null)
			{
				StopCoroutine(m_hideTextCoroutine);
				m_hideTextCoroutine = StartCoroutine(HideTextDelayed());
				return;
			}
			
			m_optionalValueText.gameObject.SetActive(true);
			if (m_optionalShowTextAnimation != null)
				m_optionalShowTextAnimation.Play();

			m_hideTextCoroutine = StartCoroutine(HideTextDelayed());
		}

		IEnumerator HideTextDelayed()
		{
			yield return new WaitForSeconds(m_hideTextDelaySeconds);
			
			m_hideTextCoroutine = null;

			if (m_optionalShowTextAnimation != null)
			{
				m_optionalShowTextAnimation.Play(true);
				yield break;
			}
			
			if (m_optionalValueText != null)
				m_optionalValueText.gameObject.SetActive(false);
		}
		
		protected virtual void OnOnOffValueChanged( bool _value )
		{
			// The on/off toggle is inverted - when it's on, the slider is off and vice versa
			_value = !_value;

			m_slider.interactable = _value;
			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.EnabledInHierarchy = _value;

			if (m_optionalUiImagesToDisableWhenOff != null)
			{
				foreach (var uiImage in m_optionalUiImagesToDisableWhenOff)
					uiImage.EnabledInHierarchy = _value;
			}

			if (_value)
			{
				m_slider.value = m_savedSliderVal;
				return;
			}

			m_savedSliderVal = m_slider.value;
			m_slider.value = 0;
		}

		private void OnNoVolumeButton()
		{
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