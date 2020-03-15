using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{

	public class UiStyle : MonoBehaviour
	{
#if UNITY_EDITOR
		[Serializable]
		public class MemberInfoBools
		{
			public Component Component;
			public bool[] ToApply;
			public bool Used;
		}
#endif

		public List<ComponentMemberInfo> m_componentInfos = new List<ComponentMemberInfo>();

		private Dictionary<Type, ComponentMemberInfo> m_componentDict = null;

		public void Apply(GameObject _gameObject)
		{
			InitDictIfNecessary();
			Component[] components = _gameObject.GetComponents<Component>();
			foreach (var component in components)
			{
				if (m_componentDict.ContainsKey(component.GetType()))
				{
					component.CopyValuesFrom(m_componentDict[component.GetType()]);
				}
			}
		}

		private void InitDictIfNecessary()
		{
			if (m_componentDict != null)
				return;
			m_componentDict = new Dictionary<Type, ComponentMemberInfo>();
			foreach (var componentInfo in m_componentInfos)
			{
				if (componentInfo != null && componentInfo.m_component != null)
					m_componentDict.Add(componentInfo.m_component.GetType(), componentInfo);
			}
		}

#if UNITY_EDITOR
		public List<MemberInfoBools> m_memberInfosToApply = new List<MemberInfoBools>();
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiStyle))]
	public class UiStyleEditor : Editor
	{
		private readonly Dictionary<Type, UiStyle.MemberInfoBools> m_memberInfosToApplyDict = new Dictionary<Type, UiStyle.MemberInfoBools>();
		private readonly Dictionary<Type, EditorComponentMemberInfo> m_styleInfoDict = new Dictionary<Type, EditorComponentMemberInfo>();

		public override void OnInspectorGUI()
		{
			UiStyle thisUiStyle = (UiStyle)target;

			m_memberInfosToApplyDict.Clear();
			foreach( var mib in thisUiStyle.m_memberInfosToApply)
			{
				if (mib != null && mib.Component != null)
					m_memberInfosToApplyDict.Add(mib.Component.GetType(), mib);
			}

			m_styleInfoDict.Clear();
			Component[] components = thisUiStyle.GetComponents<Component>();
			foreach( var monoBehaviour in components )
			{
				if (monoBehaviour == thisUiStyle)
					continue;

				EditorComponentMemberInfo styleInfo = monoBehaviour.GetEditorComponentMemberInfo();
				m_styleInfoDict.Add(styleInfo.ComponentType, styleInfo);

				if (!m_memberInfosToApplyDict.ContainsKey(styleInfo.ComponentType))
				{
					UiStyle.MemberInfoBools mib = new UiStyle.MemberInfoBools { Component = styleInfo.Component, ToApply = new bool[styleInfo.Count]};
					m_memberInfosToApplyDict.Add(styleInfo.ComponentType, mib);
				}
			}

			HashSet<Type> moribund = new HashSet<Type>();
			foreach( var memberInfoToApply in m_memberInfosToApplyDict)
				if (!m_styleInfoDict.ContainsKey(memberInfoToApply.Key))
					moribund.Add(memberInfoToApply.Key);

			foreach( var kill in moribund )
				m_memberInfosToApplyDict.Remove(kill);

			foreach( var memberInfoToApply in m_memberInfosToApplyDict)
			{
				Type type = memberInfoToApply.Key;
				UiStyle.MemberInfoBools mib = memberInfoToApply.Value;
				EditorComponentMemberInfo styleInfo = m_styleInfoDict[type];

				mib.Used = GUILayout.Toggle(mib.Used, new GUIContent(ObjectNames.NicifyVariableName(mib.Component.GetType().Name)));
				
				if (mib.Used)
				{
					int len = mib.ToApply.Length;

					Debug.Assert( len == styleInfo.Count);
					if (len != styleInfo.Count)
						continue;

					for (int i=0; i<styleInfo.Count; i++)
					{
						GUILayout.BeginHorizontal();
						GUILayout.Space(20);
						mib.ToApply[i] = GUILayout.Toggle(mib.ToApply[i], ObjectNames.NicifyVariableName(styleInfo.MemberInfos[i].Name));
						GUILayout.EndHorizontal();
					}
				}
			}

			thisUiStyle.m_memberInfosToApply.Clear();
			foreach( var memberInfoToApply in m_memberInfosToApplyDict)
				thisUiStyle.m_memberInfosToApply.Add(memberInfoToApply.Value);

			int oldCount = thisUiStyle.m_componentInfos.Count;

			thisUiStyle.m_componentInfos.Clear();
			foreach( var memberInfoToApply in m_memberInfosToApplyDict)
			{
				Type type = memberInfoToApply.Key;
				UiStyle.MemberInfoBools mib = memberInfoToApply.Value;
				EditorComponentMemberInfo styleInfo = m_styleInfoDict[type];

				ComponentMemberInfo finalStyleInfo = new ComponentMemberInfo(styleInfo.Component);

				if (mib.Used)
				{
					int len = mib.ToApply.Length;
					Debug.Assert( len == styleInfo.Count);
					if (len != styleInfo.Count)
						continue;

					for (int i=0; i<len; i++)
					{
						if (mib.ToApply[i])
						{
							finalStyleInfo.Add(styleInfo.MemberInfos[i]);
						}
					}

					if (finalStyleInfo.Count > 0)
					{
						thisUiStyle.m_componentInfos.Add(finalStyleInfo);
					}
				}
			}

			if (thisUiStyle.m_componentInfos.Count != oldCount)
				EditorUtility.SetDirty(target);
		}
	}
#endif
}