using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit.Editor
{
	public sealed class UiResolutionRescalerWindow : EditorWindow
	{
		private float m_oldWidth = 1024f;
		private float m_oldHeight = 768f;
		private float m_newWidth = 1920f;
		private float m_newHeight = 1080f;

		private bool m_scaleAnchoredPosition = true;
		private bool m_scaleFontSizes = true;
		private bool m_scaleTmpSpacingAndMargins = true;
		private bool m_scaleUiEffects = true;
		private bool m_disableFittersDuringScale = true;
		private bool m_processInactiveObjects = true;
		// Fallbacks when no CanvasScaler is found in parent (important for sub-prefabs)
		private CanvasScaler.ScaleMode m_fallbackScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		private float m_fallbackMatchWidthOrHeight = .5f; // 0=width, 1=height

		[MenuItem(StringConstants.RESOLUTION_RESCALER)]
		private static void Open()
		{
			UiResolutionRescalerWindow wnd = GetWindow<UiResolutionRescalerWindow>();
			wnd.titleContent = new GUIContent("UI Resolution Rescaler");
			wnd.minSize = new Vector2(360f, 260f);
			wnd.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox(
				"\nPreview version — handle with care! While the tool is already functional, many cases are still untested or unsupported (especially UI styles). Unexpected results may occur.\n",
				MessageType.Warning);

			EditorGUILayout.HelpBox(
				"\nUse this tool to rescale an entire dialog after changing the Canvas Scaler reference resolution.\n\n" +
				"1. Enter the previous and new reference resolutions.\n" +
				"2. Select the affected dialog root(s).\n" +
				"3. Click 'Scale Selected Hierarchies'.\n",
				MessageType.Info);

			EditorGUILayout.LabelField("Reference Resolution", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			m_oldWidth = EditorGUILayout.FloatField("Old Width", m_oldWidth);
			m_oldHeight = EditorGUILayout.FloatField("Old Height", m_oldHeight);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			m_newWidth = EditorGUILayout.FloatField("New Width", m_newWidth);
			m_newHeight = EditorGUILayout.FloatField("New Height", m_newHeight);
			EditorGUILayout.EndHorizontal();

			float sx = SafeDiv(m_newWidth, m_oldWidth);
			float sy = SafeDiv(m_newHeight, m_oldHeight);
			EditorGUILayout.Space(4f);
			EditorGUILayout.HelpBox($"ScaleX: {sx:0.###}   ScaleY: {sy:0.###}", MessageType.Info);

			EditorGUILayout.Space(6f);
			EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
			m_scaleAnchoredPosition = EditorGUILayout.Toggle("Scale Anchored Position", m_scaleAnchoredPosition);
			m_scaleFontSizes = EditorGUILayout.Toggle("Scale TMP_Text Font Size", m_scaleFontSizes);
			m_scaleTmpSpacingAndMargins = EditorGUILayout.Toggle("Scale TMP Spacing/Margins", m_scaleTmpSpacingAndMargins);
			m_scaleUiEffects = EditorGUILayout.Toggle("Scale UI Effects (Shadow/Outline)", m_scaleUiEffects);
			m_disableFittersDuringScale = EditorGUILayout.Toggle("Disable Fitters During Scale", m_disableFittersDuringScale);
			m_processInactiveObjects = EditorGUILayout.Toggle("Process Inactive Objects", m_processInactiveObjects);
			EditorGUILayout.Space(6f);
			EditorGUILayout.LabelField("Fallback (when no Canvas Scaler is found)", EditorStyles.boldLabel);
			m_fallbackScaleMode = (CanvasScaler.ScaleMode)EditorGUILayout.EnumPopup("Fallback Scale Mode", m_fallbackScaleMode);
			m_fallbackMatchWidthOrHeight = EditorGUILayout.Slider("Fallback Match (0..1)", m_fallbackMatchWidthOrHeight, 0f, 1f);

			EditorGUILayout.Space(10f);

			using (new EditorGUI.DisabledScope(Selection.gameObjects == null || Selection.gameObjects.Length == 0))
			{
				if (GUILayout.Button($"Scale Selected Hierarchies ({Selection.gameObjects.Length})"))
				{
					ScaleSelection(sx, sy);
				}
			}

			if (GUILayout.Button("Select All Root Canvases In Scene"))
			{
				SelectAllRootCanvases();
			}
		}

		private static float SafeDiv( float _a, float _b )
		{
			if (_b == 0f)
				return 1f;
			return _a / _b;
		}

		private void ScaleSelection( float _sx, float _sy )
		{
			if (_sx == 1f && _sy == 1f)
			{
				if (!EditorUtility.DisplayDialog("No-op", "Scale factors are 1. Nothing to do.", "OK", "Cancel"))
					return;
			}

			GameObject[] roots = Selection.gameObjects;
			if (roots == null || roots.Length == 0)
				return;

			int undoGroup = Undo.GetCurrentGroup();
			Undo.SetCurrentGroupName("UI Resolution Rescale");
			try
			{
				foreach (var root in roots)
				{
					if (root == null)
						continue;

					var scaler = root.GetComponentInParent<CanvasScaler>();
					// Use real scaler if present, otherwise use fallbacks
					float u = ComputeCanvasUniformWithFallback(
						m_oldWidth, m_oldHeight, m_newWidth, m_newHeight,
						scaler, m_fallbackScaleMode, m_fallbackMatchWidthOrHeight);

					Action runner = () => ScaleRecursively(root.transform, u, u, u);

					if (m_disableFittersDuringScale)
						WithFittersDisabled(root.transform, runner);
					else
					{
						runner();
						ForceRebuildIfRect(root.transform as RectTransform);
					}

					string src = scaler != null ? $"scaler {scaler.name}" : "fallback settings";
					UiLog.Log(
						$"--- Scaling for root {root.name}, {src} by factor '{u.ToString(CultureInfo.InvariantCulture)}' done. ---\n" +
						"If you need to rescale unsupported items, use the factor shown here.");
				}

				Undo.CollapseUndoOperations(undoGroup);
			}
			catch
			{
				Undo.RevertAllInCurrentGroup();
				throw;
			}
		}

		private void ScaleRecursively( Transform _t, float _sx, float _sy, float _sUniform )
		{
			if (_t == null) return;
			if (!m_processInactiveObjects && !_t.gameObject.activeInHierarchy) return;

			int childCount = _t.childCount;
			for (int i = 0; i < childCount; i++)
				ScaleRecursively(_t.GetChild(i), _sx, _sy, _sUniform);

			var rect = _t as RectTransform;
			if (rect != null)
				ScaleRectTransform(rect, _sx, _sy);

			var legacy = _t.GetComponent<Text>();
			if (legacy != null && m_scaleFontSizes)
				ScaleLegacyText(legacy, _sUniform);

			var tmp = _t.GetComponent<TMP_Text>();
			if (tmp != null && m_scaleFontSizes)
				ScaleTmpText(tmp, _sx, _sy, _sUniform);

			var grid = _t.GetComponent<GridLayoutGroup>();
			if (grid != null)
				ScaleGridLayout(grid, _sx, _sy);

			var hv = _t.GetComponent<HorizontalOrVerticalLayoutGroup>();
			if (hv != null)
				ScaleLayoutGroup(hv, _sx, _sy);

			var le = _t.GetComponent<LayoutElement>();
			if (le != null)
				ScaleLayoutElement(le, _sx, _sy);

			if (m_scaleUiEffects)
				ScaleUiEffects(_t, _sx, _sy);
		}

		// Anchor-aware scaling: use offsets for stretch, sizeDelta for non-stretch.
		private void ScaleRectTransform( RectTransform _rect, float _sx, float _sy )
		{
			Undo.RegisterCompleteObjectUndo(_rect, "Scale RectTransform");

			Vector2 aMin = _rect.anchorMin;
			Vector2 aMax = _rect.anchorMax;

			bool stretchX = !NearlyEqual(aMin.x, aMax.x);
			bool stretchY = !NearlyEqual(aMin.y, aMax.y);

			// X axis
			if (stretchX)
			{
				Vector2 offMin = _rect.offsetMin;
				Vector2 offMax = _rect.offsetMax;
				offMin.x *= _sx;
				offMax.x *= _sx;
				_rect.offsetMin = offMin;
				_rect.offsetMax = offMax;
			}
			else
			{
				Vector2 sd = _rect.sizeDelta;
				sd.x *= _sx;
				_rect.sizeDelta = sd;

				if (m_scaleAnchoredPosition)
				{
					Vector2 ap = _rect.anchoredPosition;
					ap.x *= _sx;
					_rect.anchoredPosition = ap;
				}
			}

			// Y axis
			if (stretchY)
			{
				Vector2 offMin = _rect.offsetMin;
				Vector2 offMax = _rect.offsetMax;
				offMin.y *= _sy;
				offMax.y *= _sy;
				_rect.offsetMin = offMin;
				_rect.offsetMax = offMax;
			}
			else
			{
				Vector2 sd = _rect.sizeDelta;
				sd.y *= _sy;
				_rect.sizeDelta = sd;

				if (m_scaleAnchoredPosition)
				{
					Vector2 ap = _rect.anchoredPosition;
					ap.y *= _sy;
					_rect.anchoredPosition = ap;
				}
			}
		}

		private static bool NearlyEqual( float _a, float _b )
		{
			return Mathf.Abs(_a - _b) <= 0.0001f;
		}

		private void ScaleLegacyText( Text _text, float _sUniform )
		{
			Undo.RegisterCompleteObjectUndo(_text, "Scale Legacy Text");

			float s = _sUniform;

			// Font size
			_text.fontSize = Mathf.RoundToInt(_text.fontSize * s);

			// Best Fit
			if (_text.resizeTextForBestFit)
			{
				_text.resizeTextMinSize = Mathf.Max(1, Mathf.RoundToInt(_text.resizeTextMinSize * s));
				_text.resizeTextMaxSize = Mathf.Max(1, Mathf.RoundToInt(_text.resizeTextMaxSize * s));
				if (_text.resizeTextMinSize > _text.resizeTextMaxSize)
					_text.resizeTextMinSize = _text.resizeTextMaxSize;
			}

			// Line spacing (UI.Text has a scalar float)
			_text.lineSpacing *= s;

			// Note: alignment, supportRichText, overflow modes do not require scaling.
		}

		private void ScaleTmpText( TMP_Text _tmp, float _sx, float _sy, float _sUniform )
		{
			Undo.RegisterCompleteObjectUndo(_tmp, "Scale TMP_Text");

			float s = _sUniform;

			_tmp.fontSize *= s;

			if (_tmp.enableAutoSizing)
			{
				_tmp.fontSizeMin *= s;
				_tmp.fontSizeMax *= s;
			}

			if (m_scaleTmpSpacingAndMargins)
			{
				_tmp.characterSpacing *= s;
				_tmp.wordSpacing *= s;
				_tmp.lineSpacing *= s;
				_tmp.paragraphSpacing *= s;
				_tmp.lineSpacingAdjustment *= s;

				Vector4 m = _tmp.margin;
				m.x *= _sx; // left
				m.z *= _sx; // right
				m.y *= _sy; // top
				m.w *= _sy; // bottom
				_tmp.margin = m;
			}
		}

		private void ScaleGridLayout( GridLayoutGroup _grid, float _sx, float _sy )
		{
			Undo.RegisterCompleteObjectUndo(_grid, "Scale GridLayoutGroup");

			Vector2 cell = _grid.cellSize;
			cell.x *= _sx;
			cell.y *= _sy;
			_grid.cellSize = cell;

			Vector2 spacing = _grid.spacing;
			spacing.x *= _sx;
			spacing.y *= _sy;
			_grid.spacing = spacing;

			RectOffset pad = _grid.padding;
			if (pad != null)
			{
				pad.left = Mathf.RoundToInt(pad.left * _sx);
				pad.right = Mathf.RoundToInt(pad.right * _sx);
				pad.top = Mathf.RoundToInt(pad.top * _sy);
				pad.bottom = Mathf.RoundToInt(pad.bottom * _sy);
				_grid.padding = pad;
			}
		}

		private void ScaleLayoutGroup( HorizontalOrVerticalLayoutGroup _lg, float _sx, float _sy )
		{
			Undo.RegisterCompleteObjectUndo(_lg, "Scale LayoutGroup");

			bool isHorizontal = _lg is HorizontalLayoutGroup;
			if (isHorizontal)
				_lg.spacing *= _sx;
			else
				_lg.spacing *= _sy;

			RectOffset pad = _lg.padding;
			if (pad != null)
			{
				pad.left = Mathf.RoundToInt(pad.left * _sx);
				pad.right = Mathf.RoundToInt(pad.right * _sx);
				pad.top = Mathf.RoundToInt(pad.top * _sy);
				pad.bottom = Mathf.RoundToInt(pad.bottom * _sy);
				_lg.padding = pad;
			}
		}

		private void ScaleLayoutElement( LayoutElement _le, float _sx, float _sy )
		{
			Undo.RegisterCompleteObjectUndo(_le, "Scale LayoutElement");

			if (_le.minWidth >= 0f) _le.minWidth *= _sx;
			if (_le.minHeight >= 0f) _le.minHeight *= _sy;
			if (_le.preferredWidth >= 0f) _le.preferredWidth *= _sx;
			if (_le.preferredHeight >= 0f) _le.preferredHeight *= _sy;
			// flexibleWidth / flexibleHeight are weights; do not scale
		}

		private void ScaleUiEffects( Transform _t, float _sx, float _sy )
		{
			Shadow shadow = _t.GetComponent<Shadow>();
			if (shadow == null)
				return;

			Undo.RegisterCompleteObjectUndo(shadow, "Scale UI Effect");
			Vector2 d = shadow.effectDistance;
			d.x *= _sx;
			d.y *= _sy;
			shadow.effectDistance = d;
		}

		private void WithFittersDisabled( Transform _root, Action _action )
		{
			ContentSizeFitter[] fitters = _root.GetComponentsInChildren<ContentSizeFitter>(m_processInactiveObjects);
			List<(ContentSizeFitter fitter, bool enabled)> states = new List<(ContentSizeFitter, bool)>(fitters.Length);

			for (int i = 0; i < fitters.Length; i++)
			{
				ContentSizeFitter f = fitters[i];
				states.Add((f, f.enabled));
				f.enabled = false;
			}

			try
			{
				_action?.Invoke();
			}
			finally
			{
				for (int i = 0; i < states.Count; i++)
					states[i].fitter.enabled = states[i].enabled;

				ForceRebuildIfRect(_root as RectTransform);
			}
		}

		private static void ForceRebuildIfRect( RectTransform _rect )
		{
			if (_rect != null)
				LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
		}

		private void SelectAllRootCanvases()
		{
			Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
			List<GameObject> roots = new List<GameObject>();
			for (int i = 0; i < canvases.Length; i++)
			{
				Canvas c = canvases[i];
				if (c != null)
					roots.Add(c.gameObject);
			}
			Selection.objects = roots.ToArray();
		}

		private static float ComputeUniformScale( Transform _root, float _sx, float _sy )
		{
			var scaler = _root.GetComponentInParent<CanvasScaler>();
			if (scaler == null)
				return _sx;

			if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
				return _sx;

			float m = Mathf.Clamp01(scaler.matchWidthOrHeight);
			return Mathf.Lerp(_sx, _sy, m);
		}

		private static float ComputeCanvasUniform( float _oldW, float _oldH, float _newW, float _newH, CanvasScaler _scaler )
		{
			if (_scaler == null || _scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
				return _newW / _oldW;

			float widthScale = _newW / _oldW;
			float heightScale = _newH / _oldH;

			// Unity does log interpolation:
			float logW = Mathf.Log(widthScale, 2f);
			float logH = Mathf.Log(heightScale, 2f);
			float lerp = Mathf.Lerp(logW, logH, Mathf.Clamp01(_scaler.matchWidthOrHeight));
			return Mathf.Pow(2f, lerp);
		}

		private static float ComputeCanvasUniformWithFallback
		(
			float _oldW, float _oldH, float _newW, float _newH,
			CanvasScaler _scalerOrNull,
			CanvasScaler.ScaleMode _fallbackMode,
			float _fallbackMatchWidthOrHeight 
		)
		{
			// If we have a scaler and it is ScaleWithScreenSize, use its match (Unity's log interpolation)
			if (_scalerOrNull != null && _scalerOrNull.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
				return DoScale(_scalerOrNull.matchWidthOrHeight);

			// No scaler: respect fallback mode
			if (_fallbackMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
				return DoScale(_fallbackMatchWidthOrHeight);

			// For ConstantPixelSize or ConstantPhysicalSize we return width ratio as neutral fallback
			// (Unity does not rescale pixel sizes in these modes the same way; width is a sane bake base.)
			return _newW / _oldW;

			float DoScale(float _scale)
			{
				float widthScale = _newW / _oldW;
				float heightScale = _newH / _oldH;
				float logW = Mathf.Log(widthScale, 2f);
				float logH = Mathf.Log(heightScale, 2f);
				float lerp = Mathf.Lerp(logW, logH, Mathf.Clamp01(_scale));
				return Mathf.Pow(2f, lerp);
			}
		}

	}
}
