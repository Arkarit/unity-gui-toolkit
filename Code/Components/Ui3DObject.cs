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

		[SerializeField]
		private EZSize m_zSize;

		public static readonly int s_propOffset = Shader.PropertyToID("_Offset");
		public static readonly int s_propScale = Shader.PropertyToID("_Scale");

		private MeshFilter m_meshFilter;
		private MeshRenderer m_meshRenderer;
		private RectTransform m_rectTransform;
		private MaterialCloner m_materialCloner;

		private Bounds m_originalBounds;

		private Rect m_previousRect = new Rect();

		private MaterialPropertyBlock m_materialPropertyBlock;

		public Material Material
		{
			get
			{
				if (m_materialCloner == null)
					Init();
				return m_materialCloner.Material;
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

		private void Init()
		{
			m_meshFilter = this.GetOrCreateComponent<MeshFilter>();
			m_meshRenderer = this.GetOrCreateComponent<MeshRenderer>();
			m_rectTransform =this.GetOrCreateComponent<RectTransform>();
			m_materialCloner =this.GetOrCreateComponent<MaterialCloner>();
			m_materialPropertyBlock = new MaterialPropertyBlock();

			m_originalBounds = RecalculateBounds();
		}

		private void SetMaterialProperties( Vector3 scale, Vector3 offset )
		{
			if (m_meshRenderer && m_materialCloner.Material.enableInstancing)
			{
				m_materialPropertyBlock.SetVector(s_propScale, scale);
				m_materialPropertyBlock.SetVector(s_propOffset, offset);
				m_meshRenderer.SetPropertyBlock(m_materialPropertyBlock);
				return;
			}

			m_materialCloner.Material.SetVector(s_propScale, scale);
			m_materialCloner.Material.SetVector(s_propOffset, offset);
		}

		private void AlignMaterialToRectTransformSize()
		{
			if (m_rectTransform == null || m_materialCloner == null || !m_materialCloner.Valid)
				return;

			if (!m_materialCloner.Material.HasProperty(s_propOffset) || !m_materialCloner.Material.HasProperty(s_propScale))
				return;

			Rect rect = m_rectTransform.rect;
			if (rect == m_previousRect)
				return;
			m_previousRect = rect;

			Vector3 scale = Vector4.one;
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
			Vector3 offset = -m_originalBounds.min;
			offset.Scale( scale );
			offset.x += rect.min.x;
			offset.y += rect.min.y;
			offset.z = 0;
			SetMaterialProperties(scale, offset);

			Bounds bounds = m_originalBounds;
			if (bounds.extents == Vector3.zero)
				return;

			Vector3 extents = bounds.extents;
			extents.Scale(scale);
			bounds.extents = extents;
			bounds.center += offset;
			if (m_meshFilter.sharedMesh)
				m_meshFilter.sharedMesh.bounds = bounds;
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
	public class Ui3DObjectEditor : Editor
	{
		protected SerializedProperty m_zSizeProp;

		public virtual void OnEnable()
		{
			m_zSizeProp = serializedObject.FindProperty("m_zSize");
		}

		public override void OnInspectorGUI()
		{
			Ui3DObject thisUi3DObject = (Ui3DObject)target;
			GameObject go = thisUi3DObject.gameObject;
			MaterialCloner materialCloner = go.GetComponent<MaterialCloner>();

			if (materialCloner == null)
				return;

			bool error = false;
			Material material = materialCloner.Material;

			if (!material.HasProperty(Ui3DObject.s_propOffset) || !material.HasProperty(Ui3DObject.s_propScale))
			{
				error = true;
				EditorGUILayout.HelpBox("Ui3DObject needs a material with _Offset and _Scale property support to work. You can assign UI_3D.mat to the Mesh Renderer, which supports this.\n" + 
					"Or you can examine Ui3D.shader how it's done. ", MessageType.Warning);
			}

			if (materialCloner.UseClonedMaterialsCache)
			{
				if (!material.enableInstancing)
				{
					error = true;
					EditorGUILayout.HelpBox("If 'Share Material between instances' is selected in the adjacent MaterialCloner script, " + 
						"Ui3DObject needs a material, which has 'GPU Instancing' enabled. Otherwise scaling will not work properly.", MessageType.Warning);
				}
			}

			if (error)
				return;

			int previousZSizeProp = m_zSizeProp.intValue;
			EditorGUILayout.PropertyField(m_zSizeProp);
			if (previousZSizeProp != m_zSizeProp.intValue)
				thisUi3DObject.SetDirty();

			serializedObject.ApplyModifiedProperties();

			
		}
	}
#endif

}