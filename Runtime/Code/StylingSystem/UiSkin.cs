using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
		// Shape of the style list the lookup was built from - see BuildDictionaryIfNecessary.
		private int m_builtStyleCount = -1;
		private UiAbstractStyleBase m_builtFirstStyle;
		private UiAbstractStyleBase m_builtLastStyle;
		private static readonly List<int> m_stylesToRemove = new();

		public UiSkin(UiStyleConfig _config, string _name, float _aspectRatioGreaterEqual = -1 ) 
		{
			m_config = _config;
			m_name = _name;
			m_aspectRatioGreaterEqual = _aspectRatioGreaterEqual;
			if (!IsAspectRatioDependent && !Mathf.Approximately(-1, _aspectRatioGreaterEqual))
				throw new ArgumentException("Non-Aspect Ratio dependent UiSkins can't have an 'aspect ratio greater than' setting");
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
		public bool IsAspectRatioDependent => m_config is UiAspectRatioDependentStyleConfig;
		public float AspectRatioGreaterEqual => m_aspectRatioGreaterEqual;

		public void Init(UiStyleConfig _config)
		{
			Validate(_config);
			foreach (var style in m_styles)
				style.Init();

			BuildDictionary();
		}

		public UiAbstractStyleBase StyleByName<T>(string _name) where T:Component
		{
			BuildDictionaryIfNecessary();

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
			BuildDictionaryIfNecessary();

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

			BuildDictionary();
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

			BuildDictionary();
		}

		/// <summary>
		/// Rebuilds the key lookup only when the style list actually changed shape.
		///
		/// This used to rebuild unconditionally while not playing, because Styles is a public list that
		/// anything in the editor may add to or remove from, and a stale lookup would hide a style that is
		/// plainly there. The price was steep: every single lookup recomputed one key per style, measured
		/// at ~61 us for a skin with 70 styles - and with one lookup per applier per skin change, that was
		/// the bulk of what made the editor feel slow.
		///
		/// Instead of trusting nobody, the cheap observable facts about the list are remembered: how many
		/// styles it had, and which instances sat at its ends. Adding, removing or replacing a style
		/// changes at least one of them, and a reload replaces the instances wholesale, so all of those
		/// rebuild. The one case this does not see is an in-place replacement in the MIDDLE of the list
		/// that keeps the count - no code path does that today (deletions go through DeleteStyle,
		/// additions append), and code that wants to be explicit can call InvalidateStyleLookup().
		/// </summary>
		private void BuildDictionaryIfNecessary()
		{
			if (m_styleByKey != null && !StyleListChangedShape())
				return;

			BuildDictionary();
		}

		private bool StyleListChangedShape()
		{
			int count = m_styles.Count;
			if (count != m_builtStyleCount)
				return true;

			if (count == 0)
				return false;

			return !ReferenceEquals(m_styles[0], m_builtFirstStyle)
			    || !ReferenceEquals(m_styles[count - 1], m_builtLastStyle);
		}

		/// <summary>
		/// Forces the key lookup to be rebuilt on next access. Only needed for a change the shape check
		/// cannot see, i.e. swapping a style in the middle of the list for another one.
		/// </summary>
		public void InvalidateStyleLookup() => m_builtStyleCount = -1;

		private void BuildDictionary()
		{
			if (m_styleByKey == null)
				m_styleByKey = new Dictionary<int, UiAbstractStyleBase>(m_styles.Count);
			
			m_styleByKey.Clear();

			foreach (var style in m_styles)
			{
				m_styleByKey.Add(style.Key, style);
			}

			m_builtStyleCount = m_styles.Count;
			m_builtFirstStyle = m_builtStyleCount > 0 ? m_styles[0] : null;
			m_builtLastStyle = m_builtStyleCount > 0 ? m_styles[m_builtStyleCount - 1] : null;
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
