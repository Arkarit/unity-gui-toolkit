using System;

namespace GuiToolkit
{
	/// <summary>
	/// One-time overlay shown during the post-login startup sequence (changelogs, sale-event
	/// popups, news teasers, …). Driven by <see cref="UiStartupOverlayQueue"/>: each registered
	/// overlay is invoked in priority order, and the queue waits for the supplied callback
	/// before moving to the next entry.
	///
	/// An overlay must always invoke <c>_onClosed</c> — even when it decides not to show — so
	/// the queue can continue. The order of the remaining overlays is preserved regardless of
	/// which ones skip.
	/// </summary>
	public interface IUiStartupOverlay
	{
		/// <summary>
		/// Stable identifier used by the queue to track which overlays have already been
		/// processed this app session. MUST be unique across all overlays and stable across
		/// instance re-creation — when the player navigates away and returns, the new
		/// MonoBehaviour instance reports the same id and the queue skips it.
		///
		/// Implementations typically expose this as a serialized field per prefab, or as a
		/// constant for single-instance overlays.
		/// </summary>
		string OverlayId { get; }

		/// <summary>
		/// Sort key. Lower values run first. Ties run in registration order.
		/// </summary>
		int Priority { get; }

		/// <summary>
		/// Show the overlay (or skip it), then invoke <paramref name="_onClosed"/> when done.
		/// The callback MUST be invoked exactly once, even on the skip path, so the queue
		/// advances to the next entry.
		/// </summary>
		void Show( Action _onClosed );
	}
}
