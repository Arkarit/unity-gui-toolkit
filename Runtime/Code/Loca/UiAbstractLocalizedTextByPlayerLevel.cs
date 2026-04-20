using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Abstract base component that sets the text of a <see cref="TMP_Text"/> component on the same
	/// GameObject based on the current player level, with automatic re-translation on language change.
	///
	/// Provide one localization key per player level in the Inspector list (index 0 = MinPlayerLevel).
	/// Derived classes implement <see cref="CurrentPlayerLevel"/>, <see cref="MinPlayerLevel"/>,
	/// and <see cref="MaxPlayerLevel"/>, and wire level-change events to call
	/// <see cref="OnPlayerLevelChanged"/> (e.g. via EventsManager.UpdateLevel).
	/// </summary>
	[RequireComponent(typeof(TMP_Text))]
	public abstract class UiAbstractLocalizedTextByPlayerLevel : UiThing, ILocaKeyProvider
	{
		[SerializeField] private List<string> m_keys = new();

		private TMP_Text m_tmpText;

		/// <summary>Returns the current player level.</summary>
		protected abstract int CurrentPlayerLevel { get; }

		/// <summary>Returns the minimum player level (inclusive).</summary>
		protected abstract int MinPlayerLevel { get; }

		/// <summary>Returns the maximum player level (inclusive).</summary>
		protected abstract int MaxPlayerLevel { get; }

		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnEnable()
		{
			base.OnEnable();
			ApplyTranslation();
		}

		protected override void OnLanguageChanged( string _languageId )
		{
			ApplyTranslation();
		}

		/// <summary>
		/// Call this from derived classes when the player level changes.
		/// </summary>
		protected void OnPlayerLevelChanged( int _level )
		{
			ApplyTranslation();
		}

		private void ApplyTranslation()
		{
			if (!Application.isPlaying)
				return;

			var manager = LocaManager.Instance;
			if (manager == null)
				return;

			if (m_tmpText == null)
				m_tmpText = GetComponent<TMP_Text>();

			if (m_tmpText == null)
				return;

			string key = GetKeyForLevel(CurrentPlayerLevel);
			if (string.IsNullOrEmpty(key))
				return;

			m_tmpText.text = manager.Translate(key);
		}

		private string GetKeyForLevel( int _level )
		{
			if (m_keys == null || m_keys.Count == 0)
				return null;

			int index = Mathf.Clamp(_level - MinPlayerLevel, 0, m_keys.Count - 1);
			return m_keys[index];
		}

#if UNITY_EDITOR
		bool ILocaKeyProvider.UsesLocaKey => m_keys != null && m_keys.Any(k => !string.IsNullOrEmpty(k));
		bool ILocaKeyProvider.UsesMultipleLocaKeys => true;
		string ILocaKeyProvider.LocaKey => null;
		List<string> ILocaKeyProvider.LocaKeys => m_keys?
			.Where(k => !string.IsNullOrEmpty(k))
			.Distinct()
			.ToList();
		string ILocaKeyProvider.Group => null;
#endif
	}
}
