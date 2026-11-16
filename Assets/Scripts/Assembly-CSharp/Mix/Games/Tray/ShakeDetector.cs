using System;
using UnityEngine;

namespace Mix.Games.Tray
{
	public class ShakeDetector : MonoBehaviour
	{
		private const float ACCELEROMETER_FREQ = 1f / 30f;

		private const float LOW_PASS_KERNEL_WIDH_SECS = 1f;

		private const float SHAKE_DETECTION_THRESHOLD_SQ = 4f;

		private const float LOW_PASS_FILTER_FACTOR = 1f / 30f;

		private Vector3 mLowPassVal;

		private Vector3 mAcceleration;

		private Vector3 mDeltaAcceleration;

		private Action mOnShakeDetected;

		private float mDetectedCoolDown = 0.25f;

		private float mNextCheckTime;

		private void Start()
		{
			mAcceleration = UnityEngine.InputSystem.Accelerometer.current != null ? UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue() : Vector3.zero;
			mNextCheckTime = -1f;
		}

		private void Update()
		{
			if (Time.time >= mNextCheckTime)
			{
				bool flag = false;
				mAcceleration = UnityEngine.InputSystem.Accelerometer.current != null ? UnityEngine.InputSystem.Accelerometer.current.acceleration.ReadValue() : Vector3.zero;
				mLowPassVal = Vector3.Lerp(mLowPassVal, mAcceleration, 1f / 30f);
				mDeltaAcceleration = mAcceleration - mLowPassVal;
				if (mDeltaAcceleration.sqrMagnitude >= 4f)
				{
					flag = true;
				}
				if (flag)
				{
					mOnShakeDetected();
					mNextCheckTime = Time.time + mDetectedCoolDown;
				}
			}
		}

		public static void StartShakeDetection(GameObject aParent, Action aOnDetected, float aCooldown)
		{
			if (aParent == null)
			{
				Debug.LogError("ShakeDetector requires a parent object");
				return;
			}
			if (aOnDetected == null)
			{
				Debug.LogError("ShakeDetector requires an Action callback");
				return;
			}
			ShakeDetector shakeDetector = aParent.AddComponent<ShakeDetector>();
			shakeDetector.mOnShakeDetected = aOnDetected;
			shakeDetector.mDetectedCoolDown = aCooldown;
		}
	}
}
