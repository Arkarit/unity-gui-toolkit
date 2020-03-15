using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
			public Type Type;
			public bool[] ToApply;
			public bool Used;
		}
#endif

		public readonly List<StyleInfo> m_styles = new List<StyleInfo>();

#if UNITY_EDITOR
		public readonly List<MemberInfoBools> m_memberInfosToApply = new List<MemberInfoBools>();
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiStyle))]
	public class UiStyleEditor : Editor
	{
		private readonly Dictionary<Type, UiStyle.MemberInfoBools> m_memberInfosToApplyDict = new Dictionary<Type, UiStyle.MemberInfoBools>();
		private readonly Dictionary<Type, StyleInfo> m_styleInfoDict = new Dictionary<Type, StyleInfo>();

		public override void OnInspectorGUI()
		{
			UiStyle thisUiStyle = (UiStyle)target;

			m_memberInfosToApplyDict.Clear();
			foreach( var mib in thisUiStyle.m_memberInfosToApply)
				m_memberInfosToApplyDict.Add(mib.Type, mib);

			m_styleInfoDict.Clear();
			Component[] components = thisUiStyle.GetComponents<Component>();
			foreach( var monoBehaviour in components )
			{
				if (monoBehaviour == thisUiStyle)
					continue;

				StyleInfo styleInfo = monoBehaviour.GetStyleInfo();
				m_styleInfoDict.Add(styleInfo.ComponentType, styleInfo);

				if (!m_memberInfosToApplyDict.ContainsKey(styleInfo.ComponentType))
				{
					UiStyle.MemberInfoBools mib = new UiStyle.MemberInfoBools { Type = styleInfo.ComponentType, ToApply = new bool[styleInfo.MemberInfos.Count]};
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

				mib.Used = GUILayout.Toggle(mib.Used, new GUIContent(mib.Type.Name));
				
				if (mib.Used)
				{
					GUILayout.Label("Lorem");
				}
			}

			thisUiStyle.m_memberInfosToApply.Clear();
			foreach( var memberInfoToApply in m_memberInfosToApplyDict)
				thisUiStyle.m_memberInfosToApply.Add(memberInfoToApply.Value);





//			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}