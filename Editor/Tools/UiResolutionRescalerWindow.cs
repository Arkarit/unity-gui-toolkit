using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GuiToolkit.Editor
{
    public sealed class UiResolutionRescalerWindow : EditorWindow
    {
        private float m_newWidth = 1024f;
        private float m_newHeight = 768f;
        private float m_oldWidth = 1920f;
        private float m_oldHeight = 1080f;

        private bool m_scaleAnchoredPosition = true;
        private bool m_scaleFontSizes = true;
        private bool m_scaleTmpSpacingAndMargins = true;
        private bool m_scaleUiEffects = true;
        private bool m_disableFittersDuringScale = true;
        private bool m_processInactiveObjects = true;

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
            m_scaleAnchoredPosition     = EditorGUILayout.Toggle("Scale Anchored Position", m_scaleAnchoredPosition);
            m_scaleFontSizes            = EditorGUILayout.Toggle("Scale TMP_Text Font Size", m_scaleFontSizes);
            m_scaleTmpSpacingAndMargins = EditorGUILayout.Toggle("Scale TMP Spacing/Margins", m_scaleTmpSpacingAndMargins);
            m_scaleUiEffects            = EditorGUILayout.Toggle("Scale UI Effects (Shadow/Outline)", m_scaleUiEffects);
            m_disableFittersDuringScale = EditorGUILayout.Toggle("Disable Fitters During Scale", m_disableFittersDuringScale);
            m_processInactiveObjects    = EditorGUILayout.Toggle("Process Inactive Objects", m_processInactiveObjects);

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

        private static float SafeDiv(float _a, float _b)
        {
            if (_b == 0f)
                return 1f;
            return _a / _b;
        }

        private void ScaleSelection(float _sx, float _sy)
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
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject root = roots[i];
                    if (root == null)
                        continue;

                    if (m_disableFittersDuringScale)
                    {
                        WithFittersDisabled(root.transform, () =>
                        {
                            ScaleRecursively(root.transform, _sx, _sy);
                        });
                    }
                    else
                    {
                        ScaleRecursively(root.transform, _sx, _sy);
                        ForceRebuildIfRect(root.transform as RectTransform);
                    }
                }

                Undo.CollapseUndoOperations(undoGroup);
                EditorUtility.DisplayDialog("Done", "Rescale completed.", "OK");
            }
            catch
            {
                Undo.RevertAllInCurrentGroup();
                throw;
            }
        }

        private void ScaleRecursively(Transform _t, float _sx, float _sy)
        {
            if (_t == null)
                return;

            if (!m_processInactiveObjects && !_t.gameObject.activeInHierarchy)
                return;

            RectTransform rect = _t as RectTransform;
            if (rect != null)
                ScaleRectTransform(rect, _sx, _sy);

            TMP_Text tmp = _t.GetComponent<TMP_Text>();
            if (tmp != null && m_scaleFontSizes)
                ScaleTmpText(tmp, _sx, _sy);

            GridLayoutGroup grid = _t.GetComponent<GridLayoutGroup>();
            if (grid != null)
                ScaleGridLayout(grid, _sx, _sy);

            HorizontalOrVerticalLayoutGroup hv = _t.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (hv != null)
                ScaleLayoutGroup(hv, _sx, _sy);

            LayoutElement le = _t.GetComponent<LayoutElement>();
            if (le != null)
                ScaleLayoutElement(le, _sx, _sy);

            if (m_scaleUiEffects)
                ScaleUiEffects(_t, _sx, _sy);

            int childCount = _t.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = _t.GetChild(i);
                ScaleRecursively(child, _sx, _sy);
            }
        }

        // Anchor-aware scaling: use offsets for stretch, sizeDelta for non-stretch.
        private void ScaleRectTransform(RectTransform _rect, float _sx, float _sy)
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

        private static bool NearlyEqual(float _a, float _b)
        {
            return Mathf.Abs(_a - _b) <= 0.0001f;
        }

        private void ScaleTmpText(TMP_Text _tmp, float _sx, float _sy)
        {
            Undo.RegisterCompleteObjectUndo(_tmp, "Scale TMP_Text");

            // Uniform factor; if you prefer geometric mean: Mathf.Sqrt(_sx * _sy)
            float s = _sx;

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

                Vector4 m = _tmp.margin;
                m.x *= _sx; // left
                m.z *= _sx; // right
                m.y *= _sy; // top
                m.w *= _sy; // bottom
                _tmp.margin = m;
            }
        }

        private void ScaleGridLayout(GridLayoutGroup _grid, float _sx, float _sy)
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
                pad.left   = Mathf.RoundToInt(pad.left   * _sx);
                pad.right  = Mathf.RoundToInt(pad.right  * _sx);
                pad.top    = Mathf.RoundToInt(pad.top    * _sy);
                pad.bottom = Mathf.RoundToInt(pad.bottom * _sy);
                _grid.padding = pad;
            }
        }

        private void ScaleLayoutGroup(HorizontalOrVerticalLayoutGroup _lg, float _sx, float _sy)
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
                pad.left   = Mathf.RoundToInt(pad.left   * _sx);
                pad.right  = Mathf.RoundToInt(pad.right  * _sx);
                pad.top    = Mathf.RoundToInt(pad.top    * _sy);
                pad.bottom = Mathf.RoundToInt(pad.bottom * _sy);
                _lg.padding = pad;
            }
        }

        private void ScaleLayoutElement(LayoutElement _le, float _sx, float _sy)
        {
            Undo.RegisterCompleteObjectUndo(_le, "Scale LayoutElement");

            if (_le.minWidth >= 0f)       _le.minWidth       *= _sx;
            if (_le.minHeight >= 0f)      _le.minHeight      *= _sy;
            if (_le.preferredWidth >= 0f) _le.preferredWidth *= _sx;
            if (_le.preferredHeight >= 0f)_le.preferredHeight*= _sy;
            // flexibleWidth / flexibleHeight are weights; do not scale
        }

        private void ScaleUiEffects(Transform _t, float _sx, float _sy)
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

        private void WithFittersDisabled(Transform _root, Action _action)
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

        private static void ForceRebuildIfRect(RectTransform _rect)
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
    }
}
