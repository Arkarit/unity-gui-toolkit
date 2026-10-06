using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Render pipeline specific part of the 3D icon renderer.
	/// Built-in: <see cref="BuiltinIcon3DBackend"/>. URP and HDRP get their own implementations later;
	/// the renderer itself does not know which pipeline it runs on.
	/// </summary>
	public interface IIcon3DRenderBackend
	{
		/// <summary>
		/// Whether <c>Light.cullingMask</c> keeps scene lights off the icons. Built-in does; URP and HDRP ignore it, so the
		/// renderer switches the scene's lights off for the duration of the batch instead.
		/// </summary>
		bool LightsHonourCullingMask { get; }

		/// <summary>
		/// Whether this pipeline's own transparent shaders leave the right alpha (URP and HDRP blend alpha with One /
		/// OneMinusSrcAlpha; Built-in's SrcAlpha / OneMinusSrcAlpha squares it). Where they do, the two render path for
		/// transparent materials (<see cref="UiIcon3DPreset.EAlphaMode.Auto"/>) is not needed.
		/// </summary>
		bool BlendedAlphaIsCorrect { get; }

		/// <summary>
		/// Size of the scratch render relative to the icon (1 = same). 2 renders four times the pixels and averages them down:
		/// antialiasing for a pipeline whose MSAA can not be used.
		/// </summary>
		int ScratchScale { get; }

		/// <summary>
		/// Whether presets can have a shadow catcher. Without it the renderer skips the catcher and says so once.
		/// </summary>
		bool SupportsShadowCatcher { get; }

		/// <summary>One-time setup of the stage camera.</summary>
		void SetupCamera( Camera _camera );

		/// <summary>
		/// Make the environment exactly what the preset says - nothing from the scene may come through.
		/// Called right before <see cref="Render"/>; every call is followed by <see cref="EndEnvironment"/>.
		/// _preset may be null (neutral environment).
		/// </summary>
		void BeginEnvironment( UiIcon3DPreset _preset );

		void Render( Camera _camera );

		/// <summary>Restore what <see cref="BeginEnvironment"/> changed.</summary>
		void EndEnvironment();

		/// <summary>Release everything the backend created (called when the stage is torn down).</summary>
		void Dispose();
	}
}
