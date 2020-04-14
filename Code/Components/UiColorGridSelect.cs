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
			Hue,
			Saturation,
			Value,
		}

		[Serializable]
		public class Channel
		{
			public Mode Mode;

			[Range(1, 50)]
			public int Count = 1;

			[Range(0f, 1f)]
			public float Min = 1f;

			[Range(0f, 1f)]
			public float Max = 1f;

			public Channel(Mode _mode)
			{
				Mode = _mode;
			}

			public void SetHSVValue(ref Vector3 _hsv, int _idx)
			{
				if (_idx >= Count)
					throw new IndexOutOfRangeException();

				float step = (Max - Min) / (float) Count;
				_hsv[(int) Mode] = step * _idx + Min;
			}
		}


		[SerializeField]
		protected RectTransform m_colorPatchContainer;

		[SerializeField]
		protected UiColorPatch m_colorPatchPrefab;

		[SerializeField]
		protected ToggleGroup m_toggleGroup;

		[SerializeField]
		protected Channel m_channelA = new Channel(Mode.Hue);

		[SerializeField]
		protected Channel m_channelB = new Channel(Mode.Saturation);

		[SerializeField]
		protected Channel m_channelC = new Channel(Mode.Value);

		public Action<Color> OnColorChanged;

		private readonly List<UiColorPatch> m_patches = new List<UiColorPatch>();
		private Color m_currentColor;
		private bool m_currentColorSet;

		private int m_numACalculated;
		private int m_numBCalculated;

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
			int count = 0;
			for (int c = 0; c < m_channelC.Count; c++)
			{
				for (int b = 0; b < m_channelB.Count; b++)
				{
					for (int a = 0; a < m_channelA.Count; a++)
					{
						Vector3 hsv = new Vector3();
						m_channelA.SetHSVValue(ref hsv, a);
						m_channelB.SetHSVValue(ref hsv, b);
						m_channelC.SetHSVValue(ref hsv, c);
						Color color = Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
						UiColorPatch newPatch = Instantiate(m_colorPatchPrefab);
						newPatch.Color = color;
						m_patches.Add(newPatch);
						newPatch.Toggle.group  = m_toggleGroup;
						m_toggleGroup.RegisterToggle(newPatch.Toggle);
						newPatch.OnSelected = OnPatchSelected;
						newPatch.transform.SetParent( m_colorPatchContainer, false );
						newPatch.name = "ColorPatch" + count++;
					}
				}
			}
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

		public void Select( int _a, int _b, int _c )
		{
			StartCoroutine(SelectDelayed(_a * m_channelB.Count * m_channelC.Count + _b * m_channelC.Count + _c));
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