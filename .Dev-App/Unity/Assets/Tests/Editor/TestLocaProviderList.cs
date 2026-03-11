using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using GuiToolkit;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Editor tests for <see cref="LocaProviderList"/> JSON deserialization and
	/// migration from the legacy string-paths format to typed <see cref="LocaProviderEntry"/> objects.
	/// <para>
	/// <see cref="LocaProviderList.Load"/> cannot be called directly in editor tests because it
	/// calls <c>Resources.Load</c>.  Instead the tests drive
	/// <see cref="JsonUtility.FromJson{T}"/> and replicate the migration step in-line.
	/// </para>
	/// </summary>
	public class TestLocaProviderList
	{
		[Test]
		public void OldJsonWithPaths_ProducesProviderEntries()
		{
			// Old format stores bare path strings under the "Paths" array.
			const string oldJson = "{\"Paths\":[\"LocaJson/MyBridge\"],\"Providers\":[]}";
			var list = JsonUtility.FromJson<LocaProviderList>(oldJson);

			// Replicate the migration step from LocaProviderList.Load()
			if (list.Providers.Count == 0 && list.Paths.Count > 0)
			{
				foreach (var path in list.Paths)
					list.Providers.Add(new LocaProviderEntry { Path = path });
				list.Paths.Clear();
			}

			Assert.AreEqual(1, list.Providers.Count,
				"Migration must produce exactly one provider entry from the old Paths array");
			Assert.AreEqual("LocaJson/MyBridge", list.Providers[0].Path,
				"Migrated provider path must match the original Paths entry");
		}

		[Test]
		public void NewJsonWithProviders_LoadsCorrectly()
		{
			// New format stores typed entries directly under the "Providers" array.
			const string newJson =
				"{\"Paths\":[],"
				+ "\"Providers\":[{\"Path\":\"LocaJson/MyBridge\","
				+ "\"TypeName\":\"GuiToolkit.LocaExcelBridge\"}]}";

			var list = JsonUtility.FromJson<LocaProviderList>(newJson);

			Assert.AreEqual(1, list.Providers.Count,
				"New-format JSON must deserialize to exactly one provider entry");
			Assert.AreEqual("LocaJson/MyBridge", list.Providers[0].Path);
			Assert.AreEqual("GuiToolkit.LocaExcelBridge", list.Providers[0].TypeName);
		}

		[Test]
		public void ProviderEntry_DefaultTypeName_IsLocaExcelBridge()
		{
			var entry = new LocaProviderEntry();
			Assert.AreEqual("GuiToolkit.LocaExcelBridge", entry.TypeName,
				"Default TypeName must be GuiToolkit.LocaExcelBridge for backward-compatibility");
		}
	}
}
