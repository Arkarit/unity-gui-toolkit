using GuiToolkit.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiAbstractStyleBase), true)]
	public class UiAbstractStyleBaseDrawer : AbstractPropertyDrawer<UiAbstractStyleBase>
	{
		private const float LineEndGapHor = 20;
		private const float LineGapVert = 4;
		private const float EndGap = 30;
		private bool m_applicableChanged;

		protected override void OnInspectorGUI()
		{
			m_applicableChanged = false;
			var thisStyle = Property.boxedValue as UiAbstractStyleBase;
			if (thisStyle == null)
				return;
			
			// Which config holds this style, and is it the one being edited?
			//
			// Both come from the context the surrounding editor set, not from this property and not from
			// UiAbstractStyleBase.m_styleConfig. That back-reference looks like the obvious source and is
			// not: it is set in the constructor and never maintained, and in the config shipped with the
			// package 64 of 70 styles have it null - which is why UiAbstractStyleBase.EffectiveStyleConfig
			// exists at all, and why anything that trusts it silently does nothing for those styles.
			var owningConfig = UiStyleRowContext.OwnerOf(thisStyle)
			                ?? Property.serializedObject.targetObject as UiStyleConfig
			                ?? thisStyle.StyleConfig;

			bool isInherited = UiStyleRowContext.IsInherited(thisStyle);

			// Where it comes from, phrased so the two cases cannot be confused: another config is named by
			// its asset, a sibling skin of this config by its skin name.
			string inheritedFrom = SourceName(UiStyleRowContext.SkinOwnerOf(thisStyle));

			// Three states worth telling apart at a glance: plain grey for a style this config simply has,
			// blue for one it inherits, yellow for one it inherits AND overrides - the last being the only
			// one that carries a decision, and the only one that can drift from what it came from.
			bool isOverride = UiStyleRowContext.IsOverride(thisStyle);

			// Removed here: one line, no tint, no values. Deliberately NOT a colour of its own - a red row
			// would read as something being broken, and nothing is: the removal is a decision, and the row
			// exists so it can be seen and taken back. Nothing is drawn below the header either, because
			// there is nothing of this style in this skin to draw.
			if (UiStyleRowContext.IsSuppressed(thisStyle))
			{
				DrawRemovedRow(thisStyle, owningConfig, inheritedFrom);
				return;
			}

			if (isInherited)
				Background(InheritedTint, InheritedTint, -3, 0, 0, -10);
			else if (isOverride)
				Background(OverrideTint, OverrideTint, -3, 0, 0, -10);
			else
				Background(-3, 0, 0, -10);
			Space(3);
			Horizontal(SingleLineHeight, () =>
			{
				LabelField("   " + thisStyle.Alias, 0, EditorStyles.boldLabel);
				IncreaseX(EditorGUIUtility.labelWidth + 18);
				// Where the style comes from belongs in this line only when nobody else says it. An applier
				// heads its single style with a line naming the origin, so repeating it here says the same
				// thing twice and costs the room the buttons need; a config inspector has no such line.
				LabelField
				(
					UiStyleRowContext.OriginShownAbove
						? $"T: {thisStyle.SupportedComponentType.Name}"
						: isInherited
							? $"T: {thisStyle.SupportedComponentType.Name}  inh. from {inheritedFrom}"
							: isOverride
								? $"T: {thisStyle.SupportedComponentType.Name}  overr. {OverriddenSourceName(thisStyle)}"
								: $"T: {thisStyle.SupportedComponentType.Name}",
					0,
					EditorStyles.boldLabel
				);
				// One menu instead of up to five buttons. The header text used to run underneath them and
				// the row's actions cost 160 to 215 px of it; none of them is used often enough to be worth
				// that. The menu also has somewhere to put a REASON, which a greyed-out button does not -
				// see BuildRowMenu, where every unavailable entry says why in its own label.
				IncreaseX(-40);

				if (IconButton(EditorUiUtility.MenuIcon("What can be done with this style"), 20))
				{
					// Everything the entries need is captured HERE, while this drawer still points at this
					// row. Unity keeps one drawer instance for a whole array, and a menu callback runs a tick
					// later - by then Property and the fields around it belong to whatever was drawn last,
					// and Delete would take the wrong style with it.
					BuildRowMenu
					(
						thisStyle,
						owningConfig,
						new RowTarget
						(
							owningConfig,
							UiStyleRowContext.SkinOwnerOf(thisStyle)?.Name,
							thisStyle.Key,
							Property.serializedObject.targetObject,
							Property.propertyPath
						),
						isInherited,
						isOverride
					).ShowAsContext();
				}
			});

			Space(5);
			Line(LineGapVert, m_currentRect.width - 5);

			Indent(() =>
			{
				Space(5);

				EditorGUI.BeginChangeCheck();
				
				// Values of an inherited entry are shown, not edited: the instance behind them belongs to
				// another asset, so a change would edit that config - and for the one inside the package it
				// would be dropped on save without a word. "Override here" is the way in.
				using var readOnly = new EditorGUI.DisabledScope(isInherited);

				var oldVal = ApplicableValueBaseDrawer.DrawCondition;
				ApplicableValueBaseDrawer.DrawCondition = ApplicableValueBaseDrawer.EDrawCondition.OnlyEnabled;
				DrawProperties();

				ApplicableValueBaseDrawer.DrawCondition = ApplicableValueBaseDrawer.EDrawCondition.OnlyDisabled;
				if (HasHiddenProperties())
				{
					Foldout(thisStyle, "Unused Properties", false, () =>
					{
						DrawProperties();
					});
				}

				Space(-SingleLineHeight);

				ApplicableValueBaseDrawer.DrawCondition = oldVal;

				if (EditorGUI.EndChangeCheck())
				{
					Property.serializedObject.ApplyModifiedProperties();
					UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
					if (m_applicableChanged)
					{
						InvalidateHeightCache();
						UiEventDefinitions.EvStyleApplicableChanged.InvokeAlways(owningConfig, thisStyle);
#if UNITY_EDITOR
						UiStyleConfig.SetDirty(owningConfig);
#endif
					}
				}
			});

			Space(EndGap);
		}

		/// <summary>
		/// The row of a style this skin has removed from what it inherits.
		///
		/// The plain background every ordinary row gets, a dimmed header, and the word Unity uses for the
		/// same thing on a prefab instance. The menu button stays live - it is the only way back.
		/// </summary>
		private void DrawRemovedRow
		(
			UiAbstractStyleBase _style,
			UiStyleConfig _owningConfig,
			string _inheritedFrom
		)
		{
			Background(-3, 0, 0, -10);
			Space(3);
			Horizontal(SingleLineHeight, () =>
			{
				LabelField("   " + _style.Alias + "   (Removed)", 0, RemovedLabelStyle);
				IncreaseX(EditorGUIUtility.labelWidth + 18);
				LabelField
				(
					UiStyleRowContext.OriginShownAbove
						? $"T: {_style.SupportedComponentType.Name}"
						: $"T: {_style.SupportedComponentType.Name}  was inh. from {_inheritedFrom}",
					0,
					RemovedLabelStyle
				);
				IncreaseX(-40);

				// Same rule as for the full row: only values cross into the callbacks. One drawer instance
				// serves the whole array, and a menu entry runs a tick after the pass that built it.
				if (IconButton(EditorUiUtility.MenuIcon("What can be done with this style"), 20))
				{
					BuildRemovedRowMenu
					(
						_style,
						_owningConfig,
						UiStyleRowContext.Skin,
						UiStyleRowContext.SkinOwnerOf(_style)
					).ShowAsContext();
				}
			});

			Space(10);
		}

		/// <summary>
		/// Greyed-out version of the row header style. Built lazily, because EditorStyles is only there
		/// once there is a GUI, and thrown away with every domain reload like any other static.
		/// </summary>
		private static GUIStyle RemovedLabelStyle
		{
			get
			{
				if (s_removedLabelStyle == null)
				{
					s_removedLabelStyle = new GUIStyle(EditorStyles.boldLabel);
					var color = s_removedLabelStyle.normal.textColor;
					color.a *= 0.5f;
					s_removedLabelStyle.normal.textColor = color;
				}

				return s_removedLabelStyle;
			}
		}

		private static GUIStyle s_removedLabelStyle;

		/// <summary>Subtle tint that sets an inherited entry apart without shouting.</summary>
		private static readonly Color InheritedTint = new Color(0.35f, 0.55f, 0.75f, 0.10f);

		/// <summary>And one for an entry that overrides what it inherits. Slightly stronger, because yellow
		/// reads weaker than blue against the grey behind it.</summary>
		private static readonly Color OverrideTint = new Color(0.85f, 0.70f, 0.20f, 0.13f);

		/// <summary>
		/// Where an overriding entry diverges from - not necessarily the immediate parent.
		/// </summary>
		private static string OverriddenSourceName( UiAbstractStyleBase _style )
			=> UiStyleRowContext.OverriddenSourceName(_style);

		private static string SourceName( UiSkin _skin ) => UiStyleRowContext.SourceName(_skin);

		#region The row menu

		/// <summary>
		/// Everything this row can do.
		///
		/// Built when the button is clicked, and handed only VALUES - never the drawer's own fields. One
		/// drawer instance serves a whole array (see UiSkinDrawer), and a GenericMenu callback runs a tick
		/// after the pass that built it, by which time those fields point at whatever was drawn last;
		/// Delete would take the wrong style with it. The same goes for UiStyleRowContext, which is scoped
		/// to the drawing pass - so the two skins it would have been asked for are resolved here.
		///
		/// What an entry cannot do it still shows, greyed out, with the reason in its own label. That is the
		/// one thing a menu has over a strip of buttons: a disabled button can only say "no".
		/// </summary>
		private static GenericMenu BuildRowMenu
		(
			UiAbstractStyleBase _style,
			UiStyleConfig _owningConfig,
			RowTarget _row,
			bool _isInherited,
			bool _isOverride
		)
		{
			var menu = new GenericMenu();
			var editedSkin = UiStyleRowContext.Skin;
			var sourceSkin = UiStyleRowContext.SkinOwnerOf(_style);

			menu.AddItem(new GUIContent("Copy Values"), false,
				() => CopyValues(_row, _style));

			if (CanPaste(_style, _owningConfig, _isInherited, out string pasteReason))
			{
				menu.AddItem(new GUIContent("Paste Values"), false,
					() => PasteValues(_row, _style));
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(MenuText("Paste Values  -  " + pasteReason)));
			}

			menu.AddSeparator(string.Empty);

			if (_isInherited)
			{
				menu.AddItem(new GUIContent("Open Source Config"), false,
					() => RevealInParent(_style, sourceSkin));
				menu.AddItem(new GUIContent("Override Here"), false,
					() => OverrideInherited(_style, editedSkin));
				menu.AddItem(new GUIContent(RemoveLabel), false,
					() => RemoveHere(_style, editedSkin));

				return menu;
			}

			if (_isOverride)
			{
				menu.AddItem(new GUIContent("Revert to Inherited"), false,
					() => RevertToInherited(_style, editedSkin));

				// Offered on an override too, rather than making it a revert followed by a removal: it is
				// one decision ("we do not have this"), so it should cost one step and one undo entry. The
				// dialog says that the values here go with it.
				menu.AddItem(new GUIContent(RemoveLabel), false,
					() => RemoveHere(_style, editedSkin));
			}

			menu.AddItem(new GUIContent("Find Appliers"), false, () => FindAppliers(_style));
			menu.AddItem(new GUIContent("Rename..."), false, () => Rename(_style, _owningConfig));
			menu.AddSeparator(string.Empty);
			menu.AddItem(new GUIContent(DeleteLabel(_isOverride)), false,
				() => Delete(_style, _owningConfig, _isOverride));

			return menu;
		}

		/// <summary>
		/// Two entries that live next to each other and do very different things, so each says its SCOPE
		/// rather than relying on the reader to know that "remove" is local and "delete" is not. One skin
		/// against every skin is the whole difference, and it is what the two labels are built around.
		/// </summary>
		private const string RemoveLabel = "Remove from This Skin";

		/// <summary>
		/// What Delete takes with it, which is not the same thing on an override row.
		///
		/// EvDeleteStyle removes the style from every skin of the config that HOLDS it - and on an override
		/// row that config is the one being edited, which holds copies, not the style. So the style keeps
		/// resolving from where it is inherited afterwards, and calling that "Delete" promised a deletion
		/// that does not happen. It deletes the override, in every skin; the label says so now.
		/// </summary>
		private static string DeleteLabel( bool _isOverride )
			=> _isOverride ? "Delete Override in All Skins" : "Delete Style in All Skins";

		/// <summary>
		/// Everything a REMOVED row can do, which is deliberately little: take the removal back, go and
		/// look at where the style still lives, or push the removal up and delete it there for everybody -
		/// the pendant to "Apply to Prefab" on a removed component.
		///
		/// No Copy/Paste here. There is nothing of this style in this skin to copy, and pasting values into
		/// something that has been removed would have to un-remove it behind the reader's back.
		/// </summary>
		private static GenericMenu BuildRemovedRowMenu
		(
			UiAbstractStyleBase _style,
			UiStyleConfig _owningConfig,
			UiSkin _editedSkin,
			UiSkin _sourceSkin
		)
		{
			var menu = new GenericMenu();

			menu.AddItem(new GUIContent("Restore"), false,
				() => RestoreRemoved(_style, _editedSkin));
			menu.AddItem(new GUIContent("Open Source Config"), false,
				() => RevealInParent(_style, _sourceSkin));
			menu.AddSeparator(string.Empty);

			// Named by the CONFIG and said in full, not by the skin the row says it comes from: a deletion
			// takes the style out of every skin of that config, so "Delete in skin 'Default'" would promise
			// something narrower than what happens - and for a skin building on a sibling, the config it
			// would delete from is this very one.
			// The config is named here and nowhere else in these menus: on every other row Delete acts on
			// the config being looked at, on this one it acts on another asset entirely.
			string label = MenuText($"Delete Style in All Skins of '{_owningConfig?.name}'");
			if (UiStyleEditorUtility.IsWritable(_owningConfig, out string reason))
			{
				menu.AddItem(new GUIContent(label), false,
					() => Delete(_style, _owningConfig, false));
			}
			else
			{
				menu.AddDisabledItem(new GUIContent(MenuText(label + "  -  " + reason)));
			}

			return menu;
		}

		/// <summary>
		/// A slash in a GenericMenu label does not read as a slash - it opens a submenu. Reasons carry style
		/// aliases and config names, and both of those have slashes in them.
		/// </summary>
		private static string MenuText( string _text ) => _text?.Replace('/', DivisionSlash);

		/// <summary>Looks like a slash, is not one as far as GenericMenu is concerned.</summary>
		private const char DivisionSlash = (char)0x2215;

		private static bool CanPaste
		(
			UiAbstractStyleBase _style,
			UiStyleConfig _config,
			bool _isInherited,
			out string _reason
		)
		{
			// Asked before the clipboard, because it is the answer whatever the clipboard holds - and
			// "override it first" is the actual next step, which a "wrong type" would hide.
			if (_isInherited)
			{
				_reason = "inherited, override it here first";
				return false;
			}

			if (!UiStyleEditorUtility.IsWritable(_config, out _reason))
				return false;

			return UiStyleClipboard.CanPasteInto(_style, out _reason);
		}

		/// <summary>
		/// Where a row's style actually LIVES, as opposed to the property it happens to be drawn through.
		///
		/// The two are not the same, and the difference is not cosmetic. An applier inspector shows its
		/// style through a shared throwaway ScriptableObject (see EditorDisplayHelper), so the property
		/// handed to this drawer belongs to that helper - reading it back yields a fraction of the values,
		/// and writing to it reaches the helper instead of the config, which is a paste that reports
		/// success and changes nothing. Measured: "Pasted 5 values" three times, config untouched.
		///
		/// So the row is named by config, skin and style key, and resolved from the asset when it is
		/// actually needed. The property path is kept only as a fallback, for a style that belongs to no
		/// config at all.
		/// </summary>
		private readonly struct RowTarget
		{
			public RowTarget
			(
				UiStyleConfig _config,
				string _skinName,
				int _key,
				UnityEngine.Object _fallbackObject,
				string _fallbackPath
			)
			{
				Config = _config;
				SkinName = _skinName;
				Key = _key;
				FallbackObject = _fallbackObject;
				FallbackPath = _fallbackPath;
			}

			public UiStyleConfig Config { get; }
			public string SkinName { get; }
			public int Key { get; }
			public UnityEngine.Object FallbackObject { get; }
			public string FallbackPath { get; }

			public SerializedProperty Resolve()
			{
				if (Config != null && !string.IsNullOrEmpty(SkinName))
				{
					var byKey = UiStyleEditorUtility.StylePropertiesByKey(Config, SkinName);
					if (byKey.TryGetValue(Key, out var fromConfig))
						return fromConfig;
				}

				if (FallbackObject == null || string.IsNullOrEmpty(FallbackPath))
					return null;

				return new SerializedObject(FallbackObject).FindProperty(FallbackPath);
			}
		}

		private static void CopyValues( RowTarget _row, UiAbstractStyleBase _style )
		{
			var rowProp = _row.Resolve();
			if (rowProp == null)
			{
				UiLog.LogError($"Cannot copy '{_style?.Alias}': its row no longer resolves.");
				return;
			}

			UiStyleClipboard.Copy(rowProp, _style);
		}

		private static void PasteValues( RowTarget _row, UiAbstractStyleBase _style )
		{
			var rowProp = _row.Resolve();
			if (rowProp == null)
			{
				UiLog.LogError($"Cannot paste into '{_style?.Alias}': its row no longer resolves.");
				return;
			}

			if (UiStyleClipboard.Paste(rowProp, _style) == 0)
				return;

			// A paste flips applicable flags, and an applicable value draws taller than an unused one, so
			// every remembered row height from here down is now wrong.
			PropertyDrawerView.ClearHeightCache();
			UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
		}

		private static void Rename( UiAbstractStyleBase _style, UiStyleConfig _owningConfig )
		{
			Action<AbstractEditorInputDialog> additionalContent = dialog =>
			{
				if (GUILayout.Button("Reset Name"))
				{
					UiEventDefinitions.EvSetStyleAlias.InvokeAlways(_owningConfig, _style, null);
					dialog.Cancel();
				}

				EditorGUILayout.Space(20);
			};

			var newName = EditorInputDialog.Show("Rename", "Please enter new name/path", _style.Alias,
				additionalContent);

			if (!string.IsNullOrEmpty(newName))
				UiEventDefinitions.EvSetStyleAlias.InvokeAlways(_owningConfig, _style, newName);
		}

		/// <summary>
		/// Deletes the style from every skin of the config that holds it - which on an override row means
		/// the copies, not the style, so the two cases cannot share a sentence. See <see cref="DeleteLabel"/>.
		/// </summary>
		private static void Delete( UiAbstractStyleBase _style, UiStyleConfig _owningConfig, bool _isOverride )
		{
			string message = _isOverride
				? $"'{_style.Alias}' loses the values overridden here, in EVERY skin of "
					+ $"'{_owningConfig?.name}'. The style itself is not touched - it stays where it is "
					+ "inherited from, and appliers keep resolving it from there. To be rid of it in this "
					+ $"skin alone, use '{RemoveLabel}'."
				: $"The style '{_style.Alias}' will be removed from '{_owningConfig?.name}' and all of its "
					+ "skins. Appliers using it will then find no style.";

			if (!EditorUtility.DisplayDialog("Are you sure?", message, "OK", "Cancel"))
			{
				return;
			}

			RegisterUndo(_owningConfig, "Delete style");
			UiEventDefinitions.EvDeleteStyle.InvokeAlways(_owningConfig, _style);
		}

		/// <summary>
		/// One snapshot of the whole config, so a row action is a single step in the undo history.
		///
		/// A style is a [SerializeReference] object inside a list inside a plain class inside the config, and
		/// nothing short of the complete object survives that: this is the same call the inheritance
		/// conversion makes for the same reason. Undo also has to be registered BEFORE the change, which is
		/// why every caller does it inside its deferred closure rather than next to its dialog.
		/// </summary>
		private static void RegisterUndo( UiStyleConfig _config, string _what )
		{
			if (_config == null)
				return;

			Undo.RegisterCompleteObjectUndo(_config, _what);
		}

		#endregion

		/// <summary>
		/// Takes the reader to where an inherited style actually lives: selects that config and opens the
		/// inspector on this row.
		///
		/// The skin is handed in rather than looked up, because by the time a menu entry runs the row
		/// context that knew it is already gone.
		/// </summary>
		private static void RevealInParent( UiAbstractStyleBase _style, UiSkin _sourceSkin )
		{
			var config = _sourceSkin?.StyleConfig;

			if (config == null)
			{
				UiLog.LogError($"Cannot open the source of '{_style.Alias}': it does not resolve to any skin. "
					+ "Check 'Inherits from' and 'Inherits skin from'.");
				return;
			}

			UiStyleConfigEditor.Reveal(config, _sourceSkin, _style);
		}

		/// <summary>
		/// Copies an inherited style into the edited config so it can be changed there. Deferred, because it
		/// adds to the very list that is being drawn at that moment.
		/// </summary>
		private static void OverrideInherited( UiAbstractStyleBase _style, UiSkin _editedSkin )
		{
			if (_editedSkin == null)
			{
				UiLog.LogError($"Cannot override '{_style.Name}': the edited config does not declare the skin " +
				               "this style is shown under. Add that skin to it first.");
				return;
			}

			int key = _style.Key;

			EditorApplication.delayCall += () =>
			{
				RegisterUndo(_editedSkin.StyleConfig, "Override style");
				_editedSkin.MaterializeStyle(key);
				PropertyDrawerView.ClearHeightCache();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			};
		}

		/// <summary>
		/// Drops the edited config's own copy so the style is inherited again. Deferred for the same reason.
		/// </summary>
		private static void RevertToInherited( UiAbstractStyleBase _style, UiSkin _editedSkin )
		{
			if (_editedSkin == null)
				return;

			int key = _style.Key;
			string alias = _style.Alias;

			if (!EditorUtility.DisplayDialog
			(
				"Revert to inherited?",
				$"'{alias}' loses the values set here and follows the config it is inherited from again.",
				"Revert",
				"Cancel"
			))
			{
				return;
			}

			EditorApplication.delayCall += () =>
			{
				RegisterUndo(_editedSkin.StyleConfig, "Revert style to inherited");
				_editedSkin.RevertStyleToInherited(key);
				PropertyDrawerView.ClearHeightCache();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			};
		}

		/// <summary>
		/// Removes an inherited style from the edited skin: it stops resolving there, the config it comes
		/// from keeps it. Deferred like the two above, because it changes the very list being drawn.
		///
		/// The dialog names the one consequence that is not obvious - appliers pointing at the style find
		/// nothing from now on - and, for an override, that the values set here go with it.
		/// </summary>
		private static void RemoveHere( UiAbstractStyleBase _style, UiSkin _editedSkin )
		{
			if (_editedSkin == null)
			{
				UiLog.LogError($"Cannot remove '{_style.Name}': the edited config does not declare the skin " +
				               "this style is shown under. Add that skin to it first.");
				return;
			}

			int key = _style.Key;
			string alias = _style.Alias;
			string valuesGo = _editedSkin.OwnsStyle(key)
				? "The values overridden here are dropped, and "
				: string.Empty;

			if (!EditorUtility.DisplayDialog
			(
				$"Remove from skin '{_editedSkin.Name}'?",
				$"{valuesGo}'{alias}' stops resolving in skin '{_editedSkin.Name}': appliers using it will "
				+ "find no style. It stays untouched where it is inherited from, and 'Restore' on the row "
				+ "brings it back.",
				"Remove",
				"Cancel"
			))
			{
				return;
			}

			EditorApplication.delayCall += () =>
			{
				RegisterUndo(_editedSkin.StyleConfig, "Remove style in skin");
				_editedSkin.SuppressStyle(key);
				PropertyDrawerView.ClearHeightCache();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			};
		}

		/// <summary>
		/// Takes a removal back. No dialog: nothing is lost by it, and it is the way out of a removal one
		/// did not mean.
		/// </summary>
		private static void RestoreRemoved( UiAbstractStyleBase _style, UiSkin _editedSkin )
		{
			if (_editedSkin == null)
				return;

			int key = _style.Key;

			EditorApplication.delayCall += () =>
			{
				RegisterUndo(_editedSkin.StyleConfig, "Restore removed style");
				_editedSkin.RestoreSuppressedStyle(key);
				PropertyDrawerView.ClearHeightCache();
				UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			};
		}

		private static void FindAppliers(UiAbstractStyleBase style)
		{
			if (style == null)
				return;
			
			var type = style.GetType();
			HashSet<string> prefabPathsDone = new();
			string prefabPaths = string.Empty;
			string alias = style.Alias;
			
			EditorAssetUtility.FindAllComponentsInAllPrefabs<UiAbstractApplyStyleBase>((applier, _, path) =>
			{
				if (applier.Style == null)
					return true;
				
				if (applier.Style.GetType() != type)
					return true;
				
				if (applier.Style.Alias != alias)
					return true;
				
				if (!prefabPathsDone.Contains(path))
				{
					prefabPaths += $"\t{path}\n";
					prefabPathsDone.Add(path);
				}
				
				prefabPaths += $"\t\t{applier.gameObject.GetPath()}\n";
				return true;
			});
			
			string scenePaths = string.Empty;
			HashSet<string> scenePathsDone = new();
			
			EditorAssetUtility.FindAllComponentsInAllScenes<UiAbstractApplyStyleBase>((applier, _, path) =>
			{
				if (applier.Style == null)
					return true;
				
				if (applier.Style.GetType() != type)
					return true;
				
				if (applier.Style.Alias != alias)
					return true;
				
				if (!scenePathsDone.Contains(path))
				{
					scenePaths += $"\t{path}\n";
					scenePathsDone.Add(path);
				}
				
				scenePaths += $"\t\t{applier.gameObject.GetPath()}\n";
				return true;
			});
			
			if (string.IsNullOrEmpty(prefabPaths) && string.IsNullOrEmpty(scenePaths))
			{
				UiLog.LogInternal($"No Appliers of type {type.FullName} found");
				return;
			}
			
			string s = $"Found Appliers of type {type.FullName}\n";
			
			if (!string.IsNullOrEmpty(prefabPaths))
				s += $"Prefabs:\n{prefabPaths}\n";
			
			if (!string.IsNullOrEmpty(scenePaths))
				s += $"Scenes:\n{scenePaths}\n";
			
			GUIUtility.systemCopyBuffer = s;
			
			s += "\nA copy of this has been pasted to clipboard.";
			UiLog.LogInternal(s);
		}

		/// <summary>
		/// This drawer measures the same value rows twice with different outcomes - once for the applicable
		/// ones and once for the unused ones - by flipping ApplicableValueBaseDrawer.DrawCondition. So the
		/// condition has to be part of the cache key, or the second pass reads the first one's heights.
		/// </summary>
		protected override string HeightCacheKeySuffix => ApplicableValueBaseDrawer.DrawCondition.ToString();

		protected override float GetPropertyHeight(SerializedProperty _property)
		{
			var val = _property.boxedValue as ApplicableValueBase;
			if (val != null && !val.IsApplicable)
			{
				if (ApplicableValueBaseDrawer.DrawCondition == ApplicableValueBaseDrawer.EDrawCondition.OnlyEnabled)
					return 0;
				if (ApplicableValueBaseDrawer.DrawCondition == ApplicableValueBaseDrawer.EDrawCondition.OnlyDisabled)
					return SingleLineHeight;
			}
			
			return base.GetPropertyHeight(_property);
		}

		private bool HasHiddenProperties()
		{
			bool result = false;

			ForEachChildProperty(Property, childProperty =>
			{
				if (childProperty.name == "m_name")
					return true;

				var val = childProperty.boxedValue as ApplicableValueBase;
				if (val != null && !val.IsApplicable)
				{
					result = true;
					return false;
				}

				return true;
			});

			return result;
		}

		private void DrawProperties()
		{
			EditorGUI.BeginChangeCheck();

			ForEachChildProperty(Property, childProperty =>
			{
				if (childProperty.name == "m_name")
					return true;

				PropertyField(childProperty);
				return true;
			});

			if (EditorGUI.EndChangeCheck())
				m_applicableChanged = true;
		}
	}
}
