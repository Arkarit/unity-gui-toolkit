using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor window hosting the <see cref="ImageMagickConfig"/> inspector.
	/// Mirrors <see cref="DoxygenWindow"/> in shape and behavior.
	/// </summary>
	public class ImageMagickWindow : EditorWindow, IEditorAware
	{
		private static ImageMagickWindow s_window;

		public static ImageMagickWindow Instance => s_window;

		[MenuItem("Window/ImageMagick Bridge")]
		public static void Init()
		{
			s_window = (ImageMagickWindow)GetWindow(typeof(ImageMagickWindow), false, "ImageMagick");
			s_window.minSize = new Vector2(420, 245);
			s_window.maxSize = new Vector2(420, 720);
		}

		private void OnGUI()
		{
			if (!AssetReadyGate.Ready)
				GUIUtility.ExitGUI();

			EditorDisplayHelper.Draw(ImageMagickConfig.Instance, "ImageMagickConfig instance is null. Please create one.");
		}
	}
}
