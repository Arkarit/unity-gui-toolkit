using System;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	public static class UiStyleConfigMerger
	{
		public enum EOptionExisting
		{
			Skip,
			Overwrite,
			Ask,
		}

		public enum EOptionMissing
		{
			Keep,
			Remove,
			Ask,
		}

		public enum EOptionAdditional
		{
			Skip,
			Add,
			Ask,
		}

		[Serializable]
		public class Option
		{
			public EOptionExisting Existing;
			public EOptionMissing Missing;
			public EOptionAdditional Additional;
		}

		[Serializable]
		public class Options
		{
			private Option SkinOptions = new Option()
			{
				Existing = EOptionExisting.Overwrite,
				Missing = EOptionMissing.Keep,
				Additional = EOptionAdditional.Ask,
			};
		}

		public struct Entry
		{
			public UiSkin Skin;
			public UiAbstractApplyStyleBase Style;
			public ApplicableValueBase Value;
		}

		public static void Merge(UiStyleConfig _source, UiStyleConfig _target, Options _options)
		{
			foreach (var skin in _source.Skins)
			{
				foreach (var style in skin.Styles)
				{
					var valueInfoArray = style.GetValueInfoArray();
					foreach (var valueInfo in valueInfoArray)
					{
					}
				}
			}
		}
	}
}