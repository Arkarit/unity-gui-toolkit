using NUnit.Framework;
using GuiToolkit;

namespace GuiToolkit.Test
{
	/// <summary>
	/// EditMode tests for <see cref="YamlUtility.PatchYaml"/>.
	/// All tests operate on synthetic YAML strings — no file I/O, no Unity import.
	/// </summary>
	public class TestYamlUtility
	{
		// -----------------------------------------------------------------------
		// Minimal synthetic Unity YAML fragments used across tests
		// -----------------------------------------------------------------------

		// A valid MonoBehaviour block preceded by a different block type.
		private const string OldGuid = "aaabbbccc111122223333444455556666";
		private const string NewGuid = "fffeeedddd555566667777888899990000";
		private const long TargetId = 2356781234L;

		private static string BuildYaml(long localId, string scriptGuid)
			=> $"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" +
			   $"--- !u!29 &1\nOcclusionCullingSettings:\n  serializedVersion: 2\n" +
			   $"--- !u!114 &{localId}\nMonoBehaviour:\n" +
			   $"  m_ObjectHideFlags: 0\n" +
			   $"  m_Script: {{fileID: 11500000, guid: {scriptGuid}, type: 3}}\n" +
			   $"  m_Name: \n" +
			   $"--- !u!1 &9876543210\nGameObject:\n  m_Name: MyObject\n";

		// -----------------------------------------------------------------------
		// Happy-path replacement
		// -----------------------------------------------------------------------

		[Test]
		public void PatchYaml_ValidBlock_ReturnsModifiedYaml()
		{
			string yaml = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.PatchYaml(yaml, TargetId, OldGuid, NewGuid);

			Assert.IsNotNull(result, "Must return a non-null string on success");
			StringAssert.Contains($"guid: {NewGuid}, type:", result,
				"Result must contain the new script GUID");
			StringAssert.DoesNotContain($"guid: {OldGuid}, type:", result,
				"Result must not contain the old script GUID anymore");
		}

		[Test]
		public void PatchYaml_ValidBlock_PreservesContentOutsideBlock()
		{
			string yaml = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.PatchYaml(yaml, TargetId, OldGuid, NewGuid);

			// The surrounding YAML blocks must be unchanged.
			StringAssert.Contains("OcclusionCullingSettings:", result,
				"Block before the target must be preserved");
			StringAssert.Contains("m_Name: MyObject", result,
				"Block after the target must be preserved");
		}

		// -----------------------------------------------------------------------
		// Block not found
		// -----------------------------------------------------------------------

		[Test]
		public void PatchYaml_WrongLocalId_ReturnsNull()
		{
			string yaml = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.PatchYaml(yaml, 9999999999L, OldGuid, NewGuid);

			Assert.IsNull(result, "Must return null when the block is not found");
		}

		// -----------------------------------------------------------------------
		// GUID not found inside the correct block
		// -----------------------------------------------------------------------

		[Test]
		public void PatchYaml_WrongOldGuid_ReturnsNull()
		{
			string yaml = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.PatchYaml(yaml, TargetId, "wrongguid000000000000000000000000", NewGuid);

			Assert.IsNull(result, "Must return null when the old GUID is not present in the block");
		}

		// -----------------------------------------------------------------------
		// Multiple MonoBehaviour blocks — only the target block must be touched
		// -----------------------------------------------------------------------

		[Test]
		public void PatchYaml_MultipleBlocks_OnlyReplacesTargetBlock()
		{
			const long otherId = 1111111111L;
			string yaml = BuildYaml(TargetId, OldGuid) +
			              $"--- !u!114 &{otherId}\nMonoBehaviour:\n" +
			              $"  m_Script: {{fileID: 11500000, guid: {OldGuid}, type: 3}}\n" +
			              $"  m_Name: \n";

			string result = YamlUtility.PatchYaml(yaml, TargetId, OldGuid, NewGuid);

			Assert.IsNotNull(result);

			// Target block must have the new GUID.
			int targetBlockIdx = result.IndexOf($"--- !u!114 &{TargetId}");
			int otherBlockIdx = result.IndexOf($"--- !u!114 &{otherId}");

			string targetBlock = result.Substring(targetBlockIdx, otherBlockIdx - targetBlockIdx);
			StringAssert.Contains($"guid: {NewGuid}, type:", targetBlock,
				"Target block must contain the new GUID");

			// Other block must still have the old GUID.
			string otherBlock = result.Substring(otherBlockIdx);
			StringAssert.Contains($"guid: {OldGuid}, type:", otherBlock,
				"Non-target block must still contain the old GUID");
		}

