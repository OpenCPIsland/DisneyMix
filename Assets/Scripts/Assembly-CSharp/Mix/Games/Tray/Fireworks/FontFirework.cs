using System;
using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class FontFirework : MonoBehaviour
	{
		private delegate void mDelUpdate();

		public const int MAX_NUM_OF_CHARACTERS = 16;

		public Font mFont;

		public string mMessage = "Hello World";

		public int mMultiRate = 1;

		public float mSizeMod = 0.05f;

		public float PositionRandomness = 0.2f;

		public bool mIsSpread = true;

		public float mSpreadSpeed = 1f;

		public float mRandomRate = 1f;

		public float mExplosionDelay = 2f;

		public Vector3 mStartV = new Vector3(0f, 0f, 0f);

		public Vector3 mStartControlV = new Vector3(2.5f, 2.5f, 0f);

		public Vector3 mEndControlV = new Vector3(-2.5f, 2.5f, 0f);

		public Vector3 mEndV = new Vector3(8f, 0f, 0f);

		public bool mIsCurveDrawn;

		public float mCurveSpeed;

		public float mExplosionLerpColorFade = 0.02f;

		public float mFlyLerpSpeed = 0.97f;

		public float mBurstSpeed = 5f;

		public int mMaxGlyphWidth = 13;

		private float mTimeMod;

		private float mBezTimer;

		public float mBezierPercentage = 1f;

		private bool isFlipped;

		private bool isSquashed;

		private int mSquashedDif;

		private int mNumXRow;

		private int mNumYRow;

		private int mNumCharacters;

		private int mTotalNum;

		private int mGlyphHeight;

		private int mGlyphWidth;

		private int mCenterIndex;

		private float mTimer;

		private Texture2D mFontTexture;

		private ParticleSystem mParticleSystem;

		private Vector2 mUvStart;

		private Vector2 mUvEnd;

		private Vector3 mStartPos;

		private Bezier mBezierCurve;

		private Bezier mTravelBezier;

		private mDelUpdate mCurUpdatefunc;

		private mDelUpdate mTravelUpdateFunc;

		private float[] mBezPosTimes;

		private Vector3[] mPositions;

		private Vector3[] mBezPositons;

		private Color[] mColors;

		private ParticleSystem.Particle[] mParticles;

		public bool built;

		public bool build;

		private void Start()
		{
			mCurUpdatefunc = NullUpdate;
			mTravelUpdateFunc = NullUpdate;
			if (build)
			{
				BuildFirework();
			}
		}

		public void BuildFirework()
		{
			mParticleSystem = GetComponent<ParticleSystem>();
			if (mMessage.Length > 16)
			{
				mMessage = mMessage.Remove(16);
			}
			mMessage = mMessage.ToUpper();
			mNumCharacters = mMessage.Length;
			Debug.Log("this.mNumCharacters: " + mNumCharacters);
			Debug.Log("mNumCharacters: " + mNumCharacters);
			if (mNumCharacters == 0)
			{
				return;
			}
			mFontTexture = (Texture2D)mFont.material.mainTexture;
			CharacterInfo info = default(CharacterInfo);
			int num = 0;
			for (int i = 0; i < mMessage.Length; i++)
			{
				mFont.GetCharacterInfo(mMessage[i], out info);
				int glyphWidth = info.glyphWidth;
				int glyphHeight = info.glyphHeight;
				if (glyphWidth > mMaxGlyphWidth)
				{
					glyphWidth = mMaxGlyphWidth;
				}
				num += glyphWidth * glyphHeight * mMultiRate * mMultiRate;
			}
			float num2 = 1f;
			mSizeMod *= num2 * (1.75f + (1f - (float)(mNumCharacters - 1) / 16f * 2f));
			float num3 = num2 * mBezierPercentage * (float)mNumCharacters / 16f;
			mTimeMod = mCurveSpeed / 180f * (float)num;
			if (mNumCharacters > 2)
			{
				mBezierCurve = new Bezier(mStartV * num3, mStartControlV * num3, mEndControlV * num3, (mEndV + new Vector3(4f * (1f - (float)(mNumCharacters - 2) / 14f), 0f, 0f)) * num3);
				mTravelBezier = new Bezier(mStartV * num3 + base.transform.position - (mEndV * num3 - mStartV * num3) / 2f, mStartControlV * num3, mEndControlV * num3, mEndV * num3 + base.transform.position - (mEndV * num3 - mStartV * num3) / 2f);
			}
			else
			{
				mBezierCurve = new Bezier(mStartV * num3, (mEndV * num3 - mStartV * num3) / 2f, -(mEndV * num3 - mStartV * num3) / 2f, (mEndV + new Vector3(2f, 0f, 0f)) * num3);
				mTravelBezier = new Bezier(mStartV * num3 + base.transform.position - (mEndV * num3 - mStartV * num3) / 2f, (mEndV * num3 - mStartV * num3) / 2f, -(mEndV * num3 - mStartV * num3) / 2f, mEndV * num3 + base.transform.position - (mEndV * num3 - mStartV * num3) / 2f);
			}
			mBezTimer = 0f;
			if (mIsCurveDrawn && mNumCharacters > 1)
			{
				mTravelBezier.DrawBezier(mNumCharacters, Vector3.zero);
			}
			mBezPositons = mBezierCurve.GetEquidistantPoints(mNumCharacters, 100f, out mBezPosTimes);
			mColors = new Color[num];
			mPositions = new Vector3[num];
			mStartPos = base.transform.parent.parent.position;
			mTimer = 0f;
			for (int j = 0; j < mMessage.Length; j++)
			{
				mFont.GetCharacterInfo(mMessage[j], out info);
				isFlipped = false;
				isSquashed = false;
				if (info.uvBottomLeft.x - info.uvTopLeft.x != 0f)
				{
					isFlipped = true;
				}
				int glyphWidth2 = info.glyphWidth;
				int glyphHeight2 = info.glyphHeight;
				if (glyphWidth2 > mMaxGlyphWidth)
				{
					glyphWidth2 = mMaxGlyphWidth;
					mSquashedDif = info.glyphWidth - glyphWidth2;
					isSquashed = true;
				}
				mGlyphHeight = glyphHeight2;
				mGlyphWidth = glyphWidth2;
				mUvStart = info.uvBottomLeft;
				mUvEnd = info.uvTopRight;
				Add(j);
			}
			ParticleSystem.MainModule mainModule1 = mParticleSystem.main;
			mainModule1.maxParticles = mTotalNum;
			ParticleSystem.EmissionModule emission = mParticleSystem.emission;
			emission.rateOverTime = new ParticleSystem.MinMaxCurve
			{
				constantMax = mTotalNum
			};
			ParticleSystem.MainModule mainModule2 = base.transform.GetChild(0).GetComponent<ParticleSystem>().main;
			mainModule2.maxParticles = mTotalNum * 48;
			mParticles = new ParticleSystem.Particle[mTotalNum];
			mCenterIndex = mTotalNum / 2;
			built = true;
		}

		private void Add(int curChar)
		{
			int num = mTotalNum;
			Vector3 vector = mBezPositons[curChar];
			if (isSquashed)
			{
				vector += -mBezierCurve.GetNormalAtTimeO(mBezPosTimes[curChar]) * mSizeMod * mSquashedDif / 2f * mMultiRate;
			}
			if (!isFlipped)
			{
				mNumXRow = mGlyphWidth * mMultiRate;
				mNumYRow = mGlyphHeight * mMultiRate;
				for (int i = 0; i < mNumXRow; i++)
				{
					for (int j = 0; j < mNumYRow; j++)
					{
						float u = ((i != 0) ? (mUvStart.x + (float)i / (float)(mNumXRow - 1) * (mUvEnd.x - mUvStart.x)) : mUvStart.x);
						float v = ((j != 0) ? (mUvStart.y + (float)j / (float)(mNumYRow - 1) * (mUvEnd.y - mUvStart.y)) : mUvStart.y);
						Color pixelBilinear = mFontTexture.GetPixelBilinear(u, v);
						if (pixelBilinear.a > 0.5f)
						{
							Quaternion quaternion = ((mNumCharacters <= 1) ? Quaternion.Euler(0f, 0f, 0f - Vector3.Angle(new Vector3(1f, 0f, 0f), mBezierCurve.GetTangetAtTimeO(0.5f))) : ((!((float)curChar / (float)(mNumCharacters - 1) < 0.5f)) ? Quaternion.Euler(0f, 0f, 0f - Vector3.Angle(new Vector3(1f, 0f, 0f), mBezierCurve.GetTangetAtTimeO((float)curChar / (float)(mNumCharacters - 1)))) : Quaternion.Euler(0f, 0f, Vector3.Angle(new Vector3(1f, 0f, 0f), mBezierCurve.GetTangetAtTimeO((float)curChar / (float)(mNumCharacters - 1))))));
							mPositions[num] = vector + quaternion * new Vector3((float)i * mSizeMod - (float)(mNumXRow / 2) * mSizeMod, (float)j * mSizeMod, 0f);
							mPositions[num] += new Vector3(UnityEngine.Random.Range(0f, mRandomRate * mSizeMod), UnityEngine.Random.Range(0f, mRandomRate * mSizeMod), 0f);
							mPositions[num] += (Vector3)UnityEngine.Random.insideUnitCircle * PositionRandomness;
							mColors[num] = pixelBilinear;
							num++;
						}
					}
				}
			}
			else
			{
				mNumXRow = mGlyphHeight * mMultiRate;
				mNumYRow = mGlyphWidth * mMultiRate;
				for (int k = 0; k < mNumYRow; k++)
				{
					for (int l = 0; l < mNumXRow; l++)
					{
						float u = ((k != 0) ? (mUvStart.x + (float)l / (float)(mNumXRow - 1) * (mUvEnd.x - mUvStart.x)) : mUvStart.x);
						float v = ((l != 0) ? (mUvStart.y + (float)k / (float)(mNumYRow - 1) * (mUvEnd.y - mUvStart.y)) : mUvStart.y);
						Color pixelBilinear2 = mFontTexture.GetPixelBilinear(u, v);
						if (pixelBilinear2.a > 0.1f)
						{
							Quaternion quaternion2 = ((mNumCharacters <= 1) ? Quaternion.Euler(0f, 0f, 0f - Vector3.Angle(new Vector3(1f, 0f, 0f), mBezierCurve.GetTangetAtTimeO(0.5f))) : ((!((float)curChar / (float)(mNumCharacters - 1) < 0.5f)) ? Quaternion.Euler(0f, 0f, 0f - Vector3.Angle(new Vector3(1f, 0f, 0f), mBezierCurve.GetTangetAtTimeO((float)curChar / (float)(mNumCharacters - 1)))) : Quaternion.Euler(0f, 0f, Vector3.Angle(new Vector3(1f, 0f, 0f), mBezierCurve.GetTangetAtTimeO((float)curChar / (float)(mNumCharacters - 1))))));
							mPositions[num] = vector + quaternion2 * new Vector3((float)k * mSizeMod - (float)(mNumXRow / 2) * mSizeMod, (float)l * mSizeMod, 0f);
							mPositions[num] += new Vector3(UnityEngine.Random.Range(0f, mRandomRate * mSizeMod), UnityEngine.Random.Range(0f, mRandomRate * mSizeMod), 0f);
							mColors[num] = pixelBilinear2;
							num++;
						}
					}
				}
			}
			mTotalNum = num;
		}

		public void Explode()
		{
			mParticleSystem.Emit(mTotalNum);
			int particles = mParticleSystem.GetParticles(mParticles);
			int num = 0;
			if (mParticles == null)
			{
				return;
			}
			for (int i = 0; i < particles; i++)
			{
				mParticles[i].position = Vector3.zero;
				mParticles[i].startColor = mColors[num];
				if (mIsSpread)
				{
					mParticles[i].velocity = mPositions[num] * mSpreadSpeed;
				}
				else
				{
					mParticles[i].position = base.transform.position + mPositions[num] - (mBezierCurve.EndPoint - mBezierCurve.StartPoint) / 2f;
				}
				num++;
				if (num == mTotalNum)
				{
					num = 0;
				}
			}
			mParticleSystem.SetParticles(mParticles, particles);
		}

		public void ExplodeB()
		{
			mParticleSystem.Play();
			mTimer = 0f;
			mBezTimer = 0f;
			ParticleSystem.EmissionModule emission = mParticleSystem.emission;
			emission.rateOverTime = new ParticleSystem.MinMaxCurve
			{
				constantMax = 300f
			};
			ParticleSystem.MainModule mainModule3 = mParticleSystem.main;
			mainModule3.gravityModifier = 0f;
			mCurUpdatefunc = (mDelUpdate)Delegate.Combine(mCurUpdatefunc, new mDelUpdate(FlyToPosition));
		}

		public void ExplodeC()
		{
			mParticleSystem.Play();
			mTimer = 0f;
			mBezTimer = 0f;
			ParticleSystem.EmissionModule emission = mParticleSystem.emission;
			emission.rateOverTime = new ParticleSystem.MinMaxCurve
			{
				constantMax = 300f
			};
			ParticleSystem.MainModule mainModule4 = mParticleSystem.main;
			mainModule4.gravityModifier = 0f;
			mCurUpdatefunc = (mDelUpdate)Delegate.Combine(mCurUpdatefunc, new mDelUpdate(FlyToPositionFromCenter));
		}

		public void ExplodeD()
		{
			mTimer = 0f;
			ParticleSystem.MainModule mainModule5 = mParticleSystem.main;
			mainModule5.gravityModifier = 0f;
			mParticleSystem.Emit(mTotalNum);
			int particles = mParticleSystem.GetParticles(mParticles);
			int num = 0;
			if (mParticles != null)
			{
				for (int i = 0; i < particles; i++)
				{
					mParticles[i].position = base.transform.position;
					mParticles[i].startColor = new Color((int)mParticles[i].startColor.r, (int)mParticles[i].startColor.b, (int)mParticles[i].startColor.g, mColors[i].a);
					mParticles[i].velocity = (mPositions[num] - (mBezierCurve.EndPoint - mBezierCurve.StartPoint) / 2f) * mBurstSpeed;
					num++;
					if (num == mTotalNum)
					{
						num = 0;
					}
				}
				mParticleSystem.SetParticles(mParticles, particles);
			}
			mCurUpdatefunc = NullUpdate;
			mCurUpdatefunc = (mDelUpdate)Delegate.Combine(mCurUpdatefunc, new mDelUpdate(ExplodeDUpdate));
		}

		public void Return()
		{
			mParticleSystem.Clear();
			mParticleSystem.Stop();
		}

		private void FixedUpdate()
		{
			mTravelUpdateFunc();
			mCurUpdatefunc();
		}

		private void NullUpdate()
		{
		}

		private void ExplodeDUpdate()
		{
			if (mTimer < 2f)
			{
				int particles = mParticleSystem.GetParticles(mParticles);
				if (mParticles != null)
				{
					for (int i = 0; i < particles; i++)
					{
						mParticles[i].velocity *= 0.93f;
					}
					mParticleSystem.SetParticles(mParticles, particles);
				}
				mTimer += Time.deltaTime;
			}
			else
			{
				mCurUpdatefunc = (mDelUpdate)Delegate.Remove(mCurUpdatefunc, new mDelUpdate(ExplodeDUpdate));
				mCurUpdatefunc = (mDelUpdate)Delegate.Combine(mCurUpdatefunc, new mDelUpdate(FinaleUpdateD));
			}
		}

		private void FlyToPosition()
		{
			int particles = mParticleSystem.GetParticles(mParticles);
			int num = 0;
			if (mParticles == null)
			{
				return;
			}
			for (int i = 0; i < particles; i++)
			{
				mParticles[i].position = Vector3.Lerp(mParticles[i].position, mStartPos + mPositions[num] - (mBezierCurve.EndPoint - mBezierCurve.StartPoint) / 2f, mFlyLerpSpeed) + base.transform.parent.localPosition + base.transform.parent.parent.localPosition - new Vector3(0f, mEndControlV.y * 2f, 0f);
				if (num < mTotalNum)
				{
					num++;
				}
				if (num >= mTotalNum)
				{
					mParticles[i].remainingLifetime = -1f;
				}
			}
			if (particles >= mTotalNum - 1)
			{
				mTimer += Time.deltaTime;
				if (mTimer > 0.2f)
				{
					mCurUpdatefunc = (mDelUpdate)Delegate.Remove(mCurUpdatefunc, new mDelUpdate(FlyToPosition));
					mCurUpdatefunc = (mDelUpdate)Delegate.Combine(mCurUpdatefunc, new mDelUpdate(FinaleUpdate));
					mTimer = 0f;
					ParticleSystem.EmissionModule emission = mParticleSystem.emission;
					emission.rateOverTime = new ParticleSystem.MinMaxCurve
					{
						constantMax = 0f
					};
					for (int j = mTotalNum; j < particles; j++)
					{
						mParticles[j].remainingLifetime = -1f;
					}
				}
			}
			mParticleSystem.SetParticles(mParticles, particles);
		}

		private void FlyToPositionFromCenter()
		{
			int particles = mParticleSystem.GetParticles(mParticles);
			int num = 0;
			if (mParticles == null)
			{
				return;
			}
			for (int i = 0; i < particles; i++)
			{
				int num2 = mCenterIndex + num;
				mParticles[i].position = Vector3.Lerp(mParticles[i].position, mStartPos + mPositions[num2] - (mBezierCurve.EndPoint - mBezierCurve.StartPoint) / 2f, mFlyLerpSpeed) + base.transform.parent.localPosition + new Vector3(0f, base.transform.parent.parent.localPosition.y, 0f) - new Vector3(0f, mEndControlV.y * 2f, 0f);
				if (num2 < mTotalNum)
				{
					if (num < 1)
					{
						num = -num;
						num++;
					}
					else
					{
						num = -num;
					}
				}
				if (num2 >= mTotalNum)
				{
					mParticles[i].remainingLifetime = -1f;
				}
			}
			if (particles >= mTotalNum - 1)
			{
				mTimer += Time.deltaTime;
				if (mTimer > 0.2f)
				{
					mCurUpdatefunc = (mDelUpdate)Delegate.Remove(mCurUpdatefunc, new mDelUpdate(FlyToPositionFromCenter));
					mCurUpdatefunc = (mDelUpdate)Delegate.Combine(mCurUpdatefunc, new mDelUpdate(FinaleUpdate));
					mTimer = 0f;
					for (int j = mTotalNum; j < particles; j++)
					{
						mParticles[j].remainingLifetime = -1f;
					}
					ParticleSystem.EmissionModule emission = mParticleSystem.emission;
					emission.rateOverTime = new ParticleSystem.MinMaxCurve
					{
						constantMax = 0f
					};
				}
			}
			mParticleSystem.SetParticles(mParticles, particles);
		}

		private void FinaleUpdate()
		{
			mTimer += Time.deltaTime;
			if (mTimer > mExplosionDelay)
			{
				int particles = mParticleSystem.GetParticles(mParticles);
				for (int i = 0; i < particles; i++)
				{
					mParticles[i].startColor = Color.Lerp(mParticles[i].startColor, new Color(0f, 0f, 0f, 0f), mExplosionLerpColorFade);
				}
				mParticleSystem.SetParticles(mParticles, particles);
			}
			if (mTimer > mExplosionDelay && mTimer < mExplosionDelay + 1f)
			{
				int particles2 = mParticleSystem.GetParticles(mParticles);
				ParticleSystem.MainModule mainModule6 = mParticleSystem.main;
				mainModule6.gravityModifier = 0.3f;
				for (int j = 0; j < particles2; j++)
				{
					Vector3 velocity = new Vector3(0f, 3f, 0f) + UnityEngine.Random.insideUnitSphere * 3f;
					velocity.Set(velocity.x, velocity.y, 0f);
					mParticles[j].velocity = velocity;
				}
				mParticleSystem.SetParticles(mParticles, particles2);
				mTimer += 1f;
			}
			else if (mTimer > mExplosionDelay + 2f)
			{
				mCurUpdatefunc = NullUpdate;
				mTimer = 0f;
			}
		}

		private void FinaleUpdateD()
		{
			mTimer += Time.deltaTime;
			if (mTimer > mExplosionDelay)
			{
				int particles = mParticleSystem.GetParticles(mParticles);
				for (int i = 0; i < particles; i++)
				{
					mParticles[i].startColor = Color.Lerp(mParticles[i].startColor, new Color(0f, 0f, 0f, 0f), mExplosionLerpColorFade);
				}
				mParticleSystem.SetParticles(mParticles, particles);
			}
			if (mTimer > mExplosionDelay && mTimer < mExplosionDelay + 1f)
			{
				int particles2 = mParticleSystem.GetParticles(mParticles);
				for (int j = 0; j < particles2; j++)
				{
					Vector3 velocity = new Vector3(0f, 0f - UnityEngine.Random.Range(0.01f, 1f), 0f);
					velocity.Set(velocity.x, velocity.y, 0f);
					mParticles[j].velocity = velocity;
				}
				mParticleSystem.SetParticles(mParticles, particles2);
				mTimer += 1f;
			}
			else if (mTimer > mExplosionDelay + 2f)
			{
				mCurUpdatefunc = (mDelUpdate)Delegate.Remove(mCurUpdatefunc, new mDelUpdate(FinaleUpdate));
			}
		}

		private void TravelAlongBezier()
		{
			base.transform.position = mTravelBezier.GetPointAtTimeO(mBezTimer);
			if (mBezTimer >= 1f)
			{
				mTravelUpdateFunc = (mDelUpdate)Delegate.Remove(mTravelUpdateFunc, new mDelUpdate(TravelAlongBezier));
			}
			mBezTimer += Time.deltaTime * mTimeMod;
			if (mBezTimer > 1f)
			{
				mBezTimer = 1f;
			}
		}
	}
}
