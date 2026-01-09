using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Debugging
{
	public class LoremIpsator : MonoBehaviour
	{
		private const string LoremIpsum =
			"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt " +
			"ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo " +
			"dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit " +
			"amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor " +
			"invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et " +
			"justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum " +
			"dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod " +
			"tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam " +
			"et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum " +
			"dolor sit amet.\r\n\r\n" +
			"Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum " +
			"dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit " +
			"praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit " +
			"amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore " +
			"magna aliquam erat volutpat.\r\n\r\n" +
			"Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut " +
			"aliquip ex ea commodo consequat. Duis autem vel eum iriure dolor in hendrerit in vulputate velit " +
			"esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et " +
			"iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla " +
			"facilisi.\r\n\r\n" +
			"Nam liber tempor cum soluta nobis eleifend option congue nihil imperdiet doming id quod mazim " +
			"placerat facer possim assum. Lorem";

		[SerializeField, Mandatory] private TMP_Text m_text;
		[SerializeField] private float m_frequencySeconds = 1;

		private float m_counter;

		private void Update()
		{
			m_counter += Time.deltaTime;
			if (m_counter >= m_frequencySeconds)
			{
				m_counter = 0;
				SetText();
			}
		}

		private void SetText()
		{
			var randomLength = Random.Range(30, LoremIpsum.Length);
			m_text.SetTextAndUpdateMesh(LoremIpsum.Substring(randomLength));
		}
	}
}