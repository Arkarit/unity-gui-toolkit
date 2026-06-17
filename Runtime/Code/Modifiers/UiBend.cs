using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// <summary>
	/// Bends the mesh of a UI Graphic (Image / RawImage / TextMeshPro) along a circular arc.
	///
	/// <list type="bullet">
	///   <item><b>Angle</b> — total arc (in degrees) the bounding-box width is bent into.
	///         Positive values bend upwards (arc center above the pivot), negative downwards.</item>
	///   <item><b>Rotation</b> — Z rotation (in degrees) applied to the whole bent result around the pivot.</item>
	///   <item><b>Pivot</b> — normalized position inside the bounding rect (0..1, 0.5/0.5 = center).
	///         The pivot is the fixed point of the bend and the rotation center; it is editable
	///         via a scene-view handle.</item>
	/// </list>
	///
	/// Vertex-based modifier: for Images, combine with <see cref="UiTessellator"/> to get a smooth arc;
	/// TextMeshPro provides enough vertices per glyph for the effect to look smooth out of the box.
	/// </summary>
	[ExecuteAlways]
	public class UiBend : BaseMeshEffectTMP
	{
		[SerializeField] private float m_angle;
		[SerializeField] private float m_rotation;
		[SerializeField] private Vector2 m_pivot = new Vector2(0.5f, 0.5f);

		public Rect Bounding { get; protected set; }

		protected static readonly List<UIVertex> s_verts = new();
		protected static UIVertex s_vertex;

		public float Angle
		{
			get => m_angle;
			set
			{
				if (Mathf.Approximately(m_angle, value))
					return;
				m_angle = value;
				SetDirty();
			}
		}

		public float Rotation
		{
			get => m_rotation;
			set
			{
				if (Mathf.Approximately(m_rotation, value))
					return;
				m_rotation = value;
				SetDirty();
			}
		}

		/// <summary>Normalized pivot position in the bounding rect, (0,0) = bottom-left, (1,1) = top-right.</summary>
		public Vector2 Pivot
		{
			get => m_pivot;
			set
			{
				if (m_pivot == value)
					return;
				m_pivot = value;
				SetDirty();
			}
		}

		public void ResetPivot()
		{
			Pivot = new Vector2(0.5f, 0.5f);
		}

		public override void ModifyMesh(VertexHelper _vertexHelper)
		{
			if (!IsActive())
				return;

			_vertexHelper.GetUIVertexStream(s_verts);
			Bounding = UiMeshModifierUtility.GetBounds(s_verts);

			if (Bounding.width <= 0 || Bounding.height <= 0)
				return;

			Vector2 pivot = new Vector2
			(
				Bounding.x + m_pivot.x * Bounding.width,
				Bounding.y + m_pivot.y * Bounding.height
			);

			float angleRad = m_angle * Mathf.Deg2Rad;
			float rotationRad = m_rotation * Mathf.Deg2Rad;
			float cosRot = Mathf.Cos(rotationRad);
			float sinRot = Mathf.Sin(rotationRad);

			bool hasBend = Mathf.Abs(angleRad) > 1e-5f;
			float r = hasBend ? Bounding.width / angleRad : 0f;

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				Vector3 pos = s_vertex.position;
				float dx = pos.x - pivot.x;
				float dy = pos.y - pivot.y;

				Vector2 bent;
				if (hasBend)
				{
					// Wrap dx around an arc of radius r whose center is r above the pivot,
					// preserving vertical offset as radial distance from that center.
					float phi = dx / r;
					float radial = r - dy;
					bent.x = radial * Mathf.Sin(phi);
					bent.y = r - radial * Mathf.Cos(phi);
				}
				else
				{
					bent.x = dx;
					bent.y = dy;
				}

				Vector2 rotated = new Vector2
				(
					bent.x * cosRot - bent.y * sinRot,
					bent.x * sinRot + bent.y * cosRot
				);

				s_vertex.position = new Vector3
				(
					pivot.x + rotated.x,
					pivot.y + rotated.y,
					pos.z
				);

				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiBend))]
	public class UiBendEditor : UnityEditor.Editor
	{
		private SerializedProperty m_angleProp;
		private SerializedProperty m_rotationProp;
		private SerializedProperty m_pivotProp;

		private void OnEnable()
		{
			m_angleProp = serializedObject.FindProperty("m_angle");
			m_rotationProp = serializedObject.FindProperty("m_rotation");
			m_pivotProp = serializedObject.FindProperty("m_pivot");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_angleProp);
			EditorGUILayout.PropertyField(m_rotationProp);
			EditorGUILayout.PropertyField(m_pivotProp);

			if (GUILayout.Button("Reset Gizmo"))
				m_pivotProp.vector2Value = new Vector2(0.5f, 0.5f);

			if (serializedObject.ApplyModifiedProperties())
				((UiBend)target).SetDirty();
		}

		protected virtual void OnSceneGUI()
		{
			UiBend thisUiBend = (UiBend)target;
			if (thisUiBend == null || !thisUiBend.IsActive())
				return;

			RectTransform rt = (RectTransform)thisUiBend.transform;
			if (rt.rect.size.x == 0 || rt.rect.size.y == 0)
				return;

			Rect bounding = thisUiBend.Bounding;
			if (bounding.width <= 0 || bounding.height <= 0)
				return;

			Vector2 pivotNorm = m_pivotProp.vector2Value;
			Vector3 pivotLocal = new Vector3
			(
				bounding.x + pivotNorm.x * bounding.width,
				bounding.y + pivotNorm.y * bounding.height,
				0f
			);
			Vector3 pivotWorld = rt.TransformPoint(pivotLocal);

			Handles.color = Constants.HANDLE_COLOR;
			float handleSize = HandleUtility.GetHandleSize(pivotWorld) * Constants.HANDLE_SIZE;

			EditorGUI.BeginChangeCheck();
			Vector3 newPivotWorld = Handles.FreeMoveHandle(pivotWorld, handleSize, Vector3.one, Handles.SphereHandleCap);
			if (EditorGUI.EndChangeCheck())
			{
				Vector3 newPivotLocal = rt.InverseTransformPoint(newPivotWorld);
				m_pivotProp.vector2Value = new Vector2
				(
					(newPivotLocal.x - bounding.x) / bounding.width,
					(newPivotLocal.y - bounding.y) / bounding.height
				);
				serializedObject.ApplyModifiedProperties();
				EditorGeneralUtility.SetDirty(target);
			}
		}
	}
#endif
}