		// -----------------------------------------------------------------------
		// Block is the last block in the file (no trailing "\n---")
		// -----------------------------------------------------------------------

		[Test]
		public void PatchYaml_TargetIsLastBlock_Replaces()
		{
			// Build YAML where the target MonoBehaviour is the last block (nothing after it).
			string yaml = $"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" +
			              $"--- !u!114 &{TargetId}\nMonoBehaviour:\n" +
			              $"  m_Script: {{fileID: 11500000, guid: {OldGuid}, type: 3}}\n";

			string result = YamlUtility.PatchYaml(yaml, TargetId, OldGuid, NewGuid);

			Assert.IsNotNull(result);
			StringAssert.Contains($"guid: {NewGuid}, type:", result);
		}

		// -----------------------------------------------------------------------
		// Does not accidentally replace other GUID fields (e.g. m_GameObject references)
		// -----------------------------------------------------------------------

		[Test]
		public void PatchYaml_DoesNotReplaceNonScriptGuids()
		{
			// In real Unity YAML, m_Script always appears before other serialized fields.
			// Placing it first means PatchYaml's first-match strategy replaces the right token.
			string yaml = $"%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n" +
			              $"--- !u!114 &{TargetId}\nMonoBehaviour:\n" +
			              $"  m_Script: {{fileID: 11500000, guid: {OldGuid}, type: 3}}\n" +
			              $"  m_SomeRef: {{fileID: 100, guid: {OldGuid}, type: 2}}\n";

			string result = YamlUtility.PatchYaml(yaml, TargetId, OldGuid, NewGuid);

			Assert.IsNotNull(result);
			// m_Script line must have the new GUID.
			StringAssert.Contains($"guid: {NewGuid}, type: 3", result,
				"m_Script GUID must be replaced");
			// m_SomeRef (type: 2) must still carry the old GUID — only the first match is replaced.
			StringAssert.Contains($"guid: {OldGuid}, type: 2", result,
				"Non-script reference GUID must be left untouched");
		}

		// -----------------------------------------------------------------------
		// Empty / null inputs
		// -----------------------------------------------------------------------

		[Test]
		public void PatchYaml_EmptyYaml_ReturnsNull()
		{
			string result = YamlUtility.PatchYaml(string.Empty, TargetId, OldGuid, NewGuid);
			Assert.IsNull(result);
		}

		// -----------------------------------------------------------------------
		// ReplaceMonoBehaviourBlock
		// -----------------------------------------------------------------------

		private static string BuildNewBlockContent(string marker = "REPLACED")
			=> $"MonoBehaviour:\n  m_marker: {marker}\n";

		[Test]
		public void ReplaceMonoBehaviourBlock_ValidBlock_ReplacesContent()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.ReplaceMonoBehaviourBlock(yaml, TargetId, BuildNewBlockContent());

			Assert.IsNotNull(result, "Must return a non-null string on success");
			StringAssert.Contains("m_marker: REPLACED", result, "New content must appear in result");
			StringAssert.DoesNotContain($"guid: {OldGuid}", result, "Old block content must be gone");
		}

		[Test]
		public void ReplaceMonoBehaviourBlock_PreservesHeaderAnchor()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.ReplaceMonoBehaviourBlock(yaml, TargetId, BuildNewBlockContent());

