using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class RandomizeColorsFountain : RandomizeColors
	{
		public ParticleSystem[] AffectedTrails;

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
			Color.Lerp(Colors[whichColor], Colors[num], t);
			Color startColor = Color.Lerp(Colors[whichColor], Colors[num], t);
			whichColor = (whichColor + 2) % Colors.Length;
			num = (whichColor + 1) % Colors.Length;
			Color startColor2 = Color.Lerp(Colors[whichColor], Colors[num], t);
			for (int i = 0; i < AffectedSystems.Length; i++)
			{
				ParticleSystem.MainModule mainModule1 = AffectedSystems[i].main;
				mainModule1.startColor = startColor;
			}
			for (int i = 0; i < AffectedTrails.Length; i++)
			{
				ParticleSystem.MainModule mainModule2 = AffectedSystems[i].main;
				mainModule2.startColor = startColor2;
			}
		}
	}
}
