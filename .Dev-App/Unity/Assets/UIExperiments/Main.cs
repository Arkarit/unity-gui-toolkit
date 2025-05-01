using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace UiExperiments
{
	public class Main : MonoBehaviour
	{
		public Button okButton;
		public Button cancelButton;

		public Button button1;
		public Button button2;
		public Button button3;
		public Button button4;
		public Button button5;

		public TMP_Text text;

		private void Awake()
		{
			Debug.Log("Awake");
		}

		private void Start()
		{
			Debug.Log("Start");
		}

		private void OnEnable()
		{
			Debug.Log("OnEnable");
			okButton.onClick.AddListener(OnOkButton);
			cancelButton.onClick.AddListener(OnCancelButton);

			button1.onClick.AddListener(OnButton1);
			button2.onClick.AddListener(OnButton2);
			button3.onClick.AddListener(OnButton3);
			button4.onClick.AddListener(OnButton4);
			button5.onClick.AddListener(OnButton5);
		}

		private void OnDisable()
		{
			Debug.Log("OnDisable");
			okButton.onClick.RemoveListener(OnOkButton);
			cancelButton.onClick.RemoveListener(OnCancelButton);

			button1.onClick.RemoveListener(OnButton1);
			button2.onClick.RemoveListener(OnButton2);
			button3.onClick.RemoveListener(OnButton3);
			button4.onClick.RemoveListener(OnButton4);
			button5.onClick.RemoveListener(OnButton5);
		}

		private void OnButton1()
		{
			text.text = "Button 1 pressed";
		}

		private void OnButton2()
		{
			text.text = "Button 2 pressed";
		}

		private void OnButton3()
		{
			text.text = "Button 3 pressed";
		}

		private void OnButton4()
		{
			text.text = "Button 4 pressed";
		}

		private void OnButton5()
		{
			text.text = "Button 5 pressed";
		}

		private void OnOkButton()
		{
			Debug.Log("OnOkButton");
#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}

		private void OnCancelButton()
		{
			Debug.Log("OnCancelButton");
#if UNITY_EDITOR
			EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}

}
