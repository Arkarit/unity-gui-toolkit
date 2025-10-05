using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[Serializable]
	public class OrientationDependentDefinition
	{
		public Component Target;

		// Template types:
		// 0: mobile landscape
		// 1: mobile portrait
		// 2: pc landscape
		// 3: pc portrait
		// mobile is all tablets and phones,
		// pc is all pc, mac, webplayer and console
		public Component[] OrientationTemplates = new Component[ScreenOrientation.DeprecatedCount];
	}

	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class UiOrientationDependentSwitcher : UiThing
	{
		[SerializeField] protected OrientationDependentDefinition[] m_definitions = new OrientationDependentDefinition[0];
		[SerializeField] protected GameObject[] m_visibleInLandscape = new GameObject[0];
		[SerializeField] protected GameObject[] m_visibleInPortrait = new GameObject[0];
		[SerializeField] protected bool m_autoUpdateOnEnable = true;
#if UNITY_EDITOR
		[SerializeField] private ScreenOrientation m_lastScreenOrientation = ScreenOrientation.Empty;
#endif

		public OrientationDependentDefinition[] Definitions => m_definitions;

		protected override bool NeedsOnScreenOrientationCallback => true;

		public virtual void UpdateElements()
		{
			if (!enabled)
				return;

			ScreenOrientation orientation = UiUtility.GetCurrentScreenOrientation();

			foreach( var definition in m_definitions )
			{
				Component source = definition.OrientationTemplates[orientation.DeprecatedIndex];

				source.CopyTo(definition.Target);

				// Special treatments after copy
				// -----------------------------

				// BaseMeshEffectTmp needs a SetDirty() after values have been changed to actually display the changes
				var baseMeshEffectTmp = definition.Target as BaseMeshEffectTMP;
				if (baseMeshEffectTmp)
					baseMeshEffectTmp.SetDirty();
			}

			foreach (var go in m_visibleInLandscape)
				go.SetActive(orientation.IsLandscape);

			foreach (var go in m_visibleInPortrait)
				go.SetActive(orientation.IsPortrait);

		}

		// Shitty Unity has wrong Screen.width and Screen.height during OnEnable() :-O~
		// Thus, we need to define Update() in Editor - in Update() the values are correct.
#if UNITY_EDITOR
		private void Update()
		{
			if (Application.isPlaying)
				return;

			//UiLog.Log($"Update() UiUtility.GetCurrentScreenOrientation():{UiUtility.GetCurrentScreenOrientation()}");

			ScreenOrientation orientation = UiUtility.GetCurrentScreenOrientation();

			if (orientation == m_lastScreenOrientation)
				return;

			m_lastScreenOrientation = orientation;
			UpdateElements();
		}
#endif

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_autoUpdateOnEnable)
				UpdateElements();
		}

		protected override void OnScreenOrientationChanged( ScreenOrientation _oldScreenOrientation, ScreenOrientation _newScreenOrientation )
		{
			UpdateElements();
		}

	}
}