using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class UiDistortBase : BaseMeshEffectTMP
	{
		[FormerlySerializedAs("m_mirrorDirection")]
		[SerializeField] protected EAxis2DFlags m_mirrorAxisFlags;
		[SerializeField] protected Vector2 m_topLeft = Vector2.zero;
		[SerializeField] protected Vector2 m_topRight = Vector2.zero;
		[SerializeField] protected Vector2 m_bottomLeft = Vector2.zero;
		[SerializeField] protected Vector2 m_bottomRight = Vector2.zero;

		protected static readonly List<UIVertex> s_verts = new();
		protected static UIVertex s_vertex;

		public virtual bool IsAbsolute
		{
			get => false;
			set => throw new NotImplementedException($"IsAbsolute for {GetType().Name} has no setter");
		}

		protected virtual void Prepare() {}

		public Rect Bounding {get; protected set;}

		public EAxis2DFlags Mirror
		{
			get => m_mirrorAxisFlags;
			set => m_mirrorAxisFlags = value;
		}
		
		public virtual Vector2 TopLeft
		{
			get => m_topLeft;
			set
			{
				if (m_topLeft == value)
					return;
				m_topLeft = value;
				SetDirty();
			}
		}

		public virtual Vector2 TopRight
		{
			get => m_topRight;
			set
			{
				if (m_topRight == value)
					return;
				m_topRight = value;
				SetDirty();
			}
		}

		public virtual Vector2 BottomLeft
		{
			get => m_bottomLeft;
			set
			{
				if (m_bottomLeft == value)
					return;
				m_bottomLeft = value;
				SetDirty();
			}
		}

		public virtual Vector2 BottomRight
		{
			get => m_bottomRight;
			set
			{
				if (m_bottomRight == value)
					return;
				m_bottomRight = value;
				SetDirty();
			}
		}

		public override void ModifyMesh( VertexHelper _vertexHelper )
		{
			if (!IsActive())
				return;

			_vertexHelper.GetUIVertexStream(s_verts);

			Bounding = UiMeshModifierUtility.GetBounds(s_verts);

			Prepare();

			Vector2 size = IsAbsolute ? Vector2.one : Bounding.size;

			Vector2 tl = TopLeft * size;
			Vector2 bl = BottomLeft * size;
			Vector2 tr = TopRight * size;
			Vector2 br = BottomRight * size;

			bool mirrorHorizontal = m_mirrorAxisFlags.IsFlagSet(EAxis2DFlags.Horizontal);
			bool mirrorVertical = m_mirrorAxisFlags.IsFlagSet(EAxis2DFlags.Vertical);

			Vector2 mirrorVec = new Vector2(mirrorHorizontal ? -1 : 1, mirrorVertical ? -1 : 1);

			if (mirrorHorizontal)
			{
				UiMathUtility.Swap( ref tl, ref tr );
				UiMathUtility.Swap( ref bl, ref br );
			}

			if (mirrorVertical)
			{
				UiMathUtility.Swap( ref tl, ref bl );
				UiMathUtility.Swap( ref tr, ref br );
			}

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				Vector2 pointNormalized = s_vertex.position.GetNormalizedPointInRect(Bounding);
				if (IsNaN(pointNormalized))
					continue;

				Vector2 point = s_vertex.position.Xy() + UiMathUtility.Lerp4P(tl, tr, bl, br, pointNormalized, false, true) * mirrorVec;
				s_vertex.position = new Vector3(point.x, point.y, s_vertex.position.z);

				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}

		private static bool IsNaN(Vector2 v)
		{
			return float.IsNaN(v.x) || float.IsNaN(v.y);
		}

		public void SetMirror( EAxis2DFlags _axes )
		{
			m_mirrorAxisFlags = _axes;
			SetDirty();
		}
	}

#if UNITY_EDITOR
	public abstract class UiDistortEditorBase : UnityEditor.Editor
	{
		protected SerializedProperty m_mirrorAxisFlagsProp;

		protected virtual bool HasMirror { get { return false; } }

		public virtual void OnEnable()
		{
			m_mirrorAxisFlagsProp = serializedObject.FindProperty("m_mirrorAxisFlags");
		}

		public override void OnInspectorGUI()
		{
			UiDistortBase thisUiDistort = (UiDistortBase)target;

			Edit(thisUiDistort);

			if (HasMirror)
			{
				if (EditorUiUtility.BoolBar<EAxis2DFlags>(m_mirrorAxisFlagsProp, "Mirror"))
					thisUiDistort.SetDirty();
			}

			Edit2(thisUiDistort);

			serializedObject.ApplyModifiedProperties();
		}

		protected abstract void Edit( UiDistortBase _thisUiDistortBase );
		protected virtual void Edit2( UiDistortBase _thisUiDistortBase ) { }

	}
#endif


}
