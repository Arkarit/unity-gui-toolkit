using System;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Base class for ScriptableObjects that need to be editor-safe:
	/// initialization is deferred until the editor is no longer importing/compiling
	/// and all required assets are ready.
	/// </summary>
	public abstract class AbstractEditorAwareScriptableObject : ScriptableObject, IEditorAware
	{
		/// <summary>
		/// Safe initialization logic. Called only after the gate conditions are met.
		/// </summary>
		protected virtual void SafeOnEnable() { }

		/// <summary>
		/// Optional extra condition to check in the editor; ignored in play mode.
		/// </summary>
		protected virtual bool Condition() => true;

		protected virtual void OnEnable()
		{
			AssetReadyGate.WhenReady
			(
				// callback
				() =>
				{
					// Might have been destroyed/unloaded while waiting
					if (!this)
						return;

					try
					{
						SafeOnEnable();
					}
					catch (Exception ex)
					{
						Debug.LogException(ex, this);
					}
				},
				// condition to evaluate only in the editor
				Condition
			);
		}

#if UNITY_EDITOR
		/// <summary>
		/// Safe validation logic. Override instead of OnValidate to ensure
		/// it only runs when asset import/domain reload is finished.
		/// </summary>
		protected virtual void SafeOnValidate() { }

		private void OnValidate()
		{
			AssetReadyGate.WhenReady
			(
				() =>
				{
					if (!this) 
						return;
					
					try
					{
						SafeOnValidate();
					}
					catch (Exception ex)
					{
						Debug.LogException(ex, this);
					}
				},
				Condition
			);
		}
#endif
	}
}
