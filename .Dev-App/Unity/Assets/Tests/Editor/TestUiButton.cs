using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Tests for a UiButton whose cached Button reference has gone stale.
	///
	/// After a domain reload the console showed "MissingReferenceException: m_button of UiButton" from
	/// UiButtonBase.OnValidate: the init flag had survived, the Button behind the cached reference had
	/// not. The reload itself cannot be staged in a test, so the stale state is set up directly - a
	/// reference to a Button that has been destroyed, on a button that believes it is initialized.
	/// </summary>
	[EditorAware]
	public class TestUiButton
	{
		private static readonly FieldInfo s_buttonField =
			typeof(UiButton).GetField("m_button", BindingFlags.Instance | BindingFlags.NonPublic);

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
		public void AStaleButtonReference_DoesNotThrowOnEnabledChange()
		{
			var uiButton = CreateWithStaleReference();

			Assert.DoesNotThrow(() => uiButton.OnEnabledInHierarchyChanged(false));
			Assert.IsFalse(uiButton.GetComponent<Button>().interactable,
				"The change has to reach the live Button, not be dropped along with the stale one.");
		}

		[Test]
		public void AStaleButtonReference_IsResolvedAgain()
		{
			var uiButton = CreateWithStaleReference();

			Assert.AreSame(uiButton.GetComponent<Button>(), uiButton.Button);
		}

		private UiButton CreateWithStaleReference()
		{
			Assert.IsNotNull(s_buttonField, "UiButton.m_button was renamed - update this test.");

			var go = new GameObject("TestUiButton", typeof(RectTransform));
			m_created.Add(go);
			var uiButton = go.AddComponent<UiButton>();

			// Initialize, so the flag is set and the reference cached
			Assert.IsNotNull(uiButton.Button);

			var other = new GameObject("TestUiButton_Destroyed", typeof(RectTransform));
			var destroyed = other.AddComponent<Button>();
			Object.DestroyImmediate(other);
			s_buttonField.SetValue(uiButton, destroyed);

			return uiButton;
		}
	}
}
