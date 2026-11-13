using Disney.Mix.SDK;
using Mix.AssetBundles;
using Mix.Assets;
using Mix.Data;
using Mix.Entitlements;
using Mix.GagManagement;
using Mix.Session.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class MixGagController : MonoBehaviour, IBundleObject
	{
		public MixGagItem chatItem;

		private Gag gag;

		private bool playGag;

		private bool replayGag;

		private bool replaceImage = true;

		private bool checkForCheck = true;

		private Object ActiveObject;

		private Sprite createdSprite;

		void IBundleObject.OnBundleAssetObject(Object aGameObject, object aUserData)
		{
			if (this.IsNullOrDisposed() || chatItem == null)
			{
				return;
			}
			if (aGameObject == null)
			{
				chatItem.ErrorObject.SetActive(true);
			}
			else if (aUserData != null)
			{
				GameObject gameObject = Util.FindGameObjectByName(base.gameObject, "GagImage");
				if (!(gameObject != null))
				{
					return;
				}
gameObject.SetActive(true);
if (ActiveObject != null) return;   // already have it (guards against two loads in flight)

ActiveObject = MonoSingleton<AssetManager>.Instance.GetBundleInstance(gag.GetThumb());
if (ActiveObject is Texture2D)
{
    Texture2D tex = (Texture2D)ActiveObject;
    createdSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0f, 0f));
    gameObject.GetComponent<Image>().sprite = createdSprite;
    return;
}
GameObject gameObject2 = (GameObject)ActiveObject;
if (gameObject2 != null)
{
    gameObject.GetComponent<Image>().sprite = gameObject2.GetComponent<Image>().sprite;
}
			}
			else
			{
				GagManager.SenderPosition senderPosition = GagManager.SenderPosition.LEFT;
				if (chatItem.message != null && chatItem.message.IsMine())
				{
					senderPosition = GagManager.SenderPosition.RIGHT;
				}
				else if (!chatItem.hasBeenSeen)
				{
					chatItem.hasBeenSeen = true;
					replaceImage = true;
				}
				if (chatItem.thread is IGroupChatThread)
				{
					IGagMessage gagMessage = chatItem.message as IGagMessage;
					IAvatarHolder avatarHolderFromId = chatItem.thread.GetAvatarHolderFromId(gagMessage.SenderId);
					IAvatarHolder avatarHolderFromId2 = chatItem.thread.GetAvatarHolderFromId(gagMessage.TargetUserId);
					MonoSingleton<GagManager>.Instance.PlayGroupGag((GameObject)aGameObject, gag.GetHd(), senderPosition, avatarHolderFromId.Avatar, avatarHolderFromId2.Avatar, "sender", "receiver", true);
				}
				else
				{
					MonoSingleton<GagManager>.Instance.PlayGag((GameObject)aGameObject, gag.GetHd(), senderPosition, "sender", "receiver", true);
				}
			}
		}

		private void Update()
		{
			if (gag == null && chatItem != null)
			{
				gag = Singleton<EntitlementsManager>.Instance.GetGagData(((IGagMessage)chatItem.message).ContentId);
				if (replayGag)
				{
					Analytics.LogReplayGag(chatItem, gag.GetName());
					replayGag = false;
				}
			}
			if (gag == null)
			{
				return;
			}
if (replaceImage)
{
    if ((chatItem.message != null && chatItem.message.IsMine()) || chatItem.hasBeenSeen || gag.IsAutoPlay())
    {
        if (ActiveObject == null)   // don't reload if we already have it
        {
            MonoSingleton<AssetManager>.Instance.LoadABundle(this, gag.GetThumb(), "thumb", string.Empty, false, false, true);
        }
    }
    else
    {
        GameObject go = Util.FindGameObjectByName(base.gameObject, "GagImage");
        if (go != null) go.SetActive(true);
    }
    replaceImage = false;
}
			if (checkForCheck && chatItem.message != null && chatItem.message.IsMine() && chatItem.hasBeenSeen)
			{
				GameObject gameObject2 = Util.FindGameObjectByName(base.gameObject, "CheckIcon");
				if (gameObject2 != null)
				{
					gameObject2.SetActive(true);
				}
				checkForCheck = false;
			}
			if (playGag)
			{
				MonoSingleton<AssetManager>.Instance.LoadABundle(this, gag.GetHd(), null, string.Empty, false, false, true);
				playGag = false;
			}
		}

		private void OnDestroy()
		{
if (createdSprite != null) { 
	Object.Destroy(createdSprite); 
	createdSprite = null; 
	}

		}

		public void OnPlayGag()
		{
			if (MonoSingleton<GagManager>.Instance != null && !MonoSingleton<GagManager>.Instance.IsGagPlaying())
			{
				playGag = true;
			}
			if (gag != null)
			{
				Analytics.LogReplayGag(chatItem, gag.GetName());
			}
			else
			{
				replayGag = true;
			}
		}

		public void AutoPlayGag()
		{
			playGag = true;
		}
	}
}
