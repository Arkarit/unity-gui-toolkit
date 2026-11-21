// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiIcon3D : UiAbstractStyle<GuiToolkit.UiIcon3D>
	{
		public UiStyleUiIcon3D(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueBounds : ApplicableValue<UnityEngine.Bounds> {}
		private class ApplicableValueCanvas : ApplicableValue<UnityEngine.Canvas> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueInt32 : ApplicableValue<System.Int32> {}
		private class ApplicableValueVector3 : ApplicableValue<UnityEngine.Vector3> {}
		private class ApplicableValueTexture : ApplicableValue<UnityEngine.Texture> {}
		private class ApplicableValueEMode : ApplicableValue<GuiToolkit.UiIcon3D.EMode> {}
		private class ApplicableValueGameObject : ApplicableValue<UnityEngine.GameObject> {}
		private class ApplicableValueString : ApplicableValue<System.String> {}
		private class ApplicableValueUiIcon3DPreset : ApplicableValue<GuiToolkit.UiIcon3DPreset> {}
		private class ApplicableValueQuaternion : ApplicableValue<UnityEngine.Quaternion> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				AnimationSpeed,
				AnimationStartTime,
				Bounds,
				CanvasInParent,
				Enabled,
				EnabledInHierarchy,
				FrameDivider,
				FramingOffset,
				FramingPadding,
				LoadingTexture,
				Mode,
				OverrideBounds,
				OverrideViewRotation,
				Paused,
				Prefab,
				PrefabId,
				Preset,
				RefreshInterval,
				ResolutionScale,
				RewindSpeed,
				ViewRotation,
			};
		}

		[SerializeReference] private ApplicableValueSingle m_AnimationSpeed = new();
		[SerializeReference] private ApplicableValueSingle m_AnimationStartTime = new();
		[SerializeReference] private ApplicableValueBounds m_Bounds = new();
		[SerializeReference] private ApplicableValueCanvas m_CanvasInParent = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueBoolean m_EnabledInHierarchy = new();
		[SerializeReference] private ApplicableValueInt32 m_FrameDivider = new();
		[SerializeReference] private ApplicableValueVector3 m_FramingOffset = new();
		[SerializeReference] private ApplicableValueSingle m_FramingPadding = new();
		[SerializeReference] private ApplicableValueTexture m_LoadingTexture = new();
		[SerializeReference] private ApplicableValueEMode m_Mode = new();
		[SerializeReference] private ApplicableValueBoolean m_OverrideBounds = new();
		[SerializeReference] private ApplicableValueBoolean m_OverrideViewRotation = new();
		[SerializeReference] private ApplicableValueBoolean m_Paused = new();
		[SerializeReference] private ApplicableValueGameObject m_Prefab = new();
		[SerializeReference] private ApplicableValueString m_PrefabId = new();
		[SerializeReference] private ApplicableValueUiIcon3DPreset m_Preset = new();
		[SerializeReference] private ApplicableValueSingle m_RefreshInterval = new();
		[SerializeReference] private ApplicableValueSingle m_ResolutionScale = new();
		[SerializeReference] private ApplicableValueSingle m_RewindSpeed = new();
		[SerializeReference] private ApplicableValueQuaternion m_ViewRotation = new();

		public ApplicableValue<System.Single> AnimationSpeed
		{
			get
			{
				if (m_AnimationSpeed == null)
					m_AnimationSpeed = new ApplicableValueSingle();
				return m_AnimationSpeed;
			}
		}

		public ApplicableValue<System.Single> AnimationStartTime
		{
			get
			{
				if (m_AnimationStartTime == null)
					m_AnimationStartTime = new ApplicableValueSingle();
				return m_AnimationStartTime;
			}
		}

		public ApplicableValue<UnityEngine.Bounds> Bounds
		{
			get
			{
				if (m_Bounds == null)
					m_Bounds = new ApplicableValueBounds();
				return m_Bounds;
			}
		}

		public ApplicableValue<UnityEngine.Canvas> CanvasInParent
		{
			get
			{
				if (m_CanvasInParent == null)
					m_CanvasInParent = new ApplicableValueCanvas();
				return m_CanvasInParent;
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

		public ApplicableValue<System.Boolean> EnabledInHierarchy
		{
			get
			{
				if (m_EnabledInHierarchy == null)
					m_EnabledInHierarchy = new ApplicableValueBoolean();
				return m_EnabledInHierarchy;
			}
		}

		public ApplicableValue<System.Int32> FrameDivider
		{
			get
			{
				if (m_FrameDivider == null)
					m_FrameDivider = new ApplicableValueInt32();
				return m_FrameDivider;
			}
		}

		public ApplicableValue<UnityEngine.Vector3> FramingOffset
		{
			get
			{
				if (m_FramingOffset == null)
					m_FramingOffset = new ApplicableValueVector3();
				return m_FramingOffset;
			}
		}

		public ApplicableValue<System.Single> FramingPadding
		{
			get
			{
				if (m_FramingPadding == null)
					m_FramingPadding = new ApplicableValueSingle();
				return m_FramingPadding;
			}
		}

		public ApplicableValue<UnityEngine.Texture> LoadingTexture
		{
			get
			{
				if (m_LoadingTexture == null)
					m_LoadingTexture = new ApplicableValueTexture();
				return m_LoadingTexture;
			}
		}

		public ApplicableValue<GuiToolkit.UiIcon3D.EMode> Mode
		{
			get
			{
				if (m_Mode == null)
					m_Mode = new ApplicableValueEMode();
				return m_Mode;
			}
		}

		public ApplicableValue<System.Boolean> OverrideBounds
		{
			get
			{
				if (m_OverrideBounds == null)
					m_OverrideBounds = new ApplicableValueBoolean();
				return m_OverrideBounds;
			}
		}

		public ApplicableValue<System.Boolean> OverrideViewRotation
		{
			get
			{
				if (m_OverrideViewRotation == null)
					m_OverrideViewRotation = new ApplicableValueBoolean();
				return m_OverrideViewRotation;
			}
		}

		public ApplicableValue<System.Boolean> Paused
		{
			get
			{
				if (m_Paused == null)
					m_Paused = new ApplicableValueBoolean();
				return m_Paused;
			}
		}

		public ApplicableValue<UnityEngine.GameObject> Prefab
		{
			get
			{
				if (m_Prefab == null)
					m_Prefab = new ApplicableValueGameObject();
				return m_Prefab;
			}
		}

		public ApplicableValue<System.String> PrefabId
		{
			get
			{
				if (m_PrefabId == null)
					m_PrefabId = new ApplicableValueString();
				return m_PrefabId;
			}
		}

		public ApplicableValue<GuiToolkit.UiIcon3DPreset> Preset
		{
			get
			{
				if (m_Preset == null)
					m_Preset = new ApplicableValueUiIcon3DPreset();
				return m_Preset;
			}
		}

		public ApplicableValue<System.Single> RefreshInterval
		{
			get
			{
				if (m_RefreshInterval == null)
					m_RefreshInterval = new ApplicableValueSingle();
				return m_RefreshInterval;
			}
		}

		public ApplicableValue<System.Single> ResolutionScale
		{
			get
			{
				if (m_ResolutionScale == null)
					m_ResolutionScale = new ApplicableValueSingle();
				return m_ResolutionScale;
			}
		}

		public ApplicableValue<System.Single> RewindSpeed
		{
			get
			{
				if (m_RewindSpeed == null)
					m_RewindSpeed = new ApplicableValueSingle();
				return m_RewindSpeed;
			}
		}

		public ApplicableValue<UnityEngine.Quaternion> ViewRotation
		{
			get
			{
				if (m_ViewRotation == null)
					m_ViewRotation = new ApplicableValueQuaternion();
				return m_ViewRotation;
			}
		}

	}
}
