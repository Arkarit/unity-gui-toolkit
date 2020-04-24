using System;
using System.Collections;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Unity allows a mix of UI and 3D objects, in a manner that 3D objects get a
	/// RectTransform applied when they are in an UI hierarchy.
	/// The support for 3D in the UI however is very bad; Changing the size of the rect transform
	/// does not affect the 3D object at all.
	/// 
	/// Here's a simple and fast solution to overcome this.
	/// It is done via a cheap vertex shader.
	/// In that shader, the mesh is transformed in a way that it matches the RectTransform bounds perfectly.
	/// 
	/// </summary>
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class Ui3DObject : UiThing
	{
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


		protected override void Awake()
		{
			Init();

			base.Awake();
		}

		protected override void OnEnable()
		{
			SetShaderProperties();
		}

		protected override void OnDisable()
		{
			m_meshFilter.sharedMesh.bounds = m_originalBounds;
			base.OnDisable();
		}

		protected override void Update()
		{
			base.Update();
			SetShaderProperties();
		}

		private void Init()
		{
			m_meshFilter = GetComponent<MeshFilter>();
			m_meshRenderer = GetComponent<MeshRenderer>();
			m_rectTransform = GetComponent<RectTransform>();
			m_material = new Material(m_meshRenderer.sharedMaterial);
			m_meshRenderer.sharedMaterial = m_material;
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
			//Debug.Log($"Scale:{scale} Offset:{offset} m_originalBounds:{m_originalBounds} Bounds:{bounds}");
		}

		private void OnValidate()
		{
			Init();
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
}