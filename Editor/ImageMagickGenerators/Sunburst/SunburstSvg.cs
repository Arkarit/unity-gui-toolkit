using System.Globalization;
using System.Text;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Composes the SVG string for a parametric sunburst.
	///
	/// Geometry: N rays distributed around the canvas center. Each ray has an angular *center*
	/// and an angular *width*; the two are now decoupled, so the two randomness parameters are
	/// fully orthogonal:
	///   - <c>randomnessRayWidth</c>: per-ray width multiplier in [1-r, 1+r] (clamped ≥0), then
	///     widths are rescaled to preserve the average set by <c>dutyCycle</c>.
	///   - <c>randomnessRayDistribution</c>: per-ray center jitter in [-1, +1] * (slot/2) * r.
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
			float rayGradient,
			float randomnessRayWidth,
			float randomnessRayDistribution,
			int seed,
			int svgWidth,
			int svgHeight
		)
		{
			int n = Mathf.Max(3, rayCount);
			float twoPi = 2f * Mathf.PI;
			float slot = twoPi / n;
			float baseRayWidth = Mathf.Clamp(dutyCycle, 0.001f, 0.999f) * slot;

			var rayWidths = new float[n];
			var rayCenters = new float[n];
			ComputeRayLayout(n, slot, baseRayWidth, randomnessRayWidth, randomnessRayDistribution, seed, rayCenters, rayWidths);

			float outerR = Mathf.Clamp01(outerRadiusRatio);
			float innerR = Mathf.Clamp(innerRadiusRatio, 0f, outerR);
			float gradT = Mathf.Clamp01(rayGradient);
			bool useGradient = gradT > 1e-4f && outerR > 1e-5f;

			// We render into a viewBox of (-1,-1)..(1,1); the actual pixel canvas is svgWidth × svgHeight.
			var sb = new StringBuilder(2048);
			sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
			sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgWidth}\" height=\"{svgHeight}\" viewBox=\"-1 -1 2 2\" preserveAspectRatio=\"none\">\n");

			const string gradientId = "sunburstGradient";
			if (useGradient)
			{
				// Inner-stop SVG offset: 1.0 at gradT=0 (stop at outer rim, no visible fade),
				// innerR/outerR at gradT=1 (stop at the inner edge of the rays, full fade across the ray).
				float innerStopOffset = 1f - gradT * (1f - innerR / outerR);
				sb.Append("  <defs>\n");
				sb.Append($"    <radialGradient id=\"{gradientId}\" cx=\"0\" cy=\"0\" r=\"{F(outerR)}\" gradientUnits=\"userSpaceOnUse\">\n");
				sb.Append("      <stop offset=\""); sb.Append(F(innerStopOffset)); sb.Append("\" ");
				AppendStopColor(sb, rayColor);
				sb.Append("/>\n");
				sb.Append("      <stop offset=\"1\" ");
				AppendStopColor(sb, backgroundColor);
				sb.Append("/>\n");
				sb.Append("    </radialGradient>\n");
				sb.Append("  </defs>\n");
			}

			if (backgroundColor.a > 0.001f)
			{
				sb.Append("  <rect x=\"-1\" y=\"-1\" width=\"2\" height=\"2\" ");
				AppendFill(sb, backgroundColor);
				sb.Append("/>\n");
			}

			// Inner disc fill (rotation-invariant, so emit outside the rotate group).
			// When fillInnerCircle is false the inner area shows whatever is behind (background, or transparent).
			if (fillInnerCircle && innerR > 1e-5f && rayColor.a > 0.001f)
			{
				sb.Append($"  <circle cx=\"0\" cy=\"0\" r=\"{F(innerR)}\" ");
				if (useGradient)
					sb.Append($"fill=\"url(#{gradientId})\"");
				else
					AppendFill(sb, rayColor);
				sb.Append("/>\n");
			}

			sb.Append($"  <g transform=\"rotate({F(rotationDegrees)})\">\n");
			float baseW = Mathf.Max(0f, rayBaseWidth);
			float tipW = Mathf.Clamp01(rayTipWidth);
			float baseOffsetHalfRad = Mathf.Max(0f, rayBaseWidthOffsetDegrees) * Mathf.Deg2Rad * 0.5f;
			string rayFill = useGradient ? $"fill=\"url(#{gradientId})\"" : BuildFillAttrs(rayColor);

			// Each ray is independently placed at its center with its width (no sequential layout,
			// no gap arithmetic). Adjacent rays can overlap when widths + distribution jitter push
			// them together; SVG fill handles that naturally.
			//
			// Path per ray:
			//   - inner arc along the inner circle from ai1 to ai2 (sweep=1, CW)
			//   - line from inner-right to outer-right
			//   - outer arc along the outer circle from ao2 back to ao1 (sweep=0, CCW)
			//   - line back to inner-left (closes via Z)
			//
			// Arcs (not chords) keep the edges on the inner/outer circles even at large RayBaseWidth.
			string innerRadiusStr = F(innerR);
			string outerRadiusStr = F(outerR);
			for (int i = 0; i < n; i++)
			{
				if (rayWidths[i] <= 1e-5f)
					continue;

				float center = rayCenters[i];
				float halfSlot = rayWidths[i] * 0.5f;
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

			sb.Append("  </g>\n</svg>\n");
			return sb.ToString();
		}

		private static void ComputeRayLayout(
			int n,
			float slot,
			float baseRayWidth,
			float randomnessRayWidth,
			float randomnessRayDistribution,
			int seed,
			float[] rayCenters,
			float[] rayWidths
		)
		{
			float rW = Mathf.Clamp01(randomnessRayWidth);
			float rD = Mathf.Clamp01(randomnessRayDistribution);
			bool needRng = rW > 1e-4f || rD > 1e-4f;

			System.Random rng = null;
			if (needRng)
			{
				// seed == -1 is a sentinel for "no explicit seed"; map to a deterministic default so that
				// untouched generators still produce a stable, reproducible result.
				int effectiveSeed = seed < 0 ? 0 : seed;
				rng = new System.Random(effectiveSeed);
			}

			// Phase 1: widths. Compute per-ray weights, then rescale so the average width equals baseRayWidth
			// (i.e. the average duty cycle remains exactly as set even though individual rays vary).
			if (rW > 1e-4f)
			{
				float weightSum = 0f;
				for (int i = 0; i < n; i++)
				{
					float w = Mathf.Max(0f, 1f + rW * ((float)rng.NextDouble() * 2f - 1f));
					rayWidths[i] = w;
					weightSum += w;
				}
				float scale = weightSum > 1e-6f ? (baseRayWidth * n) / weightSum : 0f;
				for (int i = 0; i < n; i++)
					rayWidths[i] *= scale;
			}
			else
			{
				for (int i = 0; i < n; i++)
					rayWidths[i] = baseRayWidth;
			}

			// Phase 2: centers. Uniform positions, optionally jittered by up to ±slot/2 * rD.
			float maxJitter = slot * 0.5f * rD;
			for (int i = 0; i < n; i++)
			{
				float jitter = rD > 1e-4f ? maxJitter * ((float)rng.NextDouble() * 2f - 1f) : 0f;
				rayCenters[i] = i * slot + jitter;
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

		private static void AppendStopColor(StringBuilder sb, Color c)
		{
			int r = Mathf.Clamp(Mathf.RoundToInt(c.r * 255f), 0, 255);
			int g = Mathf.Clamp(Mathf.RoundToInt(c.g * 255f), 0, 255);
			int b = Mathf.Clamp(Mathf.RoundToInt(c.b * 255f), 0, 255);
			sb.Append("stop-color=\"rgb("); sb.Append(r); sb.Append(','); sb.Append(g); sb.Append(','); sb.Append(b); sb.Append(")\" ");
			sb.Append("stop-opacity=\""); sb.Append(F(Mathf.Clamp01(c.a))); sb.Append("\"");
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
