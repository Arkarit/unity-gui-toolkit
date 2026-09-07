using System;
using UnityEngine;

namespace GuiToolkit.Style
{
	/// <summary>
	/// One style a skin deliberately does NOT have, although it would inherit it - the pendant to a prefab
	/// instance's removed component.
	///
	/// Identified by <see cref="Key"/> alone; the name and the component type are carried for the sake of
	/// anyone reading the asset or a log line, where a bare hash says nothing. They are a snapshot and are
	/// never matched against, so a later alias change cannot orphan a removal.
	///
	/// Note the vocabulary: the code says "suppress", because "remove" is one letter away from the
	/// deletion this is not. The UI says "Removed", because that is what Unity calls it.
	/// </summary>
	[Serializable]
	public class UiSuppressedStyle
	{
		[SerializeField] private int m_key;
		[SerializeField] private string m_name;
		[SerializeField] private string m_typeName;

		public UiSuppressedStyle( UiAbstractStyleBase _style )
		{
			if (_style == null)
				throw new ArgumentNullException(nameof(_style));

			m_key = _style.Key;
			m_name = _style.Name;
			m_typeName = _style.SupportedComponentType?.Name;
		}

		public int Key => m_key;
		public string Name => m_name;
		public string TypeName => m_typeName;

		/// <summary>
		/// How to name it in a message. The component type belongs in there: a style is identified by name
		/// AND type, so the same name may exist for another component and the bare name would read as a lie.
		/// </summary>
		public string Description => string.IsNullOrEmpty(m_typeName)
			? $"'{m_name}'"
			: $"'{m_name}' ({m_typeName})";
	}
}
