using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Per-control override for the global <see cref="UiSound"/> detector. Place it on
	/// (or anywhere above) a button/toggle to change what the automatic UI sound does
	/// for that control: suppress it, redirect it to a different configured sound type,
	/// or play a custom clip. <see cref="UiSound"/> finds it via
	/// <c>GetComponentInParent</c>, so an override on a panel applies to every control
	/// beneath it.
	///
	/// Note: this refines controls the detector already recognizes (GuiToolkit buttons
	/// / toggles and plain UGUI Buttons / Toggles). It does not make an otherwise
	/// silent, non-standard control emit sound.
	/// </summary>
	public class UiSoundOverride : MonoBehaviour
	{
		public enum EMode
		{
			/// <summary>Play nothing — e.g. the control handles its own sound, or should be silent.</summary>
			Suppress,
			/// <summary>Play a different configured sound type instead of the detected one.</summary>
			Redirect,
			/// <summary>Play a custom clip defined here, bypassing the shared config.</summary>
			Custom,
		}

		[Tooltip("How this control's automatic UI sound is overridden.")]
		[SerializeField] private EMode m_mode = EMode.Suppress;

		[Tooltip("Sound type to play instead of the detected one (used when Mode is Redirect).")]
		[SerializeField] private EUiSoundType m_redirectType = EUiSoundType.Click;

		[Tooltip("Custom sound to play, bypassing the shared config (used when Mode is Custom).")]
		[SerializeField] private UiSoundConfig.SoundDef m_customSound = new UiSoundConfig.SoundDef();

		public EMode Mode => m_mode;
		public EUiSoundType RedirectType => m_redirectType;
		public UiSoundConfig.SoundDef CustomSound => m_customSound;
	}
}
