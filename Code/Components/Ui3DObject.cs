using System;
using System.Collections;
using UnityEngine;

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

		private static readonly int s_propOffset = Shader.PropertyToID("_Offset");
		private static readonly int s_propScale = Shader.PropertyToID("_Scale");

		private MeshFilter m_meshFilter;
		private MeshRenderer m_meshRenderer;
		private RectTransform m_rectTransform;
		private Material m_material;

		private Bounds m_originalBounds;

		public Material Material
		{
			get
			{
				if (m_material == null || m_material != m_meshRenderer.sharedMaterial)
					Init();
				return m_material;
			}
		}

		protected override void Awake()
		{
			Init();

			base.Awake();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetShaderProperties();
		}

		protected override void OnDisable()
		{
			m_meshFilter.sharedMesh.bounds = m_originalBounds;
			base.OnDisable();
		}

		protected virtual void Update()
		{
			SetShaderProperties();
		}

		private void Init()
		{
			m_meshFilter = GetComponent<MeshFilter>();
			m_meshRenderer = GetComponent<MeshRenderer>();
			m_rectTransform = GetComponent<RectTransform>();

			if (m_material == null || m_material == m_meshRenderer.sharedMaterial)
			{
				if (m_material != null)
					m_material.Destroy();
				m_material = new Material(m_meshRenderer.sharedMaterial);
				m_meshRenderer.sharedMaterial = m_material;
			}

			m_originalBounds = RecalculateBounds();
		}

		private void SetShaderProperties()
		{
			Rect rect = m_rectTransform.rect;
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
			m_material.SetVector( s_propScale, scale );
			Vector3 offset = -m_originalBounds.min;
			offset.Scale( scale );
			offset.x += rect.min.x;
			offset.y += rect.min.y;
			offset.z = 0;
			m_material.SetVector( s_propOffset, offset);

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

#if UNITY_EDITOR
		private void OnValidate()
		{
			Init();
		}
#endif

		private Bounds RecalculateBounds()
		{
			Bounds result = new Bounds();
			var vertices = m_meshFilter.sharedMesh.vertices;
			for (int i=0; i<vertices.Length; i++)
				result.Encapsulate(vertices[i]);
			return result;
		}
	}
}