using System.Collections;
using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleManager
	{
		private static float s_currentTime;
		private static float s_tweenDuration;

		public static bool SetSkin(string _skinName, float _tweenDuration = 0) => SetSkin(UiMainStyleConfig.Instance, _skinName, _tweenDuration);
		public static bool SetSkin(UiStyleConfig _styleConfig, string _skinName, float _tweenDuration = 0)
		{
			if (_styleConfig == null)
				_styleConfig = UiMainStyleConfig.Instance;

			var previousSkin = _styleConfig.CurrentSkin;
			if (previousSkin != null && previousSkin.Name == _skinName)
				return false;

			if (!_styleConfig.SetCurrentSkinByNameOrAlias(_skinName, false, false))
				return false;

			var skin = _styleConfig.CurrentSkin;
			if (skin == previousSkin)
				return true;

			var previousStyles = previousSkin.Styles;
			var previousStylesCount = previousStyles.Count;
			if (_tweenDuration <= 0 || !Application.isPlaying)
			{
				foreach (var style in previousStyles)
					foreach (var value in style.Values)
						value.StopTween();

				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
				return true;
			}

			var styles = skin.Styles;
			var stylesCount = styles.Count;
			Debug.Assert(previousStylesCount == stylesCount);
			if (previousStyles.Count != stylesCount)
				return false;

			for (int i = 0; i < stylesCount; i++)
			{
				var values = styles[i].Values;
				var valuesLength = values.Length;
				var prevValues = previousStyles[i].Values;
				var prevValuesLength = prevValues.Length;

				Debug.Assert(prevValuesLength == valuesLength);
				if (prevValuesLength != valuesLength)
					return false;

				for (int j = 0; j < valuesLength; j++)
				{
					var value = values[j];
					if (value == null)
					{
						UiLog.LogError($"Value {j} of style '{styles[i].Alias}' (type '{styles[i].GetType().Name}') is null!");
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
			CoRoutineRunner.Instance.StartCoroutine(UpdateTween(_styleConfig));
			UiEventDefinitions.EvSkinChanged.Invoke(_tweenDuration);
			return true;
		}

		private static IEnumerator UpdateTween(UiStyleConfig _styleConfig)
		{
			yield return 0;
			while (true)
			{
				var normalizedValue = s_currentTime / s_tweenDuration;
				var styles = _styleConfig.CurrentSkin.Styles;
				foreach (var style in styles)
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
