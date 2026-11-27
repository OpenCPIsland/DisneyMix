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
		private const string LOG_TAG = "[MixGameResponseItem] ";

		private const string RESULT_OBJECT = "Result";

		private const string GAME_READ_ERROR_OBJECT = "Content/Holder/GroupReadError";

		private const string GAME_DATA_KEY = "GameData";

		public GameObject GroupReadErrorObject;

		protected ChatThreadGameSession mChatThreadGameSession;

		protected MixGameData mGameData;

		private static GameObject prefabObjectLeft;

		private static GameObject prefabObjectRight;

		private IEntitlementGameData mEntitlement;

		private GameObject mGameChatItemChild;

		private SdkActions actionGenerator = new SdkActions();

		private Action cancelSnapshot;

		// Bumped on every Generate and Destroy so late SDK callbacks from an
		// earlier generation are ignored instead of touching a dead instance.
		private int mGeneration;

		#region IGameMessageData / IGameEventMessageData

		public string SenderId
		{
			get { return message.SenderId; }
		}

		public bool Sent
		{
			get { return message.Sent; }
		}

		public DateTime TimeSent
		{
			get { return message.TimeSent; }
		}

		public string Id
		{
			get { return message.Id; }
		}

		public bool IsMine
		{
			get { return message.IsMine(); }
		}

		public object MixMessageType
		{
			get { return message; }
		}

		public string GameName
		{
			get
			{
				IGameEventMessage gameEventMessage = message as IGameEventMessage;
				return (gameEventMessage == null) ? null : gameEventMessage.GameName;
			}
		}

		public Dictionary<string, object> EventState
		{
			get
			{
				IGameEventMessage gameEventMessage = message as IGameEventMessage;
				return (gameEventMessage == null) ? null : gameEventMessage.EventState;
			}
		}

		public Dictionary<string, object> State
		{
			get
			{
				IGameStateMessage gameStateMessage = GetGameStateMessage();
				return (gameStateMessage == null) ? null : gameStateMessage.State;
			}
		}

		public string StateMessageId
		{
			get
			{
				IGameStateMessage gameStateMessage = GetGameStateMessage();
				return (gameStateMessage == null) ? null : gameStateMessage.Id;
			}
		}

		public string StateMessageSenderId
		{
			get
			{
				IGameStateMessage gameStateMessage = GetGameStateMessage();
				return (gameStateMessage == null) ? null : gameStateMessage.SenderId;
			}
		}

		public string GetJson()
		{
			return ReadGameDataJson(EventState);
		}

		#endregion

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

		#region IScrollItem / IScrollItemHelper

		float IScrollItemHelper.GetGameObjectHeight()
		{
			// Before the real entitlement is known, fall back to the first game's height.
			IEntitlementGameData entitlement = mEntitlement ?? GetFallbackEntitlement();
			if (entitlement == null)
			{
				return -1f;
			}
			float resultsHeight = (float)entitlement.GetResultsHeight();
			float avatarHeight = (thread is IOneOnOneChatThread) ? 0f : BaseChatItem.AVATAR_NAME_HEIGHT;
			return (resultsHeight > 0f) ? (resultsHeight + avatarHeight) : (-1f);
		}

		GameObject IScrollItem.GenerateGameObject(bool aGenerateForHeightOnly)
		{
			int generation = ++mGeneration;

			base.instance = UnityEngine.Object.Instantiate(message.IsMine() ? prefabObjectRight : prefabObjectLeft);
			SetupOffline("Content/Holder/ResendBtn", "Content/Holder/ContextualLoader", "Content/Holder/Error", "Content/Holder/ForceUpdate");

			Dictionary<string, object> state = GetSessionState();
			if (state == null)
			{
				// State isn't cached locally yet; fetch it, then finish loading in the callback.
				thread.GetGameStateMessage(
					message as IGameEventMessage,
					actionGenerator.CreateAction((IGetGameStateMessageResult result) => OnGameStateMessageResult(result, generation)));
			}
			else
			{
				mGameData = ParseGameData(state);
				if (mGameData == null && !aGenerateForHeightOnly)
				{
					ShowAssetError();
				}
				else
				{
					LoadGameObject(aGenerateForHeightOnly);
				}
			}
			SetupHeight();
			return base.instance;
		}

		void IScrollItem.Destroy()
		{
			mGeneration++;
			OnDestroy();

			AssetManager assetManager = MonoSingleton<AssetManager>.Instance;
			if (assetManager != null && mGameChatItemChild != null && mEntitlement != null)
			{
				string bundle = mEntitlement.GetResult();
				if (!string.IsNullOrEmpty(bundle))
				{
					assetManager.DestroyBundleInstance(bundle, mGameChatItemChild);
				}
			}
			if (cancelSnapshot != null)
			{
				cancelSnapshot();
			}
			mGameChatItemChild = null;
		}

		#endregion

		#region IBundleObject

		void IBundleObject.OnBundleAssetObject(UnityEngine.Object aGameObject, object aUserData)
		{
			if (LoaderObject.IsNullOrDisposed() || !mGameChatItemChild.IsNullOrDisposed() || base.instance.IsNullOrDisposed())
			{
				return;
			}
			LoaderObject.SetActive(false);
			if (aUserData != null && aUserData.ToString() == RESULT_OBJECT)
			{
				string bundle = (mEntitlement == null) ? null : mEntitlement.GetResult();
				if (!string.IsNullOrEmpty(bundle))
				{
					SetupGameChatItem(bundle);
				}
				else
				{
					ShowAssetError();
				}
			}
			SetupHeight();
		}

		#endregion

		#region Loading

		private void OnGameStateMessageResult(IGetGameStateMessageResult result, int generation)
		{
			// The item was destroyed or regenerated while the request was in flight.
			if (generation != mGeneration)
			{
				return;
			}
			if (result != null && result.Success)
			{
				mChatThreadGameSession.MessageData = this;
				mGameData = ParseGameData(State);
			}
			else
			{
				mEntitlement = GetFallbackEntitlement();
			}
			LoadGameObject(false, result);
		}

		protected void LoadGameChatItem(bool aGenerateForHeightOnly)
		{
			if (aGenerateForHeightOnly || mGameData == null || mEntitlement == null)
			{
				return;
			}

			string bundle = mEntitlement.GetResult();
			if (string.IsNullOrEmpty(bundle) || string.IsNullOrEmpty(bundle.Trim()))
			{
				// Some games (e.g. Fireworks) have no result card; responses to them show nothing, as in the original client.
				SetActive(LoaderObject, false);
				return;
			}

			AssetManager assetManager = MonoSingleton<AssetManager>.Instance;
			if (assetManager == null)
			{
				Debug.LogWarning(LOG_TAG + "AssetManager is not available");
				ShowAssetError();
				return;
			}

			if (!assetManager.WillBundleLoadFromWeb(bundle))
			{
				SetActive(LoaderObject, false);
			}
			assetManager.LoadABundle(this, bundle, RESULT_OBJECT, string.Empty, false, false, true);
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
					SetActive(LoaderObject, false);
					if (aResult is IGetGameStateMessageForbiddenResult)
					{
						SetActive(ErrorObject, false);
						SetActive(GroupReadErrorObject, true);
					}
					else
					{
						SetActive(ErrorObject, true);
					}
				}
				return;
			}

			EntitlementsManager entitlementsManager = Singleton<EntitlementsManager>.Instance;
			mEntitlement = (entitlementsManager == null) ? null : entitlementsManager.GetGameData(mGameData.Entitlement);
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

		protected override void LoadObject()
		{
			if (mGameData == null)
			{
				return;
			}
			EntitlementsManager entitlementsManager = Singleton<EntitlementsManager>.Instance;
			mEntitlement = (entitlementsManager == null) ? null : entitlementsManager.GetGameData(mGameData.Entitlement);
			LoadGameObject(false);
		}

		#endregion

		#region UI

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

		protected void SetupHeight()
		{
			if (base.instance == null || mEntitlement == null)
			{
				return;
			}
			float height = (float)mEntitlement.GetResultsHeight();
			if (height > 0f)
			{
				RectTransform component = base.instance.GetComponent<RectTransform>();
				if (component != null)
				{
					component.sizeDelta = new Vector2(component.sizeDelta.x, height + avatarNameHeight);
				}
			}
		}

		protected override void SetupOffline(string aResend = "", string aLoader = "", string aError = "", string aForceUpdate = "")
		{
			base.SetupOffline(aResend, aLoader, aError, aForceUpdate);
			Transform groupReadError = base.instance.transform.Find(GAME_READ_ERROR_OBJECT);
			if (groupReadError != null)
			{
				GroupReadErrorObject = groupReadError.gameObject;
			}
		}

		private void ShowAssetError()
		{
			SetActive(LoaderObject, false);
			SetActive(ErrorObject, true);
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
				AssetManager assetManager = MonoSingleton<AssetManager>.Instance;
				if (assetManager == null)
				{
					ShowAssetError();
					return;
				}
				mGameChatItemChild = assetManager.GetBundleInstance(itemType) as GameObject;
			}

			if (mGameChatItemChild != null)
			{
				mGameChatItemChild.transform.SetParent(component.ItemTarget, false);
				mGameChatItemChild.SetActive(true);
				BaseGameChatController controller = mGameChatItemChild.GetComponent<BaseGameChatController>();
				if (controller != null)
				{
					controller.Initialize(mChatThreadGameSession, mEntitlement, component);
				}
			}
		}

		public override void UpdateClientMessage(IChatMessage aMessage)
		{
			message = aMessage;
			if (!base.instance.IsNullOrDisposed() && message.IsMine())
			{
				SetActive(ResendObject, FailedToSend);
			}
		}

		protected override void OnResendClicked()
		{
			SetActive(ResendObject, false);
			ResendFailedMessage(message);
		}

		private static void SetActive(GameObject target, bool active)
		{
			if (target != null)
			{
				target.SetActive(active);
			}
		}

		#endregion

		#region Helpers

		private IGameStateMessage GetGameStateMessage()
		{
			IGameEventMessage gameEventMessage = message as IGameEventMessage;
			return (gameEventMessage == null) ? null : gameEventMessage.GameStateMessage;
		}

		private Dictionary<string, object> GetSessionState()
		{
			if (mChatThreadGameSession == null)
			{
				return null;
			}
			IGameEventMessageData data = mChatThreadGameSession.MessageData as IGameEventMessageData;
			return (data == null) ? null : data.State;
		}

		private static string ReadGameDataJson(Dictionary<string, object> state)
		{
			object value;
			if (state == null || !state.TryGetValue(GAME_DATA_KEY, out value))
			{
				return null;
			}
			return value as string;
		}

		private static MixGameData ParseGameData(Dictionary<string, object> state)
		{
			string json = ReadGameDataJson(state);
			if (string.IsNullOrEmpty(json))
			{
				Debug.LogWarning(LOG_TAG + "Game state has no '" + GAME_DATA_KEY + "' entry");
				return null;
			}
			try
			{
				return JsonMapper.ToObject<MixGameData>(json);
			}
			catch (Exception ex)
			{
				Debug.LogWarning(LOG_TAG + "Failed to parse GameData: " + ex.Message);
				return null;
			}
		}

		private static IEntitlementGameData GetFallbackEntitlement()
		{
			EntitlementsManager entitlementsManager = Singleton<EntitlementsManager>.Instance;
			if (entitlementsManager == null)
			{
				return null;
			}
			List<Game> allGames = entitlementsManager.GetAllGames();
			return (allGames != null && allGames.Count > 0) ? allGames[0] : null;
		}

		#endregion
	}
}