using UnityEngine;
using UnityEngine.Rendering;

namespace GuiToolkit
{
	/// <summary>
	/// 3D icon backend for the built-in render pipeline.
	///
	/// Environment: a neutral baseline (black flat ambient, no fog, no skybox, black custom reflection) is applied
	/// first, the preset's <see cref="UiCameraRenderSettings"/> on top. Both are restored after the render.
	/// The baseline is what an empty scene would give - the scene isolation tests compare against exactly that.
	/// </summary>
	public class BuiltinIcon3DBackend : IIcon3DRenderBackend
	{
		private GameObject m_baselineGo;
		private UiCameraRenderSettings m_baseline;
		private Cubemap m_blackCube;
		private UiCameraRenderSettings m_activePresetSettings;
		private GameObject m_shadowGo;
		private UiCameraRenderSettings m_shadowSettings;
		private bool m_shadowsActive;
		private bool m_presetSetsShadowDistance;

		public void SetupCamera( Camera _camera )
		{
			_camera.enabled = false;
			_camera.clearFlags = CameraClearFlags.SolidColor;
			_camera.renderingPath = RenderingPath.Forward;   // deferred does not keep a transparent background
			_camera.allowHDR = false;
			_camera.allowMSAA = true;
			_camera.useOcclusionCulling = false;
			_camera.depthTextureMode = DepthTextureMode.None;
		}

		public void BeginEnvironment( UiIcon3DPreset _preset )
		{
			EnsureBaseline();
			m_baseline.Apply();

			// The shadow catcher needs shadows to be on, and the quality settings are global: what the scene's quality
			// level says (maybe "Disable Shadows" on a phone) must not decide whether an icon has one. Under the
			// preset's own settings, which may say more.
			m_shadowsActive = _preset != null && _preset.ShadowCatcher.Enabled;
			if (m_shadowsActive)
			{
				EnsureShadowSettings();
				m_shadowSettings.Apply();
			}

			m_activePresetSettings = _preset != null ? _preset.RenderSettings : null;
			m_presetSetsShadowDistance = m_activePresetSettings != null && m_activePresetSettings.ShadowDistance.Enabled;
			if (m_activePresetSettings != null)
				m_activePresetSettings.Apply();
		}

		public void Render( Camera _camera )
		{
			// The shadow map covers [camera, shadow distance]: as far as the camera sees is exactly what is needed, and a
			// single cascade over anything more spends its resolution on empty space
			if (m_shadowsActive && !m_presetSetsShadowDistance)
				QualitySettings.shadowDistance = _camera.farClipPlane;

			_camera.Render();
		}

		public void EndEnvironment()
		{
			if (m_activePresetSettings != null)
				m_activePresetSettings.Restore();

			m_activePresetSettings = null;

			if (m_shadowsActive)
				m_shadowSettings.Restore();

			m_shadowsActive = false;
			m_baseline.Restore();
		}

		public void Dispose()
		{
			DestroyObject(m_baselineGo);
			DestroyObject(m_shadowGo);
			m_shadowGo = null;
			m_shadowSettings = null;
			DestroyObject(m_blackCube);
			m_baselineGo = null;
			m_baseline = null;
			m_blackCube = null;
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

		private void EnsureBaseline()
		{
			if (m_baseline != null)
				return;

			m_blackCube = new Cubemap(4, TextureFormat.RGBA32, false)
			{
				name = "Icon3D Black Reflection",
				hideFlags = HideFlags.HideAndDontSave
			};
			var black = new Color[16];
			for (int face = 0; face < 6; face++)
				m_blackCube.SetPixels(black, (CubemapFace)face);
			m_blackCube.Apply(false, true);

			// No camera on this GameObject: the settings are applied manually, never automatically
			m_baselineGo = new GameObject("Icon3D Environment Baseline") { hideFlags = HideFlags.HideAndDontSave };
			m_baseline = m_baselineGo.AddComponent<UiCameraRenderSettings>();
			Enable(m_baseline.AmbientMode, AmbientMode.Flat);
			Enable(m_baseline.AmbientLight, Color.black);
			Enable(m_baseline.AmbientIntensity, 1f);
			Enable(m_baseline.Fog, false);
			Enable(m_baseline.Skybox, null);
			Enable(m_baseline.DefaultReflectionMode, DefaultReflectionMode.Custom);
			Enable(m_baseline.CustomReflection, m_blackCube);
			Enable(m_baseline.ReflectionIntensity, 1f);
		}

		private static void Enable<T>( PerCameraSetting<T> _setting, T _value )
		{
			_setting.Value = _value;
			_setting.Enabled = true;
		}

		private static void DestroyObject( Object _obj )
		{
			if (_obj == null)
				return;

			if (Application.isPlaying)
				Object.Destroy(_obj);
			else
				Object.DestroyImmediate(_obj);
		}
	}
}
