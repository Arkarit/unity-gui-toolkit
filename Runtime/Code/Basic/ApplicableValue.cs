using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[Serializable]
	public abstract class ApplicableValueBase
	{
		public bool IsApplicable = false;
		protected object m_oldValue;
		protected bool m_tweenRunning;
		protected float m_normalizedTweenAmount;


#if UNITY_EDITOR
		[NonSerialized] public ETriState ValueHasChildren = ETriState.Indeterminate;
#endif
		public abstract object RawValueObj { get; }
		public abstract object ValueObj { get; }

		public void StartTween(object from)
		{
			m_oldValue = from;
			m_tweenRunning = true;
			m_normalizedTweenAmount = 0f;
		}

		public void StopTween() => UpdateTween(1);

		public void UpdateTween(float normalizedTweenAmount)
		{
			if (!m_tweenRunning)
				return;

			m_normalizedTweenAmount = Mathf.Clamp01(normalizedTweenAmount);
			if (m_normalizedTweenAmount >= 1)
				m_tweenRunning = false;
		}

		protected static object Lerp(object _lhs, object _rhs, float _amount)
		{
			return (_lhs, _rhs) switch
			{
				(double lhs, double rhs) => lhs + (rhs - lhs) * Mathf.Clamp01(_amount),
				(float lhs, float rhs) => Mathf.Lerp(lhs, rhs, _amount),
				(int lhs, int rhs) => Mathf.Lerp(lhs, rhs, _amount),
				(char lhs, char rhs) => Mathf.Lerp(lhs, rhs, _amount),
				(byte lhs, byte rhs) => Mathf.Lerp(lhs, rhs, _amount),
				(Vector2 lhs, Vector2 rhs) => Vector2.Lerp(lhs, rhs, _amount),
				(Vector3 lhs, Vector3 rhs) => Vector3.Lerp(lhs, rhs, _amount),
				(Vector4 lhs, Vector4 rhs) => Vector4.Lerp(lhs, rhs, _amount),
				(Vector2Int lhs, Vector2Int rhs) => Vector2.Lerp(lhs, rhs, _amount),
				(Vector3Int lhs, Vector3Int rhs) => Vector3.Lerp(lhs, rhs, _amount),
				(Quaternion lhs, Quaternion rhs) => Quaternion.Slerp(lhs, rhs, _amount),
				(Color lhs, Color rhs) => Color.Lerp(lhs, rhs, _amount),
				_ => _amount >= .5f ? _rhs : _lhs
			};
		}
	}
	
	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		[FormerlySerializedAs("Value")]
		[SerializeField] protected T m_value;

		public override object RawValueObj => m_value;

		public override object ValueObj
		{
			get
			{
				if (!m_tweenRunning)
					return m_value;

				return Lerp(m_oldValue, m_value, m_normalizedTweenAmount);
			}
		}

		public T Value
		{
			get => (T) ValueObj;
			set => m_value = value;
		}

		public ApplicableValue<T> Clone() => new ApplicableValue<T>()
		{
			IsApplicable = IsApplicable,
			m_value = m_value,
			m_oldValue = m_oldValue
		};
	}
}
