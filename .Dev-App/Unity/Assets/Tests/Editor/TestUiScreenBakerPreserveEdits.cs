using GuiToolkit.Editor.AiSupport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the JSON-level edit-preservation behind preserveEdits: a re-bake keeps hand edits (props/text
	/// that diverged from the baseline and that the new JSON doesn't specify), while the new JSON wins wherever
	/// it does specify a value, and a value merely equal to the baseline (i.e. generated, not hand-edited) is
	/// not resurrected.
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

			UiScreenBaker.MergePreservedEdits(neu, baseline, current);

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

			UiScreenBaker.MergePreservedEdits(neu, baseline, current);

			var rootProps = neu["props"] as JObject;
			Assert.IsTrue(rootProps == null || !rootProps.ContainsKey("layer"),
				"A prop equal to the baseline is generated, not a hand edit, so it must not be re-added.");
		}

		// Builds a { root: UiView(props) -> [ OkButton(text?, props{custom}?) ] } tree.
		private static JObject Root( JObject _rootProps, string okText, int? okExtra )
		{
			var ok = new JObject { ["template"] = "OkButton", ["id"] = "okBtn" };
			if (okText != null) ok["text"] = okText;
			if (okExtra != null) ok["props"] = new JObject { ["custom"] = okExtra.Value };

			return new JObject
			{
				["type"] = "UiView",
				["id"] = "root",
				["props"] = _rootProps,
				["children"] = new JArray { ok },
			};
		}
	}
}
