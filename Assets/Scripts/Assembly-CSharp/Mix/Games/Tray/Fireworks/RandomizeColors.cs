using System;
using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class RandomizeColors : MonoBehaviour
	{
		public Color[] Colors;

		public ParticleSystem[] AffectedSystems;

		private int lastColorIndex;

		protected FireworksGame mFireworksGame;

		protected Firework mFirework;

		private void OnEnable()
		{
			if (mFirework == null)
			{
				mFirework = GetComponent<Firework>();
			}
			Firework firework = mFirework;
			firework.OnFireworkLaunch = (Action)Delegate.Combine(firework.OnFireworkLaunch, new Action(SetColor));
		}

		private void OnDisable()
		{
			Firework firework = mFirework;
			firework.OnFireworkLaunch = (Action)Delegate.Remove(firework.OnFireworkLaunch, new Action(SetColor));
		}

		private void Awake()
		{
			mFireworksGame = GetComponentInParent<FireworksGame>();
		}

		public int GetColorIndex()
		{
			return lastColorIndex;
		}

		public virtual void SetColor()
		{
			float t;
			int whichColor;
			if (mFireworksGame != null)
			{
				t = mFireworksGame.GetColorLerp(Colors.Length, out whichColor);
			}
			else
			{
				whichColor = UnityEngine.Random.Range(0, Colors.Length);
				t = UnityEngine.Random.Range(0f, 1f);
			}
			lastColorIndex = whichColor;
			int num = (whichColor + 1) % Colors.Length;
			Color.Lerp(Colors[whichColor], Colors[num], t);
			Color startColor = Color.Lerp(Colors[whichColor], Colors[num], t);
			ParticleSystem[] affectedSystems = AffectedSystems;
			foreach (ParticleSystem particleSystem in affectedSystems)
			{
				ParticleSystem.MainModule mainModule1 = particleSystem.main;
				mainModule1.startColor = startColor;
			}
			mFirework.ColorIndex = whichColor;
		}

		public virtual void SetColorR()
		{
			Color startColor = Colors[UnityEngine.Random.Range(0, Colors.Length)];
			ParticleSystem[] affectedSystems = AffectedSystems;
			foreach (ParticleSystem particleSystem in affectedSystems)
			{
				ParticleSystem.MainModule mainModule2 = particleSystem.main;
				mainModule2.startColor = startColor;
			}
		}
	}
}
