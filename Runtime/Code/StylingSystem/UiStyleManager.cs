using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleManager
	{
		private static float s_currentTime;
		private static float s_tweenDuration;

		/// <summary>
		/// One style of the outgoing skin together with the style of the incoming skin that continues it.
		/// </summary>
		public readonly struct StyleTweenPair
		{
			public readonly UiAbstractStyleBase From;
			public readonly UiAbstractStyleBase To;

			public StyleTweenPair( UiAbstractStyleBase _from, UiAbstractStyleBase _to )
			{
				From = _from;
				To = _to;
			}
		}

		/// <summary>
		/// Matches the styles of two skins by KEY, i.e. by component type and name.
		///
		/// This used to pair them by position, with an assert that both skins hold the same number of
		/// styles. That held as long as every skin carried every style, which stopped being true the moment
		/// a config could inherit: a child stores only its overrides, so two of its skins may well hold
		/// different subsets. Position would then tween a button background into a headline colour, and the
		/// count assert would refuse the skin change outright.
		///
		/// A style without a counterpart yields no pair - there is nothing to tween it from, so it simply
		/// takes its new value.
		/// </summary>
		public static List<StyleTweenPair> PairStylesByKey
		(
			IReadOnlyList<UiAbstractStyleBase> _from,
			IReadOnlyList<UiAbstractStyleBase> _to
		)
		{
			var result = new List<StyleTweenPair>();
			if (_from == null || _to == null)
				return result;

			var fromByKey = new Dictionary<int, UiAbstractStyleBase>(_from.Count);
			foreach (var style in _from)
			{
				if (style != null)
					fromByKey[style.Key] = style;
			}

			foreach (var style in _to)
			{
				if (style == null)
					continue;

				if (fromByKey.TryGetValue(style.Key, out var counterpart))
					result.Add(new StyleTweenPair(counterpart, style));
			}

			return result;
		}

		public static bool SetSkin(string _skinName, float _tweenDuration = 0) => SetSkin(UiStyleConfig.Instance, _skinName, _tweenDuration);
		public static bool SetSkin(UiStyleConfig _styleConfig, string _skinName, float _tweenDuration = 0)
		{
			if (_styleConfig == null)
				_styleConfig = UiStyleConfig.Instance;

			var previousSkin = _styleConfig.CurrentSkin;
			if (previousSkin != null && previousSkin.Name == _skinName)
				return false;

			if (!_styleConfig.SetCurrentSkinByNameOrAlias(_skinName, false, false))
				return false;

			var skin = _styleConfig.CurrentSkin;
			if (skin == previousSkin)
				return true;

			// The effective set, not the raw list: with an inheriting config most of what a skin resolves to
			// lives in the parent, and a tween that skipped those would leave the majority of the UI to
			// jump while a few overridden styles glide.
			var previousStyles = previousSkin.EffectiveStyles;
			if (_tweenDuration <= 0 || !Application.isPlaying)
			{
				foreach (var style in previousStyles)
					foreach (var value in style.Values)
						value.StopTween();

				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
				return true;
			}

			var styles = skin.EffectiveStyles;
			var pairs = PairStylesByKey(previousStyles, styles);

			foreach (var pair in pairs)
			{
				var values = pair.To.Values;
				var valuesLength = values.Length;
				var prevValues = pair.From.Values;

				// Same key means same component type and name, so the two styles are of the same class and
				// carry the same values in the same order. If that ever stops holding, tweening the pair
				// would read the wrong value, so it is skipped rather than guessed.
				if (prevValues.Length != valuesLength)
				{
					UiLog.LogError($"Style '{pair.To.Alias}' has {valuesLength} values in the new skin but " +
					               $"{prevValues.Length} in the previous one; not tweened.");
					continue;
				}

				for (int j = 0; j < valuesLength; j++)
				{
					var value = values[j];
					if (value == null)
					{
						UiLog.LogError($"Value {j} of style '{pair.To.Alias}' (type '{pair.To.GetType().Name}') is null!");
						continue;
					}

					if (!value.IsApplicable)
						continue;

					var prevValue = prevValues[j];
					value.StartTween(prevValue.ValueObj);
					prevValue.StopTween();
				}
			}

			s_currentTime = 0;
			s_tweenDuration = _tweenDuration;
			// The list is captured here rather than re-read every frame: it is the set that was just
			// prepared for tweening, and rebuilding an effective set per frame would be wasteful.
			CoRoutineRunner.Instance.StartCoroutine(UpdateTween(styles));
			UiEventDefinitions.EvSkinChanged.Invoke(_tweenDuration);
			return true;
		}

		private static IEnumerator UpdateTween(List<UiAbstractStyleBase> _styles)
		{
			yield return 0;
			while (true)
			{
				var normalizedValue = s_currentTime / s_tweenDuration;
				foreach (var style in _styles)
					foreach (var value in style.Values)
						value.UpdateTween(normalizedValue);

				UiEventDefinitions.EvSkinValuesChanged.Invoke(normalizedValue);
				if (normalizedValue >= 1)
					yield break;

				s_currentTime += Time.deltaTime;
				yield return 0;
			}
		}
	}
}
