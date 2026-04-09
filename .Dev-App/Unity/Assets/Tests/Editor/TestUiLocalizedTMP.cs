using NUnit.Framework;
using UnityEngine;
using GuiToolkit;

namespace GuiToolkit.Test
{
	/// <summary>
	/// EditMode tests for <see cref="UiLocalizedTextMeshProUGUI"/>.
	/// <para>
	/// Because <c>Application.isPlaying == false</c> in EditMode,
	/// <c>ApplyTranslation()</c> returns early without touching <c>base.text</c>.
	/// Setting <c>.text</c> stores the value in <c>m_locaKey</c> but does not update
	/// the displayed text — tests are written with this contract in mind.
	/// </para>
	/// </summary>
	public class TestUiLocalizedTMP
	{
		private GameObject m_gameObject;
		private UiLocalizedTextMeshProUGUI m_component;

		[SetUp]
		public void SetUp()
		{
			m_gameObject = new GameObject("TestTMP");
			m_component = m_gameObject.AddComponent<UiLocalizedTextMeshProUGUI>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_gameObject);
			m_gameObject = null;
			m_component = null;
		}

		[Test]
		public void DefaultLocaKey_IsEmpty()
		{
			Assert.AreEqual(string.Empty, m_component.LocaKey,
				"LocaKey must default to empty string");
		}

		[Test]
		public void DefaultGroup_IsEmpty()
		{
			Assert.AreEqual(string.Empty, m_component.Group,
				"Group must default to empty string");
		}

		[Test]
		public void TextSetter_StoresKey()
		{
			// In EditMode ApplyTranslation() exits early, so base.text is NOT updated.
			// However m_locaKey IS stored so the component is ready to translate at runtime.
			m_component.text = "my_key";
			Assert.AreEqual("my_key", m_component.LocaKey,
				"text setter must store the value as the localization key");
		}

		[Test]
		public void LocaKeyProperty_StoresKey()
		{
			m_component.LocaKey = "foo";
			Assert.AreEqual("foo", m_component.LocaKey,
				"LocaKey property must store and return the assigned key");
		}

		[Test]
		public void GroupProperty_CanBeSet()
		{
			m_component.Group = "my_group";
			Assert.AreEqual("my_group", m_component.Group,
				"Group property must store and return the assigned group name");
		}

		[Test]
		public void PlaceholderText_LocaKeyReturnsEmpty()
		{
			m_component.text = "[Text]";
			Assert.AreEqual(string.Empty, m_component.LocaKey,
				"Placeholder text in [brackets] must return empty LocaKey");
		}

		[Test]
		public void EmptyText_LocaKeyReturnsEmpty()
		{
			m_component.text = "";
			Assert.AreEqual(string.Empty, m_component.LocaKey,
				"Empty text must return empty LocaKey");
		}
	}
}
