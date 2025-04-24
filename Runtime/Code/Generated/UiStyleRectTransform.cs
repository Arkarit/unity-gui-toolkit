// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
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

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				AnchoredPosition,
				AnchorMax,
				AnchorMin,
				LocalEulerAngles,
				LocalPosition,
				LocalRotation,
				LocalScale,
				OffsetMax,
				OffsetMin,
				Pivot,
				SizeDelta,
			};
		}

#if UNITY_EDITOR
		public override List<ValueInfo> GetValueInfos()
		{
			return new List<ValueInfo>()
			{
				new ValueInfo()
				{
					GetterName = "AnchoredPosition",
					GetterType = typeof(ApplicableValueVector2),
					Value = AnchoredPosition,
				},
				new ValueInfo()
				{
					GetterName = "AnchorMax",
					GetterType = typeof(ApplicableValueVector2),
					Value = AnchorMax,
				},
				new ValueInfo()
				{
					GetterName = "AnchorMin",
					GetterType = typeof(ApplicableValueVector2),
					Value = AnchorMin,
				},
				new ValueInfo()
				{
					GetterName = "LocalEulerAngles",
					GetterType = typeof(ApplicableValueVector3),
					Value = LocalEulerAngles,
				},
				new ValueInfo()
				{
					GetterName = "LocalPosition",
					GetterType = typeof(ApplicableValueVector3),
					Value = LocalPosition,
				},
				new ValueInfo()
				{
					GetterName = "LocalRotation",
					GetterType = typeof(ApplicableValueQuaternion),
					Value = LocalRotation,
				},
				new ValueInfo()
				{
					GetterName = "LocalScale",
					GetterType = typeof(ApplicableValueVector3),
					Value = LocalScale,
				},
				new ValueInfo()
				{
					GetterName = "OffsetMax",
					GetterType = typeof(ApplicableValueVector2),
					Value = OffsetMax,
				},
				new ValueInfo()
				{
					GetterName = "OffsetMin",
					GetterType = typeof(ApplicableValueVector2),
					Value = OffsetMin,
				},
				new ValueInfo()
				{
					GetterName = "Pivot",
					GetterType = typeof(ApplicableValueVector2),
					Value = Pivot,
				},
				new ValueInfo()
				{
					GetterName = "SizeDelta",
					GetterType = typeof(ApplicableValueVector2),
					Value = SizeDelta,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueVector2 m_anchoredPosition = new();
		[SerializeReference] private ApplicableValueVector2 m_anchorMax = new();
		[SerializeReference] private ApplicableValueVector2 m_anchorMin = new();
		[SerializeReference] private ApplicableValueVector3 m_localEulerAngles = new();
		[SerializeReference] private ApplicableValueVector3 m_localPosition = new();
		[SerializeReference] private ApplicableValueQuaternion m_localRotation = new();
		[SerializeReference] private ApplicableValueVector3 m_localScale = new();
		[SerializeReference] private ApplicableValueVector2 m_offsetMax = new();
		[SerializeReference] private ApplicableValueVector2 m_offsetMin = new();
		[SerializeReference] private ApplicableValueVector2 m_pivot = new();
		[SerializeReference] private ApplicableValueVector2 m_sizeDelta = new();

		public ApplicableValue<UnityEngine.Vector2> AnchoredPosition
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_anchoredPosition == null)
						m_anchoredPosition = new ApplicableValueVector2();
				#endif
				return m_anchoredPosition;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> AnchorMax
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_anchorMax == null)
						m_anchorMax = new ApplicableValueVector2();
				#endif
				return m_anchorMax;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> AnchorMin
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_anchorMin == null)
						m_anchorMin = new ApplicableValueVector2();
				#endif
				return m_anchorMin;
			}
		}

		public ApplicableValue<UnityEngine.Vector3> LocalEulerAngles
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_localEulerAngles == null)
						m_localEulerAngles = new ApplicableValueVector3();
				#endif
				return m_localEulerAngles;
			}
		}

		public ApplicableValue<UnityEngine.Vector3> LocalPosition
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_localPosition == null)
						m_localPosition = new ApplicableValueVector3();
				#endif
				return m_localPosition;
			}
		}

		public ApplicableValue<UnityEngine.Quaternion> LocalRotation
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_localRotation == null)
						m_localRotation = new ApplicableValueQuaternion();
				#endif
				return m_localRotation;
			}
		}

		public ApplicableValue<UnityEngine.Vector3> LocalScale
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_localScale == null)
						m_localScale = new ApplicableValueVector3();
				#endif
				return m_localScale;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> OffsetMax
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_offsetMax == null)
						m_offsetMax = new ApplicableValueVector2();
				#endif
				return m_offsetMax;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> OffsetMin
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_offsetMin == null)
						m_offsetMin = new ApplicableValueVector2();
				#endif
				return m_offsetMin;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> Pivot
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_pivot == null)
						m_pivot = new ApplicableValueVector2();
				#endif
				return m_pivot;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> SizeDelta
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_sizeDelta == null)
						m_sizeDelta = new ApplicableValueVector2();
				#endif
				return m_sizeDelta;
			}
		}

	}
}
