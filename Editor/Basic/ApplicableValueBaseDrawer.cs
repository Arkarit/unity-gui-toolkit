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
		
		public override void OnGUI( Rect _rect, SerializedProperty _property, GUIContent _label )
		{
			var screenPos = GUIUtility.GUIToScreenPoint(_rect.position);
			if (screenPos.y > Screen.height || screenPos.y + _rect.height < 0)
				return;
			
			EditorGUI.BeginProperty(_rect, _label, _property);

			var isApplicableProp = _property.FindPropertyRelative("IsApplicable");
			bool isApplicable = isApplicableProp.boolValue;	

			var thisApplicableValueBase = _property.boxedValue as ApplicableValueBase;

			if (thisApplicableValueBase.ValueHasChildren == ETriState.Indeterminate)
				thisApplicableValueBase.ValueHasChildren = DoesValueHaveChildren(_property, thisApplicableValueBase) ? ETriState.True : ETriState.False;

			bool hasChildren = thisApplicableValueBase.ValueHasChildren == ETriState.True;

			if (isApplicable && s_drawCondition == EDrawCondition.OnlyDisabled ||
			    !isApplicable && s_drawCondition == EDrawCondition.OnlyEnabled)
			{
				EditorGUI.EndProperty();
				return;
			}

			var newIsApplicable = EditorGUI.Toggle(new Rect(_rect.x, _rect.y, ToggleWidth, EditorGUIUtility.singleLineHeight), isApplicableProp.boolValue);
			if (newIsApplicable != isApplicable)
			{
				isApplicableProp.boolValue = newIsApplicable;
			}

			EditorGUI.LabelField(new Rect(_rect.x + ToggleWidth, _rect.y, EditorGUIUtility.labelWidth - ToggleWidth, EditorGUIUtility.singleLineHeight), _label);

			if (!isApplicable)
			{
				EditorGUI.EndProperty();
				return;
			}

			var valueProp = _property.FindPropertyRelative("m_value");
			using (new EditorGUI.DisabledScope(newIsApplicable == false))
			{
				var hashBefore = valueProp.contentHash;
				EditorGUI.PropertyField(
					new Rect(_rect.x + EditorGUIUtility.labelWidth, _rect.y, _rect.width - EditorGUIUtility.labelWidth, _rect.height), 
					valueProp, 
					new GUIContent(),
					valueProp.isExpanded && hasChildren
				);

				if (hashBefore != valueProp.contentHash)
					if (_property.boxedValue is ApplicableValueBase applicableValue)
						applicableValue.Touch();
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
				case Color:
				case string:
				case Material:
					return false;
				
				default:
					return true;
			}
		}

		private static float GetPropHeight(ApplicableValueBase _applicableValueBase, SerializedProperty _property) => EditorGUI.GetPropertyHeight(_property, DoesValueHaveChildren(_property, _applicableValueBase));
		
		public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
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

			var valueProp = _property.FindPropertyRelative("m_value");
			var thisApplicableValueBase = _property.boxedValue as ApplicableValueBase;
			return GetPropHeight(thisApplicableValueBase, valueProp);
		}
	}
}
