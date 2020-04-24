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
	[RequireComponent(typeof(MeshFilter))]
	[RequireComponent(typeof(RectTransform))]
	[ExecuteAlways]
	public class Ui3DObject : UiThing
	{
		private MeshFilter m_meshFilter;
		private MeshRenderer m_meshRenderer;
		private RectTransform m_rectTransform;
		private Mesh m_originalMesh;
		private Mesh m_clonedMesh;

		protected override void Awake()
		{
			m_meshFilter = GetComponent<MeshFilter>();
			m_meshRenderer = GetComponent<MeshRenderer>();
			m_rectTransform = GetComponent<RectTransform>();
			m_originalMesh = m_meshFilter.sharedMesh;
			m_clonedMesh = Instantiate(m_originalMesh);
//			m_meshRenderer.Me

			base.Awake();
		}

		protected override void OnEnable()
		{
			ModifyMesh();
			base.OnEnable();
		}

		protected override void Update()
		{
			base.Update();
			ModifyMesh();
		}

		private void ModifyMesh()
		{
			Bounds originalBounds = m_originalMesh.bounds;
			var vertices = m_originalMesh.vertices;
			for( int i=0; i<vertices.Length; i++ )
			{
				Vector3 vertex = vertices[i];
				vertex -= originalBounds.min;
				Vector3 vertexNormalized = Normalize(vertex, originalBounds.extents);
				vertexNormalized.x *= m_rectTransform.sizeDelta.x;
				vertexNormalized.y *= m_rectTransform.sizeDelta.y;
				vertex = Denormalize(vertexNormalized, originalBounds.extents);
				vertex += originalBounds.min;
				vertices[i] = vertex;
			}
			m_meshFilter.mesh.vertices = vertices;
		}

		private Vector3 Normalize(Vector3 _vec, Vector3 _extents)
		{
			Vector3 result;
			result.x = _vec.x / _extents.x;
			result.y = _vec.y / _extents.y;
			result.z = _vec.z / _extents.z;
			return result;
		}

		private Vector3 Denormalize(Vector3 _normalized, Vector3 _extents)
		{
			return Vector3.Scale(_normalized, _extents);
		}

	}
}