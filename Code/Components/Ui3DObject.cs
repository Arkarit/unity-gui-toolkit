using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Unity allows a mix of UI and 3D objects, in a manner that 3D objects get a
	/// RectTransform applied when they are in an UI hierarchy.
	/// The support for 3D in the UI however is very bad.
	/// 
	/// </summary>
	[RequireComponent(typeof(MeshRenderer))]
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class Ui3DObject : UiThing
	{
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
			base.OnEnable();
			SetShader();
		}

		protected override void Update()
		{
			base.Update();
			SetShader();
		}

		private void SetShader()
		{
			Mesh mesh = m_meshFilter.sharedMesh;
			Bounds bounds = mesh.bounds;
			Vector4 scale = Vector4.one;
			scale.x = m_rectTransform.sizeDelta.x / bounds.size.x;
			scale.y = m_rectTransform.sizeDelta.y / bounds.size.y;
			m_material.SetVector( s_propScale, scale );
			Vector4 offset = -bounds.min;
			offset.Scale( scale );
			m_material.SetVector( s_propOffset, offset);
		}

	}
}