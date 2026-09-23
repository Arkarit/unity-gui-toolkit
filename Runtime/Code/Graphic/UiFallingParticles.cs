using System;
using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Falling particles (snow flakes, leaves, confetti ...) rendered as ONE UI graphic.
	///
	/// All particles are quads in a single mesh, so the whole effect costs one draw call and behaves like any
	/// other Graphic: it is tinted by Color, clipped by RectMask2D / Mask, sorted by hierarchy order.
	/// Particles fall through the RectTransform's rect; stretch it over the area that should be snowed on.
	///
	/// Movement is purely programmatic: fall speed, a sinusoidal sway, wind, spin and an optional "flutter"
	/// (the quad's width oscillates, which reads as a leaf turning over in 3D).
	///
	/// Multiple sprites are picked at random per particle. They are drawn with ONE texture, so several sprites
	/// only work when they live on the same texture (sprite atlas / sprite sheet). If they do not, only the
	/// first sprite's texture is used and a warning is logged once.
	///
	/// The mesh is rebuilt every frame. That is cheap for the mesh itself, but it also rebuilds the batch of
	/// the Canvas it lives on - put the effect on its own (nested) Canvas when that Canvas is large.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiFallingParticles : MaskableGraphic
	{
		private const string DefaultSpritePath = "BasicSprites/UITK_Simple_Circle";
		private const float MaxDeltaTime = 0.1f;

		[Tooltip("Sprites to pick from at random. Several sprites must share one texture (atlas). "
		         + "Empty: a soft circle from the toolkit's basic sprites is used.")]
		[SerializeField] protected Sprite[] m_sprites = Array.Empty<Sprite>();

		[Tooltip("Number of particles alive at any time.")]
		[UnityEngine.Range(1, 1000)]
		[SerializeField] protected int m_count = 60;

		[Tooltip("Particle size in pixels (min, max). Larger particles also fall faster - see Size Speed Coupling.")]
		[SerializeField] protected Vector2 m_sizeRange = new Vector2(8, 24);

		[Tooltip("Fall speed in pixels per second (min, max).")]
		[SerializeField] protected Vector2 m_fallSpeedRange = new Vector2(40, 90);

		[Tooltip("0: size and fall speed are independent. 1: the biggest particles always get the highest speed "
		         + "(cheap depth / parallax impression).")]
		[UnityEngine.Range(0, 1)]
		[SerializeField] protected float m_sizeSpeedCoupling = 0.7f;

		[Tooltip("Horizontal drift in pixels per second. Negative blows left.")]
		[SerializeField] protected float m_wind = 10;

		[Tooltip("Sideways sway amplitude in pixels (min, max).")]
		[SerializeField] protected Vector2 m_swayAmplitudeRange = new Vector2(5, 25);

		[Tooltip("Sway frequency in oscillations per second (min, max).")]
		[SerializeField] protected Vector2 m_swayFrequencyRange = new Vector2(0.2f, 0.6f);

		[Tooltip("Spin in degrees per second (min, max). The direction is chosen at random.")]
		[SerializeField] protected Vector2 m_rotationSpeedRange = new Vector2(0, 60);

		[Tooltip("Width oscillation, reads as a leaf turning over. Oscillations per second (min, max). 0/0 = off.")]
		[SerializeField] protected Vector2 m_flutterFrequencyRange = Vector2.zero;

		[Tooltip("Each particle gets a random color between these two, multiplied with the graphic's Color.")]
		[SerializeField] protected Color m_colorA = Color.white;
		[SerializeField] protected Color m_colorB = new Color(1, 1, 1, 0.5f);

		[Tooltip("Particles fade in / out over this distance (pixels) at the top and bottom edge of the rect.")]
		[SerializeField] protected float m_edgeFade = 40;

		[Tooltip("Use unscaled time, so the effect keeps running while the game is paused (Time.timeScale = 0).")]
		[SerializeField] protected bool m_unscaledTime = true;

		[Tooltip("Animate in Edit Mode, too. Otherwise a frozen snapshot is shown there.")]
		[SerializeField] protected bool m_animateInEditMode = false;

		private struct Particle
		{
			public float X;         // sway center, local space
			public float Y;         // local space
			public float Size;
			public float FallSpeed;
			public float SwayAmplitude;
			public float SwayFrequency;
			public float SwayPhase;
			public float Rotation;
			public float RotationSpeed;
			public float FlutterFrequency;
			public float FlutterPhase;
			public int SpriteIndex;
			public Color Color;
		}

		private Particle[] m_particles;
		private Rect m_lastRect;
		private double m_lastTime = -1;
		private float m_time;
		private bool m_warnedMixedTextures;
		private static Sprite s_defaultSprite;

		public int Count
		{
			get => m_count;
			set
			{
				value = Mathf.Max(1, value);
				if (m_count == value)
					return;
				m_count = value;
				m_particles = null;
				SetVerticesDirty();
			}
		}

		public float Wind
		{
			get => m_wind;
			set => m_wind = value;
		}

		public Sprite[] Sprites
		{
			get => m_sprites;
			set
			{
				m_sprites = value ?? Array.Empty<Sprite>();
				m_particles = null;
				m_warnedMixedTextures = false;
				SetAllDirty();
			}
		}

		public override Texture mainTexture
		{
			get
			{
				var sprite = GetSprite(0);
				return sprite != null ? sprite.texture : s_WhiteTexture;
			}
		}

		/// <summary>
		/// Distribute all particles over the whole rect again, as if the effect had been running for a while.
		/// </summary>
		public void Restart()
		{
			m_particles = null;
			SetVerticesDirty();
		}

		protected override void Awake()
		{
			base.Awake();
			raycastTarget = false;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_lastTime = -1;
		}

#if UNITY_EDITOR
		protected override void Reset()
		{
			base.Reset();
			raycastTarget = false;
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			m_particles = null;
			m_warnedMixedTextures = false;
		}
#endif

		protected virtual void Update()
		{
			float dt = GetDeltaTime();

#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				if (!m_animateInEditMode)
					return;

				// Edit Mode only runs Update when something changes; keep the loop going.
				UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
			}
#endif

			if (m_particles == null || dt <= 0)
				return;

			Simulate(dt);
			SetVerticesDirty();
		}

		private float GetDeltaTime()
		{
			if (!Application.isPlaying || m_unscaledTime)
			{
				double now = Time.realtimeSinceStartupAsDouble;
				float result = m_lastTime < 0 ? 0 : (float)(now - m_lastTime);
				m_lastTime = now;
				return Mathf.Min(result, MaxDeltaTime);
			}

			return Mathf.Min(Time.deltaTime, MaxDeltaTime);
		}

		private void Simulate( float _dt )
		{
			m_time += _dt;
			Rect rect = rectTransform.rect;

			for (int i = 0; i < m_particles.Length; i++)
			{
				ref Particle p = ref m_particles[i];
				p.Y -= p.FallSpeed * _dt;
				p.X += m_wind * _dt;
				p.Rotation += p.RotationSpeed * _dt;

				float margin = p.Size + p.SwayAmplitude;
				if (p.Y < rect.yMin - p.Size)
				{
					InitParticle(ref p, rect, false);
					continue;
				}

				// Wrap horizontally, so wind does not blow the rect empty
				if (p.X < rect.xMin - margin)
					p.X += rect.width + 2 * margin;
				else if (p.X > rect.xMax + margin)
					p.X -= rect.width + 2 * margin;
			}
		}

		private void EnsureParticles( Rect _rect )
		{
			if (m_particles != null && m_particles.Length == m_count)
			{
				if (_rect != m_lastRect)
					Rescale(_rect);
				return;
			}

			m_particles = new Particle[m_count];
			for (int i = 0; i < m_count; i++)
				InitParticle(ref m_particles[i], _rect, true);

			m_lastRect = _rect;
		}

		// Keeps the distribution when the rect changes (resolution change, layout), instead of
		// leaving a gap or a pile-up.
		private void Rescale( Rect _rect )
		{
			if (m_lastRect.width <= 0 || m_lastRect.height <= 0)
			{
				m_particles = null;
				EnsureParticles(_rect);
				return;
			}

			for (int i = 0; i < m_particles.Length; i++)
			{
				ref Particle p = ref m_particles[i];
				p.X = _rect.xMin + (p.X - m_lastRect.xMin) / m_lastRect.width * _rect.width;
				p.Y = _rect.yMin + (p.Y - m_lastRect.yMin) / m_lastRect.height * _rect.height;
			}

			m_lastRect = _rect;
		}

		private void InitParticle( ref Particle _p, Rect _rect, bool _anywhere )
		{
			float sizeT = UnityEngine.Random.value;
			float speedT = Mathf.Lerp(UnityEngine.Random.value, sizeT, m_sizeSpeedCoupling);

			_p.Size = Mathf.Lerp(m_sizeRange.x, m_sizeRange.y, sizeT);
			_p.FallSpeed = Mathf.Lerp(m_fallSpeedRange.x, m_fallSpeedRange.y, speedT);
			_p.SwayAmplitude = RandomRange(m_swayAmplitudeRange);
			_p.SwayFrequency = RandomRange(m_swayFrequencyRange);
			_p.SwayPhase = UnityEngine.Random.value * Mathf.PI * 2;
			_p.Rotation = UnityEngine.Random.value * 360;
			_p.RotationSpeed = RandomRange(m_rotationSpeedRange) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
			_p.FlutterFrequency = RandomRange(m_flutterFrequencyRange);
			_p.FlutterPhase = UnityEngine.Random.value * Mathf.PI * 2;
			_p.SpriteIndex = m_sprites.Length > 0 ? UnityEngine.Random.Range(0, m_sprites.Length) : 0;
			_p.Color = Color.Lerp(m_colorA, m_colorB, UnityEngine.Random.value);

			_p.X = UnityEngine.Random.Range(_rect.xMin, _rect.xMax);
			_p.Y = _anywhere
				? UnityEngine.Random.Range(_rect.yMin, _rect.yMax + _p.Size)
				// A little random extra height, so particles do not arrive in lock-step rows
				: _rect.yMax + _p.Size * (1 + UnityEngine.Random.value);
		}

		private static float RandomRange( Vector2 _range ) => UnityEngine.Random.Range(_range.x, _range.y);

		protected override void OnPopulateMesh( VertexHelper _vh )
		{
			_vh.Clear();

			Rect rect = rectTransform.rect;
			if (rect.width <= 0 || rect.height <= 0)
				return;

			EnsureParticles(rect);
			CheckTextures();

			for (int i = 0; i < m_particles.Length; i++)
			{
				ref Particle p = ref m_particles[i];

				float alpha = 1;
				if (m_edgeFade > 0)
				{
					alpha = Mathf.Clamp01((rect.yMax - p.Y) / m_edgeFade)
					        * Mathf.Clamp01((p.Y - rect.yMin) / m_edgeFade);
					if (alpha <= 0)
						continue;
				}

				float x = p.X + Mathf.Sin(p.SwayPhase + m_time * p.SwayFrequency * Mathf.PI * 2) * p.SwayAmplitude;
				float halfW = p.Size * 0.5f;
				float halfH = halfW;
				if (p.FlutterFrequency > 0)
					halfW *= Mathf.Cos(p.FlutterPhase + m_time * p.FlutterFrequency * Mathf.PI * 2);

				Color c = p.Color * color;
				c.a *= alpha;
				AddQuad(_vh, new Vector2(x, p.Y), halfW, halfH, p.Rotation, GetUv(p.SpriteIndex), c);
			}
		}

		private static void AddQuad( VertexHelper _vh, Vector2 _center, float _halfW, float _halfH, float _rotation, Vector4 _uv, Color32 _color )
		{
			float rad = _rotation * Mathf.Deg2Rad;
			float cos = Mathf.Cos(rad);
			float sin = Mathf.Sin(rad);
			Vector2 right = new Vector2(cos, sin) * _halfW;
			Vector2 up = new Vector2(-sin, cos) * _halfH;

			int start = _vh.currentVertCount;
			_vh.AddVert(_center - right - up, _color, new Vector2(_uv.x, _uv.y));
			_vh.AddVert(_center - right + up, _color, new Vector2(_uv.x, _uv.w));
			_vh.AddVert(_center + right + up, _color, new Vector2(_uv.z, _uv.w));
			_vh.AddVert(_center + right - up, _color, new Vector2(_uv.z, _uv.y));
			_vh.AddTriangle(start, start + 1, start + 2);
			_vh.AddTriangle(start + 2, start + 3, start);
		}

		private Vector4 GetUv( int _spriteIndex )
		{
			var sprite = GetSprite(_spriteIndex);
			if (sprite == null)
				return new Vector4(0, 0, 1, 1);

			// A sprite on another texture would show a random part of the first texture; use the first sprite instead.
			if (sprite.texture != mainTexture)
				sprite = GetSprite(0);

			return DataUtility.GetOuterUV(sprite);
		}

		private Sprite GetSprite( int _index )
		{
			if (m_sprites != null && m_sprites.Length > 0)
			{
				var sprite = m_sprites[Mathf.Clamp(_index, 0, m_sprites.Length - 1)];
				if (sprite != null)
					return sprite;
			}

			if (s_defaultSprite == null)
				s_defaultSprite = Resources.Load<Sprite>(DefaultSpritePath);

			return s_defaultSprite;
		}

		private void CheckTextures()
		{
			if (m_warnedMixedTextures || m_sprites == null || m_sprites.Length < 2)
				return;

			Texture first = mainTexture;
			foreach (var sprite in m_sprites)
			{
				if (sprite != null && sprite.texture != first)
				{
					m_warnedMixedTextures = true;
					UiLog.LogWarning($"{nameof(UiFallingParticles)} on '{name}': sprites live on different textures; "
					                 + "only sprites on the first sprite's texture are drawn. Put them into one sprite atlas.", this);
					return;
				}
			}
		}

		[ContextMenu("Preset: Snow")]
		public void ApplySnowPreset()
		{
			RecordUndo("Snow Preset");
			m_count = 80;
			m_sizeRange = new Vector2(6, 20);
			m_fallSpeedRange = new Vector2(30, 80);
			m_sizeSpeedCoupling = 0.8f;
			m_wind = 8;
			m_swayAmplitudeRange = new Vector2(5, 20);
			m_swayFrequencyRange = new Vector2(0.15f, 0.4f);
			m_rotationSpeedRange = new Vector2(0, 30);
			m_flutterFrequencyRange = Vector2.zero;
			m_colorA = Color.white;
			m_colorB = new Color(0.85f, 0.92f, 1f, 0.5f);
			OnPresetApplied();
		}

		[ContextMenu("Preset: Leaves")]
		public void ApplyLeavesPreset()
		{
			RecordUndo("Leaves Preset");
			m_count = 25;
			m_sizeRange = new Vector2(20, 44);
			m_fallSpeedRange = new Vector2(50, 110);
			m_sizeSpeedCoupling = 0.5f;
			m_wind = 25;
			m_swayAmplitudeRange = new Vector2(20, 60);
			m_swayFrequencyRange = new Vector2(0.2f, 0.5f);
			m_rotationSpeedRange = new Vector2(30, 120);
			m_flutterFrequencyRange = new Vector2(0.3f, 1.0f);
			m_colorA = new Color(0.95f, 0.55f, 0.1f);
			m_colorB = new Color(0.7f, 0.2f, 0.05f);
			OnPresetApplied();
		}

		private void RecordUndo( string _name )
		{
#if UNITY_EDITOR
			UnityEditor.Undo.RecordObject(this, _name);
#endif
		}

		private void OnPresetApplied()
		{
#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
			Restart();
		}
	}
}
