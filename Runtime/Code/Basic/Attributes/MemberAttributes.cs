using System;
using System.Reflection;
using GuiToolkit;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Marks an Object field that must be assigned externally (i.e., in a context where this component is instantiated).
/// In the prefab asset itself no error is shown (only an info), because the reference cannot be set there.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class MandatoryExternalAttribute : PropertyAttribute
{
	/// <summary>
	/// Optional type name of a "context" component to look up in parents; helps the drawer offer Ping/Select.
	/// Provide short type name or assembly-qualified; first match wins.
	/// </summary>
	public readonly string ContextTypeName;

	/// <summary>
	/// Optional friendly name for the context shown in the help box.
	/// </summary>
	public readonly string ContextNameHint;

	/// <summary>
	/// If true, the validator will log to the console when violations are found.
	/// </summary>
	public readonly bool LogErrorInConsole;

	public MandatoryExternalAttribute( string contextTypeName = null, string contextNameHint = null, bool logErrorInConsole = true )
	{
		ContextTypeName = contextTypeName;
		ContextNameHint = contextNameHint;
		LogErrorInConsole = logErrorInConsole;
	}
}

/// <summary>
/// Marks an Object field as mandatory. Must be assigned in *any* context (Prefab, Prefab Instance, Scene).
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class MandatoryAttribute : PropertyAttribute
{
    public readonly string ContextNameHint;

    public MandatoryAttribute(string contextNameHint = null)
    {
        ContextNameHint = contextNameHint;
    }
}

/// <summary>
/// Marks an Object field as explicitly optional (draws a subtle info box if null).
/// Mostly for documentation in Inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class OptionalAttribute : PropertyAttribute
{
    public readonly string ContextNameHint;

    public OptionalAttribute(string contextNameHint = null)
    {
        ContextNameHint = contextNameHint;
    }
}


#if UNITY_EDITOR

[CustomPropertyDrawer(typeof(MandatoryExternalAttribute))]
public sealed class MandatoryExternalDrawer : PropertyDrawer
{
	public override float GetPropertyHeight( SerializedProperty property, GUIContent label )
	{
		float h = EditorGUI.GetPropertyHeight(property, label, true);
		if (!IsObjectRef(property)) return h;

		// Determine context (asset vs instance) and null-status
		var target = property.serializedObject.targetObject;
		bool isAsset = IsPrefabAssetOrEditingContents(target);
		bool needs = NeedsExternalAssignment(property, out bool isNull);
		if (!needs) return h; // not an object field

		if (isNull)
		{
			// Error if instance; Info if asset
			if (isAsset)
				h += EditorGUIUtility.singleLineHeight * 2.0f + 8f;
			else
				h += EditorGUIUtility.singleLineHeight * 3.0f + 12f;
		}
		return h;
	}

