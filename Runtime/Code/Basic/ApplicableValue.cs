using System;
using UnityEngine;

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
		public T Value;
		public override object ValueObj => Value;

		private T m_oldValue;

		public ApplicableValue<T> Clone() => new ApplicableValue<T>()
		{
			IsApplicable = IsApplicable,
			Value = Value,
			m_oldValue = m_oldValue
		};

		public void TweenTo(T _value, float _amount = 1)
		{
			if (!IsApplicable)
				return;

			if (_amount >= 1)
			{
				Value = _value;
				return;
			}

			if (_amount <= 0)
			{
				m_oldValue = Value;
				return;
			}

			Value = Lerp(m_oldValue, _value, _amount);
		}

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
