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
