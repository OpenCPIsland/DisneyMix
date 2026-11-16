using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class RandomizeColorsSnowflakeBurst : RandomizeColors
	{
		public FireworkSnowflake FireworkSnowflake;

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
			ParticleSystem[] affectedSystems = AffectedSystems;
			foreach (ParticleSystem particleSystem in affectedSystems)
			{
				ParticleSystem.MainModule mainModule1 = particleSystem.main;
				mainModule1.startColor = startColor;
			}
			Snowflake[] mSnowflakes = FireworkSnowflake.mSnowflakes;
			foreach (Snowflake snowflake in mSnowflakes)
			{
				ParticleSystem.MainModule mainModule2 = snowflake.transform.GetChild(0).GetComponent<ParticleSystem>().main;
				mainModule2.startColor = startColor2;
				ParticleSystem.MainModule mainModule3 = snowflake.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().main;
				mainModule3.startColor = new Color(startColor.r, startColor.g, startColor.b, 0.5f);
			}
		}
	}
}
