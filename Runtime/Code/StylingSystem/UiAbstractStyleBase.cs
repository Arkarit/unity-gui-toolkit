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

		public void AddSkin(string _skinName, object _defaultValue)
		{
			foreach (var value in Values)
				value.AddSkin(_skinName, _defaultValue);
		}

		public void RemoveSkin(string _skinName)
		{
			foreach (var value in Values)
				value.RemoveSkin(_skinName);
		}
	}
}