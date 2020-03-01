using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public interface ICanvasListener
	{
		void OnCanvasChanged();
	}

	/// <summary>
	/// Canvas tracker is a class which tracks a canvas for the most relevant changes:
	/// Render mode and plane distance.
	/// Normally, there is no need to react manually to these kinds of changes, since Unity aligns everything manually.
	/// There is however a problem with TextMeshPro in conjunction with BaseMeshEffectTMP.
	/// TextMeshPro sometimes re-creates its meshes on canvas changes - silently.
	/// This breaks the BaseMeshEffectTMP update system (which is awkward anyway, but the only possible way to modify TMP meshes)
	/// So the whole tracker here is a workaround for the workaround.
	/// </summary>
	public class CanvasTracker : IEditorUpdateable
	{
		private class CanvasData
		{
			private bool m_pristine = true;

			private readonly List<ICanvasListener> m_listeners = new List<ICanvasListener>();

			private RenderMode m_renderMode;
			private float m_planeDistance;

			public bool Empty { get { return m_listeners.Count == 0; }}

			public void UpdateValues(Canvas _canvas)
			{
				if (!m_pristine
				&&  m_renderMode == _canvas.renderMode
				&&  m_planeDistance == _canvas.planeDistance
				)
					return;

				m_renderMode = _canvas.renderMode;
				m_planeDistance = _canvas.planeDistance;

				CallListeners();

				m_pristine = false;
			}

			public void AddListener(ICanvasListener _listener)
			{
				if (m_listeners.Contains(_listener))
					return;
				m_listeners.Add(_listener);
			}

			public void RemoveListener(ICanvasListener _listener)
			{
				if (!m_listeners.Contains(_listener))
					return;
				m_listeners.Remove(_listener);
			}

			private void CallListeners()
			{
				foreach(var listener in m_listeners)
					listener.OnCanvasChanged();
			}

		}

		private readonly Dictionary<Canvas,CanvasData> s_canvasValues = new Dictionary<Canvas, CanvasData>();

		private static CanvasTracker s_instance = null;

		public static CanvasTracker Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new CanvasTracker();
				return s_instance;
			}
		}

		public void AddListener(Canvas _canvas, ICanvasListener _listener)
		{
			EditorUpdater.StartUpdating(this);

			if (!s_canvasValues.ContainsKey(_canvas))
			{
				s_canvasValues[_canvas] = new CanvasData();
			}
			s_canvasValues[_canvas].AddListener(_listener);
		}

		public void RemoveListener(Canvas _canvas, ICanvasListener _listener)
		{
			if (!s_canvasValues.ContainsKey(_canvas))
				return;

			s_canvasValues[_canvas].RemoveListener(_listener);

			if (s_canvasValues[_canvas].Empty)
				s_canvasValues.Remove(_canvas);
		}

		private CanvasTracker()
		{
			Canvas.willRenderCanvases += Update;
		}

		private void Update()
		{
			foreach( var kv in s_canvasValues)
			{
				kv.Value.UpdateValues(kv.Key);
			}
		}

		public void UpdateInEditor( float _deltaTime )
		{
			Update();
		}

		public bool RemoveFromEditorUpdate()
		{
			return false;
		}
	}
}