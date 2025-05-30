using System;
using System.IO;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Purpose of this class to create a path property drawer (with path select button)
	/// </summary>
	[Serializable]
	public struct PathField
	{
		public string Path;

		public bool IsRelative
		{
			get
			{
				if (string.IsNullOrEmpty(Path))
					return false;

				return Path.StartsWith('.');
			}
		}

		public string FullPath
		{
			get
			{
				if (string.IsNullOrEmpty(Path))
					return null;

				if (IsRelative)
					return System.IO.Path.GetFullPath("./" + Path);

				return Path;
			}
		}
		
		public PathField(string _val = null) => Path = _val;
		public static implicit operator string(PathField _val) => _val.Path;
	}
}
