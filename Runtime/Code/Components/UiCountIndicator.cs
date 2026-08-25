using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GuiToolkit.Style;

namespace GuiToolkit
{
	/// <summary>
	/// A "x / y" counter that also says whether the number is acceptable - too few, right, too many - with
	/// an optional second counter for a side condition.
	///
	/// Display only: it holds no rule. Whether five athletes are enough is a question about a division, not
	/// about a label, so the caller sets the numbers and may set the verdict; deriving it from the two
	/// numbers is only the default. The three verdicts wear styles ("CountIndicator/Below", "/Ok",
	/// "/Above"), so the colours belong to the skin and this component has no state machine of its own.
	/// </summary>
	public class UiCountIndicator : UiTextContainer, ILocaKeyProvider
	{
		public enum EState
		{
			/// <summary>Fewer than required.</summary>
			Below,
			/// <summary>Exactly as required.</summary>
			Ok,
			/// <summary>More than allowed.</summary>
			Above,
		}

		[Header("Count Indicator")]

		[Tooltip("The counter line. Mandatory - without it there is nothing to show.")]
		[SerializeField][Mandatory] protected TMP_Text m_valueText;

		[Tooltip("Localization key of the format string, with {0} for the current value and {1} for the "
			+ "maximum. A key rather than a literal so a language may swap the order of the two.")]
		[SerializeField] protected string m_formatLocaKey = "{0} / {1}";

		[Tooltip("Optional localization group for the keys of this component.")]
		[SerializeField][Optional] protected string m_locaGroup;

		[SerializeField] protected int m_current;
		[SerializeField] protected int m_max = 1;

		[Header("Side condition (optional)")]

		[Tooltip("Show the side condition at all. A prefab ships with the row wired but switched off, so "
			+ "the plain case needs no setup and the second counter is one flag away.")]
		[SerializeField] protected bool m_showSecondary;

		[Tooltip("The object holding the side condition's row. Switched on and off with the flag above, so "
			+ "a layout group does not keep a gap for a line nobody asked for.")]
		[SerializeField][Optional] protected GameObject m_secondaryRow;

		[Tooltip("Counter line of the side condition. Leave empty for a single counter.")]
		[SerializeField][Optional] protected TMP_Text m_secondaryValueText;

		[Tooltip("Caption of the side condition, e.g. male athletes.")]
		[SerializeField][Optional] protected TMP_Text m_secondaryCaptionText;

		[Tooltip("Localization key of the side condition caption.")]
		[SerializeField][Optional] protected string m_secondaryCaptionLocaKey;

		[SerializeField] protected int m_secondaryCurrent;
		[SerializeField] protected int m_secondaryMax = 1;

		[Header("Verdict")]

		[Tooltip("Take the verdict from the field below instead of deriving it from the two numbers. What "
			+ "counts as enough is a rule, and rules live outside this component.")]
		[SerializeField] protected bool m_overrideState;

		[Tooltip("The verdict to show while the override above is on.")]
		[SerializeField] protected EState m_state;

		[Tooltip("Style appliers that carry the look of the verdict - background, text, icon. Each one is "
			+ "retargeted to prefix/Below, prefix/Ok or prefix/Above when the verdict changes, so the "
			+ "colours are the skin's business and not this component's.")]
		[SerializeField][Optional] protected List<UiAbstractApplyStyleBase> m_stateStyleAppliers = new();

		[Tooltip("Style path in front of the verdict name.")]
		[SerializeField] protected string m_stylePrefix = "CountIndicator";

		[Tooltip("Optional icon that changes with the verdict. Without a sprite for the current verdict its "
			+ "object is switched off.")]
		[SerializeField][Optional] protected Image m_stateIcon;

		[SerializeField][Optional] protected Sprite m_iconBelow;
		[SerializeField][Optional] protected Sprite m_iconOk;
		[SerializeField][Optional] protected Sprite m_iconAbove;

		[Header("Change highlight")]

		[Tooltip("Play the highlight below when a value changes.")]
		[SerializeField] protected bool m_highlightOnChange = true;

		[Tooltip("Optional short animation played on a value change.")]
		[SerializeField][Optional] protected UiSimpleAnimationBase m_changeHighlight;

		[Tooltip("Invoked after the verdict changed, with the new verdict.")]
		public CEvent<EState> OnStateChanged = new();

		private EState m_shownState;
		private bool m_shownStateValid;

		protected override bool NeedsLanguageChangeCallback => true;

		public int Current
		{
			get => m_current;
			set => SetValues(value, m_max);
		}

		public int Max
		{
			get => m_max;
			set => SetValues(m_current, value);
		}

		public int SecondaryCurrent
		{
			get => m_secondaryCurrent;
			set => SetSecondaryValues(value, m_secondaryMax);
		}

		public int SecondaryMax
		{
			get => m_secondaryMax;
			set => SetSecondaryValues(m_secondaryCurrent, value);
		}

		/// <summary>
		/// Whether the side condition is shown. Both halves have to be true: somebody has to want it, and
		/// the prefab has to have a line to put it in.
		/// </summary>
		public bool ShowSecondary
		{
			get => m_showSecondary && m_secondaryValueText != null;
			set
			{
				m_showSecondary = value;
				Refresh();
			}
		}

		public bool HasSecondary => ShowSecondary;

		/// <summary>
		/// The verdict currently shown. Reading it gives the override where one is set, the derivation
		/// otherwise; setting it turns the override on, so a caller that knows the rule always wins.
		/// </summary>
		public EState State
		{
			get => m_overrideState ? m_state : Derive(m_current, m_max);
			set
			{
				m_overrideState = true;
				m_state = value;
				Refresh();
			}
		}

