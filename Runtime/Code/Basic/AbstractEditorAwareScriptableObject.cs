using System;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
		/// Absolute Unity asset paths (e.g. "Assets/Resources/...") that must be ready
		/// before initialization. May be empty but never null.
		/// </summary>
		public virtual string[] RequiredScriptableObjects
		{
			get
			{
#if UNITY_EDITOR
				if (Application.isPlaying)
					return Array.Empty<string>();
				
				var result = new List<string> ()
				{
					UiToolkitConfiguration.AssetPath, 
					UiMainStyleConfig.AssetPath, 
					UiOrientationDependentStyleConfig.AssetPath
				};
				
				var path = AssetDatabase.GetAssetPath(this);
				if (!string.IsNullOrEmpty(path))
					result.Add(path);
				
				return result.ToArray();
#else
				return Array.Empty<string>();
#endif
			}
		}

		/// <summary>
		/// Safe initialization logic. Called only after the gate conditions are met.
		/// </summary>
		protected abstract void SafeOnEnable();

		/// <summary>
		/// Optional extra condition to check in the editor; ignored in play mode.
		/// </summary>
		protected virtual bool Condition() => true;

		// Do not override: always goes through AssetReadyGate
		private void OnEnable()
		{
			var assets = RequiredScriptableObjects ?? Array.Empty<string>();

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
				Condition,
				assets
			);
		}

#if UNITY_EDITOR
		/// <summary>
		/// Convenience helper for saving this asset safely in the editor.
		/// Calls SetDirty + SaveAssetIfDirty, but only if the asset import is idle.
		/// </summary>
		protected void Save()
		{
			var path = UnityEditor.AssetDatabase.GetAssetPath(this);
			AssetReadyGate.ThrowIfNotReady(path);
			UnityEditor.EditorUtility.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
		}

		/// <summary>
		/// Safe validation logic. Override instead of OnValidate to ensure
		/// it only runs when asset import/domain reload is finished.
		/// </summary>
		protected virtual void SafeOnValidate() { }

		private void OnValidate()
		{
			var assets = RequiredScriptableObjects ?? Array.Empty<string>();
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
				Condition,
				assets
			);
		}
#endif
	}
}
