using System.Collections.Generic;
using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class FireworkFountain : Firework
	{
		[HideInInspector]
		public int CenterFountain;

		[HideInInspector]
		public int LeftMiddleFountain = 1;

		[HideInInspector]
		public int RightMiddleFountain = 2;

		[HideInInspector]
		public int LeftShortFountain = 3;

		[HideInInspector]
		public int RightShortFountain = 4;

		[HideInInspector]
		public int CenterGlow = 5;

		[HideInInspector]
		public int LeftMiddleGlow = 6;

		[HideInInspector]
		public int RightMiddleGlow = 7;

		[HideInInspector]
		public int LeftShortGlow = 8;

		[HideInInspector]
		public int RightShortGlow = 9;

		public float setHeight;

		public float startSpeedDifference = 2f;

		public float minHeightVariance = -1f;

		public float maxHeightVariance = 2f;

		private Dictionary<ParticleSystem, float> mOriginalStartSpeeds;

		public void Awake()
		{
			mOriginalStartSpeeds = new Dictionary<ParticleSystem, float>();
			for (int i = 0; i < Explosions.Count; i++)
			{
				mOriginalStartSpeeds.Add(Explosions[i], Explosions[i].main.startSpeed.constant);
			}
		}

		public override bool Launch(Vector3 launchVector)
		{
			launchVector = new Vector3(launchVector.x, setHeight, launchVector.z);
			return base.Launch(launchVector);
		}

		public override void Explode()
		{
			float num = Random.Range(minHeightVariance, maxHeightVariance);
			for (int i = 0; i < Explosions.Count; i++)
			{
				ParticleSystem.MainModule mainModule1 = Explosions[i].main;
				mainModule1.startSpeed = mOriginalStartSpeeds[Explosions[i]] + num;
			}
			base.Explode();
		}

		public override void Return()
		{
			base.Return();
		}
	}
}
