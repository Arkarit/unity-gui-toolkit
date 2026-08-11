#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiReferencedSizeFitter))]
	[CanEditMultipleObjects]
	public class UiReferencedSizeFitterEditor : UnityEditor.Editor
	{
		private SerializedProperty m_reference;
		private SerializedProperty m_horizontalFit;
		private SerializedProperty m_verticalFit;
		private SerializedProperty m_paddingLeft;
		private SerializedProperty m_paddingRight;
		private SerializedProperty m_paddingTop;
		private SerializedProperty m_paddingBottom;
		private SerializedProperty m_maxWidth;
		private SerializedProperty m_maxHeight;
		private SerializedProperty m_layoutPriority;

		private void OnEnable()
		{
			m_reference = serializedObject.FindProperty("m_reference");
			m_horizontalFit = serializedObject.FindProperty("m_horizontalFit");
			m_verticalFit = serializedObject.FindProperty("m_verticalFit");
			m_paddingLeft = serializedObject.FindProperty("m_paddingLeft");
			m_paddingRight = serializedObject.FindProperty("m_paddingRight");
			m_paddingTop = serializedObject.FindProperty("m_paddingTop");
			m_paddingBottom = serializedObject.FindProperty("m_paddingBottom");
			m_maxWidth = serializedObject.FindProperty("m_maxWidth");
			m_maxHeight = serializedObject.FindProperty("m_maxHeight");
			m_layoutPriority = serializedObject.FindProperty("m_layoutPriority");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_reference, new GUIContent("Reference",
				"The object being measured. May be a child (usually this object's own label), a sibling, " +
				"or something elsewhere - but not this object or one of its ancestors."));

			if (m_reference.objectReferenceValue == null && !m_reference.hasMultipleDifferentValues)
			{
				EditorGUILayout.HelpBox("No reference assigned - nothing is measured and no size is set.",
					MessageType.Warning);
			}

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(m_horizontalFit, new GUIContent("Horizontal Fit",
				"PreferredSize measures the reference without a width constraint - for a text that is its " +
				"single-line width. MinSize is the narrowest it can be without breaking a word; for a text " +
				"that is measured as the widest whitespace-separated token, since TMP reports no minimum " +
				"of its own."));

			bool horizontal = HasAnyActiveFit(m_horizontalFit);
			if (horizontal)
			{
				EditorGUI.indentLevel++;
				DrawPadding(m_paddingLeft, "Padding Left");
				DrawPadding(m_paddingRight, "Padding Right");
				DrawMax(m_maxWidth, "Max Width");
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(m_verticalFit, new GUIContent("Vertical Fit"));

			bool vertical = HasAnyActiveFit(m_verticalFit);
			if (vertical)
			{
				EditorGUI.indentLevel++;
				DrawPadding(m_paddingTop, "Padding Top");
				DrawPadding(m_paddingBottom, "Padding Bottom");
				DrawMax(m_maxHeight, "Max Height");
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(m_layoutPriority, new GUIContent("Layout Priority",
				"Must stay above the priority of any LayoutElement on this object. LayoutUtility takes the " +
				"maximum among the components of highest priority, so an equal priority would let an " +
				"unclamped value win and the maximum above would do nothing."));

			serializedObject.ApplyModifiedProperties();

			if (!horizontal && !vertical)
			{
				EditorGUILayout.HelpBox("Both axes are Unconstrained - this component currently does nothing.",
					MessageType.Info);
			}

			DrawDiagnostics(horizontal, vertical);
		}

		private static void DrawPadding( SerializedProperty _property, string _label )
		{
			EditorGUILayout.PropertyField(_property, new GUIContent(_label,
				"Added to the measured size, so the reference keeps this distance from the edge."));
		}

		private static void DrawMax( SerializedProperty _property, string _label )
		{
			EditorGUILayout.PropertyField(_property, new GUIContent(_label,
				"Negative means no maximum. At the maximum the size stops growing and the reference has to " +
				"cope inside it - for a text that means whatever its own TMP settings say (auto size, " +
				"wrapping, ellipsis)."));

			if (_property.hasMultipleDifferentValues)
				return;

			if (_property.floatValue == 0f)
			{
				EditorGUILayout.HelpBox("A maximum of exactly 0 collapses the size. Use a negative value for " +
					"\"no maximum\".", MessageType.Warning);
			}
		}

		private bool HasAnyActiveFit( SerializedProperty _fit )
		{
			if (!_fit.hasMultipleDifferentValues)
				return _fit.enumValueIndex != (int)UiReferencedSizeFitter.EFitMode.Unconstrained;

			// Mixed selection: show the details, otherwise they would be unreachable for part of it.
			return true;
		}

		private void DrawDiagnostics( bool _horizontal, bool _vertical )
		{
			if (targets.Length != 1)
				return;

			var fitter = (UiReferencedSizeFitter)target;

			// Report-only mode is invisible otherwise, and debugging "why does nothing happen" without
			// this hint costs real time.
			if (_horizontal && fitter.IsControlledByParent(RectTransform.Axis.Horizontal))
			{
				EditorGUILayout.HelpBox("The parent layout group controls the width. This component reports " +
					"the clamped width but does not write it - the group stays in charge.", MessageType.Info);
			}

			if (_vertical && fitter.IsControlledByParent(RectTransform.Axis.Vertical))
			{
				EditorGUILayout.HelpBox("The parent layout group controls the height. This component reports " +
					"the clamped height but does not write it - the group stays in charge.", MessageType.Info);
			}

			var layoutElement = fitter.GetComponent<LayoutElement>();
			if (layoutElement != null && layoutElement.enabled
			    && layoutElement.layoutPriority >= fitter.layoutPriority)
			{
				EditorGUILayout.HelpBox(
					$"The LayoutElement on this object has Layout Priority {layoutElement.layoutPriority}, " +
					$"which is not below this component's {fitter.layoutPriority}. LayoutUtility takes the " +
					"maximum among equals, so the maximum size can be overruled. Raise the priority here or " +
					"clear the LayoutElement's width and height.", MessageType.Warning);
			}

			var contentSizeFitter = fitter.GetComponent<ContentSizeFitter>();
			if (contentSizeFitter != null && contentSizeFitter.enabled)
			{
				EditorGUILayout.HelpBox("There is also a ContentSizeFitter on this object. Both write the same " +
					"rect and the later component wins - keep only one.", MessageType.Warning);
			}

			if (fitter.Reference != null && fitter.Reference == (RectTransform)fitter.transform)
			{
				EditorGUILayout.HelpBox("The reference is this object itself.", MessageType.Error);
			}
			else if (fitter.Reference != null && fitter.transform.IsChildOf(fitter.Reference))
			{
				EditorGUILayout.HelpBox("The reference is an ancestor of this object, so its size is derived " +
					"from ours. It is ignored.", MessageType.Error);
			}
		}
	}
}
#endif
