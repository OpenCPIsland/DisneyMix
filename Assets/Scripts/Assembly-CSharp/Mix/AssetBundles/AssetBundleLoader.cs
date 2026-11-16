using System;
using System.Collections;
using Mix.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Mix.AssetBundles
{
	public class AssetBundleLoader
	{
		private AssetBundleData bundleData;

		private MonoBehaviour monoBehaviour;

		private Action<bool, AssetBundleData> callback;

		public AssetBundleLoader(MonoBehaviour aMonoBehaviour, AssetBundleData aAssetBundleData, Action<bool, AssetBundleData> aCallback)
		{
			monoBehaviour = aMonoBehaviour;
			bundleData = aAssetBundleData;
			callback = aCallback;
			monoBehaviour.StartCoroutine(BundleLoader());
		}

		private IEnumerator BundleLoader()
		{
			while (!Caching.ready)
			{
				yield return null;
			}
			int version = bundleData.Version;
			string loadPath = bundleData.URL;
			if (bundleData.ForceStreamingAssetsLoad)
			{
				loadPath = bundleData.StreamingAssetsPath;
			}
			Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
			using (UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle(loadPath, Hash128.Compute(version.ToString())))
			{
				www.SendWebRequest();
				while (!www.isDone)
				{
					if (bundleData.DownloadPercentCallback != null)
					{
						bundleData.DownloadPercentCallback(www.downloadProgress);
					}
					yield return null;
				}
				if (!string.IsNullOrEmpty(www.error))
				{
					Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
					callback(false, bundleData);
					yield break;
				}
				if (DownloadHandlerAssetBundle.GetContent(www) == null)
				{
					Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
					callback(false, bundleData);
					yield break;
				}
				bundleData.Bundle = DownloadHandlerAssetBundle.GetContent(www);
			}
			AssetBundleRequest request = bundleData.Bundle.LoadAssetAsync(bundleData.Bundle.GetAllAssetNames()[0]);
			yield return request;
			bundleData.LoadedAsset = request.asset;
			bundleData.Bundle.Unload(false);
			Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
			if (bundleData.LoadedAsset == null)
			{
				callback(false, bundleData);
			}
			else
			{
				callback(true, bundleData);
			}
		}
	}
}
