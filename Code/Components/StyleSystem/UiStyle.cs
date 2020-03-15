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

		public readonly List<ComponentMemberInfo> m_styles = new List<ComponentMemberInfo>();

#if UNITY_EDITOR
		public readonly List<MemberInfoBools> m_memberInfosToApply = new List<MemberInfoBools>();
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiStyle))]
	public class UiStyleEditor : Editor
	{
		private static bool s_drawDefaultInspector = false;

		private readonly Dictionary<Type, UiStyle.MemberInfoBools> m_memberInfosToApplyDict = new Dictionary<Type, UiStyle.MemberInfoBools>();
		private readonly Dictionary<Type, EditorComponentMemberInfo> m_styleInfoDict = new Dictionary<Type, EditorComponentMemberInfo>();

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

				EditorComponentMemberInfo styleInfo = monoBehaviour.GetEditorComponentMemberInfo();
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
				EditorComponentMemberInfo styleInfo = m_styleInfoDict[type];

				mib.Used = GUILayout.Toggle(mib.Used, new GUIContent(ObjectNames.NicifyVariableName(mib.Type.Name)));
				
				if (mib.Used)
				{
					int len = mib.ToApply.Length;
					Debug.Assert( len == styleInfo.MemberInfos.Count);
					if (len != styleInfo.MemberInfos.Count)
						continue;

					for (int i=0; i<len; i++)
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

			int oldCount = thisUiStyle.m_styles.Count;

			thisUiStyle.m_styles.Clear();
			foreach( var memberInfoToApply in m_memberInfosToApplyDict)
			{
				Type type = memberInfoToApply.Key;
				UiStyle.MemberInfoBools mib = memberInfoToApply.Value;
				EditorComponentMemberInfo styleInfo = m_styleInfoDict[type];

				ComponentMemberInfo finalStyleInfo = new ComponentMemberInfo(styleInfo.Component);

				if (mib.Used)
				{
					int len = mib.ToApply.Length;
					Debug.Assert( len == styleInfo.MemberInfos.Count);
					if (len != styleInfo.MemberInfos.Count)
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
						thisUiStyle.m_styles.Add(finalStyleInfo);
					}
				}
			}

			if (thisUiStyle.m_styles.Count != oldCount)
				EditorUtility.SetDirty(target);
		}
	}
#endif
}