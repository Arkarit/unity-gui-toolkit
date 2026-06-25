using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// <summary>
	/// Animated per-vertex UV wobble. Distorts UV0 over time with a layered sinusoidal
	/// wave, producing flowing "underwater refraction"-style movement in any UV-driven
	/// shader (caustics, water, fog, etc.) at vertex cost instead of fragment cost.
	///
	/// Requires sufficient vertex density — combine with <see cref="UiTessellator"/> on
	/// Image / RawImage so the wobble interpolates smoothly across the surface.
	/// TextMeshPro provides enough vertices per glyph by default.
	///
	/// The two layers use an irrational frequency ratio (≈√3) so the combined wave
	/// has no perceivable period — useful for breaking visible tiling in periodic
	/// procedural shaders (e.g. the iterative caustic shader at high Scale values).
	/// </summary>
	[ExecuteAlways]
	public class UiUvWobble : BaseMeshEffectTMP
	{
		private const float TAU = 6.28318530718f;

		[Tooltip("Maximum UV offset applied per vertex. Stays in UV space (0..1), so " +
			"0.02–0.08 is typical. The caustic shader at Scale=4 has a period of 0.25 " +
			"in UV — an amplitude of ~0.06 nicely hides the underlying tiling.")]
		[SerializeField][Range(0f, 0.3f)] private float m_amplitude = 0.05f;

		[Tooltip("Waves per UV unit, separately for X and Y. The wobble in X depends on " +
			"vertex.uv.y and vice versa, so the deformation is perpendicular to the " +
			"frequency axis.")]
		[SerializeField] private Vector2 m_frequency = new Vector2(3f, 3f);

		[Tooltip("Time multiplier for the animation. 0 = static deformation, " +
			"0.5 = slow flow, 2+ = frantic.")]
		[SerializeField] private float m_speed = 0.5f;

		[Tooltip("When on, uses Time.unscaledTime (continues during pause / time-scale changes).")]
		[SerializeField] private bool m_useUnscaledTime = false;

		protected static UIVertex s_vertex;

		public float Amplitude
		{
			get => m_amplitude;
			set
			{
				if (Mathf.Approximately(m_amplitude, value))
					return;
				m_amplitude = value;
				SetDirty();
			}
		}

		public Vector2 Frequency
		{
			get => m_frequency;
			set
			{
				if (m_frequency == value)
					return;
				m_frequency = value;
				SetDirty();
			}
		}

		public float Speed
		{
			get => m_speed;
			set
			{
				if (Mathf.Approximately(m_speed, value))
					return;
				m_speed = value;
				SetDirty();
			}
		}

		/// <summary>
		/// Layered sin wobble; returns the UV offset for a given (uv, t).
		/// Two layers at √3 ratio keep the combined wave aperiodic over any practical range.
		/// </summary>
		public Vector2 WobbleOffsetLocal(Vector2 _uv, float _t)
		{
			float fx = m_frequency.x;
			float fy = m_frequency.y;
			float ts = _t * m_speed;

			float dx = Mathf.Sin(_uv.y * fy * TAU + ts) +
					   Mathf.Sin(_uv.y * fy * 1.732f * TAU + ts * 0.7f) * 0.5f;
			float dy = Mathf.Sin(_uv.x * fx * TAU + ts * 1.1f) +
					   Mathf.Sin(_uv.x * fx * 1.732f * TAU + ts * 0.9f) * 0.5f;

			return new Vector2(dx, dy) * m_amplitude;
		}

		public override void ModifyMesh(VertexHelper _vertexHelper)
		{
			if (!IsActive())
				return;

			float t = GetTime();

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);
				Vector2 uv = s_vertex.uv0;
				s_vertex.uv0 = uv + WobbleOffsetLocal(uv, t);
				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}

		/// <summary>
		/// Animation driver: marks the mesh dirty every frame so ModifyMesh runs continuously.
		/// Skips when speed is zero (static deformation needs no rebuild).
		/// </summary>
		protected override void Update()
		{
			base.Update();

			if (!IsActive())
				return;

			if (Mathf.Approximately(m_speed, 0f))
				return;

			SetDirty();
		}

		private float GetTime()
		{
			if (m_useUnscaledTime)
				return Time.unscaledTime;

			if (Application.isPlaying)
				return Time.time;

#if UNITY_EDITOR
			return (float)EditorApplication.timeSinceStartup;
#else
			return 0f;
#endif
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiUvWobble))]
	public class UiUvWobbleEditor : UnityEditor.Editor
	{
		private SerializedProperty m_amplitudeProp;
		private SerializedProperty m_frequencyProp;
		private SerializedProperty m_speedProp;
		private SerializedProperty m_useUnscaledTimeProp;

		private void OnEnable()
		{
			m_amplitudeProp       = serializedObject.FindProperty("m_amplitude");
			m_frequencyProp       = serializedObject.FindProperty("m_frequency");
			m_speedProp           = serializedObject.FindProperty("m_speed");
			m_useUnscaledTimeProp = serializedObject.FindProperty("m_useUnscaledTime");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.HelpBox(
				"Animated per-vertex UV wobble — needs sufficient vertex density. " +
				"Combine with UiTessellator on Image/RawImage. Useful for breaking " +
				"visible tiling in periodic procedural shaders (e.g. UI_Caustics at " +
				"high Scale).",
				MessageType.Info);

			EditorGUILayout.PropertyField(m_amplitudeProp);
			EditorGUILayout.PropertyField(m_frequencyProp);
			EditorGUILayout.PropertyField(m_speedProp);
			EditorGUILayout.PropertyField(m_useUnscaledTimeProp);

			if (serializedObject.ApplyModifiedProperties())
				((UiUvWobble)target).SetDirty();
		}
	}
#endif
}
