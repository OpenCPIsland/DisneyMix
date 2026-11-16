using System.Collections.Generic;
using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class SnakeScatter : MonoBehaviour
	{
		public float mDelayTime;

		public float mVelocityMultiplier;

		public float mVelocityDecayIntial;

		public float mVelocityDecay;

		public float mVelocityScatterDecay;

		public float mEndTime = 1f;

		public float mSecondAngle;

		public float mStartVelocity;

		public float mVariance = 15f;

		public float mMinSpeed = 0.1f;

		public int mNumParticlesPerFrame = 3;

		private bool mIsDelaydone;

		private bool mIsNotEmitting;

		private bool starting;

		[HideInInspector]
		public Color mStartColor;

		private int delay;

		private float mTime;

		private ParticleSystem mParticleSystem;

		private ParticleSystem.Particle[] mParticles;

		private float mTempMulti;

		private List<int> mRandomRange;

		private int[] mNumbers;

		private void Start()
		{
			mParticleSystem = GetComponent<ParticleSystem>();
			mParticles = new ParticleSystem.Particle[mParticleSystem.main.maxParticles];
			mStartColor = mParticleSystem.main.startColor.color;
			mRandomRange = new List<int>();
			mNumbers = new int[mParticleSystem.main.maxParticles];
			for (int i = 0; i < mParticleSystem.main.maxParticles; i++)
			{
				mNumbers[i] = i;
			}
		}

		public void Explode()
		{
			mTime = 0f;
			mIsDelaydone = false;
			mIsNotEmitting = false;
			mTempMulti = mVelocityDecay;
			delay = 0;
			ParticleSystem.EmissionModule emission = base.transform.GetChild(0).GetComponent<ParticleSystem>().emission;
			emission.rateOverTime = new ParticleSystem.MinMaxCurve
			{
				constantMax = 48f
			};
			starting = true;
			mRandomRange.Clear();
			mRandomRange.AddRange(mNumbers);
		}

		public void Return()
		{
			starting = false;
		}

		private void FixedUpdate()
		{
			if (!starting)
			{
				return;
			}
			if (mTime < 0.1f)
			{
				int particles = mParticleSystem.GetParticles(mParticles);
				for (int i = 0; i < particles; i++)
				{
					Vector3 vector = Quaternion.AngleAxis(Random.Range(0f - mVariance, mVariance) + (float)(360 / particles * i), Vector3.forward) * Vector3.right * mStartVelocity;
					mParticles[i].velocity = new Vector3(vector.x, vector.y, 0f);
				}
				mParticleSystem.SetParticles(mParticles, particles);
			}
			if (mTime < mDelayTime)
			{
				int particles2 = mParticleSystem.GetParticles(mParticles);
				if (mParticles[0].velocity.magnitude > mMinSpeed)
				{
					for (int j = 0; j < particles2; j++)
					{
						mParticles[j].velocity *= mVelocityDecayIntial;
					}
				}
				mParticleSystem.SetParticles(mParticles, particles2);
			}
			if (mTime - mDelayTime > mEndTime && !mIsNotEmitting)
			{
				ParticleSystem.EmissionModule emission = base.transform.GetChild(0).GetComponent<ParticleSystem>().emission;
				emission.rateOverTime = new ParticleSystem.MinMaxCurve
				{
					constantMax = 0f
				};
				mIsNotEmitting = true;
			}
			if (mTime > mDelayTime && !mIsDelaydone)
			{
				int particles3 = mParticleSystem.GetParticles(mParticles);
				for (int k = 0; k < mNumParticlesPerFrame; k++)
				{
					int index = Random.Range(0, mRandomRange.Count);
					int num = mRandomRange[index];
					mRandomRange.RemoveAt(index);
					Vector3 vector2 = Quaternion.AngleAxis(Random.Range(0f - mVariance, mVariance) + mSecondAngle + (float)(360 / particles3 * num + 1), Vector3.forward) * Vector3.right * mVelocityMultiplier;
					mParticles[num].velocity = new Vector3(vector2.x, vector2.y, 0f);
					delay++;
					if (delay == particles3)
					{
						break;
					}
				}
				mParticleSystem.SetParticles(mParticles, particles3);
				if (delay == particles3)
				{
					mIsDelaydone = true;
				}
			}
			else if (mTime > mDelayTime && mIsDelaydone)
			{
				int particles4 = mParticleSystem.GetParticles(mParticles);
				mTempMulti *= mVelocityDecay;
				if (mParticles[0].velocity.magnitude > mMinSpeed)
				{
					for (int l = 0; l < particles4; l++)
					{
						mParticles[l].velocity *= mVelocityScatterDecay;
						mParticles[l].startColor = Color.Lerp(mStartColor, new Color(0f, 0f, 0f, 0f), (mTime - mDelayTime) / 2f);
					}
				}
				mParticleSystem.SetParticles(mParticles, particles4);
			}
			mTime += Time.fixedDeltaTime;
		}
	}
}