		/// <summary>Hands the verdict back to the derivation from the two numbers.</summary>
		public void ClearStateOverride()
		{
			if (!m_overrideState)
				return;

			m_overrideState = false;
			Refresh();
		}

		public bool HighlightOnChange
		{
			get => m_highlightOnChange;
			set => m_highlightOnChange = value;
		}

		/// <summary>Sets both numbers in one step, so the line is rebuilt once instead of twice.</summary>
		public void SetValues( int _current, int _max )
		{
			bool changed = _current != m_current || _max != m_max;
			m_current = _current;
			m_max = _max;
			Refresh();

			if (changed)
			{
				PlayHighlight();
			}
		}

		public void SetSecondaryValues( int _current, int _max )
		{
			bool changed = _current != m_secondaryCurrent || _max != m_secondaryMax;
			m_secondaryCurrent = _current;
			m_secondaryMax = _max;
			Refresh();

			if (changed)
			{
				PlayHighlight();
			}
		}

		/// <summary>
		/// Rebuilds the lines, the verdict styles and the icon. Public because a preview cheat or a test
		/// bench sets the fields directly and then wants to see the result.
		/// </summary>
		public void Refresh()
		{
			InitIfNecessary();

			string format = string.IsNullOrEmpty(m_formatLocaKey)
				? "{0} / {1}"
				: _(m_formatLocaKey, m_locaGroup);

			if (m_valueText != null)
			{
				m_valueText.text = Format(format, m_current, m_max);
			}

			bool showSecondary = ShowSecondary;

			if (m_secondaryRow != null)
			{
				m_secondaryRow.SetActive(showSecondary);
			}

			if (showSecondary)
			{
				m_secondaryValueText.text = Format(format, m_secondaryCurrent, m_secondaryMax);

				if (m_secondaryCaptionText != null && !string.IsNullOrEmpty(m_secondaryCaptionLocaKey))
				{
					m_secondaryCaptionText.text = _(m_secondaryCaptionLocaKey, m_locaGroup);
				}
			}

			var state = State;
			ApplyState(state);

			if (!m_shownStateValid || m_shownState != state)
			{
				m_shownState = state;
				m_shownStateValid = true;
				OnStateChanged.Invoke(state);
			}
		}

		/// <summary>
		/// The default reading of the two numbers: short of the mark, on it, or past it. Static and
		/// side-effect free on purpose - it is the one piece of this component worth testing.
		/// </summary>
		public static EState Derive( int _current, int _max )
		{
			if (_current < _max)
			{
				return EState.Below;
			}

			return _current == _max ? EState.Ok : EState.Above;
		}

		protected override void Awake()
		{
			base.Awake();
			Refresh();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Refresh();
		}

		protected override void OnLanguageChanged( string _languageId ) => Refresh();

		private static string Format( string _format, int _current, int _max )
		{
			// A format string comes out of a translation file, so a broken one is a content error and must
			// not take the screen down with it.
			try
			{
				return string.Format(_format, _current, _max);
			}
			catch (System.FormatException)
			{
				UiLog.LogError($"Count indicator format string '{_format}' is not usable. Expected " +
				               "{0} for the current value and {1} for the maximum.");
				return $"{_current} / {_max}";
			}
		}

		private void ApplyState( EState _state )
		{
			string styleName = $"{m_stylePrefix}/{_state}";

			foreach (var applier in m_stateStyleAppliers)
			{
				if (applier == null)
				{
					continue;
				}

				applier.Name = styleName;
				applier.Apply();
			}

			if (m_stateIcon == null)
			{
				return;
			}

			var sprite = _state switch
			{
				EState.Below => m_iconBelow,
				EState.Ok => m_iconOk,
				_ => m_iconAbove,
			};

			m_stateIcon.sprite = sprite;
			m_stateIcon.gameObject.SetActive(sprite != null);
		}

		private void PlayHighlight()
		{
			if (!m_highlightOnChange || m_changeHighlight == null)
			{
				return;
			}

			if (!Application.isPlaying)
			{
				return;
			}

			m_changeHighlight.Play();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_max < 0)
			{
				m_max = 0;
			}

			if (m_secondaryMax < 0)
			{
				m_secondaryMax = 0;
			}

			// Deferred on purpose: Refresh() reads the style config and the localization tables, and doing
			// that from inside OnValidate means doing it while the asset it belongs to may still be
			// importing. AssetReadyGate waits for the editor to go quiet AND for those assets to exist -
			// a plain delayCall only waits one frame and can still land mid-import.
			//
			// This is also the only moment an author gets a live update, which is deliberate: the
			// component is NOT [ExecuteAlways]. Running it on every prefab open would let it write text
			// and style names into an asset nobody edited, and the styling system already carries the
			// look. Here somebody did edit something, so writing is what they asked for.
			if (!Application.isPlaying)
			{
				AssetReadyGate.WhenReady(RefreshInEditor);
			}
		}

		private void RefreshInEditor()
		{
			if (this == null)
			{
				return;
			}

			Refresh();
		}

		public bool UsesLocaKey => !string.IsNullOrEmpty(m_formatLocaKey)
			|| !string.IsNullOrEmpty(m_secondaryCaptionLocaKey);

		public bool UsesMultipleLocaKeys => true;

		public string LocaKey => m_formatLocaKey;

		public List<string> LocaKeys
		{
			get
			{
				var keys = new List<string>();
				if (!string.IsNullOrEmpty(m_formatLocaKey))
				{
					keys.Add(m_formatLocaKey);
				}

				if (!string.IsNullOrEmpty(m_secondaryCaptionLocaKey))
				{
					keys.Add(m_secondaryCaptionLocaKey);
				}

				return keys;
			}
		}

		public string Group => m_locaGroup;
#endif
	}
}
