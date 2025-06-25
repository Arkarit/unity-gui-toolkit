using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingSlider : UiPlayerSettingBase
	{
		[SerializeField] protected UiSlider m_slider;

		private Func<float, string> OptionsValueToStringFn => m_playerSetting.Options.ValueToStringFn;
		
		protected override void OnEnable()
		{
			base.OnEnable();
			m_slider.OnValueChanged.AddListener(OnSliderValueChanged);
			StartCoroutine(SetSliderOptionsDelayed());
		}

		private string ValueToStringFn(UiSlider _slider) => 
			OptionsValueToStringFn == null ? 
				UiSlider.DefaultValueToStringFn(_slider) : 
				OptionsValueToStringFn.Invoke(_slider.Value);
		
		private IEnumerator SetSliderOptionsDelayed()
		{
			while (m_playerSetting == null || m_playerSetting.Options == null)
				yield return 0;
			
			m_slider.ValueToStringFn = ValueToStringFn;
			m_slider.TextMode = OptionsValueToStringFn != null ? 
				UiSlider.ETextMode.OnMove : 
				UiSlider.ETextMode.NoText;
		}

		protected override void OnDisable()
		{
			m_slider.OnValueChanged.RemoveListener(OnSliderValueChanged);
			base.OnDisable();
		}

		public override object Value
		{
			get => base.Value;
			set
			{
				base.Value = value;
				float f = GetValue<float>();
				m_slider.Value = f;
			}
		}

		private void OnSliderValueChanged( float _value )
		{
			base.Value = _value;
		}

		public override void ApplyIcons(List<string> _assetPaths)
		{
			if (_assetPaths.Count == 1)
			{
				_assetPaths = _assetPaths.ShallowClone();
				_assetPaths.Add(_assetPaths[0]);
				m_slider.FirstIconSmall = true;
			}
			else
				m_slider.FirstIconSmall = false;

			m_slider.Icons = _assetPaths;
		}
	}
}