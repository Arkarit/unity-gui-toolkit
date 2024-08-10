using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	[Serializable]
	public abstract class UiAbstractStyleBase
	{
		public enum EScreenOrientationCondition
		{
			Always = -1,
			Landscape,
			Portrait,
		}

		[SerializeField][HideInInspector] private string m_name;
		[SerializeField][HideInInspector] private EScreenOrientationCondition m_screenOrientationCondition = EScreenOrientationCondition.Always;
		
		private int m_key;

		public string Name
		{
			get => m_name;
			set => m_name = value;
		}

		public EScreenOrientationCondition ScreenOrientationCondition
		{
			get => m_screenOrientationCondition;
			set => m_screenOrientationCondition = value;
		}

		public abstract Type SupportedMonoBehaviourType { get; }
		
		public int Key
		{
			get
			{
				if (m_key == 0 || !Application.isPlaying)
					m_key = UiStyleUtility.GetKey(SupportedMonoBehaviourType, Name);

				return m_key;
			}
		}

		public virtual void Init()
		{
			UiEvents.EvStyleApplicableChanged.RemoveListener(OnStyleApplicableChanged);
			UiEvents.EvStyleApplicableChanged.AddListener(OnStyleApplicableChanged);
		}

		private void OnStyleApplicableChanged(UiAbstractStyleBase _from)
		{
			if (_from == this || _from == null)
				return;

			if (Key != _from.Key)
				return;

			Debug.Assert(GetType() == _from.GetType());

			UiStyleUtility.SynchronizeApplicableness(_from, this);
		}
	}
}