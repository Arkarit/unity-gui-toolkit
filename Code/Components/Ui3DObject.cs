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
	/// Caveat: This does NOT affect camera frustum culling, leading to the object to disappear "randomly" at screen borders.
	/// The mesh may appear big on the screen, but logically it can be still very small to the camera, and also has
	/// a different offset. So it disappears when its original borders are not visible to the camera anymore - although
	/// the mesh itself may be still visible on the screen.
	/// 
	/// </summary>
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class Ui3DObject : UiThing, IExcludeFromFrustumCulling
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

		protected override void Awake()
		{
			m_meshFilter = GetComponent<MeshFilter>();
			m_meshRenderer = GetComponent<MeshRenderer>();
			m_rectTransform = GetComponent<RectTransform>();
			m_material = new Material(m_meshRenderer.sharedMaterial);
			m_meshRenderer.sharedMaterial = m_material;

			base.Awake();
		}

		protected override void OnEnable()
		{
			UiMain.Instance.RegisterExcludeFromFrustumCulling(this);
			SetShaderProperties();
		}

		protected override void OnDisable()
		{
			UiMain.Instance.UnregisterExcludeFromFrustumCulling(this);
			base.OnDisable();
		}

		protected override void Update()
		{
			base.Update();
			SetShaderProperties();
		}

		private void SetShaderProperties()
		{
			Bounds bounds = m_meshFilter.sharedMesh.bounds;
			Rect rect = m_rectTransform.rect;
			Vector4 scale = Vector4.one;
			scale.x = rect.width / bounds.size.x;
			scale.y = rect.height / bounds.size.y;
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
			Vector4 offset = -bounds.min;
			offset.Scale( scale );
			offset.x += rect.min.x;
			offset.y += rect.min.y;
			offset.z = 0;
			m_material.SetVector( s_propOffset, offset);
		}

		public Mesh GetMesh()
		{
			return m_meshFilter.sharedMesh;
		}
	}
}