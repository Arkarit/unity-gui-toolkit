using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	public abstract class UiAbstractStyleBase : MonoBehaviour
	{
		[SerializeField] private List<ApplicableValueBase> m_Values = new();

		public abstract Type SupportedMonoBehaviourType { get; }

		public bool Empty => m_Values.Count == 0;

		[SerializeField][HideInInspector] private string m_name;
		
		private int m_key;

		public string Name
		{
			get => m_name;
			set => m_name = value;
		}

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

		public UiAbstractStyleBase Clone()
		{
			return MemberwiseClone() as UiAbstractStyleBase;
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