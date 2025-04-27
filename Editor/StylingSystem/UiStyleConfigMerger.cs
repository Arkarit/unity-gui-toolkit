using System;
using UnityEditor;
using UnityEngine;

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
			public Option StyleOptions = new();
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
				MergeSkin(_source, _target, _options, skin);
		}

		private static void MergeSkin(UiStyleConfig _source, UiStyleConfig _target, Options _options, UiSkin _sourceSkin)
		{
			if (!_target.SkinNames.Contains(_sourceSkin.Name))
			{
				switch (_options.SkinOptions.MissingTarget)
				{
					case EOptionMissingTarget.Skip:
						return;
					case EOptionMissingTarget.Add:
						AddSkin(_target, _sourceSkin, false);
						break;
					case EOptionMissingTarget.Ask:
						if (!AddSkin(_target, _sourceSkin, true))
							return;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			var targetSkin = _target.GetSkinByName(_sourceSkin.Name);
			Debug.Assert(targetSkin != null, $"target skin '{_sourceSkin.Name}' is not defined!");

			foreach (var style in _sourceSkin.Styles)
				MergeStyle(_source, _target, _options, targetSkin, style);
		}

		private static bool AddSkin(UiStyleConfig _target, UiSkin _sourceSkin, bool _ask)
		{
			if (_ask && !EditorUtility.DisplayDialog("Add skin?",
				    $"Do you want the skin '{_sourceSkin.Name}' to be added?", "Ok", "Cancel"))
				return false;

			UiStyleEditorUtility.AddSkin(_target, _sourceSkin.Name,
				_target.NumSkins == 0 ? null : _target.Skins[0].Name);

			return true;
		}

		private static void MergeStyle( UiStyleConfig _source, UiStyleConfig _target, Options _options, UiSkin _targetSkin, UiAbstractStyleBase _sourceStyle )
		{
			// We need to also take affected component into account, not only the name. So we use Key here.
			var sourceKey = _sourceStyle.Key;
			if (!_targetSkin.HasStyle(sourceKey))
			{
				switch (_options.StyleOptions.MissingTarget)
				{
					case EOptionMissingTarget.Skip:
						return;
					case EOptionMissingTarget.Add:
						break;
					case EOptionMissingTarget.Ask:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

	}
}