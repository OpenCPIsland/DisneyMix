using System.Collections.Generic;
using UnityEngine;

namespace Mix.Games.Tray.Fireworks
{
	public class Gesture
	{
		private const int mINDEX_FOR_TAP_FIREWORK = 10;

		private const int mINDEX_FOR_SWIPE_FIREWORK = 11;

		private const float mMinSquaredDistanceBetweenPoints = 3f;

		private const float mMinTimeBetweenPoints = 0.4f;

		private List<GesturePoint> mGesturePoints = new List<GesturePoint>();

		private GesturePoint mLastGesturePoint = new GesturePoint(0f, Vector3.zero);

		public Vector3 AddNewPoint(float timestamp, Camera viewCamera)
		{
			Vector3 mousePosition = UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero;
			Vector3 point = viewCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 60f));
			return AddNewPoint(timestamp, point);
		}

		public Vector3 AddNewPoint(GesturePoint point)
		{
			mGesturePoints.Add(point);
			return point.Point;
		}

		public Vector3 AddNewPoint(float timestamp, Vector3 point)
		{
			if (mGesturePoints.Count == 0)
			{
				mGesturePoints.Add(new GesturePoint(timestamp, point));
			}
			else
			{
				float sqrMagnitude = (point - mGesturePoints[mGesturePoints.Count - 1].Point).sqrMagnitude;
				if (sqrMagnitude > 3f)
				{
					mGesturePoints.Add(new GesturePoint(timestamp, point));
				}
				else if (timestamp - mGesturePoints[mGesturePoints.Count - 1].Timestamp > 0.4f)
				{
					mGesturePoints.Add(new GesturePoint(timestamp, point));
				}
			}
			mLastGesturePoint.Timestamp = timestamp;
			mLastGesturePoint.Point = point;
			return point;
		}

		public void ClearPoints()
		{
			mGesturePoints.Clear();
		}

		public int EndGesture()
		{
			if (!Mathf.Approximately(mLastGesturePoint.Timestamp, mGesturePoints[mGesturePoints.Count - 1].Timestamp))
			{
				mGesturePoints.Add(mLastGesturePoint);
			}
			int result = 10;
			if (mGesturePoints.Count > 1)
			{
				result = 11;
				Gesture gesture = new Gesture();
				CopyGesture(gesture);
				if (gesture.GetPointCount() > 1)
				{
					Toolbox.Instance.mGestureTracker.AddNewGesture(gesture);
				}
			}
			return result;
		}

		public void CopyGesture(Gesture copyTarget)
		{
			copyTarget.ClearPoints();
			foreach (GesturePoint mGesturePoint in mGesturePoints)
			{
				copyTarget.AddNewPoint(mGesturePoint.Timestamp, mGesturePoint.Point);
			}
		}

		public int GetPointCount()
		{
			return mGesturePoints.Count;
		}

		public GesturePoint GetGesturePointAtIndex(int index)
		{
			return mGesturePoints[index];
		}

		public Vector3 GetPointAtTime(float time)
		{
			if (time <= mGesturePoints[0].Timestamp)
			{
				return mGesturePoints[0].Point;
			}
			if (time > mGesturePoints[mGesturePoints.Count - 1].Timestamp)
			{
				return mGesturePoints[mGesturePoints.Count - 1].Point;
			}
			GesturePoint gesturePoint = mGesturePoints[0];
			GesturePoint gesturePoint2 = mGesturePoints[0];
			GesturePoint gesturePoint3 = mGesturePoints[1];
			GesturePoint gesturePoint4 = mGesturePoints[1];
			for (int i = 0; i < mGesturePoints.Count - 1 && mGesturePoints[i].Timestamp <= time; i++)
			{
				gesturePoint = mGesturePoints[Mathf.Max(0, i - 1)];
				gesturePoint2 = mGesturePoints[i];
				gesturePoint3 = mGesturePoints[i + 1];
				gesturePoint4 = mGesturePoints[Mathf.Min(mGesturePoints.Count - 1, i + 2)];
			}
			float t = (time - gesturePoint2.Timestamp) / (gesturePoint3.Timestamp - gesturePoint2.Timestamp);
			return CatmullRom.PointOnCurve(gesturePoint.Point, gesturePoint2.Point, gesturePoint3.Point, gesturePoint4.Point, t);
		}

		public float GetStartTime()
		{
			return mGesturePoints[0].Timestamp;
		}

		public float GetEndTime()
		{
			return mGesturePoints[mGesturePoints.Count - 1].Timestamp;
		}

		private void DebugDrawGesture()
		{
			if (mGesturePoints.Count > 1)
			{
				Debug.DrawLine(mGesturePoints[0].Point, mGesturePoints[0].Point + Vector3.back, Color.red, 3f);
				for (int i = 1; i < mGesturePoints.Count; i++)
				{
					Debug.DrawLine(mGesturePoints[i].Point, mGesturePoints[i].Point + Vector3.back, Color.red, 3f);
				}
				int num = 5000;
				float timestamp = mGesturePoints[0].Timestamp;
				float timestamp2 = mGesturePoints[mGesturePoints.Count - 1].Timestamp;
				float num2 = (timestamp2 - timestamp) / (float)num;
				Vector3 start = mGesturePoints[0].Point;
				for (int j = 1; j < num; j++)
				{
					Vector3 pointAtTime = GetPointAtTime(timestamp + num2 * (float)j);
					Debug.DrawLine(start, pointAtTime, Color.yellow, 3f);
					start = pointAtTime;
				}
			}
		}

		public void TrimGesture(int pointCount)
		{
			if (pointCount <= mGesturePoints.Count && pointCount > 0)
			{
				mGesturePoints.RemoveRange(pointCount - 1, mGesturePoints.Count - pointCount);
			}
		}
	}
}
