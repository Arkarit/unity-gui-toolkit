using System.Collections;
using GuiToolkit.Style;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Toggle))]
	public class UiToggle: UiButtonBase
	{
		public enum EToggleGroupHandling
		{
			Ignore,
			Find,
			FindOrCreate,
		}
		
		[FormerlySerializedAs("m_animatedWhenSelected")] [SerializeField] protected bool m_playButtonAnimation;
		[SerializeField][Optional] protected UiSimpleAnimation m_selectionAnimation;
		[SerializeField] protected EToggleGroupHandling m_toggleGroupHandling = EToggleGroupHandling.Ignore;

		[SerializeField][Optional] protected UiAbstractApplyStyleBase[] m_styleAppliers = new UiAbstractApplyStyleBase[0];
		[SerializeField] protected string m_skinOn = "On";
		[SerializeField] protected string m_skinOff = "Off";
		
		private Toggle m_toggle;
		private Color m_savedColor;
		private bool m_toggleGroupWasManuallySet;

		public Toggle Toggle
		{
			get
			{
				if (!m_toggle)
					m_toggle = GetComponent<Toggle>();

				return m_toggle;
			}
		}

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

		public void SetDelayed(bool _value) => CoRoutineRunner.Instance.StartCoroutine(SetDelayedCoroutine(_value));

		protected override void Awake()
		{
			base.Awake();
			m_savedColor = Toggle.colors.normalColor;
			m_toggleGroupWasManuallySet = Toggle.group != null;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_toggleGroupHandling != EToggleGroupHandling.Ignore && !m_toggleGroupWasManuallySet)
				ExecuteFrameDelayed(SetToggleGroupIfNecessary);
			
			PlaySelectionAnimationIfNecessary(Toggle.isOn, true);
			Toggle.onValueChanged.AddListener(PlaySelectionAnimationIfNecessary);
			if (m_styleAppliers != null && m_styleAppliers.Length > 0)
				m_toggle.onValueChanged.AddListener(OnToggleValueChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Toggle.onValueChanged.RemoveListener(PlaySelectionAnimationIfNecessary);
			PlaySelectionAnimationIfNecessary(false, true);
			if (!m_toggleGroupWasManuallySet && m_toggleGroupHandling != EToggleGroupHandling.Ignore)
				Toggle.group = null;

			if (m_styleAppliers != null && m_styleAppliers.Length > 0)
				m_toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
		}

		private void OnToggleValueChanged(bool _isOn)
		{
			string skin = _isOn ? m_skinOn : m_skinOff;
			foreach (var styleApplier in m_styleAppliers)
			{
				if (!styleApplier)
					continue;

				styleApplier.FixedSkinName = skin;
			}
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (!m_playButtonAnimation && Toggle.isOn)
				return;

			base.OnPointerDown(eventData);
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			if (!m_playButtonAnimation && Toggle.isOn)
				return;

			base.OnPointerUp(eventData);
		}
		
		private void SetToggleGroupIfNecessary()
		{
			Debug.Assert(m_toggleGroupHandling != EToggleGroupHandling.Ignore && !m_toggleGroupWasManuallySet);
			
			var toggleGroup = GetComponentInParent<ToggleGroup>();
			if (toggleGroup != null)
			{
				Toggle.group = toggleGroup;
				return;
			}
			
			if (m_toggleGroupHandling == EToggleGroupHandling.Find)
			{
				UiLog.LogWarning($"Toggle group could not be found, toggle might not work properly\n{this.GetPath()}", this);
				return;
			}
			
			if (transform.parent == null)
			{
				UiLog.LogWarning($"Toggle group could not be created, since toggle has no parent; might not work properly\n{this.GetPath()}", this);
				return;
			}
			
			Toggle.group = transform.parent.gameObject.AddComponent<ToggleGroup>();
		}

		private void PlaySelectionAnimationIfNecessary(bool _active) => PlaySelectionAnimationIfNecessary(_active, false);

		private void PlaySelectionAnimationIfNecessary(bool _active, bool _instant)
		{
			// Workaround for Unity issue
			var colors = Toggle.colors;
			colors.normalColor = _active ? colors.selectedColor : m_savedColor;
			Toggle.colors = colors;
			
			if (m_selectionAnimation != null)
			{
				if (_instant)
					m_selectionAnimation.Reset(_active);
				else
					m_selectionAnimation.Play(!_active);
			}
		}

		protected IEnumerator SetDelayedCoroutine(bool _value)
		{
			yield return 0;
			IsOn = _value;
		}

		public override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			base.OnEnabledInHierarchyChanged(_enabled);
			InitIfNecessary();
			Toggle.interactable = _enabled;
		}

		private void OnValidate()
		{
			m_toggle = GetComponent<Toggle>();
		}
	}
}