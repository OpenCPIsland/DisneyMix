using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class RandomizeColorsSnakeScatter : RandomizeColors
	{
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
			ParticleSystem[] affectedSystems = AffectedSystems;
			foreach (ParticleSystem particleSystem in affectedSystems)
			{
				ParticleSystem.MainModule mainModule1 = particleSystem.main;
				mainModule1.startColor = startColor;
			}
			whichColor = (whichColor + 2) % Colors.Length;
			num = (whichColor + 1) % Colors.Length;
			AffectedSystems[2].GetComponent<SnakeScatter>().mStartColor = Color.Lerp(Colors[whichColor], Colors[num], t);
		}
	}
}
