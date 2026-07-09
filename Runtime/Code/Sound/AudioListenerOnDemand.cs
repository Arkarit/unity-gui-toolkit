using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	/// <summary>
	/// A fallback <see cref="AudioListener"/> that switches itself on only when no other
	/// active listener exists. Solves the two classic annoyances: Unity's "more than one
	/// audio listener" warning, and silence when a view (e.g. a full-screen 3D view)
	/// disables the main camera — and with it the AudioListener that Unity puts on the
	/// camera by default.
	///
	/// It re-evaluates on two cheap, event-driven signals and never polls per frame:
	/// <list type="bullet">
	/// <item><see cref="UiEventDefinitions.EvMainCameraChanged"/> — raised by
	/// <see cref="CoRoutineRunner"/> whenever <c>Camera.main</c> changes. Because that is
	/// driven by <c>Camera.main</c> (not a reference swap), it also fires when the main
	/// camera is merely disabled, which covers the full-screen-view case. The event is
	/// auto-invoking, so subscribing performs the initial evaluation for us.</item>
	/// <item><see cref="SceneManager.sceneLoaded"/> — a backstop for listener changes that
	/// are not caused by the main camera.</item>
	/// </list>
	///
	/// Placed manually on <see cref="UiMain"/> so client projects can remove or replace it
	/// with their own listener strategy. Requires its own AudioListener, which it toggles.
	/// </summary>
	[RequireComponent(typeof(AudioListener))]
	public class AudioListenerOnDemand : MonoBehaviour
	{
		private AudioListener m_listener;

		private void OnEnable()
		{
			m_listener = GetComponent<AudioListener>();

			// EvMainCameraChanged is auto-invoking (sticky): AddListener immediately calls
			// the handler with the current camera, giving us the initial evaluation.
			UiEventDefinitions.EvMainCameraChanged.AddListener(OnMainCameraChanged);
			SceneManager.sceneLoaded += OnSceneLoaded;

			UpdateState();
		}

		private void OnDisable()
		{
			UiEventDefinitions.EvMainCameraChanged.RemoveListener(OnMainCameraChanged);
			SceneManager.sceneLoaded -= OnSceneLoaded;
		}

		private void OnMainCameraChanged( Camera _old, Camera _new ) => UpdateState();
		private void OnSceneLoaded( Scene _scene, LoadSceneMode _mode ) => UpdateState();

		// Enable our listener only when no other active one exists. Because the camera
		// event fires from CoRoutineRunner's poll after Camera.main already reflects the
		// change, the scan always sees the settled state — no deferral needed.
		private void UpdateState()
		{
			if (m_listener == null)
				return;

			m_listener.enabled = !HasOtherActiveListener();
		}

		private bool HasOtherActiveListener()
		{
			// Excludes listeners on inactive GameObjects (they produce neither sound nor a
			// warning); isActiveAndEnabled additionally filters out disabled components.
			var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
			foreach (var listener in listeners)
			{
				if (listener == m_listener)
					continue;
				if (listener.isActiveAndEnabled)
					return true;
			}
			return false;
		}
	}
}
