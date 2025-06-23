using System;

namespace GuiToolkit
{
	[AttributeUsage(AttributeTargets.Field)]
	public class PathFieldAttribute : Attribute
	{
		public bool IsFolder;
		public string RelativeToPath;
		public string Extensions; 
		
		public PathFieldAttribute(bool _isFolder, string _relativeToPath = null, string _extensions = null)
		{
			IsFolder = _isFolder;
			RelativeToPath = _relativeToPath;
			Extensions = _extensions;
		}
	}
}