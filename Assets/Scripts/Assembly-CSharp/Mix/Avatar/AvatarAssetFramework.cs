using System;
using System.Collections.Generic;
using Avatar.DataTypes;
using Avatar.Interfaces;
using Mix.Assets;
using Mix.Data;
using Mix.Entitlements;
using UnityEngine;

namespace Mix.Avatar
{
	public class AvatarAssetFramework : global::Avatar.Interfaces.IAssetManager
	{
		private AssetManager assetManager;

		private EntitlementsManager entitlementsManager;

		public AvatarAssetFramework(AssetManager aAssetManager, EntitlementsManager aEntitlementsManager)
		{
			assetManager = aAssetManager;
			entitlementsManager = aEntitlementsManager;
		}

		public void UnloadAssetsWhenPossible()
		{
			if (assetManager != null)
			{
				assetManager.UnloadAssetsWhenPossible();
			}
		}

		public void LoadABundle(BundleCallback aCaller, string aUrl, object aUserData = null, string aAssetName = "", bool aMemCache = true, bool aIsBackgroundLoad = false, bool aUseCache = false)
		{
			assetManager.LoadABundle(new AvatarAssetObject(aCaller), aUrl, aUserData, aAssetName, aMemCache, aIsBackgroundLoad, aUseCache);
		}

		public string GetShaString(string input)
		{
			return AssetManager.GetShaString(input);
		}

		public bool IsAvatarDataLoaded()
		{
			return entitlementsManager.AreAvatarsLoaded();
		}

		public void LoadCachedImage(string path, Action<bool, Texture2D> callback)
		{
			string relPath = "/Avatar/cache/" + path;
			if (assetManager == null || !assetManager.DoesExist(relPath))
			{
				callback(false, Texture2D.whiteTexture);
				return;
			}
			Texture2D image = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			assetManager.LoadImage(relPath, ref image, delegate(Texture2D texture)
			{
				if (texture == null)
				{
					callback(false, Texture2D.whiteTexture);
				}
				else
				{
					callback(true, texture);
				}
			});
		}

		public void SaveCachedImage(Texture2D texture, string path, Action<bool> callback)
		{
			if (assetManager == null)
			{
				callback(false);
				return;
			}
			assetManager.SaveImage("/Avatar/cache/" + path, texture, delegate(bool success)
			{
				callback(success);
			});
		}

		public List<AvatarElement> GetAllAvatarDataByCategory(string categoryName)
		{
			List<AvatarElement> list = new List<AvatarElement>();
			List<Avatar_Multiplane> allAvatarDataByCategory = entitlementsManager.GetAllAvatarDataByCategory(categoryName);
			while (allAvatarDataByCategory != null && allAvatarDataByCategory.Count > 0)
			{
				AvatarElement avatarElement = TranslateWebAvatarElement(allAvatarDataByCategory[0]);
				if (avatarElement != null)
				{
					list.Add(avatarElement);
				}
				allAvatarDataByCategory.RemoveAt(0);
			}
			return list;
		}

		public List<AvatarElement> GetMyAvatarDataByCategory(string categoryName)
		{
			List<AvatarElement> list = new List<AvatarElement>();
			List<Avatar_Multiplane> myAvatarDataByCategory = entitlementsManager.GetMyAvatarDataByCategory(categoryName);
			while (myAvatarDataByCategory != null && myAvatarDataByCategory.Count > 0)
			{
				AvatarElement avatarElement = TranslateWebAvatarElement(myAvatarDataByCategory[0]);
				if (avatarElement != null)
				{
					list.Add(avatarElement);
				}
				myAvatarDataByCategory.RemoveAt(0);
			}
			return list;
		}

		public AvatarElement GetAvatarData(string referenceId)
		{
			try
			{
				return TranslateWebAvatarElement(entitlementsManager.GetAvatarByReferenceId(referenceId));
			}
			catch (UnityException exception)
			{
				Log.Exception("UnityException", exception);
			}
			return null;
		}

		public string GetAvatarElementVersionInfo(AvatarElement element)
		{
			if (assetManager != null && assetManager.cpipeManager != null && assetManager.cpipeManager.cpipe != null && element != null)
			{
				Cpipe cpipe = assetManager.cpipeManager.cpipe;
				return GetShaString(cpipe.GetLatestHash(assetManager.GetCpipePrefix(element.Hd)) + cpipe.GetLatestHash(assetManager.GetCpipePrefix(element.Sd)));
			}
			return string.Empty;
		}

		private AvatarElement TranslateWebAvatarElement(Avatar_Multiplane elem)
		{
			if (elem != null)
			{
				return new AvatarElement(elem.GetUid(), elem.GetReferenceId(), elem.GetName(), elem.GetCategory(), elem.GetSd(), elem.GetHd(), elem.GetThumb(), elem.GetWhiteList(), elem.GetDefaultTint());
			}
			return null;
		}
	}
}
