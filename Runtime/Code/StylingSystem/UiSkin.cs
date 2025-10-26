using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
		[FormerlySerializedAs("m_aspectRatioGE")] [SerializeField] private float m_aspectRatioGreaterEqual = 0;


		private Dictionary<int, UiAbstractStyleBase> m_styleByKey;
		private static readonly List<int> m_stylesToRemove = new();

		public UiSkin(UiStyleConfig _config, string _name, float _aspectRatioGreaterEqual = -1 ) 
		{
			m_config = _config;
			m_name = _name;
			m_aspectRatioGreaterEqual = _aspectRatioGreaterEqual;
			if (!IsOrientationDependent && !Mathf.Approximately(-1, _aspectRatioGreaterEqual))
				throw new ArgumentException("Non-Orientation dependent UiSkins can't have an 'aspect ratio greater than' setting");
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
		public bool IsOrientationDependent => m_config is UiOrientationDependentStyleConfig;
		public float AspectRatioGreaterEqual => m_aspectRatioGreaterEqual;

		public void Init(UiStyleConfig _config)
		{
			Validate(_config);
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

		public void Validate(UiStyleConfig _config)
		{
			bool doSetDirty = false;

			if (m_config != _config)
			{
				m_config = _config;
				doSetDirty = true;
			}

			for (int i=0; i < m_styles.Count; i++)
			{
				var style = m_styles[i];
				if (style == null)
					m_stylesToRemove.Add(i);
			}

			if (m_stylesToRemove.Count > 0)
			{
				string styleIndicesToRemoveStr = string.Empty;
				for (int i = 0; i < m_stylesToRemove.Count; i++)
				{
					var styleIdx = m_stylesToRemove[i];
					styleIndicesToRemoveStr += styleIdx.ToString();
					var isLast = i == m_stylesToRemove.Count - 1;
					styleIndicesToRemoveStr += isLast ? " " : ", ";
				}

				UiLog.LogError($"Styling system: The styles {styleIndicesToRemoveStr} are null and will be removed. This is most likely caused by one or more missing Style/StyleApplier classes pair(s)." + 
				               " Sorry, the exact types of these classes pairs can not be determined here - well, because the styles are null. Please be sure to revert your git changes, if you accidentally deleted it.");

				for (int i = m_stylesToRemove.Count - 1; i >= 0; i--)
				{
					var styleIdx = m_stylesToRemove[i];
					m_styles.RemoveAt(styleIdx);
				}

				m_stylesToRemove.Clear();
				doSetDirty = true;
			}

#if UNITY_EDITOR
			if (!doSetDirty)
				return;

			EditorGeneralUtility.SetDirty(m_config);
			AssetDatabase.SaveAssets();
#endif

		}
	}
}
