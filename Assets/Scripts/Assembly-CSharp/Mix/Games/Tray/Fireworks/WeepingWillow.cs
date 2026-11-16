using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class WeepingWillow : MonoBehaviour
	{
		private ParticleSystem ps;

		private ParticleSystem.Particle[] particles;

		public float mVelocityDecay = 0.97f;

		private void Start()
		{
			ps = GetComponent<ParticleSystem>();
			particles = new ParticleSystem.Particle[ps.main.maxParticles];
		}

		private void FixedUpdate()
		{
			int num = ps.GetParticles(particles);
			for (int i = 0; i < num; i++)
			{
			}
			ps.SetParticles(particles, num);
		}
	}
}
