using System.Globalization;
using System.Text;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Composes the SVG string for a parametric sunburst.
	///
	/// Geometry: N rays distributed around the canvas center. Each ray is a quad with two inner
	/// corners (at innerRadius) and two outer corners (at outerRadius). Without randomness, rays
	/// and gaps have uniform angular width derived from <c>dutyCycle</c>. With randomness, each
	/// individual ray and gap gets a weight in <c>[1-r, 1+r]</c> (clamped to non-negative) and
	/// widths are scaled so that the total ray-angle and total gap-angle are preserved — i.e.
	/// the overall duty cycle stays exactly as set even though individual widths vary.
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
			float randomness,
			int seed,
			int svgWidth,
			int svgHeight
		)
		{
			int n = Mathf.Max(3, rayCount);
			float twoPi = 2f * Mathf.PI;
			float totalRayAngle = Mathf.Clamp(dutyCycle, 0.001f, 0.999f) * twoPi;
			float totalGapAngle = twoPi - totalRayAngle;

			var rayWidths = new float[n];
			var gapWidths = new float[n];
			ComputeWidths(n, totalRayAngle, totalGapAngle, randomness, seed, rayWidths, gapWidths);

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

			// Lay rays out starting with ray 0 centered on angle 0 (top); subsequent rays clockwise.
			float angle = -rayWidths[0] * 0.5f;
			for (int i = 0; i < n; i++)
			{
				float a1 = angle;
				float a2 = angle + rayWidths[i];

				if (rayWidths[i] > 1e-5f)
				{
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

				angle += rayWidths[i] + gapWidths[i];
			}

			sb.Append("  </g>\n</svg>\n");
			return sb.ToString();
		}

		private static void ComputeWidths(int n, float totalRayAngle, float totalGapAngle, float randomness, int seed, float[] rayWidths, float[] gapWidths)
		{
			if (randomness <= 1e-4f)
			{
				float uniformRay = totalRayAngle / n;
				float uniformGap = totalGapAngle / n;
				for (int i = 0; i < n; i++)
				{
					rayWidths[i] = uniformRay;
					gapWidths[i] = uniformGap;
				}
				return;
			}

			// seed == -1 is a sentinel for "no explicit seed"; map to a deterministic default so that
			// untouched generators still produce a stable, reproducible result.
			int effectiveSeed = seed < 0 ? 0 : seed;
			var rng = new System.Random(effectiveSeed);

			float r = Mathf.Clamp01(randomness);
			float rayWeightSum = 0f;
			float gapWeightSum = 0f;
			for (int i = 0; i < n; i++)
			{
				rayWidths[i] = Mathf.Max(0f, 1f + r * ((float)rng.NextDouble() * 2f - 1f));
				gapWidths[i] = Mathf.Max(0f, 1f + r * ((float)rng.NextDouble() * 2f - 1f));
				rayWeightSum += rayWidths[i];
				gapWeightSum += gapWidths[i];
			}

			float rayScale = rayWeightSum > 1e-6f ? totalRayAngle / rayWeightSum : 0f;
			float gapScale = gapWeightSum > 1e-6f ? totalGapAngle / gapWeightSum : 0f;
			for (int i = 0; i < n; i++)
			{
				rayWidths[i] *= rayScale;
				gapWidths[i] *= gapScale;
			}
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
