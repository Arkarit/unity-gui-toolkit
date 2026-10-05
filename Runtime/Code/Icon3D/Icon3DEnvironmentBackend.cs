using UnityEngine;
using UnityEngine.Rendering;

namespace GuiToolkit
{
	/// <summary>
	/// What every 3D icon backend shares: the environment. A neutral baseline (black flat ambient, no fog, no skybox,
	/// black custom reflection) is applied first, the preset's <see cref="UiCameraRenderSettings"/> on top, both are
	/// restored after the render. The baseline is what an empty scene would give - the scene isolation tests compare
	/// against exactly that. <see cref="RenderSettings"/> are global in every pipeline, so this part is the same for
	/// Built-in, URP and HDRP.
	///
	/// What differs is the camera, the shadow settings (QualitySettings in Built-in, the pipeline asset in URP and
	/// HDRP) and whether lights honour their culling mask; derived classes fill that in.
	/// </summary>
	public abstract class Icon3DEnvironmentBackend : IIcon3DRenderBackend
	{
		private GameObject m_baselineGo;
		private UiCameraRenderSettings m_baseline;
		private Cubemap m_blackCube;
		private UiCameraRenderSettings m_activePresetSettings;

		/// <summary>True while the current render has a shadow catcher: shadows must be on whatever the scene's quality says.</summary>
		protected bool ShadowsRequested { get; private set; }

		/// <summary>True if the preset's own <see cref="UiCameraRenderSettings"/> sets the shadow distance (then it wins over ours).</summary>
		protected bool PresetSetsShadowDistance { get; private set; }

		public abstract void SetupCamera( Camera _camera );

		public virtual bool LightsHonourCullingMask => true;

		public void BeginEnvironment( UiIcon3DPreset _preset )
		{
			EnsureBaseline();
			m_baseline.Apply();

			// The shadow catcher needs shadows to be on, and the shadow settings are global: what the scene's quality
			// level says (maybe "Disable Shadows" on a phone) must not decide whether an icon has one.
			ShadowsRequested = _preset != null && _preset.ShadowCatcher.Enabled;
			if (ShadowsRequested)
				BeginShadows(_preset);

			// Under the preset's own settings, which may say more
			m_activePresetSettings = _preset != null ? _preset.RenderSettings : null;
			PresetSetsShadowDistance = m_activePresetSettings != null && m_activePresetSettings.ShadowDistance.Enabled;
			if (m_activePresetSettings != null)
				m_activePresetSettings.Apply();
		}

		public void Render( Camera _camera )
		{
			BeforeRender(_camera);
			RenderCamera(_camera);
		}

		public void EndEnvironment()
		{
			if (m_activePresetSettings != null)
				m_activePresetSettings.Restore();

			m_activePresetSettings = null;

			if (ShadowsRequested)
				EndShadows();

			ShadowsRequested = false;
			m_baseline.Restore();
		}

		public virtual void Dispose()
		{
			DestroyObject(m_baselineGo);
			DestroyObject(m_blackCube);
			m_baselineGo = null;
			m_baseline = null;
			m_blackCube = null;
		}

		/// <summary>Switch shadows on for this render (and remember what to restore). Called only when the preset has a shadow catcher.</summary>
		protected virtual void BeginShadows( UiIcon3DPreset _preset ) { }

		protected virtual void EndShadows() { }

		/// <summary>Last chance to adjust things that depend on the framed camera, e.g. the shadow distance.</summary>
		protected virtual void BeforeRender( Camera _camera ) { }

		protected virtual void RenderCamera( Camera _camera ) => _camera.Render();

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

		protected static void Enable<T>( PerCameraSetting<T> _setting, T _value )
		{
			_setting.Value = _value;
			_setting.Enabled = true;
		}

		protected static void DestroyObject( Object _obj )
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
