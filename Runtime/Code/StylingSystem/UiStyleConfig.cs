using UnityEngine;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	public class UiStyleConfig : MonoBehaviour
	{
		public static UiStyleConfig Instance => UiToolkitConfiguration.Instance.m_styleConfig;
	}
}