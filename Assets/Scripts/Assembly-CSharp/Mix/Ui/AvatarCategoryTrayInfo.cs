using System.Collections.Generic;
using Mix.Assets;
using Mix.Data;
using Mix.Entitlements;
using UnityEngine;

namespace Mix.Ui
{
	public class AvatarCategoryTrayInfo
	{
		public string categoryName;

		public string locCategoryName;

		public string disableId;

		public AvatarCategoryTrayInfo(string aCategoryName, string aLocText, string aDisableId)
		{
			categoryName = aCategoryName;
			locCategoryName = aLocText;
			disableId = aDisableId;
		}

		public List<BaseContentData> GetThumbContent()
		{
			List<BaseContentData> list = new List<BaseContentData>();
			bool includeAllLocalCharacterAvatars = categoryName == "Skin" || categoryName == "Costume";
			List<Avatar_Multiplane> avatarDataByCategory = includeAllLocalCharacterAvatars
				? Singleton<EntitlementsManager>.Instance.GetAllAvatarDataByCategory(categoryName)
				: Singleton<EntitlementsManager>.Instance.GetMyAvatarDataByCategory(categoryName);
			AssetManager assetManager = MonoSingleton<AssetManager>.Instance;
			if (avatarDataByCategory != null)
			{
				for (int i = 0; i < avatarDataByCategory.Count; i++)
				{
					Avatar_Multiplane avatarData = avatarDataByCategory[i];
					bool isLocalCharacterAvatar = !includeAllLocalCharacterAvatars ||
						(assetManager != null && assetManager.IsBundleCached(avatarData.GetHd()));
					if (avatarData.GetUid() != disableId && !avatarData.GetHidden() && isLocalCharacterAvatar)
					{
						list.Add(avatarData);
					}
				}
			}
			return list;
		}

		public string GetDefaultUid()
		{
			if (!string.IsNullOrEmpty(disableId))
			{
				return disableId;
			}
			List<Avatar_Multiplane> myAvatarDataByCategory = Singleton<EntitlementsManager>.Instance.GetMyAvatarDataByCategory(categoryName);
			if (myAvatarDataByCategory != null)
			{
				return myAvatarDataByCategory[0].GetUid();
			}
			return null;
		}
	}
}
