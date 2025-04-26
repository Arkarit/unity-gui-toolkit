using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit.Style.Editor
{
	public static class UiStyleConfigMerger
	{
		// In case someone is wondering why there are 3 enums having more or less the same parameters:
		// Yes, their values are pretty much the same, but wording is much more understandable in Editor
		public enum EOptionExistingBoth
		{
			Skip,
			Overwrite,
			Ask,
		}

		public enum EOptionMissingSource
		{
			Keep,
			Remove,
			Ask,
		}

		public enum EOptionMissingTarget
		{
			Skip,
			Add,
			Ask,
		}

		[Serializable]
		public class Option
		{
			public EOptionExistingBoth ExistingBoth = EOptionExistingBoth.Overwrite;
			public EOptionMissingSource MissingSource = EOptionMissingSource.Keep;
			public EOptionMissingTarget MissingTarget = EOptionMissingTarget.Add;
		}

		[Serializable]
		public class Options
		{
			public Option SkinOptions = new();
		}

		public struct Entry
		{
			public UiSkin Skin;
			public UiAbstractApplyStyleBase Style;
			public ApplicableValueBase Value;
		}

		public static void Merge(UiStyleConfig _source, UiStyleConfig _target, Options _options)
		{
			for (int i = 0; i < _source.Skins.Count; i++)
				Merge(_source, _target, _options, _source.Skins[0]);
		}

		private static void Merge(UiStyleConfig _source, UiStyleConfig _target, Options _options, UiSkin _sourceSkin)
		{
			if (!_target.SkinNames.Contains(_sourceSkin.Name))
			{
				switch (_options.SkinOptions.MissingTarget)
				{
					case EOptionMissingTarget.Skip:
						return;
					case EOptionMissingTarget.Add:
						UiStyleEditorUtility.AddSkin(_target, _sourceSkin.Name,
							_target.NumSkins == 0 ? null : _target.Skins[0].Name);
						break;
					case EOptionMissingTarget.Ask:
//						if (!EditorUtility.DisplayDialog())
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}