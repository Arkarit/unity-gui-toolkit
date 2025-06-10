using System;
using UnityEngine;

namespace GuiToolkit.Debugging
{
	[ExecuteAlways]
	public class WhatEnablesMe : MonoBehaviour
	{
		[Flags]
		public enum Features
		{
			OnEnable							= 0x0001,
			OnDisable							= 0x0002,
			Awake								= 0x0004,
			OnDestroy							= 0x0008,
			OnBeforeTransformParentChanged		= 0x0010,
			OnTransformParentChanged			= 0x0020,
			OnTransformChildrenChanged			= 0x0040,
			Start								= 0x0080,
		}

		[SerializeField] private bool m_executeInEditor;
		[SerializeField] private Features m_features;
		[SerializeField] private string m_prefix = "WhatEnablesMe:";


		private void Start() => Log(Features.Start);
		private void OnEnable() => Log(Features.OnEnable);
		private void OnDisable() => Log(Features.OnDisable);
		private void Awake() => Log(Features.Awake);
		private void OnDestroy() => Log(Features.OnDestroy);
		private void OnBeforeTransformParentChanged() => Log(Features.OnBeforeTransformParentChanged);
		private void OnTransformParentChanged() => Log(Features.OnTransformParentChanged);
		private void OnTransformChildrenChanged() => Log(Features.OnTransformChildrenChanged);

		private void Log(Features _feature)
		{
			if (!m_executeInEditor && !Application.isPlaying)
				return;

			if ((m_features & _feature) == 0)
				return;

			string children = "Children:";
			foreach (Transform child in transform)
				children += $"'{child.GetPath(1)}' ";
			Debug.Log($"{m_prefix} '{gameObject.GetPath(1)}' {_feature}\n" + 
			          $"Path: '{gameObject.GetPath()}'\n" + 
			          $"Parent: '{transform.parent.GetPath()}'\n" +
			          $"{children}\n" +
			          "\n");
		}
	}
}