using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit.Style
{
	/// <summary>
	/// One systematic shortcoming of the UI styling system is, that a lot of values can not be faded.
	/// E.g. a reference to a class or struct can obviously not be faded smoothly into another class or struct.
	/// UiStyleFader helps to fade them visually nevertheless.
	/// It uses 2 UiAbstractApplyStyleBase and 2 CanvasGroup to fade.
	/// The listening of the UiAbstractApplyStyleBase for style changes is disabled; instead UiStyleFader listens for events
	/// and handles the style application manually.  
	/// </summary>
	[ExecuteAlways]
	public class UiStyleFader : MonoBehaviour
	{
		[SerializeField] private UiAbstractApplyStyleBase[] m_applyStyle0 = Array.Empty<UiAbstractApplyStyleBase>();
		[SerializeField] private UiAbstractApplyStyleBase[] m_applyStyle1 = Array.Empty<UiAbstractApplyStyleBase>();
		[SerializeField] private CanvasGroup m_canvasGroup0;
		[SerializeField] private CanvasGroup m_canvasGroup1;

		public UiAbstractApplyStyleBase[] ApplyStyle0 => m_applyStyle0;
		public UiAbstractApplyStyleBase[] ApplyStyle1 => m_applyStyle1;
		
		protected void Awake()
		{
			// Appliers shouldn't update themselves, we do it instead
			SetAllApplyStyles(applyStyle =>
			{
				applyStyle.enabled = false;
				applyStyle.Tweenable = false;
			} );

			m_canvasGroup0.alpha = 1;
			m_canvasGroup1.alpha = 0;
		}

		protected void OnEnable()
		{
			UiEventDefinitions.EvSkinChanged.AddListener(OnSkinChanged);
			UiEventDefinitions.EvSkinValuesChanged.AddListener(OnSkinValuesChanged);
			OnSkinChanged(0);
		}
		protected void OnDisable()
		{
			UiEventDefinitions.EvSkinChanged.RemoveListener(OnSkinChanged);
			UiEventDefinitions.EvSkinValuesChanged.RemoveListener(OnSkinValuesChanged);
		}

		private void OnSkinValuesChanged(float _normVal)
		{
			if (_normVal >= 1)
			{
				SetApplyStyles(m_applyStyle0, applyStyle => applyStyle.OnSkinChanged(0) );
				m_canvasGroup0.alpha = 1;
				m_canvasGroup1.alpha = 0;
				return;
			}
			
			m_canvasGroup0.alpha = 1-_normVal;
			m_canvasGroup1.alpha = _normVal;
		}

		private void OnSkinChanged(float _duration)
		{
			m_canvasGroup0.alpha = 1;
			m_canvasGroup1.alpha = 0;
			
			if (_duration <= 0)
			{
				SetApplyStyles(m_applyStyle0, applyStyle => applyStyle.OnSkinChanged(0) );
				return;
			}
			
			SetApplyStyles(m_applyStyle1, applyStyle => applyStyle.OnSkinChanged(0) );
		}
		
		private void SetAllApplyStyles(Action<UiAbstractApplyStyleBase> _action)
		{
			SetApplyStyles(m_applyStyle0, _action);
			SetApplyStyles(m_applyStyle1, _action);
		}
		
		private void SetApplyStyles(UiAbstractApplyStyleBase[] _arr, Action<UiAbstractApplyStyleBase> _action)
		{
			foreach (var applyStyle in _arr)
				_action(applyStyle);
		}
		
		private void EmitOnSkinChanged(UiAbstractApplyStyleBase _applyStyle)
		{
			foreach (var value in _applyStyle.Style.Values)
				value.StopTween();
			_applyStyle.OnSkinChanged(0);
		}
	}
}
