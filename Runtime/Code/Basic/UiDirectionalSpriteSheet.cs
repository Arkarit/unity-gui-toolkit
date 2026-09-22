using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Describes a sprite sheet that holds one animation rendered from several viewing directions:
	/// one row per direction, one column per frame.
	///
	/// The sheet is addressed by UV rectangle rather than by sliced Sprite assets, so a 16x16 sheet is
	/// one texture and two integers instead of 256 sub-assets. <see cref="GetUvRect"/> returns something
	/// that goes straight into RawImage.uvRect.
	///
	/// Direction angles are stored explicitly and need not be evenly spaced. Rendered sprite sets often
	/// are not: a common 16-direction set samples the union of a 45 degree and a 30 degree grid, so its
	/// gaps alternate between 30 and 15 degrees and no amount of dividing by a step count will find the
	/// right row.
	/// </summary>
	[CreateAssetMenu(fileName = "DirectionalSpriteSheet", menuName = StringConstants.CREATE_DIRECTIONAL_SPRITE_SHEET)]
	public class UiDirectionalSpriteSheet : ScriptableObject
	{
		[Tooltip("The sheet. Rows are directions (top row = first entry in Direction Angles), columns are frames.")]
		public Texture2D Texture;

		[Tooltip("Number of frames, i.e. the number of columns.")]
		public int FrameCount = 1;

		[Tooltip("Viewing angle of each row, in degrees. 0 = the subject faces up the screen, "
		         + "angles increase clockwise. Need not be evenly spaced or sorted.")]
		public float[] DirectionAngles = { 0f };

		[Tooltip("Where the subject actually stands inside a cell, normalised, (0,0) = bottom left. "
		         + "Applied to the RectTransform pivot so the thing the sprite stands on lines up with "
		         + "the transform's position.")]
		public Vector2 Pivot = new Vector2(0.5f, 0.5f);

		[Tooltip("Distance the subject travels per frame, in the units the sheet is used at, at its "
		         + "natural pace. Drives the frame from distance covered rather than from a clock, which "
		         + "is what stops the legs sliding when the speed changes.")]
		public float UnitsPerFrame = 8f;

		/// <summary>Number of rows.</summary>
		public int DirectionCount => DirectionAngles != null ? DirectionAngles.Length : 0;

		public bool IsValid => Texture != null && FrameCount > 0 && DirectionCount > 0;

		/// <summary>
		/// UV rectangle of one cell, ready for RawImage.uvRect. Row 0 is the top row of the image, which
		/// is the high end of V.
		/// </summary>
		public Rect GetUvRect( int _direction, int _frame )
		{
			int rows = Mathf.Max(1, DirectionCount);
			int columns = Mathf.Max(1, FrameCount);

			int row = Mathf.Clamp(_direction, 0, rows - 1);
			int column = ((_frame % columns) + columns) % columns;

			float w = 1f / columns;
			float h = 1f / rows;

			return new Rect(column * w, 1f - (row + 1) * h, w, h);
		}

		/// <summary>
		/// Row whose viewing angle is closest to <paramref name="_angleDegrees"/>. A plain scan, because
		/// the angles are not evenly spaced and there are at most a couple of dozen of them.
		/// </summary>
		public int NearestDirection( float _angleDegrees )
		{
			int count = DirectionCount;
			if (count == 0)
				return 0;

			int best = 0;
			float bestDelta = float.MaxValue;

			for (int i = 0; i < count; i++)
			{
				float delta = Mathf.Abs(Mathf.DeltaAngle(_angleDegrees, DirectionAngles[i]));
				if (delta < bestDelta)
				{
					bestDelta = delta;
					best = i;
				}
			}

			return best;
		}

		/// <summary>
		/// Screen heading of a direction vector, in the convention the rows use: 0 = up, growing
		/// clockwise. Note the argument order - this is not the usual Atan2(y, x), which measures from
		/// +X counter-clockwise.
		/// </summary>
		public static float HeadingToAngle( Vector2 _direction )
		{
			if (_direction.sqrMagnitude < 1e-12f)
				return 0f;

			return Mathf.Repeat(Mathf.Atan2(_direction.x, _direction.y) * Mathf.Rad2Deg, 360f);
		}
	}
}
