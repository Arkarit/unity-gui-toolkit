#define DEBUG_MERGE
using System;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

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
			foreach (var sourceSkin in _source.Skins)
				CreateSkinIfNotExists(_source, _target, sourceSkin);

			foreach (var targetSkin in _target.Skins)
				targetSkin.BuildDictionaries();

			foreach (var sourceSkin in _source.Skins)
				MergeSkin(_source, _target, sourceSkin);
		}

		private static void CreateSkinIfNotExists(UiStyleConfig _source, UiStyleConfig _target, UiSkin _sourceSkin)
		{
			if (!_target.SkinNames.Contains(_sourceSkin.Name))
			{
				Log($"Add skin '{_sourceSkin.Name}' to UiStyleConfig '{AssetDatabase.GetAssetPath(_target)}'");
				UiStyleEditorUtility.AddSkin(_target, _sourceSkin.Name,
					_target.NumSkins == 0 ? null : _target.Skins[0].Name);
			}
		}

		private static void MergeSkin(UiStyleConfig _source, UiStyleConfig _target, UiSkin _sourceSkin)
		{
			Log($"Merge skin '{_sourceSkin.Name}' to UiStyleConfig '{AssetDatabase.GetAssetPath(_target)}'");
			var targetSkin = _target.GetSkinByName(_sourceSkin.Name);
			Debug.Assert(targetSkin != null, $"target skin '{_sourceSkin.Name}' is not defined!");
			foreach (var style in _sourceSkin.Styles)
				CreateStyleIfNotExists(_source, _target, targetSkin, style);
		}

		private static void CreateStyleIfNotExists( UiStyleConfig _source, UiStyleConfig _target, UiSkin _targetSkin, UiAbstractStyleBase _sourceStyle )
		{
			foreach (var targetSkin in _target.Skins)
			{
				var sourceKey = _sourceStyle.Key;
				var targetStyle = _targetSkin.StyleByKey(sourceKey);
				if (targetStyle != null)
					continue;

				targetStyle = _sourceStyle.DeepClone();
				targetSkin.Styles.Add(targetStyle);
			}
		}

		[Conditional("DEBUG_MERGE")]
		private static void Log(string _s) => Debug.Log($"---::: {_s}");

	}
}