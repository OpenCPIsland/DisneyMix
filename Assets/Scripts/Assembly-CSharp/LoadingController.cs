using System.Collections;
using Mix;
using UnityEngine;

public class LoadingController : BaseController
{
	public static bool HideManagePC = true;

	private void Awake()
	{
		QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true);
#if UNITY_ANDROID || UNITY_IOS
		QualitySettings.antiAliasing = 4;
#else
		QualitySettings.antiAliasing = 8;
#endif
	}

	private void Start()
	{
		StartUp.BeginStartSequence();
		StartCoroutine(LogLoadingPageView());
	}

	public IEnumerator LogLoadingPageView()
	{
		while (!Analytics.IsAnalyticsReady())
		{
			yield return null;
		}
		Analytics.LogLoadingPageView();
	}
}
