using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Packs a folder of single-frame renders into one directional sprite sheet: one row per viewing
	/// direction, one column per frame, plus a <see cref="UiDirectionalSpriteSheet"/> describing it.
	///
	/// Expected source layout - one subfolder per direction, named after its angle in degrees, each
	/// holding the frames of the animation in filename order:
	/// <code>
	///   Walk/Body/000/Walk_Body_0_0001.png ... _0016.png
	///   Walk/Body/030/...
	/// </code>
	///
	/// Cropping is the part worth getting right. Frames out of a render farm carry a lot of empty
	/// margin, but cropping each frame to its own content would make the subject jitter and would put
	/// its anchor in a different place in every cell. So the crop is a single rectangle for the whole
	/// set, taken from the union of every frame's bounds and then squared off around the source image
	/// centre, which is where the renderer's origin sits. Cells are scaled to
	/// <see cref="CellSize"/> afterwards rather than cropped down to it - a tighter crop would saw the
	/// legs off the diagonal views.
	///
	/// The sheet is written as a plain texture, not as sliced Sprites: a 16x16 sheet addressed through
	/// RawImage.uvRect is one asset instead of 256 sub-assets.
	/// </summary>
	[CreateAssetMenu(fileName = "SpriteSheet", menuName = StringConstants.CREATE_SPRITE_SHEET_GENERATOR)]
	public class UiSpriteSheetGenerator : ScriptableObject
	{
		public const string DefaultOutputFolder = "Assets/Resources/Generated/SpriteSheets";

		private static readonly Regex s_trimBoxRegex =
			new(@"^(\d+)x(\d+)\+(-?\d+)\+(-?\d+)", RegexOptions.Compiled);

		[Tooltip("Folder holding one subfolder per direction. Each subfolder's name is that direction's "
		         + "angle in degrees, and holds the animation's frames in filename order.")]
		public PathField SourceFolder;

		[Tooltip("File extension of the frames.")]
		public string FrameExtension = "png";

		[Tooltip("Edge length of one cell in the finished sheet. The crop is scaled to this, never "
		         + "cropped down to it.")]
		[Range(16, 512)] public int CellSize = 128;

		[Tooltip("Work out the crop from the union of all frames' contents. Turn this off to dictate it "
		         + "with Manual Crop.")]
		public bool AutoCrop = true;

		[Tooltip("Crop rectangle in source pixels, used when Auto Crop is off.")]
		public RectInt ManualCrop = new RectInt(0, 0, 256, 256);

		[Tooltip("Extra margin in source pixels added around the computed crop.")]
		[Range(0, 64)] public int CropPadding = 0;

		[Tooltip("Where the subject actually meets the ground, in source pixels, measured from the top "
		         + "left. Leave at (-1,-1) to use the source image centre. Becomes the sheet's pivot, so "
		         + "this is what the subject's transform position will line up with.")]
		public Vector2 AnchorPixel = new Vector2(-1f, -1f);

		[Tooltip("Distance the subject covers per frame at its natural pace, written into the sheet asset.")]
		public float UnitsPerFrame = 8f;

		[Tooltip("Where the finished PNG goes.")]
		public PathField OutputPath;

		[Tooltip("Sheet asset to fill in. Created next to the PNG when left empty.")]
		public UiDirectionalSpriteSheet SheetAsset;

		private void Reset()
		{
			if (string.IsNullOrEmpty(OutputPath.Path))
				OutputPath = new PathField($"{DefaultOutputFolder}/{name}.png");
		}

		/// <summary>
		/// One direction's folder: its angle and its frames, in order.
		/// </summary>
		private readonly struct DirectionSource
		{
			public readonly float Angle;
			public readonly string[] Frames;

			public DirectionSource( float _angle, string[] _frames )
			{
				Angle = _angle;
				Frames = _frames;
			}
		}

		public bool Generate( out string writtenPath, out string error )
		{
			writtenPath = null;

			if (!ImageMagickRunner.MagickExecutableFound)
			{
				error = "ImageMagick was not found. Set it up in the ImageMagick config first.";
				return false;
			}

			if (!CollectDirections(out List<DirectionSource> directions, out error))
				return false;

			if (!ReadSourceSize(directions[0].Frames[0], out int sourceWidth, out int sourceHeight, out error))
				return false;

			RectInt crop = AutoCrop
				? ComputeCrop(directions, sourceWidth, sourceHeight, out error)
				: ManualCrop;

			if (crop.width <= 0 || crop.height <= 0)
			{
				error ??= "Could not work out a crop rectangle - are all frames empty?";
				return false;
			}

			string targetRelative = OutputPath.Path;
			if (string.IsNullOrEmpty(targetRelative))
			{
				error = "Output path is empty.";
				return false;
			}

			string targetAbsolute = Path.GetFullPath(targetRelative).Replace('\\', '/');
			string workFolder = Path.Combine(Path.GetTempPath(), "UiSpriteSheet_" + GetInstanceID()).Replace('\\', '/');

			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(targetAbsolute) ?? string.Empty);
				if (Directory.Exists(workFolder))
					Directory.Delete(workFolder, true);
				Directory.CreateDirectory(workFolder);

				// One montage call per direction, then stack the strips. Doing it in one call would
				// put every source path on a single command line, which for a 16x16 set is already
				// half the Windows limit and would silently break on longer paths.
				var rowFiles = new List<string>(directions.Count);
				for (int i = 0; i < directions.Count; i++)
				{
					string rowFile = $"{workFolder}/row_{i:D3}.png";
					if (!BuildRow(directions[i], crop, rowFile, out error))
						return false;

					rowFiles.Add(rowFile);
				}

				if (!AppendRows(rowFiles, targetAbsolute, out error))
					return false;
			}
			catch (Exception e)
			{
				error = $"Could not build the sprite sheet: {e.Message}";
				return false;
			}
			finally
			{
				try { if (Directory.Exists(workFolder)) Directory.Delete(workFolder, true); }
				catch { /* a leftover temp folder is not worth failing the build over */ }
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			string unityAssetPath = ToUnityAssetPath(targetAbsolute);
			if (string.IsNullOrEmpty(unityAssetPath))
			{
				UiLog.LogWarning($"Sprite sheet written to '{targetAbsolute}', but that is outside Assets/ and "
				                 + "outside every registered package, so Unity cannot import it.");
				writtenPath = targetAbsolute;
				error = null;
				return true;
			}

			AssetDatabase.ImportAsset(unityAssetPath, ImportAssetOptions.ForceUpdate);
			ConfigureTextureImport(unityAssetPath);
			UpdateSheetAsset(unityAssetPath, directions, crop, sourceWidth, sourceHeight);

			writtenPath = unityAssetPath;
			error = null;
			return true;
		}

		private bool CollectDirections( out List<DirectionSource> _directions, out string _error )
		{
			_directions = new List<DirectionSource>();
			_error = null;

			string root = SourceFolder.Path;
			if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
			{
				_error = $"Source folder '{root}' does not exist.";
				return false;
			}

			var folders = Directory.GetDirectories(root);
			var parsed = new List<(float angle, string folder)>();

			foreach (string folder in folders)
			{
				string leaf = Path.GetFileName(folder);
				if (!float.TryParse(leaf, NumberStyles.Float, CultureInfo.InvariantCulture, out float angle))
				{
					UiLog.LogWarning($"Skipping '{leaf}': a direction folder has to be named after its angle in degrees.");
					continue;
				}

				parsed.Add((angle, folder));
			}

			if (parsed.Count == 0)
			{
				_error = $"No direction folders found under '{root}'. Each must be named after its angle, e.g. '000', '045'.";
				return false;
			}

			parsed.Sort((a, b) => a.angle.CompareTo(b.angle));

			int expectedFrames = -1;
			foreach (var (angle, folder) in parsed)
			{
				string[] frames = Directory.GetFiles(folder, "*." + FrameExtension.TrimStart('.'));
				Array.Sort(frames, StringComparer.OrdinalIgnoreCase);

				if (frames.Length == 0)
				{
					_error = $"Direction folder '{folder}' holds no *.{FrameExtension} frames.";
					return false;
				}

				if (expectedFrames < 0)
					expectedFrames = frames.Length;
				else if (frames.Length != expectedFrames)
				{
					// A ragged set would shift every following row, so this has to be fatal.
					_error = $"Direction '{angle}' has {frames.Length} frames but the first direction has "
					         + $"{expectedFrames}. Every direction needs the same frame count.";
					return false;
				}

				_directions.Add(new DirectionSource(angle, frames));
			}

			return true;
		}

		private bool ReadSourceSize( string _frame, out int _width, out int _height, out string _error )
		{
			_width = _height = 0;
			string output = null;

			int exit = ImageMagickRunner.RunSync(
				new[] { "identify", "-format", "%wx%h", _frame },
				line => { if (!string.IsNullOrWhiteSpace(line)) output ??= line.Trim(); });

			if (exit != 0 || string.IsNullOrEmpty(output))
			{
				_error = $"ImageMagick could not read '{_frame}'.";
				return false;
			}

			string[] parts = output.Split('x');
			if (parts.Length != 2 || !int.TryParse(parts[0], out _width) || !int.TryParse(parts[1], out _height))
			{
				_error = $"Could not make sense of the frame size '{output}'.";
				return false;
			}

			_error = null;
			return true;
		}

		/// <summary>
		/// Union of every frame's content bounds, squared off around the source image centre so the
		/// renderer's origin stays dead centre in each cell.
		/// </summary>
		private RectInt ComputeCrop( List<DirectionSource> _directions, int _sourceWidth, int _sourceHeight, out string _error )
		{
			_error = null;

			int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;

			foreach (var direction in _directions)
			{
				var args = new List<string>();
				args.AddRange(direction.Frames);
				args.AddRange(new[] { "-background", "none", "-flatten", "-format", "%@", "info:" });

				string last = null;
				int exit = ImageMagickRunner.RunSync(args.ToArray(),
					line => { if (!string.IsNullOrWhiteSpace(line)) last = line.Trim(); });

				if (exit != 0 || last == null)
				{
					_error = $"ImageMagick could not measure the frames of direction {direction.Angle}.";
					return default;
				}

				var match = s_trimBoxRegex.Match(last);
				if (!match.Success)
					continue;                       // fully transparent direction - nothing to contribute

				int w = int.Parse(match.Groups[1].Value);
				int h = int.Parse(match.Groups[2].Value);
				int x = int.Parse(match.Groups[3].Value);
				int y = int.Parse(match.Groups[4].Value);

				minX = Mathf.Min(minX, x);
				minY = Mathf.Min(minY, y);
				maxX = Mathf.Max(maxX, x + w);
				maxY = Mathf.Max(maxY, y + h);
			}

			if (minX > maxX || minY > maxY)
			{
				_error = "Every frame is fully transparent.";
				return default;
			}

			float centreX = _sourceWidth * 0.5f;
			float centreY = _sourceHeight * 0.5f;

			// Square, and symmetric about the centre: anything else would move the origin from cell to
			// cell and make the subject drift as it turns.
			float reach = Mathf.Max(
				Mathf.Max(centreX - minX, maxX - centreX),
				Mathf.Max(centreY - minY, maxY - centreY)) + CropPadding;

			int side = Mathf.CeilToInt(reach) * 2;
			int originX = Mathf.RoundToInt(centreX) - side / 2;
			int originY = Mathf.RoundToInt(centreY) - side / 2;

			return new RectInt(originX, originY, side, side);
		}

		private bool BuildRow( DirectionSource _direction, RectInt _crop, string _outputFile, out string _error )
		{
			var args = new List<string> { "montage" };
			args.AddRange(_direction.Frames);
			args.AddRange(new[]
			{
				"-background", "none",
				"-crop", $"{_crop.width}x{_crop.height}+{_crop.x}+{_crop.y}",
				"+repage",
				"-resize", $"{CellSize}x{CellSize}!",
				"-tile", $"{_direction.Frames.Length}x1",
				"-geometry", "+0+0",
				"PNG32:" + _outputFile,
			});

			var log = new StringBuilder();
			int exit = ImageMagickRunner.RunSync(args.ToArray(), line => log.AppendLine(line));

			if (exit != 0 || !File.Exists(_outputFile))
			{
				_error = $"ImageMagick failed on direction {_direction.Angle}: {log}";
				return false;
			}

			_error = null;
			return true;
		}

		private static bool AppendRows( List<string> _rowFiles, string _outputFile, out string _error )
		{
			var args = new List<string>();
			args.AddRange(_rowFiles);
			// +repage after the append, or the file keeps the page geometry of a single row strip.
			args.AddRange(new[] { "-background", "none", "-append", "+repage", "PNG32:" + _outputFile });

			var log = new StringBuilder();
			int exit = ImageMagickRunner.RunSync(args.ToArray(), line => log.AppendLine(line));

			if (exit != 0 || !File.Exists(_outputFile))
			{
				_error = $"ImageMagick could not stack the rows: {log}";
				return false;
			}

			_error = null;
			return true;
		}

		private static string ToUnityAssetPath( string _absolute )
		{
			string dataPath = Application.dataPath.Replace('\\', '/');
			if (_absolute.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
				return "Assets" + _absolute.Substring(dataPath.Length);

			string projectRoot = Directory.GetParent(dataPath)?.FullName.Replace('\\', '/');
			if (!string.IsNullOrEmpty(projectRoot) && _absolute.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
				return _absolute.Substring(projectRoot.Length + 1);

			return null;
		}

		/// <summary>
		/// Imported as a plain texture, not as a Sprite: the sheet is addressed by UV rectangle, and a
		/// 2048 sheet has no business in a sprite atlas either.
		/// </summary>
		private void ConfigureTextureImport( string _assetPath )
		{
			if (AssetImporter.GetAtPath(_assetPath) is not TextureImporter importer)
				return;

			importer.textureType = TextureImporterType.Default;
			importer.spriteImportMode = SpriteImportMode.None;
			importer.mipmapEnabled = false;
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.filterMode = FilterMode.Bilinear;
			importer.alphaIsTransparency = true;
			importer.alphaSource = TextureImporterAlphaSource.FromInput;
			importer.npotScale = TextureImporterNPOTScale.None;
			importer.maxTextureSize = Mathf.Max(2048, Mathf.NextPowerOfTwo(CellSize * 16));

			importer.SaveAndReimport();
		}

		private void UpdateSheetAsset(
			string _texturePath,
			List<DirectionSource> _directions,
			RectInt _crop,
			int _sourceWidth,
			int _sourceHeight )
		{
			var sheet = SheetAsset;
			if (sheet == null)
			{
				string sheetPath = Path.ChangeExtension(_texturePath, null) + "_Sheet.asset";
				sheet = CreateInstance<UiDirectionalSpriteSheet>();
				AssetDatabase.CreateAsset(sheet, AssetDatabase.GenerateUniqueAssetPath(sheetPath));
				SheetAsset = sheet;
				EditorUtility.SetDirty(this);
			}

			float anchorX = AnchorPixel.x >= 0f ? AnchorPixel.x : _sourceWidth * 0.5f;
			float anchorY = AnchorPixel.y >= 0f ? AnchorPixel.y : _sourceHeight * 0.5f;

			// Source pixels count down from the top, Unity pivots count up from the bottom.
			float pivotX = (anchorX - _crop.x) / _crop.width;
			float pivotY = 1f - (anchorY - _crop.y) / _crop.height;

			sheet.Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(_texturePath);
			sheet.FrameCount = _directions[0].Frames.Length;
			sheet.DirectionAngles = _directions.Select(d => d.Angle).ToArray();
			sheet.Pivot = new Vector2(pivotX, pivotY);
			sheet.UnitsPerFrame = UnitsPerFrame;

			EditorUtility.SetDirty(sheet);
			AssetDatabase.SaveAssets();
		}
	}
}
