/// Credit SimonDarksideJ
/// Required for scrollbar support to work across ALL scroll snaps


namespace GuiToolkit
{
	internal interface IScrollSnap
	{
		void ChangePage( int page );
		void SetLerp( bool value );
		int CurrentPage();
		void StartScreenChange();
	}
}
