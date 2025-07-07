using System;
using UnityEngine;

namespace GuiToolkit
{
	public class VisibilityByFullscreenDialog : MonoBehaviour
	{
		[SerializeField] private bool m_visibleInFullscreen = true;
		
		private void Awake()
		{
			UiEventDefinitions.EvFullScreenView.AddListener(OnFullScreenView);
		}

		private void Start()
		{
			gameObject.SetActive(UiMain.Instance.IsFullScreenViewOpen);
		}

		private void OnDestroy()
		{
			UiEventDefinitions.EvFullScreenView.RemoveListener(OnFullScreenView);
		}

		private void OnFullScreenView(UiView _, bool _visible)
		{
			var active = m_visibleInFullscreen ? _visible : !_visible;
			gameObject.SetActive(active);
		}
	}
}