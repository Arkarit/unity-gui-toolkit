using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Global background music with crossfading — the music counterpart to
	/// <see cref="UiSound"/>. A single, self-bootstrapping instance plays the looping (or
	/// one-shot) tracks defined in the project's <see cref="UiSoundConfig"/> (its music
	/// section), addressed by a plain string id. Switching tracks crossfades over a
	/// configurable duration.
	///
	/// Content lives in the same <see cref="UiSoundConfig"/> asset the host supplies via
	/// <see cref="UiToolkitConfiguration"/>. The host owns mute/volume through
	/// <see cref="MutedProvider"/> / <see cref="VolumeProvider"/> — the same contract as
	/// <see cref="UiSound"/>, so the game's music setting drives it. Both are optional.
	///
	/// Ducking: a prominent one-shot (typically a jingle) can briefly lower the music
	/// while it plays. Any <see cref="UiSoundConfig.SoundDef"/> with
	/// <see cref="UiSoundConfig.SoundDef.DuckMusic"/> enabled attenuates the music for the
	/// clip's duration. Both <see cref="UiSound"/> and game SFX code route through
	/// <see cref="Duck(UiSoundConfig.SoundDef, float)"/>, so library UI sounds and client
	/// SFX duck the music the same way.
	/// </summary>
	public class UiMusic : MonoBehaviour
	{
		/// When assigned and it returns true, the music is silenced. The track keeps
		/// playing logically, so clearing the mute resumes it in place.
		public static Func<bool> MutedProvider;

		/// When assigned, scales the music volume; expected range [0..1].
		public static Func<float> VolumeProvider;

		private static UiMusic s_instance;

		// Bootstrap runs after scene load, so a Play() issued during earlier startup would
		// be lost. Remember the latest such request and apply it once the instance exists.
		private static string s_pendingId;
		private static float s_pendingFade;
		private static bool s_hasPending;

		private UiSoundConfig m_config;
		private readonly List<Channel> m_channels = new List<Channel>();
		private readonly List<DuckEnvelope> m_ducks = new List<DuckEnvelope>();
		private string m_currentId;
		private bool m_paused;

		// One AudioSource per concurrently-audible track. On a switch the current channel
		// fades out (Target 0) while a fresh one fades in (Target 1); a channel is reused
		// once it has fully faded out, so only a handful ever exist.
		private class Channel
		{
			public AudioSource Source;
			public float BaseVolume;  // track.Volume * musicMaster
			public float Fade;        // current crossfade level [0..1]
			public float Target;      // 0 (fading out) or 1 (fading in / active)
			public float Speed;       // fade units per second
			public bool Active;       // true for the current track; outgoing channels are false
		}

		// A timed music attenuation: fade down to Target over Attack, hold for the
		// one-shot's length, fade back up over Release. Several may overlap; the most
		// aggressive (lowest multiplier) wins each frame.
		private class DuckEnvelope
		{
			public float Target;   // music multiplier while held [0..1]
			public float Attack;
			public float Hold;
			public float Release;
			public float Elapsed;

			public bool Done => Elapsed >= Attack + Hold + Release;

			public float Multiplier()
			{
				float e = Elapsed;
				if (e < Attack)
					return Attack > 0f ? Mathf.Lerp(1f, Target, e / Attack) : Target;
				e -= Attack;
				if (e < Hold)
					return Target;
				e -= Hold;
				if (e < Release)
					return Release > 0f ? Mathf.Lerp(Target, 1f, e / Release) : 1f;
				return 1f;
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Bootstrap()
		{
			if (s_instance != null)
				return;

			var go = new GameObject(nameof(UiMusic));
			DontDestroyOnLoad(go);
			go.AddComponent<UiMusic>();
		}

		/// <summary>Overrides the sound config at runtime (otherwise taken from UiToolkitConfiguration).</summary>
		public static void SetConfig( UiSoundConfig _config )
		{
			if (s_instance != null)
				s_instance.m_config = _config;
		}

		/// <summary>
		/// Plays (crossfading to) the music track with the given id. A negative fade uses
		/// the config's default crossfade duration; 0 switches instantly. Replaying the
		/// current track is a no-op.
		/// </summary>
		public static void Play( string _id, float _fade = -1f )
		{
			if (s_instance != null)
			{
				s_instance.PlayInternal(_id, _fade);
				return;
			}

			s_pendingId = _id;
			s_pendingFade = _fade;
			s_hasPending = true;
		}

		/// <summary>Fades out and stops the current track. Negative fade uses the config default; 0 stops instantly.</summary>
		public static void Stop( float _fade = -1f )
		{
			if (s_instance != null)
				s_instance.StopInternal(_fade);
			else
				s_hasPending = false;
		}

		/// <summary>Pauses the music in place (playback positions are held); resume with <see cref="Resume"/>.</summary>
		public static void Pause()
		{
			if (s_instance != null)
				s_instance.SetPaused(true);
		}

		/// <summary>Resumes music paused with <see cref="Pause"/>.</summary>
		public static void Resume()
		{
			if (s_instance != null)
				s_instance.SetPaused(false);
		}

		/// <summary>The id of the current track, or null if none is playing.</summary>
		public static string CurrentTrackId => s_instance != null ? s_instance.m_currentId : null;

		/// <summary>
		/// Ducks the music for the duration of a one-shot, if the sound requests it
		/// (<see cref="UiSoundConfig.SoundDef.DuckMusic"/>). Called by <see cref="UiSound"/> and
		/// game SFX code so both duck consistently. The hold time follows the clip's real
		/// (pitch-adjusted) length, so the music recovers just as the sound ends.
		/// </summary>
		public static void Duck( UiSoundConfig.SoundDef _def, float _pitch = 1f )
		{
			if (s_instance == null || _def == null || !_def.DuckMusic || _def.Clip == null)
				return;

			float pitch = Mathf.Abs(_pitch) > 0.0001f ? Mathf.Abs(_pitch) : 1f;
			float hold = _def.Clip.length / pitch;
			s_instance.AddDuck(_def.DuckVolume, _def.DuckAttack, hold, _def.DuckRelease);
		}

		/// <summary>Ducks the music with explicit parameters: target multiplier plus attack/hold/release seconds.</summary>
		public static void Duck( float _targetVolume, float _attack, float _hold, float _release )
		{
			if (s_instance != null)
				s_instance.AddDuck(_targetVolume, _attack, _hold, _release);
		}

		private void Awake()
		{
			if (s_instance != null && s_instance != this)
			{
				Destroy(gameObject);
				return;
			}
			s_instance = this;

			var config = UiToolkitConfiguration.Instance;
			if (config != null)
				m_config = config.UiSoundConfig;

			if (s_hasPending)
			{
				PlayInternal(s_pendingId, s_pendingFade);
				s_hasPending = false;
			}
		}

		private void OnDestroy()
		{
			if (s_instance == this)
				s_instance = null;
		}

		private void PlayInternal( string _id, float _fade )
		{
			if (m_config == null)
				return;

			var track = m_config.ResolveMusic(_id);
			if (track == null || track.Clip == null)
			{
				if (m_config.DebugLog)
					UiLog.Log($"Music '{_id}' skipped — no track/clip configured", this, nameof(UiMusic));
				return;
			}

			// Already the current, still-audible track → leave it running.
			if (_id == m_currentId)
			{
				foreach (var channel in m_channels)
				{
					if (channel.Active && channel.Source.isPlaying)
						return;
				}
			}

			float fade = _fade < 0f ? m_config.MusicDefaultFade : _fade;
			float speed = fade > 0f ? 1f / fade : float.PositiveInfinity;

			// Fade out whatever is currently playing.
			foreach (var channel in m_channels)
			{
				channel.Active = false;
				channel.Target = 0f;
				channel.Speed = speed;
			}

			var incoming = AcquireChannel();
			incoming.Source.clip = track.Clip;
			incoming.Source.loop = track.Loop;
			incoming.Source.pitch = 1f;
			// Route through the optional music mixer group (re-applied each play so a config
			// swap via SetConfig takes effect); null routes directly to the AudioListener.
			incoming.Source.outputAudioMixerGroup = m_config.MusicOutputMixerGroup;
			incoming.BaseVolume = track.Volume * m_config.MusicMasterVolume;
			incoming.Fade = 0f;
			incoming.Target = 1f;
			incoming.Speed = speed;
			incoming.Active = true;
			incoming.Source.volume = 0f;
			incoming.Source.Play();
			if (m_paused)
				incoming.Source.Pause();

			m_currentId = _id;

			if (m_config.DebugLog)
				UiLog.Log($"Music '{_id}' → crossfade {fade:F2}s (clip '{track.Clip.name}', loop {track.Loop})", this, nameof(UiMusic));
		}

		private void StopInternal( float _fade )
		{
			float fallback = m_config != null ? m_config.MusicDefaultFade : 0f;
			float fade = _fade < 0f ? fallback : _fade;
			float speed = fade > 0f ? 1f / fade : float.PositiveInfinity;

			foreach (var channel in m_channels)
			{
				channel.Active = false;
				channel.Target = 0f;
				channel.Speed = speed;
			}
			m_currentId = null;
		}

		private void SetPaused( bool _paused )
		{
			if (m_paused == _paused)
				return;

			m_paused = _paused;
			foreach (var channel in m_channels)
			{
				if (_paused)
					channel.Source.Pause();
				else
					channel.Source.UnPause();
			}
		}

		private void AddDuck( float _target, float _attack, float _hold, float _release )
		{
			m_ducks.Add(new DuckEnvelope
			{
				Target = Mathf.Clamp01(_target),
				Attack = Mathf.Max(0f, _attack),
				Hold = Mathf.Max(0f, _hold),
				Release = Mathf.Max(0f, _release),
				Elapsed = 0f,
			});
		}

		// Reuses a channel that has fully faded out, or grows the (tiny) pool.
		private Channel AcquireChannel()
		{
			foreach (var channel in m_channels)
			{
				if (!channel.Active && channel.Fade <= 0f)
				{
					channel.Source.Stop();
					return channel;
				}
			}

			var source = gameObject.AddComponent<AudioSource>();
			source.playOnAwake = false;
			var created = new Channel { Source = source };
			m_channels.Add(created);
			return created;
		}

		// Music is driven off unscaled time so pausing the game (Time.timeScale = 0) does
		// not freeze fades or ducking.
		private void Update()
		{
			if (m_paused)
				return;

			float dt = Time.unscaledDeltaTime;

			// Advance ducks; the most aggressive (lowest) overlapping value wins.
			float duck = 1f;
			for (int i = m_ducks.Count - 1; i >= 0; i--)
			{
				var envelope = m_ducks[i];
				envelope.Elapsed += dt;
				if (envelope.Done)
				{
					m_ducks.RemoveAt(i);
					continue;
				}
				duck = Mathf.Min(duck, envelope.Multiplier());
			}

			// Host mute/volume — same contract as UiSound.
			float external = 1f;
			if (MutedProvider != null && MutedProvider())
				external = 0f;
			else if (VolumeProvider != null)
				external = Mathf.Clamp01(VolumeProvider());

			foreach (var channel in m_channels)
			{
				channel.Fade = Mathf.MoveTowards(channel.Fade, channel.Target, channel.Speed * dt);
				channel.Source.volume = Mathf.Clamp01(channel.BaseVolume * channel.Fade * duck * external);

				// A fully faded-out, non-current channel is done — stop it so AcquireChannel
				// can recycle it (and so a non-looping outgoing clip cannot resurface).
				if (!channel.Active && channel.Fade <= 0f && channel.Source.isPlaying)
				{
					channel.Source.Stop();
					channel.Source.clip = null;
				}
			}
		}
	}
}