	public override void OnGUI( Rect position, SerializedProperty property, GUIContent label )
	{
		EditorGUI.BeginProperty(position, label, property);

		Rect fieldRect = position;
		float y = position.y;
		float line = EditorGUIUtility.singleLineHeight;

		bool isObjRef = IsObjectRef(property);
		bool isAsset = IsPrefabAssetOrEditingContents(property.serializedObject.targetObject);
		_ = NeedsExternalAssignment(property, out bool isNull);

		// Draw the field first
		float fieldHeight = EditorGUI.GetPropertyHeight(property, label, true);
		Rect helpRect = new Rect(position.x, y, position.width, 0f);
		if (isObjRef && isNull)
		{
			// Reserve help space
			if (isAsset)
				helpRect.height = line * 2.0f + 8f;
			else
				helpRect.height = line * 3.0f + 12f;
			// Help above field
			y += helpRect.height;
		}

		if (isObjRef)
		{
			// Help box (if any)
			if (isAsset)
			{
				EditorGUI.HelpBox(helpRect,
					$"The Field '{label.text}' is meant to be assigned externally e.g. in a screen or higher-level prefab. It Is always null here.",
					MessageType.Info);

				if (property.objectReferenceValue != null) // guard to avoid undo/dirty spam
				{
					property.objectReferenceValue = null;
					property.serializedObject.ApplyModifiedProperties();
				}
			}
			else if (isNull)
			{
				string contextName = (attribute as MandatoryExternalAttribute)?.ContextNameHint;
				bool hasContext = string.IsNullOrEmpty(contextName);
				string contextLabel = hasContext ? contextName : "external context";

				// Try find context component in parents (if type provided)
				UnityEngine.Object ctx = FindContextObject(property);
				string msg = $"The field '{label.text}' needs to be set here, or in an even higher-level prefab or screen.\nIf the latter applies, you can ignore this error.";
				EditorGUI.HelpBox(helpRect, msg, MessageType.Error);

				// Buttons row
				Rect row = new Rect(helpRect.x + 6f, helpRect.yMax - (line + 4f), helpRect.width - 12f, line);
				float bw = 110f;
				if (ctx)
				{
					Rect b1 = new Rect(row.x, row.y, bw, line);
					if (GUI.Button(b1, "Ping Context"))
						EditorGUIUtility.PingObject(ctx);

					Rect b2 = new Rect(row.x + bw + 8f, row.y, bw, line);
					if (GUI.Button(b2, "Select Context"))
						Selection.activeObject = ctx;
				}
			}
		}

		fieldRect.y = y;
		fieldRect.height = fieldHeight;
		using (new EditorGUI.DisabledScope(isAsset))
		{
			EditorGUI.PropertyField(fieldRect, property, label, true);
		}

		EditorGUI.EndProperty();
	}

	private static bool IsObjectRef( SerializedProperty p )
	{
		return p.propertyType == SerializedPropertyType.ObjectReference;
	}

	private static bool NeedsExternalAssignment( SerializedProperty p, out bool isNull )
	{
		isNull = p.objectReferenceValue == null;
		return IsObjectRef(p);
	}

	private static bool IsPrefabAssetOrEditingContents( Object target )
	{
		if (!target) return false;

		// True for pure prefab assets (Project window selection etc.)
		if (PrefabUtility.IsPartOfPrefabAsset(target))
			return true;

		// True when currently editing this prefab in Prefab Mode
		if (IsEditingPrefabContents(target))
			return true;

		return false;
	}

	private static bool IsEditingPrefabContents( Object target )
	{
		if (!target) return false;

		// Normalize to GameObject
		if (target is Component c) target = c.gameObject;
		var go = target as GameObject;
		if (!go) return false;

		// Returns non-null if 'go' lives inside an open Prefab Stage (Prefab Mode)
		var stage = PrefabStageUtility.GetPrefabStage(go);
		return stage != null;
	}

	private UnityEngine.Object FindContextObject( SerializedProperty property )
	{
		var attr = (MandatoryExternalAttribute)attribute;
		if (string.IsNullOrEmpty(attr.ContextTypeName)) return null;

		var target = property.serializedObject.targetObject as Component;
		if (!target) return null;

		var t = ResolveType(attr.ContextTypeName);
		if (t == null) return null;

		Transform current = target.transform;
		while (current)
		{
			var comp = current.GetComponent(t);
			if (comp) return comp;
			current = current.parent;
		}
		return null;
	}

	private static Type ResolveType( string typeName )
	{
		// Try by full name
		var t = Type.GetType(typeName);
		if (t != null) return t;

		// Try by short name across loaded assemblies
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			try
			{
				foreach (var tt in asm.GetTypes())
				{
					if (tt.Name == typeName) return tt;
				}
			}
			catch { /* ignore */ }
		}
		return null;
	}
}

[InitializeOnLoad]
public static class ExternalRequiredValidator
{
	static ExternalRequiredValidator()
	{
		EditorApplication.playModeStateChanged += OnPlayModeChanged;
	}

	private static void OnPlayModeChanged( PlayModeStateChange state )
	{
		if (state == PlayModeStateChange.ExitingEditMode)
		{
			ValidateOpenScenes(logToConsole: true);
		}
	}

	[MenuItem("Tools/Validation/Check ExternalRequired in Open Scenes", priority = 5000)]
	private static void ValidateMenu()
	{
		ValidateOpenScenes(logToConsole: true);
	}

