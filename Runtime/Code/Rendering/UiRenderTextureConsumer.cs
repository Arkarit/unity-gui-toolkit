using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Base for components that display the managed render texture for a keyword.
	/// See <see cref="RenderTextureManager"/>.
	/// </summary>
	[ExecuteAlways]
	public abstract class UiRenderTextureConsumer : MonoBehaviour, IRenderTextureConsumer
	{
		[SerializeField] private string m_key;

		private string m_registeredKey;
		private RenderTexture m_texture;

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

		/// <summary>The current render texture, or null if there is none (the placeholder is shown then).</summary>
		public RenderTexture Texture => m_texture;

		public abstract Vector2Int RequestedSize { get; }

		/// <summary>Show the texture; _texture is never null (the placeholder replaces a missing render texture).</summary>
		protected abstract void ApplyTexture( Texture _texture );

		public void OnRenderTextureChanged( RenderTexture _texture )
		{
			m_texture = _texture;
			ApplyTexture(_texture != null ? _texture : RenderTextureManager.Placeholder);
		}

		protected virtual void OnEnable()
		{
			ApplyTexture(RenderTextureManager.Placeholder);
			Register();
		}

		protected virtual void OnDisable()
		{
			Unregister();
		}

		protected virtual void OnValidate()
		{
			if (isActiveAndEnabled && m_key != m_registeredKey)
				Reregister();
		}

		private void Reregister()
		{
			if (!isActiveAndEnabled)
				return;

			Unregister();
			ApplyTexture(RenderTextureManager.Placeholder);
			Register();
		}

		private void Register()
		{
			if (string.IsNullOrEmpty(m_key))
				return;

			m_registeredKey = m_key;
			RenderTextureManager.RegisterConsumer(m_registeredKey, this);
		}

		private void Unregister()
		{
			if (string.IsNullOrEmpty(m_registeredKey))
				return;

			RenderTextureManager.UnregisterConsumer(m_registeredKey, this);
			m_registeredKey = null;
			m_texture = null;
		}
	}
}