			StringAssert.Contains($"--- !u!114 &{TargetId}", result,
				"The separator line / anchor must be preserved");
		}

		[Test]
		public void ReplaceMonoBehaviourBlock_PreservesBlocksBefore()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.ReplaceMonoBehaviourBlock(yaml, TargetId, BuildNewBlockContent());

			StringAssert.Contains("OcclusionCullingSettings:", result, "Block before target must be preserved");
		}

		[Test]
		public void ReplaceMonoBehaviourBlock_PreservesBlocksAfter()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.ReplaceMonoBehaviourBlock(yaml, TargetId, BuildNewBlockContent());

			StringAssert.Contains("m_Name: MyObject", result, "Block after target must be preserved");
		}

		[Test]
		public void ReplaceMonoBehaviourBlock_WrongId_ReturnsNull()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.ReplaceMonoBehaviourBlock(yaml, 9999999999L, BuildNewBlockContent());

			Assert.IsNull(result, "Must return null when the block is not found");
		}

		[Test]
		public void ReplaceMonoBehaviourBlock_TargetIsLastBlock_Replaces()
		{
			string yaml   = $"--- !u!114 &{TargetId}\nMonoBehaviour:\n  m_Script: old\n";
			string result = YamlUtility.ReplaceMonoBehaviourBlock(yaml, TargetId, BuildNewBlockContent());

			Assert.IsNotNull(result);
			StringAssert.Contains("m_marker: REPLACED", result);
		}

		[Test]
		public void ReplaceMonoBehaviourBlock_OnlyTargetBlockAffected()
		{
			const long otherId = 9876543210L;
			string yaml = BuildYaml(TargetId, OldGuid) +
			              $"--- !u!114 &{otherId}\nMonoBehaviour:\n  m_Name: OtherBlock\n";

			string result = YamlUtility.ReplaceMonoBehaviourBlock(yaml, TargetId, BuildNewBlockContent());

			Assert.IsNotNull(result);
			StringAssert.Contains("m_Name: OtherBlock", result, "Non-target block must be untouched");
			StringAssert.DoesNotContain($"guid: {OldGuid}", result, "Target block must be replaced");
		}

		// -----------------------------------------------------------------------
		// RemoveMonoBehaviourBlock
		// -----------------------------------------------------------------------

		[Test]
		public void RemoveMonoBehaviourBlock_ValidBlock_RemovesBlock()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.RemoveMonoBehaviourBlock(yaml, TargetId);

			Assert.IsNotNull(result, "Must return a non-null string on success");
			StringAssert.DoesNotContain($"--- !u!114 &{TargetId}", result, "Separator must be removed");
			StringAssert.DoesNotContain($"guid: {OldGuid}", result, "Block content must be removed");
		}

		[Test]
		public void RemoveMonoBehaviourBlock_PreservesBlocksBefore()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.RemoveMonoBehaviourBlock(yaml, TargetId);

			StringAssert.Contains("OcclusionCullingSettings:", result, "Block before target must be preserved");
		}

		[Test]
		public void RemoveMonoBehaviourBlock_PreservesBlocksAfter()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.RemoveMonoBehaviourBlock(yaml, TargetId);

			StringAssert.Contains("m_Name: MyObject", result, "Block after target must be preserved");
		}

		[Test]
		public void RemoveMonoBehaviourBlock_WrongId_ReturnsNull()
		{
			string yaml   = BuildYaml(TargetId, OldGuid);
			string result = YamlUtility.RemoveMonoBehaviourBlock(yaml, 9999999999L);

			Assert.IsNull(result, "Must return null when the block is not found");
		}

		[Test]
		public void RemoveMonoBehaviourBlock_OnlyTargetRemoved()
		{
			const long otherId = 9876543210L;
			string yaml = BuildYaml(TargetId, OldGuid) +
			              $"--- !u!114 &{otherId}\nMonoBehaviour:\n  m_Name: Keeper\n";

			string result = YamlUtility.RemoveMonoBehaviourBlock(yaml, TargetId);

			Assert.IsNotNull(result);
			StringAssert.Contains($"--- !u!114 &{otherId}", result, "Non-target block must remain");
			StringAssert.Contains("m_Name: Keeper", result, "Non-target block content must remain");
		}

		[Test]
		public void RemoveMonoBehaviourBlock_TargetIsLastBlock_RemovesBlock()
		{
			string yaml   = $"--- !u!29 &1\nOcclusionCullingSettings:\n  a: 1\n" +
			                $"--- !u!114 &{TargetId}\nMonoBehaviour:\n  m_Name: Last\n";
			string result = YamlUtility.RemoveMonoBehaviourBlock(yaml, TargetId);

			Assert.IsNotNull(result);
			StringAssert.Contains("OcclusionCullingSettings:", result);
			StringAssert.DoesNotContain($"&{TargetId}", result);
		}
	}
}
