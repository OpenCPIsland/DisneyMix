using System;
using System.Collections;
using Disney.MobileNetwork;
using Disney.Native;
using LitJson;
using Mix.Assets;
using UnityEngine;

namespace Mix.Localization
{
	public class Localizer : Singleton<Localizer>, IAccessibilityLocalization, ITextAssetObject
	{
		public const string ENGLISH_LOCALE = "en_US";

		public const string SPANISH_LOCALE = "es_LA";

		public const string GERMAN_LOCALE = "de_DE";

		public const string FRENCH_LOCALE = "fr_FR";

		public const string PORTUGUESE_LOCALE = "pt_BR";

		private static string DEVON_CHECK = "2.3";

		private static string filePath = "localization/";

		private JsonData data;

		private ILocalization Caller;

		public static string NO_TOKEN = "No token!";

		public static string DEVON_VERSION
		{
			get
			{
				if (EnvironmentManager.BundleVersion == null)
				{
					return DEVON_CHECK;
				}
				string text = EnvironmentManager.BundleVersion.Major + "." + EnvironmentManager.BundleVersion.Minor;
				if (text != DEVON_CHECK)
				{
				}
				return text;
			}
		}

		void ITextAssetObject.TextAssetObjectComplete(string aText, object aUserData)
		{
			if (!string.IsNullOrEmpty(aText))
			{
				try
				{
					data = JsonMapper.ToObject(aText);
				}
				catch (Exception exception)
				{
					Log.Exception(exception);
					string shaString = AssetManager.GetShaString(GetLocFileUrl());
					MonoSingleton<AssetManager>.Instance.AssetDatabaseApi.DeleteRecordBySha(shaString);
					MonoSingleton<AssetManager>.Instance.FlagRefreshContent();
				}
			}
			if (Caller != null)
			{
				Caller.OnLocalizationReady();
				Caller = null;
			}
		}

		string IAccessibilityLocalization.GetString(string aToken)
		{
			return getString(aToken);
		}

		private IEnumerator WaitForPreloadData(ILocalization aCaller, MonoBehaviour aMonoEngine)
		{
			while (!PreloadData.LocDataComplete)
			{
				yield return null;
			}
			data = PreloadData.LocData;
			aCaller.OnLocalizationReady();
		}

		public void SetLocData(ILocalization aCaller, MonoBehaviour aMonoEngine, bool aFirstRun = false)
		{
			if (aFirstRun)
			{
				aMonoEngine.StartCoroutine(WaitForPreloadData(aCaller, aMonoEngine));
				return;
			}
			Caller = aCaller;
			ReloadLocData(aMonoEngine, CachePolicy.CacheThenBundleThenDownload);
		}

		public static string GetLocFileUrl()
		{
			return filePath + GetLocale() + ".json";
		}

		public void ReloadLocData(MonoBehaviour aMonoEngine, CachePolicy aCachePolicy)
		{
			LoadParams aLoadParams = new LoadParams(AssetManager.GetShaString(GetLocFileUrl()), GetLocFileUrl(), aCachePolicy);
			MonoSingleton<AssetManager>.Instance.LoadText(this, aLoadParams);
		}

		public static string GetLocale()
		{
			return "en_US";
		}

		public string getString(string aToken, bool aRequired = true)
		{
			if (data != null && aToken != null && ((IDictionary)data).Contains((object)aToken))
			{
				return (string)data[aToken];
			}
			return (!aRequired && aToken != null) ? aToken : NO_TOKEN;
		}
	}
}
