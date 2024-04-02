using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// \brief
/// 
/// BoolStack is a stack of bools (stack depth max 63)
/// It's main purpose is to keep track of multiple enable/disable actions
/// Note: This surely would better be implemented as struct, but shitty C# prevents this by its absurd struct constructor rules
[Serializable]
public class BoolStack
{
	private const int MAX_BIT = 63;
	[SerializeField] private ulong m_val = 0;
	[SerializeField] private int m_bit = -1;

	public bool Empty => m_bit < 0;
	public bool Full => m_bit >= MAX_BIT;
	public bool AnyBitSet => m_val != 0;
	public bool AnyBitCleared
	{
		get
		{
			ulong val = ~(ulong.MaxValue & ~m_val);
			return val != 0;
		}
	}

	public void Push(bool _val)
	{
		if (m_bit >= MAX_BIT)
			throw new Exception("Stack overflow in BoolStack");
		m_bit++;

		if (_val)
			m_val |= 1ul << m_bit;
		else
			m_val &= ~(1ul << m_bit);
	}

	public bool Pop()
	{
		if (m_bit < 0)
			throw new Exception("Stack underflow in BoolStack");
		ulong v = m_val & (1ul << m_bit);
		m_bit--;
		return v != 0;
	}

	public void Clear()
	{
		m_val = 0ul;
		m_bit = -1;
	}

	public static implicit operator BoolStack(bool _val)
	{
		BoolStack result = new BoolStack();
		result.Push(_val);
		return result;
	}
}
