using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.UiStateSystem
{
	[DisallowMultipleComponent]
	public class UiStateMachine : MonoBehaviour, IEditorUpdateable
	{
#if UNITY_EDITOR
		private const bool FORCE_DICT_REBUILD_IN_EDITOR = true;
#else
		private const bool FORCE_DICT_REBUILD_IN_EDITOR = false;
#endif

		[SerializeField]
		// CS0649: Field 'UiStateMachine.m_propertySupport' is never assigned to, and will always have its default value 0
#pragma warning disable 0649
		// Has to be stored as long - Unity does not support long enums.
		private long m_propertySupport;
#pragma warning restore 0649

		[SerializeField]
		private List<GameObject> m_gameObjects;

		[SerializeField]
		private List<string> m_stateNames;

		[SerializeField]
		private List<UiState> m_states;

		[SerializeField]
		private List<UiStateMachine> m_subStateMachines;

		[SerializeField]
		private string m_currentStateName;

		[SerializeField]
		private UiTransition[] m_transitions;


#if UNITY_EDITOR
		[SerializeField]
		private bool m_pristine = true;
#endif

		// Unfortunately the states dictionary has to be built on runtime - Unity does not support dictionary persistence
		private Dictionary<string, List<UiState>> m_statesMap;
		private Dictionary<string, List<UiStateMachine>> m_subStateMachinesMap;

		// Animation related, runtime
		private float m_currentTime;
		private UiTransition m_currentTransition;
		private UiState[] m_from;
		private UiState[] m_to;

		public EStatePropertySupport Support
		{
			get
			{
				return (EStatePropertySupport)m_propertySupport;
			}
		}

		public List<string> StateNames
		{
			get
			{
				return m_stateNames;
			}
		}

		public string State
		{
			get
			{
				return m_currentStateName;
			}
			set
			{
				SetState(value, true);
			}
		}

		public List<UiState> States
		{
			get
			{
				return m_states;
			}
		}

		public List<UiStateMachine> SubStateMachines
		{
			get
			{
				return m_subStateMachines;
			}
		}

		public List<GameObject> GameObjects
		{
			get
			{
				return m_gameObjects;
			}
		}

		public UiTransition[] Transitions
		{
			get
			{
				return m_transitions;
			}
		}

#if UNITY_EDITOR
		public int GetStateNameIndex( string _state = null )
		{
			if (string.IsNullOrEmpty(_state))
				_state = m_currentStateName;
			if (string.IsNullOrEmpty(_state))
				return -1;

			for (int i = 0; i < m_stateNames.Count; i++)
				if (m_stateNames[i] == _state)
					return i;
			return -1;
		}

		public string GetStateName( int _index )
		{
			if (_index < 0 || _index >= m_stateNames.Count)
				return "";
			return m_stateNames[_index];
		}
#endif

		public void UpdateHierarchy()
		{
			for (int i=0; i<m_gameObjects.Count; i++)
			{
				if (m_gameObjects[i] == null)
				{
					m_gameObjects.RemoveAt(i);
					i--;
				}
			}
			for (int i=0; i<m_states.Count; i++)
			{
				if (m_states[i].GameObject == null)
				{
					m_states.RemoveAt(i);
					i--;
				}
			}
		}

		public void OnTransformChildrenChanged()
		{
			UpdateHierarchy();
		}

		public void Start()
		{
			UpdateHierarchy();
			FillStateDictionary();
		}

		public void Update()
		{
			Update(Time.deltaTime);
		}

		public void Update(float _deltaTime)
		{
			if (IsTransitionRunning())
			{
				float val;
				if (m_currentTransition.Eval(m_currentTime, out val))
				{
					ApplyInstant(m_currentTransition.To, false);
					m_currentTransition = null;
					m_from = new UiState[0];
					m_to = new UiState[0];
				}
				else
				{
					for (int i = 0; i < m_to.Length; i++)
					{
						m_to[i].Apply(m_from[i], val);
					}
					m_currentTime += _deltaTime;
				}
			}
		}

#if UNITY_EDITOR
		public void UpdateInEditor( float _deltaTime )
		{
			Update(_deltaTime);
		}

		public bool RemoveFromEditorUpdate()
		{
			return !IsTransitionRunning();
		}

		public UpdateCondition editorUpdateCondition => UpdateCondition.Always;
#endif

		public bool IsTransitionRunning()
		{
			return m_currentTransition != null;
		}

		public void SetState( string _newStateName, bool _useTransition = true )
		{
			Debug.Assert(!string.IsNullOrEmpty(_newStateName));

			List<UiStateMachine> subStateMachines;
			if (m_subStateMachinesMap.TryGetValue(m_currentStateName, out subStateMachines))
			{
				foreach(var subStateMachine in subStateMachines)
					subStateMachine.SetState(_newStateName, _useTransition);
			}

			if (_newStateName == m_currentStateName && !IsTransitionRunning())
				return;

			UiTransition transition = null;
			if (_useTransition)
				transition = FindTransition(_newStateName);

			//TODO handle already running transition - most probably create temp state as a basis

			if (transition == null)
			{
				ApplyInstant(_newStateName);
				return;
			}

			FillStateDictionary(FORCE_DICT_REBUILD_IN_EDITOR);

			//FIXME: states sort order awkward - sort order should be maintained on adding states
			List<UiState> fromStates;
			if (!m_statesMap.TryGetValue(m_currentStateName, out fromStates))
			{
				Debug.Assert(false);
				return;
			}
			List<UiState> toStates;
			if (!m_statesMap.TryGetValue(transition.To, out toStates))
			{
				Debug.Assert(false);
				return;
			}
			int numStates = fromStates.Count;
			if (numStates != toStates.Count)
			{
				Debug.Assert(false);
				return;
			}
			m_from = new UiState[numStates];
			m_to = new UiState[numStates];
			for (int i = 0; i < numStates; i++)
			{
				UiState state = fromStates[i];
				m_from[i] = state;
				for (int j = 0; j < numStates; j++)
				{
					if (toStates[j].GameObject == state.GameObject)
					{
						m_to[i] = toStates[j];
						break;
					}
				}
			}

			m_currentTime = 0;
			m_currentTransition = transition;
#if UNITY_EDITOR
			EditorUpdater.StartUpdating(this);
#endif
		}


#if UNITY_EDITOR
		public void Record()
		{
			if (string.IsNullOrEmpty(m_currentStateName))
			{
				Debug.LogError("No state selected, not possible to Record");
				return;
			}

			FillStateDictionary(true);

			List<UiState> states;

			if (!m_statesMap.TryGetValue(m_currentStateName, out states))
			{
				Debug.Assert(false);
				return;
			}

			foreach (var state in states)
				state.Record();

			List<UiStateMachine> subStateMachines;
			if (m_subStateMachinesMap.TryGetValue(m_currentStateName, out subStateMachines))
			{
				foreach(var subStateMachine in subStateMachines)
				{
					subStateMachine.m_currentStateName = m_currentStateName;
					subStateMachine.Record();
				}
			}
		}

#endif

		public void ApplyInstant( bool _alsoSubStateMachines = true )
		{
			if (string.IsNullOrEmpty(m_currentStateName))
				return;

			FillStateDictionary(FORCE_DICT_REBUILD_IN_EDITOR);

			List<UiState> states;

			if (!m_statesMap.TryGetValue(m_currentStateName, out states))
			{
				Debug.Assert(false);
				return;
			}

			foreach (var state in states)
				state.ApplyInstant();

			if (!_alsoSubStateMachines)
				return;

			List<UiStateMachine> subStateMachines;
			if (m_subStateMachinesMap.TryGetValue(m_currentStateName, out subStateMachines))
			{
				foreach(var subStateMachine in subStateMachines)
					subStateMachine.ApplyInstant(m_currentStateName);
			}
		}

		public void ApplyInstant( string _newStateName, bool _alsoSubStateMachines = true )
		{
			Debug.Assert(!string.IsNullOrEmpty(_newStateName));
			if (string.IsNullOrEmpty(_newStateName) || (m_currentStateName == _newStateName && !IsTransitionRunning()))
				return;

			m_currentTransition = null;
			m_currentStateName = _newStateName;
			ApplyInstant( _alsoSubStateMachines );
		}

#if UNITY_EDITOR
		private static void GetSubStateMachineExclusionSet( UiStateMachine _stateMachine, HashSet<Transform> _exclusionSet )
		{
			foreach(UiStateMachine subStateMachine in _stateMachine.m_subStateMachines)
			{
				foreach(GameObject gameObject in subStateMachine.m_gameObjects)
					_exclusionSet.Add(gameObject.transform);
				GetSubStateMachineExclusionSet( subStateMachine, _exclusionSet );
			}
		}

		public HashSet<Transform> GetSubStateMachineExclusionSet()
		{
			HashSet<Transform> result = new HashSet<Transform>();
			GetSubStateMachineExclusionSet(this, result);
			return result;
		}
#endif

		private UiTransition FindTransition( string _stateName )
		{
			Debug.Assert(!string.IsNullOrEmpty(_stateName));

			if (string.IsNullOrEmpty(m_currentStateName))
				return null;

			// try to find a transition matching "from" and "to"
			for (var i = 0; i < m_transitions.Length; i++)
				if (m_transitions[i].To == _stateName && m_transitions[i].From == m_currentStateName)
					return m_transitions[i];

			// then, find a transition matching "to", but empty "from" (transition from any state)
			for (var i = 0; i < m_transitions.Length; i++)
				if (m_transitions[i].To == _stateName && string.IsNullOrEmpty(m_transitions[i].From))
					return m_transitions[i];

			// no transition was found
			return null;
		}

		private void FillStateDictionary( bool _force = false )
		{
			Debug.Assert(m_states.Count == m_stateNames.Count * m_gameObjects.Count);

			if (!_force && m_statesMap != null)
				return;

			m_statesMap = new Dictionary<string, List<UiState>>();
			m_subStateMachinesMap = new Dictionary<string, List<UiStateMachine>>();

			foreach(UiStateMachine subStateMachine in m_subStateMachines)
			{
				subStateMachine.FillStateDictionary(_force);
			}

			foreach (string stateName in m_stateNames)
			{
				List<UiState> states = new List<UiState>();
				foreach (UiState state in m_states)
				{
					if (state.Name == stateName)
						states.Add(state);
				}

				if (states.Count != 0)
					m_statesMap[stateName] = states;

				List<UiStateMachine> subStateMachines = new List<UiStateMachine>();
				foreach(UiStateMachine subStateMachine in m_subStateMachines)
				{
					if (subStateMachine.m_statesMap.ContainsKey(stateName))
						subStateMachines.Add(subStateMachine);
				}

				if (subStateMachines.Count != 0)
					m_subStateMachinesMap[stateName] = subStateMachines;
			}
		}

	}
}
