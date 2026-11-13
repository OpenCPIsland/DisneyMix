using System;
using System.Collections.Generic;
using Disney.Mix.SDK;
using LitJson;
using Mix.AssetBundles;
using Mix.Assets;
using Mix.Avatar;
using Mix.Data;
using Mix.Entitlements;
using Mix.Games;
using Mix.Games.Chat;
using Mix.Games.Data;
using Mix.Games.Message;
using Mix.Games.Session;
using Mix.Games.Ui;
using Mix.Session.Extensions;
using UnityEngine;

namespace Mix.Ui
{
	public class MixGameResponseItem : BaseChatItem, IGameEventMessageData, IGameMessageData, IBundleObject, IScrollItem, IScrollItemHelper
	{
		private sealed class GenerateGameObject_003Ec__AnonStorey2B1
		{
			internal Dictionary<string, object> state;

			internal MixGameResponseItem _003C_003Ef__this;

			internal void _003C_003Em__5B5(IGetGameStateMessageResult result)
			{
				if (result.Success)
				{
					_003C_003Ef__this.mChatThreadGameSession.MessageData = _003C_003Ef__this;
					state = (_003C_003Ef__this.mChatThreadGameSession.MessageData as IGameEventMessageData).State;
					string json = (string)state["GameData"];
					_003C_003Ef__this.mGameData = JsonMapper.ToObject<MixGameData>(json);
				}
				else
				{
					List<Game> allGames = Singleton<EntitlementsManager>.Instance.GetAllGames();
					_003C_003Ef__this.mEntitlement = allGames[0];
				}
				_003C_003Ef__this.LoadGameObject(false, result);
			}
		}

		private const string RESULT_OBJECT = "Result";

		private const string GAME_READ_ERROR_OBJECT = "Content/Holder/GroupReadError";

		public GameObject GroupReadErrorObject;

		protected ChatThreadGameSession mChatThreadGameSession;

		protected MixGameData mGameData;

		private static GameObject prefabObjectLeft;

		private static GameObject prefabObjectRight;

		private IEntitlementGameData mEntitlement;

		private GameObject mGameChatItemChild;

		private SdkActions actionGenerator = new SdkActions();

		private Action cancelSnapshot;

		public string SenderId
		{
			get
			{
				return message.SenderId;
			}
		}

		public bool Sent
		{
			get
			{
				return message.Sent;
			}
		}

		public DateTime TimeSent
		{
			get
			{
				return message.TimeSent;
			}
		}

		public string Id
		{
			get
			{
				return message.Id;
			}
		}

		public bool IsMine
		{
			get
			{
				return message.IsMine();
			}
		}

		public object MixMessageType
		{
			get
			{
				return message;
			}
		}

		public string GameName
		{
			get
			{
				IGameEventMessage gameEventMessage = message as IGameEventMessage;
				return gameEventMessage.GameName;
			}
		}

		public Dictionary<string, object> EventState
		{
			get
			{
				return (message as IGameEventMessage).EventState;
			}
		}

public Dictionary<string, object> State
{
    get
    {
        IGameEventMessage gameEventMessage = message as IGameEventMessage;
        if (gameEventMessage == null)
        {
            return null;
        }
        IGameStateMessage gameStateMessage = gameEventMessage.GameStateMessage;
        return (gameStateMessage == null) ? null : gameStateMessage.State;
    }
}

		public string StateMessageId
		{
			get
			{
				IGameStateMessage gameStateMessage = (message as IGameEventMessage).GameStateMessage;
				return gameStateMessage.Id;
			}
		}

		public string StateMessageSenderId
		{
			get
			{
				IGameStateMessage gameStateMessage = (message as IGameEventMessage).GameStateMessage;
				return gameStateMessage.SenderId;
			}
		}

		public MixGameResponseItem(IChatThread aThread, IChatMessage aChatMessage, ScrollView aScrollView, IGameTray aGameListener)
			: base(aThread, aChatMessage, aScrollView)
		{
			if (prefabObjectLeft == null)
			{
				prefabObjectLeft = Resources.Load<GameObject>("Prefabs/Screens/ChatMix/GameUpdateItemHolderLeft");
			}
			if (prefabObjectRight == null)
			{
				prefabObjectRight = Resources.Load<GameObject>("Prefabs/Screens/ChatMix/GameUpdateItemHolderRight");
			}
			GameSessionThreadParameters aThreadParameters = new GameSessionThreadParameters(aThread);
			mChatThreadGameSession = new ChatThreadGameSession(MonoSingleton<GameManager>.Instance, aGameListener, aThreadParameters, this);
		}

