#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiFFD : BaseMeshEffectTMP
	{
		[SerializeField]
		[Range(2, 6)]
		protected int m_pointsHorizontal = 3;

		[SerializeField]
		[Range(2, 6)]
		protected int m_pointsVertical = 2;

		[SerializeField]
		protected Vector2[] m_points;

		[SerializeField]
		protected bool m_absoluteValues;

		[SerializeField]
		protected float m_tension = 25;

		[SerializeField]
		protected float m_continuity = 25;

		private float m1;
		private float m2;

		protected static readonly List<UIVertex> s_verts = new List<UIVertex>();
		protected static UIVertex s_vertex;

		public Rect Bounding { get; protected set; }
		public int PointsHorizontal { get {return m_pointsHorizontal; }}
		public int PointsVertical { get {return m_pointsVertical; }}

		protected override void Awake()
		{
			base.Awake();
			if (m_points == null)
				m_points = new Vector2[m_pointsHorizontal * m_pointsVertical];
		}

		public override void ModifyMesh( VertexHelper _vertexHelper )
		{
			if (!IsActive())
				return;

//			ComputeTension(m_tension, m_continuity, ref m1, ref m2);

			_vertexHelper.GetUIVertexStream(s_verts);

			Bounding = UiModifierUtil.GetBounds(s_verts);
			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				Vector2 pointNormalized = s_vertex.position.GetNormalizedPointInRect(Bounding);
				Vector2 point = s_vertex.position.Xy() + UiMath.InterpPoint(m_points, m_pointsHorizontal, m_pointsVertical, pointNormalized);
				s_vertex.position = new Vector3(point.x, point.y, s_vertex.position.z);

				_vertexHelper.SetUIVertex(s_vertex, i);
			}

		}

		private void Get( int _x, int _y, ref Vector2 _v )
		{
			_v = m_points[_y * m_pointsHorizontal + _x];
		}

		private Vector2 Get( int _x, int _y )
		{
			return m_points[_y * m_pointsHorizontal + _x];
		}

		private void Set( int _x, int _y, ref Vector2 _v )
		{
			m_points[_y * m_pointsHorizontal + _x] = _v;
		}

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiFFD))]
	public class UiFFDEditor : Editor
	{
		protected SerializedProperty m_pointsHorizontalProp;
		protected SerializedProperty m_pointsVerticalProp;
		protected SerializedProperty m_pointsProp;
		protected SerializedProperty m_absoluteValuesProp;

		public virtual void OnEnable()
		{
			m_pointsHorizontalProp = serializedObject.FindProperty("m_pointsHorizontal");
			m_pointsVerticalProp = serializedObject.FindProperty("m_pointsVertical");
			m_pointsProp = serializedObject.FindProperty("m_points");
			m_absoluteValuesProp = serializedObject.FindProperty("m_absoluteValues");
		}

		public override void OnInspectorGUI()
		{
			UiFFD thisUiFFD = (UiFFD)target;

			EditorGUILayout.PropertyField(m_pointsHorizontalProp);
			EditorGUILayout.PropertyField(m_pointsVerticalProp);

			serializedObject.ApplyModifiedProperties();

			int numHorizontal = thisUiFFD.PointsHorizontal;
			int numVertical = thisUiFFD.PointsVertical;
			int arrayLength = numHorizontal * numVertical;
			m_pointsProp.arraySize = arrayLength;

			serializedObject.ApplyModifiedProperties();
		}

		protected virtual void OnSceneGUI()
		{
			UiFFD thisUiFFD = (UiFFD)target;
			if (!thisUiFFD.IsActive())
				return;

			RectTransform rt = (RectTransform) thisUiFFD.transform;

			// Avoid div/0
			if (rt.rect.size.x == 0 || rt.rect.size.y == 0)
				return;

			Rect bounding = thisUiFFD.Bounding;
			Vector2[] corners = thisUiFFD.Bounding.GetWorldCorners2D(rt);
            Handles.color = Color.yellow;

			bool isAbsolute = m_absoluteValuesProp.boolValue;
			Vector2 size = isAbsolute ? bounding.size / rt.rect.size : bounding.size;

			bool hasChanged = false;
			float xStep = 1.0f / (thisUiFFD.PointsHorizontal - 1);
			float yStep = 1.0f / (thisUiFFD.PointsVertical - 1);
			for (int iy=0; iy<thisUiFFD.PointsVertical; iy++)
			{
				for (int ix=0; ix<thisUiFFD.PointsHorizontal; ix++)
				{
					int arrayIdx = iy * thisUiFFD.PointsHorizontal + ix;
					Vector2 normCorners = new Vector2(ix * xStep, iy * yStep);
					Vector2 tp = UiMath.Lerp4P(corners, normCorners);
					hasChanged |= UiEditorUtility.DoHandle( m_pointsProp.GetArrayElementAtIndex(arrayIdx), tp, thisUiFFD.Bounding.size, rt ); 
				}
			}

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorUtility.SetDirty(target);
			}

		}

	}
#endif
}
