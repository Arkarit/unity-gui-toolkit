using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuiToolkit
{
	/// <summary>
	/// Base for components that override global (static) settings while one particular camera renders,
	/// e.g. ambient light or fog from <see cref="RenderSettings"/>. The originals are restored right after
	/// that camera finished, so neither the scene nor other cameras see the change.
	///
	/// Derived classes declare <see cref="PerCameraSetting{T}"/> fields and bind them in <see cref="CollectSettings"/>.
	/// Several components on one camera are applied in the order they were enabled and restored in reverse.
	///
	/// Works with the built-in pipeline (Camera.onPreCull / onPostRender) and with any SRP
	/// (RenderPipelineManager.begin/endCameraRendering). Whether an SRP actually reads a given global
	/// at camera time is up to the pipeline; URP and HDRP take many of these from their own assets/volumes.
	///
	/// Without a Camera on the same GameObject the component is a plain container: nothing applies it
	/// automatically, the owner calls <see cref="Apply"/> / <see cref="Restore"/> around its own render call.
	/// 3D icon presets use it that way (a camera inside a preset prefab would render by itself).
	/// </summary>
	[ExecuteAlways]
	public abstract class UiAbstractPerCameraSettings : MonoBehaviour
	{
		private readonly List<PerCameraSettingBase> m_settings = new();
		private Camera m_camera;
		private bool m_isApplied;

		public Camera Camera
		{
			get
			{
				if (m_camera == null)
					m_camera = GetComponent<Camera>();
				return m_camera;
			}
		}

		public bool IsApplied => m_isApplied;

		/// <summary>
		/// Bind all settings to their globals and add them to the list, in the order they should be applied.
		/// Called before every apply, because deserialization (e.g. an inspector edit) may have replaced the
		/// setting instances; keep it cheap and allocation free (see <see cref="PerCameraSetting{T}.Bind"/>).
		/// </summary>
		protected abstract void CollectSettings( List<PerCameraSettingBase> _settings );

		/// <summary>Copy the current global values into all settings, enabled or not.</summary>
		public void CaptureCurrentValues()
		{
			if (m_isApplied)
			{
				UiLog.LogWarning("Can not capture while the settings are applied", this);
				return;
			}

			Rebuild();
			foreach (var setting in m_settings)
				setting.Capture();
		}

		/// <summary>
		/// Apply the overrides now. Normally called automatically when the camera starts rendering;
		/// call it manually only together with <see cref="Restore"/>, e.g. around a custom render call.
		/// </summary>
		public void Apply()
		{
			if (m_isApplied)
				return;

			Rebuild();
			for (int i = 0; i < m_settings.Count; i++)
				m_settings[i].Apply();

			m_isApplied = true;
		}

		/// <summary>Restore the original global values.</summary>
		public void Restore()
		{
			if (!m_isApplied)
				return;

			for (int i = m_settings.Count - 1; i >= 0; i--)
				m_settings[i].Restore();

			m_isApplied = false;
		}

		protected virtual void OnEnable()
		{
			PerCameraSettingsDispatcher.Register(Camera, this);
		}

		protected virtual void OnDisable()
		{
			Restore();
			PerCameraSettingsDispatcher.Unregister(Camera, this);
		}

		private void Rebuild()
		{
			m_settings.Clear();
			CollectSettings(m_settings);
		}
	}

	/// <summary>
	/// Routes camera render callbacks to the per-camera settings components of that camera.
	/// One static subscription for all components instead of one per component.
	/// </summary>
	internal static class PerCameraSettingsDispatcher
	{
		private static readonly Dictionary<Camera, List<UiAbstractPerCameraSettings>> s_byCamera = new();
		private static bool s_hooked;

		public static void Register( Camera _camera, UiAbstractPerCameraSettings _settings )
		{
			if (_camera == null)
				return;

			if (!s_byCamera.TryGetValue(_camera, out var list))
			{
				list = new List<UiAbstractPerCameraSettings>();
				s_byCamera.Add(_camera, list);
			}

			if (!list.Contains(_settings))
				list.Add(_settings);

			Hook(true);
		}

		public static void Unregister( Camera _camera, UiAbstractPerCameraSettings _settings )
		{
			if (_camera == null || !s_byCamera.TryGetValue(_camera, out var list))
			{
				// camera may already be destroyed; search by value
				RemoveEverywhere(_settings);
				return;
			}

			list.Remove(_settings);
			if (list.Count == 0)
				s_byCamera.Remove(_camera);

			if (s_byCamera.Count == 0)
				Hook(false);
		}

		private static void RemoveEverywhere( UiAbstractPerCameraSettings _settings )
		{
			Camera emptied = null;
			foreach (var kv in s_byCamera)
			{
				if (kv.Value.Remove(_settings) && kv.Value.Count == 0)
				{
					emptied = kv.Key;
					break;
				}
			}

			if (!ReferenceEquals(emptied, null))
				s_byCamera.Remove(emptied);

			if (s_byCamera.Count == 0)
				Hook(false);
		}

		private static void Hook( bool _on )
		{
			if (_on == s_hooked)
				return;

			s_hooked = _on;
			if (_on)
			{
				Camera.onPreCull += OnBeginCamera;
				Camera.onPostRender += OnEndCamera;
				RenderPipelineManager.beginCameraRendering += OnBeginCameraSrp;
				RenderPipelineManager.endCameraRendering += OnEndCameraSrp;
				return;
			}

			Camera.onPreCull -= OnBeginCamera;
			Camera.onPostRender -= OnEndCamera;
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraSrp;
			RenderPipelineManager.endCameraRendering -= OnEndCameraSrp;
		}

		private static void OnBeginCameraSrp( ScriptableRenderContext _, Camera _camera ) => OnBeginCamera(_camera);
		private static void OnEndCameraSrp( ScriptableRenderContext _, Camera _camera ) => OnEndCamera(_camera);

		private static void OnBeginCamera( Camera _camera )
		{
			if (!s_byCamera.TryGetValue(_camera, out var list))
				return;

			for (int i = 0; i < list.Count; i++)
				list[i].Apply();
		}

		private static void OnEndCamera( Camera _camera )
		{
			if (!s_byCamera.TryGetValue(_camera, out var list))
				return;

			for (int i = list.Count - 1; i >= 0; i--)
				list[i].Restore();
		}
	}
}
