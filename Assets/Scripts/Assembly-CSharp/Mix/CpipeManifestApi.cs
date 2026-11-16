using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Mix.Assets;
using UnityEngine;

namespace Mix
{
	public class CpipeManifestApi : Singleton<CpipeManifestApi>, ITextAssetObject
	{
		public const string CPIPE_CACHE_KEY = "cpipeManfiest";

		private string key;

		void ITextAssetObject.TextAssetObjectComplete(string aText, object aUserData)
		{
			if (aUserData != null)
			{
				Hashtable hashtable = (Hashtable)aUserData;
				ITextAssetObject textAssetObject = (ITextAssetObject)hashtable["handler"];
				textAssetObject.TextAssetObjectComplete(aText, hashtable);
			}
		}

		public void GetCpipeManifest(string aEnvironment, string aClientVersionNumber, string aManifestVersionNumber, ITextAssetObject aCallingObject, CachePolicy aCachePolicy, bool getLatest = false, object aUserData = null)
		{
			if (aCallingObject != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(string.Format("restrict={0}", "true"));
				stringBuilder.Append("&");
				stringBuilder.Append(string.Format("cv={0}", UnityEngine.Networking.UnityWebRequest.EscapeURL(aClientVersionNumber)));
				if (!string.IsNullOrEmpty(aManifestVersionNumber))
				{
					stringBuilder.Append("&");
					stringBuilder.Append(string.Format("v={0}", UnityEngine.Networking.UnityWebRequest.EscapeURL(aManifestVersionNumber)));
				}
				string aUrl = ExternalizedConstants.CpipeUrl + "?" + stringBuilder.ToString();
				File.WriteAllText(Application.PersistentDataPath + "/ver.txt", aClientVersionNumber);
				Hashtable hashtable = new Hashtable();
				hashtable["handler"] = aCallingObject;
				hashtable["manifestCall"] = true;
				if (aUserData != null)
				{
					hashtable["ud"] = aUserData;
				}
				hashtable["responseHeaders"] = new Dictionary<string, string>();
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				if (ExternalizedConstants.CellophaneClientIdAssets != null)
				{
					dictionary.Add("Authorization", ExternalizedConstants.CellophaneClientIdAssets);
				}
				LoadParams aLoadParams = new LoadParams(AssetManager.GetShaString("cpipeManfiest"), aUrl, aCachePolicy, System.Threading.ThreadPriority.Normal, dictionary);
				MonoSingleton<AssetManager>.Instance.LoadText(this, aLoadParams, hashtable);
			}
		}

		private string generateToken(string aEnvironment, string aVersionNumber)
		{
			byte[] aKey = getKey();
			string aValue;
			if (string.IsNullOrEmpty(aEnvironment) || string.Empty.Equals(aEnvironment.Trim()) || string.IsNullOrEmpty(aVersionNumber) || string.Empty.Equals(aVersionNumber.Trim()))
			{
				aValue = "xyz";
			}
			else
			{
				aVersionNumber = aVersionNumber.Trim();
				aEnvironment = aEnvironment.Trim();
				aValue = aVersionNumber + "-" + aEnvironment.ToUpper();
			}
			return EncryptionUtil.EncryptSha1(aValue, aKey, true);
		}

		private byte[] getKey()
		{
			if (string.IsNullOrEmpty(key))
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("AbM");
				stringBuilder.Append("HrLp");
				stringBuilder.Append("PVP");
				stringBuilder.Append("Var8");
				stringBuilder.Append("M7s");
				stringBuilder.Append("Gu7");
				stringBuilder.Append("9g==");
				key = stringBuilder.ToString();
			}
			return Convert.FromBase64String(key);
		}
	}
}
