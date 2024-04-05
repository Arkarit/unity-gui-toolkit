using System;
using System.Collections;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// \brief Integrate 3D objects into GUI
	/// 
	/// Unity allows a mix of UI and 3D objects, in a manner that 3D objects get a
	/// RectTransform applied when they are in an UI hierarchy.<BR>
	/// The support for 3D in the UI however is quite bad; Changing the size of the rect transform
	/// does not affect the 3D object at all.<BR>
	/// 
	/// Here's a simple and fast solution to overcome this.<BR>
	/// It is done via a cheap vertex shader.<BR>
	/// In that shader, the mesh is transformed in a way that it matches the RectTransform bounds in X and Y direction perfectly.
	/// The Z mesh scale can be either left untouched, or determined by the x, y or the average of the x and y scale.
	/// The cool thing with this is that the normals are completely unimpaired. You can have an object which is very flat in the Z axis,
	/// but due to its normals looks as if it is a regular 3D object. However, if you want to rotate the object in the x or y axis, 
	/// you should choose one of the other options.
	/// Note that the scale of the RectTransform is unimpaired; the whole scaling and transforming is done in the shader.
	/// 
	/// The downside of this approach is obviously, that you need a special shader for this.
	/// The provided UI_3D.mat provides this functionality; if you want to use it in your own shader it's quite simple to implement.
	/// See the provided Ui3D.shader for an example; examine USE_UI3D, _Offset and _Scale.

	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(MaterialCloner))]
	[ExecuteAlways]
	public class Ui3DObject : UiThing
	{
		/// \brief Defines the Z Size of the 3D object.
		/// 
		/// <UL>
		/// <LI>Untouched: Keeps the original Z size</LI>
		/// <LI>ByX: Scales the mesh Z size determined by X size</LI>
		/// <LI>ByX: Scales the mesh Z size determined by Y size</LI>
		/// <LI>ByXYAverage: Scales the mesh determined by an average of X and Y size</LI>
		/// </UL>
		public enum EZSize
		{
			Untouched,
			ByX,
			ByY,
			ByXYAverage,
		}

		[SerializeField] protected EZSize m_zSize;
		[SerializeField] protected float m_zSizeFactor = 1;

		public static readonly int s_propOffset = Shader.PropertyToID("_Offset");
		public static readonly int s_propScale = Shader.PropertyToID("_Scale");

		public EZSize ZSizePolicy
		{
			get => m_zSize;
			set
			{
				if (m_zSize == value)
					return;

				m_zSize = value;
				SetDirty();
			}
		}

		public float ZSizeFactor
		{
			get => m_zSizeFactor;
			set
			{
				if (m_zSizeFactor == value)
					return;

				m_zSizeFactor = value;
				SetDirty();
			}
		}

		private MeshFilter m_meshFilter;
		private MeshRenderer m_meshRenderer;
		private RectTransform m_rectTransform;
		private MaterialCloner m_materialCloner;
		private BoxCollider m_boxCollider;

		private Bounds m_originalBounds;

		private Rect m_previousRect = new Rect();
		private Material m_previousMaterial;

		private MaterialPropertyBlock m_materialPropertyBlock;

		public Material Material
		{
			get
			{
				if (m_materialCloner == null)
					Init();
				return m_materialCloner.ClonedMaterial;
			}
		}

		public void SetDirty()
		{
			m_previousRect = new Rect();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Init();
		}

		protected override void OnDisable()
		{
			m_meshFilter.sharedMesh.bounds = m_originalBounds;
			base.OnDisable();
		}

		protected virtual void Update()
		{
#if UNITY_EDITOR
			Init();
#endif
			AlignMaterialToRectTransformSize();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (m_meshRenderer)
				m_meshRenderer.SetPropertyBlock(null);
			if (m_meshFilter && m_meshFilter.sharedMesh)
				m_meshFilter.sharedMesh.bounds = RecalculateBounds();
		}

		private void Init()
		{
			m_meshFilter = this.GetOrCreateComponent<MeshFilter>();
			m_meshRenderer = this.GetOrCreateComponent<MeshRenderer>();
			m_rectTransform = this.GetOrCreateComponent<RectTransform>();
			m_materialCloner = this.GetOrCreateComponent<MaterialCloner>();
			m_materialPropertyBlock = new MaterialPropertyBlock();

			m_boxCollider = GetComponent<BoxCollider>();

			m_originalBounds = RecalculateBounds();
		}

		private void SetMaterialProperties( Vector4 _scale, Vector4 _offset )
		{
			if (m_meshRenderer && m_materialCloner.ClonedMaterial.enableInstancing)
			{
				m_meshRenderer.GetPropertyBlock(m_materialPropertyBlock);

				m_materialPropertyBlock.SetVector(s_propScale, _scale);
				m_materialPropertyBlock.SetVector(s_propOffset, _offset);

				m_meshRenderer.SetPropertyBlock(m_materialPropertyBlock);
				return;
			}

			m_meshRenderer.SetPropertyBlock(null);
			m_materialCloner.ClonedMaterial.SetVector(s_propScale, _scale);
			m_materialCloner.ClonedMaterial.SetVector(s_propOffset, _offset);
		}

		private void AlignMaterialToRectTransformSize()
		{
			if (m_rectTransform == null)
				return;

			Rect rect = m_rectTransform.rect;
			if (rect == m_previousRect && m_previousMaterial == Material)
				return;

			if (m_materialCloner == null || !m_materialCloner.Valid)
				return;

			m_previousRect = rect;
			m_previousMaterial = m_materialCloner.ClonedMaterial;

			if (!m_materialCloner.ClonedMaterial.HasProperty(s_propOffset) || !m_materialCloner.ClonedMaterial.HasProperty(s_propScale))
				return;

			Vector4 scale = Vector4.one;
			scale.x = rect.width / m_originalBounds.size.x;
			scale.y = rect.height / m_originalBounds.size.y;
			switch (m_zSize)
			{
				case EZSize.Untouched:
					break;
				case EZSize.ByX:
					scale.z = scale.x;
					break;
				case EZSize.ByY:
					scale.z = scale.y;
					break;
				case EZSize.ByXYAverage:
					scale.z = (scale.x + scale.y) * 0.5f;
					break;
				default:
					Debug.Assert(false);
					break;
			}
			scale.z *= m_zSizeFactor;
			scale.w = 1;

			Vector4 offset = -m_originalBounds.min;
			offset.Scale( scale );
			offset.x += rect.min.x;
			offset.y += rect.min.y;
			offset.z = offset.w = 0;
			SetMaterialProperties(scale, offset);

			Bounds bounds = m_originalBounds;
			if (bounds.extents == Vector3.zero)
				return;

			Vector3 extents = bounds.extents;
			extents.Scale(scale);
			bounds.extents = extents;
			bounds.center += offset.Xyz();
			if (m_meshFilter.sharedMesh)
				m_meshFilter.sharedMesh.bounds = bounds;

			if (m_boxCollider != null)
			{
				m_boxCollider.center = bounds.center;
				m_boxCollider.size = bounds.extents;
			}
		}

		private Bounds RecalculateBounds()
		{
			Bounds result = new Bounds();
			var vertices = m_meshFilter.sharedMesh.vertices;
			for (int i=0; i<vertices.Length; i++)
				result.Encapsulate(vertices[i]);
			return result;
		}
	}

