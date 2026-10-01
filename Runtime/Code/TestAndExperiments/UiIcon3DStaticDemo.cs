using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Minimal visual check for <see cref="UiIcon3DRenderer"/> (phase 1 of the 3D icon plan): shows one static icon
	/// in the RawImage on this GameObject, in edit and play mode. Not the final component - that is UiIcon3D (phase 2).
	/// Assign the UI_Icon3D material to the RawImage, otherwise the edges get dark fringes.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(RawImage))]
	public class UiIcon3DStaticDemo : MonoBehaviour
	{
		[SerializeField] private GameObject m_prefab;
		[SerializeField] private UiIcon3DPreset m_preset;
		[SerializeField] private Vector2Int m_size = new(256, 256);

		private Icon3DHandle m_handle;
		private RawImage m_rawImage;

		private void OnEnable()
		{
			m_rawImage = GetComponent<RawImage>();
			Request();
		}

		private void OnDisable()
		{
			ReleaseHandle();
		}

		private void OnValidate()
		{
			if (isActiveAndEnabled)
				Request();
		}

		private void Update()
		{
			// The texture object only changes when the size changes, but it may appear one frame later
			if (m_handle != null && m_rawImage.texture != m_handle.Texture)
				m_rawImage.texture = m_handle.Texture;
		}

		private void Request()
		{
			ReleaseHandle();
			if (m_prefab == null)
				return;

			m_handle = UiIcon3DRenderer.RenderStatic(m_prefab, m_preset, m_size);
			m_rawImage.texture = m_handle.Texture;
		}

		private void ReleaseHandle()
		{
			m_handle?.Release();
			m_handle = null;
			if (m_rawImage != null)
				m_rawImage.texture = null;
		}
	}
}
