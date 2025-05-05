using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace GuiToolkit.Style.Editor
{
	public static class UiStyleConfigMerger
	{
		private static readonly StringBuilder s_report = new ();

		private class Indent : IDisposable
		{
			private static int s_indent = 0;

			public static string tabs => new string('\t', s_indent);

			public Indent()
			{
				s_indent++;
			}
			public void Dispose()
			{
				s_indent--;
			}
		}

		public enum MergeStyleOptions
		{
			Merge,
			Skip,
			MergeIfNewer,
		}

		public static void Merge(UiStyleConfig _source, UiStyleConfig _target, MergeStyleOptions _mergeStyleOptions = MergeStyleOptions.Skip)
		{
			s_report.AppendLine($"Start Merge Style Config '{AssetDatabase.GetAssetPath(_source)}' into '{AssetDatabase.GetAssetPath(_target)}'\n");

			s_report.AppendLine("Creating skins if not exists");
			foreach (var sourceSkin in _source.Skins)
				using (new Indent())
					CreateSkinIfNotExists(_source, _target, sourceSkin);

			s_report.AppendLine("\nMerging skins");
			foreach (var sourceSkin in _source.Skins)
				using (new Indent())
					MergeSkin(_source, _target, sourceSkin, _mergeStyleOptions);

			foreach (var targetSkin in _target.Skins)
				using (new Indent())
					targetSkin.BuildDictionaries();

			var reportString = s_report.ToString();
			Debug.Log(reportString);
			s_report.Clear();
		}

		private static void CreateSkinIfNotExists(UiStyleConfig _source, UiStyleConfig _target, UiSkin _sourceSkin)
		{
			if (!_target.SkinNames.Contains(_sourceSkin.Name))
			{
				Report($"Add skin '{_sourceSkin.Name}' to UiStyleConfig '{AssetDatabase.GetAssetPath(_target)}'");
				UiStyleEditorUtility.AddSkin(_target, _sourceSkin.Name,
					_target.NumSkins == 0 ? null : _target.Skins[0].Name);
			}
		}

		private static void MergeSkin(UiStyleConfig _source, UiStyleConfig _target, UiSkin _sourceSkin, MergeStyleOptions _mergeStyleOptions)
		{
			Report($"Merge skin '{_sourceSkin.Name}' to UiStyleConfig '{AssetDatabase.GetAssetPath(_target)}'");
			var targetSkin = _target.GetSkinByName(_sourceSkin.Name);
			Debug.Assert(targetSkin != null, $"target skin '{_sourceSkin.Name}' is not defined!");

			foreach (var sourceStyle in _sourceSkin.Styles)
				CreateStyleIfNotExists(_source, _target, targetSkin, sourceStyle);

			if (_mergeStyleOptions == MergeStyleOptions.Skip)
				return;

			Report("Merging styles");
			foreach (var sourceStyle in _sourceSkin.Styles)
				using (new Indent())
					MergeStyle(_source, _target, targetSkin, sourceStyle, _mergeStyleOptions);
		}

		private static void CreateStyleIfNotExists( UiStyleConfig _source, UiStyleConfig _target, UiSkin _targetSkin, UiAbstractStyleBase _sourceStyle )
		{
			foreach (var targetSkin in _target.Skins)
			{
				UiAbstractStyleBase targetStyle= GetTargetStyle(_targetSkin, _sourceStyle);
				if (targetStyle != null) 
					continue;

				targetStyle = _sourceStyle.DeepClone();
				targetStyle.StyleConfig = _target;
				targetSkin.Styles.Add(targetStyle);
				Report($"Cloned source style '{_sourceStyle.Name}', Config '{AssetDatabase.GetAssetPath(_sourceStyle.StyleConfig)}' to '{targetStyle.Name}' skin '{targetSkin.Name}', Config '{AssetDatabase.GetAssetPath(targetStyle.StyleConfig)}'");
			}
		}

		private static void MergeStyle(UiStyleConfig _source, UiStyleConfig _target, UiSkin _targetSkin, UiAbstractStyleBase _sourceStyle, MergeStyleOptions _mergeStyleOptions)
		{
			Report($"Merging style '{_sourceStyle.Name}'");
			UiAbstractStyleBase targetStyle= GetTargetStyle(_targetSkin, _sourceStyle);
			Debug.Assert(targetStyle != null, $"A matching style '{_sourceStyle.Name}' does not exist in '{_targetSkin.Name}'!");
			var sourceValueInfos = _sourceStyle.GetValueInfoArray();
			var targetValueInfos = targetStyle.GetValueInfoArray();

			foreach (var sourceValueInfo in sourceValueInfos)
			{
				var targetValueInfo = targetValueInfos.FirstOrDefault(info => info.Equals(sourceValueInfo) );
				if (targetValueInfo == null)
				{
					Report($"Target style '{targetStyle.Name}' does not contain a property '{sourceValueInfo.GetterName}'.");
					continue;
				}

				var sourceValue = sourceValueInfo.Value;
				var targetValue = targetValueInfo.Value;

				// Equal access time; we assume values are identical.
				if (sourceValue.IsAccessTimeEqual(targetValue))
					continue;

				if (_mergeStyleOptions == MergeStyleOptions.MergeIfNewer)
				{
					if (targetValue.IsAccessTimeLater(targetValue))
					{
						Report($"Target value '{targetValueInfo.GetterName}' access time ({targetValue.AccessTime}) is later than source access time ({sourceValue.AccessTime}).");
						continue;
					}
				}

				Report($"Value '{targetValueInfo.GetterName}' merged.");
				targetValue.RawValueObj = sourceValue.RawValueObj;
				targetValue.AccessTime = sourceValue.AccessTime;
			}
		}

		private static UiAbstractStyleBase GetTargetStyle(UiSkin _targetSkin, UiAbstractStyleBase _sourceStyle) => _targetSkin.StyleByKey(_sourceStyle.Key);


		private static void Report(string _s)
		{
			Debug.Log(_s);
			s_report.AppendLine(Indent.tabs + _s);
		}

	}
}