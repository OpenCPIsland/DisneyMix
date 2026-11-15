using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using Mix.Assets;
using Mix.Games.Session;
using UnityEngine;

namespace Mix.Games.Harness
{
	public class MixStandaloneAssetManager : MonoSingleton<MixStandaloneAssetManager>
	{
		private const string cdnAssetsDataPath = "CDNAssets/";

		private const string cdnAssetsPath = "CDNAssets/AssetBundles/android/hd/";

		private static byte[] DecompressGZip(byte[] bytes)
		{
			using (GZipStream gZipStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
			{
				int num = 4096;
				byte[] array = new byte[num];
				using (MemoryStream memoryStream = new MemoryStream())
				{
					int num2 = 0;
					do
					{
						num2 = gZipStream.Read(array, 0, num);
						if (num2 > 0)
						{
							memoryStream.Write(array, 0, num2);
						}
					}
					while (num2 > 0);
					return memoryStream.ToArray();
				}
			}
		}

		private object loadLocalData(string aPath, string aFileName, Func<string, object> aMethod)
		{
			byte[] bytes = DecompressGZip(File.ReadAllBytes("CDNAssets/" + aPath));
			string text = "CDNAssets/" + aPath.Replace(".gz", string.Empty);
			text += ".json";
			File.WriteAllBytes(text, bytes);
			string arg = File.ReadAllText(text);
			return aMethod(arg);
		}

		public void CancelBundles()
		{
		}

		public void CancelBundles(string aUrl)
		{
		}

		public void DestroyBundleInstance(string aPath, UnityEngine.Object aObject)
		{
		}

		public UnityEngine.Object GetBundleInstance(string aPath)
		{
			string path = "CDNAssets/AssetBundles/android/hd/" + aPath;
			AssetBundle assetBundle = AssetBundle.LoadFromFile(path);
			GameObject model = (GameObject)assetBundle.LoadAsset(assetBundle.GetAllAssetNames()[0]);
			AssetUtils.ConfigureAnisotropicFiltering(model);
			return model;
		}

		public void LoadAsset(IGameAsset aSessionAsset, string aPath, object aParam)
		{
			Hashtable hashtable = new Hashtable();
			hashtable["SessionAsset"] = aSessionAsset;
			if (aParam != null)
			{
				hashtable["Parameter"] = aParam;
			}
			hashtable["BundleUrl"] = aPath;
			aSessionAsset.OnGameSessionAssetLoaded(hashtable);
		}

		public void LoadData(GameSession aSession, string aPath, string aFileName, Func<string, object> aMethod)
		{
			object aObject = loadLocalData(aPath, aFileName, aMethod);
			aSession.OnDataLoaded(aObject, aMethod);
		}

		public bool WillBundleLoadFromWeb(string aPath)
		{
			return !((double)UnityEngine.Random.Range(0, 1) >= 0.5);
		}
	}
}
