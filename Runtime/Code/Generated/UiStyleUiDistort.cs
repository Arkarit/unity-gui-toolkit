// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiDistort : UiAbstractStyle<GuiToolkit.UiDistort>
	{
		public UiStyleUiDistort(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueEAxis2DFlags : ApplicableValue<GuiToolkit.EAxis2DFlags> {}
		private class ApplicableValueVector2 : ApplicableValue<UnityEngine.Vector2> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				m_IsAbsolute,
				m_Mirror,
				m_TopLeft,
				m_TopRight,
				m_BottomLeft,
				m_BottomRight,
			};
		}

		[SerializeReference] private ApplicableValueBoolean m_IsAbsolute = new();
		[SerializeReference] private ApplicableValueEAxis2DFlags m_Mirror = new();
		[SerializeReference] private ApplicableValueVector2 m_TopLeft = new();
		[SerializeReference] private ApplicableValueVector2 m_TopRight = new();
		[SerializeReference] private ApplicableValueVector2 m_BottomLeft = new();
		[SerializeReference] private ApplicableValueVector2 m_BottomRight = new();

		public ApplicableValue<System.Boolean> IsAbsolute => m_IsAbsolute;
		public ApplicableValue<GuiToolkit.EAxis2DFlags> Mirror => m_Mirror;
		public ApplicableValue<UnityEngine.Vector2> TopLeft => m_TopLeft;
		public ApplicableValue<UnityEngine.Vector2> TopRight => m_TopRight;
		public ApplicableValue<UnityEngine.Vector2> BottomLeft => m_BottomLeft;
		public ApplicableValue<UnityEngine.Vector2> BottomRight => m_BottomRight;
	}
}
