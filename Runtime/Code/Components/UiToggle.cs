using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Toggle))]
	public class UiToggle: UiButtonBase
	{
		private Toggle m_toggle;
		private Color m_savedColor;

		public Toggle Toggle => m_toggle;

		public Toggle.ToggleEvent OnValueChanged => Toggle.onValueChanged;

		public bool IsOn
		{
			get
			{
				return Toggle.isOn;
			}
			set
			{
				Toggle.isOn = value;
			}
		}

		public void SetDelayed(bool _value) => CoroutineManager.Instance.StartCoroutine(SetDelayedCoroutine(_value));

		protected override void Awake()
		{
			base.Awake();
			m_toggle = GetComponent<Toggle>();
			m_savedColor = m_toggle.colors.normalColor;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			ToggleWorkaround(Toggle.isOn);
			Toggle.onValueChanged.AddListener(ToggleWorkaround);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Toggle.onValueChanged.RemoveListener(ToggleWorkaround);
			ToggleWorkaround(false);
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (Toggle.isOn)
				return;

			base.OnPointerDown(eventData);
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			if (Toggle.isOn)
				return;

			base.OnPointerUp(eventData);
		}

		private void ToggleWorkaround(bool _active)
		{
			var colors = Toggle.colors;
			colors.normalColor = _active ? colors.selectedColor : m_savedColor;
			Toggle.colors = colors;
		}

		protected IEnumerator SetDelayedCoroutine(bool _value)
		{
			yield return 0;
			IsOn = _value;
		}

		protected override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			base.OnEnabledInHierarchyChanged(_enabled);
			InitIfNecessary();
			m_toggle.interactable = _enabled;
		}

		private void OnValidate()
		{
			m_toggle = GetComponent<Toggle>();
		}
	}
}