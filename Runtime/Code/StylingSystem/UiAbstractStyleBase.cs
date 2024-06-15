using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Style
{
	[ExecuteAlways]
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

		protected virtual void Awake()
		{
			ReAddListeners();
		}

		protected virtual void OnEnable()
		{
			ReAddListeners();
		}

		protected virtual void OnDisable()
		{
			RemoveListeners();
		}

		protected virtual void OnValidate()
		{
			ReAddListeners();
		}

		private void AddListeners()
		{
			UiEvents.EvSkinAdded.AddListener(OnSkinAdded);
			UiEvents.EvSkinRemoved.AddListener(OnSkinRemoved);
			UiEvents.EvSkinChanged.AddListener(OnSkinChanged);
		}

		private void RemoveListeners()
		{
			UiEvents.EvSkinAdded.RemoveListener(OnSkinAdded);
			UiEvents.EvSkinRemoved.RemoveListener(OnSkinRemoved);
			UiEvents.EvSkinChanged.RemoveListener(OnSkinChanged);
		}

		private void ReAddListeners()
		{
			RemoveListeners();
			AddListeners();
		}

		private void OnSkinChanged(string _name)
		{
			foreach (var value in Values)
				value.Skin = _name;
#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		private void OnSkinRemoved(string _name)
		{
			foreach (var value in Values)
				value.RemoveSkin(_name);
#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}

		private void OnSkinAdded(string _name)
		{
			var defaultValues = DefaultValues;
			for (int i = 0; i < Values.Count; i++)
			{
				var value = Values[i];
				value.AddSkin(_name, defaultValues[i]);
			}
#if UNITY_EDITOR
			EditorUtility.SetDirty(this);
#endif
		}
	}
}