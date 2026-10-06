using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace GuiToolkit
{
	/// <summary>
	/// 3D icon backend for the High Definition Render Pipeline. Compiled only where the HDRP package is installed (see
	/// the asmdef); registers itself with <see cref="Icon3DBackends"/>.
	///
	/// HDRP has no <see cref="RenderSettings"/> environment, so what the other backends set there is rebuilt with a Volume
	/// of the icon stage that only the icon camera sees (layer mask), and that overrides what the project's default
	/// volume profile says:
	/// <list type="bullet">
	/// <item><b>Sky and ambient:</b> an HDRI sky from the preset's reflection cubemap (black without one). It lights and
	/// reflects. The preset's flat ambient colour is not applied separately - HDRP derives ambient from the sky; author
	/// the reflection cubemap with the ambient mood in mind.</item>
	/// <item><b>Exposure:</b> fixed so that the exposure multiplier is 1; without it every icon would be adapted to
	/// its own brightness.</item>
	/// <item><b>Light units:</b> presets are authored in Built-in intensity (1 = white surface at full brightness). The
	/// preset lights are converted once to lux (candela for the other types) as intensity times pi: HDRP divides by pi in
	/// its diffuse term, so this is the Built-in image at exposure 1. (Measured in a linear colour space project: a white
	/// sphere under intensity 0.3 reads 0.608 encoded = 0.33 linear in HDRP, and the same in Built-in and URP there.)</item>
	/// <item><b>Camera:</b> clears to transparent; post processing, motion vectors, decals, screen space effects,
	/// volumetrics and the like are off in its frame settings. The colour buffer format of the HDRP asset needs an alpha
	/// channel (R16G16B16A16) for transparent icons.</item>
	/// </list>
	/// The shadow catcher is a SubShader of its own that asks HDRP's shadow loop for the main directional light.
	/// </summary>
	public class HdrpIcon3DBackend : Icon3DEnvironmentBackend
	{
		/// <summary>Exposure value for which HDRP's exposure multiplier is exactly 1: 1 / (1.2 * 2^EV).</summary>
		private const float UnitExposure = -0.26303440583f;

		private GameObject m_volumeGo;
		private Volume m_volume;
		private HDRISky m_sky;
		private HDShadowSettings m_shadowSettings;
		private readonly HashSet<Light> m_convertedLights = new();

		public override bool LightsHonourCullingMask => false;

		public override bool BlendedAlphaIsCorrect => true;

		/// <summary>
		/// Smooth edges by rendering twice as large and scaling down: with MSAA enabled in the camera's frame settings the
		/// image came out magnified and cropped (the top left quarter of the icon filling the texture), reproducibly, whatever
		/// the order of renders. Without MSAA the render is right, so the edges are antialiased the plain way.
		/// </summary>
		public override int ScratchScale => 2;

		private HDAdditionalCameraData m_cameraData;

		public override void SetupCamera( Camera _camera )
		{
			_camera.enabled = false;
			_camera.clearFlags = CameraClearFlags.SolidColor;
			_camera.allowHDR = true;
			_camera.useOcclusionCulling = false;

			var data = _camera.gameObject.GetComponent<HDAdditionalCameraData>() ?? _camera.gameObject.AddComponent<HDAdditionalCameraData>();
			data.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
			data.backgroundColorHDR = Color.clear;
			data.clearDepth = true;
			data.volumeLayerMask = 1 << UiIcon3DRenderer.Layer;
			data.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
			data.dithering = false;
			data.stopNaNs = false;

			// Only the icon's own Volume reaches the camera. Features that make an icon depend on more than its object
			// and its preset are switched off in the camera's frame settings.
			data.customRenderingSettings = true;
			m_cameraData = data;
			ref var settings = ref data.renderingPathCustomFrameSettings;
			var mask = data.renderingPathCustomFrameSettingsOverrideMask;
			foreach (var fieldName in s_disabledFields)
			{
				if (!System.Enum.TryParse<FrameSettingsField>(fieldName, out var field))
					continue;

				mask.mask[(uint)field] = true;
				settings.SetEnabled(field, false);
			}

			data.renderingPathCustomFrameSettingsOverrideMask = mask;
		}

		protected override void BeforeRender( Camera _camera )
		{
			// HDRP clears to its own colour, not Camera.backgroundColor, which is what the renderer varies (black and white
			// for the exact alpha path)
			// The shadow cascade covers [camera, max distance]: as far as the camera sees is what is needed, in one cascade
			if (m_shadowSettings != null)
				m_shadowSettings.maxShadowDistance.Override(_camera.farClipPlane);

			if (m_cameraData != null)
				m_cameraData.backgroundColorHDR = _camera.backgroundColor.linear;   // HDRP takes it as a linear colour, Camera.backgroundColor is not
		}

		// By name: the set of frame settings changes between HDRP versions (ProbeVolume became something else in 17), and a
		// feature that does not exist needs no switching off
		private static readonly string[] s_disabledFields =
		{
			"Postprocess", "AfterPostprocess", "Decals", "MotionVectors", "ObjectMotionVectors", "TransparentsWriteMotionVector",
			"Refraction", "Distortion", "SSR", "TransparentSSR", "SSAO", "SSGI", "ContactShadows", "ScreenSpaceShadows",
			"VolumetricClouds", "AtmosphericScattering", "Volumetrics", "ReprojectionForVolumetrics", "ProbeVolume", "RayTracing",
			"Water", "CustomPass", "ReflectionProbe", "PlanarProbe",
		};

		protected override void OnEnvironmentBegin( UiIcon3DPreset _preset )
		{
			EnsureVolume();
			m_volume.gameObject.layer = UiIcon3DRenderer.Layer;

			// The preset's reflection cubemap is the sky; black without one
			Texture cube = BlackCubemap;
			var settings = ActivePresetSettings;
			if (settings != null && settings.CustomReflection.Enabled && settings.CustomReflection.Value is Cubemap custom)
				cube = custom;

			m_sky.hdriSky.Override(cube);
			m_sky.multiplier.Override(settings != null && settings.ReflectionIntensity.Enabled ? settings.ReflectionIntensity.Value : 1f);

			if (_preset != null)
				ConvertLights(_preset.gameObject);
		}

		/// <summary>Objects bring their own lights; those are authored for the project and left as they are.</summary>
		private void ConvertLights( GameObject _presetRoot )
		{
			foreach (var light in _presetRoot.GetComponentsInChildren<Light>(true))
			{
				if (!m_convertedLights.Add(light))
					continue;

				float builtinIntensity = light.intensity;
				var hd = light.GetComponent<HDAdditionalLightData>() ?? light.gameObject.AddComponent<HDAdditionalLightData>();
				if (light.type == LightType.Directional)
					hd.SetIntensity(builtinIntensity * Mathf.PI, LightUnit.Lux);
				else
					hd.SetIntensity(builtinIntensity * Mathf.PI, LightUnit.Candela);
			}
		}

		private void EnsureVolume()
		{
			if (m_volume != null)
				return;

			m_volumeGo = new GameObject("Icon3D Volume") { hideFlags = HideFlags.HideAndDontSave, layer = UiIcon3DRenderer.Layer };
			m_volume = m_volumeGo.AddComponent<Volume>();
			m_volume.isGlobal = true;
			m_volume.priority = 1000;
			m_volume.weight = 1;

			var profile = ScriptableObject.CreateInstance<VolumeProfile>();
			profile.hideFlags = HideFlags.HideAndDontSave;
			m_volume.sharedProfile = profile;

			var environment = profile.Add<VisualEnvironment>(true);
			environment.skyType.Override((int)SkyType.HDRI);
			environment.skyAmbientMode.Override(SkyAmbientMode.Dynamic);

			m_sky = profile.Add<HDRISky>(true);
			m_sky.exposure.Override(0);

			var exposure = profile.Add<Exposure>(true);
			exposure.mode.Override(ExposureMode.Fixed);
			exposure.fixedExposure.Override(UnitExposure);

			m_shadowSettings = profile.Add<HDShadowSettings>(true);
			m_shadowSettings.cascadeShadowSplitCount.Override(1);

			var fog = profile.Add<Fog>(true);
			fog.enabled.Override(false);
		}

		public override void Dispose()
		{
			if (m_volume != null && m_volume.sharedProfile != null)
				DestroyObject(m_volume.sharedProfile);
			DestroyObject(m_volumeGo);
			m_volumeGo = null;
			m_volume = null;
			m_sky = null;
			m_shadowSettings = null;
			m_convertedLights.Clear();
			base.Dispose();
		}
	}

	internal static class HdrpIcon3DBackendRegistration
	{
		private static void Register() =>
			Icon3DBackends.Register("HDRP", _pipeline => _pipeline is HDRenderPipelineAsset, () => new HdrpIcon3DBackend());

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void RegisterAtRuntime() => Register();

#if UNITY_EDITOR
		// Edit mode (icons render there too) never runs the runtime hook
		[UnityEditor.InitializeOnLoadMethod]
		private static void RegisterInEditor() => Register();
#endif
	}
}