	/// <summary>
	/// Scans all open scenes for components that contain fields marked with ExternalRequiredAttribute and logs errors if null.
	/// Prefab assets are ignored; only scene objects and prefab instances are considered.
	/// </summary>
	public static int ValidateOpenScenes( bool logToConsole )
	{
		int violations = 0;

		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			var s = SceneManager.GetSceneAt(i);
			if (!s.isLoaded) continue;

			foreach (var root in s.GetRootGameObjects())
			{
				var comps = root.GetComponentsInChildren<Component>(true);
				foreach (var c in comps)
				{
					if (!c) continue;
					if (PrefabUtility.IsPartOfPrefabAsset(c)) continue;

					var so = new SerializedObject(c);
					var sp = so.GetIterator();
					if (sp.NextVisible(true))
					{
						do
						{
							if (sp.propertyType != SerializedPropertyType.ObjectReference) continue;

							var field = GetFieldInfo(c.GetType(), sp.name);
							if (field == null) continue;

							var attr = (MandatoryExternalAttribute)Attribute.GetCustomAttribute(field, typeof(MandatoryExternalAttribute));
							if (attr == null) continue;

							if (sp.objectReferenceValue == null)
							{
								violations++;
								if (logToConsole && attr.LogErrorInConsole)
								{
									string compName = c.GetType().Name;
									string fieldName = field.Name;

									// Context hint
									string ctx = string.IsNullOrEmpty(attr.ContextNameHint) ? "external context" : attr.ContextNameHint;
									Debug.LogError(
										$"ExternalRequired violation: <{compName}.{fieldName}> is null. " +
										$"This reference must be assigned in {ctx}.",
										c
									);
								}
							}
						}
						while (sp.NextVisible(false));
					}
				}
			}
		}

		if (logToConsole)
		{
			if (violations == 0)
				Debug.Log("ExternalRequired: No violations found in open scenes.");
			else
				Debug.Log($"ExternalRequired: Found {violations} violation(s) in open scenes.");
		}

		return violations;
	}

	private static FieldInfo GetFieldInfo( Type t, string serializedPropertyName )
	{
		// SerializedProperty.name equals the backing field name for fields (not properties).
		// Search up the inheritance chain.
		while (t != null)
		{
			var f = t.GetField(serializedPropertyName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			if (f != null) return f;
			t = t.BaseType;
		}
		return null;
	}
}

[CustomPropertyDrawer(typeof(MandatoryAttribute))]
public sealed class MandatoryDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUI.GetPropertyHeight(property, label, true);
        if (property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue == null)
        {
            h += EditorGUIUtility.singleLineHeight * 2 + 6f;
        }
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var attr = (MandatoryAttribute)attribute;
        Rect fieldRect = position;

        if (property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue == null)
        {
            var helpRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * 2);
            string ctx = string.IsNullOrEmpty(attr.ContextNameHint) ? "mandatory" : attr.ContextNameHint;
            EditorGUI.HelpBox(helpRect, $"Field '{label.text}' is mandatory: must be assigned ({ctx}).", MessageType.Error);

            fieldRect.y += helpRect.height + 2f;
        }

        EditorGUI.PropertyField(fieldRect, property, label, true);

        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(OptionalAttribute))]
public sealed class OptionalDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUI.GetPropertyHeight(property, label, true);
        if (property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue == null)
        {
            h += EditorGUIUtility.singleLineHeight + 4f;
        }
        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var attr = (OptionalAttribute)attribute;
        Rect fieldRect = position;

        if (property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue == null)
        {
            var helpRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            string ctx = string.IsNullOrEmpty(attr.ContextNameHint) ? "optional" : attr.ContextNameHint;
            EditorGUI.HelpBox(helpRect, $"Field '{label.text}' is optional (currently not assigned, {ctx}).", MessageType.Info);
            fieldRect.y += helpRect.height + 2f;
        }

        EditorGUI.PropertyField(fieldRect, property, label, true);

        EditorGUI.EndProperty();
    }
}
#endif
