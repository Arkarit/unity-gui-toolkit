using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Build;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for <c>LocaPreBuildProcessor</c>.
	/// The class lives in the Demo project's default editor assembly, so it is
	/// accessed via reflection to avoid a hard assembly dependency.
	/// Verifies the interface contract without triggering an actual build.
	/// </summary>
	public class TestLocaPreBuildProcessor
	{
		private static Type FindType()
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a =>
				{
					try { return a.GetTypes(); }
					catch { return Array.Empty<Type>(); }
				})
				.FirstOrDefault(t => t.Name == "LocaPreBuildProcessor");
		}

		[Test]
		public void LocaPreBuildProcessor_ImplementsIPreprocessBuildWithReport()
		{
			var type = FindType();
			Assert.IsNotNull(type, "LocaPreBuildProcessor type must exist in a loaded assembly");
			Assert.IsTrue(
				typeof(IPreprocessBuildWithReport).IsAssignableFrom(type),
				"LocaPreBuildProcessor must implement IPreprocessBuildWithReport");
		}

		[Test]
		public void LocaPreBuildProcessor_CallbackOrder_IsZero()
		{
			var type = FindType();
			Assert.IsNotNull(type, "LocaPreBuildProcessor type must exist in a loaded assembly");

			var instance = Activator.CreateInstance(type) as IPreprocessBuildWithReport;
			Assert.IsNotNull(instance, "Must be constructible as IPreprocessBuildWithReport");
			Assert.AreEqual(0, instance.callbackOrder,
				"callbackOrder must be 0 so the processor runs before default build steps");
		}
	}
}
