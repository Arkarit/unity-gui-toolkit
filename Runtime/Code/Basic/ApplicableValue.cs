using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	// Just a stub to satisfy PropertyDrawer
	[Serializable]
	public abstract class ApplicableValueBase
	{
		public bool IsApplicable = false;
#if UNITY_EDITOR
		[NonSerialized] public ETriState ValueHasChildren = ETriState.Indeterminate;
#endif
		public abstract object ValueObj { get;}
	}
	
	[Serializable]
	public class ApplicableValue<T> : ApplicableValueBase
	{
		[FormerlySerializedAs("Value")]
		public T m_value;
		public override object ValueObj => m_value;

		private T m_oldValue;
		private bool m_tweenRunning;
		private float m_normalizedTweenAmount;

		public T Value
		{
			get
			{
				if (!m_tweenRunning)
					return m_value;

				return Lerp(m_oldValue, m_value, m_normalizedTweenAmount);
			}

			set => m_value = value;
		}

		public void StartTween(T from)
		{
			m_oldValue = from;
			m_tweenRunning = true;
			m_normalizedTweenAmount = 0f;
		}

		public void UpdateTween(float normalizedTweenAmount)
		{
			m_normalizedTweenAmount = Mathf.Clamp01(normalizedTweenAmount);
			if (m_normalizedTweenAmount >= 1)
				m_tweenRunning = false;
		}

		public ApplicableValue<T> Clone() => new ApplicableValue<T>()
		{
			IsApplicable = IsApplicable,
			m_value = m_value,
			m_oldValue = m_oldValue
		};

		private static T Lerp<T>(T _lhs, T _rhs, float _amount)
		{
			return (_lhs, _rhs) switch
			{
				(double lhs, double rhs) => (T)(object)(lhs + (rhs - lhs) * Mathf.Clamp01(_amount)),
				(float lhs, float rhs) => (T)(object)Mathf.Lerp(lhs, rhs, _amount),
				(int lhs, int rhs) => (T)(object)Mathf.Lerp(lhs, rhs, _amount),
				(char lhs, char rhs) => (T)(object)Mathf.Lerp(lhs, rhs, _amount),
				(byte lhs, byte rhs) => (T)(object)Mathf.Lerp(lhs, rhs, _amount),
				(Vector2 lhs, Vector2 rhs) => (T)(object)Vector2.Lerp(lhs, rhs, _amount),
				(Vector3 lhs, Vector3 rhs) => (T)(object)Vector3.Lerp(lhs, rhs, _amount),
				(Vector4 lhs, Vector4 rhs) => (T)(object)Vector4.Lerp(lhs, rhs, _amount),
				(Vector2Int lhs, Vector2Int rhs) => (T)(object)Vector2.Lerp(lhs, rhs, _amount),
				(Vector3Int lhs, Vector3Int rhs) => (T)(object)Vector3.Lerp(lhs, rhs, _amount),
				(Quaternion lhs, Quaternion rhs) => (T)(object)Quaternion.Slerp(lhs, rhs, _amount),
				(Color lhs, Color rhs) => (T)(object)Color.Lerp(lhs, rhs, _amount),
				_ => _amount >= .5f ? _rhs : _lhs
			};
		}
	}
}
