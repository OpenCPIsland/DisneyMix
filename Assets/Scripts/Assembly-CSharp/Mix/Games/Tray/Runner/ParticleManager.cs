using UnityEngine;

namespace Mix.Games.Tray.Runner
{
	[RequireComponent(typeof(ParticleSystem))]
	public class ParticleManager : MonoBehaviour
	{
		private ParticleSystem mParticle;

		private float mCurrentPlayback;

		private bool mIsPlaying;

		private void Awake()
		{
			mParticle = GetComponent<ParticleSystem>();
		}

		private void OnEnable()
		{
			if (mIsPlaying && (mCurrentPlayback < mParticle.main.duration || mParticle.main.loop))
			{
				mParticle.Simulate(mCurrentPlayback, false);
				mParticle.Play(false);
			}
		}

		private void OnDisable()
		{
			mIsPlaying = mParticle.emission.enabled;
			ParticleSystem.MainModule mainModule1 = mParticle.main;
			mainModule1.playOnAwake = false;
			mCurrentPlayback = mParticle.time;
		}
	}
}
