using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Fireworks
{
	public class Firework : MonoBehaviour
	{
		public Action OnFireworkLaunch = delegate
		{
		};

		public Action OnFireworkExplode = delegate
		{
		};

		public Vector3 OffScreenLocation;

		public List<ParticleSystem> Trails;

		public List<ParticleSystem> Explosions;

		public ParticleSystem ParentParticleSystem;

		[Space(10f)]
		public Sprite ButtonImage;

		public string TrailingSoundEvent;

		public string ExplodingSoundEvent;

		public string LaunchName;

		public float LaunchTime;

		public Button LaunchButton;

		public FireworksGame FireworksGame;

		[HideInInspector]
		public bool mIsPlaying;

		private Animator mAnimator;

		private float mCreateModeVolume = 0.5f;

		private float mPlayModeVolume = 1f;

		private float mAliveTimer;

		private float mAliveCheckInterval = 0.2f;

		public int ColorIndex { get; set; }

		public Animator Animator
		{
			get
			{
				if (mAnimator == null)
				{
					mAnimator = GetComponent<Animator>();
				}
				return mAnimator;
			}
		}

		public virtual bool Launch(Vector3 launchVector)
		{
			bool result = !mIsPlaying;
			if (!mIsPlaying)
			{
				base.transform.localPosition = launchVector;
				base.gameObject.SetActive(true);
				OnFireworkLaunch();
				mAliveTimer = mAliveCheckInterval;
				mIsPlaying = true;
				if (Toolbox.Instance.mFireworkGame.mGameStage != FireworksGame.FireworksStage.Creation)
				{
					if (LaunchName.Length == 0)
					{
						ParentParticleSystem.transform.localPosition = Vector3.zero;
						Explode();
					}
					else
					{
						foreach (ParticleSystem trail in Trails)
						{
							trail.Play();
						}
						Animator.Play(LaunchName, 0);
						if ((bool)Toolbox.Instance.mFireworkGame && TrailingSoundEvent.Length > 0 && LaunchTime > 0f)
						{
							BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent(TrailingSoundEvent, this);
						}
					}
				}
				else
				{
					ParentParticleSystem.transform.localPosition = Vector3.zero;
					Explode();
				}
			}
			return result;
		}

		public virtual void Explode()
		{
			OnFireworkExplode();
			foreach (ParticleSystem trail in Trails)
			{
				trail.Stop();
			}
			foreach (ParticleSystem explosion in Explosions)
			{
				explosion.Play();
			}
			if ((bool)Toolbox.Instance.mFireworkGame)
			{
				Toolbox.Instance.mFireworkGame.LightUpScene(ColorIndex);
				if (ExplodingSoundEvent != null)
				{
					if (Toolbox.Instance.mFireworkGame.mGameStage != FireworksGame.FireworksStage.Creation)
					{
						BaseGameController.Instance.Session.SessionSounds.SetVolumeEvent(ExplodingSoundEvent, mCreateModeVolume);
					}
					else
					{
						BaseGameController.Instance.Session.SessionSounds.SetVolumeEvent(ExplodingSoundEvent, mPlayModeVolume);
					}
					BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent(ExplodingSoundEvent, this);
				}
			}
			mAliveTimer = mAliveCheckInterval;
		}

		private void Update()
		{
			if (!mIsPlaying)
			{
				return;
			}
			mAliveTimer -= Time.deltaTime;
			if (mAliveTimer < 0f)
			{
				mAliveTimer = mAliveCheckInterval;
				if (!ParentParticleSystem.IsAlive(true))
				{
					Return();
				}
			}
		}

		public virtual void Return()
		{
			if (this is FireworkF) Debug.Log($"[FWDBG] Message firework Return() at t={Time.time:F2}");
			mIsPlaying = false;
			foreach (ParticleSystem trail in Trails)
			{
				trail.Stop();
			}
			foreach (ParticleSystem explosion in Explosions)
			{
				explosion.Stop();
			}
			base.transform.GetChild(0).localPosition = Vector3.zero;
			base.gameObject.SetActive(false);
			base.transform.localPosition = OffScreenLocation;
			if (LaunchButton != null && !LaunchButton.interactable)
			{
				LaunchButton.interactable = true;
			}
		}

		[ContextMenu("Find Parent")]
		private void FindParent()
		{
			if (ParentParticleSystem == null)
			{
				ParentParticleSystem = base.transform.Find("Firework_Shell").gameObject.GetComponent<ParticleSystem>();
			}
		}
	}
}
