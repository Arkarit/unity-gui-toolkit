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
		private UiMain m_main;

		public override void Awake()
		{
			base.Awake();
			Init();
		}

		public virtual void Show(bool _instant = false)
		{
			gameObject.SetActive(true);
		}

		public virtual void Hide(bool _instant = false)
		{
			gameObject.SetActive(false);
		}

		public void SetRenderMode( RenderMode _renderMode, Camera _camera )
		{
#if UNITY_EDITOR
			Init();
#endif
			m_canvas.renderMode = _renderMode;
			m_canvas.worldCamera = _camera;

			if (m_main)
				m_canvas.planeDistance = m_main.LayerDistance * (float) m_layer;
		}

		private void Init()
		{
			m_main = GetComponentInParent<UiMain>();
			m_canvas = GetComponent<Canvas>();

			if (m_main)
				m_canvas.planeDistance = m_main.LayerDistance * (float) m_layer;
		}

		public void OnValidate()
		{
			Init();
		}
	}
}