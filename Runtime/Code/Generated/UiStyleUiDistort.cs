// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
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

		private class ApplicableValueVector2 : ApplicableValue<UnityEngine.Vector2> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueEAxis2DFlags : ApplicableValue<GuiToolkit.EAxis2DFlags> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				BottomLeft,
				BottomRight,
				Enabled,
				IsAbsolute,
				Mirror,
				TopLeft,
				TopRight,
			};
		}

#if UNITY_EDITOR
		public override ValueInfo[] GetValueInfoArray()
		{
			return new ValueInfo[]
			{
				new ValueInfo()
				{
					GetterName = "BottomLeft",
					GetterType = typeof(ApplicableValueVector2),
					Value = BottomLeft,
				},
				new ValueInfo()
				{
					GetterName = "BottomRight",
					GetterType = typeof(ApplicableValueVector2),
					Value = BottomRight,
				},
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "IsAbsolute",
					GetterType = typeof(ApplicableValueBoolean),
					Value = IsAbsolute,
				},
				new ValueInfo()
				{
					GetterName = "Mirror",
					GetterType = typeof(ApplicableValueEAxis2DFlags),
					Value = Mirror,
				},
				new ValueInfo()
				{
					GetterName = "TopLeft",
					GetterType = typeof(ApplicableValueVector2),
					Value = TopLeft,
				},
				new ValueInfo()
				{
					GetterName = "TopRight",
					GetterType = typeof(ApplicableValueVector2),
					Value = TopRight,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueVector2 m_BottomLeft = new();
		[SerializeReference] private ApplicableValueVector2 m_BottomRight = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueBoolean m_IsAbsolute = new();
		[SerializeReference] private ApplicableValueEAxis2DFlags m_Mirror = new();
		[SerializeReference] private ApplicableValueVector2 m_TopLeft = new();
		[SerializeReference] private ApplicableValueVector2 m_TopRight = new();

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

	}
}
