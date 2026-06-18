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
			float rayBaseWidth,
			float rayBaseWidthOffsetDegrees,
			float rayTipWidth,
			float rotationDegrees,
			Color rayColor,
			Color backgroundColor,
			bool fillInnerCircle,
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

			float outerR = Mathf.Clamp01(outerRadiusRatio);
			float innerR = Mathf.Clamp(innerRadiusRatio, 0f, outerR);

			// Inner disc fill (rotation-invariant, so emit outside the rotate group).
			// When fillInnerCircle is false the inner area shows whatever is behind (background, or transparent).
			if (fillInnerCircle && innerR > 1e-5f && rayColor.a > 0.001f)
			{
				sb.Append($"  <circle cx=\"0\" cy=\"0\" r=\"{F(innerR)}\" ");
				AppendFill(sb, rayColor);
				sb.Append("/>\n");
			}

			sb.Append($"  <g transform=\"rotate({F(rotationDegrees)})\">\n");
			float baseW = Mathf.Max(0f, rayBaseWidth);
			float tipW = Mathf.Clamp01(rayTipWidth);
			float baseOffsetHalfRad = Mathf.Max(0f, rayBaseWidthOffsetDegrees) * Mathf.Deg2Rad * 0.5f;
			string rayFill = BuildFillAttrs(rayColor);

			// Lay rays out starting with ray 0 centered on angle 0 (top); subsequent rays clockwise.
			//
			// Each ray is bounded by:
			//   - inner arc along the inner circle from ai1 to ai2 (sweep=1, CW)
			//   - line from inner-right to outer-right
			//   - outer arc along the outer circle from ao2 back to ao1 (sweep=0, CCW)
			//   - line back to inner-left (closes via Z)
			//
			// Using arcs (rather than chords) keeps the ray edges *on* the inner/outer circles even
			// at large RayBaseWidth: overlapping inner arcs of adjacent rays just fill the inner
			// disc cleanly instead of producing chord-polygons.
			string innerRadiusStr = F(innerR);
			string outerRadiusStr = F(outerR);
			float angle = -rayWidths[0] * 0.5f;
			for (int i = 0; i < n; i++)
			{
				if (rayWidths[i] > 1e-5f)
				{
					float a1 = angle;
					float a2 = angle + rayWidths[i];
					float center = (a1 + a2) * 0.5f;
					float halfSlot = (a2 - a1) * 0.5f;
					float innerHalf = halfSlot * baseW + baseOffsetHalfRad;
					float outerHalf = halfSlot * tipW;
					float ai1 = center - innerHalf;
					float ai2 = center + innerHalf;
					float ao1 = center - outerHalf;
					float ao2 = center + outerHalf;

					float ix1 = innerR * Mathf.Sin(ai1), iy1 = -innerR * Mathf.Cos(ai1);
					float ix2 = innerR * Mathf.Sin(ai2), iy2 = -innerR * Mathf.Cos(ai2);
					float ox1 = outerR * Mathf.Sin(ao1), oy1 = -outerR * Mathf.Cos(ao1);
					float ox2 = outerR * Mathf.Sin(ao2), oy2 = -outerR * Mathf.Cos(ao2);

					int innerLargeArc = (ai2 - ai1) > Mathf.PI ? 1 : 0;
					int outerLargeArc = (ao2 - ao1) > Mathf.PI ? 1 : 0;

					sb.Append("    <path d=\"M ");
					if (innerR > 1e-5f)
					{
						sb.Append(F(ix1)); sb.Append(' '); sb.Append(F(iy1));
						sb.Append(" A "); sb.Append(innerRadiusStr); sb.Append(' '); sb.Append(innerRadiusStr);
						sb.Append(" 0 "); sb.Append(innerLargeArc); sb.Append(" 1 ");
						sb.Append(F(ix2)); sb.Append(' '); sb.Append(F(iy2));
					}
					else
					{
						// innerR ≈ 0: collapse inner edge to the origin (no inner arc).
						sb.Append("0 0");
					}
					sb.Append(" L "); sb.Append(F(ox2)); sb.Append(' '); sb.Append(F(oy2));
					sb.Append(" A "); sb.Append(outerRadiusStr); sb.Append(' '); sb.Append(outerRadiusStr);
					sb.Append(" 0 "); sb.Append(outerLargeArc); sb.Append(" 0 ");
					sb.Append(F(ox1)); sb.Append(' '); sb.Append(F(oy1));
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
