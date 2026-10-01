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

		[Tooltip("Rotation of the icon camera (euler angles). Unity objects face +Z, so (0,180,0) looks at an object's front and (90,0,0) looks down on it.")]
		[SerializeField] private Vector3 m_viewRotation = new(15, 150, 0);
		[SerializeField] private EProjection m_projection = EProjection.Perspective;
		[SerializeField][Range(1, 120)] private float m_fieldOfView = 25;
		[SerializeField] private EFitMode m_fitMode = EFitMode.Box;
		[Tooltip("Empty space around the object, as fraction of the icon size")]
		[SerializeField][Range(0, 0.5f)] private float m_padding = 0.05f;
		[Tooltip("Background; alpha 0 for a transparent icon")]
		[SerializeField] private Color m_backgroundColor = Color.clear;

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

		/// <summary>Optional render settings on the same GameObject.</summary>
		public UiCameraRenderSettings RenderSettings => GetComponent<UiCameraRenderSettings>();
	}
}
