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
	/// This means that when <c>AutoLocalize = true</c> and <c>text</c> is set,
	/// only <c>m_locaKey</c> is updated — <c>base.text</c> remains unchanged.
	/// Tests are written with this contract in mind.
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
		public void DefaultAutoLocalize_IsTrue()
		{
			Assert.IsTrue(m_component.AutoLocalize,
				"AutoLocalize must default to true");
		}

		[Test]
		public void DefaultGroup_IsEmpty()
		{
			Assert.AreEqual(string.Empty, m_component.Group,
				"Group must default to empty string");
		}

		[Test]
		public void AutoLocalizeFalse_TextProperty_SetsDirectly()
		{
			// When AutoLocalize is false the text setter passes straight through to base.
			m_component.AutoLocalize = false;
			m_component.text = "hello";
			Assert.AreEqual("hello", m_component.text,
				"When AutoLocalize=false, setting text must update the displayed text directly");
		}

		[Test]
		public void AutoLocalizeTrue_TextSetter_StoresKey()
		{
			// In EditMode ApplyTranslation() exits early, so base.text is NOT updated.
			// However m_locaKey IS stored so the component is ready to translate at runtime.
			m_component.AutoLocalize = true;
			m_component.text = "my_key";
			Assert.AreEqual("my_key", m_component.LocaKey,
				"When AutoLocalize=true, text setter must store the value as the localization key");
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
	}
}
