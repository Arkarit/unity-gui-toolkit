using System;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public struct OrientationDependentDefinition
	{
		public Component Target;
		public Component TemplateLandscape;
		public Component TemplatePortrait;
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
		[SerializeField] private EScreenOrientation m_lastScreenOrientation = EScreenOrientation.Invalid;
#endif

		public OrientationDependentDefinition[] Definitions => m_definitions;

		protected override bool NeedsOnScreenOrientationCallback => true;

		public virtual void UpdateElements()
		{
			if (!enabled)
				return;

			bool isLandscape = Screen.width >= Screen.height;
			//Debug.Log($"isLandscape: {isLandscape} Screen.width:{Screen.width} Screen.height:{Screen.height}");

			foreach( var definition in m_definitions )
			{
				Component source = isLandscape ? definition.TemplateLandscape : definition.TemplatePortrait;
				//Debug.Log($"Copy {source} to {definition.Target}");

				definition.Target.CopyFrom(source);

				// Special treatments after copy
				// -----------------------------

				// BaseMeshEffectTmp needs a SetDirty() after values have been changed to actually display the changes
				var baseMeshEffectTmp = definition.Target as BaseMeshEffectTMP;
				if (baseMeshEffectTmp)
					baseMeshEffectTmp.SetDirty();
			}

			foreach (var go in m_visibleInLandscape)
				go.SetActive(isLandscape);

			foreach (var go in m_visibleInPortrait)
				go.SetActive(!isLandscape);

		}

		// Shitty Unity has wrong Screen.width and Screen.height during OnEnable() :-O~
		// Thus, we need to define Update() in Editor - in Update() the values are correct.
#if UNITY_EDITOR
		private void Update()
		{
			if (Application.isPlaying)
				return;

			//Debug.Log("Update()");

			EScreenOrientation screenOrientation = Screen.width >= Screen.height ? EScreenOrientation.Landscape : EScreenOrientation.Portrait;
			if (screenOrientation == m_lastScreenOrientation)
				return;

			m_lastScreenOrientation = screenOrientation;
			UpdateElements();
		}
#endif

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_autoUpdateOnEnable)
				UpdateElements();
		}

		protected override void OnScreenOrientationChanged( EScreenOrientation _oldScreenOrientation, EScreenOrientation _newScreenOrientation )
		{
			UpdateElements();
		}

	}
}