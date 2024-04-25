using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	public abstract class UiAbstractApplyStyle<MB,ST> : MonoBehaviour 
		where MB : MonoBehaviour 
		where ST : UiAbstractStyle<MB>
	{
		private MB m_monoBehaviour;

		public Type SupportedMonoBehaviour => typeof(MB);
		public Type SupportedStyle => typeof(ST);
		public MB MonoBehaviour => m_monoBehaviour;

		public abstract void Apply(MB monoBehaviourToApply, ST style);

		public virtual void Awake()
		{
			m_monoBehaviour = GetComponent<MB>();
			if (m_monoBehaviour == null)
			{
				Debug.LogWarning($"{GetType().Name}: Required MonoBehaviour type '{SupportedMonoBehaviour.Name}'" + 
				                 $" not found on GameObject '{gameObject.name}', styling won't work here");
			}
		}

		public virtual void OnEnable()
		{
			if (m_monoBehaviour == null)
				return;

			Apply();
		}

		private void Apply()
		{
			var skin = UiMainStyleConfig.Instance.CurrentSkin;
			var style = skin.StyleByMonoBehaviourClass(SupportedMonoBehaviour);
			Apply(MonoBehaviour, style as ST);

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