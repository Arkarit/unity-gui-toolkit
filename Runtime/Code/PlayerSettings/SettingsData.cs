using System.Collections.Generic;
using System;

[Serializable]
public sealed class SettingsData
{
	public readonly Dictionary<string, int> ints = new();
	public readonly Dictionary<string, float> floats = new();
	public readonly Dictionary<string, string> strings = new();
	public readonly Dictionary<string, bool> bools = new();
}
