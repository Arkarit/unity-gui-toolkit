namespace GuiToolkit
{
	/// \interface IEditorAware
	/// \brief Marker interface to explicitly allow editor-time access to toolkit singletons.
	///
	/// In Unity Editor, direct access to singleton `Instance` getters may be unsafe during 
	/// domain reload, reimport or other asset pipeline operations. By default such access is blocked 
	/// to avoid undefined states.
	///
	/// Classes implementing `IEditorAware` signal that they are aware of these restrictions 
	/// and take responsibility for using safe access patterns (e.g. via \ref AssetReadyGate.WhenReady).
	/// 
	/// The policy enforced by \ref EditorCallerGate requires that at least one caller on the 
	/// current call stack implements `IEditorAware` in order to access editor-only singletons.
	/// 
	/// Typical implementors are:
	/// - Custom EditorWindows
	/// - Custom Inspectors (Editor classes)
	/// - Editor tooling that runs outside of Play Mode
	///
	/// \note This is an **editor-only** concept. At runtime, `IEditorAware` has no effect.
	public interface IEditorAware
	{
	}
}