using UnityEngine;

namespace GuiToolkit
{
	public static class UiMath
	{
		// Note: In this implementation, only the absolute necessary values position and uv0 are lerped due to performance reasons,
		public static UIVertex Bilerp( UIVertex _bl, UIVertex _tl, UIVertex _tr, UIVertex _br, float _h, float _v )
		{
			UIVertex result = _bl;
			result.position = Bilerp(_bl.position, _tl.position, _tr.position, _br.position, _h, _v);
			result.uv0 = Bilerp(_bl.uv0, _tl.uv0, _tr.uv0, _br.uv0, _h, _v);
			return result;
		}

		public static float Bilerp( float _bl, float _tl, float _tr, float _br, float _h, float _v )
		{
			float top = Mathf.Lerp(_tl, _tr, _h);
			float bottom = Mathf.Lerp(_bl, _br, _h);
			return Mathf.Lerp(bottom, top, _v);
		}

		public static Vector2 Bilerp( Vector2 _bl, Vector2 _tl, Vector2 _tr, Vector2 _br, float _h, float _v )
		{
			Vector2 top = Vector2.Lerp(_tl, _tr, _h);
			Vector2 bottom = Vector2.Lerp(_bl, _br, _h);
			return Vector2.Lerp(bottom, top, _v);
		}

		public static Vector3 Bilerp( Vector3 _bl, Vector3 _tl, Vector3 _tr, Vector3 _br, float _h, float _v )
		{
			Vector3 top = Vector3.Lerp(_tl, _tr, _h);
			Vector3 bottom = Vector3.Lerp(_bl, _br, _h);
			return Vector3.Lerp(bottom, top, _v);
		}

		public static Vector4 Bilerp( Vector4 _bl, Vector4 _tl, Vector4 _tr, Vector4 _br, float _h, float _v )
		{
			Vector4 top = Vector4.Lerp(_tl, _tr, _h);
			Vector4 bottom = Vector4.Lerp(_bl, _br, _h);
			return Vector4.Lerp(bottom, top, _v);
		}

		public static Color Bilerp( Color _bl, Color _tl, Color _tr, Color _br, float _h, float _v )
		{
			Color top = Color.Lerp(_tl, _tr, _h);
			Color bottom = Color.Lerp(_bl, _br, _h);
			return Color.Lerp(bottom, top, _v);
		}
	}
}
