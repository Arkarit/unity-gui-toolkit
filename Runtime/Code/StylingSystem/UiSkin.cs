using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiSkin
	{
		// Config this skin belongs to
		[SerializeField] private UiStyleConfig m_config;
		// The m_name member should never change. It's the identifier of the skin and is only ever set in ctor.
		[SerializeField] private string m_name;
		// m_alias can be changed and used for display purposes.
		[SerializeField] private string m_alias;
		[NonReorderable][SerializeReference] private List<UiAbstractStyleBase> m_styles = new();

		private Dictionary<int, UiAbstractStyleBase> m_styleByKey;

		public UiSkin(UiStyleConfig _config, string _name) 
		{
			m_config = _config;
			m_name = _name;
		}
		
		public string Name => m_name;
		public string Alias
		{
			get
			{
				if (string.IsNullOrEmpty(m_alias))
					return m_name;
				
				return m_alias;
			}
			
			set => m_alias = value;
		}

		public List<UiAbstractStyleBase> Styles => m_styles;
		public UiStyleConfig StyleConfig => m_config;

		public void Init()
		{
			foreach (var style in m_styles)
				style.Init();

			BuildDictionaries();
		}

		public UiAbstractStyleBase StyleByName<T>(string _name) where T:Component
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				BuildDictionaries();
#endif

			var key = UiStyleUtility.GetKey(typeof(T), _name);
			return StyleByKey(key);
		}

		public CT StyleByName<T,CT>(string _name) 
			where CT:UiAbstractStyleBase 
			where T:Component
		{
			return (CT) StyleByName<T>(_name);
		}

		public UiAbstractStyleBase StyleByKey(int _key)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				BuildDictionaries();
#endif

			if (m_styleByKey.TryGetValue(_key, out UiAbstractStyleBase result))
			{
				return result;
			}

			return null;
		}

		public void DeleteStyle(UiAbstractStyleBase _style)
		{
			for (int i = 0; i < m_styles.Count; i++)
			{
				if (m_styles[i].Key == _style.Key)
				{
					m_styles.RemoveAt(i);
					break;
				}
			}

			BuildDictionaries();
		}

		public void SetStyleAlias(UiAbstractStyleBase _style, string _newDisplayName)
		{
			for (int i = 0; i < m_styles.Count; i++)
			{
				if (m_styles[i].Key == _style.Key)
				{
					m_styles[i].Alias = _newDisplayName;
					break;
				}
			}

			BuildDictionaries();
		}

		private void BuildDictionaries()
		{
			if (m_styleByKey == null)
				m_styleByKey = new Dictionary<int, UiAbstractStyleBase>(m_styles.Count);
			
			m_styleByKey.Clear();
			foreach (var style in m_styles)
			{
				m_styleByKey.Add(style.Key, style);
			}
		}
	}
}
