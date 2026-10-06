using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// 3D icon backend for the built-in render pipeline. The environment is in <see cref="Icon3DEnvironmentBackend"/>.
	///
	/// Shadows come from QualitySettings: for a shadow catcher they are forced on (high resolution, one cascade over
	/// exactly the camera's depth range) for this render only and restored afterwards.
	/// </summary>
	public class BuiltinIcon3DBackend : Icon3DEnvironmentBackend
	{
		private GameObject m_shadowGo;
		private UiCameraRenderSettings m_shadowSettings;

		public override void SetupCamera( Camera _camera )
		{
			_camera.enabled = false;
			_camera.clearFlags = CameraClearFlags.SolidColor;
			_camera.renderingPath = RenderingPath.Forward;   // deferred does not keep a transparent background
			_camera.allowHDR = false;
			_camera.allowMSAA = true;
			_camera.useOcclusionCulling = false;
			_camera.depthTextureMode = DepthTextureMode.None;
		}

		protected override void BeginShadows( UiIcon3DPreset _preset )
		{
			EnsureShadowSettings();
			m_shadowSettings.Apply();
		}

		protected override void EndShadows() => m_shadowSettings.Restore();

		protected override void BeforeRender( Camera _camera )
		{
			// The shadow map covers [camera, shadow distance]: as far as the camera sees is exactly what is needed, and a
			// single cascade over anything more spends its resolution on empty space
			if (ShadowsRequested && !PresetSetsShadowDistance)
				QualitySettings.shadowDistance = _camera.farClipPlane;
		}

		public override void Dispose()
		{
			DestroyObject(m_shadowGo);
			m_shadowGo = null;
			m_shadowSettings = null;
			base.Dispose();
		}

		private void EnsureShadowSettings()
		{
			if (m_shadowSettings != null)
				return;

			m_shadowGo = new GameObject("Icon3D Shadow Settings") { hideFlags = HideFlags.HideAndDontSave };
			m_shadowSettings = m_shadowGo.AddComponent<UiCameraRenderSettings>();
			Enable(m_shadowSettings.Shadows, ShadowQuality.All);
			Enable(m_shadowSettings.ShadowResolution, UnityEngine.ShadowResolution.High);   // Low blurs a small object's shadow to a faint plateau
			Enable(m_shadowSettings.ShadowCascades, 1);
			Enable(m_shadowSettings.ShadowDistance, 20f);
		}
	}
}
