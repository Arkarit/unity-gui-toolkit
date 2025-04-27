using System;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	public static class UiStyleConfigMerger
	{
		public struct Entry
		{
			public UiSkin Skin;
			public UiAbstractApplyStyleBase Style;
			public ApplicableValueBase Value;
		}

		public static void Merge(UiStyleConfig _source, UiStyleConfig _target)
		{
			foreach (var skin in _source.Skins)
				MergeSkin(_source, _target, skin);
		}

		private static void MergeSkin(UiStyleConfig _source, UiStyleConfig _target, UiSkin _sourceSkin)
		{
			if (!_target.SkinNames.Contains(_sourceSkin.Name))
			{
				UiStyleEditorUtility.AddSkin(_target, _sourceSkin.Name,
					_target.NumSkins == 0 ? null : _target.Skins[0].Name);
			}

			var targetSkin = _target.GetSkinByName(_sourceSkin.Name);
			Debug.Assert(targetSkin != null, $"target skin '{_sourceSkin.Name}' is not defined!");

			foreach (var style in _sourceSkin.Styles)
				MergeStyle(_source, _target, targetSkin, style);
		}

		private static void MergeStyle( UiStyleConfig _source, UiStyleConfig _target, UiSkin _targetSkin, UiAbstractStyleBase _sourceStyle )
		{
			// We need to also take affected component into account, not only the name. So we use Key here.
			var sourceKey = _sourceStyle.Key;
			var targetStyle = _targetSkin.StyleByKey(sourceKey);
			if (targetStyle == null)
			{
				targetStyle = _sourceStyle.DeepClone();
				_targetSkin.Styles.Add(targetStyle);
			}


		}

	}
}