// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleRectTransform : UiAbstractStyle<UnityEngine.RectTransform>
	{
		public UiStyleRectTransform(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueVector2 : ApplicableValue<UnityEngine.Vector2> {}
		private class ApplicableValueVector3 : ApplicableValue<UnityEngine.Vector3> {}
		private class ApplicableValueQuaternion : ApplicableValue<UnityEngine.Quaternion> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				m_anchorMin,
				m_anchorMax,
				m_anchoredPosition,
				m_sizeDelta,
				m_pivot,
				m_offsetMin,
				m_offsetMax,
				m_localPosition,
				m_localEulerAngles,
				m_rotation,
				m_localRotation,
				m_localScale,
			};
		}

		[SerializeReference] private ApplicableValueVector2 m_anchorMin = new();
		[SerializeReference] private ApplicableValueVector2 m_anchorMax = new();
		[SerializeReference] private ApplicableValueVector2 m_anchoredPosition = new();
		[SerializeReference] private ApplicableValueVector2 m_sizeDelta = new();
		[SerializeReference] private ApplicableValueVector2 m_pivot = new();
		[SerializeReference] private ApplicableValueVector2 m_offsetMin = new();
		[SerializeReference] private ApplicableValueVector2 m_offsetMax = new();
		[SerializeReference] private ApplicableValueVector3 m_localPosition = new();
		[SerializeReference] private ApplicableValueVector3 m_localEulerAngles = new();
		[SerializeReference] private ApplicableValueQuaternion m_rotation = new();
		[SerializeReference] private ApplicableValueQuaternion m_localRotation = new();
		[SerializeReference] private ApplicableValueVector3 m_localScale = new();

		public ApplicableValue<UnityEngine.Vector2> AnchorMin => m_anchorMin;
		public ApplicableValue<UnityEngine.Vector2> AnchorMax => m_anchorMax;
		public ApplicableValue<UnityEngine.Vector2> AnchoredPosition => m_anchoredPosition;
		public ApplicableValue<UnityEngine.Vector2> SizeDelta => m_sizeDelta;
		public ApplicableValue<UnityEngine.Vector2> Pivot => m_pivot;
		public ApplicableValue<UnityEngine.Vector2> OffsetMin => m_offsetMin;
		public ApplicableValue<UnityEngine.Vector2> OffsetMax => m_offsetMax;
		public ApplicableValue<UnityEngine.Vector3> LocalPosition => m_localPosition;
		public ApplicableValue<UnityEngine.Vector3> LocalEulerAngles => m_localEulerAngles;
		public ApplicableValue<UnityEngine.Quaternion> Rotation => m_rotation;
		public ApplicableValue<UnityEngine.Quaternion> LocalRotation => m_localRotation;
		public ApplicableValue<UnityEngine.Vector3> LocalScale => m_localScale;
	}
}
