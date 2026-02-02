using GuiToolkit.Settings;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public partial class PlayerSettings
	{
		public enum KeyListenerType
		{
			Up,
			Down,
			Pressed,
		}

		public class DragInfo
		{
			public bool MeasuringDistance;
			public bool Active;
			public Vector3 StartPosition;
			public PlayerSetting PlayerSetting;
		}

		private readonly Dictionary<int, KeyBinding> m_keyBindings = new();
		private readonly Dictionary<int, PlayerSetting> m_keyBindingPlayerSettings = new();
		private readonly HashSet<int> m_activeKeyBindings = new();
		private readonly Dictionary<int, DragInfo> m_dragInfos = new();
		private float m_dragTreshold;
		private float m_dragTresholdSqr;
		private bool m_manualUpdate;

		internal bool ManualUpdate
		{
			get => m_manualUpdate;
			set
			{
				m_manualUpdate = value;
				if (value)
					UiEventDefinitions.OnTickPerFrame.RemoveListener(Update);
				else
					UiEventDefinitions.OnTickPerFrame.AddListener(Update);
			}
		}

		public float DragTreshold
		{
			get => m_dragTreshold;
			set
			{
				m_dragTreshold = value;
				m_dragTresholdSqr = value * value;
			}
		}

		public bool HasUnboundKeys
		{
			get
			{
				foreach (var kv in m_keyBindings)
					if (kv.Value.KeyCode == KeyCode.None)
						return true;

				return false;
			}
		}

		public bool GetKey( KeyBinding _originalKeyBinding )
		{
			KeyBinding binding = ResolveKey(_originalKeyBinding);
			return IsPressed(binding);
		}

		public bool GetKeyDown( KeyBinding _original )
		{
			KeyBinding resolved = ResolveKey(_original);
			return IsPressedDown(resolved);
		}

		public bool GetKeyUp( KeyBinding _original )
		{
			KeyBinding resolved = ResolveKey(_original);
			return IsPressedUp(resolved);
		}

		public KeyBinding ResolveKey( KeyBinding _originalKeyBinding ) =>
			m_keyBindings.GetValueOrDefault(_originalKeyBinding.Encoded, _originalKeyBinding);

		public void AddKeyUpListener( KeyBinding _originalKeyBinding, UnityAction _callback ) =>
			AddKeyListener(_originalKeyBinding, KeyListenerType.Up, _callback);
		public void AddKeyDownListener( KeyBinding _originalKeyBinding, UnityAction _callback ) =>
			AddKeyListener(_originalKeyBinding, KeyListenerType.Down, _callback);
		public void AddKeyPressedListener( KeyBinding _originalKeyBinding, UnityAction _callback ) =>
			AddKeyListener(_originalKeyBinding, KeyListenerType.Pressed, _callback);

		public void RemoveKeyUpListener( KeyBinding _originalKeyBinding, UnityAction _callback ) =>
			RemoveKeyListener(_originalKeyBinding, KeyListenerType.Up, _callback);
		public void RemoveKeyDownListener( KeyBinding _originalKeyBinding, UnityAction _callback ) =>
			RemoveKeyListener(_originalKeyBinding, KeyListenerType.Down, _callback);
		public void RemoveKeyPressedListener( KeyBinding _originalKeyBinding, UnityAction _callback ) =>
			RemoveKeyListener(_originalKeyBinding, KeyListenerType.Pressed, _callback);

		public (KeyBinding originalKeyBinding, KeyListenerType type, UnityAction callback)[]
			AddKeyListeners( params (KeyBinding originalKeyBinding, KeyListenerType type, UnityAction callback)[] _listenerDefinitions )
		{
			foreach (var listenerDefinition in _listenerDefinitions)
				AddKeyListener(listenerDefinition.originalKeyBinding, listenerDefinition.type, listenerDefinition.callback);

			return _listenerDefinitions;
		}

		public void RemoveKeyListeners( params (KeyBinding originalKeyBinding, KeyListenerType type, UnityAction callback)[] _listenerDefinitions )
		{
			foreach (var listenerDefinition in _listenerDefinitions)
				RemoveKeyListener(listenerDefinition.originalKeyBinding, listenerDefinition.type, listenerDefinition.callback);
		}

		public bool AddKeyListener( KeyBinding _originalKeyBinding, KeyListenerType _type, UnityAction _callback )
		{
			if (!m_keyBindingPlayerSettings.TryGetValue(_originalKeyBinding.Encoded, out var playerSetting))
			{
				UiLog.LogWarning($"Attempt to add listener to unknown key binding {_originalKeyBinding}");
				return false;
			}

			switch (_type)
			{
				case KeyListenerType.Up:
					playerSetting.OnKeyUp.AddListener(_callback);
					break;
				case KeyListenerType.Down:
					playerSetting.OnKeyDown.AddListener(_callback);
					break;
				case KeyListenerType.Pressed:
					playerSetting.WhileKey.AddListener(_callback);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_type), _type, null);
			}

			return true;
		}

		public void RemoveKeyListener( KeyBinding _originalKeyBinding, KeyListenerType _type, UnityAction _callback )
		{
			if (!m_keyBindingPlayerSettings.TryGetValue(_originalKeyBinding.Encoded, out var playerSetting))
			{
				UiLog.LogWarning($"Attempt to add listener to unknown key binding {_originalKeyBinding}");
				return;
			}

			switch (_type)
			{
				case KeyListenerType.Up:
					playerSetting.OnKeyUp.RemoveListener(_callback);
					break;
				case KeyListenerType.Down:
					playerSetting.OnKeyDown.RemoveListener(_callback);
					break;
				case KeyListenerType.Pressed:
					playerSetting.WhileKey.RemoveListener(_callback);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_type), _type, null);
			}
		}

		private void InitializeInput( float _dragTreshold )
		{
			DragTreshold = _dragTreshold;

			UiEventDefinitions.OnTickPerFrame.RemoveListener(Update);
			if (!ManualUpdate)
				UiEventDefinitions.OnTickPerFrame.AddListener(Update);
		}

		private void ClearInput()
		{
			m_keyBindingPlayerSettings.Clear();
			m_keyBindings.Clear();
			m_activeKeyBindings.Clear();
			m_dragInfos.Clear();
		}

		private void RebuildKeyBindings()
		{
			m_keyBindings.Clear();

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting ps = kv.Value;
				if (!ps.IsKeyBinding)
					continue;

				KeyBinding original = ps.GetDefaultValue<KeyBinding>();

				if (m_keyBindings.ContainsKey(original.Encoded))
				{
					UiLog.LogError(
						$"Default KeyBinding '{original}' of player setting '{ps.Key}' already exists. " +
						"Each default key binding has to be unique.");
					continue;
				}

				KeyBinding bound = ps.GetValue<KeyBinding>();
				m_keyBindings.Add(original.Encoded, bound);
			}
		}

		private bool IsModifierActive( KeyBinding.EModifiers _mod )
		{
			if ((_mod & KeyBinding.EModifiers.Shift) != 0)
			{
				if (!InputProxy.GetKey(KeyCode.LeftShift) && !InputProxy.GetKey(KeyCode.RightShift))
					return false;
			}

			if ((_mod & KeyBinding.EModifiers.Ctrl) != 0)
			{
				if (!InputProxy.GetKey(KeyCode.LeftControl) && !InputProxy.GetKey(KeyCode.RightControl))
					return false;
			}

			if ((_mod & KeyBinding.EModifiers.Alt) != 0)
			{
				if (!InputProxy.GetKey(KeyCode.LeftAlt) && !InputProxy.GetKey(KeyCode.RightAlt))
					return false;
			}

			return true;
		}

		private bool IsPressed( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!IsModifierActive(_binding.Modifiers))
				return false;

			return InputProxy.GetKey(_binding.KeyCode);
		}

		private bool IsPressedDown( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!IsModifierActive(_binding.Modifiers))
				return false;

			return InputProxy.GetKeyDown(_binding.KeyCode);
		}

		private bool IsPressedUp( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			// Key went up?
			if (InputProxy.GetKeyUp(_binding.KeyCode))
				return true;

			// Modifier went up?
			if ((_binding.Modifiers & KeyBinding.EModifiers.Shift) != 0)
				if (InputProxy.GetKeyUp(KeyCode.LeftShift) || InputProxy.GetKeyUp(KeyCode.RightShift))
					return true;

			if ((_binding.Modifiers & KeyBinding.EModifiers.Ctrl) != 0)
				if (InputProxy.GetKeyUp(KeyCode.LeftControl) || InputProxy.GetKeyUp(KeyCode.RightControl))
					return true;

			if ((_binding.Modifiers & KeyBinding.EModifiers.Alt) != 0)
				if (InputProxy.GetKeyUp(KeyCode.LeftAlt) || InputProxy.GetKeyUp(KeyCode.RightAlt))
					return true;

			return false;
		}

		private void HandleKeyBindings( PlayerSetting _playerSetting )
		{
			KeyBinding original = _playerSetting.GetDefaultValue<KeyBinding>();
			KeyBinding bound = _playerSetting.GetValue<KeyBinding>();

			Debug.Assert(m_keyBindings.ContainsKey(original.Encoded));
			m_keyBindings[original.Encoded] = bound;

			if (bound.KeyCode == KeyCode.None)
				return;

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting ps = kv.Value;
				if (!ps.IsKeyBinding)
					continue;

				KeyBinding currOriginal = ps.GetDefaultValue<KeyBinding>();
				if (currOriginal == original)
					continue;

				KeyBinding currBound = ps.GetValue<KeyBinding>();

				// A) Exact conflict: same key+mods already used
				if (currBound == bound)
				{
					ps.Value = new KeyBinding(KeyCode.None);
					continue;
				}

				// B) New binding uses modifiers -> kick out single-key bindings that use modifier keys as primary
				if (bound.HasKeycodeAsModifier(currBound.KeyCode))
				{
					ps.Value = new KeyBinding(KeyCode.None);
					continue;
				}

				// C) New binding is a standalone modifier key -> kick out all bindings which use it as a modifier key
				if (currBound.HasKeycodeAsModifier(bound.KeyCode))
				{
					ps.Value = new KeyBinding(KeyCode.None);
					continue;
				}
			}
		}

		internal void Update( int _ )
		{
			if (!IsLoaded)
				return;

			Vector3 currMousePosition = InputProxy.MousePosition;

			foreach (var kv in m_dragInfos)
			{
				DragInfo dragInfo = kv.Value;

				if (!dragInfo.MeasuringDistance)
					continue;

				var distance = (dragInfo.StartPosition - currMousePosition).sqrMagnitude;
				if (distance > m_dragTresholdSqr)
				{
					dragInfo.MeasuringDistance = false;
					dragInfo.Active = true;
					dragInfo.PlayerSetting.OnBeginDrag.Invoke(dragInfo.StartPosition, currMousePosition);
				}
			}

			foreach (var kv in m_keyBindingPlayerSettings)
			{
				var playerSetting = kv.Value;
				KeyBinding binding = playerSetting.GetValue<KeyBinding>();
				int bindingKey = binding.Encoded;
				m_dragInfos.TryGetValue(bindingKey, out DragInfo dragInfo);
				bool isDragging = dragInfo?.Active ?? false;

				if (m_activeKeyBindings.Contains(bindingKey))
				{
					if (IsPressedUp(binding))
					{
						playerSetting.OnKeyUp.Invoke();

						if (isDragging)
							playerSetting.OnEndDrag.Invoke(dragInfo.StartPosition, currMousePosition);
						else
							playerSetting.OnClick.Invoke();

						m_dragInfos.Remove(bindingKey);
						m_activeKeyBindings.Remove(bindingKey);
						continue;
					}

					playerSetting.WhileKey.Invoke();
					if (isDragging)
						playerSetting.WhileDrag.Invoke(dragInfo.StartPosition, currMousePosition);

					continue;
				}

				if (IsPressedDown(binding))
				{
					playerSetting.OnKeyDown.Invoke();
					m_activeKeyBindings.Add(binding.Encoded);

					if (playerSetting.SupportDrag)
					{
						m_dragInfos.Add(bindingKey, new DragInfo()
						{
							MeasuringDistance = true,
							Active = false,
							PlayerSetting = playerSetting,
							StartPosition = currMousePosition
						});
					}
				}
			}
		}

	}
}