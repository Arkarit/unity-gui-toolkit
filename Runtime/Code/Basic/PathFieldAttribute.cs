using System;

namespace GuiToolkit
{
	[AttributeUsage(AttributeTargets.Field)]
	public class PathFieldAttribute : Attribute
	{
		public bool IsFolder;
		public string RelativeToPath;
		
		public PathFieldAttribute(bool isFolder, string relativeToPath = null)
		{
			IsFolder = isFolder;
			RelativeToPath = relativeToPath;
		}
	}
}