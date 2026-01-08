// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleGraphic : UiAbstractStyle<UnityEngine.UI.Graphic>
	{
		public UiStyleGraphic(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueVector4 : ApplicableValue<UnityEngine.Vector4> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				Color,
				Enabled,
				Material,
				RaycastPadding,
				RaycastTarget,
			};
		}

		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueMaterial m_material = new();
		[SerializeReference] private ApplicableValueVector4 m_raycastPadding = new();
		[SerializeReference] private ApplicableValueBoolean m_raycastTarget = new();

		public ApplicableValue<UnityEngine.Color> Color
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_color == null)
						m_color = new ApplicableValueColor();
				#endif
				return m_color;
			}
		}

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_enabled == null)
						m_enabled = new ApplicableValueBoolean();
				#endif
				return m_enabled;
			}
		}

		public ApplicableValue<UnityEngine.Material> Material
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_material == null)
						m_material = new ApplicableValueMaterial();
				#endif
				return m_material;
			}
		}

		public ApplicableValue<UnityEngine.Vector4> RaycastPadding
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_raycastPadding == null)
						m_raycastPadding = new ApplicableValueVector4();
				#endif
				return m_raycastPadding;
			}
		}

		public ApplicableValue<System.Boolean> RaycastTarget
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_raycastTarget == null)
						m_raycastTarget = new ApplicableValueBoolean();
				#endif
				return m_raycastTarget;
			}
		}

	}
}
