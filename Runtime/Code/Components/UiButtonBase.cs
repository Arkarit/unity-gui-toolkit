using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	public abstract class UiButtonBase : UiTextContainer, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
	{
		[Tooltip("Simple animation")]
		[SerializeField][Optional] protected UiSimpleAnimation m_simpleAnimation;
		[Tooltip("Audio source")]
		[SerializeField][Optional] protected AudioSource m_audioSource;
		[Tooltip("Button Image. Mandatory if you want to use the 'Color' property or the 'Enabled' property.")]
		[SerializeField] protected UiImage m_uiImage;

		[FormerlySerializedAs("m_optionalAdditionalMouseOver")]
		[Tooltip("Additional mouse over graphic")]
		[SerializeField][Optional] protected Graphic m_additionalMouseOver;
		[Tooltip("Mouse over fade duration")]
		[SerializeField][Optional] protected float m_additionalMouseOverDuration = 0.2f;
		[Tooltip("Forward button. Forwards the click to another button. Animations etc. for this button are not played in that case")]
		[SerializeField][Optional] protected UiButtonBase m_forwardButton;

		public override bool IsEnableableInHierarchy => true;

		public Color Color
		{
			get
			{
				if (m_uiImage == null)
					return Color.white;

				return m_uiImage.Color;
			}
			set
			{
				if (m_uiImage == null)
				{
					UiLog.LogError("Attempt to set button color, but UI image was not set");
					return;
				}

				m_uiImage.Color = value;
			}
		}

		public void SetSimpleGradientColors(Color _leftOrTop, Color _rightOrBottom)
		{
			if (m_uiImage == null)
			{
				UiLog.LogError("Attempt to set simple gradient colors, but simple gradient was not set");
				return;
			}
			m_uiImage.SetSimpleGradientColors(_leftOrTop, _rightOrBottom);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			OnPointerExit(null);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			OnPointerExit(null);
		}

		public (Color leftOrTop, Color rightOrBottom) GetSimpleGradientColors()
		{
			if (m_uiImage == null)
				return (leftOrTop:Color.white, rightOrBottom:Color.white);
			return m_uiImage.GetSimpleGradientColors();
		}

		public override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			base.OnEnabledInHierarchyChanged(_enabled);
			if (!_enabled && m_simpleAnimation)
				m_simpleAnimation.Stop(false);
		}

		public virtual void OnPointerDown( PointerEventData _ )
		{
			EvaluateButton(false);
		}

		protected virtual bool ForwardClick() => false;

		protected virtual bool EvaluateButton(bool _playBackwardsAnimation)
		{
			if (!EnabledInHierarchy || !enabled || !gameObject.activeInHierarchy)
				return false;

			if (m_forwardButton != null)
				return m_forwardButton.ForwardClick();
			
			if (m_simpleAnimation != null)
			{
				if (_playBackwardsAnimation)
					m_simpleAnimation.Play(false, () => m_simpleAnimation.Play(true));
				else
					m_simpleAnimation.Play();
			}
			
			if (m_audioSource != null)
				m_audioSource.Play();
			
			return true;
		}

		public virtual void OnPointerUp( PointerEventData _ )
		{
			if (!EnabledInHierarchy)
				return;

			if (m_simpleAnimation != null)
				m_simpleAnimation.Play(true);
		}

		public void OnPointerEnter(PointerEventData _)
		{
			if (m_additionalMouseOver)
				m_additionalMouseOver.CrossFadeColor(Color.white, m_additionalMouseOverDuration, false, true);
		}

		public void OnPointerExit(PointerEventData _)
		{
			if (m_additionalMouseOver)
				m_additionalMouseOver.CrossFadeColor(Color.clear, m_additionalMouseOverDuration, false, true);
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			OnEnabledInHierarchyChanged(EnabledInHierarchy);
		}
#endif

	}
}