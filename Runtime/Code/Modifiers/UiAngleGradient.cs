using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Draws the Graphic on the same GameObject with an animated angle gradient (shader UI_AngleGradient), with
	/// its own colours, count, sharpness and rotation. Meant for a frame: a UiRoundedImage with a Frame Size and
	/// no sprite, whose UVs span the rect, so the gradient turns around the rect's centre. With Count 4 the
	/// highlights sit in the middle of the sides and the corners are darkest.
	///
	/// The values live here, not in a material asset, so every frame can have its own and a style can set them.
	/// The component hands the Graphic a material with these values (IMaterialModifier); graphics with the same
	/// values share one material and keep batching. The animation runs in the shader, without a script update.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(Graphic))]
	public class UiAngleGradient : MonoBehaviour, IMaterialModifier
	{
		public const string ShaderName = "UIToolkit/UI_AngleGradient";
		public const float MinSharpness = 0.1f;
		public const float MaxSharpness = 32;

		[Tooltip("The shader the gradient is drawn with (UIToolkit/UI_AngleGradient); referenced so a build includes it.")]
		[SerializeField][Mandatory] private Shader m_shader;
		[Tooltip("The colour between the highlights.")]
		[SerializeField] private Color m_colorBase = Color.black;
		[Tooltip("The colour at the highlights.")]
		[SerializeField] private Color m_colorHighlight = Color.white;
		[Tooltip("How many highlights per full turn. 4 puts one in the middle of each side of a rectangle.")]
		[SerializeField] private float m_count = 4;
		[Tooltip("1 is a soft wave; higher values make the highlights narrower and leave more of the base colour.")]
		[SerializeField][Range(MinSharpness, MaxSharpness)] private float m_sharpness = 1;
		[Tooltip("Turns the pattern, in degrees.")]
		[SerializeField] private float m_rotation;
		[Tooltip("Turns the pattern over time, in degrees per second. 0 stands still.")]
		[SerializeField] private float m_speed = 30;

		private static readonly int s_colorBaseId = Shader.PropertyToID("_ColorBase");
		private static readonly int s_colorHighlightId = Shader.PropertyToID("_ColorHighlight");
		private static readonly int s_countId = Shader.PropertyToID("_Count");
		private static readonly int s_sharpnessId = Shader.PropertyToID("_Sharpness");
		private static readonly int s_rotationId = Shader.PropertyToID("_Rotation");
		private static readonly int s_speedId = Shader.PropertyToID("_Speed");

		// What a stencil (Mask, a UiShapeImage's mask settings) sets on the incoming material; carried over, so the
		// gradient keeps masking whichever material modifier runs first.
		private static readonly int[] s_stencilIds =
		{
			Shader.PropertyToID("_Stencil"),
			Shader.PropertyToID("_StencilComp"),
			Shader.PropertyToID("_StencilOp"),
			Shader.PropertyToID("_StencilReadMask"),
			Shader.PropertyToID("_StencilWriteMask"),
			Shader.PropertyToID("_ColorMask"),
		};

		// One material per combination of values, shared by every graphic using it, released with the last one.
		private static readonly Dictionary<Key, Entry> s_materials = new();
		private Key? m_key;

		private Graphic m_graphic;
		private Graphic Graphic => m_graphic ? m_graphic : m_graphic = GetComponent<Graphic>();

		public Color ColorBase
		{
			get => m_colorBase;
			set
			{
				if (m_colorBase == value)
					return;

				m_colorBase = value;
				SetDirty();
			}
		}

		public Color ColorHighlight
		{
			get => m_colorHighlight;
			set
			{
				if (m_colorHighlight == value)
					return;

				m_colorHighlight = value;
				SetDirty();
			}
		}

		public float Count
		{
			get => m_count;
			set
			{
				if (m_count == value)
					return;

				m_count = value;
				SetDirty();
			}
		}

		public float Sharpness
		{
			get => m_sharpness;
			set
			{
				value = Mathf.Clamp(value, MinSharpness, MaxSharpness);
				if (m_sharpness == value)
					return;

				m_sharpness = value;
				SetDirty();
			}
		}

		public float Rotation
		{
			get => m_rotation;
			set
			{
				if (m_rotation == value)
					return;

				m_rotation = value;
				SetDirty();
			}
		}

		public float Speed
		{
			get => m_speed;
			set
			{
				if (m_speed == value)
					return;

				m_speed = value;
				SetDirty();
			}
		}

		public Material GetModifiedMaterial( Material _baseMaterial )
		{
			// Added at runtime (AddComponent), Reset never ran; the shader is in a build when the package's material is.
			if (!m_shader)
				m_shader = Shader.Find(ShaderName);

			if (!isActiveAndEnabled || !m_shader)
				return _baseMaterial;

			var key = new Key(this, _baseMaterial);
			if (m_key != key)
			{
				Release();
				m_key = key;
				if (!s_materials.TryGetValue(key, out var entry))
				{
					entry = new Entry { Material = CreateMaterial(key) };
					s_materials.Add(key, entry);
				}

				entry.Users++;
			}

			return s_materials[key].Material;
		}

		private void OnEnable() => SetDirty();

		private void OnDisable()
		{
			Release();
			SetDirty();
		}

		private void OnDestroy() => Release();

#if UNITY_EDITOR
		private void Reset()
		{
			m_shader = Shader.Find(ShaderName);
		}

		private void OnValidate()
		{
			m_sharpness = Mathf.Clamp(m_sharpness, MinSharpness, MaxSharpness);
			SetDirty();
		}
#endif

		private void SetDirty()
		{
			if (Graphic)
				Graphic.SetMaterialDirty();
		}

		private void Release()
		{
			if (m_key == null)
				return;

			var key = m_key.Value;
			m_key = null;
			if (!s_materials.TryGetValue(key, out var entry))
				return;

			entry.Users--;
			if (entry.Users > 0)
				return;

			s_materials.Remove(key);
			entry.Material.SafeDestroy(false);
		}

		private static Material CreateMaterial( Key _key )
		{
			var material = new Material(_key.Shader)
			{
				name = "UI_AngleGradient (shared)",
				hideFlags = HideFlags.HideAndDontSave,
			};

			material.SetColor(s_colorBaseId, _key.ColorBase);
			material.SetColor(s_colorHighlightId, _key.ColorHighlight);
			material.SetFloat(s_countId, _key.Count);
			material.SetFloat(s_sharpnessId, _key.Sharpness);
			material.SetFloat(s_rotationId, _key.Rotation);
			material.SetFloat(s_speedId, _key.Speed);
			for (int i = 0; i < s_stencilIds.Length; i++)
				material.SetFloat(s_stencilIds[i], _key.Stencil[i]);

			return material;
		}

		private class Entry
		{
			public Material Material;
			public int Users;
		}

		private readonly struct Key : IEquatable<Key>
		{
			public readonly Shader Shader;
			public readonly Color ColorBase;
			public readonly Color ColorHighlight;
			public readonly float Count;
			public readonly float Sharpness;
			public readonly float Rotation;
			public readonly float Speed;
			public readonly StencilValues Stencil;

			public Key( UiAngleGradient _gradient, Material _baseMaterial )
			{
				Shader = _gradient.m_shader;
				ColorBase = _gradient.m_colorBase;
				ColorHighlight = _gradient.m_colorHighlight;
				Count = _gradient.m_count;
				Sharpness = _gradient.m_sharpness;
				Rotation = _gradient.m_rotation;
				Speed = _gradient.m_speed;
				Stencil = new StencilValues(_baseMaterial);
			}

			public bool Equals( Key _other ) =>
				Shader == _other.Shader && ColorBase == _other.ColorBase && ColorHighlight == _other.ColorHighlight
				&& Count == _other.Count && Sharpness == _other.Sharpness && Rotation == _other.Rotation
				&& Speed == _other.Speed && Stencil.Equals(_other.Stencil);

			public override bool Equals( object _obj ) => _obj is Key other && Equals(other);

			public override int GetHashCode() =>
				HashCode.Combine(Shader, ColorBase, ColorHighlight, Count, Sharpness, Rotation, Speed, Stencil);

			public static bool operator ==( Key _a, Key _b ) => _a.Equals(_b);
			public static bool operator !=( Key _a, Key _b ) => !_a.Equals(_b);
		}

		// The stencil values of the incoming material, in the order of s_stencilIds; the shader's defaults without one.
		private readonly struct StencilValues : IEquatable<StencilValues>
		{
			private readonly float m_stencil, m_comp, m_op, m_readMask, m_writeMask, m_colorMask;

			public StencilValues( Material _material )
			{
				m_stencil = Get(_material, 0, 0);
				m_comp = Get(_material, 1, 8);
				m_op = Get(_material, 2, 0);
				m_readMask = Get(_material, 3, 255);
				m_writeMask = Get(_material, 4, 255);
				m_colorMask = Get(_material, 5, 15);
			}

			private static float Get( Material _material, int _index, float _default ) =>
				_material && _material.HasProperty(s_stencilIds[_index]) ? _material.GetFloat(s_stencilIds[_index]) : _default;

			public float this[ int _index ] => _index switch
			{
				0 => m_stencil,
				1 => m_comp,
				2 => m_op,
				3 => m_readMask,
				4 => m_writeMask,
				_ => m_colorMask,
			};

			public bool Equals( StencilValues _other ) =>
				m_stencil == _other.m_stencil && m_comp == _other.m_comp && m_op == _other.m_op
				&& m_readMask == _other.m_readMask && m_writeMask == _other.m_writeMask && m_colorMask == _other.m_colorMask;

			public override bool Equals( object _obj ) => _obj is StencilValues other && Equals(other);

			public override int GetHashCode() => HashCode.Combine(m_stencil, m_comp, m_op, m_readMask, m_writeMask, m_colorMask);
		}
	}
}
