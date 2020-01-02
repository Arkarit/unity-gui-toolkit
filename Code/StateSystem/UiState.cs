using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.UiStateSystem
{
	[Serializable]
	public class UiState
	{
		//CAUTION!
		// Be sure to add editor clone function
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
		private Vector3 m_rotation;
		[SerializeField]
		private Vector3 m_scale;
		[SerializeField]
		private float m_alpha;


#if UNITY_EDITOR
		public UiState() { }
		public UiState( SerializedProperty _prop)
		{
			m_name				= (string) _prop.FindPropertyRelative("m_name").stringValue;
			m_stateMachine		= (UiStateMachine) _prop.FindPropertyRelative("m_stateMachine").objectReferenceValue;
			m_gameObject		= (GameObject) _prop.FindPropertyRelative("m_gameObject").objectReferenceValue;
			m_rectTransform		= (RectTransform) _prop.FindPropertyRelative("m_rectTransform").objectReferenceValue;
			m_layoutElement		= (LayoutElement) _prop.FindPropertyRelative("m_layoutElement").objectReferenceValue;
			m_canvasGroup		= (CanvasGroup) _prop.FindPropertyRelative("m_canvasGroup").objectReferenceValue;
			m_preferredWidth	= (float) _prop.FindPropertyRelative("m_preferredWidth").floatValue;
			m_preferredHeight	= (float) _prop.FindPropertyRelative("m_preferredHeight").floatValue;
			m_sizeDelta			= (Vector2) _prop.FindPropertyRelative("m_sizeDelta").vector2Value;
			m_position			= (Vector2) _prop.FindPropertyRelative("m_position").vector2Value;
			m_active			= (bool) _prop.FindPropertyRelative("m_active").boolValue;
			m_rotation			= (Vector3) _prop.FindPropertyRelative("m_rotation").vector3Value;
			m_scale				= (Vector3) _prop.FindPropertyRelative("m_scale").vector3Value;
			m_alpha				= (float) _prop.FindPropertyRelative("m_alpha").floatValue;
		}

		public void SetSerializedProperty( SerializedProperty _prop)
		{
			_prop.FindPropertyRelative("m_name").stringValue = m_name;
			_prop.FindPropertyRelative("m_stateMachine").objectReferenceValue = m_stateMachine;
			_prop.FindPropertyRelative("m_gameObject").objectReferenceValue = m_gameObject;
			_prop.FindPropertyRelative("m_rectTransform").objectReferenceValue = m_rectTransform;
			_prop.FindPropertyRelative("m_layoutElement").objectReferenceValue = m_layoutElement;
			_prop.FindPropertyRelative("m_canvasGroup").objectReferenceValue = m_canvasGroup;
			_prop.FindPropertyRelative("m_preferredWidth").floatValue = m_preferredWidth;
			_prop.FindPropertyRelative("m_preferredHeight").floatValue = m_preferredHeight;
			_prop.FindPropertyRelative("m_sizeDelta").vector2Value = m_sizeDelta;
			_prop.FindPropertyRelative("m_position").vector2Value = m_position;
			_prop.FindPropertyRelative("m_active").boolValue = m_active;
			_prop.FindPropertyRelative("m_rotation").vector3Value = m_rotation;
			_prop.FindPropertyRelative("m_scale").vector3Value = m_scale;
			_prop.FindPropertyRelative("m_alpha").floatValue = m_alpha;
		}
#endif


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
				m_rotation = localEulerAnglesHint.vector3Value;
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
				m_rectTransform.sizeDelta = Vector2.LerpUnclamped(_from.m_sizeDelta, m_sizeDelta, _normalizedValue);
				m_rectTransform.anchoredPosition = Vector2.LerpUnclamped(_from.m_position, m_position, _normalizedValue);
			}
			if ((_support & EStatePropertySupport.Active) != 0)
			{
				m_gameObject.SetActive(LerpBool(_from.m_active, m_active, _normalizedValue));
			}
			if ((_support & EStatePropertySupport.Rotation) != 0)
			{
				Vector3 rot = Vector3.LerpUnclamped(_from.m_rotation, m_rotation, _normalizedValue);
				RotateRectTransform(rot);
			}
			if ((_support & EStatePropertySupport.Scale) != 0)
			{
				m_rectTransform.localScale = Vector3.LerpUnclamped(_from.m_scale, m_scale, _normalizedValue);
			}
			if ((_support & EStatePropertySupport.PreferredWidth) != 0 && m_layoutElement != null)
			{
				float val = Mathf.LerpUnclamped(_from.m_preferredWidth, m_preferredWidth, _normalizedValue);
				m_layoutElement.preferredWidth = val;
			}
			if ((_support & EStatePropertySupport.PreferredHeight) != 0 && m_layoutElement != null)
			{
				float val = Mathf.LerpUnclamped(_from.m_preferredHeight, m_preferredHeight, _normalizedValue);
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

		private void RotateRectTransform( Vector3 _rotation )
		{
#if UNITY_EDITOR
			SerializedObject serializedTransform = new SerializedObject(m_rectTransform);
			SerializedProperty localEulerAnglesHint = serializedTransform.FindProperty("m_LocalEulerAnglesHint");
			Vector3 rotSerialized = localEulerAnglesHint.vector3Value;
			rotSerialized = _rotation;
			localEulerAnglesHint.vector3Value = rotSerialized;
			serializedTransform.ApplyModifiedProperties();
#endif
			Vector3 rot = m_rectTransform.eulerAngles;
			rot = _rotation;
			m_rectTransform.localRotation = Quaternion.Euler(rot);
		}

		private static bool LerpBool( bool _val1, bool _val2, float _normalizedValue )
		{
			return _normalizedValue > 0.5f ? _val2 : _val1;
		}

	}
}
