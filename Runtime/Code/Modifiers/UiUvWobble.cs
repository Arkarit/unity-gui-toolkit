using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// <summary>
	/// Per-vertex UV wobble. Distorts UV0 with a layered sinusoidal wave to break
	/// periodicity in UV-driven shaders (caustics, water, fog, etc.) at vertex cost
	/// instead of fragment cost.
	///
	/// The deformation splits into two independent components:
	/// <list type="bullet">
	///   <item><b>Static</b>: a fixed, time-independent base distortion. Always
	///         applied, free at runtime (one mesh rebuild on parameter change).</item>
	///   <item><b>Dynamic</b>: an animated layer added on top of the static one.
	///         Costs a SetDirty per frame while enabled — toggle off to make the
	///         modifier completely passive (static only).</item>
	/// </list>
	///
	/// Requires sufficient vertex density — combine with <see cref="UiTessellator"/>
	/// on Image/RawImage so the wobble interpolates smoothly across the surface.
	/// TextMeshPro provides enough vertices per glyph by default.
	///
	/// Both layers use an irrational frequency ratio (≈√3) so the combined wave has
	/// no perceivable period — useful for hiding visible tiling in periodic procedural
	/// shaders (e.g. the iterative caustic shader at high Scale).
	/// </summary>
	[ExecuteAlways]
	public class UiUvWobble : BaseMeshEffectTMP
	{
		private const float TAU = 6.28318530718f;

		[Tooltip("Time-independent UV offset amplitude. Always applied, no per-frame cost. " +
			"Use this for a baseline distortion that breaks tiling even when animation is off.")]
		[SerializeField][Range(0f, 0.3f)] private float m_staticAmplitude = 0.03f;

		[Tooltip("Waves per UV unit for the static layer, separately for X and Y. " +
			"The wobble in X depends on vertex.uv.y and vice versa, so the deformation " +
			"is perpendicular to the frequency axis. Typical baseline: 2..4 (broad undulations).")]
		[SerializeField] private Vector2 m_staticFrequency = new Vector2(3f, 3f);

		[Tooltip("Master switch for the animated layer. When off, the modifier is fully passive — " +
			"no per-frame SetDirty, only the static distortion is in the mesh.")]
		[SerializeField] private bool m_animate = true;

		[Tooltip("Amplitude of the animated UV offset, added on top of the static one. " +
			"Set to 0 to keep animation enabled but disable the dynamic component.")]
		[SerializeField][Range(0f, 0.3f)] private float m_dynamicAmplitude = 0.04f;

		[Tooltip("Waves per UV unit for the dynamic layer. Same rule as static frequency. " +
			"Pair with a different static frequency for coarse-baseline + fine-ripple " +
			"(or vice versa).")]
		[SerializeField] private Vector2 m_dynamicFrequency = new Vector2(3f, 3f);

		[Tooltip("Time multiplier for the dynamic layer. 0 = animation enabled but frozen, " +
			"0.5 = slow flow, 2+ = frantic.")]
		[SerializeField] private float m_speed = 0.5f;

		[Tooltip("When on, dynamic layer uses Time.unscaledTime (continues during pause / time-scale changes).")]
		[SerializeField] private bool m_useUnscaledTime = false;

		protected static UIVertex s_vertex;

		public float StaticAmplitude
		{
			get => m_staticAmplitude;
			set
			{
				if (Mathf.Approximately(m_staticAmplitude, value))
					return;
				m_staticAmplitude = value;
				SetDirty();
			}
		}

		public float DynamicAmplitude
		{
			get => m_dynamicAmplitude;
			set
			{
				if (Mathf.Approximately(m_dynamicAmplitude, value))
					return;
				m_dynamicAmplitude = value;
				SetDirty();
			}
		}

		public bool Animate
		{
			get => m_animate;
			set
			{
				if (m_animate == value)
					return;
				m_animate = value;
				SetDirty();
			}
		}

		public Vector2 StaticFrequency
		{
			get => m_staticFrequency;
			set
			{
				if (m_staticFrequency == value)
					return;
				m_staticFrequency = value;
				SetDirty();
			}
		}

		public Vector2 DynamicFrequency
		{
			get => m_dynamicFrequency;
			set
			{
				if (m_dynamicFrequency == value)
					return;
				m_dynamicFrequency = value;
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
		/// Layered sin wave base function — independent of amplitude.
		/// Two sin layers at √3 ratio keep the combined wave aperiodic over any practical range.
		/// </summary>
		private static Vector2 WobbleBase(Vector2 _uv, float _ts, Vector2 _freq)
		{
			float fx = _freq.x;
			float fy = _freq.y;

			float dx = Mathf.Sin(_uv.y * fy * TAU + _ts) +
					   Mathf.Sin(_uv.y * fy * 1.732f * TAU + _ts * 0.7f) * 0.5f;
			float dy = Mathf.Sin(_uv.x * fx * TAU + _ts * 1.1f) +
					   Mathf.Sin(_uv.x * fx * 1.732f * TAU + _ts * 0.9f) * 0.5f;

			return new Vector2(dx, dy);
		}

		/// <summary>
		/// Full UV offset for a vertex at <paramref name="_uv"/> at time <paramref name="_t"/>.
		/// Sums the static (time-independent) and, if enabled, the dynamic (animated) components.
		/// </summary>
		public Vector2 ComputeOffset(Vector2 _uv, float _t)
		{
			Vector2 offset = Vector2.zero;

			if (m_staticAmplitude > 0f)
				offset += WobbleBase(_uv, 0f, m_staticFrequency) * m_staticAmplitude;

			if (m_animate && m_dynamicAmplitude > 0f)
				offset += WobbleBase(_uv, _t * m_speed, m_dynamicFrequency) * m_dynamicAmplitude;

			return offset;
		}

		public override void ModifyMesh(VertexHelper _vertexHelper)
		{
			if (!IsActive())
				return;

			// Skip the per-vertex loop entirely when nothing would be applied.
			bool hasStatic  = m_staticAmplitude > 0f;
			bool hasDynamic = m_animate && m_dynamicAmplitude > 0f;
			if (!hasStatic && !hasDynamic)
				return;

			float t = hasDynamic ? GetTime() : 0f;

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);
				Vector2 uv = s_vertex.uv0;
				s_vertex.uv0 = uv + ComputeOffset(uv, t);
				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}

		/// <summary>
		/// Animation driver: marks the mesh dirty every frame so ModifyMesh runs continuously.
		/// Gated by every condition that would make a rebuild a no-op, so a passive modifier
		/// truly costs zero per frame.
		/// </summary>
		protected override void Update()
		{
			base.Update();

			if (!IsActive())
				return;
			if (!m_animate)
				return;
			if (Mathf.Approximately(m_speed, 0f))
				return;
			if (m_dynamicAmplitude <= 0f)
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
		private SerializedProperty m_staticAmplitudeProp;
		private SerializedProperty m_staticFrequencyProp;
		private SerializedProperty m_animateProp;
		private SerializedProperty m_dynamicAmplitudeProp;
		private SerializedProperty m_dynamicFrequencyProp;
		private SerializedProperty m_speedProp;
		private SerializedProperty m_useUnscaledTimeProp;

		private void OnEnable()
		{
			m_staticAmplitudeProp  = serializedObject.FindProperty("m_staticAmplitude");
			m_staticFrequencyProp  = serializedObject.FindProperty("m_staticFrequency");
			m_animateProp          = serializedObject.FindProperty("m_animate");
			m_dynamicAmplitudeProp = serializedObject.FindProperty("m_dynamicAmplitude");
			m_dynamicFrequencyProp = serializedObject.FindProperty("m_dynamicFrequency");
			m_speedProp            = serializedObject.FindProperty("m_speed");
			m_useUnscaledTimeProp  = serializedObject.FindProperty("m_useUnscaledTime");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.HelpBox(
				"Animated per-vertex UV wobble — needs sufficient vertex density. " +
				"Combine with UiTessellator on Image/RawImage. Useful for hiding " +
				"visible tiling in periodic procedural shaders (e.g. UI_Caustics at " +
				"high Scale).",
				MessageType.Info);

			EditorGUILayout.LabelField("Static", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			EditorGUILayout.PropertyField(m_staticAmplitudeProp, new GUIContent("Amplitude"));
			EditorGUILayout.PropertyField(m_staticFrequencyProp, new GUIContent("Frequency"));
			EditorGUI.indentLevel--;

			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.PropertyField(m_animateProp, new GUIContent("Dynamic (animated)"));

			using (new EditorGUI.DisabledScope(!m_animateProp.boolValue))
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(m_dynamicAmplitudeProp, new GUIContent("Amplitude"));
				EditorGUILayout.PropertyField(m_dynamicFrequencyProp, new GUIContent("Frequency"));
				EditorGUILayout.PropertyField(m_speedProp,            new GUIContent("Speed"));
				EditorGUILayout.PropertyField(m_useUnscaledTimeProp,  new GUIContent("Use Unscaled Time"));
				EditorGUI.indentLevel--;
			}

			if (serializedObject.ApplyModifiedProperties())
				((UiUvWobble)target).SetDirty();
		}
	}
#endif
}
