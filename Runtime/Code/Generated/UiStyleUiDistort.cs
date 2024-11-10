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
				IsAbsolute,
				Mirror,
				TopLeft,
				TopRight,
				BottomLeft,
				BottomRight,
				Enabled,
			};
		}

		[SerializeReference] private ApplicableValueBoolean m_IsAbsolute = new();
		[SerializeReference] private ApplicableValueEAxis2DFlags m_Mirror = new();
		[SerializeReference] private ApplicableValueVector2 m_TopLeft = new();
		[SerializeReference] private ApplicableValueVector2 m_TopRight = new();
		[SerializeReference] private ApplicableValueVector2 m_BottomLeft = new();
		[SerializeReference] private ApplicableValueVector2 m_BottomRight = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();

		public ApplicableValue<System.Boolean> IsAbsolute
		{
			get
			{
				if (m_IsAbsolute == null)
					m_IsAbsolute = new ApplicableValueBoolean();
				return m_IsAbsolute;
			}
		}

		public ApplicableValue<GuiToolkit.EAxis2DFlags> Mirror
		{
			get
			{
				if (m_Mirror == null)
					m_Mirror = new ApplicableValueEAxis2DFlags();
				return m_Mirror;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> TopLeft
		{
			get
			{
				if (m_TopLeft == null)
					m_TopLeft = new ApplicableValueVector2();
				return m_TopLeft;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> TopRight
		{
			get
			{
				if (m_TopRight == null)
					m_TopRight = new ApplicableValueVector2();
				return m_TopRight;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> BottomLeft
		{
			get
			{
				if (m_BottomLeft == null)
					m_BottomLeft = new ApplicableValueVector2();
				return m_BottomLeft;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> BottomRight
		{
			get
			{
				if (m_BottomRight == null)
					m_BottomRight = new ApplicableValueVector2();
				return m_BottomRight;
			}
		}

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

	}
}
