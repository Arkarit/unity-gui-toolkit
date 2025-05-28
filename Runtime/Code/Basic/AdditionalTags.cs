using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class AdditionalTags : MonoBehaviour
	{
		[SerializeField] private List<TagField> m_Tags = new ();
		
		private readonly HashSet<string> m_HashSetTags = new();

		private void Awake()
		{
			SetHashSetTags();
		}

		public bool CompareTag(string _tag)
		{
			if (gameObject.CompareTag(_tag))
				return true;
			
			return m_HashSetTags.Contains(_tag);
		}

		private void OnValidate()
		{
			SetHashSetTags();
		}

		private void SetHashSetTags()
		{
			m_HashSetTags.Clear();
			foreach (var tag in m_Tags)
				m_HashSetTags.Add(tag);
		}
	}
}
