using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A UiThing that fires its EvOnLanguageChanged event a configurable number of frames after enable and after each
	/// language change, and can force a rebuild of child LayoutGroups so localized text re-lays out.
	/// </summary>
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
