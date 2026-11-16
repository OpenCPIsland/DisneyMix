using UnityEngine;

namespace Mix.Games.Tray.Friendzy.FriendzyQuiz
{
	public class SelectionParticle : MonoBehaviour
	{
		[SerializeField]
		private ParticleSystem mSelectedParticle;

		[SerializeField]
		private ParticleSystem mExplosionParticle;

		public void SetParticleEffectColor(Color aColor)
		{
			ParticleSystem.MainModule mainModule1 = mSelectedParticle.main;
			mainModule1.startColor = aColor;
			ParticleSystem.MainModule mainModule2 = mExplosionParticle.main;
			mainModule2.startColor = aColor;
		}

		public void EnableSelectionParticle(bool aEnable)
		{
			ParticleSystem.EmissionModule emission = mSelectedParticle.emission;
			emission.enabled = aEnable;
		}

		public void EnableExplosionParticle(bool aEnable)
		{
			ParticleSystem.EmissionModule emission = mExplosionParticle.emission;
			emission.enabled = aEnable;
		}

		public void SelectionExplosion()
		{
			mExplosionParticle.Emit(10);
		}
	}
}
