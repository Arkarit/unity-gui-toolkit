using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Whether a UiRoundedImage with a disabled material renders with it: only while the UiThing it belongs to is
	/// disabled in the hierarchy.
	///
	/// A shape's state used to be serialized false by default and was only written when its button changed state,
	/// so a shape with a disabled material under an enabled button looked disabled.
	/// </summary>
	[EditorAware]
	public class TestUiShapeImageEnabledInHierarchy
	{
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
		public void AShapeStoredDisabledFollowsItsEnabledButton()
		{
			var (button, shape, disabled) = ButtonWithShape();
			StoreEnabled(shape, false);

			shape.gameObject.SetActive(false);
			shape.gameObject.SetActive(true);

			Assert.That(button.EnabledInHierarchy, Is.True);
			Assert.That(shape.EnabledInHierarchy, Is.True);
			Assert.That(shape.materialForRendering.name, Does.Not.Contain(disabled.name));
		}

		[Test]
		public void AShapeStoredEnabledFollowsItsDisabledButton()
		{
			var (button, shape, disabled) = ButtonWithShape();
			button.EnabledInHierarchy = false;
			StoreEnabled(shape, true);

			shape.gameObject.SetActive(false);
			shape.gameObject.SetActive(true);

			Assert.That(shape.EnabledInHierarchy, Is.False);
			Assert.That(shape.materialForRendering.name, Does.Contain(disabled.name));
		}

		[Test]
		public void DisablingTheButtonStillSwitchesTheShape()
		{
			var (button, shape, disabled) = ButtonWithShape();

			button.EnabledInHierarchy = false;
			Assert.That(shape.materialForRendering.name, Does.Contain(disabled.name));

			button.EnabledInHierarchy = true;
			Assert.That(shape.materialForRendering.name, Does.Not.Contain(disabled.name));
		}

		[Test]
		public void AShapeWithoutAUiThingKeepsItsOwnState()
		{
			var canvas = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
			var shape = Track(new GameObject("Shape", typeof(RectTransform))).AddComponent<UiRoundedImage>();
			shape.transform.SetParent(canvas.transform, false);
			shape.DisabledMaterial = Track(new Material(Graphic.defaultGraphicMaterial) { name = "TestDisabled" });
			shape.EnabledInHierarchy = false;

			shape.gameObject.SetActive(false);
			shape.gameObject.SetActive(true);

			Assert.That(shape.EnabledInHierarchy, Is.False);
		}

		private (UiButton button, UiRoundedImage shape, Material disabled) ButtonWithShape()
		{
			var canvas = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
			var button = Track(new GameObject("Button", typeof(RectTransform))).AddComponent<UiButton>();
			button.transform.SetParent(canvas.transform, false);
			var shape = Track(new GameObject("Shape", typeof(RectTransform))).AddComponent<UiRoundedImage>();
			shape.transform.SetParent(button.transform, false);
			var disabled = Track(new Material(Graphic.defaultGraphicMaterial) { name = "TestDisabled" });
			shape.DisabledMaterial = disabled;
			return (button, shape, disabled);
		}

		// What an asset saved before the fix (false) or a stale one carries, without going through the utility.
		private static void StoreEnabled( UiRoundedImage _shape, bool _enabled )
			=> ((IEnableableInHierarchy)_shape).StoreEnabledInHierarchy = _enabled;

		private T Track<T>( T _obj ) where T : Object
		{
			m_created.Add(_obj);
			return _obj;
		}
	}
}
