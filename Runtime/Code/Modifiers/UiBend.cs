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
	///
	/// Set <c>FollowSource</c> to another <see cref="UiBend"/> to mirror its effect in world space, so a
	/// banner / underlay curves identically to the text it follows, regardless of its own mesh size.
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

		/// <summary>
		/// Pure bend math in local coordinates. Uses the most recently cached <see cref="Bounding"/>.
		/// Returns the input unchanged if bounding is degenerate.
		/// </summary>
		public Vector2 BendPointLocal(Vector2 _point)
		{
			if (Bounding.width <= 0 || Bounding.height <= 0)
				return _point;

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

			float dx = _point.x - pivot.x;
			float dy = _point.y - pivot.y;

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

			return new Vector2
			(
				pivot.x + bent.x * cosRot - bent.y * sinRot,
				pivot.y + bent.x * sinRot + bent.y * cosRot
			);
		}

		public override void ModifyMesh(VertexHelper _vertexHelper)
		{
			if (!IsActive())
				return;

			if (IsFollowing)
			{
				ModifyMeshFollowing(_vertexHelper, FollowSource);
				return;
			}

			_vertexHelper.GetUIVertexStream(s_verts);
			Bounding = UiMeshModifierUtility.GetBounds(s_verts);

			DirtyFollowers();

			if (Bounding.width <= 0 || Bounding.height <= 0)
				return;

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);
				Vector3 pos = s_vertex.position;
				Vector2 bent = BendPointLocal(new Vector2(pos.x, pos.y));
				s_vertex.position = new Vector3(bent.x, bent.y, pos.z);
				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}

		/// <summary>
		/// Replicates the source UiBend's effect in world space. Per vertex:
		/// follower-local → world → source-local → bend (using source params + source bounds)
		/// → world → follower-local. The source's <see cref="Bounding"/> is expected to be up-to-date;
		/// SetDirty propagation from the source ensures the follower refreshes after the source.
		/// </summary>
		protected override void ModifyMeshFollowing(VertexHelper _vertexHelper, BaseMeshEffectTMP _source)
		{
			UiBend src = _source as UiBend;
			if (src == null || !src.IsActive())
				return;

			Transform followerTr = transform;
			Transform sourceTr = src.transform;

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				Vector3 pFollowerLoc = s_vertex.position;
				Vector3 pWorld = followerTr.TransformPoint(pFollowerLoc);
				Vector3 pSourceLoc = sourceTr.InverseTransformPoint(pWorld);

				Vector2 bentSourceLoc = src.BendPointLocal(new Vector2(pSourceLoc.x, pSourceLoc.y));

				Vector3 bentWorld = sourceTr.TransformPoint(new Vector3(bentSourceLoc.x, bentSourceLoc.y, pSourceLoc.z));
				Vector3 bentFollowerLoc = followerTr.InverseTransformPoint(bentWorld);

				s_vertex.position = bentFollowerLoc;
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
		private SerializedProperty m_followSourceProp;

		private void OnEnable()
		{
			m_angleProp = serializedObject.FindProperty("m_angle");
			m_rotationProp = serializedObject.FindProperty("m_rotation");
			m_pivotProp = serializedObject.FindProperty("m_pivot");
			m_followSourceProp = serializedObject.FindProperty("m_followSource");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			// Type-filtered field: only allow UiBend references.
			var currentSrc = (UiBend)m_followSourceProp.objectReferenceValue;
			var newSrc = (UiBend)EditorGUILayout.ObjectField("Follow Source", currentSrc, typeof(UiBend), true);
			if (newSrc != currentSrc)
				m_followSourceProp.objectReferenceValue = newSrc;

			UiBend thisUiBend = (UiBend)target;
			bool isFollowing = thisUiBend.IsFollowing;

			if (isFollowing)
			{
				EditorGUILayout.HelpBox("This UiBend follows another UiBend in world space. Local parameters are ignored.", MessageType.Info);
				if (GUILayout.Button("Select Source"))
				{
					var src = thisUiBend.FollowSource;
					if (src != null)
						Selection.activeObject = src.gameObject;
				}
			}
			else
			{
				EditorGUILayout.PropertyField(m_angleProp);
				EditorGUILayout.PropertyField(m_rotationProp);
				EditorGUILayout.PropertyField(m_pivotProp);

				if (GUILayout.Button("Reset Gizmo"))
					m_pivotProp.vector2Value = new Vector2(0.5f, 0.5f);
			}

			if (serializedObject.ApplyModifiedProperties())
				thisUiBend.SetDirty();
		}

		protected virtual void OnSceneGUI()
		{
			UiBend thisUiBend = (UiBend)target;
			if (thisUiBend == null || !thisUiBend.IsActive() || thisUiBend.IsFollowing)
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
