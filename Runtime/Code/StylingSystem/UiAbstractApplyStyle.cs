using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public abstract class UiAbstractApplyStyle<CO,ST> : UiAbstractApplyStyleBase 
		where CO : Component 
		where ST : UiAbstractStyle<CO>
	{
		private CO m_component;
		private int m_key = 0;

		public override Type SupportedComponentType => typeof(CO);
		public override Type SupportedStyleType => typeof(ST);

		public override Component Component
		{
			get
			{
				if (m_component == null)
				{
					m_component = GetComponent<CO>();
					if (m_component == null)
					{
						UiLog.LogWarning($"{GetType().Name}: Required Component type '{SupportedComponentType.Name}'" + 
						                 $" not found on GameObject '{gameObject.name}', styling won't work here");
					}
				}

				return m_component;
			}
		}

		public override int Key
		{
			get
			{
				if (m_key == 0 || !Application.isPlaying)
					m_key = UiStyleUtility.GetKey(SupportedComponentType, Name);

				return m_key;
			}
		}

		public CO SpecificComponent => Component as CO;
		public ST SpecificStyle => Style as ST;
	}
}