#if UNITY_EDITOR
	/// \addtogroup Editor Code
	/// Ui3DObjectEditor can have several circumstances, under which it is technically impossible
	/// to work. This editor's purpose is to show some warning if these issues occur.
	[CustomEditor(typeof(Ui3DObject))]
	public class Ui3DObjectEditor : UiThingEditor
	{
		protected SerializedProperty m_zSizeProp;
		protected SerializedProperty m_zSizeFactorProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_zSizeProp = serializedObject.FindProperty("m_zSize");
			m_zSizeFactorProp = serializedObject.FindProperty("m_zSizeFactor");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			Ui3DObject thisUi3DObject = (Ui3DObject)target;
			GameObject go = thisUi3DObject.gameObject;
			MaterialCloner materialCloner = go.GetComponent<MaterialCloner>();

			if (materialCloner == null)
				return;

			if (EditorUiUtility.InfoBoxIfPrefab(go))
				return;

			bool error = false;
			Material material = materialCloner.ClonedMaterial;

			if (!material.HasProperty(Ui3DObject.s_propOffset) || !material.HasProperty(Ui3DObject.s_propScale))
			{
				error = true;
				EditorGUILayout.HelpBox("Ui3DObject needs a material with _Offset and _Scale property (for scaling the mesh) support to work.\n" + 
					"You can assign UI_3D.mat (which supports this feature) to the MaterialCloner on this game object.\n" + 
					"Or, you can examine Ui3D.shader how it's done. ", MessageType.Warning);
			}

			if (materialCloner.IsSharedMaterial)
			{
				if (!material.enableInstancing)
				{
					error = true;
					EditorGUILayout.HelpBox("If 'Share Material between instances' is selected in the MaterialCloner script on this game object," + 
						"Ui3DObject needs a material, which has 'GPU Instancing' enabled. Otherwise scaling will not work properly.", MessageType.Warning);
				}
			}

			if (error)
				return;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_zSizeProp);
			EditorGUILayout.PropertyField(m_zSizeFactorProp);

			if (EditorGUI.EndChangeCheck())
			{
				thisUi3DObject.SetDirty();
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
#endif

}