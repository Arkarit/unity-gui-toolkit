namespace GuiToolkit
{
	// A struct mimicking C "bool" handling
	// As C doesn't know bools but only ints, you can do all kinds of numerical and logical operations
	// on them. C# is different; it strictly distinguishes between bool (only logical operations) and int (only numerical operations)
	// CBool mimicks this behaviour by implicit conversions, so that you can
	// write CBool cb = 100 as well as CBool cb = true.
	// As it contradicts the usual C# rules and is questionable regarding performance, it should be only used for special cases like integrating 
	// C code without changes (See LocaPlurals.cs)
	public struct CBool
	{
		private int m_value;
		public static implicit operator bool(CBool _val) => _val.m_value != 0;
		public static implicit operator int(CBool _val) => _val.m_value;
		public static implicit operator CBool(int _val) => new CBool(_val);
		public static implicit operator CBool(bool _val) => new CBool(_val);
		public CBool(int _val = 0) { m_value = _val; }
		public CBool(bool _val) { m_value = _val ? 1 : 0; }
	}
}
