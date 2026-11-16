using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class TextureFirework : MonoBehaviour
	{
		private delegate void mDelUpdate();

		public int NumXRow = 100;

		public int NumYRow = 100;

		public Vector2 UVStart = new Vector2(0f, 0f);

		public Vector2 UVEnd = new Vector2(1f, 1f);

		public Texture2D Texture;

		public float Size = 1f;

		public float RoundOut = -1f;

		public float PosVarianceRate = 1f;

		private mDelUpdate mCurUpdatefunc;

		private ParticleSystem mParticleSystem;

		private float mTimeExplodeOut;

		private Vector2 mSizeRatio;

		private ParticleSystem.Particle[] mParticles;

		[SerializeField]
		private int TotalNumberOfParticles;

		[SerializeField]
		private bool IsBuilt;

		[SerializeField]
		private Vector3[] Positions;

		[SerializeField]
		private Color[] Colors;

		private void NullUpdate()
		{
		}

		private void Awake()
		{
			if (!IsBuilt)
			{
				Debug.LogError("Firework is not built on object " + base.transform.name + ". Please right-click on TextureFirework script AND 'Build Firework' from context menu.");
				return;
			}
			mCurUpdatefunc = NullUpdate;
			mParticles = new ParticleSystem.Particle[TotalNumberOfParticles];
			mParticleSystem = GetComponent<ParticleSystem>();
		}

		[ContextMenu("Reset Firework")]
		private void ResetFirework()
		{
			Colors = null;
			Positions = null;
			IsBuilt = false;
			TotalNumberOfParticles = 0;
			Debug.Log("Reset the Firework's colors and positions");
		}

		[ContextMenu("Build Firework")]
		private void BuildFirework()
		{
			Debug.Log("Build");
			ResetFirework();
			mParticleSystem = GetComponent<ParticleSystem>();
			mSizeRatio.x = (float)Texture.width * (UVEnd.x - UVStart.x);
			mSizeRatio.y = (float)Texture.height * (UVEnd.y - UVStart.y);
			Colors = new Color[NumXRow * NumYRow];
			Positions = new Vector3[NumXRow * NumYRow];
			int num = 0;
			for (int i = 0; i < NumXRow; i++)
			{
				for (int j = 0; j < NumYRow; j++)
				{
					float num2 = ((i != 0) ? (UVStart.x + (float)i / (float)(NumXRow - 1) * (UVEnd.x - UVStart.x)) : UVStart.x);
					float num3 = ((j != 0) ? (UVStart.y + (float)j / (float)(NumYRow - 1) * (UVEnd.y - UVStart.y)) : UVStart.y);
					Texture.GetPixelBilinear(num2, num3);
					bool flag = false;
					if (RoundOut > 0f && Vector3.Distance(new Vector3((0f - mSizeRatio.x) / 2f, (0f - mSizeRatio.y) / 2f) + new Vector3(mSizeRatio.x * num2, mSizeRatio.y * num3, 0f), Vector3.zero) > RoundOut)
					{
						flag = true;
					}
					Color pixelBilinear = Texture.GetPixelBilinear(num2, num3);
					if (pixelBilinear.a > 0.1f && !flag)
					{
						Positions[num] = new Vector3((0f - mSizeRatio.x) / 2f, (0f - mSizeRatio.y) / 2f) + new Vector3(mSizeRatio.x * num2, mSizeRatio.y * num3);
						Positions[num] += new Vector3(Random.Range(0f, PosVarianceRate), Random.Range(0f, PosVarianceRate), 0f);
						Colors[num] = pixelBilinear;
						num++;
					}
				}
			}
			TotalNumberOfParticles = num;
			Color[] array = new Color[TotalNumberOfParticles];
			Vector3[] array2 = new Vector3[TotalNumberOfParticles];
			for (int k = 0; k < TotalNumberOfParticles; k++)
			{
				array[k] = Colors[k];
				array2[k] = Positions[k];
			}
			Colors = null;
			Positions = null;
			Colors = array;
			Positions = array2;
			array = null;
			array2 = null;
			IsBuilt = true;
			Debug.Log(base.transform.name + "'s color and position arrays are now built");
			ParticleSystem.MainModule mainModule1 = mParticleSystem.main;
			mainModule1.maxParticles = TotalNumberOfParticles;
		}

		public void Explode()
		{
			mTimeExplodeOut = 0f;
			mParticleSystem.Emit(TotalNumberOfParticles);
			int particles = mParticleSystem.GetParticles(mParticles);
			int num = 0;
			if (mParticles != null)
			{
				for (int i = 0; i < particles; i++)
				{
					mParticles[i].position = base.transform.localPosition;
					mParticles[i].startColor = new Color(mParticleSystem.main.startColor.color.r, mParticleSystem.main.startColor.color.g, mParticleSystem.main.startColor.color.b, Colors[num].a);
					mParticles[i].velocity = Positions[num] * Size;
					num++;
					if (num == TotalNumberOfParticles)
					{
						num = 0;
					}
				}
				mParticleSystem.SetParticles(mParticles, particles);
			}
			mCurUpdatefunc = NullUpdate;
		}

		private void ExplodeUpdate()
		{
			if (mTimeExplodeOut < 2f)
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
				mTimeExplodeOut += Time.deltaTime;
			}
			else
			{
				mCurUpdatefunc = NullUpdate;
			}
		}

		private void FixedUpdate()
		{
			mCurUpdatefunc();
		}

		public void Return()
		{
			mParticleSystem.Clear();
			mParticleSystem.Stop();
		}
	}
}
