namespace GuiToolkit
{
	/// <summary>
	/// Optional configuration passed to <see cref="PlayerSettings.Add"/>. Kept as a small
	/// options object so further settings-dialog options can be added later without
	/// changing the <see cref="PlayerSettings.Add"/> signature again.
	/// </summary>
	public class PlayerSettingsOptions
	{
		/// <summary>
		/// Background music track id (a MusicTrack Id in the UiSoundConfig) to crossfade to
		/// while the player settings dialog is shown. Empty leaves the current music unchanged.
		/// </summary>
		public string BackgroundMusicId;
	}
}
