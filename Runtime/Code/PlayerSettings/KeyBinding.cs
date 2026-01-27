using System;
using UnityEngine;

namespace GuiToolkit
{
    [Serializable]
    public sealed class KeyBinding
    {
        [Flags]
        public enum EModifiers
        {
            None = 0,
            Shift = 1 << 0,
            Ctrl  = 1 << 1,
            Alt   = 1 << 2,
        }

        private const int KeyCodeBits = 24;
        private const int ModifiersShift = KeyCodeBits;

        private const uint KeyCodeMask = (1u << KeyCodeBits) - 1u; // 0x00FFFFFF
        private const uint ModifiersMask = 0xFFu;                  // plenty (we use only 3 bits)

        [SerializeField] private int m_encoded;

        public int Encoded
        {
            get => m_encoded;
            set => m_encoded = value;
        }

        public KeyCode KeyCode
        {
            get
            {
                Decode(m_encoded, out KeyCode keyCode, out _);
                return keyCode;
            }
            set
            {
                Decode(m_encoded, out _, out EModifiers modifiers);
                m_encoded = Encode(value, modifiers);
            }
        }

        public EModifiers Modifiers
        {
            get
            {
                Decode(m_encoded, out _, out EModifiers modifiers);
                return modifiers;
            }
            set
            {
                Decode(m_encoded, out KeyCode keyCode, out _);
                m_encoded = Encode(keyCode, value);
            }
        }

        public static int Encode(KeyCode _keyCode, EModifiers _modifiers)
        {
            uint keyPart = (uint)(int)_keyCode & KeyCodeMask;
            uint modPart = ((uint)(int)_modifiers & ModifiersMask) << ModifiersShift;

            uint encoded = modPart | keyPart;
            return unchecked((int)encoded);
        }

        public static void Decode(int _encoded, out KeyCode _keyCode, out EModifiers _modifiers)
        {
            uint encoded = unchecked((uint)_encoded);

            uint keyPart = encoded & KeyCodeMask;
            uint modPart = (encoded >> ModifiersShift) & ModifiersMask;

            _keyCode = (KeyCode)(int)keyPart;
            _modifiers = (EModifiers)(int)modPart;
        }

        public override string ToString()
        {
            Decode(m_encoded, out KeyCode keyCode, out EModifiers modifiers);

            if (keyCode == KeyCode.None)
            {
                return "<Unbound>";
            }

            if (modifiers == EModifiers.None)
            {
                return keyCode.ToString();
            }

            return $"{modifiers}+{keyCode}";
        }
    }
}
