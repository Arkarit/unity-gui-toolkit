using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Full-screen input blocker for <see cref="UiStartupOverlayQueue"/>. Put this on an
	/// always-active, screen-filling UI GameObject that has a raycast-target Graphic (e.g. a
	/// transparent Image), positioned in the hierarchy ABOVE the screen(s) you want to block but
	/// BELOW the startup overlays — so the overlays stay interactive while everything behind them
	/// is blocked. No canvas sorting is involved.
	///
	/// The component owns its state so the prefab needs no particular initial setup:
	/// <list type="bullet">
	///   <item>It registers itself with the queue in <see cref="Awake"/> and clears that
	///         registration in <see cref="OnDestroy"/> — no external assignment, and no stale
	///         static reference after a scene unload.</item>
	///   <item>It forces the resting state to "not blocking" on awake, so the serialized
	///         <see cref="CanvasGroup.blocksRaycasts"/> value is irrelevant.</item>
	/// </list>
	/// The only expectation is that the GameObject is active at scene start (the natural state for
	/// a scene UI element); if it isn't, the queue simply runs unblocked — it never dead-locks the
	/// UI. The queue drives <see cref="SetBlocking"/> for the duration of a run.
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public class UiStartupOverlayBlocker : MonoBehaviour
	{
		private CanvasGroup m_canvasGroup;

		protected void Awake()
		{
			m_canvasGroup = GetComponent<CanvasGroup>();
			SetBlocking(false);
			UiStartupOverlayQueue.Blocker = this;
		}

		protected void OnDestroy()
		{
			// Only clear if we're still the registered blocker, so a newer instance (e.g. after a
			// scene reload where the new object's Awake already ran) is never clobbered.
			if (UiStartupOverlayQueue.Blocker == this)
				UiStartupOverlayQueue.Blocker = null;
		}

		/// <summary>
		/// Toggle whether the blocker swallows pointer input to everything behind it. Called by
		/// <see cref="UiStartupOverlayQueue"/>; there is no need to call it directly.
		/// </summary>
		public void SetBlocking( bool _blocking )
		{
			if (m_canvasGroup != null)
				m_canvasGroup.blocksRaycasts = _blocking;
		}
	}
}
