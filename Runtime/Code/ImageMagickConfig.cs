using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// <para>Configuration singleton for the ImageMagick bridge.</para>
	/// <para>
	/// The bridge invokes an external ImageMagick installation via Process.Start (see
	/// <see cref="ImageMagickRunner"/>). This config holds bridge-wide settings; per-generator
	/// settings (sunburst parameters, etc.) belong on their own ScriptableObjects.
	/// </para>
	/// </summary>
	[CreateAssetMenu(fileName = nameof(ImageMagickConfig), menuName = StringConstants.CREATE_IMAGEMAGICK_CONFIG)]
	public class ImageMagickConfig : AbstractSingletonScriptableObject<ImageMagickConfig>
	{
#if UNITY_EDITOR
		[Tooltip("Optional full path to the magick executable. If empty, PATH is searched. "
		         + "Use this only if you have ImageMagick in a non-standard location or want to pin a specific version.")]
		public string ExecutableOverride;

		[Tooltip("Default output directory for generated assets. Individual generators may override this.")]
		[PathField(_isFolder:true, _relativeToPath:".")]
		public PathField DefaultOutputDirectory;

		[Tooltip("Keep intermediate files (SVGs, temp images) after generation for debugging. "
		         + "Off by default; enable when a generator misbehaves to inspect the intermediate state.")]
		public bool KeepIntermediateFiles;
#endif
	}
}
