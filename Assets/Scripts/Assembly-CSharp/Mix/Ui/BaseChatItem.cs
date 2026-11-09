using System;
using Disney.Mix.SDK;
using Mix.AssetBundles;
using Mix.Assets;
using Mix.Avatar;
using Mix.Data;
using Mix.DeviceDb;
using Mix.Entitlements;
using Mix.Session;
using Mix.Session.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public abstract class BaseChatItem : IBundleObject, IEntitlementsManager
	{
		private const int MAX_NUM_MANIFEST_RETRIES = 3;

		public static float GAG_PREFAB_HEIGHT = 170f;

		public static float AVATAR_NAME_HEIGHT = 35f;

		public DateTime DateTime;

		public ScrollView ScrollView;

		public bool FailedToSend;

		public IChatMessage message;

		public IChatThread thread;

		public GameObject ResendObject;

		public GameObject LoaderObject;

		public GameObject ErrorObject;

		public GameObject ForceUpdateObject;

		protected Transform avatarName;

		protected float avatarNameHeight;

		private bool isIconLoaded;

		private string entitlementId;

		private int numManifestRetries;

		private SdkActions actionGenerator = new SdkActions();

		private readonly Action cancelSnapshot;

		public GameObject instance { get; protected set; }

		protected BaseChatItem(IChatThread aThread, IChatMessage aMessage, ScrollView aScrollView)
		{
			DateTime = aMessage.TimeSent;
			ScrollView = aScrollView;
			thread = aThread;
			message = aMessage;
			FailedToSend = !aMessage.Sent;
		}

		void IEntitlementsManager.OnEntitlementsManagerReady(bool isSuccessful)
		{
			if (isSuccessful)
			{
				if (Singleton<EntitlementsManager>.Instance.DoesEntitlementExist(entitlementId))
				{
					ErrorObject.SetActive(false);
					ForceUpdateObject.SetActive(false);
					LoadObject();
					return;
				}
				ChatController lastProcessedRequestController = MonoSingleton<NavigationManager>.Instance.GetLastProcessedRequestController<ChatController>();
				if (lastProcessedRequestController != null)
				{
					if (ErrorObject != null)
					{
						ErrorObject.SetActive(false);
					}
					if (ForceUpdateObject != null)
					{
						ForceUpdateObject.SetActive(true);
					}
					if (!lastProcessedRequestController.ForceUpdateNotificationBar.activeInHierarchy)
					{
						lastProcessedRequestController.ForceUpdateNotificationBar.SetActive(true);
					}
				}
			}
			else
			{
				numManifestRetries++;
				if (MixSession.connection == MixSession.ConnectionState.ONLINE && numManifestRetries < 3)
				{
					Singleton<EntitlementsManager>.Instance.LoadNewContentData(this);
				}
			}
		}

		void IBundleObject.OnBundleAssetObject(UnityEngine.Object aGameObject, object aUserData)
		{
			if (this == null || instance.IsNullOrDisposed())
			{
				return;
			}
			Transform transform = instance.transform.Find("Content/AvatarLeft/ImageTarget");
			Transform transform2 = instance.transform.Find(message.IsMine() ? "Content/PlaceholderAvatarRight/AvatarHeadRight/ImageTarget" : "Content/PlaceholderAvatarLeft/AvatarHeadLeft/ImageTarget");
			if (transform != null)
			{
				Image component = transform.gameObject.GetComponent<Image>();
				GameObject gameObject = MonoSingleton<AssetManager>.Instance.GetBundleInstance((string)aUserData) as GameObject;
				if (gameObject != null)
				{
					isIconLoaded = true;
					Image component2 = gameObject.GetComponent<Image>();
					if (component2 != null)
					{
						Image component3 = component.GetComponent<Image>();
						component3.sprite = component2.sprite;
						component3.enabled = true;
					}
					UnityEngine.Object.Destroy(gameObject);
				}
			}
			else
			{
				if (!(transform2 != null))
				{
					return;
				}
				Image component4 = transform2.gameObject.GetComponent<Image>();
				GameObject gameObject2 = MonoSingleton<AssetManager>.Instance.GetBundleInstance((string)aUserData) as GameObject;
				if (gameObject2 != null)
				{
					Image component5 = gameObject2.GetComponent<Image>();
					if (component5 != null)
					{
						Image component6 = component4.GetComponent<Image>();
						component6.sprite = component5.sprite;
						component6.enabled = true;
					}
					UnityEngine.Object.Destroy(gameObject2);
				}
			}
		}

		public void SaveHeight(float height)
		{
			Singleton<MixDocumentCollections>.Instance.chatMetaDataDocumentCollectionApi.AddHeight(message.Id, height);
		}

		public abstract void UpdateClientMessage(IChatMessage aMessage);

		public void BaseUpdateClientMessage(IChatMessage aMessage)
		{
			FailedToSend = !aMessage.Sent;
			DateTime = aMessage.TimeSent;
			UpdateClientMessage(aMessage);
		}

		protected void ResendFailedMessage(IChatMessage message)
		{
			FailedToSend = false;
			thread.ResendChatMessage(message, actionGenerator.CreateAction(delegate(IResendChatMessageResult e)
			{
				FailedToSend = !e.Success;
				UpdateClientMessage(message);
			}));
			Analytics.LogChatFail(thread);
			Analytics.LogResend(thread);
		}

		protected virtual void OnResendClicked()
		{
		}

		protected virtual void LoadObject()
		{
		}

		protected virtual void SetupOffline(string aResend = "", string aLoader = "", string aError = "", string aForceUpdate = "")
		{
			avatarName = instance.transform.Find("AvatarNameText");
			if (avatarName != null)
			{
				avatarName.gameObject.SetActive(false);
				if (!(thread is IOneOnOneChatThread) && !message.IsMine())
				{
					avatarNameHeight = avatarName.GetComponent<LayoutElement>().preferredHeight;
					avatarName.gameObject.SetActive(true);
					avatarName.GetComponent<Text>().text = thread.GetNickOrDisplayById(message.SenderId);
				}
			}
			Transform transform = instance.transform.Find(aResend);
			if (transform != null)
			{
				ResendObject = transform.gameObject;
				Button component = ResendObject.GetComponent<Button>();
				component.onClick.AddListener(OnResendClicked);
				ResendObject.SetActive(FailedToSend);
			}
			Transform transform2 = instance.transform.Find(aLoader);
			if (transform2 != null)
			{
				LoaderObject = transform2.gameObject;
			}
			Transform transform3 = instance.transform.Find(aError);
			if (transform3 != null)
			{
				ErrorObject = transform3.gameObject;
			}
			Transform transform4 = instance.transform.Find(aForceUpdate);
			if (transform4 != null)
			{
				ForceUpdateObject = transform4.gameObject;
			}
			if (thread is IOfficialAccountChatThread)
			{
				Official_Account officialAccount = Singleton<EntitlementsManager>.Instance.GetOfficialAccount(((IOfficialAccountChatThread)thread).OfficialAccount.AccountId);
				if (officialAccount != null)
				{
					Util.UpdateTintablesForOfficialAccount(officialAccount, instance);
				}
			}
		}

		public void OnForceUpdateDetected(string aEntitlementId)
		{
			entitlementId = aEntitlementId;
			MonoSingleton<AssetManager>.Instance.cpipeManager.OnCpipeLoaded += OnCpipeLoaded;
			if (entitlementId != null && MixSession.connection == MixSession.ConnectionState.ONLINE)
			{
				MonoSingleton<AssetManager>.Instance.cpipeManager.LoadCpipe(CachePolicy.DefaultCacheControlProtocol);
			}
			LoaderObject.SetActive(false);
			ErrorObject.SetActive(true);
		}

		private void OnCpipeLoaded(CpipeEvent cpipeEvent)
		{
			switch (cpipeEvent)
			{
			case CpipeEvent.CpipeUnchanged:
			case CpipeEvent.CpipeData:
				MonoSingleton<AssetManager>.Instance.cpipeManager.OnCpipeLoaded -= OnCpipeLoaded;
				Singleton<EntitlementsManager>.Instance.LoadNewContentData(this);
				break;
			case CpipeEvent.CpipeDataFailed:
				numManifestRetries++;
				if (MixSession.connection == MixSession.ConnectionState.ONLINE)
				{
					if (numManifestRetries < 3)
					{
						MonoSingleton<AssetManager>.Instance.cpipeManager.LoadCpipe(CachePolicy.DefaultCacheControlProtocol);
					}
					else
					{
						MonoSingleton<AssetManager>.Instance.cpipeManager.OnCpipeLoaded -= OnCpipeLoaded;
					}
				}
				break;
			}
		}

		protected void SetupAndSkinAvatar()
		{
			string text = (message.IsMine() ? "Content/PlaceholderAvatarRight/AvatarHeadRight/ImageTarget" : "Content/PlaceholderAvatarLeft/AvatarHeadLeft/ImageTarget");
			IAvatarHolder avatarHolderFromId = thread.GetAvatarHolderFromId(message.SenderId);
			Transform transform = instance.transform.Find(text);
			Transform transform2 = instance.transform.Find("Content/AvatarLeft/ImageTarget");
			Debug.Log($"[Avatar] mine={message.IsMine()} path={text} target={(transform != null)} holder={(avatarHolderFromId != null)} sender={message.SenderId}");
			if (thread is IOfficialAccountChatThread)
			{
				if (!message.IsMine())
				{
					Transform transform3 = ((!(transform2 != null)) ? transform : transform2);
					transform3.parent.gameObject.SetActive(true);
					string iconOfBotOrOA = Singleton<EntitlementsManager>.Instance.GetIconOfBotOrOA(message.SenderId);
					if (!string.IsNullOrEmpty(iconOfBotOrOA))
					{
						MonoSingleton<AssetManager>.Instance.LoadABundle(this, iconOfBotOrOA, iconOfBotOrOA, string.Empty, true, false, true);
					}
				}
				else
				{
					MonoSingleton<AvatarManager>.Instance.RenderAvatarSnapshotWithCancel(avatarHolderFromId, instance, text, cancelSnapshot);
				}
			}
			else if (thread is IGroupChatThread && transform != null)
			{
				MonoSingleton<AvatarManager>.Instance.RenderAvatarSnapshotWithCancel(avatarHolderFromId, instance, text, cancelSnapshot);
			}
			else if (thread is IOneOnOneChatThread && transform != null)
			{
				transform.parent.parent.gameObject.SetActive(false);
			}
		}

		protected void OnDestroy()
		{
			if (cancelSnapshot != null)
			{
				cancelSnapshot();
			}
			if (MonoSingleton<AssetManager>.Instance != null)
			{
				MonoSingleton<AssetManager>.Instance.CancelBundles(this);
				if (thread is IOfficialAccountChatThread && !message.IsMine() && isIconLoaded)
				{
					MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(Singleton<EntitlementsManager>.Instance.GetIconOfBotOrOA(message.SenderId));
				}
				MonoSingleton<AssetManager>.Instance.cpipeManager.OnCpipeLoaded -= OnCpipeLoaded;
			}
		}
	}
}
