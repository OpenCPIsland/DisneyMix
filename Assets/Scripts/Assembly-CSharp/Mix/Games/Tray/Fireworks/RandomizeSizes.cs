using System;
using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	[RequireComponent(typeof(Firework))]
	public class RandomizeSizes : MonoBehaviour
	{
		public float[] Sizes;

		public ParticleSystem[] AffectedSystems;

		private Firework mFirework;

		private void OnEnable()
		{
			if (mFirework == null)
			{
				mFirework = GetComponent<Firework>();
			}
			Firework firework = mFirework;
			firework.OnFireworkLaunch = (Action)Delegate.Combine(firework.OnFireworkLaunch, new Action(SetSizeR));
		}

		private void OnDisable()
		{
			Firework firework = mFirework;
			firework.OnFireworkLaunch = (Action)Delegate.Remove(firework.OnFireworkLaunch, new Action(SetSizeR));
		}

		public void SetSizeR()
		{
			float startSpeed = Sizes[UnityEngine.Random.Range(0, Sizes.Length)];
			ParticleSystem[] affectedSystems = AffectedSystems;
			foreach (ParticleSystem particleSystem in affectedSystems)
			{
				ParticleSystem.MainModule mainModule1 = particleSystem.main;
				mainModule1.startSpeed = startSpeed;
			}
		}
	}
}
