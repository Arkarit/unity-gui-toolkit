using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(Camera))]
	[ExecuteAlways]
	public class UiMain : UiThing
	{
		private readonly static Dictionary<string, UiView> s_views = new Dictionary<string, UiView>();

		[SerializeField]
		private RenderMode m_renderMode = RenderMode.ScreenSpaceCamera;

		[SerializeField]
		private int m_layerDistance = 10;
		
		private Camera m_camera;

		public override void Awake()
		{
			base.Awake();
			m_camera = GetComponent<Camera>();

			if (Application.isPlaying)
				DontDestroyOnLoad(gameObject);

			SetViews();
		}

		public void OnValidate()
		{
			SetViews();
			foreach (var kv in s_views)
				kv.Value.SetRenderMode(m_renderMode, GetComponent<Camera>(), m_layerDistance);
		}

		public void LoadDialog(string _name, int _layer)
		{

		}

		public void CloseDialog(string _name)
		{

		}

		private void SetViews()
		{
			s_views.Clear();

			foreach (Transform child in transform)
			{
				UiView uiView = child.GetComponent<UiView>();
				if (uiView == null)
					continue;
				if (string.IsNullOrEmpty(uiView.m_name))
					uiView.m_name = uiView.gameObject.name;

				bool keyFound = s_views.ContainsKey(uiView.m_name);

				Debug.Assert(!keyFound, $"Duplicate UiView name '{uiView.m_name}' found. (Check also game object name if UiView Name is not set)");
				if (keyFound)
					continue;

				s_views.Add(uiView.m_name, uiView);
			}

			SetSortOrder();
		}

		private void SetSortOrder()
		{
		}

	}
}