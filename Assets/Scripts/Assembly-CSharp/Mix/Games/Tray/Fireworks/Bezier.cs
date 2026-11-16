using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class Bezier
	{
		private Vector3 mP0;

		private Vector3 mP1;

		private Vector3 mP2;

		private Vector3 mP3;

		private Vector3 mB0 = Vector3.zero;

		private Vector3 mB1 = Vector3.zero;

		private Vector3 mB2 = Vector3.zero;

		private Vector3 mB3 = Vector3.zero;

		private float Ax;

		private float Ay;

		private float Az;

		private float Bx;

		private float By;

		private float Bz;

		private float Cx;

		private float Cy;

		private float Cz;

		public Vector3 StartPoint
		{
			get
			{
				return mB0;
			}
		}

		public Vector3 StartControlPoint
		{
			get
			{
				return mB1;
			}
		}

		public Vector3 EndControlPoint
		{
			get
			{
				return mB2;
			}
		}

		public Vector3 EndPoint
		{
			get
			{
				return mB3;
			}
		}

		public Bezier(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
		{
			mP0 = v0;
			mP1 = v1;
			mP2 = v2;
			mP3 = v3;
			SetConstant();
		}

		public Vector3 GetPointAtTime(float t)
		{
			CheckConstant();
			float num = t * t;
			float num2 = t * t * t;
			float x = Ax * num2 + Bx * num + Cx * t + mP0.x;
			float y = Ay * num2 + By * num + Cy * t + mP0.y;
			float z = Az * num2 + Bz * num + Cz * t + mP0.z;
			return new Vector3(x, y, z);
		}

		public Vector3 GetPointAtTimeO(float t)
		{
			float num = t * t;
			float num2 = t * t * t;
			float x = Ax * num2 + Bx * num + Cx * t + mP0.x;
			float y = Ay * num2 + By * num + Cy * t + mP0.y;
			float z = Az * num2 + Bz * num + Cz * t + mP0.z;
			return new Vector3(x, y, z);
		}

		public Vector3[] GetEquidistantPoints(int numPoints, float degreeOfAccuracy, out float[] times)
		{
			CheckConstant();
			Vector3[] array = new Vector3[numPoints];
			times = new float[numPoints];
			array[0] = GetPointAtTimeO(0f);
			times[0] = 0f;
			float num = 0f;
			for (int i = 0; i < (int)degreeOfAccuracy; i++)
			{
				num += Vector3.Distance(GetPointAtTimeO((float)i / degreeOfAccuracy), GetPointAtTimeO((float)(i + 1) / degreeOfAccuracy));
			}
			float num2 = num / (float)(numPoints - 1);
			num = 0f;
			int num3 = 1;
			for (float num4 = 0f; num4 < (float)(int)degreeOfAccuracy; num4 += 1f)
			{
				Vector3 pointAtTimeO = GetPointAtTimeO(num4 / degreeOfAccuracy);
				Vector3 pointAtTimeO2 = GetPointAtTimeO((num4 + 1f) / degreeOfAccuracy);
				num += Vector3.Distance(pointAtTimeO, pointAtTimeO2);
				if (num > num2 * (float)num3)
				{
					array[num3] = Vector3.Lerp(pointAtTimeO, pointAtTimeO2, 1f);
					times[num3] = (num4 + 1f) / degreeOfAccuracy;
					num3++;
					if (num3 == numPoints)
					{
						num4 = degreeOfAccuracy;
					}
				}
			}
			array[numPoints - 1] = GetPointAtTimeO(1f);
			times[numPoints - 1] = 1f;
			return array;
		}

		public Vector3 GetTangetAtTime(float t)
		{
			CheckConstant();
			float num = t * t;
			float x = 3f * Ax * num + 2f * Bx * t + Cx;
			float y = 3f * Ay * num + 2f * By * t + Cy;
			float z = 3f * Az * num + 2f * Bz * t + Cz;
			return new Vector3(x, y, z).normalized;
		}

		public Vector3 GetTangetAtTimeO(float t)
		{
			float num = t * t;
			float x = 3f * Ax * num + 2f * Bx * t + Cx;
			float y = 3f * Ay * num + 2f * By * t + Cy;
			float z = 3f * Az * num + 2f * Bz * t + Cz;
			return new Vector3(x, y, z).normalized;
		}

		public Vector3 GetNormalAtTime(float t)
		{
			return Vector3.Cross(GetTangetAtTime(t), new Vector3(0f, 0f, 1f)).normalized;
		}

		public Vector3 GetNormalAtTimeO(float t)
		{
			return Vector3.Cross(GetTangetAtTimeO(t), new Vector3(0f, 0f, 1f)).normalized;
		}

		private void SetConstant()
		{
			Cx = 3f * (mP0.x + mP1.x - mP0.x);
			Bx = 3f * (mP3.x + mP2.x - (mP0.x + mP1.x)) - Cx;
			Ax = mP3.x - mP0.x - Cx - Bx;
			Cy = 3f * (mP0.y + mP1.y - mP0.y);
			By = 3f * (mP3.y + mP2.y - (mP0.y + mP1.y)) - Cy;
			Ay = mP3.y - mP0.y - Cy - By;
			Cz = 3f * (mP0.z + mP1.z - mP0.z);
			Bz = 3f * (mP3.z + mP2.z - (mP0.z + mP1.z)) - Cz;
			Az = mP3.z - mP0.z - Cz - Bz;
		}

		private void CheckConstant()
		{
			if (mP0 != mB0 || mP1 != mB1 || mP2 != mB2 || mP3 != mB3)
			{
				SetConstant();
				mB0 = mP0;
				mB1 = mP1;
				mB2 = mP2;
				mB3 = mP3;
			}
		}

		public void DrawBezier(int numVertexes, Vector3 offset)
		{
			GameObject gameObject = new GameObject("BezierCurver");
			LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.startWidth = 0.1f;
			lineRenderer.endWidth = 0.1f;
			lineRenderer.positionCount = numVertexes;
			for (int i = 0; i < numVertexes; i++)
			{
				lineRenderer.SetPosition(i, GetPointAtTime((float)i / (float)(numVertexes - 1)) + offset);
				DrawNormal((float)i / (float)(numVertexes - 1), offset);
			}
		}

		private void DrawTangent(float t)
		{
			GameObject gameObject = new GameObject("BezierCurverTangent");
			LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.startWidth = 0.05f;
			lineRenderer.endWidth = 0.05f;
			lineRenderer.positionCount = 3;
			Vector3 pointAtTime = GetPointAtTime(t);
			lineRenderer.SetPosition(0, pointAtTime - GetTangetAtTime(t) * 3f);
			lineRenderer.SetPosition(1, pointAtTime);
			lineRenderer.SetPosition(2, pointAtTime + GetTangetAtTime(t) * 3f);
		}

		private void DrawNormal(float t, Vector3 offset)
		{
			GameObject gameObject = new GameObject("BezierCurverTangent");
			LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.startWidth = 0.05f;
			lineRenderer.endWidth = 0.05f;
			lineRenderer.positionCount = 2;
			Vector3 vector = GetPointAtTime(t) + offset;
			lineRenderer.SetPosition(0, vector);
			lineRenderer.SetPosition(1, vector - GetNormalAtTime(t));
		}
	}
}
