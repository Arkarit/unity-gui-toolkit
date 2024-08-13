using TMPro;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomPropertyDrawer(typeof(ApplicableValueBase), true)]
	public class ApplicableValueBaseDrawer : PropertyDrawer
	{
		private const int ToggleWidth = 25;

		public enum EDrawCondition
		{
			Always,
			OnlyDisabled,
			OnlyEnabled,
		}

		private static EDrawCondition s_drawCondition;

		public static EDrawCondition DrawCondition
		{
			get => s_drawCondition;
			set => s_drawCondition = value;
		}

		public override void OnGUI( Rect _position, SerializedProperty _property, GUIContent _label )
		{
			EditorGUI.BeginProperty(_position, _label, _property);

			var isApplicableProp = _property.FindPropertyRelative("IsApplicable");
			bool isApplicable = isApplicableProp.boolValue;	

			var thisApplicableValueBase = _property.boxedValue as ApplicableValueBase;

			if (thisApplicableValueBase.ValueHasChildren == ETriState.Indeterminate)
				thisApplicableValueBase.ValueHasChildren = DoesValueHaveChildren(_property, thisApplicableValueBase) ? ETriState.True : ETriState.False;

			bool hasChildren = thisApplicableValueBase.ValueHasChildren == ETriState.True;

			if (isApplicableProp.boolValue && s_drawCondition == EDrawCondition.OnlyDisabled ||
			    !isApplicableProp.boolValue && s_drawCondition == EDrawCondition.OnlyEnabled)
			{
				EditorGUI.EndProperty();
				return;
			}

			EditorGUI.BeginChangeCheck();
			var newIsApplicable = EditorGUI.Toggle(new Rect(_position.x, _position.y, ToggleWidth, EditorGUIUtility.singleLineHeight), isApplicableProp.boolValue);
			if (EditorGUI.EndChangeCheck())
			{
				isApplicableProp.boolValue = newIsApplicable;
			}

			EditorGUI.LabelField(new Rect(_position.x + ToggleWidth, _position.y, EditorGUIUtility.labelWidth - ToggleWidth, EditorGUIUtility.singleLineHeight), _label);

			if (!isApplicable)
			{
				EditorGUI.EndProperty();
				return;
			}

			var valueProp = _property.FindPropertyRelative("Value");
			using (new EditorGUI.DisabledScope(newIsApplicable == false))
			{
				EditorGUI.PropertyField(
					new Rect(_position.x + EditorGUIUtility.labelWidth, _position.y, _position.width - EditorGUIUtility.labelWidth, _position.height), 
					valueProp, 
					new GUIContent(),
					valueProp.isExpanded && hasChildren
				);
			}

			EditorGUI.EndProperty();
		}

		private static bool DoesValueHaveChildren(SerializedProperty _property, ApplicableValueBase _thisApplicableValueBase)
		{
			if (!_thisApplicableValueBase.IsApplicable)
				return false;

			var valueObj = _thisApplicableValueBase.ValueObj;
			if (valueObj == null)
				return false;

			var type = valueObj.GetType();
			if (type.IsPrimitive)
				return false;
			if (type.IsEnum) 
				return false;

			switch (valueObj)
			{
				case Color c:
				case string s:
				case Quaternion q:
				case Vector2 v2:
				case Vector2Int v2i:
				case Vector3 v3:
				case Vector3Int v3i:
				case Material:
					return false;
			}

			return true;
		}

		public override float GetPropertyHeight(SerializedProperty _property, GUIContent label)
		{
			var isApplicableProp = _property.FindPropertyRelative("IsApplicable");
			bool isApplicable = isApplicableProp.boolValue;	
			if (isApplicable && s_drawCondition == EDrawCondition.OnlyDisabled ||
			    !isApplicable && s_drawCondition == EDrawCondition.OnlyEnabled)
			{
				return 0;
			}

			if (!isApplicable)
				return EditorGUIUtility.singleLineHeight;

			var valueProp = _property.FindPropertyRelative("Value");
			var thisApplicableValueBase = _property.boxedValue as ApplicableValueBase;
			return EditorGUI.GetPropertyHeight(valueProp, DoesValueHaveChildren(_property, thisApplicableValueBase));
		}
	}
}