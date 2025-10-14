using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{

	/// <summary>
	/// This component allows easy access to UiMultiPurposeShader.shader features from C#
	/// </summary>
	[RequireComponent(typeof(Graphic))]
	public class UiMultiPurposeShaderHelper : MonoBehaviour
	{
		public const string SHADER_NAME = "UIToolkit/UI_MultiPurpose";
		private const string KEYWORD_SCROLLING = "Scrolling";
		private const string KEYWORD_SCROLLING_SPEED_U = "_ScrollingSpeedU";
		private const string KEYWORD_SCROLLING_SPEED_V = "_ScrollingSpeedV";
		
		[Tooltip("If checked, a clone of the original material is created. Usually you want to enable this, because otherwise all other assets which use the material are affected.")]
		[SerializeField] private bool m_doCloneMaterial = true;
		
		private Material m_material;
		
		private int m_keywordScrolling;
		private int m_keywordScrollingSpeedU;
		private int m_keywordScrollingSpeedV;
		
		private bool m_isScrolling;
		private Vector2 m_scrollingSpeed = new();

		private Shader Shader => m_material != null ? m_material.shader : null;

		private void Awake()
		{
			m_keywordScrolling = Animator.StringToHash(KEYWORD_SCROLLING);
			m_keywordScrollingSpeedU = Animator.StringToHash(KEYWORD_SCROLLING_SPEED_U);
			m_keywordScrollingSpeedV = Animator.StringToHash(KEYWORD_SCROLLING_SPEED_V);
		}

		private Material Material
		{
			get
			{
				InitIfNecessary();
				return m_material;
			}
		}

		private void InitIfNecessary()
		{
			if (m_material != null)
				return;

			var graphic = GetComponent<Graphic>();
			m_material = graphic.material;

			if (Shader.name != SHADER_NAME)
			{
				var foundShaderName = Shader.name;
				m_material = null;
				throw new Exception(
					$"This component is only suitable for shader '{SHADER_NAME}', but found {foundShaderName}");
			}

			if (m_doCloneMaterial)
			{
				m_material = Instantiate(m_material);
				graphic.material = m_material;
			}
			
			m_isScrolling = m_material.IsKeywordEnabled(KEYWORD_SCROLLING);
			m_scrollingSpeed.x = m_material.GetFloat(KEYWORD_SCROLLING_SPEED_U);
			m_scrollingSpeed.y = m_material.GetFloat(KEYWORD_SCROLLING_SPEED_V);
		}

		public bool IsScrolling
		{
			get
			{
				InitIfNecessary();
				return m_isScrolling;
			}
			set
			{
				InitIfNecessary();
				if (value == m_isScrolling)
					return;
				
				m_isScrolling = SetKeywordEnabled(KEYWORD_SCROLLING, value);
			}
		}
		
		public Vector2 ScrollingSpeed
		{
			get
			{
				InitIfNecessary();
				return m_scrollingSpeed;
			}
			set
			{
				InitIfNecessary();
				m_scrollingSpeed = value;
				m_material.SetFloat(m_keywordScrollingSpeedU, m_scrollingSpeed.x);
				m_material.SetFloat(m_keywordScrollingSpeedV, m_scrollingSpeed.y);
			}
		}
		
		private bool SetKeywordEnabled(string _keyword, bool _isEnabled)
		{
			var mat = Material;
			if (_isEnabled)
			{
				mat.EnableKeyword(_keyword);
				return true;
			}
			
			mat.DisableKeyword(_keyword);
			return false;
		}
	}
}