		float IScrollItemHelper.GetGameObjectHeight()
		{
			float num = Singleton<EntitlementsManager>.Instance.GetAllGames()[0].GetResultsHeight();
			float num2 = ((mEntitlement == null) ? num : ((float)mEntitlement.GetResultsHeight()));
			float num3 = ((thread is IOneOnOneChatThread) ? 0f : BaseChatItem.AVATAR_NAME_HEIGHT);
			return (!(num2 > 0f)) ? (-1f) : (num2 + num3);
		}

		GameObject IScrollItem.GenerateGameObject(bool aGenerateForHeightOnly)
		{
			GenerateGameObject_003Ec__AnonStorey2B1 CS_0024_003C_003E8__locals12 = new GenerateGameObject_003Ec__AnonStorey2B1();
			CS_0024_003C_003E8__locals12._003C_003Ef__this = this;
			base.instance = UnityEngine.Object.Instantiate((!message.IsMine()) ? prefabObjectLeft : prefabObjectRight);
			SetupOffline("Content/Holder/ResendBtn", "Content/Holder/ContextualLoader", "Content/Holder/Error", "Content/Holder/ForceUpdate");
			CS_0024_003C_003E8__locals12.state = (mChatThreadGameSession.MessageData as IGameEventMessageData).State;
			if (CS_0024_003C_003E8__locals12.state == null)
			{
				thread.GetGameStateMessage(message as IGameEventMessage, actionGenerator.CreateAction(delegate(IGetGameStateMessageResult result)
				{
					if (result.Success)
					{
						CS_0024_003C_003E8__locals12._003C_003Ef__this.mChatThreadGameSession.MessageData = CS_0024_003C_003E8__locals12._003C_003Ef__this;
						CS_0024_003C_003E8__locals12.state = (CS_0024_003C_003E8__locals12._003C_003Ef__this.mChatThreadGameSession.MessageData as IGameEventMessageData).State;
						string json = (string)CS_0024_003C_003E8__locals12.state["GameData"];
						CS_0024_003C_003E8__locals12._003C_003Ef__this.mGameData = JsonMapper.ToObject<MixGameData>(json);
					}
					else
					{
						List<Game> allGames = Singleton<EntitlementsManager>.Instance.GetAllGames();
						CS_0024_003C_003E8__locals12._003C_003Ef__this.mEntitlement = allGames[0];
					}
					CS_0024_003C_003E8__locals12._003C_003Ef__this.LoadGameObject(false, result);
				}));
			}
			else
			{
				mGameData = JsonMapper.ToObject<MixGameData>((string)CS_0024_003C_003E8__locals12.state["GameData"]);
				LoadGameObject(aGenerateForHeightOnly);
			}
			SetupHeight();
			return base.instance;
		}

		void IScrollItem.Destroy()
		{
			OnDestroy();
			if (MonoSingleton<AssetManager>.Instance != null && mGameChatItemChild != null)
			{
				MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(mEntitlement.GetResult(), mGameChatItemChild);
			}
			if (cancelSnapshot != null)
			{
				cancelSnapshot();
			}
			mGameChatItemChild = null;
		}

		void IBundleObject.OnBundleAssetObject(UnityEngine.Object aGameObject, object aUserData)
		{
			if (LoaderObject.IsNullOrDisposed() || !mGameChatItemChild.IsNullOrDisposed() || base.instance.IsNullOrDisposed())
			{
				return;
			}
			LoaderObject.SetActive(false);
			if (aUserData != null && aUserData.ToString() == "Result")
			{
				if (mEntitlement != null)
				{
					SetupGameChatItem(mEntitlement.GetResult());
				}
				else
				{
					ShowAssetError();
				}
			}
			SetupHeight();
		}

		protected void LoadGameChatItem(bool aGenerateForHeightOnly)
		{
			if (!aGenerateForHeightOnly && mGameData != null && mEntitlement != null && !string.IsNullOrEmpty(mEntitlement.GetResult().Trim()))
			{
				if (!MonoSingleton<AssetManager>.Instance.WillBundleLoadFromWeb(mEntitlement.GetResult()))
				{
					LoaderObject.SetActive(false);
				}
				MonoSingleton<AssetManager>.Instance.LoadABundle(this, mEntitlement.GetResult(), "Result", string.Empty, false, false, true);
			}
		}

