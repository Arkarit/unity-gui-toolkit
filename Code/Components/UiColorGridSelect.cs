using GuiToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiColorGridSelect : UiThing
	{
		public enum Mode
		{
			XHueYBright,
			XBrightYHue,
		}

		[SerializeField]
		protected RectTransform m_colorPatchContainer;
		[SerializeField]
		protected UiColorPatch m_colorPatchPrefab;
		[SerializeField]
		protected ToggleGroup m_toggleGroup;
		[SerializeField]
		protected Mode m_mode;
		[SerializeField]
		[FormerlySerializedAs("m_numHorizontal")]
		protected int m_numX;
		[SerializeField]
		[FormerlySerializedAs("m_numVertical")]
		protected int m_numY;
		[SerializeField]
		[Range(0f, 1f)]
		protected float m_minX = 0f;
		[SerializeField]
		[Range(0f, 1f)]
		protected float m_maxX = 1f;
		[SerializeField]
		[Range(0f, 1f)]
		protected float m_minY = 0f;
		[SerializeField]
		[Range(0f, 1f)]
		protected float m_maxY = 1f;
		[SerializeField]
		[Range(0f, 1f)]
		protected float m_fixedValue0 = 1f;
		[SerializeField]
		[Range(0f, 1f)]
		protected float m_fixedValue1 = 1f;
		[SerializeField]
		protected bool m_disallowRed = false;
		[SerializeField]
		protected bool m_disallowGreen = false;
		[SerializeField]
		protected bool m_disallowBlue = false;
		[SerializeField]
		protected bool m_disallowCyan = false;
		[SerializeField]
		protected bool m_disallowYellow = false;
		[SerializeField]
		protected bool m_disallowMagenta = false;

		public Action<Color> OnColorChanged;

		private readonly List<UiColorPatch> m_patches = new List<UiColorPatch>();
		private Color m_currentColor;
		private bool m_currentColorSet;

		public Color Color
		{
			get
			{
				Debug.Assert(m_currentColorSet, "Attempt to ask for color, but current color has not been set yet");
				return m_currentColor;
			}
		}

		protected override void Awake()
		{
			base.Awake();

			float stepX = ((m_maxX - m_minX) / (float) m_numX);
			float stepY = ((m_maxY - m_minY) / (float) m_numY);

			for (int y = 0; y < m_numY; y++ )
			{
				for (int x = 0; x < m_numX; x++ )
				{
					(float h, float s, float v) = GetColor(x, y, stepX, stepY);
					Color color = Color.HSVToRGB(h, s, v);
					if (!IsColorAllowed(color))
						continue;

					UiColorPatch newPatch = Instantiate(m_colorPatchPrefab);
					newPatch.Toggle.group  = m_toggleGroup;
					m_toggleGroup.RegisterToggle(newPatch.Toggle);
					newPatch.Color = Color.HSVToRGB(h, s, v);
					newPatch.transform.SetParent( m_colorPatchContainer, false );
					newPatch.name = "ColorPatch" + (y*m_numX + x);
					newPatch.OnSelected = OnPatchSelected;
					m_patches.Add(newPatch);
				}
			}
		}

		private bool IsColorAllowed( Color _color )
		{
			if (m_disallowRed &&  !IsColorAllowed( _color.r, _color.g, _color.b))
				return false;
			if (m_disallowGreen &&  !IsColorAllowed( _color.g, _color.r, _color.b))
				return false;
			if (m_disallowBlue &&  !IsColorAllowed( _color.b, _color.r, _color.g))
				return false;
			if (m_disallowCyan &&  !IsMixColorAllowed( _color.b, _color.g, _color.r))
				return false;
			if (m_disallowYellow &&  !IsMixColorAllowed( _color.r, _color.g, _color.b))
				return false;
			if (m_disallowMagenta &&  !IsMixColorAllowed( _color.r, _color.b, _color.g))
				return false;
			return true;
		}

		private bool IsColorAllowed( float _channelToCheck, float _otherChannel0, float _otherChannel1 )
		{
			if (_channelToCheck < Mathf.Epsilon)
				return true;
			float otherAverage = (_otherChannel0 +_otherChannel1) *.5f;
			return otherAverage / _channelToCheck >= 0.3333f;
		}

		private bool IsMixColorAllowed( float _channelToCheck0, float _channelToCheck1, float _otherChannel )
		{
			if (_channelToCheck0 == 0 || _channelToCheck1 == 0)
				return true;

			if (_channelToCheck0 > _channelToCheck1)
				UiMathUtility.Swap( ref _channelToCheck0, ref _channelToCheck1);

			if (_channelToCheck0 / _channelToCheck1 < 0.5f)
				return true;

			float channelsToCheck = (_channelToCheck0 + _channelToCheck1) *.5f;
			if (channelsToCheck < Mathf.Epsilon)
				return true;
			return _otherChannel / channelsToCheck >= 0.5f;
		}

		private (float h, float s, float v) GetColor( int _x, int _y, float _stepX, float _stepY )
		{
			float h,s,v;

			switch (m_mode)
			{
				case Mode.XHueYBright:
					h = m_minX + _x * _stepX;
					s = m_fixedValue0;
					v = m_minY + _y * _stepY;
					break;
				case Mode.XBrightYHue:
					h = m_minY + _y * _stepY;
					s = m_fixedValue0;
					v = m_minX + _x * _stepX;
					break;
				default:
					Debug.Assert(false);
					h = s = v = 0;
					break;
			}

			return (h,s,v);
		}

		private void OnPatchSelected( UiColorPatch _patch )
		{
			m_currentColor = _patch.Color;
			m_currentColorSet = true;
			OnColorChanged?.Invoke(_patch.Color);
		}

		public void SelectRandom()
		{
			if (m_patches.Count == 0)
				return;

			StartCoroutine(SelectRandomDelayed());
		}

		// Select patch by color.
		// Note that this may fail, if the patches don't contain the supplied color.
		public bool Select( Color _color )
		{
			for (int i = 0; i < m_patches.Count; i++)
			{
				UiColorPatch patch = m_patches[i];
				if (_color.IsSimilar(patch.Color))
				{
					StartCoroutine(SelectDelayed(i));
					return true;
				}
			}
			return false;
		}

		public void Select( int _x, int _y )
		{
			StartCoroutine(SelectDelayed(_y * m_numY + _x));
		}

		// Shitty Unity Toggles unregister themselves from the group in OnDisable().
		// Thus, if SelectRandom() is called in OnEnable() of another component, the OnEnable()'s of the toggles
		// may have not be called yet, and thus the m_Toggles member of the ToggleGroup may be EMPTY here.
		// Unnecessary to say, that m_Toggles is protected and you have NO option to ask ToggleGroup if its empty or to say CollectToggles() or something.
		// Workaround: Postpone all selection calls until all shitty Unity enabling and disabling and hassenichgesehn is done.
		// Again three hours of my life sacrificed to shitty Unity.

		private IEnumerator SelectRandomDelayed()
		{
			yield return new WaitForEndOfFrame();

			int idx = UnityEngine.Random.Range(0, m_patches.Count);
			m_patches[idx].Selected = true;
		}

		private IEnumerator SelectDelayed( int _idx )
		{
			yield return new WaitForEndOfFrame();
			m_patches[_idx].Selected = true;
		}
	}
}