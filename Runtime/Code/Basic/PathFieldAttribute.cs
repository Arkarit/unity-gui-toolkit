using System;

namespace GuiToolkit
{
	[AttributeUsage(AttributeTargets.Field)]
	public class PathFieldAttribute : Attribute
	{
		public bool IsFolder;
		public bool IsRelativeIfPossible;
		
		public PathFieldAttribute(bool _isFolder, bool _isRelativeIfPossible = true)
		{
			IsFolder = _isFolder;
			IsRelativeIfPossible = _isRelativeIfPossible;
		}
	}
}