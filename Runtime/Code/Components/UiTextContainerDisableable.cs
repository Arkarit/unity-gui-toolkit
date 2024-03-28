using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiTextContainerDisableable : UiTextContainer
	{
		[Range(0f, 1f)][SerializeField] protected float m_disabledBrightness = 0.6f;
		[Range(0f, 1f)][SerializeField] protected float m_disabledDesaturationStrength = 0.8f;
		[Range(0f, 1f)][SerializeField] protected float m_disabledAlpha = 0.9f;

#if UNITY_EDITOR
		[SerializeField] protected bool m_colorsInitialized = false;
#endif
		[SerializeField] protected VertexGradient m_tmpGradient;
		[SerializeField] protected Color m_color;
		[SerializeField] protected VertexGradient m_disabledTmpGradient;
		[SerializeField] protected Color m_disabledColor;

		public override bool IsEnableableInHierarchy => true;

		protected override void OnEnabledInHierarchyChanged( bool _enabled )
		{
			base.OnEnabledInHierarchyChanged(_enabled);

			InitIfNecessary();

			ApplyEnabledDisabledColors(_enabled);
		}

		private void ApplyEnabledDisabledColors( bool _enabled )
		{
			if (m_tmpText != null)
			{
				m_tmpText.colorGradient = _enabled ? m_tmpGradient : m_disabledTmpGradient;
				m_tmpText.color = _enabled ? m_color : m_disabledColor;
			}
			else
			{
				m_text.color = _enabled ? m_color : m_disabledColor;
			}
		}

		private Color CreateDisabledColor( Color _color )
		{
			float h,s,v;
			Color.RGBToHSV(_color, out h, out s, out v);

			s *= 1.0f - m_disabledDesaturationStrength;
			v *= m_disabledBrightness;
			Color result = Color.HSVToRGB(h, s, v);
			result.a = _color.a * m_disabledAlpha;
			return result;
		}

#if UNITY_EDITOR

		public void SetColorMembersIfNecessary(bool _disabledValuesChanged)
		{
			if (!m_colorsInitialized)
			{
				if (!EnabledInHierarchy)
				{
					Debug.LogError("Can not set enabled color - please temporarily switch to Enabled");
					return;
				}

				InitColors(false);

				m_colorsInitialized = true;

				return;
			}

			if (_disabledValuesChanged)
			{
				InitColors(true);
				if (!EnabledInHierarchy)
				{
					if (m_tmpText)
					{
						m_tmpText.colorGradient = m_disabledTmpGradient;
						m_tmpText.color = m_disabledColor;
						EditorUtility.SetDirty(m_tmpText);
					}
					else
					{
						m_text.color = m_disabledColor;
						EditorUtility.SetDirty(m_text);
					}
				}
				return;
			}

			bool changed = false;

			if (m_tmpText != null)
			{
				changed = !VertexGradientApproximately(m_tmpText.colorGradient, EnabledInHierarchy ? m_tmpGradient : m_disabledTmpGradient);
				if (!changed)
					changed = !ColorApproximately(m_tmpText.color, EnabledInHierarchy ? m_color : m_disabledColor);
			}
			else if (m_text != null)
				changed = !ColorApproximately(m_text.color, EnabledInHierarchy ? m_color : m_disabledColor);

			if (changed)
			{
				if (EnabledInHierarchy)
				{
					InitColors(false);
				}
				else
				{
					Debug.LogError("Please don't change text colors when disabled! Disabled color has been reset");
				}
			}
		}

		private void InitColors(bool _onlyDisabledColors)
		{
			if (!_onlyDisabledColors)
			{
				if (m_tmpText != null)
				{
					m_tmpGradient = m_tmpText.colorGradient;
					m_color = m_tmpText.color;
				}
				else if (m_text)
				{
					m_color = m_text.color;
				}
			}

			m_disabledColor = CreateDisabledColor(m_color);
			m_disabledTmpGradient.bottomLeft = CreateDisabledColor(m_tmpGradient.bottomLeft);
			m_disabledTmpGradient.bottomRight = CreateDisabledColor(m_tmpGradient.bottomRight);
			m_disabledTmpGradient.topLeft = CreateDisabledColor(m_tmpGradient.topLeft);
			m_disabledTmpGradient.topRight = CreateDisabledColor(m_tmpGradient.topRight);
		}

		public static bool ColorApproximately( Color _a, Color _b )
		{
			return
				   Mathf.Approximately(_a.r, _b.r)
				&& Mathf.Approximately(_a.g, _b.g)
				&& Mathf.Approximately(_a.b, _b.b)
				&& Mathf.Approximately(_a.a, _b.a);
		}

		public static bool VertexGradientApproximately( VertexGradient _a, VertexGradient _b)
		{
			return 
				   ColorApproximately(_a.bottomLeft, _b.bottomLeft)
				&& ColorApproximately(_a.bottomRight, _b.bottomRight)
				&& ColorApproximately(_a.topLeft, _b.topLeft)
				&& ColorApproximately(_a.topRight, _b.topRight);
		}
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiTextContainerDisableable))]
	public class UiTextContainerDisableableEditor : UiTextContainerEditor
	{
		protected SerializedProperty m_disabledBrightnessProp;
		protected SerializedProperty m_disabledDesaturationStrengthProp;
		protected SerializedProperty m_disabledAlphaProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_disabledBrightnessProp = serializedObject.FindProperty("m_disabledBrightness");
			m_disabledDesaturationStrengthProp = serializedObject.FindProperty("m_disabledDesaturationStrength");
			m_disabledAlphaProp = serializedObject.FindProperty("m_disabledAlpha");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			UiTextContainerDisableable thisUiTextContainerDisableable = (UiTextContainerDisableable)target;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_disabledAlphaProp);
			EditorGUILayout.PropertyField(m_disabledDesaturationStrengthProp);
			EditorGUILayout.PropertyField(m_disabledBrightnessProp);

			bool changed = EditorGUI.EndChangeCheck();

			serializedObject.ApplyModifiedProperties();

			thisUiTextContainerDisableable.SetColorMembersIfNecessary(changed);
		}

	}
#endif
}