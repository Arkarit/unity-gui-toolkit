using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Button))]
	public class UiButton : UiButtonBase
	{
		[Tooltip("Simple wiggle animation (optional)")]
		public UiSimpleAnimation m_simpleWiggleAnimation;
		
		private Button m_button;

		public Button Button
		{
			get
			{
				InitIfNecessary();
				return m_button;
			}
		}

		public Button.ButtonClickedEvent OnClick => Button.onClick;

		public void Wiggle()
		{
			if (m_simpleWiggleAnimation)
				m_simpleWiggleAnimation.Play();
		}

		protected override void Init()
		{
			base.Init();

			m_button = GetComponent<Button>();
		}

	}
}