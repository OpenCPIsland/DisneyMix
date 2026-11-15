using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	[DisallowMultipleComponent]
	public class ResponsiveCanvasScaler : MonoBehaviour
	{
		private CanvasScaler[] canvasScalers;

		private float[] portraitMatchValues;

		private bool lastLandscape;

		public static void Ensure(GameObject target)
		{
			if (target != null && target.GetComponent<ResponsiveCanvasScaler>() == null)
			{
				target.AddComponent<ResponsiveCanvasScaler>();
			}
		}

		private void Awake()
		{
			canvasScalers = GetComponentsInChildren<CanvasScaler>(true);
			portraitMatchValues = new float[canvasScalers.Length];
			for (int i = 0; i < canvasScalers.Length; i++)
			{
				portraitMatchValues[i] = canvasScalers[i].matchWidthOrHeight;
			}
		}

		private void OnEnable()
		{
			UpdateScale();
		}

		private void Update()
		{
			if (IsLandscapeLayout() != lastLandscape)
			{
				UpdateScale();
			}
		}

		private void UpdateScale()
		{
			bool isLandscape = IsLandscapeLayout();
			for (int i = 0; i < canvasScalers.Length; i++)
			{
				if (canvasScalers[i] != null)
				{
					canvasScalers[i].matchWidthOrHeight = isLandscape ? 1f : portraitMatchValues[i];
				}
			}

			lastLandscape = isLandscape;
		}

		public static bool IsLandscapeLayout()
		{
#if UNITY_EDITOR
			return Screen.width > Screen.height;
#elif UNITY_ANDROID || UNITY_IOS
			return false;
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || UNITY_WEBGL
			return true;
#else
			return Screen.width > Screen.height;
#endif
		}
	}
}
