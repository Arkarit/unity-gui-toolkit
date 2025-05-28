using System;

namespace GuiToolkit
{
	/// <summary>
	/// Purpose of this class to create a path property drawer (with path select button)
	/// </summary>
	[Serializable]
	public struct PathField
	{
		public string Path;
		
		public PathField(string _val = null) => Path = _val;
		public static implicit operator string(PathField _val) => _val.Path;
	}
}
