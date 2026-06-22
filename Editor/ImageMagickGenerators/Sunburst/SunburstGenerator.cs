using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// <para>Parametric sunburst sprite generator.</para>
	/// <para>
	/// Holds the sunburst parameters, builds an SVG from them, calls ImageMagick to rasterize
	/// (with supersampling and optional gaussian edge softening), and writes the result to
	/// <see cref="OutputPath"/>, configured as a Sprite.
	/// </para>
	/// <para>
	/// One ScriptableObject = one output (or one numbered series, when <see cref="Incremental"/>
	/// is enabled).
	/// </para>
	/// </summary>
	[CreateAssetMenu(fileName = "Sunburst", menuName = StringConstants.CREATE_SUNBURST_GENERATOR)]
	public class SunburstGenerator : ScriptableObject
	{
		public const string DefaultOutputFolder = "Assets/Resources/Generated/Sunburst";

		private const int IncrementalDigits = 4;
		private static readonly Regex s_trailingNumberRegex = new(@"^(.*?)_(\d+)$", RegexOptions.Compiled);

		public const int MinRays = 3;
		public const int MaxRays = 200;
		public const int MinSupersampling = 1;
		public const int MaxSupersampling = 8;

		[Tooltip("Number of rays around the center.")]
		[Range(MinRays, MaxRays)] public int RayCount = 24;

		[Tooltip("Ratio of ray angular width to total angular slot (ray + gap). 0.5 = equal widths.")]
		[Range(0.05f, 0.95f)] public float DutyCycle = 0.5f;

		[Tooltip("Inner radius as fraction of canvas half-size. 0 = rays start at center; >0 = donut sunburst.")]
		[Range(0f, 0.95f)] public float InnerRadiusRatio = 0f;

		[Tooltip("Fill the disc inside InnerRadius with ray color (true) instead of background color (false). "
		         + "Only visible when InnerRadius > 0. Turns a donut sunburst into a solid-center one.")]
		public bool FillInnerCircle = false;

		[Tooltip("Outer radius as fraction of canvas half-size. 1 = fill canvas; <1 leaves margin.")]
		[Range(0.1f, 1f)] public float OuterRadiusRatio = 1f;

		[Tooltip("Angular width of the ray base at the inner perimeter, as a factor of the ray's slot. "
		         + "1 = base spans the full slot in angular terms (rays just touch at the inner perimeter when InnerRadius > 0). "
		         + "Note: visual width ≈ angular width × radius, so for visually parallel sides you need "
		         + "RayBaseWidth ≈ OuterRadius / InnerRadius. Larger values produce rays that widen toward the center.")]
		[Range(0f, 10f)] public float RayBaseWidth = 1f;

		[Tooltip("Constant angular width (in degrees) added to each ray's base, on top of the RayBaseWidth factor. "
		         + "Equalizes ray base widths under Randomness: narrow rays gain the same absolute boost as wide ones, "
		         + "so their relative width disparity at the inner perimeter shrinks.")]
		[Range(0f, 60f)] public float RayBaseWidthOffset = 0f;

		[Tooltip("Angular width of the ray tip at the outer perimeter, as a factor of the ray's slot. "
		         + "1 = parallel sides (default); 0 = ray tapers to a single point at the rim; "
		         + "values in between yield tapered rays.")]
		[Range(0f, 1f)] public float RayTipWidth = 1f;

		[Tooltip("Rotation around center, in degrees. 0 = first ray center points up.")]
		[Range(-180f, 180f)] public float Rotation = 0f;

		public Color RayColor = Color.white;

		[Tooltip("Background fill. Set alpha to 0 for transparent background.")]
		public Color BackgroundColor = new Color(0f, 0f, 0f, 0f);

		[Tooltip("Radial gradient amount across each ray (and the inner-circle fill, if enabled). "
		         + "0 = no gradient, rays are solid RayColor. "
		         + "1 = full radial gradient from RayColor at the inner edge to BackgroundColor at the outer rim. "
		         + "Intermediate values keep an inner band solid and fade the rest.")]
		[Range(0f, 1f)] public float RayGradient = 0f;

		[Tooltip("Random variation of individual ray and gap widths. 0 = perfectly uniform. "
		         + "Each ray and gap is independently weighted in [1-r, 1+r] (clamped to ≥0) and rescaled "
		         + "so that the overall duty cycle is preserved.")]
		[Range(0f, 1f)] public float Randomness = 0f;

		[Tooltip("Seed for the randomness PRNG. -1 = use a deterministic default seed (still reproducible). "
		         + "Change this to a different non-negative value to get a different random pattern.")]
		public int Seed = -1;

		[Tooltip("Gaussian blur sigma (in output pixels) applied after the supersample downsample. 0 = sharp edges.")]
		[Range(0f, 20f)] public float EdgeSoftness = 0f;

		[Tooltip("Supersample factor for AA. SVG is rendered at OutputSize × Supersampling, then downsampled.")]
		[Range(MinSupersampling, MaxSupersampling)] public int Supersampling = 4;

		[Tooltip("Output PNG size in pixels.")]
		public Vector2Int OutputSize = new Vector2Int(512, 512);

		[Tooltip("Re-render the inspector preview automatically when parameters change.")]
		public bool LivePreview = true;

		[Tooltip("Target file for Generate. Pre-filled with " + DefaultOutputFolder + "/{name}.png on create. "
		         + "Must live under Assets/ for Unity to pick it up as a Sprite.")]
		[PathField(_isFolder: false, _relativeToPath: ".", _extensions: "png")]
		public PathField OutputPath;

		[Tooltip("When enabled, each Generate writes a new file with an incremented 4-digit suffix "
		         + "(MyFile_0001.png, MyFile_0002.png, ...). The suffix is computed by scanning the "
		         + "output directory for existing files matching the pattern and picking max + 1. "
		         + "When disabled, Generate overwrites OutputPath each time.")]
		public bool Incremental = false;

		private void Reset() => EnsureDefaultOutputPath();

		private void EnsureDefaultOutputPath()
		{
			if (string.IsNullOrEmpty(OutputPath.Path))
			{
				string fileName = !string.IsNullOrEmpty(name) ? name : "Sunburst";
				OutputPath = new PathField($"{DefaultOutputFolder}/{fileName}.png");
			}
		}

		/// <summary>
		/// Resolved relative target path for the next Generate call. With Incremental, this scans the
		/// output directory and returns the next available numbered filename.
		/// </summary>
		public string ResolveTargetPath()
		{
			string raw = OutputPath.Path;
			if (string.IsNullOrEmpty(raw))
				return null;

			raw = raw.Replace('\\', '/');
			string dir = Path.GetDirectoryName(raw)?.Replace('\\', '/') ?? string.Empty;
			string stem = Path.GetFileNameWithoutExtension(raw);
			string ext = Path.GetExtension(raw);
			if (string.IsNullOrEmpty(ext))
				ext = ".png";

			if (!Incremental)
				return string.IsNullOrEmpty(dir) ? $"{stem}{ext}" : $"{dir}/{stem}{ext}";

			// Strip an existing _NNNN suffix so the base stem is stable across regenerations.
			var suffixMatch = s_trailingNumberRegex.Match(stem);
			string baseStem = suffixMatch.Success ? suffixMatch.Groups[1].Value : stem;

			int maxN = 0;
			string absDir = string.IsNullOrEmpty(dir)
				? Directory.GetCurrentDirectory()
				: Path.Combine(Directory.GetCurrentDirectory(), dir);
			if (Directory.Exists(absDir))
			{
				var scan = new Regex(
					$@"^{Regex.Escape(baseStem)}_(\d{{{IncrementalDigits}}}){Regex.Escape(ext)}$",
					RegexOptions.IgnoreCase);
				foreach (var f in Directory.GetFiles(absDir))
				{
					var m = scan.Match(Path.GetFileName(f));
					if (m.Success && int.TryParse(m.Groups[1].Value, out int n) && n > maxN)
						maxN = n;
				}
			}

			string nextName = $"{baseStem}_{(maxN + 1).ToString("D" + IncrementalDigits, CultureInfo.InvariantCulture)}{ext}";
			return string.IsNullOrEmpty(dir) ? nextName : $"{dir}/{nextName}";
		}

		/// <summary>
		/// Renders the sunburst at full quality and writes it to the resolved target path.
		/// Returns true on success and populates <paramref name="writtenPath"/> with the relative
		/// path that was actually written; populates <paramref name="error"/> on failure.
		/// </summary>
		public bool Generate(out string writtenPath, out string error)
		{
			writtenPath = null;

			string targetRelative = ResolveTargetPath();
			if (string.IsNullOrEmpty(targetRelative))
			{
				error = "Output path is empty. Set OutputPath before generating.";
				return false;
			}

			var bytes = RenderToBytes(OutputSize, Supersampling, EdgeSoftness, out error);
			if (bytes == null)
				return false;

			string targetAbsolute = Path.Combine(Directory.GetCurrentDirectory(), targetRelative).Replace('\\', '/');

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(targetAbsolute) ?? string.Empty);
				File.WriteAllBytes(targetAbsolute, bytes);
			}
			catch (Exception e)
			{
				error = $"Could not write output PNG to '{targetRelative}': {e.Message}";
				return false;
			}

			if (targetRelative.StartsWith("Assets/", StringComparison.Ordinal))
			{
				AssetDatabase.ImportAsset(targetRelative, ImportAssetOptions.ForceUpdate);
				ConfigureSpriteImport(targetRelative);
			}
			else
			{
				UiLog.LogWarning($"Sunburst written to '{targetRelative}' (outside Assets/), so Unity won't import it as a Sprite. Move OutputPath under Assets/ for asset-database integration.");
			}

			writtenPath = targetRelative;
			error = null;
			return true;
		}

		/// <summary>
		/// Renders the sunburst at preview resolution (smaller, lower supersampling) and returns the PNG bytes.
		/// Does NOT write anything to the asset database.
		/// </summary>
		public byte[] RenderPreviewBytes(Vector2Int previewSize, int previewSupersampling, out string error)
		{
			float scaledBlur = OutputSize.x > 0 ? EdgeSoftness * (float)previewSize.x / OutputSize.x : EdgeSoftness;
			return RenderToBytes(previewSize, previewSupersampling, scaledBlur, out error);
		}

		private byte[] RenderToBytes(Vector2Int size, int supersampling, float blurSigma, out string error)
		{
			string tempSvg = null;
			string tempPng = null;

			try
			{
				int ss = Mathf.Clamp(supersampling, MinSupersampling, MaxSupersampling);
				int svgW = Mathf.Max(1, size.x * ss);
				int svgH = Mathf.Max(1, size.y * ss);

				string svg = SunburstSvg.Build(
					RayCount, DutyCycle, InnerRadiusRatio, OuterRadiusRatio, RayBaseWidth, RayBaseWidthOffset,
					RayTipWidth, Rotation, RayColor, BackgroundColor, FillInnerCircle, RayGradient,
					Randomness, Seed, svgW, svgH);

				string id = Guid.NewGuid().ToString("N");
				tempSvg = Path.Combine(Path.GetTempPath(), $"sunburst_{id}.svg").Replace('\\', '/');
				tempPng = Path.Combine(Path.GetTempPath(), $"sunburst_{id}.png").Replace('\\', '/');

				File.WriteAllText(tempSvg, svg);

				var args = new List<string>
				{
					"-background", "none",
					tempSvg,
					"-resize", $"{size.x}x{size.y}!",
				};
				if (blurSigma > 0.001f)
				{
					args.Add("-blur");
					args.Add("0x" + blurSigma.ToString("F2", CultureInfo.InvariantCulture));
				}
				args.Add($"PNG32:{tempPng}");

				var output = new StringBuilder();
				int exitCode = ImageMagickRunner.RunSync(args.ToArray(), line =>
				{
					if (!string.IsNullOrEmpty(line))
						output.AppendLine(line);
				});

				if (exitCode != 0)
				{
					error = $"magick exited with code {exitCode}.\nArgs: {string.Join(" ", args)}\nOutput:\n{output}";
					return null;
				}

				if (!File.Exists(tempPng))
				{
					error = $"magick did not produce an output file.\nArgs: {string.Join(" ", args)}\nOutput:\n{output}";
					return null;
				}

				error = null;
				return File.ReadAllBytes(tempPng);
			}
			catch (FileNotFoundException)
			{
				error = "ImageMagick not found. Open 'Window/ImageMagick Bridge' to install or set the executable path.";
				return null;
			}
			catch (Exception e)
			{
				error = $"Exception during sunburst render: {e.Message}";
				return null;
			}
			finally
			{
				bool keep = false;
				try
				{
					keep = ImageMagickConfig.Instance != null && ImageMagickConfig.Instance.KeepIntermediateFiles;
				}
				catch
				{
					// singleton not ready — default to cleanup
				}

				if (!keep)
				{
					TryDelete(tempSvg);
					TryDelete(tempPng);
				}
			}
		}

		private static void TryDelete(string path)
		{
			if (string.IsNullOrEmpty(path))
				return;
			try { File.Delete(path); }
			catch { /* ignore */ }
		}

		private static void ConfigureSpriteImport(string relativePath)
		{
			var importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
			if (importer == null)
				return;

			bool changed = false;
			if (importer.textureType != TextureImporterType.Sprite)
			{
				importer.textureType = TextureImporterType.Sprite;
				changed = true;
			}
			if (importer.spriteImportMode != SpriteImportMode.Single)
			{
				importer.spriteImportMode = SpriteImportMode.Single;
				changed = true;
			}
			if (importer.mipmapEnabled)
			{
				importer.mipmapEnabled = false;
				changed = true;
			}
			if (importer.wrapMode != TextureWrapMode.Clamp)
			{
				importer.wrapMode = TextureWrapMode.Clamp;
				changed = true;
			}
			if (!importer.alphaIsTransparency)
			{
				importer.alphaIsTransparency = true;
				changed = true;
			}

			if (changed)
				importer.SaveAndReimport();
		}
	}
}
