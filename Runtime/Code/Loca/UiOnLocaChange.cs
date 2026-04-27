using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiOnLocaChange : UiThing
	{
		[SerializeField] private bool m_RefreshLayoutGroups;
		public CEvent<string> EvOnLanguageChanged = new();
		
		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnLanguageChanged( string _languageId )
		{
			base.OnLanguageChanged(_languageId);
			ExecuteFrameDelayed(DelayedOnLanguageChanged);
		}

		protected virtual void DelayedOnLanguageChanged()
		{
			if (m_RefreshLayoutGroups)
			{
				var layoutGroups = GetComponentsInChildren<LayoutGroup>(true);
				foreach (var layoutGroup in layoutGroups)
					LayoutRebuilder.MarkLayoutForRebuild((RectTransform) layoutGroup.transform);
	
				LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) transform);
			}
			
			EvOnLanguageChanged.Invoke(LocaManager.Instance.Language);
		}
	}
}
