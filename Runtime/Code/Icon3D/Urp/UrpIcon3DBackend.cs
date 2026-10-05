using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GuiToolkit
{
	/// <summary>
	/// 3D icon backend for the Universal Render Pipeline. Compiled only where the URP package is installed (see the
	/// asmdef); registers itself with <see cref="Icon3DBackends"/>.
	///
	/// What is different from Built-in:
	/// <list type="bullet">
	/// <item>The camera carries URP data: no post processing, no anti aliasing pass, no volumes, no opaque or depth
	/// copy. The scene's volumes must not touch an icon.</item>
	/// <item>Lights ignore <c>cullingMask</c>; the renderer switches the scene's lights off for the batch
	/// (<see cref="LightsHonourCullingMask"/>).</item>
	/// <item>Shadows are set on the pipeline asset, not in QualitySettings: for a shadow catcher one cascade over exactly the
	/// camera's depth range is used for this render only. Main light shadows must be enabled in the asset (a warning says so).</item>
	/// <item>The icon uses the pipeline's default renderer: renderer features of it (SSAO, full screen passes) apply.</item>
	/// </list>
	/// </summary>
	public class UrpIcon3DBackend : Icon3DEnvironmentBackend
	{
		private bool m_hasSavedAsset;
		private float m_savedShadowDistance;
		private int m_savedCascades;
		private bool m_warnedNoShadows;

		public override bool LightsHonourCullingMask => false;

		public override void SetupCamera( Camera _camera )
		{
			_camera.enabled = false;
			_camera.clearFlags = CameraClearFlags.SolidColor;
			_camera.allowHDR = false;
			_camera.allowMSAA = true;
			_camera.useOcclusionCulling = false;

			var data = _camera.GetUniversalAdditionalCameraData();
			data.renderType = CameraRenderType.Base;
			data.renderPostProcessing = false;
			data.antialiasing = AntialiasingMode.None;
			data.renderShadows = true;
			data.requiresColorOption = CameraOverrideOption.Off;
			data.requiresDepthOption = CameraOverrideOption.Off;
			data.volumeLayerMask = 0;
			data.dithering = false;
			data.stopNaN = false;
		}

		protected override void BeginShadows( UiIcon3DPreset _preset )
		{
			var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
			if (asset == null)
				return;

			// Only distance and cascade count can be set on the asset from outside; whether main light shadows exist at all,
			// their resolution and softness are the project's own choice (and cost) and stay as they are
			if (!asset.supportsMainLightShadows && !m_warnedNoShadows)
			{
				m_warnedNoShadows = true;
				UiLog.LogWarning($"3D icons: the URP asset '{asset.name}' has main light shadows disabled - a preset's shadow catcher shows nothing. Enable them in the asset (Lighting > Main Light > Cast Shadows).");
			}

			m_hasSavedAsset = true;
			m_savedShadowDistance = asset.shadowDistance;
			m_savedCascades = asset.shadowCascadeCount;
			asset.shadowCascadeCount = 1;
		}

		protected override void BeforeRender( Camera _camera )
		{
			if (!m_hasSavedAsset || PresetSetsShadowDistance)
				return;

			// The shadow map covers [camera, shadow distance]: as far as the camera sees is exactly what is needed
			var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
			if (asset != null)
				asset.shadowDistance = _camera.farClipPlane;
		}

		protected override void EndShadows()
		{
			if (!m_hasSavedAsset)
				return;

			m_hasSavedAsset = false;
			var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
			if (asset == null)
				return;

			asset.shadowDistance = m_savedShadowDistance;
			asset.shadowCascadeCount = m_savedCascades;
		}
	}

	internal static class UrpIcon3DBackendRegistration
	{
		private static void Register() =>
			Icon3DBackends.Register("URP", _pipeline => _pipeline is UniversalRenderPipelineAsset, () => new UrpIcon3DBackend());

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void RegisterAtRuntime() => Register();

#if UNITY_EDITOR
		// Edit mode (icons render there too) never runs the runtime hook
		[UnityEditor.InitializeOnLoadMethod]
		private static void RegisterInEditor() => Register();
#endif
	}
}
