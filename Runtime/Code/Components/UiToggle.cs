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

		[Tooltip("Style appliers whose skin is switched between 'On' and 'Off' to represent the toggle state.")]
		[SerializeField][Optional] protected UiAbstractApplyStyleBase[] m_styleAppliers = new UiAbstractApplyStyleBase[0];
		[SerializeField] protected string m_skinOn = "On";
		[SerializeField] protected string m_skinOff = "Off";

		[Tooltip("Optional game object which is set active while the toggle is on and inactive while it is off.")]
		[SerializeField][Optional] protected GameObject m_gameObjectOn;
		[Tooltip("Optional game object which is set active while the toggle is off and inactive while it is on.")]
		[SerializeField][Optional] protected GameObject m_gameObjectOff;

		[Tooltip("Optional animation which is played forwards to select (on) and backwards to deselect (off).")]
		[SerializeField][Optional] protected UiSimpleAnimationBase m_stateAnimation;
		
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

		/// <summary>
		/// True if any state representation (style, game object activation or state animation) is configured
		/// and therefore needs to react to toggle value changes.
		/// </summary>
		protected bool HasToggleStateRepresentation =>
			(m_styleAppliers != null && m_styleAppliers.Length > 0)
			|| m_gameObjectOn != null
			|| m_gameObjectOff != null
			|| m_stateAnimation != null;

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

		/// <summary>
		/// Sets the toggle state without firing <see cref="Toggle.onValueChanged"/>, avoiding feedback loops.
		/// The selection animation, color and all state representations (style skin, game object
		/// activation, state animation) are updated instantly.
		/// </summary>
		public void SetIsOnWithoutNotify(bool _value)
		{
			Toggle.SetIsOnWithoutNotify(_value);
			PlaySelectionAnimationIfNecessary(_value, true);
			ApplyToggleState(_value, true);
		}

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

			ApplyToggleState(Toggle.isOn, true);
			if (HasToggleStateRepresentation)
				Toggle.onValueChanged.AddListener(OnToggleValueChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Toggle.onValueChanged.RemoveListener(PlaySelectionAnimationIfNecessary);
			PlaySelectionAnimationIfNecessary(false, true);
			if (!m_toggleGroupWasManuallySet && m_toggleGroupHandling != EToggleGroupHandling.Ignore)
				Toggle.group = null;

			if (HasToggleStateRepresentation)
				Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
		}

		protected virtual void OnToggleValueChanged(bool _isOn) => ApplyToggleState(_isOn, false);

		/// <summary>
		/// Applies all configured state representations (style skin, game object activation and state animation)
		/// for the given toggle state.
		/// </summary>
		/// <param name="_isOn">The toggle state to represent.</param>
		/// <param name="_instant">If true, the state animation jumps to its final state instead of playing.</param>
		protected virtual void ApplyToggleState(bool _isOn, bool _instant)
		{
			// Style system
			if (m_styleAppliers != null)
			{
				string skin = _isOn ? m_skinOn : m_skinOff;
				foreach (var styleApplier in m_styleAppliers)
				{
					if (!styleApplier)
						continue;

					styleApplier.FixedSkinName = skin;
				}
			}

			// Hard game object activation
			if (m_gameObjectOn != null)
				m_gameObjectOn.SetActive(_isOn);
			if (m_gameObjectOff != null)
				m_gameObjectOff.SetActive(!_isOn);

			// State animation (forwards to select, backwards to deselect)
			if (m_stateAnimation != null)
			{
				if (_instant)
					m_stateAnimation.Reset(_isOn);
				else
					m_stateAnimation.Play(!_isOn);
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

		protected void PlaySelectionAnimationIfNecessary(bool _active) => PlaySelectionAnimationIfNecessary(_active, false);

		protected void PlaySelectionAnimationIfNecessary(bool _active, bool _instant)
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