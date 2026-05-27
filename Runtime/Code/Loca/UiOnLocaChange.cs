using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiOnLocaChange : UiThing
	{
		[SerializeField] private bool m_RefreshLayoutGroups = true;
		[SerializeField] private int m_DelayFrames = 1;
		public CEvent<string> EvOnLanguageChanged = new();
		
		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnEnable()
		{
			base.OnEnable();
			ExecuteFrameDelayed(DelayedOnLanguageChanged, m_DelayFrames);
		}

		protected override void OnLanguageChanged( string _languageId )
		{
			base.OnLanguageChanged(_languageId);
			ExecuteFrameDelayed(DelayedOnLanguageChanged, m_DelayFrames);
		}

		protected virtual void DelayedOnLanguageChanged()
		{
			RefreshLayoutGroups();
			EvOnLanguageChanged.Invoke(LocaManager.Instance.Language);
		}

		private void RefreshLayoutGroups()
		{
			if (m_RefreshLayoutGroups)
			{
				var layoutGroups = GetComponentsInChildren<LayoutGroup>(true);
				foreach (var layoutGroup in layoutGroups)
					LayoutRebuilder.MarkLayoutForRebuild((RectTransform) layoutGroup.transform);
	
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) transform);
			}
		}
	}
}
