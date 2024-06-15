using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	// We can not use a real interface here because Unity refuses to serialize
	public abstract class UiAbstractStyleBase : MonoBehaviour
	{
		public abstract List<ApplicableValueBase> Values { get; }

		public abstract Type SupportedMonoBehaviourType { get; }

		public abstract List<object> DefaultValues { get; }

		public bool Empty => Skin == null;

		[SerializeField][HideInInspector] private string m_name;
		
		private int m_key;

		public string Skin => Values[0].Skin;

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

		protected virtual void OnEnable()
		{
			UiEvents.EvSkinAdded.AddListener(OnSkinAdded);
			UiEvents.EvSkinRemoved.AddListener(OnSkinRemoved);
			UiEvents.EvSkinChanged.AddListener(OnSkinChanged);
		}

		protected virtual void OnDisable()
		{
			UiEvents.EvSkinAdded.RemoveListener(OnSkinAdded);
			UiEvents.EvSkinRemoved.RemoveListener(OnSkinRemoved);
			UiEvents.EvSkinChanged.RemoveListener(OnSkinChanged);
		}

		private void OnSkinChanged(string _name)
		{
			foreach (var value in Values)
				value.Skin = _name;
		}

		private void OnSkinRemoved(string _name)
		{
			foreach (var value in Values)
				value.RemoveSkin(_name);
		}

		private void OnSkinAdded(string _name)
		{
			var defaultValues = DefaultValues;
			for (int i = 0; i < Values.Count; i++)
			{
				var value = Values[i];
				value.AddSkin(_name, DefaultValues[i]);
			}
		}
	}
}