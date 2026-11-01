using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// The stencil compare a UiRoundedImage renders with, as a Mask, inside a Mask and inverted.
	///
	/// A shape used to overwrite the compare with Equal whatever it got from Unity, so a shape carrying a
	/// Mask tested against a stencil nobody had written yet, never wrote it, and hid all its children.
	/// Each case here is checked against what a plain Image gets in the same place, since Unity's
	/// Mask/MaskableGraphic are the reference for everything but the inversion.
	/// </summary>
	[EditorAware]
	public class TestUiShapeImageStencil
	{
		private const string StencilComp = "_StencilComp";

		private readonly List<Object> m_created = new();

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_created)
			{
				if (obj != null)
					Object.DestroyImmediate(obj);
			}

			m_created.Clear();
		}

		[Test]
		public void AShapeWithAMaskWritesTheStencilLikeAnImage()
		{
			var root = Root();
			var image = Masked<Image>(root.transform);
			var shape = Masked<UiRoundedImage>(root.transform);

			// Both share one pooled stencil material, so a shape that wrote into it changed the image's value
			// too: compare against the constant, never against the image read afterwards.
			Assert.That(Comp(image), Is.EqualTo((int)CompareFunction.Always));
			Assert.That(Comp(shape), Is.EqualTo((int)CompareFunction.Always));
			Assert.That(shape.materialForRendering.GetInt("_StencilOp"), Is.EqualTo((int)StencilOp.Replace));
			Assert.That(Comp(image), Is.EqualTo((int)CompareFunction.Always), "the shape changed the image's shared mask material");
		}

		[Test]
		public void AShapeInsideAMaskTestsTheStencilLikeAnImage()
		{
			var root = Root();
			var mask = Masked<Image>(root.transform);
			var image = Child<Image>(mask.transform);
			var shape = Child<UiRoundedImage>(mask.transform);

			Assert.That(Comp(shape), Is.EqualTo((int)CompareFunction.Equal));
			Assert.That(shape.materialForRendering.GetInt("_Stencil"), Is.EqualTo(image.materialForRendering.GetInt("_Stencil")));
		}

		[Test]
		public void AnInvertedShapeInsideAMaskDrawsOutsideIt()
		{
			var root = Root();
			var mask = Masked<Image>(root.transform);
			var image = Child<Image>(mask.transform);
			var shape = Child<UiRoundedImage>(mask.transform);
			shape.InvertMask = true;

			Assert.That(Comp(shape), Is.EqualTo((int)CompareFunction.NotEqual));
			Assert.That(Comp(image), Is.EqualTo((int)CompareFunction.Equal), "the inversion leaked into a sibling's material");
		}

		[Test]
		public void AShapeLeavesTheDefaultMaterialAlone()
		{
			var root = Root();
			var material = Graphic.defaultGraphicMaterial;
			int before = material.GetInt(StencilComp);
			material.SetInt(StencilComp, (int)CompareFunction.Always);
			try
			{
				var shape = Child<UiRoundedImage>(root.transform);
				shape.maskable = false;
				_ = shape.materialForRendering;
				shape.maskable = true;
				_ = shape.materialForRendering;

				Assert.That(material.GetInt(StencilComp), Is.EqualTo((int)CompareFunction.Always));
			}
			finally
			{
				material.SetInt(StencilComp, before);
			}
		}

		private static int Comp( Graphic _graphic ) => _graphic.materialForRendering.GetInt(StencilComp);

		private GameObject Root()
		{
			var root = new GameObject("StencilTestRoot", typeof(RectTransform), typeof(Canvas));
			m_created.Add(root);
			return root;
		}

		private T Masked<T>( Transform _parent ) where T : Graphic
		{
			var graphic = Child<T>(_parent);
			graphic.gameObject.AddComponent<Mask>();
			return graphic;
		}

		private static T Child<T>( Transform _parent ) where T : Graphic
		{
			var go = new GameObject(typeof(T).Name, typeof(RectTransform));
			go.transform.SetParent(_parent, false);
			return go.AddComponent<T>();
		}
	}
}
