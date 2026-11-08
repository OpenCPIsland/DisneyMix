using System.Collections.Generic;
using Mix.Assets;
using Mix.Connectivity;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.AssetBundles
{
	public class AssetBundleController
	{
		private MonoBehaviour monoBehaviour;

		public ReferenceCounter refCounter;

		private AssetCache memCache;

		private int MaxConcurrentLoads = 5;

		private int MaxReturnsPerFrame = 5;

		private int MaxRetries = 2;

		private List<AssetBundleData> BundleLoadQueue = new List<AssetBundleData>();

		private List<AssetBundleData> ActiveBundleLoads = new List<AssetBundleData>();

		private List<AssetBundleData> ReturnQueue = new List<AssetBundleData>();

		private List<string> LoadingUrls = new List<string>();

		public AssetBundleController(MonoBehaviour aMonoBehaviour, ReferenceCounter aReferenceCounter, AssetCache aAssetCache)
		{
			monoBehaviour = aMonoBehaviour;
			refCounter = aReferenceCounter;
			memCache = aAssetCache;
		}

		public static bool IsThumb(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}
			int num = path.LastIndexOf("?");
			if (num > 0)
			{
				path = path.Substring(0, num);
			}
			int num2 = path.LastIndexOf(".");
			if (num2 > -1)
			{
				path = path.Substring(0, path.LastIndexOf("."));
			}
			if (path.EndsWith("_thumb"))
			{
				return true;
			}
			return false;
		}

		public void CancelBundleLoad(IBundleObject aIBundleObject)
		{
			for (int num = BundleLoadQueue.Count - 1; num >= 0; num--)
			{
				if (BundleLoadQueue[num].ContentCallback.Equals(aIBundleObject))
				{
					BundleLoadQueue.RemoveAt(num);
				}
			}
		}

		public void CancelBundleLoad(string aUrl)
		{
			for (int num = BundleLoadQueue.Count - 1; num >= 0; num--)
			{
				if (BundleLoadQueue[num].URL.Equals(aUrl))
				{
					BundleLoadQueue.RemoveAt(num);
				}
			}
		}

		public void QueueAssetBundle(AssetBundleData aAssetBundleData)
		{
			if (!QueueAssetIfInBundles(aAssetBundleData))
			{
				BundleLoadQueue.Add(aAssetBundleData);
			}
		}

		private bool QueueAssetIfInBundles(AssetBundleData aAssetBundleData)
		{
			bool flag = Caching.IsVersionCached(aAssetBundleData.URL, aAssetBundleData.Version);
			if (aAssetBundleData.HashNew && !flag)
			{
				return false;
			}
			if (refCounter.BundleInstances.ContainsKey(aAssetBundleData.CacheKey))
			{
				QueueReturn(aAssetBundleData, true);
				return true;
			}
			return false;
		}

		public void UpdateQueue()
		{
			if (BundleLoadQueue.Count > 0 && ActiveBundleLoads.Count < MaxConcurrentLoads)
			{
				AssetBundleData assetBundleData = BundleLoadQueue[0];
				if (!LoadingUrls.Contains(assetBundleData.URL))
				{
					BundleLoadQueue.RemoveAt(0);
					LoadingUrls.Add(assetBundleData.URL);
					ActiveBundleLoads.Add(assetBundleData);
					LoadAssetBundle(assetBundleData);
				}
			}
			for (int i = 0; i < MaxReturnsPerFrame; i++)
			{
				if (ReturnQueue.Count > 0)
				{
					AssetBundleData assetBundleData2 = ReturnQueue[0];
					ReturnQueue.RemoveAt(0);
					assetBundleData2.Callback();
					refCounter.DecrementReferenceCount(assetBundleData2.CacheKey);
				}
			}
		}

private void SetStreamingAssetsLoad(AssetBundleData aAssetBundleData)
{
    bool flag = Caching.IsVersionCached(aAssetBundleData.URL, aAssetBundleData.Version);
    bool isConnected = MonoSingleton<ConnectionManager>.Instance.IsConnected;

    bool urlIsRemote = aAssetBundleData.URL != null &&
        (aAssetBundleData.URL.StartsWith("http://") || aAssetBundleData.URL.StartsWith("https://"));

    if (aAssetBundleData.InStreamingAssets && !urlIsRemote)
    {
        aAssetBundleData.ForceStreamingAssetsLoad = true;   // no web URL to fall back on
    }
    else if (!isConnected && aAssetBundleData.InStreamingAssets && !flag)
    {
        aAssetBundleData.ForceStreamingAssetsLoad = true;
    }
    else if (isConnected && !aAssetBundleData.HashNew && aAssetBundleData.InStreamingAssets)
    {
        aAssetBundleData.ForceStreamingAssetsLoad = true;
    }
}

		private void LoadAssetBundle(AssetBundleData aAssetBundleData)
		{
			if (!QueueAssetIfInBundles(aAssetBundleData))
			{
				SetStreamingAssetsLoad(aAssetBundleData);
				new AssetBundleLoader(monoBehaviour, aAssetBundleData, delegate(bool loadSuccess, AssetBundleData assetBundleData)
				{
					OnLoadComplete(loadSuccess, assetBundleData);
				});
			}
		}

		private void OnLoadComplete(bool loadSuccess, AssetBundleData aAssetBundleData)
		{
			if (loadSuccess || aAssetBundleData.Retries >= MaxRetries)
			{
				SetTexture(aAssetBundleData);
				AddToMemCache(aAssetBundleData);
				QueueReturn(aAssetBundleData, refCounter.BundleInstances.ContainsKey(aAssetBundleData.CacheKey));
			}
			else
			{
				aAssetBundleData.Retries++;
				ActiveBundleLoads.Remove(aAssetBundleData);
				LoadingUrls.Remove(aAssetBundleData.URL);
				BundleLoadQueue.Add(aAssetBundleData);
			}
		}

		private void QueueReturn(AssetBundleData aAssetBundleData, bool aInBundleInstances)
		{
			if (aInBundleInstances)
			{
				aAssetBundleData.LoadedAsset = refCounter.BundleInstances[aAssetBundleData.CacheKey].unityObject;
				if (aAssetBundleData.UnpackTexture)
				{
					aAssetBundleData.UnpackedTexture = refCounter.BundleInstances[aAssetBundleData.CacheKey].unityObject as Texture2D;
				}
				refCounter.IncrementReferenceCount(aAssetBundleData.CacheKey);
			}
			ActiveBundleLoads.Remove(aAssetBundleData);
			LoadingUrls.Remove(aAssetBundleData.URL);
			ReturnQueue.Add(aAssetBundleData);
		}

		private void SetTexture(AssetBundleData aAssetBundleData)
		{
			if (!aAssetBundleData.UnpackTexture)
			{
				refCounter.AddBundleInstance(aAssetBundleData.CacheKey, aAssetBundleData.LoadedAsset);
				return;
			}
			Texture2D texture2D = null;
			if (aAssetBundleData.LoadedAsset == null)
			{
				return;
			}
			GameObject gameObject = aAssetBundleData.LoadedAsset as GameObject;
			if (gameObject != null)
			{
				Image component = gameObject.GetComponent<Image>();
				if (component != null && component.sprite != null)
				{
					texture2D = (aAssetBundleData.UnpackedTexture = component.sprite.texture);
					refCounter.AddBundleInstance(aAssetBundleData.CacheKey, texture2D);
				}
				else
				{
					refCounter.AddBundleInstance(aAssetBundleData.CacheKey, gameObject);
				}
			}
		}

		private void AddToMemCache(AssetBundleData aAssetBundleData)
		{
			if (memCache.Get(aAssetBundleData.CacheKey) != null || !aAssetBundleData.UseMemCache)
			{
				return;
			}
			object asset = aAssetBundleData.GetAsset();
			if (asset != null)
			{
				long aSize = 0L;
				if (asset is Texture2D)
				{
					aSize = ((Texture2D)asset).width * ((Texture2D)asset).height * 8;
				}
				memCache.Add(aAssetBundleData.CacheKey, asset, aSize, true);
				refCounter.IncrementReferenceCount(aAssetBundleData.CacheKey);
			}
		}

		private void SaveUnpackImage(Texture2D tex, string aUrl)
		{
			if (tex != null)
			{
				MonoSingleton<AssetManager>.Instance.SaveImage("Unpack/" + AssetManager.GetShaString(aUrl), tex, delegate
				{
				});
			}
		}
	}
}
