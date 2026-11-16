using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class RandomizeColorsDeathstar : RandomizeColors
	{
		[HideInInspector]
		public int ConeA;

		[HideInInspector]
		public int ConeB = 1;

		[HideInInspector]
		public int Ring = 2;

		[HideInInspector]
		public int Flash = 3;

		public override void SetColor()
		{
			float t;
			int whichColor;
			if (mFireworksGame != null)
			{
				t = GetComponentInParent<FireworksGame>().GetColorLerp(Colors.Length, out whichColor);
			}
			else
			{
				whichColor = Random.Range(0, Colors.Length);
				t = Random.Range(0f, 1f);
			}
			int num = (whichColor + 1) % Colors.Length;
			Color startColor = Color.Lerp(Colors[whichColor], Colors[num], t);
			ParticleSystem.MainModule mainModule1 = AffectedSystems[ConeA].main;
			mainModule1.startColor = startColor;
			ParticleSystem.MainModule mainModule2 = AffectedSystems[ConeB].main;
			mainModule2.startColor = startColor;
			whichColor = (whichColor + 2) % Colors.Length;
			num = (whichColor + 1) % Colors.Length;
			startColor = Color.Lerp(Colors[whichColor], Colors[num], t);
			ParticleSystem.MainModule mainModule3 = AffectedSystems[Ring].main;
			mainModule3.startColor = startColor;
			ParticleSystem.MainModule mainModule4 = AffectedSystems[Flash].main;
			mainModule4.startColor = startColor;
		}
	}
}
