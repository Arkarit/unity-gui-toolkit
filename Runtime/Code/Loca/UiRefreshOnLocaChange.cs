using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Invalidates all child <see cref="LayoutGroup"/> components and force-rebuilds
	/// the layout hierarchy whenever the active language changes.
	/// Attach this to any UI container whose dimensions depend on translated text length.
	/// </summary>
	public class UiRefreshOnLocaChange : UiThing
	{
		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnLanguageChanged( string _languageId )
		{
			base.OnLanguageChanged(_languageId);
			StartCoroutine(RefreshNextFrame());
		}

		private IEnumerator RefreshNextFrame()
		{
			yield return null;

			var layoutGroups = GetComponentsInChildren<LayoutGroup>(true);
			foreach (var layoutGroup in layoutGroups)
				LayoutRebuilder.MarkLayoutForRebuild((RectTransform) layoutGroup.transform);

			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) transform);
		}
	}
}
