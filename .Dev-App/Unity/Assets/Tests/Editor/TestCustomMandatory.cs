using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	[EditorAware]
	public class TestCustomMandatory
	{
		private const string ChipError = "Chip is required unless 'Name As Title' is set.";

		private class Base : MonoBehaviour
		{
			public bool NameAsTitle;
			public Object Chip;

			private string ValidateChip() => !NameAsTitle && !Chip ? ChipError : null;
		}

		private class Derived : Base
		{
			[CustomMandatory("ValidateChip")] public Object Field;
			[CustomMandatory("DoesNotExist")] public Object Missing;
			[CustomMandatory(nameof(WrongSignature))] public Object Wrong;
			[CustomMandatory(nameof(Throws))] public Object Throwing;

			private int WrongSignature() => 0;
			private string Throws() => throw new System.InvalidOperationException("boom");
		}

		private GameObject m_go;
		private Derived m_component;

		[SetUp]
		public void SetUp()
		{
			m_go = new GameObject("CustomMandatory");
			m_component = m_go.AddComponent<Derived>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.DestroyImmediate(m_go);
		}

		private static CustomMandatoryAttribute Attr( string _field ) =>
			typeof(Derived).GetField(_field).GetCustomAttribute<CustomMandatoryAttribute>();

		[Test]
		public void Validator_Reports_Error_While_Condition_Unmet()
		{
			Assert.AreEqual(ChipError, Attr("Field").Validate(m_component));
		}

		[Test]
		public void Validator_Passes_When_Toggle_Set()
		{
			m_component.NameAsTitle = true;
			Assert.IsNull(Attr("Field").Validate(m_component));
		}

		[Test]
		public void Validator_Passes_When_Reference_Set()
		{
			m_component.Chip = m_go;
			Assert.IsNull(Attr("Field").Validate(m_component));
		}

		[Test]
		public void Missing_Or_Malformed_Validator_Is_An_Error()
		{
			StringAssert.Contains("not found", Attr("Missing").Validate(m_component));
			StringAssert.Contains("not found", Attr("Wrong").Validate(m_component));
		}

		[Test]
		public void Throwing_Validator_Is_An_Error()
		{
			StringAssert.Contains("boom", Attr("Throwing").Validate(m_component));
		}
	}
}
