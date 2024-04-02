using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Unity can scale particle systems by setting, but has NO option to scale gravity as well.
	/// https://forum.unity.com/threads/shuriken-make-particle-system-completely-scale.497226/
	/// </summary>
	[ExecuteAlways]
	public class ParticleGravityFixer : MonoBehaviour
	{
		[SerializeField] protected float m_InitialGravityScale = 1;

		[HideInInspector][SerializeField] protected readonly List<ParticleSystem> m_particleSystems = new List<ParticleSystem>();
		[HideInInspector][SerializeField] protected readonly List<float> m_particleGravityScales = new List<float>();

		void Start()
		{
			RefreshGravity();
		}

		private void CollectParticleSystems()
		{
			RestoreGravityModifiersIfNecessary();

			ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
			foreach (var particleSystem in particleSystems)
			{
				var gravityModifier = particleSystem.main.gravityModifier;
				if (gravityModifier.mode != ParticleSystemCurveMode.Constant)
				{
					Debug.LogError("ParticleGravityFixer supports only constant gravities");
					continue;
				}

				m_particleSystems.Add(particleSystem);
				m_particleGravityScales.Add(gravityModifier.constant);
			}
		}

		private void ApplyScaledGravity()
		{
			Vector3 scale = transform.lossyScale;
			float average = (scale.x + scale.y + scale.z) / 3.0f;
			float factor = average / m_InitialGravityScale;

			for (int i=0; i<m_particleSystems.Count; i++)
			{
				var main = m_particleSystems[i].main;
				main.gravityModifier = m_particleGravityScales[i] * factor;
			}
		}

		private void RestoreGravityModifiersIfNecessary()
		{
			if (m_particleSystems.Count != 0)
			{
				Debug.Assert(m_particleSystems.Count == m_particleGravityScales.Count);
				if (m_particleSystems.Count != m_particleGravityScales.Count)
					return;

				for (int i = 0; i < m_particleSystems.Count; i++)
				{
					var main = m_particleSystems[i].main;
					main.gravityModifier = m_particleGravityScales[i];
				}

				m_particleSystems.Clear();
				m_particleGravityScales.Clear();
			}
		}

		private void RefreshGravity()
		{
			CollectParticleSystems();
			ApplyScaledGravity();
		}

#if UNITY_EDITOR
		// Update is called once per frame
		void Update()
		{
			if (!Application.isPlaying)
				RefreshGravity();
		}
#endif

		private void OnValidate()
		{
			if (m_InitialGravityScale == 0)
				m_InitialGravityScale = 1;
			RefreshGravity();
		}

		private void OnTransformChildrenChanged()
		{
			RefreshGravity();
		}

		private void OnDestroy()
		{
			RestoreGravityModifiersIfNecessary();
		}
	}
}