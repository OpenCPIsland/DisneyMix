using System.Collections.Generic;
using Mix.Data;
using Mix.DeviceDb;
using Mix.Entitlements;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class AvatarCategoryTray : BaseSubSelector<AvatarCategoryTrayInfo, AvatarCategoryTrayItem>
	{
		public AvatarCategoryButton Hair;

		public AvatarCategoryButton Eye;

		public AvatarCategoryButton Brow;

		public AvatarCategoryButton Nose;

		public AvatarCategoryButton Mouth;

		public AvatarCategoryButton Accessory;

		public AvatarCategoryButton Skin;

		public AvatarCategoryButton Costume;

		public AvatarCategoryButton Hat;

		private void Start()
		{
		}

		private void Update()
		{
		}

		public override void Setup(ISubSelector<AvatarCategoryTrayInfo> aListener)
		{
			listener = aListener;
			thumbItems = new List<AvatarCategoryTrayItem>();
			thumbItems.Add(new AvatarCategoryTrayItem(Costume.gameObject, "Costume", Costume.GetLocText(), Costume.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Hair.gameObject, "Hair", Hair.GetLocText(), Hair.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Skin.gameObject, "Skin", Skin.GetLocText(), Skin.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Eye.gameObject, "Eyes", Eye.GetLocText(), Eye.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Brow.gameObject, "Brow", Brow.GetLocText(), Brow.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Nose.gameObject, "Nose", Nose.GetLocText(), Nose.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Mouth.gameObject, "Mouth", Mouth.GetLocText(), Mouth.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Accessory.gameObject, "Accessory", Accessory.GetLocText(), Accessory.disableEntitlementId));
			thumbItems.Add(new AvatarCategoryTrayItem(Hat.gameObject, "Hat", Hat.GetLocText(), Hat.disableEntitlementId));
			Transform transform = base.gameObject.transform.Find("CategoryContent");
			if (transform != null)
			{
				ToggleGroup component = transform.gameObject.GetComponent<ToggleGroup>();
				if (component != null)
				{
					SetToggleGroupForToggle(component, Hair.gameObject);
					SetToggleGroupForToggle(component, Eye.gameObject);
					SetToggleGroupForToggle(component, Brow.gameObject);
					SetToggleGroupForToggle(component, Nose.gameObject);
					SetToggleGroupForToggle(component, Mouth.gameObject);
					SetToggleGroupForToggle(component, Accessory.gameObject);
					SetToggleGroupForToggle(component, Skin.gameObject);
					SetToggleGroupForToggle(component, Costume.gameObject);
					SetToggleGroupForToggle(component, Hat.gameObject);
				}
			}
			SetHighlightForCategory(Hair.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Hair"));
			SetHighlightForCategory(Skin.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Skin"));
			SetHighlightForCategory(Eye.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Eyes"));
			SetHighlightForCategory(Brow.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Brow"));
			SetHighlightForCategory(Nose.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Nose"));
			SetHighlightForCategory(Mouth.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Mouth"));
			SetHighlightForCategory(Accessory.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Accessory"));
			SetHighlightForCategory(Costume.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Costume"));
			SetHighlightForCategory(Hat.gameObject, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew("Hat"));
			base.CurrentContent = ((base.CurrentContent == null) ? thumbItems[0].trayData : base.CurrentContent);
		}

		public override void ToggleHighlight(AvatarCategoryTrayItem aThumb)
		{
			List<BaseContentData> thumbContent = aThumb.trayData.GetThumbContent();
			foreach (BaseContentData item in thumbContent)
			{
				if (item.GetNew() && !Singleton<MixDocumentCollections>.Instance.contentSeenDocumentCollectionApi.IsContentSeen(item.GetUid()))
				{
					Singleton<MixDocumentCollections>.Instance.contentSeenDocumentCollectionApi.SetContentSeen(item.GetUid());
				}
			}
			SetHighlightForCategory(aThumb.mainObj, Singleton<EntitlementsManager>.Instance.IsAvatarCategoryNew(aThumb.trayData.categoryName));
		}

		public string GetDisableId(int direction)
		{
			if (thumbItems != null)
			{
				for (int i = 0; i < thumbItems.Count; i++)
				{
					if (thumbItems[i] != null && thumbItems[i].trayData.categoryName == GetContent(direction).categoryName)
					{
						return thumbItems[i].GetDeselectedEntitlement();
					}
				}
			}
			return string.Empty;
		}

		public override AvatarCategoryTrayInfo GetContent(int aPackIndex)
		{
			if (aPackIndex == 0)
			{
				return base.CurrentContent;
			}
			for (int i = 0; i < thumbItems.Count; i++)
			{
				if (base.CurrentContent != null && thumbItems[i].trayData.categoryName == base.CurrentContent.categoryName)
				{
					if (i + aPackIndex >= 0 && i + aPackIndex <= thumbItems.Count - 1)
					{
						return thumbItems[i + aPackIndex].trayData;
					}
					return null;
				}
			}
			return null;
		}

		public List<BaseContentData> GetSubContent(int aPackIndex)
		{
			AvatarCategoryTrayInfo content = GetContent(aPackIndex);
			return (content != null) ? content.GetThumbContent() : null;
		}

		public bool LoadCategory(string categoryName)
		{
			if (listener == null || thumbItems == null || thumbItems.Count == 0)
			{
				return false;
			}

			if (!string.IsNullOrEmpty(categoryName))
			{
				for (int i = 0; i < thumbItems.Count; i++)
				{
					if (thumbItems[i] != null && thumbItems[i].trayData != null && thumbItems[i].trayData.categoryName == categoryName)
					{
						listener.OnSubSelected(thumbItems[i].trayData);
						return true;
					}
				}
			}

			listener.OnSubSelected(thumbItems[0].trayData);
			return false;
		}

		public void OnCategoryClicked(GameObject aButton)
		{
			if (listener == null)
			{
				return;
			}
			Toggle component = aButton.GetComponent<Toggle>();
			if (component == null || !component.isOn)
			{
				return;
			}
			for (int i = 0; i < thumbItems.Count; i++)
			{
				if (thumbItems[i].mainObj.Equals(aButton))
				{
					listener.OnSubSelected(thumbItems[i].trayData);
					break;
				}
			}
		}

		private void SetHighlightForCategory(GameObject aItem, bool aState)
		{
			if (aItem.GetComponent<Animator>() != null)
			{
				aItem.GetComponent<Animator>().enabled = true;
				aItem.GetComponent<Animator>().Play((!aState) ? "idle" : "Highlight");
			}
		}

		private void SetToggleGroupForToggle(ToggleGroup tg, GameObject go)
		{
			Toggle component = go.GetComponent<Toggle>();
			if (component != null)
			{
				component.group = tg;
			}
		}
	}
}
