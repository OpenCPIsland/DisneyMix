using UnityEngine;

public class ParticleSpeedAdjuster : MonoBehaviour
{
	public float PlaybackSpeed = 1f;

	private void Start()
	{
		ParticleSystem[] componentsInChildren = GetComponentsInChildren<ParticleSystem>();
		ParticleSystem[] array = componentsInChildren;
		foreach (ParticleSystem particleSystem in array)
		{
			ParticleSystem.MainModule mainModule1 = particleSystem.main;
			mainModule1.simulationSpeed = PlaybackSpeed;
		}
	}
}
