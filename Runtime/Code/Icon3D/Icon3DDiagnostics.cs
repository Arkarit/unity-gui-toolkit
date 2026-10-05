using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>One distinct 3D icon (one render request): what it shows, how big it is and what state it is in.</summary>
	public struct Icon3DInfo
	{
		public string Key;
		public GameObject Prefab;
		public UiIcon3DPreset Preset;
		public Vector2Int Size;
		/// <summary>Handles that share this render: identical icons are rendered once.</summary>
		public int References;
		/// <summary>Of those, how many are currently shown.</summary>
		public int VisibleReferences;
		public bool IsAnimated;
		public bool IsPlaying;
		public bool IsRendered;
		/// <summary>Waiting for a (new) render.</summary>
		public bool IsDirty;
		public bool HasFailed;
		/// <summary>Rendered twice (over black and over white) for the alpha of transparent materials.</summary>
		public bool ExactAlpha;
		/// <summary>Memory of its result texture.</summary>
		public long Bytes;
	}

	/// <summary>One keyword of the <see cref="RenderTextureManager"/>.</summary>
	public struct RenderTextureInfo
	{
		public string Key;
		public bool HasTexture;
		public bool IsCreated;
		public int Width;
		public int Height;
		public RenderTextureFormat Format;
		public int DepthBits;
		public int MsaaSamples;
		public bool MipMaps;
		public int Producers;
		public int Consumers;
		public long Bytes;
	}

	/// <summary>The renderer's state at a glance.</summary>
	public struct Icon3DSummary
	{
		public int Requests;
		public int Pending;
		public int Animated;
		public int Failed;
		/// <summary>Result textures that exist, icons or not.</summary>
		public int Textures;
		/// <summary>Memory of all result textures of the manager. Unity's temporary render targets are pooled and not counted.</summary>
		public long TotalBytes;
		public long IconBytes;
		public int LastTickRenders;
		public int PeakTickRenders;
		public int TotalRenders;
		public float LastStaticRenderMilliseconds;
		public int DeferredByTimeBudget;
		public int RendersPerFrame;
		public float RenderMilliseconds;
		public int MsaaSamples;
		public int Layer;
	}

	/// <summary>
	/// What the 3D icon renderer and the render texture manager hold right now. Works at runtime (a development
	/// overlay can use it) and feeds the editor's debug window.
	/// </summary>
	public static class Icon3DDiagnostics
	{
		public const string IconKeyPrefix = "icon3d/";

		private static readonly List<RenderTextureInfo> s_textures = new();

		/// <summary>
		/// Fills _icons with every distinct icon and _otherTextures with the managed render textures that are not
		/// 3D icons (may be null). Both lists are replaced. Returns the summary.
		/// </summary>
		public static Icon3DSummary Collect( List<Icon3DInfo> _icons, List<RenderTextureInfo> _otherTextures = null )
		{
			UiIcon3DRenderer.CollectRequests(_icons);
			RenderTextureManager.CollectInfo(s_textures);
			_otherTextures?.Clear();

			var summary = new Icon3DSummary
			{
				Requests = _icons.Count,
				LastTickRenders = UiIcon3DRenderer.LastTickRenders,
				PeakTickRenders = UiIcon3DRenderer.PeakTickRenders,
				TotalRenders = UiIcon3DRenderer.RenderCount,
				LastStaticRenderMilliseconds = UiIcon3DRenderer.LastStaticRenderMilliseconds,
				DeferredByTimeBudget = UiIcon3DRenderer.DeferredByTimeBudget,
				RendersPerFrame = UiIcon3DRenderer.RendersPerFrame,
				RenderMilliseconds = UiIcon3DRenderer.RenderMilliseconds,
				MsaaSamples = UiIcon3DRenderer.MsaaSamples,
				Layer = UiIcon3DRenderer.Layer,
			};

			foreach (var icon in _icons)
			{
				if (icon.IsDirty && !icon.HasFailed)
					summary.Pending++;
				if (icon.IsAnimated)
					summary.Animated++;
				if (icon.HasFailed)
					summary.Failed++;
				summary.IconBytes += icon.Bytes;
			}

			foreach (var texture in s_textures)
			{
				if (texture.HasTexture)
				{
					summary.Textures++;
					summary.TotalBytes += texture.Bytes;
				}

				if (_otherTextures != null && !texture.Key.StartsWith(IconKeyPrefix, System.StringComparison.Ordinal))
					_otherTextures.Add(texture);
			}

			return summary;
		}

		public static string FormatBytes( long _bytes )
		{
			if (_bytes >= 1024 * 1024)
				return (_bytes / (1024.0 * 1024.0)).ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " MB";
			if (_bytes >= 1024)
				return (_bytes / 1024.0).ToString("F0", System.Globalization.CultureInfo.InvariantCulture) + " KB";
			return _bytes + " B";
		}
	}
}
