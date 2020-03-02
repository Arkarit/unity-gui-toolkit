using System;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(Canvas))]
	public class UiView : UiThing
	{
		public string m_name;

		[SerializeField]
		EUiLayerDefinition m_layer;

		private Canvas m_canvas;

		public override void Awake()
		{
			base.Awake();
			m_canvas = GetComponent<Canvas>();
		}

		public virtual void Show()
		{
			gameObject.SetActive(true);
		}

		public virtual void Hide()
		{
			gameObject.SetActive(false);
		}

		public void SetRenderMode( RenderMode _renderMode, Camera _camera, int _layerDistance )
		{
#if UNITY_EDITOR
			m_canvas = GetComponent<Canvas>();
#endif
			m_canvas.renderMode = _renderMode;
			m_canvas.worldCamera = _camera;
			m_canvas.planeDistance = (float) _layerDistance * (float) m_layer;
		}
	}
}