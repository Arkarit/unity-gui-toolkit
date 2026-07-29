using GuiToolkit.Editor.AiSupport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the JSON-level edit-preservation behind preserveEdits: a re-bake keeps hand edits (the prefab
	/// diverged from the state the baseline description baked to), the new JSON wins only where the author
	/// actually changed a value since that baseline, and a value merely equal to the baseline (i.e. generated,
	/// not hand-edited) is not resurrected.
	/// </summary>
	public class TestUiScreenBakerPreserveEdits
	{
		[Test]
		public void KeepsHandEditsButLetsNewJsonWin()
		{
			// Baseline = what the baker last generated.
			var baseline = Root(new JObject { ["layer"] = "Dialog" }, okText: null, okExtra: null);
			// Current = the prefab now: someone hand-added root.tint, changed okBtn text, and set okBtn.custom.
			var current = Root(new JObject { ["layer"] = "Dialog", ["tint"] = "#FF0000FF" }, okText: "@text:HAND", okExtra: 7);
			// New = the author's re-bake: same as baseline plus an explicit okBtn text (author wins there).
			var neu = Root(new JObject { ["layer"] = "Dialog" }, okText: "@text:NEW", okExtra: null);

			// This screen authors no styles, so the baseline description and the state it bakes to coincide.
			UiScreenBaker.MergePreservedEdits(neu, baseline, baseline, current);

			var rootProps = (JObject) neu["props"];
			Assert.AreEqual("#FF0000FF", (string) rootProps["tint"], "Hand-added root prop must be preserved.");
			Assert.AreEqual("Dialog", (string) rootProps["layer"], "Baseline-equal prop stays as the new JSON has it.");

			var ok = (JObject) ((JArray) neu["children"])[0];
			Assert.AreEqual("@text:NEW", (string) ok["text"], "The new JSON's explicit text must win over the hand edit.");
			Assert.AreEqual(7, (int) ((JObject) ok["props"])["custom"], "Hand-added child prop must be preserved.");
		}

		[Test]
		public void DoesNotResurrectAGeneratedPropTheAuthorRemoved()
		{
			// layer was generated (equal in baseline and current); the author removed it from the new JSON.
			var baseline = Root(new JObject { ["layer"] = "Dialog" }, null, null);
			var current = Root(new JObject { ["layer"] = "Dialog" }, null, null);
			var neu = Root(new JObject(), null, null);

			UiScreenBaker.MergePreservedEdits(neu, baseline, baseline, current);

			var rootProps = neu["props"] as JObject;
			Assert.IsTrue(rootProps == null || !rootProps.ContainsKey("layer"),
				"A prop equal to the baseline is generated, not a hand edit, so it must not be re-added.");
		}

		[Test]
		public void KeepsAHandResizedRectWhileTheDescriptionRepeatsItself()
		{
			// The description says 1800 as it did last time; the prefab says 1700 because a human resized it.
			var baseline = Root(new JObject(), null, null, rectWidth: 1800);
			var current = Root(new JObject(), null, null, rectWidth: 1700);
			var neu = Root(new JObject(), null, null, rectWidth: 1800);

			UiScreenBaker.MergePreservedEdits(neu, baseline, baseline, current);

			Assert.AreEqual(1700, (int) neu["rect"]["size"][0],
				"A description that only repeats itself must not overrule a hand-resized rect.");
		}

		[Test]
		public void LetsTheDescriptionWinWhereTheAuthorChangedTheRect()
		{
			// Same hand edit, but this time the author deliberately moved to 1600 — then the description decides.
			var baseline = Root(new JObject(), null, null, rectWidth: 1800);
			var current = Root(new JObject(), null, null, rectWidth: 1700);
			var neu = Root(new JObject(), null, null, rectWidth: 1600);

			UiScreenBaker.MergePreservedEdits(neu, baseline, baseline, current);

			Assert.AreEqual(1600, (int) neu["rect"]["size"][0],
				"Where the author changed the value since the baseline, the description wins.");
		}

		// Builds a { root: UiView(props, rect?) -> [ OkButton(text?, props{custom}?) ] } tree.
		private static JObject Root( JObject _rootProps, string okText, int? okExtra, int? rectWidth = null )
		{
			var ok = new JObject { ["template"] = "OkButton", ["id"] = "okBtn" };
			if (okText != null) ok["text"] = okText;
			if (okExtra != null) ok["props"] = new JObject { ["custom"] = okExtra.Value };

			var root = new JObject
			{
				["type"] = "UiView",
				["id"] = "root",
				["props"] = _rootProps,
				["children"] = new JArray { ok },
			};
			if (rectWidth != null)
				root["rect"] = new JObject { ["size"] = new JArray { rectWidth.Value, 0 } };

			return root;
		}
	}
}
