using UnityEditor;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// - Unity can't read .po files as text files.
	/// - It can however be read as a LocalizationAsset, but that is a completely useless asset in this context, since the source content can't be read.
	/// - You can create a custom file type importer, but it "rejects" being registered, since ".po" is already known.
	/// - Unity can read .po.txt files as text files.
	/// - BUT: Poedit refuses to read text files as .po files (https://github.com/vslavik/poedit/issues/839).
	/// - It is possible to create symlinks from .po.txt to .po or vice versa, but git can't handle symlinks very well, and regularly they break when saving .po files.
	///
	/// A lot of people had to work tightly together to generate the maximum annoying experience out of the super trivial act of loading a text file.
	/// 
	/// I call it the "Syndicate". It's the shadow army of people whose only purpose of existing is to annoy you.
	/// Its the agents who temporary park their cars in the street where you live, and after you had to drive to an expensive parking garage and walk home through your street already have released 5 parking spaces.
	/// It's a division of film directors who always show show people throwing up or other disgusting scenes exactly in the moment, when you eat your meal watching tv.
	/// But the strongest, most annoying and most effective department of them all is their IT division.
	///
	/// Well the wiser one gives in.
	///
	/// This tool synchronizes each .po file with a coexisting .po.txt.
	/// Regardless which file you change, the other one is always synchronized.
	/// This is done by the PoFixerAssetPostprocessor
	///
	/// And please, keep this solution confidential under all circumstances, so that the Syndicate doesn't find a way to also sabotage this approach.
	/// </summary>
	
	[InitializeOnLoad]
	public static class PoFixer
	{
		private const string LogPrefix = "PO Fixer: ";
		private static bool s_enabled = false;

		private static readonly string PrefsKey = StringConstants.PLAYER_PREFS_PREFIX + nameof(PoFixer) + ".active";
 
		static PoFixer()
		{
			IsEnabled = EditorPrefs.GetBool(PrefsKey, true);
		}

		[MenuItem(StringConstants.PO_FIXER_MENU_NAME, priority = Constants.MISC_MENU_PRIORITY)]
		private static void Toggle()
		{
			IsEnabled = !IsEnabled;
		}

		public static bool IsEnabled
		{
			get => s_enabled;
			set
			{
				if (value == s_enabled)
					return;

				s_enabled = value;
				Menu.SetChecked(StringConstants.PO_FIXER_MENU_NAME, s_enabled);
				EditorPrefs.SetBool(PrefsKey, s_enabled);
			}
		}
	}
}