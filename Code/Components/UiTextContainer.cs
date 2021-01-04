using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiTextContainer : UiThing
	{
		[Range(0f, 1f)][SerializeField] protected float m_disabledBrightness = 0.6f;
		[Range(0f, 1f)][SerializeField] protected float m_disabledDesaturationStrength = 0.8f;
		[Range(0f, 1f)][SerializeField] protected float m_disabledAlpha = 0.9f;

		protected UiTMPTranslator m_translator;
		protected TextMeshProUGUI m_tmpText;
		protected Text m_text;
		protected bool m_initialized = false;

#if UNITY_EDITOR
		[SerializeField] protected bool m_colorsInitialized = false;
#endif
		[SerializeField] protected VertexGradient m_tmpGradient;
		[SerializeField] protected Color m_color;
		[SerializeField] protected VertexGradient m_disabledTmpGradient;
		[SerializeField] protected Color m_disabledColor;

		public override bool IsEnableableInHierarchy => true;

		public virtual string Text
		{
			get
			{
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					return m_translator.Text;
				if (m_tmpText)
					return m_tmpText.text;
				if (m_text)
					return m_text.text;
				return "";
			}

			set
			{
				if (value == null)
					return;
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					m_translator.Text = value;
				else if (m_tmpText)
					m_tmpText.text = value;
				else if (m_text)
					m_text.text = value;
				else
					Debug.LogError($"No text found for '{gameObject.name}', can not set string '{value}'");
			}
		}

		public Color TextColor
		{
			get
			{
				InitIfNecessary();
				if (m_tmpText)
					return m_tmpText.color;
				if (m_text)
					return m_text.color;
				return Color.black;
			}

			set
			{
				if (value == null)
					return;

				InitIfNecessary();
				if (m_tmpText)
					m_tmpText.color = value;
				else if (m_text)
					m_text.color = value;
				else
					Debug.LogError($"No text found for '{gameObject.name}', can not set color '{value}'");
			}
		}

		public UnityEngine.Object TextComponent
		{
			get
			{
				InitIfNecessary();
				if (m_translator && Application.isPlaying)
					return m_translator;
				if (m_tmpText)
					return m_tmpText;
				if (m_text)
					return m_text;
				return null;
			}
		}

		protected override void Awake()
		{
			base.Awake();
			InitIfNecessary();
		}

		protected virtual void Init() { }

		protected void InitIfNecessary()
		{
			if (m_initialized)
				return;

			m_translator = GetComponentInChildren<UiTMPTranslator>();
			m_tmpText = GetComponentInChildren<TextMeshProUGUI>();
			m_text = GetComponentInChildren<Text>();

			Init();

			m_initialized = true;
		}

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
				if (m_tmpText.enableVertexGradient)
					m_tmpText.colorGradient = _enabled ? m_tmpGradient : m_disabledTmpGradient;
				else
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

		public void SetColorMembersIfNecessary(bool _force)
		{
			if (!m_colorsInitialized)
			{
				if (!EnabledInHierarchy)
				{
					Debug.LogError("Can not set enabled color - please temporarily switch to Enabled");
					return;
				}

				InitColors();

				m_colorsInitialized = true;

				return;
			}

			bool changed = _force;

			if (!changed)
			{
				if (m_tmpText != null)
				{
					changed = !VertexGradientApproximately(m_tmpText.colorGradient, EnabledInHierarchy ? m_tmpGradient : m_disabledTmpGradient);
					if (!changed)
						changed = ColorApproximately(m_tmpText.color, EnabledInHierarchy ? m_color : m_disabledColor);
				}
				else if (m_text != null)
					changed = ColorApproximately(m_text.color, EnabledInHierarchy ? m_color : m_disabledColor);
			}

			if (changed)
			{
				if (EnabledInHierarchy)
				{
					InitColors();
				}
				else
				{
					Debug.LogError("Please don't change text colors when disabled!");
				}
			}
		}

		private void InitColors()
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
	[CustomEditor(typeof(UiTextContainer))]
	public class UiTextContainerEditor : UiThingEditor
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

			UiTextContainer thisUiTextContainer = (UiTextContainer)target;

			UnityEngine.Object textComponent = thisUiTextContainer.TextComponent;
			if (textComponent != null)
			{
				string text = thisUiTextContainer.Text;
				string newText = EditorGUILayout.TextField("Text:", text);
				if (newText != text)
				{
					Undo.RecordObject(textComponent, "Text change");
					thisUiTextContainer.Text = newText;
				}
			}
			if (textComponent != null)
			{
				Color color = thisUiTextContainer.TextColor;
				Color newColor = EditorGUILayout.ColorField("Text Color:", color);
				if (newColor != color)
				{
					Undo.RecordObject(textComponent, "Text color change");
					thisUiTextContainer.TextColor = newColor;
				}
			}

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_disabledAlphaProp);
			EditorGUILayout.PropertyField(m_disabledDesaturationStrengthProp);
			EditorGUILayout.PropertyField(m_disabledBrightnessProp);

			thisUiTextContainer.SetColorMembersIfNecessary(EditorGUI.EndChangeCheck());

			serializedObject.ApplyModifiedProperties();
		}

	}
#endif
}