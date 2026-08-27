using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public abstract class UiAbstractStyleBase
	{
		[SerializeField][HideInInspector] private UiStyleConfig m_styleConfig;
		
		// The m_name member should never change. Together with the supported Component type it forms the identifier of this style
		// and is only ever set in ctor.
		[SerializeField][HideInInspector] private string m_name;
		// m_alias can be changed and used for display purposes.
		[SerializeField][HideInInspector] private string m_alias;
		private ApplicableValueBase[] m_values;
		
		private int m_key;

		public UiStyleConfig StyleConfig
		{
			get => m_styleConfig;
			protected set => m_styleConfig = value;
		}

		public UiStyleConfig EffectiveStyleConfig => StyleConfig ? StyleConfig : UiStyleConfig.Instance;
		
		public string Name
		{
			get => m_name;
			protected set
			{
				m_name = value;
				m_key = 0; // the key is derived from the name, so it has to be recomputed
			}
		}

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

		public abstract Type SupportedComponentType { get; }
		protected abstract ApplicableValueBase[] GetValueList();
		
		public ApplicableValueBase[] Values
		{
			get
			{
				if (m_values == null)
				{
					m_values = GetValueList();
				}

				return m_values;
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// Throws the cached value list away and builds it again, which runs every generated value getter -
		/// and those create a value whose field is null.
		///
		/// That is not a theoretical state: a [SerializeReference] field ADDED to a style type stays null in
		/// every config already on disk, because Unity does not run field initialisers for data it loads.
		/// Merely reading <see cref="Values"/> is not enough to repair it, since the first read of the run
		/// may have happened before the caller cared. Editor only - at runtime an asset is what it is.
		/// </summary>
		public void RebuildValues()
		{
			m_values = null;
			_ = Values;
		}
#endif

		/// <summary>
		/// Hash of component type plus name, cached. It used to be recomputed on every access while not
		/// playing, which cost ~1 us a time and was paid once per style per key-lookup rebuild. The name
		/// is the style's identifier and only ever set in the constructor (the setter above invalidates
		/// the cache should that ever change), and m_key is not serialized, so a reload recomputes it.
		/// </summary>
		public int Key
		{
			get
			{
				if (m_key == 0)
					m_key = UiStyleUtility.GetKey(SupportedComponentType, Name);

				return m_key;
			}
		}

		public virtual void Init()
		{
			UiEventDefinitions.EvStyleApplicableChanged.RemoveListener(OnStyleApplicableChanged);
			UiEventDefinitions.EvStyleApplicableChanged.AddListener(OnStyleApplicableChanged);
		}

		private void OnStyleApplicableChanged(UiStyleConfig _styleConfig, UiAbstractStyleBase _from)
		{
			if (_styleConfig != StyleConfig)
				return;
			
			if (_from == this || _from == null)
				return;

			if (Key != _from.Key)
				return;

			Debug.Assert(GetType() == _from.GetType());

			UiStyleUtility.SynchronizeApplicableness(_from, this);
		}

	}
}