		private void LoadGameObject(bool aGenerateForHeightOnly, IGetGameStateMessageResult aResult = null)
		{
			if (base.instance == null)
			{
				return;
			}
			if (mGameData == null)
			{
				if (aResult != null)
				{
					LoaderObject.SetActive(false);
					if (aResult is IGetGameStateMessageForbiddenResult)
					{
						ErrorObject.SetActive(false);
						GroupReadErrorObject.SetActive(true);
					}
					else
					{
						ErrorObject.SetActive(true);
					}
				}
				return;
			}
			mEntitlement = Singleton<EntitlementsManager>.Instance.GetGameData(mGameData.Entitlement);
			if (mEntitlement == null)
			{
				if (!aGenerateForHeightOnly)
				{
					OnForceUpdateDetected(mGameData.Entitlement);
				}
				return;
			}
			if (!aGenerateForHeightOnly)
			{
				SetupOffline("Content/Holder/ResendBtn", "Content/Holder/ContextualLoader", "Content/Holder/Error", "Content/Holder/ForceUpdate");
				if (!(thread is IOneOnOneChatThread))
				{
					SkinAvatar();
				}
				LoadGameChatItem(aGenerateForHeightOnly);
			}
			SetupHeight();
		}

		private void SkinAvatar()
		{
			GameChatItem component = base.instance.GetComponent<GameChatItem>();
			IAvatarHolder avatarHolderFromId = thread.GetAvatarHolderFromId(message.SenderId);
			if (avatarHolderFromId != null && !component.IsNullOrDisposed())
			{
				component.AvatarContainer.SetActive(true);
				MonoSingleton<AvatarManager>.Instance.RenderAvatarSnapshotWithCancel(avatarHolderFromId, component.AvatarContainer, "ImageTarget", cancelSnapshot);
			}
		}

		protected override void LoadObject()
		{
			if (mGameData != null)
			{
				mEntitlement = Singleton<EntitlementsManager>.Instance.GetGameData(mGameData.Entitlement);
				LoadGameObject(false);
			}
		}

		protected void SetupHeight()
		{
			float num = ((mEntitlement == null) ? (-1f) : ((float)mEntitlement.GetResultsHeight()));
			if (num > 0f)
			{
				RectTransform component = base.instance.GetComponent<RectTransform>();
				component.sizeDelta = new Vector2(component.sizeDelta.x, num + avatarNameHeight);
			}
		}

		protected override void SetupOffline(string aResend = "", string aLoader = "", string aError = "", string aForceUpdate = "")
		{
			base.SetupOffline(aResend, aLoader, aError, aForceUpdate);
			Transform transform = base.instance.transform.Find("Content/Holder/GroupReadError");
			if (transform != null)
			{
				GroupReadErrorObject = transform.gameObject;
			}
		}

		private void ShowAssetError()
		{
			LoaderObject.SetActive(false);
			ErrorObject.SetActive(true);
		}

		private void SetupGameChatItem(string itemType)
		{
			if (base.instance == null)
			{
				ShowAssetError();
				return;
			}
			GameChatItem component = base.instance.GetComponent<GameChatItem>();
			if (component == null)
			{
				ShowAssetError();
				return;
			}
			if (mGameChatItemChild == null)
			{
				mGameChatItemChild = (GameObject)MonoSingleton<AssetManager>.Instance.GetBundleInstance(itemType);
			}
			if (mGameChatItemChild != null)
			{
				mGameChatItemChild.transform.SetParent(component.ItemTarget, false);
				mGameChatItemChild.SetActive(true);
				BaseGameChatController component2 = mGameChatItemChild.GetComponent<BaseGameChatController>();
				if (component2 != null)
				{
					component2.Initialize(mChatThreadGameSession, mEntitlement, component);
				}
			}
		}

		public override void UpdateClientMessage(IChatMessage aMessage)
		{
			message = aMessage;
			if (!base.instance.IsNullOrDisposed() && message.IsMine())
			{
				ResendObject.SetActive(FailedToSend);
			}
		}

		protected override void OnResendClicked()
		{
			ResendObject.SetActive(false);
			ResendFailedMessage(message);
		}

		public string GetJson()
		{
			IGameEventMessage gameEventMessage = message as IGameEventMessage;
			Dictionary<string, object> eventState = gameEventMessage.EventState;
			return (string)eventState["GameData"];
		}
	}
}
