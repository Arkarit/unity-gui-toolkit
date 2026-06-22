using System;

namespace GuiToolkit
{
	[AttributeUsage(AttributeTargets.Field)]
	public class PathFieldAttribute : Attribute
	{
		public bool IsFolder;
		public string RelativeToPath;
		public string Extensions;

		/// <summary>
		/// When true, the picker uses a save-style dialog that allows selecting paths to files/folders
		/// that do not exist yet (intended for output paths). Default: false — the picker requires
		/// the user to choose an existing file/folder.
		/// </summary>
		public bool AllowNonExistent;

		public PathFieldAttribute(bool _isFolder, string _relativeToPath = null, string _extensions = null, bool _allowNonExistent = false)
		{
			IsFolder = _isFolder;
			RelativeToPath = _relativeToPath;
			Extensions = _extensions;
			AllowNonExistent = _allowNonExistent;
		}
	}
}