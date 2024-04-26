using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	public abstract class UiAbstractApplyStyle<MB,ST> : UiAbstractApplyStyleBase 
		where MB : MonoBehaviour 
		where ST : UiAbstractStyle<MB>
	{
		private MB m_monoBehaviour;
		private int m_key = 0;

		public override Type SupportedMonoBehaviourType => typeof(MB);
		public override Type SupportedStyleType => typeof(ST);

		public override MonoBehaviour MonoBehaviour
		{
			get
			{
				if (m_monoBehaviour == null)
				{
					m_monoBehaviour = GetComponent<MB>();
					if (m_monoBehaviour == null)
					{
						Debug.LogWarning($"{GetType().Name}: Required MonoBehaviour type '{SupportedMonoBehaviourType.Name}'" + 
						                 $" not found on GameObject '{gameObject.name}', styling won't work here");
					}
				}

				return m_monoBehaviour;
			}
		}

		public override int Key
		{
			get
			{
				if (m_key == 0)
					m_key = UiStyleUtility.GetKey(SupportedMonoBehaviourType, Name);

				return m_key;
			}
		}

		public MB SpecificMonoBehaviour => MonoBehaviour as MB;

		public abstract void Apply(ST style);

		public virtual void OnEnable()
		{
			if (m_monoBehaviour == null)
				return;

			Apply();
		}

		private void Apply()
		{
			var skin = UiMainStyleConfig.Instance.CurrentSkin;
			var style = skin.StyleByKey(Key);
			Apply(style as ST);

			UiEvents.EvSkinChanged.AddListener(OnSkinChanged);
		}

		public virtual void OnDisable()
		{
			if (m_monoBehaviour == null)
				return;

			UiEvents.EvSkinChanged.RemoveListener(OnSkinChanged);
		}

		private void OnSkinChanged()
		{
			Apply();
		}
	}
}