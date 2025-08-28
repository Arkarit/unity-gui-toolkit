using System;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSpriteHolder : MonoBehaviour
	{
		[SerializeField] [HideInInspector] private Texture2D m_texture;
		[SerializeField] [HideInInspector] private Sprite m_sprite;
		
		private static int m_counter;
		
		public Texture2D Texture => m_texture;
		public Sprite Sprite => m_sprite;
		
		public void Create(string _name = null, int _width = -1, int _height = -1)
		{
			Destroy();
			
			if (string.IsNullOrEmpty(_name))
			{
				_name = $"__sprite_{m_counter++}__";
			}
			
			if (_width == -1)
				_width = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenWidth()));
			if (_height == -1)
				_height = Mathf.Max(64, Mathf.RoundToInt(UiUtility.ScreenHeight()));
			
			m_texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false, false);
			m_sprite = Sprite.Create(m_texture, new Rect(0, 0, _width, _height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
			m_sprite.name = _name;
		}
		
		public void Destroy()
		{
			m_sprite.SafeDestroy();
			m_sprite = null;
			m_texture.SafeDestroy();
			m_texture = null;
		}

		private void OnDestroy() => Destroy();
	}
}