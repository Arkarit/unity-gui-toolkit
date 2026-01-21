using System;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Base class for components that need editor-safe initialization.
	/// Ensures Awake/OnEnable work only after asset import/compile settled.
	/// </summary>
	public abstract class AbstractEditorAwareMonoBehaviour : MonoBehaviour, IEditorAware
	{
		/// <summary> Called when it is safe to perform Awake work. </summary>
		protected abstract void SafeAwake();

		/// <summary> Called when it is safe to perform OnEnable work. </summary>
		protected abstract void SafeOnEnable();

		// --- Unity lifecycle (do not override) ---

		// Intentionally not virtual: we want the gate to always run.
		private void Awake()
		{
			ScheduleSafeInvoke(SafeAwake);
		}

		private void OnEnable()
		{
			ScheduleSafeInvoke(SafeOnEnable);
		}

		private void ScheduleSafeInvoke( Action _safeAction )
		{
			if (_safeAction == null) 
				return;

			// Editor: guard with AssetReadyGate; Runtime: should be immediate
			AssetReadyGate.WhenReady
			(
				() =>
				{
					// Object could have been disabled/destroyed while waiting
					if (!this || !isActiveAndEnabled)
						return;

					try
					{
						_safeAction();
					}
					catch (Exception ex)
					{
						Debug.LogException(ex, this);
						UiLog.LogInternal($"Exception in ScheduleSafeInvoke):{ex}");
					}
				}
			);
		}
	}
}
