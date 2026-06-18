using System.Globalization;
using System.Text;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Composes the SVG string for a parametric sunburst.
	///
	/// Geometry: N rays evenly distributed around the canvas center. Each ray is a quad with two
	/// inner corners (at innerRadius) and two outer corners (at outerRadius). The angular width of
	/// a ray is <c>dutyCycle * (2π / N)</c>; the rest of the slot is the gap between rays.
	///
	/// Coordinate system: SVG y-down, so we use (sin a, -cos a) to place rotation 0 at the top
	/// (compass convention) and positive rotation clockwise.
	/// </summary>
	public static class SunburstSvg
	{
		private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

		public static string Build(
			int rayCount,
			float dutyCycle,
			float innerRadiusRatio,
			float outerRadiusRatio,
			float rotationDegrees,
			Color rayColor,
			Color backgroundColor,
			int svgWidth,
			int svgHeight
		)
		{
			int n = Mathf.Max(3, rayCount);
			float slot = 2f * Mathf.PI / n;
			float halfRayAngle = Mathf.Clamp(dutyCycle, 0.001f, 0.999f) * slot * 0.5f;

			// We render into a viewBox of (-1,-1)..(1,1); the actual pixel canvas is svgWidth × svgHeight.
			var sb = new StringBuilder(2048);
			sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
			sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgWidth}\" height=\"{svgHeight}\" viewBox=\"-1 -1 2 2\" preserveAspectRatio=\"none\">\n");

			if (backgroundColor.a > 0.001f)
			{
				sb.Append("  <rect x=\"-1\" y=\"-1\" width=\"2\" height=\"2\" ");
				AppendFill(sb, backgroundColor);
				sb.Append("/>\n");
			}

			sb.Append($"  <g transform=\"rotate({F(rotationDegrees)})\">\n");

			float outerR = Mathf.Clamp01(outerRadiusRatio);
			float innerR = Mathf.Clamp(innerRadiusRatio, 0f, outerR);
			string rayFill = BuildFillAttrs(rayColor);

			for (int i = 0; i < n; i++)
			{
				float center = i * slot;
				float a1 = center - halfRayAngle;
				float a2 = center + halfRayAngle;

				float s1 = Mathf.Sin(a1), c1 = Mathf.Cos(a1);
				float s2 = Mathf.Sin(a2), c2 = Mathf.Cos(a2);

				float ix1 = innerR * s1, iy1 = -innerR * c1;
				float ox1 = outerR * s1, oy1 = -outerR * c1;
				float ox2 = outerR * s2, oy2 = -outerR * c2;
				float ix2 = innerR * s2, iy2 = -innerR * c2;

				sb.Append("    <path d=\"M ");
				sb.Append(F(ix1)); sb.Append(' '); sb.Append(F(iy1));
				sb.Append(" L "); sb.Append(F(ox1)); sb.Append(' '); sb.Append(F(oy1));
				sb.Append(" L "); sb.Append(F(ox2)); sb.Append(' '); sb.Append(F(oy2));
				sb.Append(" L "); sb.Append(F(ix2)); sb.Append(' '); sb.Append(F(iy2));
				sb.Append(" Z\" ");
				sb.Append(rayFill);
				sb.Append("/>\n");
			}

			sb.Append("  </g>\n</svg>\n");
			return sb.ToString();
		}

		private static void AppendFill(StringBuilder sb, Color c)
		{
			int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
			int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
			int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
			sb.Append("fill=\"rgb("); sb.Append(r); sb.Append(','); sb.Append(g); sb.Append(','); sb.Append(b); sb.Append(")\" ");
			sb.Append("fill-opacity=\""); sb.Append(F(Mathf.Clamp01(c.a))); sb.Append("\"");
		}

		private static string BuildFillAttrs(Color c)
		{
			var sb = new StringBuilder(48);
			AppendFill(sb, c);
			return sb.ToString();
		}

		private static string F(float v) => v.ToString("F4", Inv);
	}
}
