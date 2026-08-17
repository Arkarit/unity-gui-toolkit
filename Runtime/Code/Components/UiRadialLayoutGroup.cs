using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// <summary>
	/// A LayoutGroup that arranges its children around a circle by radius and angle, with modes for a fixed overall
	/// arc, a fixed per-element angle, or per-element angles taken from UiRadialLayoutElement children. It can
	/// optionally rotate elements to follow the arc and apply a per-element Z increment.
	/// </summary>
	public class UiRadialLayoutGroup : LayoutGroup
	{
		public enum Mode
		{
			FixedOverallAngle,
			FixedElementAngle,
			PerElementAngle,
		}

		[SerializeField]							protected Mode m_mode;
		[SerializeField][Range(-360f, 360f)]		protected float m_angle0;
		[SerializeField][Range(-360f, 360f)]		protected float m_angle1;
		[SerializeField][Range(-360f, 360f)]		protected float m_angleOffset;
		[SerializeField]							protected float m_radius;

		/// <summary>
		/// Centre of the arc, in this group's own local space. Zero means the transform origin, which is
		/// what this component always used to assume.
		/// </summary>
		/// <remarks>
		/// Without this the only way to place the arc was to move the whole GameObject, which is useless
		/// when the arc has to line up with something whose centre sits elsewhere — a UiBend curves around
		/// a point derived from its mesh, not around its own origin.
		/// </remarks>
		[SerializeField]							protected Vector2 m_arcCenter;

		[SerializeField]							protected float m_zIncrement = 0;

		/// <summary>
		/// Scales the arc horizontally: 1 is a circle, other values an ellipse. Formerly "XFactor".
		/// </summary>
		[FormerlySerializedAs("m_xFactor")]
		[SerializeField]							protected float m_ellipseScaleX = 1;

		[SerializeField]							protected bool m_rotateElements = false;
		[SerializeField][Range(-360f, 360f)]		protected float m_elementAngleOffset;
		[SerializeField][HideInInspector]			protected bool m_useZIncrement;
		[SerializeField][HideInInspector]			protected bool m_childRotationChanged;

		protected override void OnEnable()
		{
			base.OnEnable();
			CalculateRadial();
		}

		public override void SetLayoutHorizontal() {}
		public override void SetLayoutVertical() {}

		public override void CalculateLayoutInputVertical()
		{
			CalculateRadial();
		}

		public override void CalculateLayoutInputHorizontal()
		{
			CalculateRadial();
		}

		public new void SetDirty()
		{
			base.SetDirty();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			m_useZIncrement = !Mathf.Approximately(m_zIncrement, 0);
			CalculateRadial();
		}
#endif

		private int ChildCount
		{
			get
			{
				int result = 0;
				foreach (Transform child in transform)
				{
					if (child.gameObject.activeSelf)
						result++;
				}
				return result;
			}
		}

		private List<float> ElementAngles
		{
			get
			{
				List <float> result = new List<float>();
				foreach (Transform child in transform)
				{
					if (child.gameObject.activeSelf)
					{
						UiRadialLayoutElement layoutElement = child.GetComponent<UiRadialLayoutElement>();
						result.Add(layoutElement != null ? layoutElement.Angle : 0);
					}
				}
				return result;
			}
		}

		private void CalculateRadial()
		{
			m_Tracker.Clear();

			int childCount = ChildCount;
			if (childCount == 0)
				return;

			float topAngleOffset = 0, angleIncrement = 0;
			List<float> angleIncrements = null;

			switch(m_mode)
			{
				case Mode.FixedOverallAngle:
					topAngleOffset = -m_angle0 - 90;
					angleIncrement = ((m_angle1 - m_angle0)) / (childCount - 1);
					break;
				case Mode.PerElementAngle:
					angleIncrements = ElementAngles;
					float sum = 0;
					for (int i = 0; i<angleIncrements.Count; i++)
						sum += angleIncrements[i];
					topAngleOffset = sum * 0.5f - 90.0f;
					break;
				default:
				case Mode.FixedElementAngle:
					topAngleOffset = m_angle0 * (childCount - 1) * 0.5f - 90.0f;
					angleIncrement = m_angle0;
					break;
			}

			float runningAngle = m_angleOffset - topAngleOffset;

			float z = 0;
			if (m_useZIncrement && childCount > 1)
			{
				z = - m_zIncrement * childCount / 2;
			}

			int angleIncrementsIdx = 0;
			for (int i = 0; i < transform.childCount; i++)
			{
				RectTransform child = (RectTransform)transform.GetChild(i);
				if (!child.gameObject.activeSelf)
					continue;

				if (child != null)
				{
					m_Tracker.Add( this, child, GetDrivenTransformProperties() );

					float angle = runningAngle;
					if (m_mode == Mode.PerElementAngle)
						angle += angleIncrements[angleIncrementsIdx] * 0.5f;

					float angleRad = angle * Mathf.Deg2Rad;
					Vector3 vPos = new Vector3(Mathf.Cos(angleRad) * m_ellipseScaleX, Mathf.Sin(angleRad), 0);
					child.localPosition = (Vector3)m_arcCenter + vPos * m_radius;

					if (m_useZIncrement)
					{
						child.localPosition += new Vector3(0,0, z);
						z += m_zIncrement;
					}

					if (m_rotateElements)
					{
						child.localRotation = Quaternion.AngleAxis(SurfaceAngle(angleRad) + m_elementAngleOffset, Vector3.forward);
					}
					else if (m_childRotationChanged)
					{
						child.localRotation = Quaternion.identity;
					}

					if (m_mode == Mode.PerElementAngle)
						runningAngle += angleIncrements[angleIncrementsIdx++];
					else
						runningAngle += angleIncrement;
				}
			}

			m_childRotationChanged = false;
		}

		/// <summary>
		/// The outward normal of the arc at a given parameter angle, in degrees — the direction an element
		/// has to face to stand upright on the curve.
		/// </summary>
		/// <remarks>
		/// On a circle this is just the parameter angle itself, which is what this component used
		/// unconditionally. On an ellipse it is not: the point sits at (a·cos t, b·sin t) and the normal
		/// runs along (b·cos t, a·sin t), so elements were rotated to face the centre rather than to stand
		/// on the curve, and leaned wrong by a margin that grew with the squash. With
		/// <see cref="m_ellipseScaleX"/> at 1 the two expressions agree exactly, so a circular setup is
		/// unaffected.
		/// </remarks>
		private float SurfaceAngle( float _angleRad )
		{
			if (Mathf.Approximately(m_ellipseScaleX, 1f))
				return _angleRad * Mathf.Rad2Deg;

			return Mathf.Atan2(m_ellipseScaleX * Mathf.Sin(_angleRad), Mathf.Cos(_angleRad)) * Mathf.Rad2Deg;
		}

		private DrivenTransformProperties GetDrivenTransformProperties()
		{
			DrivenTransformProperties result = DrivenTransformProperties.AnchoredPosition;

			if (m_useZIncrement)
				result |= DrivenTransformProperties.AnchoredPositionZ;

			if (m_rotateElements)
				result |= DrivenTransformProperties.Rotation;

			return result;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiRadialLayoutGroup))]
	public class UiRadialLayoutGroupEditor : UnityEditor.Editor
	{
		protected SerializedProperty m_modeProp;
		protected SerializedProperty m_angle0Prop;
		protected SerializedProperty m_angle1Prop;
		protected SerializedProperty m_angleOffsetProp;
		protected SerializedProperty m_radiusProp;
		protected SerializedProperty m_arcCenterProp;
		protected SerializedProperty m_zIncrementProp;
		protected SerializedProperty m_ellipseScaleXProp;
		protected SerializedProperty m_rotateElementsProp;
		protected SerializedProperty m_elementAngleOffsetProp;
		protected SerializedProperty m_childRotationChangedProp;

		static private bool m_toolsVisible;

		/// Editor-session only: which bend to copy from. Deliberately not serialised — it is a tool
		/// setting for a one-shot action, not part of the layout.
		private UiBend m_arcSource;
		private bool m_takeAngles = true;

		public virtual void OnEnable()
		{
			m_modeProp = serializedObject.FindProperty("m_mode");
			m_angle0Prop = serializedObject.FindProperty("m_angle0");
			m_angle1Prop = serializedObject.FindProperty("m_angle1");
			m_angleOffsetProp = serializedObject.FindProperty("m_angleOffset");
			m_radiusProp = serializedObject.FindProperty("m_radius");
			m_arcCenterProp = serializedObject.FindProperty("m_arcCenter");
			m_zIncrementProp = serializedObject.FindProperty("m_zIncrement");
			m_ellipseScaleXProp = serializedObject.FindProperty("m_ellipseScaleX");
			m_rotateElementsProp = serializedObject.FindProperty("m_rotateElements");
			m_elementAngleOffsetProp = serializedObject.FindProperty("m_elementAngleOffset");
			m_childRotationChangedProp = serializedObject.FindProperty("m_childRotationChanged");
		}

		public override void OnInspectorGUI()
		{
			UiRadialLayoutGroup thisUiRadialLayoutGroup = (UiRadialLayoutGroup)target;

			EditorGUILayout.PropertyField(m_modeProp);
			UiRadialLayoutGroup.Mode mode = (UiRadialLayoutGroup.Mode) m_modeProp.intValue;
			switch( mode )
			{
				case UiRadialLayoutGroup.Mode.FixedOverallAngle:
					EditorGUILayout.PropertyField(m_angle0Prop, new GUIContent("Angle left"));
					EditorGUILayout.PropertyField(m_angle1Prop, new GUIContent("Angle right"));
					break;
				case UiRadialLayoutGroup.Mode.FixedElementAngle:
					EditorGUILayout.PropertyField(m_angle0Prop, new GUIContent("Angle between elements"));
					break;
			}

			EditorGUILayout.PropertyField(m_angleOffsetProp);
			EditorGUILayout.PropertyField(m_radiusProp);
			EditorGUILayout.PropertyField(m_arcCenterProp, new GUIContent("Arc Center",
				"Centre of the arc in this group's local space. Zero is the transform origin."));
			EditorGUILayout.PropertyField(m_zIncrementProp);
			EditorGUILayout.PropertyField(m_ellipseScaleXProp, new GUIContent("Ellipse Scale X",
				"Horizontal scale of the arc. 1 is a circle, anything else an ellipse."));
			bool rotateElementsBefore = m_rotateElementsProp.boolValue;
			EditorGUILayout.PropertyField(m_rotateElementsProp);
			bool rotateElementsAfter = m_rotateElementsProp.boolValue;
			m_childRotationChangedProp.boolValue = rotateElementsBefore != rotateElementsAfter;

			EditorGUILayout.PropertyField(m_elementAngleOffsetProp);

			DrawTakeArcFromBend(thisUiRadialLayoutGroup);

			serializedObject.ApplyModifiedProperties();
		}

		/// <summary>
		/// One-shot: read a UiBend's arc and write the equivalent settings into this group.
		/// </summary>
		/// <remarks>
		/// A copy, not a link. Nothing keeps the two in step afterwards — press it again after changing
		/// the bend. That is the deliberate limit of this step: a live link would need the layout to be
		/// told when the bend changed, and the layout and the mesh modifiers rebuild through different
		/// pipelines.
		/// </remarks>
		private void DrawTakeArcFromBend( UiRadialLayoutGroup _group )
		{
			EditorGUILayout.Space();
			m_toolsVisible = EditorGUILayout.Foldout(m_toolsVisible, "Match another arc", true);
			if (!m_toolsVisible)
				return;

			using (new EditorGUI.IndentLevelScope())
			{
				m_arcSource = (UiBend)EditorGUILayout.ObjectField("Arc Source (UiBend)", m_arcSource, typeof(UiBend), true);

				m_takeAngles = EditorGUILayout.Toggle(new GUIContent("Also Take Angles",
					"On: the elements spread across the bend's whole width, which switches Mode to " +
					"FixedOverallAngle. Off: only centre and radius are taken, so an existing spacing " +
					"(a fixed angle between elements, or per-element angles) survives."), m_takeAngles);

				using (new EditorGUI.DisabledScope(m_arcSource == null))
				{
					if (GUILayout.Button("Take Arc From Bend"))
						TakeArcFrom(_group, m_arcSource, m_takeAngles);
				}

				EditorGUILayout.HelpBox(
					"Copies the bend's arc into this group's space. A one-time copy — press again after " +
					"changing the bend.",
					MessageType.None);
			}
		}

		private void TakeArcFrom( UiRadialLayoutGroup _group, UiBend _bend, bool _takeAngles )
		{
			if (!_bend.TryGetArc(out Vector2 centerLocal, out float radius, out float startDeg, out float endDeg))
			{
				UiLog.LogWarning($"{_bend.name} describes no arc yet — it needs a mesh with bounds and a non-zero Angle.", _bend);
				return;
			}

			Transform bendTr = _bend.transform;
			Transform groupTr = _group.transform;

			// Carried across as points rather than as angles and lengths: any rotation or scale between
			// the two transforms is then handled by the transforms themselves instead of by arithmetic
			// here that would have to anticipate it.
			Vector3 center = groupTr.InverseTransformPoint(bendTr.TransformPoint(centerLocal));
			Vector3 start = groupTr.InverseTransformPoint(bendTr.TransformPoint(PointOnArc(centerLocal, radius, startDeg)));
			Vector3 end = groupTr.InverseTransformPoint(bendTr.TransformPoint(PointOnArc(centerLocal, radius, endDeg)));

			float startInGroup = Mathf.Atan2(start.y - center.y, start.x - center.x) * Mathf.Rad2Deg;
			float endInGroup = Mathf.Atan2(end.y - center.y, end.x - center.x) * Mathf.Rad2Deg;

			m_arcCenterProp.vector2Value = center;
			m_radiusProp.floatValue = Vector2.Distance(start, center);
			m_ellipseScaleXProp.floatValue = 1f;   // a bend produces a circular arc, never an ellipse

			if (!_takeAngles)
				return;

			m_modeProp.enumValueIndex = (int)UiRadialLayoutGroup.Mode.FixedOverallAngle;
			m_angleOffsetProp.floatValue = 0f;

			// The group states its arc as an offset from straight up, then adds AngleOffset on top.
			m_angle0Prop.floatValue = Mathf.DeltaAngle(90f, startInGroup);
			m_angle1Prop.floatValue = Mathf.DeltaAngle(90f, endInGroup);
		}

		private static Vector2 PointOnArc( Vector2 _center, float _radius, float _angleDeg )
		{
			float rad = _angleDeg * Mathf.Deg2Rad;
			return _center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;
		}
	}
#endif

}