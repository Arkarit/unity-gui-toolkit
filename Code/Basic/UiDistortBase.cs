using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class UiDistortBase : BaseMeshEffectTMP
	{
		[SerializeField]
		public Vector2 m_topLeft = Vector2.zero;

		[SerializeField]
		public Vector2 m_topRight = Vector2.zero;

		[SerializeField]
		public Vector2 m_bottomLeft = Vector2.zero;

		[SerializeField]
		public Vector2 m_bottomRight = Vector2.zero;

		[SerializeField]
		protected EDirectionFlags m_mirrorDirection;

		protected static readonly List<UIVertex> s_verts = new List<UIVertex>();
		protected static UIVertex s_vertex;
		protected Canvas m_canvas;

		protected virtual bool IsAbsolute { get { return false; } }
		protected virtual bool NeedsWorldBoundingBox { get { return false; } }
		protected virtual void Prepare( Rect _bounding ) {}

		public Rect Bounding {get; protected set;}

		protected override void Awake()
		{
			base.Awake();
			m_canvas = GetComponentInParent<Canvas>();
		}

		public override void ModifyMesh( VertexHelper _vertexHelper )
		{
			if (!IsActive())
				return;

			_vertexHelper.GetUIVertexStream(s_verts);

			Bounding = UiModifierUtil.GetBounds(s_verts);

			if (NeedsWorldBoundingBox)
			{
				Rect worldRect2D = Bounding.GetWorldRect2D( m_rectTransform, m_canvas );
				Prepare( worldRect2D );
			}
			else
			{
				Prepare( Bounding );
			}

			Vector2 size = IsAbsolute ? Vector2.one : Bounding.size;

			Vector2 tl = m_topLeft * size;
			Vector2 bl = m_bottomLeft * size;
			Vector2 tr = m_topRight * size;
			Vector2 br = m_bottomRight * size;

			bool mirrorHorizontal = m_mirrorDirection.IsFlagSet(EDirectionFlags.Horizontal);
			bool mirrorVertical = m_mirrorDirection.IsFlagSet(EDirectionFlags.Vertical);

			Vector2 mirrorVec = new Vector2(mirrorHorizontal ? -1 : 1, mirrorVertical ? -1 : 1);

			if (mirrorHorizontal)
			{
				UiMath.Swap( ref tl, ref tr );
				UiMath.Swap( ref bl, ref br );
			}

			if (mirrorVertical)
			{
				UiMath.Swap( ref tl, ref bl );
				UiMath.Swap( ref tr, ref br );
			}

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				Vector2 pointNormalized = s_vertex.position.GetNormalizedPointInRect(Bounding);
				Vector2 point = s_vertex.position.Xy() + UiMath.Lerp4P(tl, tr, bl, br, pointNormalized, false, true) * mirrorVec;
				s_vertex.position = new Vector3(point.x, point.y, s_vertex.position.z);

				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}

		public void SetMirror( EDirectionFlags _direction )
		{
			m_mirrorDirection = _direction;
			SetDirty();
		}
	}

#if UNITY_EDITOR
	public abstract class UiDistortEditorBase : Editor
	{
		protected SerializedProperty m_topLeftProp;
		protected SerializedProperty m_topRightProp;
		protected SerializedProperty m_bottomLeftProp;
		protected SerializedProperty m_bottomRightProp;
		protected SerializedProperty m_mirrorDirectionProp;

		protected virtual bool HasMirror { get { return false; } }

		public virtual void OnEnable()
		{
			m_topLeftProp = serializedObject.FindProperty("m_topLeft");
			m_topRightProp = serializedObject.FindProperty("m_topRight");
			m_bottomLeftProp = serializedObject.FindProperty("m_bottomLeft");
			m_bottomRightProp = serializedObject.FindProperty("m_bottomRight");
			m_mirrorDirectionProp = serializedObject.FindProperty("m_mirrorDirection");
		}

		public override void OnInspectorGUI()
		{
			UiDistortBase thisUiDistort = (UiDistortBase)target;

			Edit(thisUiDistort);

			if (HasMirror)
			{
				if (UiEditorUtility.BoolBar<EDirectionFlags>(m_mirrorDirectionProp, "Mirror"))
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
