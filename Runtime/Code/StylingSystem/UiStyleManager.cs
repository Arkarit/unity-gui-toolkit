using UnityEngine;

namespace GuiToolkit.Style
{
	public static class UiStyleManager
	{
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
			if (_tweenDuration <= 0)
			{
				foreach (var style in previousStyles)
					foreach (var value in style.Values)
						value.StopTween();

				UiEvents.EvSkinChanged.InvokeAlways();
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

			UiEvents.EvSkinChanged.InvokeAlways();
			return true;
		}
	}
}
