using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuiToolkit
{
	/// <summary>
	/// Per-camera overrides for <see cref="RenderSettings"/> (ambient, fog, skybox, reflections) and
	/// <see cref="QualitySettings"/> (shadows, pixel lights, LOD). Only enabled entries are applied.
	///
	/// Built-in pipeline notes:
	/// <list type="bullet">
	/// <item>Flat and trilight ambient take effect immediately. Skybox ambient is baked into the ambient probe,
	/// so switching the skybox does not change the ambient light; use flat or trilight for isolated lighting.</item>
	/// <item>QualitySettings are mostly ignored by SRPs, which take these values from their pipeline asset.</item>
	/// </list>
	/// </summary>
	public class UiCameraRenderSettings : UiAbstractPerCameraSettings
	{
		[Header("Ambient")]
		[SerializeField] private PerCameraSetting<UnityEngine.Rendering.AmbientMode> m_ambientMode = new(UnityEngine.Rendering.AmbientMode.Flat);
		[Tooltip("Flat ambient color, or sky color for trilight")]
		[SerializeField] private PerCameraSetting<Color> m_ambientLight = new(new Color(0.5f, 0.5f, 0.5f));
		[SerializeField] private PerCameraSetting<Color> m_ambientEquatorColor = new(new Color(0.4f, 0.4f, 0.4f));
		[SerializeField] private PerCameraSetting<Color> m_ambientGroundColor = new(new Color(0.2f, 0.2f, 0.2f));
		[SerializeField] private PerCameraSetting<float> m_ambientIntensity = new(1);

		[Header("Fog")]
		[SerializeField] private PerCameraSetting<bool> m_fog = new(false);
		[SerializeField] private PerCameraSetting<UnityEngine.FogMode> m_fogMode = new(UnityEngine.FogMode.ExponentialSquared);
		[SerializeField] private PerCameraSetting<Color> m_fogColor = new(Color.gray);
		[SerializeField] private PerCameraSetting<float> m_fogDensity = new(0.01f);
		[SerializeField] private PerCameraSetting<float> m_fogStartDistance = new(0);
		[SerializeField] private PerCameraSetting<float> m_fogEndDistance = new(300);

		[Header("Environment")]
		[SerializeField] private PerCameraSetting<Material> m_skybox = new(null);
		[SerializeField] private PerCameraSetting<UnityEngine.Rendering.DefaultReflectionMode> m_defaultReflectionMode = new(UnityEngine.Rendering.DefaultReflectionMode.Custom);
		[SerializeField] private PerCameraSetting<Texture> m_customReflection = new(null);
		[SerializeField] private PerCameraSetting<float> m_reflectionIntensity = new(1);

		[Header("Quality")]
		[SerializeField] private PerCameraSetting<ShadowQuality> m_shadows = new(ShadowQuality.All);
		[SerializeField] private PerCameraSetting<UnityEngine.ShadowResolution> m_shadowResolution = new(UnityEngine.ShadowResolution.High);
		[SerializeField] private PerCameraSetting<float> m_shadowDistance = new(20);
		[SerializeField] private PerCameraSetting<int> m_shadowCascades = new(1);
		[SerializeField] private PerCameraSetting<int> m_pixelLightCount = new(4);
		[SerializeField] private PerCameraSetting<float> m_lodBias = new(1);

		public PerCameraSetting<UnityEngine.Rendering.AmbientMode> AmbientMode => m_ambientMode;
		public PerCameraSetting<Color> AmbientLight => m_ambientLight;
		public PerCameraSetting<Color> AmbientEquatorColor => m_ambientEquatorColor;
		public PerCameraSetting<Color> AmbientGroundColor => m_ambientGroundColor;
		public PerCameraSetting<float> AmbientIntensity => m_ambientIntensity;
		public PerCameraSetting<bool> Fog => m_fog;
		public PerCameraSetting<UnityEngine.FogMode> FogMode => m_fogMode;
		public PerCameraSetting<Color> FogColor => m_fogColor;
		public PerCameraSetting<float> FogDensity => m_fogDensity;
		public PerCameraSetting<float> FogStartDistance => m_fogStartDistance;
		public PerCameraSetting<float> FogEndDistance => m_fogEndDistance;
		public PerCameraSetting<Material> Skybox => m_skybox;
		public PerCameraSetting<UnityEngine.Rendering.DefaultReflectionMode> DefaultReflectionMode => m_defaultReflectionMode;
		public PerCameraSetting<Texture> CustomReflection => m_customReflection;
		public PerCameraSetting<float> ReflectionIntensity => m_reflectionIntensity;
		public PerCameraSetting<ShadowQuality> Shadows => m_shadows;
		public PerCameraSetting<UnityEngine.ShadowResolution> ShadowResolution => m_shadowResolution;
		public PerCameraSetting<float> ShadowDistance => m_shadowDistance;
		public PerCameraSetting<int> ShadowCascades => m_shadowCascades;
		public PerCameraSetting<int> PixelLightCount => m_pixelLightCount;
		public PerCameraSetting<float> LodBias => m_lodBias;

		protected override void CollectSettings( List<PerCameraSettingBase> _settings )
		{
			// Mode before colors: setting a color recomputes the ambient probe for the current mode
			_settings.Add(m_ambientMode.Bind(() => RenderSettings.ambientMode, v => RenderSettings.ambientMode = v));
			_settings.Add(m_ambientLight.Bind(() => RenderSettings.ambientLight, v => RenderSettings.ambientLight = v));
			_settings.Add(m_ambientEquatorColor.Bind(() => RenderSettings.ambientEquatorColor, v => RenderSettings.ambientEquatorColor = v));
			_settings.Add(m_ambientGroundColor.Bind(() => RenderSettings.ambientGroundColor, v => RenderSettings.ambientGroundColor = v));
			_settings.Add(m_ambientIntensity.Bind(() => RenderSettings.ambientIntensity, v => RenderSettings.ambientIntensity = v));

			_settings.Add(m_fog.Bind(() => RenderSettings.fog, v => RenderSettings.fog = v));
			_settings.Add(m_fogMode.Bind(() => RenderSettings.fogMode, v => RenderSettings.fogMode = v));
			_settings.Add(m_fogColor.Bind(() => RenderSettings.fogColor, v => RenderSettings.fogColor = v));
			_settings.Add(m_fogDensity.Bind(() => RenderSettings.fogDensity, v => RenderSettings.fogDensity = v));
			_settings.Add(m_fogStartDistance.Bind(() => RenderSettings.fogStartDistance, v => RenderSettings.fogStartDistance = v));
			_settings.Add(m_fogEndDistance.Bind(() => RenderSettings.fogEndDistance, v => RenderSettings.fogEndDistance = v));

			_settings.Add(m_skybox.Bind(() => RenderSettings.skybox, v => RenderSettings.skybox = v));
			_settings.Add(m_defaultReflectionMode.Bind(() => RenderSettings.defaultReflectionMode, v => RenderSettings.defaultReflectionMode = v));
			_settings.Add(m_customReflection.Bind(() => RenderSettings.customReflectionTexture, v => RenderSettings.customReflectionTexture = v));
			_settings.Add(m_reflectionIntensity.Bind(() => RenderSettings.reflectionIntensity, v => RenderSettings.reflectionIntensity = v));

			_settings.Add(m_shadows.Bind(() => QualitySettings.shadows, v => QualitySettings.shadows = v));
			_settings.Add(m_shadowResolution.Bind(() => QualitySettings.shadowResolution, v => QualitySettings.shadowResolution = v));
			_settings.Add(m_shadowDistance.Bind(() => QualitySettings.shadowDistance, v => QualitySettings.shadowDistance = v));
			_settings.Add(m_shadowCascades.Bind(() => QualitySettings.shadowCascades, v => QualitySettings.shadowCascades = v));
			_settings.Add(m_pixelLightCount.Bind(() => QualitySettings.pixelLightCount, v => QualitySettings.pixelLightCount = v));
			_settings.Add(m_lodBias.Bind(() => QualitySettings.lodBias, v => QualitySettings.lodBias = v));
		}
	}
}
