using System;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Describes the render texture a producer wants to render into.
	/// The size is normally taken from the consumers (largest requested size wins);
	/// set <see cref="FixedSize"/> to override that.
	/// </summary>
	[Serializable]
	public struct RenderTextureSpec : IEquatable<RenderTextureSpec>
	{
		[Tooltip("Fixed texture size in pixels. Leave at 0/0 to take the size from the consumers.")]
		public Vector2Int FixedSize;
		[Tooltip("Consumer sizes are rounded up to a multiple of this, so small layout changes do not reallocate the texture.")]
		[Min(1)] public int SizeGranularity;
		public RenderTextureFormat ColorFormat;
		[Tooltip("Depth buffer bits: 0, 16, 24 or 32")]
		public int DepthBits;
		[Tooltip("MSAA samples: 1, 2, 4 or 8")]
		public int MsaaSamples;
		public bool UseMipMaps;
		public FilterMode FilterMode;

		public static RenderTextureSpec Default => new RenderTextureSpec
		{
			FixedSize = Vector2Int.zero,
			SizeGranularity = 16,
			ColorFormat = RenderTextureFormat.ARGB32,
			DepthBits = 24,
			MsaaSamples = 1,
			UseMipMaps = false,
			FilterMode = FilterMode.Bilinear,
		};

		public bool HasFixedSize => FixedSize.x > 0 && FixedSize.y > 0;

		public int SanitizedMsaaSamples => MsaaSamples >= 8 ? 8 : MsaaSamples >= 4 ? 4 : MsaaSamples >= 2 ? 2 : 1;
		public int SanitizedDepthBits => DepthBits >= 32 ? 32 : DepthBits >= 24 ? 24 : DepthBits >= 16 ? 16 : 0;

		public bool Equals( RenderTextureSpec _other ) =>
			FixedSize == _other.FixedSize
			&& SizeGranularity == _other.SizeGranularity
			&& ColorFormat == _other.ColorFormat
			&& DepthBits == _other.DepthBits
			&& MsaaSamples == _other.MsaaSamples
			&& UseMipMaps == _other.UseMipMaps
			&& FilterMode == _other.FilterMode;

		public override bool Equals( object _obj ) => _obj is RenderTextureSpec other && Equals(other);

		public override int GetHashCode() =>
			HashCode.Combine(FixedSize, SizeGranularity, ColorFormat, DepthBits, MsaaSamples, UseMipMaps, FilterMode);
	}

	/// <summary>
	/// Anything that is linked to a managed render texture by keyword.
	/// </summary>
	public interface IRenderTextureUser
	{
		/// <summary>
		/// Called whenever the texture for the user's keyword was created, resized or released.
		/// _texture is null if there currently is no texture.
		/// </summary>
		void OnRenderTextureChanged( RenderTexture _texture );
	}

	/// <summary>
	/// Renders into a managed render texture, e.g. a camera. Defines the texture format.
	/// If several producers share a keyword, the first registered one defines the spec.
	/// </summary>
	public interface IRenderTextureProducer : IRenderTextureUser
	{
		RenderTextureSpec RenderTextureSpec { get; }
	}

	/// <summary>
	/// Displays a managed render texture, e.g. a RawImage.
	/// </summary>
	public interface IRenderTextureConsumer : IRenderTextureUser
	{
		/// <summary>
		/// Size in pixels this consumer would like to have; zero means "no opinion".
		/// Polled by the manager every frame, so it should be cheap.
		/// </summary>
		Vector2Int RequestedSize { get; }
	}
}
