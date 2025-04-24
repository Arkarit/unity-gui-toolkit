using System;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	public static class UiStyleConfigMerger
	{
		public enum EOptions
		{
			Skip,
			Overwrite,
			Ask,
		}

		public struct Entry
		{
			public string SkinName;
			public string StyleName;
			public Type SupportedComponentType;
			public ApplicableValueBase Value;
		}

		public static void Merge(UiStyleConfig _source, UiStyleConfig _target, EOptions _options)
		{
			foreach (var skin in _source.Skins)
			{
				foreach (var style in skin.Styles)
				{
					foreach (var value in style.Values)
					{
					}
				}
			}
		}
	}
}