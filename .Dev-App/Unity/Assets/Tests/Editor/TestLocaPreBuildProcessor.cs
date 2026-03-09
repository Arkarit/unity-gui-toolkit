using NUnit.Framework;
using UnityEditor.Build;
using GuiToolkit;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for <see cref="LocaPreBuildProcessor"/>.
	/// Verifies the interface contract without triggering an actual build.
	/// </summary>
	public class TestLocaPreBuildProcessor
	{
		[Test]
		public void LocaPreBuildProcessor_ImplementsIPreprocessBuildWithReport()
		{
			Assert.IsTrue(
				typeof(IPreprocessBuildWithReport).IsAssignableFrom(typeof(LocaPreBuildProcessor)),
				"LocaPreBuildProcessor must implement IPreprocessBuildWithReport");
		}

		[Test]
		public void LocaPreBuildProcessor_CallbackOrder_IsZero()
		{
			var processor = new LocaPreBuildProcessor();
			Assert.AreEqual(0, processor.callbackOrder,
				"callbackOrder must be 0 so the processor runs before default build steps");
		}
	}
}
