using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using GuiToolkit.Style;
using NUnit.Framework;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// A style config serialised before its style type gained a value holds null there: Unity runs no field
	/// initialiser for [SerializeReference] fields it loads. The generated getters have to create the value at
	/// runtime too, or every Apply throws in Play Mode and in a player.
	///
	/// This has to be a Play Mode test. An Edit Mode test passes with the broken getter as well, because that
	/// one only creates while !Application.isPlaying - which is exactly how it came back unnoticed once (a merge
	/// in March 2025 restored the editor-only form that d2e66fd6 had removed).
	/// </summary>
	public class TestUiStyleValuesAtRuntime
	{
		[Test]
		public void Generated_Style_Getters_Create_Missing_Values_In_Play_Mode()
		{
			Assert.IsTrue(Application.isPlaying, "Must run in Play Mode, see class comment.");

			var styleTypes = typeof(UiAbstractStyleBase).Assembly.GetTypes()
				.Where(t => !t.IsAbstract && typeof(UiAbstractStyleBase).IsAssignableFrom(t))
				.ToList();
			Assert.IsNotEmpty(styleTypes);

			int checkedValues = 0;
			foreach (var type in styleTypes)
			{
				// Without constructor and field initialisers: all values null, like a config from before the value existed.
				var style = FormatterServices.GetUninitializedObject(type);

				foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					if (!IsApplicableValue(property.PropertyType) || property.GetIndexParameters().Length > 0)
					{
						continue;
					}

					Assert.IsNotNull(property.GetValue(style), $"{type.Name}.{property.Name} returns null for a value that was never created");
					checkedValues++;
				}
			}

			Assert.Greater(checkedValues, 0);
		}

		private static bool IsApplicableValue( Type _type )
		{
			return _type.IsGenericType && _type.GetGenericTypeDefinition() == typeof(ApplicableValue<>);
		}
	}
}
