using DigitalRuby.Tween;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class UiProgressBarBase : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text m_text;

		public UiSimpleAnimation m_animationOnDecrease;
		public UiSimpleAnimation m_animationOnIncrease;
		public UiSimpleAnimation m_animationOnReachMax;
		public UiSimpleAnimation m_animationOnReachMin;

		private static readonly string s_formatString = "{0} / {1}";
		private static StringBuilder s_stringBuilder = new StringBuilder(80);

		protected abstract void DisplayProgress(float _progressNormalized);

		private float m_lastNorm = 0;

		public void SetProgress(float _current, float _max, string _formatString = null, float _duration = 0, string _textPrefix = null)
		{
			float norm = Mathf.Clamp01( _current / _max);

			if (m_text)
			{
				s_stringBuilder.Clear();

				if (!string.IsNullOrEmpty(_textPrefix))
					s_stringBuilder.Append(_textPrefix);

				if (string.IsNullOrEmpty(_formatString))
					s_stringBuilder.AppendFormat(s_formatString, _current, _max);
				else
					s_stringBuilder.AppendFormat(_formatString, _current, _max);

				//TODO: Number counter tween
				m_text.text = s_stringBuilder.ToString();
			}
			
			if (_duration == 0)
			{
				DisplayProgress(norm);
				DisplayAnimAndSetLastValue(norm);
				return;
			}

            System.Action<ITween<float>> updateBar = (t) =>
            {
				DisplayProgress( t.CurrentValue );
            };

            System.Action<ITween<float>> finished = (t) =>
            {
				DisplayProgress( t.CurrentValue );
				DisplayAnimAndSetLastValue(t.CurrentValue);
            };

			gameObject.Tween("progressBar", m_lastNorm, _current, _duration, TweenScaleFunctions.QuadraticEaseInOut, updateBar, finished );
		}

		private void DisplayAnimAndSetLastValue(float _normalizedProgress)
		{
			if (m_lastNorm == _normalizedProgress)
				return;

			if (
				m_animationOnReachMin != null
				&& Mathf.Approximately( _normalizedProgress, 0 ) 
				&& m_lastNorm > _normalizedProgress 
				)
			{
				m_animationOnReachMin.Play();
			}
			else if (
				m_animationOnReachMax != null
				&& Mathf.Approximately( _normalizedProgress, 1 ) 
				&& m_lastNorm < _normalizedProgress 
				)
			{
				m_animationOnReachMax.Play();
			}
			else if (
				m_animationOnIncrease != null
				&& m_lastNorm < _normalizedProgress 
				)
			{
				m_animationOnIncrease.Play();
			}
			else if (
				m_animationOnDecrease != null
				&& m_lastNorm > _normalizedProgress 
				)
			{
				m_animationOnDecrease.Play();
			}

			m_lastNorm = _normalizedProgress;
		}
	}
}