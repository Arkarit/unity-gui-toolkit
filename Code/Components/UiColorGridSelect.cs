using GuiToolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
		protected int m_numHorizontal;
		[SerializeField]
		protected int m_numVertical;
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

		public Action<Color> OnColorChanged;

		private readonly List<UiColorPatch> m_patches = new List<UiColorPatch>();

		protected override void Awake()
		{
			base.Awake();

			float stepX = ((m_maxX - m_minX) / (float) m_numHorizontal);
			float stepY = ((m_maxY - m_minY) / (float) m_numVertical);

			for (int y = 0; y < m_numVertical; y++ )
			{
				for (int x = 0; x < m_numHorizontal; x++ )
				{
					(float h, float s, float v) = GetColor(x, y, stepX, stepY);
					UiColorPatch newPatch = Instantiate(m_colorPatchPrefab);
					newPatch.Toggle.group  = m_toggleGroup;
					m_toggleGroup.RegisterToggle(newPatch.Toggle);
					newPatch.Color = Color.HSVToRGB(h, s, v);
					newPatch.transform.SetParent( m_colorPatchContainer, false );
					newPatch.name = "ColorPatch" + (y*m_numHorizontal + x);
					newPatch.OnSelected = OnPatchSelected;
					m_patches.Add(newPatch);
				}
			}
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
			OnColorChanged?.Invoke(_patch.Color);
		}

		public void SelectRandom()
		{
			if (m_patches.Count == 0)
				return;

			// Shitty Unity Toggles unregister themselves from the group in OnDisable().
			// Thus, if SelectRandom() is called in OnEnable() of another component, the OnEnable()'s of the toggles
			// may have not be called yet, and thus the m_Toggles member of the ToggleGroup may be EMPTY here.
			// Unnecessary to say, that m_Toggles is protected and you have NO option to ask ToggleGroup if its empty or to say CollectToggles() or something.
			// Workaround: Postpone selection until all shitty Unity enabling and disabling and hassenichgesehn is done.
			// Again three hours of my life sacrificed to shitty Unity.
			StartCoroutine(SelectRandomDelayed());
		}

		private IEnumerator SelectRandomDelayed()
		{
			yield return new WaitForEndOfFrame();

			m_toggleGroup.EnsureValidState();
			int idx = UnityEngine.Random.Range(0, m_patches.Count);
			m_patches[idx].Selected = true;
		}
	}
}