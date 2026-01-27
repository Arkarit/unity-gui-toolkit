using System;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public sealed class KeyBinding : LocaClass
	{
		[Flags]
		public enum EModifiers
		{
			None = 0,
			Shift = 1 << 0,
			Ctrl = 1 << 1,
			Alt = 1 << 2,
		}

		private const int KeyCodeBits = 24;
		private const int ModifiersShift = KeyCodeBits;

		private const uint KeyCodeMask = (1u << KeyCodeBits) - 1u; // 0x00FFFFFF
		private const uint ModifiersMask = 0xFFu;                  // plenty (we use only 3 bits)

		[SerializeField] private int m_encoded;

		public KeyBinding( KeyCode _keyCode = KeyCode.None, EModifiers _modifiers = EModifiers.None )
		{
			m_encoded = Encode(_keyCode, _modifiers);
		}
		public KeyBinding( int _encoded )
		{
			m_encoded = _encoded;
		}

		public int Encoded
		{
			get => m_encoded;
			set => m_encoded = value;
		}

		public KeyCode KeyCode
		{
			get
			{
				Decode(m_encoded, out KeyCode keyCode, out EModifiers _);
				return keyCode;
			}
			set
			{
				Decode(m_encoded, out KeyCode _, out EModifiers modifiers);
				m_encoded = Encode(value, modifiers);
			}
		}

		public EModifiers Modifiers
		{
			get
			{
				Decode(m_encoded, out KeyCode _, out EModifiers modifiers);
				return modifiers;
			}
			set
			{
				Decode(m_encoded, out KeyCode keyCode, out EModifiers _);
				m_encoded = Encode(keyCode, value);
			}
		}

		public static int Encode( KeyCode _keyCode, EModifiers _modifiers )
		{
			uint keyPart = (uint)(int)_keyCode & KeyCodeMask;
			uint modPart = ((uint)(int)_modifiers & ModifiersMask) << ModifiersShift;

			uint encoded = modPart | keyPart;
			return unchecked((int)encoded);
		}

		public static void Decode( int _encoded, out KeyCode _keyCode, out EModifiers _modifiers )
		{
			uint encoded = unchecked((uint)_encoded);

			uint keyPart = encoded & KeyCodeMask;
			uint modPart = (encoded >> ModifiersShift) & ModifiersMask;

			_keyCode = (KeyCode)(int)keyPart;
			_modifiers = (EModifiers)(int)modPart;
		}

		public bool Equals( KeyBinding _other )
		{
			if (ReferenceEquals(_other, null))
				return false;

			if (ReferenceEquals(this, _other))
				return true;

			return m_encoded == _other.Encoded;
		}

		public bool HasKeycodeAsModifier( KeyCode _kc )
		{
			return (KeyCodeToModifiers(_kc) & Modifiers) != 0;
		}

		public static EModifiers KeyCodeToModifiers( KeyCode _keyCode )
		{
			switch (_keyCode)
			{
				case KeyCode.LeftShift:
				case KeyCode.RightShift:
					return EModifiers.Shift;

				case KeyCode.LeftControl:
				case KeyCode.RightControl:
					return EModifiers.Ctrl;

				case KeyCode.LeftAlt:
				case KeyCode.RightAlt:
					return EModifiers.Alt;

				default: return EModifiers.None;
			}
		}
		
		public static bool operator ==( KeyBinding _a, KeyBinding _b )
		{
			if (ReferenceEquals(_a, null))
				return ReferenceEquals(_b, null);

			return _a.Equals(_b);
		}

		public static bool operator !=( KeyBinding _a, KeyBinding _b )
		{
			return !(_a == _b);
		}

		public override string ToString()
		{
			Decode(m_encoded, out KeyCode keyCode, out EModifiers modifiers);

			if (keyCode == KeyCode.None)
			{
				return _("None");
			}

			if (modifiers == EModifiers.None)
			{
				return keyCode.ToString();
			}

			return $"{modifiers}+{keyCode}";
		}
	}
}
