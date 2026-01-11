using System;
using System.Collections.Generic;
using System.Reflection;
using GuiToolkit;
using UnityEngine;

public class DisplayIcons : MonoBehaviour
{
	public RectTransform m_container;
	public GameObject m_prefab;

	private void OnEnable()
	{
		m_container.DestroyAllChildren();

		var iconDefinitions = EnumerateNamed();
		foreach (var kv in iconDefinitions)
		{
			var go = Instantiate(m_prefab, m_container);
			var uiButton = go.GetComponent<UiButton>();
			var sprite = Resources.Load<Sprite>(kv.Value);
			uiButton.Image.sprite = sprite;
		}
	}

	public static IReadOnlyDictionary<string, string> EnumerateNamed()
	{
		Dictionary<string, string> result = new();

		FieldInfo[] fields = typeof(BuiltinIcons)
			.GetFields(BindingFlags.Public | BindingFlags.Static);

		foreach (FieldInfo field in fields)
		{
			if (field.FieldType == typeof(string))
			{
				result.Add(field.Name, (string)field.GetValue(null));
			}
		}

		return result;
	}

}
