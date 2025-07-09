using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiToggleFader : MonoBehaviour
	{
		[SerializeField] protected CanvasGroup m_canvasGroup;
		[SerializeField] protected Toggle m_toggle;
		[SerializeField] protected bool m_disableAfterFade;
		[SerializeField] protected float m_duration = .3f;

		protected Coroutine m_coroutine;
		
		public Toggle Toggle
		{
			get
			{
				if (m_toggle == null)
					m_toggle = GetComponentInParent<Toggle>();
				
				return m_toggle;
			}
		}
		
		public CanvasGroup CanvasGroup
		{
			get
			{
				if (m_canvasGroup == null)
					m_canvasGroup = GetComponentInParent<CanvasGroup>();
				
				return m_canvasGroup;
			}
		}

		void OnEnable()
		{
			Toggle.onValueChanged.AddListener(OnToggleValueChanged);
			OnToggleValueChanged(Toggle.isOn);
		}

		void OnDisable()
		{
			Toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
		}

		void OnToggleValueChanged( bool _isOn )
		{
			if (m_coroutine != null) 
				StopCoroutine(m_coroutine);
			
			m_coroutine = StartCoroutine(FadeRoutine(_isOn ? 1 : 0));
		}

		System.Collections.IEnumerator FadeRoutine( float target )
		{
			float start = CanvasGroup.alpha;
			for (float t = 0; t < 1; t += Time.unscaledDeltaTime / m_duration)
			{
				CanvasGroup.alpha = Mathf.Lerp(start, target, t);
				yield return null;
			}
			
			CanvasGroup.alpha = target;

			if (m_disableAfterFade)
			{
				bool interactable = target > .01f;
				CanvasGroup.interactable = interactable;
				CanvasGroup.blocksRaycasts = interactable;
			}
		}
	}
}