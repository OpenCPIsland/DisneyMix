using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Disney.Native;
using UnityEngine;
using UnityEngine.Networking;

namespace Mix.Assets
{
	public class ZipAssetObject : AssetObject
	{
		private int MaxUnZipRetries = 2;

		protected IZipAssetObject caller;

		public ZipAssetObject(IAssetManager aAssetManager, IZipAssetObject aCaller, LoadParams aLoadParams, object aUserData)
			: base(aAssetManager, aLoadParams, aUserData)
		{
			caller = aCaller;
		}

		public void LoadFromBundle()
		{
			BaseLoadFromBundle("path");
		}

		public override void Destroy()
		{
			CallCaller(null);
			caller = null;
			base.Destroy();
		}

		public override string GetFileExtension()
		{
			return ".gz";
		}

		public void CallCaller(string text)
		{
			if (caller != null && !base.flow.IsCallerCalled)
			{
				base.flow.IsCallerCalled = true;
				caller.OnZipAssetObject(text, userData);
			}
		}

		public override void RecordReturnedFromDBAndPathExistsLocal(string aPath)
		{
			CallCaller(aPath);
		}

		public override void LoadFromWeb()
		{
			LoadBinaryFromWeb();
		}

		protected override void WebBinaryResponseHandler(bool aIsSuccess, byte[] aBody, Dictionary<string, string> aHeaders, bool aIsReturnPathToAsset = false)
		{
			base.WebBinaryResponseHandler(aIsSuccess, aBody, aHeaders, true);
		}

		public byte[] DecompressGZip(byte[] bytes)
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

		public void UnZip(string aPath, string entryName, int retries = 0)
		{
			try
			{
				if (MonoSingleton<AssetManager>.Instance.IsPathAndroidStreamingAssets(aPath))
				{
					using (UnityWebRequest wWW = UnityWebRequest.Get(aPath))
					{
						wWW.SendWebRequest();
						while (!wWW.isDone)
						{
						}
						if (string.IsNullOrEmpty(wWW.error))
						{
							byte[] data = wWW.downloadHandler?.data;
							if (data != null && data.Length > 0)
							{
								File.WriteAllBytes(AssetManager.GetDiskCacheFilePath() + entryName, data);
							}
						}
						return;
					}
				}
				if (Application.Platform == 11)
				{
					MonoSingleton<NativeUtilitiesManager>.Instance.Native.UnGZipFile(aPath, AssetManager.GetDiskCacheFilePath() + entryName);
					return;
				}
				byte[] bytes = DecompressGZip(File.ReadAllBytes(aPath));
				File.WriteAllBytes(AssetManager.GetDiskCacheFilePath() + entryName, bytes);
			}
			catch (Exception exception)
			{
				if (retries < MaxUnZipRetries)
				{
					UnZip(aPath, entryName, ++retries);
					return;
				}
				Log.Exception("UnZip Exception Thrown after " + retries + " retries from path " + aPath + ", name " + entryName, exception);
			}
		}
	}
}
