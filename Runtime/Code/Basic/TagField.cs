using System;

namespace GuiToolkit
{
	/// <summary>
	/// Purpose of this class to create a tag property drawer (e.g. for list of tags)
	/// </summary>
	[Serializable]
	public struct TagField
	{
		public string Tag;
		
		public TagField(string _val = null) => Tag = _val ?? "Untagged";
		public static implicit operator string(TagField _val) => _val.Tag;
	}
}
