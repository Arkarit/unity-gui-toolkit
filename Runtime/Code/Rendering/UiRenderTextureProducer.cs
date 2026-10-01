using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Makes a camera render into the managed render texture for a keyword.
	/// See <see cref="RenderTextureManager"/>.
	///
	/// The producer owns <c>Camera.enabled</c> while it is active: a camera without target texture would
	/// otherwise render to the screen. With <see cref="RenderContinuously"/> off, the camera stays disabled
	/// and only renders when someone calls <c>Camera.Render()</c>.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(Camera))]
	public class UiRenderTextureProducer : MonoBehaviour, IRenderTextureProducer
	{
		[SerializeField] private string m_key;
		[SerializeField] private RenderTextureSpec m_spec = RenderTextureSpec.Default;
		[Tooltip("Enable the camera while a texture exists. Turn off for cameras rendered manually via Camera.Render().")]
		[SerializeField] private bool m_renderContinuously = true;

		private Camera m_camera;
		private string m_registeredKey;
		private bool m_cameraWasEnabled;
		private RenderTexture m_texture;

		public Camera Camera
		{
			get
			{
				if (m_camera == null)
					m_camera = GetComponent<Camera>();
				return m_camera;
			}
		}

		public RenderTexture Texture => m_texture;

		public string Key
		{
			get => m_key;
			set
			{
				if (m_key == value)
					return;

				m_key = value;
				Reregister();
			}
		}

		public RenderTextureSpec Spec
		{
			get => m_spec;
			set
			{
				if (m_spec.Equals(value))
					return;

				m_spec = value;
				RenderTextureManager.SetDirty(m_registeredKey);
			}
		}

		public bool RenderContinuously
		{
			get => m_renderContinuously;
			set
			{
				if (m_renderContinuously == value)
					return;

				m_renderContinuously = value;
				UpdateCamera();
			}
		}

		RenderTextureSpec IRenderTextureProducer.RenderTextureSpec => m_spec;

		public void OnRenderTextureChanged( RenderTexture _texture )
		{
			m_texture = _texture;
			UpdateCamera();
		}

		protected virtual void OnEnable()
		{
			m_cameraWasEnabled = Camera.enabled;
			m_texture = null;
			UpdateCamera();
			Register();
		}

		protected virtual void OnDisable()
		{
			Unregister();
			m_texture = null;
			if (Camera != null)
			{
				Camera.targetTexture = null;
				Camera.enabled = m_cameraWasEnabled;
			}
		}

		protected virtual void OnValidate()
		{
			if (!isActiveAndEnabled)
				return;

			if (m_key != m_registeredKey)
				Reregister();
			else
				RenderTextureManager.SetDirty(m_registeredKey);

			UpdateCamera();
		}

		private void UpdateCamera()
		{
			var cam = Camera;
			if (cam == null)
				return;

			cam.targetTexture = m_texture;
			cam.enabled = m_texture != null && m_renderContinuously;
		}

		private void Reregister()
		{
			if (!isActiveAndEnabled)
				return;

			Unregister();
			Register();
		}

		private void Register()
		{
			if (string.IsNullOrEmpty(m_key))
				return;

			m_registeredKey = m_key;
			RenderTextureManager.RegisterProducer(m_registeredKey, this);
		}

		private void Unregister()
		{
			if (string.IsNullOrEmpty(m_registeredKey))
				return;

			// Detach the camera before the manager may release the texture
			m_texture = null;
			UpdateCamera();
			RenderTextureManager.UnregisterProducer(m_registeredKey, this);
			m_registeredKey = null;
		}
	}
}
