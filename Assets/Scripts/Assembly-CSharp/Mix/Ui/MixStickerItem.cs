using Disney.Mix.SDK;
using Mix.AssetBundles;
using Mix.Assets;
using Mix.Data;
using Mix.Entitlements;
using Mix.Session.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class MixStickerItem : BaseChatItem, IBundleObject, IScrollItem, IScrollItemHelper
	{
		private static GameObject leftPrefab;

		private static GameObject rightPrefab;

		private static float AVATAR_OFFSET = 100f;

		private bool hasAutoPlayed;

		private string bundleURL;

		private GameObject stickerObject;

		private bool isNewMessage;

		private bool isInstanced;

		private float fullWidth;

		private Sticker sticker;

		public MixStickerItem(IChatThread aThread, IChatMessage aMessage, ScrollView aScrollView, bool aIsNewMessage)
			: base(aThread, aMessage, aScrollView)
		{
			isNewMessage = aIsNewMessage;
			if (message.IsMine() && rightPrefab == null)
			{
				rightPrefab = Resources.Load<GameObject>("Prefabs/Screens/ChatMix/ChatStickerRight");
			}
			else if (leftPrefab == null)
			{
				leftPrefab = Resources.Load<GameObject>("Prefabs/Screens/ChatMix/ChatStickerLeft");
			}
			sticker = Singleton<EntitlementsManager>.Instance.GetStickerData((aMessage as IStickerMessage).ContentId);
		}

		float IScrollItemHelper.GetGameObjectHeight()
		{
			float num = ((sticker == null) ? 0f : ((float)sticker.GetHeight()));
			float num2 = ((thread is IOneOnOneChatThread) ? 0f : BaseChatItem.AVATAR_NAME_HEIGHT);
			return (!(num > 0f)) ? (-1f) : (num + num2);
		}

		GameObject IScrollItem.GenerateGameObject(bool aGenerateForHeightOnly)
		{
			base.instance = Object.Instantiate((!message.IsMine()) ? leftPrefab : rightPrefab);
			SetupOffline("Content/ResendBtn", "Content/StickerHolder/ContextualLoader", "Content/StickerHolder/Error", "Content/StickerHolder/ForceUpdate");
			if (aGenerateForHeightOnly)
			{
				SetHeight();
				return base.instance;
			}
			if (sticker == null)
			{
				OnForceUpdateDetected((message as IStickerMessage).ContentId);
				return base.instance;
			}
			LoadStickerObject(aGenerateForHeightOnly);
			return base.instance;
		}

		void IScrollItem.Destroy()
		{
			OnDestroy();
			if (isInstanced && MonoSingleton<AssetManager>.Instance != null)
			{
				isInstanced = false;
				MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(bundleURL, stickerObject);
				stickerObject = null;
			}
		}

		void IBundleObject.OnBundleAssetObject(Object aGameObject, object aUserData)
		{
			if (this == null || base.instance.IsNullOrDisposed())
			{
				return;
			}
			LoaderObject.SetActive(false);
			GameObject gameObject = base.instance.transform.Find("Content/StickerHolder").gameObject;
			gameObject.SetActive(false);
			if (stickerObject != null)
			{
				MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(bundleURL, stickerObject);
				stickerObject = null;
			}
			Object stickerAsset = MonoSingleton<AssetManager>.Instance.GetBundleInstance(bundleURL);
			stickerObject = stickerAsset as GameObject;
			if (stickerObject == null)
			{
				if (stickerAsset != null)
				{
					MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(bundleURL, stickerAsset);
				}
				ErrorObject.SetActive(true);
				return;
			}
			isInstanced = true;
			gameObject.SetActive(true);
			stickerObject.transform.SetParent(gameObject.transform, false);
			float num = 0f;
			if (message.IsMine())
			{
				num = ResendObject.GetComponent<RectTransform>().sizeDelta.x;
			}
			base.instance.GetComponent<RectTransform>().sizeDelta = new Vector2(stickerObject.GetComponent<RectTransform>().sizeDelta.x + num + ((thread is IOneOnOneChatThread) ? 0f : AVATAR_OFFSET), stickerObject.GetComponent<RectTransform>().sizeDelta.y + avatarNameHeight);
			fullWidth = base.instance.GetComponent<RectTransform>().sizeDelta.x - num;
			stickerObject.SetActive(true);
			if (!hasAutoPlayed && isNewMessage)
			{
				hasAutoPlayed = true;
				if (stickerObject.GetComponent<Button>() != null)
				{
					Singleton<SoundManager>.Instance.BuildSoundSticker(stickerObject, true);
				}
			}
			else if (!hasAutoPlayed && stickerObject.GetComponent<Button>() != null)
			{
				Singleton<SoundManager>.Instance.BuildSoundSticker(stickerObject, false);
			}
			SetHeight();
		}

		public override void UpdateClientMessage(IChatMessage aClientMessage)
		{
			message = aClientMessage;
			if (!base.instance.IsNullOrDisposed() && message.IsMine())
			{
				RectTransform component = base.instance.GetComponent<RectTransform>();
				component.sizeDelta = new Vector2(fullWidth + ResendObject.GetComponent<RectTransform>().sizeDelta.x, component.sizeDelta.y);
				ResendObject.SetActive(FailedToSend);
			}
		}

		protected override void OnResendClicked()
		{
			if (ResendObject != null)
			{
				ResendObject.SetActive(false);
			}
			ResendFailedMessage(message);
		}

		protected override void LoadObject()
		{
			LoadStickerObject(false);
		}

		private void SetHeight()
		{
			float num = ((sticker == null) ? 0f : ((float)sticker.GetHeight()));
			if (num > 0f)
			{
				RectTransform component = base.instance.GetComponent<RectTransform>();
				component.sizeDelta = new Vector2(component.sizeDelta.x, num + avatarNameHeight);
			}
		}

		private void LoadStickerObject(bool aGenerateForHeightOnly)
		{
			if (sticker != null)
			{
				bundleURL = sticker.GetHd();
				if (!MonoSingleton<AssetManager>.Instance.WillBundleLoadFromWeb(bundleURL))
				{
					LoaderObject.SetActive(false);
				}
				MonoSingleton<AssetManager>.Instance.LoadABundle(this, bundleURL, null, string.Empty, false, false, true);
			}
			SetupAndSkinAvatar();
			SetHeight();
		}
	}
}
