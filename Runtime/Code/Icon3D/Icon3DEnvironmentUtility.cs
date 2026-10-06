using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Helpers for 3D icon environments.
	/// </summary>
	public static class Icon3DEnvironmentUtility
	{
		/// <summary>
		/// A simple studio reflection: sky colour above, horizon colour around, ground colour below.
		///
		/// Presets need a reflection of their own: the neutral baseline reflection is black (like an empty scene),
		/// and a metallic surface shows almost nothing but its reflection - without one, metal renders black.
		/// The side faces are uniform, so the result does not depend on the cubemap face orientation;
		/// mip maps soften it for rough surfaces.
		/// </summary>
		public static Cubemap CreateGradientCubemap( Color _sky, Color _horizon, Color _ground, int _size = 16 )
		{
			_size = Mathf.Max(1, _size);
			var result = new Cubemap(_size, TextureFormat.RGBA32, true)
			{
				name = "Icon3D Gradient Reflection",
				wrapMode = TextureWrapMode.Clamp,
				filterMode = FilterMode.Trilinear,
			};

			var pixels = new Color[_size * _size];
			Fill(result, CubemapFace.PositiveY, pixels, _sky);
			Fill(result, CubemapFace.NegativeY, pixels, _ground);
			Fill(result, CubemapFace.PositiveX, pixels, _horizon);
			Fill(result, CubemapFace.NegativeX, pixels, _horizon);
			Fill(result, CubemapFace.PositiveZ, pixels, _horizon);
			Fill(result, CubemapFace.NegativeZ, pixels, _horizon);
			result.Apply(true);
			return result;
		}

		private static void Fill( Cubemap _cubemap, CubemapFace _face, Color[] _pixels, Color _color )
		{
			for (int i = 0; i < _pixels.Length; i++)
				_pixels[i] = _color;

			_cubemap.SetPixels(_pixels, _face);
		}
	}
}
