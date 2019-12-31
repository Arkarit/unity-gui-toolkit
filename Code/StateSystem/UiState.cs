using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.UiStateSystem
{
	[AddComponentMenu("")]
	public class UiState : MonoBehaviour
	{
		[SerializeField]
		private string m_name;
		[SerializeField]
		private UiStateMachine m_stateMachine;
		[SerializeField]
		private GameObject m_gameObject;
		[SerializeField]
		private RectTransform m_rectTransform;
		[SerializeField]
		private LayoutElement m_layoutElement;
		[SerializeField]
		private CanvasGroup m_canvasGroup;
		[SerializeField]
		private float m_preferredWidth;
		[SerializeField]
		private float m_preferredHeight;
		[SerializeField]
		private Vector2 m_sizeDelta;
		[SerializeField]
		private Vector2 m_position;
		[SerializeField]
		private bool m_active = true;
		[SerializeField]
		private float m_rotation;
		[SerializeField]
		private Vector3 m_scale;
		[SerializeField]
		private float m_alpha;


		public string Name
		{
			get
			{
				return m_name;
			}
		}

		public GameObject GameObject
		{
			get
			{
				return m_gameObject;
			}
		}

		public UiStateMachine StateMachine
		{
			get
			{
				return m_stateMachine;
			}
		}

		private EStatePropertySupport _support
		{
			get
			{
				return m_stateMachine.Support;
			}
		}

#if UNITY_EDITOR
		public void SetBasicValues( string _name, UiStateMachine _stateMachine, GameObject _gameObject )
		{
			m_name = _name;
			m_stateMachine = _stateMachine;
			m_gameObject = _gameObject;
			FindNecessaryComponents();
		}

		public void FindNecessaryComponents()
		{
			if (m_gameObject == null)
				return;

			m_rectTransform = m_gameObject.transform as RectTransform;
			m_layoutElement = m_gameObject.GetComponent<LayoutElement>();
			m_canvasGroup = m_gameObject.GetComponent<CanvasGroup>();
		}

		public void Record()
		{
			if ((_support & EStatePropertySupport.SizePosition) != 0)
			{
				m_sizeDelta = m_rectTransform.sizeDelta;
				m_position = m_rectTransform.anchoredPosition;
			}
			if ((_support & EStatePropertySupport.Active) != 0)
			{
				m_active = m_gameObject.activeSelf;
			}
			if ((_support & EStatePropertySupport.Rotation) != 0)
			{
				// Aaaand... another Unity frickelation.
				// It is not possible to get the correct value of the rotation you have entered in the "Transform" editor panel during runtime.
				// The Unity team in all of their wisdom chose not to populate this value, but instead for eulerAngles etc. only to return values 
				// between 0..180 degrees for positive rotation and 360..180 for negative rotation.
				// This means, it is not possible to e.g. let an object rotate twice (0..720) since you will always get 0 instead of 720 from eulerAngles.
				// So we poke around in the serialized Transform via reflection. It contains a serialized property "m_LocalEulerAnglesHint", which holds the real
				// values the user entered. Of course, this is not available during runtime.
				SerializedObject serializedTransform = new SerializedObject(m_rectTransform);
				SerializedProperty localEulerAnglesHint = serializedTransform.FindProperty("m_LocalEulerAnglesHint");
				m_rotation = localEulerAnglesHint.vector3Value[2];
			}
			if ((_support & EStatePropertySupport.Scale) != 0)
			{
				m_scale = m_rectTransform.localScale;
			}
			if ((_support & EStatePropertySupport.PreferredWidth) != 0 && m_layoutElement != null)
			{
				m_preferredWidth = m_layoutElement.preferredWidth;
			}
			if ((_support & EStatePropertySupport.PreferredHeight) != 0 && m_layoutElement != null)
			{
				m_preferredHeight = m_layoutElement.preferredHeight;
			}
			if ((_support & EStatePropertySupport.Alpha) != 0 && m_canvasGroup != null)
			{
				m_alpha = m_canvasGroup.alpha;
			}
		}
#endif

		public void ApplyInstant()
		{
			if ((_support & EStatePropertySupport.SizePosition) != 0)
			{
				m_rectTransform.sizeDelta = m_sizeDelta;
				m_rectTransform.anchoredPosition = m_position;
			}
			if ((_support & EStatePropertySupport.Active) != 0)
			{
				m_gameObject.gameObject.SetActive(m_active);
			}
			if ((_support & EStatePropertySupport.Rotation) != 0)
			{
				RotateRectTransform(m_rotation);
			}
			if ((_support & EStatePropertySupport.Scale) != 0)
			{
				m_rectTransform.localScale = m_scale;
			}
			if ((_support & EStatePropertySupport.PreferredWidth) != 0 && m_layoutElement != null)
			{
				m_layoutElement.preferredWidth = m_preferredWidth;
			}
			if ((_support & EStatePropertySupport.PreferredHeight) != 0 && m_layoutElement != null)
			{
				m_layoutElement.preferredHeight = m_preferredHeight;
			}
			if ((_support & EStatePropertySupport.Alpha) != 0 && m_canvasGroup != null)
			{
				m_canvasGroup.alpha = m_alpha;
			}
		}
		public void Apply( UiState _from, float _normalizedValue )
		{
			if ((_support & EStatePropertySupport.SizePosition) != 0)
			{
				m_rectTransform.sizeDelta = Vector2.Lerp(_from.m_sizeDelta, m_sizeDelta, _normalizedValue);
				m_rectTransform.anchoredPosition = Vector2.Lerp(_from.m_position, m_position, _normalizedValue);
			}
			if ((_support & EStatePropertySupport.Active) != 0)
			{
				m_gameObject.SetActive(LerpBool(_from.m_active, m_active, _normalizedValue));
			}
			if ((_support & EStatePropertySupport.Rotation) != 0)
			{
				float rot = Mathf.Lerp(_from.m_rotation, m_rotation, _normalizedValue);
				RotateRectTransform(rot);
			}
			if ((_support & EStatePropertySupport.Scale) != 0)
			{
				m_rectTransform.localScale = Vector3.Lerp(_from.m_scale, m_scale, _normalizedValue);
			}
			if ((_support & EStatePropertySupport.PreferredWidth) != 0 && m_layoutElement != null)
			{
				float val = Mathf.Lerp(_from.m_preferredWidth, m_preferredWidth, _normalizedValue);
				m_layoutElement.preferredWidth = val;
			}
			if ((_support & EStatePropertySupport.PreferredHeight) != 0 && m_layoutElement != null)
			{
				float val = Mathf.Lerp(_from.m_preferredHeight, m_preferredHeight, _normalizedValue);
				m_layoutElement.preferredHeight = val;
			}
			if ((_support & EStatePropertySupport.Alpha) != 0 && m_canvasGroup != null)
			{
				float val = Mathf.Lerp(_from.m_alpha, m_alpha, _normalizedValue);
				m_canvasGroup.alpha = val;
			}
		}

		public static void Interpolate( UiState _oldState, UiState _newState, float _normalizedTime, AnimationCurve _animationCurve )
		{
			// the new state's amount is something between 0 and 1; check for ease and apply it.
			float easedValue = _animationCurve != null ? _animationCurve.Evaluate(_normalizedTime) : _normalizedTime;
			_newState.Apply(_oldState, easedValue);
		}

		private void RotateRectTransform( float _rotation )
		{
#if UNITY_EDITOR
			SerializedObject serializedTransform = new SerializedObject(m_rectTransform);
			SerializedProperty localEulerAnglesHint = serializedTransform.FindProperty("m_LocalEulerAnglesHint");
			Vector3 rotSerialized = localEulerAnglesHint.vector3Value;
			rotSerialized.z = _rotation;
			localEulerAnglesHint.vector3Value = rotSerialized;
			serializedTransform.ApplyModifiedProperties();
#endif
			Vector3 rot = m_rectTransform.eulerAngles;
			rot.z = _rotation;
			m_rectTransform.localRotation = Quaternion.Euler(rot);
		}

		private static bool LerpBool( bool _val1, bool _val2, float _normalizedValue )
		{
			return _normalizedValue > 0.5f ? _val2 : _val1;
		}

	}
}
