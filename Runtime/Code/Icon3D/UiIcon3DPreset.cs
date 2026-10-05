using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Lighting mood and framing for 3D icons, authored as a prefab.
	///
	/// The prefab contains the lights as children and, optionally, a <see cref="UiCameraRenderSettings"/> on the same
	/// GameObject for ambient, fog, reflection and quality settings. It must not contain a camera.
	///
	/// <b>Lights are camera relative.</b> Author them as if the icon camera looked along this GameObject's +Z axis;
	/// the preset is rotated with the view, so a key light stays top left whatever direction the object is seen from.
	///
	/// Everything a preset does not set is neutral, never inherited from the scene: the renderer applies a clean
	/// baseline (black ambient, no fog, black reflection) before the preset.
	/// </summary>
	[DisallowMultipleComponent]
	public class UiIcon3DPreset : MonoBehaviour
	{
		public enum EProjection
		{
			Perspective,
			Orthographic,
		}

		public enum EFitMode
		{
			/// <summary>
			/// Sphere around the bounding box: the framing does not change when the object rotates (turntables).
			/// Conservative by design - a rotating cube needs exactly this sphere, a round object looks up to
			/// sqrt(3) smaller than with <see cref="Box"/>. A tighter sphere would need mesh vertices, which are
			/// not readable in builds for most meshes.
			/// </summary>
			Sphere,
			/// <summary>Bounding box corners: tighter, best for static icons.</summary>
			Box,
		}

		public enum EAlphaMode
		{
			/// <summary>Exact where the object has transparent materials (render queue 3000 and up), fast everywhere else.</summary>
			Auto,
			/// <summary>One render. Transparent materials of the object leave a wrong alpha (a 50% layer shows as 25%).</summary>
			Fast,
			/// <summary>Always two renders (over black and over white) and the alpha derived from both: right for any material.</summary>
			Exact,
		}

		/// <summary>
		/// An invisible ground under the object that only shows the shadow the preset's light casts on it, written into
		/// the icon's alpha. Needs a DIRECTIONAL light in the preset that does not come from the camera's direction (a
		/// shadow behind the object is hidden by it): author the key light from above and the side. Point and spot lights
		/// cast no catcher shadow. Shadows of the preset's directional lights are switched on automatically.
		/// </summary>
		[System.Serializable]
		public class ShadowCatcherSettings
		{
			public bool Enabled;
			[Tooltip("How dark the shadow gets (alpha at its darkest).")]
			[Range(0, 1)] public float Strength = 0.6f;
			public Color Color = Color.black;
			[Tooltip("Half size of the ground, as multiple of the object's radius. The shadow fades out towards its edge.")]
			[Min(0.5f)] public float Extent = 2.5f;
			[Tooltip("Ground height below the object's lowest point, as fraction of its height.")]
			public float Offset = 0f;
		}

		[Tooltip("Rotation of the icon camera (euler angles). Unity objects face +Z, so (0,180,0) looks at an object's front and (90,0,0) looks down on it.")]
		[SerializeField] private Vector3 m_viewRotation = new(15, 150, 0);
		[SerializeField] private EProjection m_projection = EProjection.Perspective;
		[SerializeField][Range(1, 120)] private float m_fieldOfView = 25;
		[SerializeField] private EFitMode m_fitMode = EFitMode.Box;
		[Tooltip("Empty space around the object, as fraction of the icon size")]
		[SerializeField][Range(0, 0.5f)] private float m_padding = 0.05f;
		[Tooltip("Background; alpha 0 for a transparent icon")]
		[SerializeField] private Color m_backgroundColor = Color.clear;
		[Tooltip("Exact doubles the render cost of an icon (once over black, once over white) and fixes the alpha of transparent materials. Auto uses it only for objects that have some. Ignored with an opaque background.")]
		[SerializeField] private EAlphaMode m_alphaMode = EAlphaMode.Auto;
		[SerializeField] private ShadowCatcherSettings m_shadowCatcher = new();

		public Quaternion ViewRotation
		{
			get => Quaternion.Euler(m_viewRotation);
			set => m_viewRotation = value.eulerAngles;
		}

		public EProjection Projection
		{
			get => m_projection;
			set => m_projection = value;
		}

		public float FieldOfView
		{
			get => m_fieldOfView;
			set => m_fieldOfView = Mathf.Clamp(value, 1, 120);
		}

		public EFitMode FitMode
		{
			get => m_fitMode;
			set => m_fitMode = value;
		}

		public float Padding
		{
			get => m_padding;
			set => m_padding = Mathf.Clamp(value, 0, 0.5f);
		}

		public Color BackgroundColor
		{
			get => m_backgroundColor;
			set => m_backgroundColor = value;
		}

		public ShadowCatcherSettings ShadowCatcher => m_shadowCatcher;

		public EAlphaMode AlphaMode
		{
			get => m_alphaMode;
			set => m_alphaMode = value;
		}

		/// <summary>Optional render settings on the same GameObject.</summary>
		public UiCameraRenderSettings RenderSettings => GetComponent<UiCameraRenderSettings>();
	}
}
