﻿#if UNITY_EDITOR
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
		[Range(2, 10)]
		protected int m_pointsHorizontal = 3;

		[SerializeField]
		[Range(2, 10)]
		protected int m_pointsVertical = 2;

		[SerializeField]
		protected Vector2[] m_points;

		[SerializeField]
		protected bool m_absoluteValues;

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

			_vertexHelper.GetUIVertexStream(s_verts);

			Bounding = UiMeshModifierUtility.GetBounds(s_verts);
			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				Vector2 pointNormalized = s_vertex.position.GetNormalizedPointInRect(Bounding);
				Vector2 point = s_vertex.position.Xy() + UiMathUtility.InterpPoint(m_points, m_pointsHorizontal, m_pointsVertical, pointNormalized, false, true) * Bounding.size;
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
	public class UIFFDEditor : UnityEditor.Editor
	{
		protected SerializedProperty m_pointsHorizontalProp;
		protected SerializedProperty m_pointsVerticalProp;
		protected SerializedProperty m_pointsProp;
		protected SerializedProperty m_absoluteValuesProp;

		static private bool m_toolsVisible;

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

			m_toolsVisible = EditorGUILayout.Foldout(m_toolsVisible, "Tools");

			if (m_toolsVisible)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Copy");
				if (GUILayout.Button("Top > Bot"))
				{
				}
				if (GUILayout.Button("Bot > Top"))
				{
				}
				if (GUILayout.Button("L > R"))
				{
				}
				if (GUILayout.Button("R > L"))
				{
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Mirror");
				if (GUILayout.Button("Top > Bot"))
				{
				}
				if (GUILayout.Button("Bot > Top"))
				{
				}
				if (GUILayout.Button("L > R"))
				{
				}
				if (GUILayout.Button("R > L"))
				{
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label("Reset");
				if (GUILayout.Button("Center Points Y"))
				{
				}
				if (GUILayout.Button("Center Points X"))
				{
				}
				if (GUILayout.Button("All"))
				{
					for (int i=0; i<arrayLength; i++)
						m_pointsProp.GetArrayElementAtIndex(i).vector2Value = Vector2.zero;
				}
				EditorGUILayout.EndHorizontal();

			}

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
			Vector3[] corners = thisUiFFD.Bounding.GetWorldCorners(rt);
            Handles.color = Color.yellow;

			bool isAbsolute = m_absoluteValuesProp.boolValue;
			Vector2 size = isAbsolute ? bounding.size / rt.rect.size : bounding.size;

			bool hasChanged = false;
			float xStep = 1.0f / (thisUiFFD.PointsHorizontal - 1);
			float yStep = 1.0f / (thisUiFFD.PointsVertical - 1);
			for (int iy=0; iy<thisUiFFD.PointsVertical; iy++)
			{
				bool isYCorner = iy == 0 || iy == thisUiFFD.PointsVertical-1;

				for (int ix=0; ix<thisUiFFD.PointsHorizontal; ix++)
				{
					bool isCorner = (ix == 0 || ix == thisUiFFD.PointsHorizontal-1) && isYCorner;
					Handles.color = isCorner ? Constants.HANDLE_COLOR : Constants.HANDLE_SUPPORTING_COLOR;

					int arrayIdx = iy * thisUiFFD.PointsHorizontal + ix;
					Vector2 normCorners = new Vector2(ix * xStep, iy * yStep);
					Vector3 tp = UiMathUtility.Lerp4P(corners, ref normCorners);
					hasChanged |= EditorUiUtility.DoHandle( m_pointsProp.GetArrayElementAtIndex(arrayIdx), tp, size, rt, false, false, Constants.HANDLE_SIZE ); 
				}
			}

			if (hasChanged)
			{
				serializedObject.ApplyModifiedProperties();
				EditorGeneralUtility.SetDirty(target);
			}

			Handles.color = Constants.HANDLE_CAGE_LINE_COLOR;

			for (int iy=0; iy<thisUiFFD.PointsVertical; iy++)
			{
				for (int ix=0; ix<thisUiFFD.PointsHorizontal; ix++)
				{
					int arrayIdx0 = iy * thisUiFFD.PointsHorizontal + ix;
					SerializedProperty p0Prop = m_pointsProp.GetArrayElementAtIndex(arrayIdx0);
					Vector2 normCorners0 = new Vector2(ix * xStep, iy * yStep);
					Vector2 tp0 = UiMathUtility.Lerp4P(corners, ref normCorners0);

					if (ix<thisUiFFD.PointsHorizontal-1)
					{
						SerializedProperty p1Prop = m_pointsProp.GetArrayElementAtIndex(arrayIdx0+1);
						Vector2 normCorners1 = new Vector2((ix+1) * xStep, iy * yStep);
						Vector2 tp1 = UiMathUtility.Lerp4P(corners, ref normCorners1);
						EditorUiUtility.DrawLine(p0Prop, p1Prop, tp0, tp1, size, rt);
					}

					if (iy<thisUiFFD.PointsVertical-1)
					{
						int arrayIdx1 = (iy+1) * thisUiFFD.PointsHorizontal + ix;
						SerializedProperty p1Prop = m_pointsProp.GetArrayElementAtIndex(arrayIdx1);
						Vector2 normCorners1 = new Vector2(ix * xStep, (iy+1) * yStep);
						Vector2 tp1 = UiMathUtility.Lerp4P(corners, ref normCorners1);
						EditorUiUtility.DrawLine(p0Prop, p1Prop, tp0, tp1, size, rt);
					}

				}
			}
		}

	}
#endif
}
