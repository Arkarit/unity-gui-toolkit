namespace GuiToolkit.Style
{
	// Deprecated - do not use.
	// Only exists for backward compatibility.
	public class UiMainStyleConfig : UiStyleConfig
	{
		protected override void OnEnable()
		{
			base.OnEnable();
			UiLog.LogWarning($"{nameof(UiMainStyleConfig)} is deprecated. Please use {nameof(UiStyleConfig)} instead.");
		}
	}
}
