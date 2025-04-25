using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public abstract class UiAbstractStyleBase
	{
#if UNITY_EDITOR
		public struct ValueInfo
		{
			public string GetterName;
			public Type GetterType;
			public ApplicableValueBase Value;
		}
#endif

		public enum EScreenOrientationCondition
		{
			Always = -1,
			Landscape,
			Portrait,
		}

		[SerializeField][HideInInspector] private UiStyleConfig m_styleConfig;
		
		// The m_name member should never change. Together with the supported Component type it forms the identifier of this style
		// and is only ever set in ctor.
		[SerializeField][HideInInspector] private string m_name;
		// m_alias can be changed and used for display purposes.
		[SerializeField][HideInInspector] private string m_alias;
		[SerializeField][HideInInspector] private EScreenOrientationCondition m_screenOrientationCondition = EScreenOrientationCondition.Always;
		private ApplicableValueBase[] m_values;
		
		private int m_key;

		public UiStyleConfig StyleConfig
		{
			get => m_styleConfig;
			protected set => m_styleConfig = value;
		}

		public UiStyleConfig EffectiveStyleConfig => StyleConfig ? StyleConfig : UiMainStyleConfig.Instance;
		
		public string Name
		{
			get => m_name;
			protected set => m_name = value;
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

		public EScreenOrientationCondition ScreenOrientationCondition
		{
			get => m_screenOrientationCondition;
			set => m_screenOrientationCondition = value;
		}

		public abstract Type SupportedComponentType { get; }
		protected abstract ApplicableValueBase[] GetValueArray();

#if UNITY_EDITOR
		public abstract List<ValueInfo> GetValueInfos();
#endif       
		public ApplicableValueBase[] Values
		{
			get
			{
				if (m_values == null)
				{
					m_values = GetValueArray();
				}

				return m_values;
			}
		}

		public int Key
		{
			get
			{
				if (m_key == 0 || !Application.isPlaying)
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
