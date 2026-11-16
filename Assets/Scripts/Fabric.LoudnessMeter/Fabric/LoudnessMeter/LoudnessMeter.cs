using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fabric.LoudnessMeter
{
	[AddComponentMenu("Fabric/Mixing/LoudnessMeter")]
	public class LoudnessMeter : MonoBehaviour
	{
		private DeInterleavedBuffer deInterleavedBufer = new DeInterleavedBuffer();

		private IIRFilter filter1;

		private IIRFilter filter2;

		private int var1;

		private int var2;

		private int var3;

		private int var4;

		private int var5;

		private int var6;

		private int var7;

		private int var8;

		private int var9;

		private int var10;

		private double var11 = -70.0;

		private float var12 = -300f;

		private double var13;

		private double var14;

		private double var15;

		private double var16;

		private double var17;

		private double var18;

		private List<int> list1 = new List<int>();

		private List<List<double>> list2 = new List<List<double>>();

		private List<double> list3 = new List<double>();

		private List<double> list4 = new List<double>();

		private List<double> list5 = new List<double>();

		public float integratedLoudness;

		public List<float> shortTermLoudness = new List<float>();

		public List<float> momentaryLoudness = new List<float>();

		private static int ABS_THRES = -70;


		private static int PRC_LOW = 10;

		private static int PRC_HIGH = 95;

		private List<float> absgatedList = new List<float>();

		private List<float> rel_gate_vec = new List<float>();

		private void Awake()
		{
			filter1 = new IIRFilter(1.53512485958697, -2.69169618940638, 1.19839281085285, -1.69065929318241, 0.73248077421585);
			filter2 = new IIRFilter(1.0, -2.0, 1.0, -1.99004745483398, 0.99007225036621);
			var14 = var11;
			var15 = -80.0;
			var16 = 10.0;
			var17 = 0.1;
			var18 = Math.Pow(10.0, var17 / 10.0);
			int num = (int)Math.Ceiling((var16 - var15) / var17);
			for (int i = 0; i < num; i++)
			{
				list1.Add(0);
			}
			integratedLoudness = var12;
			Initialise(44100.0, 2, 1024, 20);
		}

		public void Initialise(double in1, int in2, int in3, int in4)
		{
			filter1.Initialise(in1, in2);
			filter2.Initialise(in1, in2);
			in4 = ((in4 >= 10) ? (((in4 - 1) / 10 + 1) * 10) : 10);
			while ((int)in1 % in4 != 0)
			{
				in4 += 10;
				if ((double)in4 > in1 / 2.0)
				{
					in4 = 10;
					break;
				}
			}
			var1 = in4 * 3;
			var2 = (int)(in1 / (double)in4);
			var3 = var1 * var2;
			var6 = (int)(0.1 * (double)in4);
			var4 = (int)(0.4 * (double)in4);
			var5 = var4 * var2;
			var8 = 0;
			var9 = 0;
			var7 = 0;
			list2.Clear();
			list3.Clear();
			list4.Clear();
			for (int i = 0; i != in2; i++)
			{
				List<double> list = new List<double>();
				for (int j = 0; j < var1; j++)
				{
					list.Add(0.0);
				}
				list2.Add(list);
				list3.Add(0.0);
				list4.Add(0.0);
			}
			list5.Clear();
			for (int k = 0; k != in2; k++)
			{
				if (k == 3 || k == 4)
				{
					list5.Add(1.41);
				}
				else
				{
					list5.Add(1.0);
				}
			}
			momentaryLoudness.Clear();
			shortTermLoudness.Clear();
			for (int l = 0; l != in2; l++)
			{
				momentaryLoudness.Add(var12);
				shortTermLoudness.Add(var12);
			}
			reset();
		}

		private void OnAudioFilterRead(float[] data, int channels)
		{
			deInterleavedBufer.SetBuffer(data, channels);
			filter1.process(deInterleavedBufer);
			filter2.process(deInterleavedBufer);
			for (int i = 0; i != deInterleavedBufer.GetNumChannels(); i++)
			{
				for (int j = 0; j != deInterleavedBufer.GetNumSamples(); j++)
				{
					float sample = deInterleavedBufer.GetSample(j, i);
					sample *= sample;
					deInterleavedBufer.SetSample(sample, j, i);
				}
			}
			if (var9 + deInterleavedBufer.GetNumSamples() < var2)
			{
				for (int k = 0; k != deInterleavedBufer.GetNumChannels(); k++)
				{
					for (int l = 0; l != deInterleavedBufer.GetNumSamples(); l++)
					{
						list2[k][var8] += deInterleavedBufer.GetSample(l, k);
					}
				}
				var9 += deInterleavedBufer.GetNumSamples();
				return;
			}
			int num = 0;
			bool flag = true;
			while (flag)
			{
				int num2 = deInterleavedBufer.GetNumSamples() - num;
				int num3;
				if (num2 < var2 - var9)
				{
					num3 = num2;
					flag = false;
				}
				else
				{
					num3 = var2 - var9;
				}
				for (int m = 0; m != deInterleavedBufer.GetNumChannels(); m++)
				{
					for (int n = num; n != num + num3; n++)
					{
						list2[m][var8] += deInterleavedBufer.GetSample(n, m);
					}
				}
				var9 += num3;
				if (!flag)
				{
					continue;
				}
				num += num3;
				for (int num4 = 0; num4 != deInterleavedBufer.GetNumChannels(); num4++)
				{
					double num5 = 0.0;
					for (int num6 = 0; num6 != var1; num6++)
					{
						num5 += list2[num4][num6];
					}
					list3[num4] = num5 / (double)var3;
					double num7 = 0.0;
					for (int num8 = 0; num8 != var4; num8++)
					{
						int num9 = var8 - num8;
						int num10 = var1;
						num9 = (num9 % num10 + num10) % num10;
						num7 += list2[num4][num9];
					}
					list4[num4] = num7 / (double)var5;
				}
				if (var7 == var6)
				{
					var7 = 0;
					double num11 = 0.0;
					for (int num12 = 0; num12 != list4.Count; num12++)
					{
						num11 += list5[num12] * list4[num12];
					}
					double num13 = -0.691 + 10.0 * Math.Log10(num11);
					if (num13 > var11)
					{
						var10++;
						var13 += num11;
						var14 = -10.691 + 10.0 * Math.Log10(var13 / (double)var10);
					}
					if (num13 > var15 && num13 < var16)
					{
						int index = (int)Math.Floor((num13 - var15) / var17);
						list1[index]++;
					}
					int num14 = (int)Math.Ceiling((var14 - var15) / var17);
					if (num14 < list1.Count)
					{
						double num15 = var15 + ((double)num14 + 0.5) * var17;
						double num16 = Math.Pow(10.0, (num15 + 0.691) / 10.0);
						int num17 = 0;
						double num18 = 0.0;
						for (int num19 = num14; num19 < list1.Count; num19++)
						{
							int num20 = list1[num19];
							num17 += num20;
							num18 += (double)num20 * num16;
							num16 *= var18;
						}
						if (num17 > 0)
						{
							integratedLoudness = (float)(-0.691 + 10.0 * Math.Log10(num18 / (double)num17));
						}
						else
						{
							integratedLoudness = var12;
						}
					}
				}
				else
				{
					var7++;
				}
				var8 = (var8 + 1) % var1;
				for (int num21 = 0; num21 != deInterleavedBufer.GetNumChannels(); num21++)
				{
					list2[num21][var8] = 0.0;
				}
				var9 = 0;
			}
		}

		public float getShortTermLoudness()
		{
			double num = 0.0;
			for (int i = 0; i != list3.Count; i++)
			{
				num += list5[i] * list3[i];
			}
			if (num > 0.0)
			{
				return Math.Max((float)(-0.691 + 10.0 * Math.Log10(num)), var12);
			}
			return var12;
		}

		public float getLoudnessRange()
		{
			float num = getShortTermLoudness();
			if (num > (float)ABS_THRES)
			{
				absgatedList.Add(num);
			}
			float num2 = 0f;
			for (int i = 0; i < absgatedList.Count; i++)
			{
				num2 += Mathf.Pow(10f, absgatedList[i] / 10f);
			}
			if (num2 == 0f)
			{
				return 0f;
			}
			num2 /= (float)absgatedList.Count;
			double num3 = 10.0 * Math.Log10(num2);
			for (int j = 0; j < absgatedList.Count; j++)
			{
				if ((double)absgatedList[j] > num3 + -20.0)
				{
					rel_gate_vec.Add(absgatedList[j]);
				}
			}
			rel_gate_vec.Sort();
			float num4 = rel_gate_vec[(int)Mathf.Round((rel_gate_vec.Count - 1) * PRC_LOW / 100 + 1)];
			float num5 = rel_gate_vec[(int)Mathf.Round((rel_gate_vec.Count - 1) * PRC_HIGH / 100 + 1)];
			return num5 - num4;
		}

		public float getTruePeak()
		{
			return 0f;
		}

		public List<float> getMomentaryLoudness()
		{
			float num = 0f;
			for (int i = 0; i != momentaryLoudness.Count; i++)
			{
				num = ((!(list4[i] > 0.0)) ? var12 : Math.Max((float)(-0.691 + 10.0 * Math.Log10(list4[i])), var12));
				momentaryLoudness[i] = num;
			}
			return momentaryLoudness;
		}

		public float getIntegratedLoudness()
		{
			return integratedLoudness;
		}

		public void reset()
		{
			for (int i = 0; i != list2.Count; i++)
			{
				List<double> list = list2[i];
				for (int j = 0; j != list.Count; j++)
				{
					list[j] = 0.0;
				}
			}
			for (int k = 0; k != momentaryLoudness.Count; k++)
			{
				momentaryLoudness[k] = var12;
				shortTermLoudness[k] = var12;
			}
			var10 = 0;
			var13 = 0.0;
			for (int l = 0; l < list1.Count; l++)
			{
				list1[l] = 0;
			}
			var14 = var11;
			integratedLoudness = var12;
		}
	}
}
