using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace GuiToolkit
{
	[Flags]
	public enum EDirectionFlags
	{
		Horizontal	= 01,
		Vertical	= 02,
	}

	public enum ESide
	{
		Top,
		Bottom,
		Left,
		Right
	}

	// Note: the lower the layer definition number, the higher (more occluding)
	// it is regarding the visibility
	public enum EUiLayerDefinition
	{
		Top = 200,
		Tooltip = 400,
		ModalStack = 600,
		Popup = 800,
		Dialog = 1000,
		Hud = 1200,
		Background = 1400,
		Back = 1600,
	}

	public enum DefaultSceneVisibility
	{
		DontCare,
		Visible,
		Invisible,
		VisibleInDevBuild,
		VisibleWhen_DEFAULT_SCENE_VISIBLE_defined,
	}

	public interface ISetDefaultSceneVisibility
	{
		void SetDefaultSceneVisibility();
	}

	public static class Constants
	{
		public const float HANDLE_SIZE = 0.06f;
		public static Color HANDLE_COLOR = Color.yellow;
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;
	}

	[Serializable]
	public class ComponentMemberInfo
	{
		public Component m_component;
		public List<string> m_names = new List<string>();
		public List<bool> m_isProperty = new List<bool>();

		public int Count => m_names == null ? 0 : m_names.Count;

		public ComponentMemberInfo(Component _component)
		{
			m_component = _component;
		}

#if UNITY_EDITOR
		public void Add(PropertyInfo _propertyInfo)
		{
			m_names.Add(_propertyInfo.Name);
			m_isProperty.Add(true);
		}

		public void Add(FieldInfo _fieldInfo)
		{
			m_names.Add(_fieldInfo.Name);
			m_isProperty.Add(false);
		}
#endif
	}

#if UNITY_EDITOR
	[Serializable]
	public class EditorComponentMemberInfo
	{
		public Component Component;
		public Type ComponentType;
		public List<PropertyInfo> PropertyInfos;
		public List<FieldInfo> FieldInfos;

		public int PropertyInfoCount => PropertyInfos != null ? PropertyInfos.Count : 0;
		public int FieldInfoCount => FieldInfos != null ? FieldInfos.Count : 0;
		public int CompleteCount => PropertyInfoCount + FieldInfoCount;

		public EditorComponentMemberInfo( Component _component, PropertyInfo[] _propertyInfos = null, FieldInfo[] _fieldInfos = null )
		{
			Component = _component;
			ComponentType = _component.GetType();
			PropertyInfos = new List<PropertyInfo>();
			FieldInfos = new List<FieldInfo>();

			if (_propertyInfos != null)
				PropertyInfos.AddRange(_propertyInfos);

			if (_fieldInfos != null)
				FieldInfos.AddRange(_fieldInfos);
		}
	}
#endif

}