using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleManager
	{
		private static float s_currentTime;
		private static float s_tweenDuration;

		public static string Skin
		{
			get => UiStyleConfig.Instance.CurrentSkinName;
			private set => SetSkin(value);
		}

		public static bool SetSkin(string _skinName, float _tweenDuration = 0)
		{
			var styleConfig = UiStyleConfig.Instance;
			var previousSkin = styleConfig.CurrentSkin;

			if (!styleConfig.SetCurrentSkin(_skinName, false))
				return false;

			var skin = styleConfig.CurrentSkin;
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
					if (!value.IsApplicable)
						continue;

					var prevValue = prevValues[j];
					value.StartTween(prevValue.ValueObj);
					prevValue.StopTween();
				}
			}

			s_currentTime = 0;
			s_tweenDuration = _tweenDuration;
			CoroutineManager.Instance.StartCoroutine(UpdateTween());
			UiEventDefinitions.EvSkinChanged.Invoke(0);
			return true;
		}

		private static IEnumerator UpdateTween()
		{
			while (true)
			{
				var normalizedValue = s_currentTime / s_tweenDuration;
				var styles = UiStyleConfig.Instance.CurrentSkin.Styles;
				foreach (var style in styles)
					foreach (var value in style.Values)
						value.UpdateTween(normalizedValue);

				UiEventDefinitions.EvSkinValuesChanged.Invoke(0);
				if (normalizedValue >= 1)
					yield break;

				s_currentTime += Time.deltaTime;
				yield return 0;
			}
		}
	}
}
