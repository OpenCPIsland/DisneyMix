using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avatar;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Disney.Native;
using LitJson;
using Mix.AssetBundles;
using Mix.Assets;
using Mix.Avatar;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.Games.Data;
using Mix.Games.Message;
using Mix.Games.Session;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using Mix.Session.Extensions;
using Mix.Threading;
using Mix.Tracking;
using Mix.Ui;
using Mix.Ui.Events;
using UnityEngine;

namespace Mix.Games
{
	public class GameManager : MonoSingleton<GameManager>, IGameAssetManager, IGameAnalytics, IGameAvatar, IGameInput, IGameLocalization, IGameLogging, IGamePlayerInfo, IGameSounds, IGameStatistics, IGameModeration, IGameSession, IGameSessionSettings, IBundleObject, IZipJsonAssetObject
	{
		private sealed class ModerateTextMessage_003Ec__AnonStorey245
		{
			internal IGameModerationResult aGame;

			internal IEntitlementGameData aEntitlement;

			internal object aUserData;

			internal GameManager _003C_003Ef__this;

			internal void _003C_003Em__4A8(ITextModerationResult result)
			{
				if (aGame == null)
				{
					return;
				}
				if (result.Success)
				{
					if (result.IsModerated && _003C_003Ef__this.ActiveSession != null)
					{
						_003C_003Ef__this.ActiveSession.LogAction(aEntitlement.GetName() + "_text_moderated");
					}
					aGame.OnModerationResult(result.IsModerated, result.ModeratedText, aUserData);
				}
				else
				{
					aGame.OnModerationError(aUserData);
				}
			}
		}

		private sealed class LoadSnapshot_003Ec__AnonStorey246
		{
			internal Action<bool, Sprite> completionCallback;

			internal void _003C_003Em__4A9(bool success, Sprite sprite)
			{
				completionCallback(success, sprite);
			}
		}

		private sealed class GetFriendDisplayName_003Ec__AnonStorey247
		{
			internal string id;

			internal bool _003C_003Em__4AA(string memberId)
			{
				return memberId == id;
			}

			internal bool _003C_003Em__4AB(string memberId)
			{
				return memberId == id;
			}
		}

		private sealed class PostGameEventData_003Ec__AnonStorey248
		{
			internal GameSession aSession;

			internal string gameName;

			internal GameManager _003C_003Ef__this;

			internal void _003C_003Em__4AC(object result)
			{
				IUpdateGameStateMessageResult updateGameStateMessageResult = result as IUpdateGameStateMessageResult;
				if (updateGameStateMessageResult.Success && aSession != null)
				{
					string aGameAction = "respond_" + gameName;
					aSession.LogAction(aGameAction);
					if (aSession.MessageData != null)
					{
						string empty = string.Empty;
						GameStateUpdatedEventArgs e = new GameStateUpdatedEventArgs(aGameDataJson: (!(aSession.MessageData is IGameEventMessageData)) ? (((IGameStateMessageData)aSession.MessageData).State["GameData"] as string) : (((IGameEventMessageData)aSession.MessageData).State["GameData"] as string), aGameStateId: aSession.MessageData.Id);
						_003C_003Ef__this.OnGameStateUpdated(_003C_003Ef__this, e);
					}
				}
			}
		}

		private sealed class PostGameStateData_003Ec__AnonStorey249
		{
			internal MixGameData mixGameData;

			internal GameSession aSession;

			internal void _003C_003Em__4AD(object sendResult)
			{
				ISendGameStateMessageResult sendGameStateMessageResult = sendResult as ISendGameStateMessageResult;
				if (sendGameStateMessageResult.Success)
				{
					Analytics.LogGamePost(aSession.Entitlement.GetName(), mixGameData, aSession.Thread as IChatThread);
				}
			}
		}

		private const string TOKEN_ERROR_GAME_NEEDS_CHAT_BODY = "customtokens.chat.error_game_needs_chat_body";

		private const string TOKEN_ERROR_GAME_NEEDS_CHAT_TITLE = "customtokens.chat.error_game_needs_chat_title";

		private List<GameSession> mSessions;

		private IGameTray mListener;

		private SdkActions actionGenerator = new SdkActions();

		private SdkEvents eventGenerator = new SdkEvents();

		private List<GameSessionData> _sessionData;

		string IGamePlayerInfo.Id
		{
			get
			{
				return MixSession.User.Id;
			}
		}

		string IGamePlayerInfo.DisplayName
		{
			get
			{
				return MixSession.User.DisplayName.Text;
			}
		}

		int IGameStatistics.SessionsCompleted
		{
			get
			{
				return GetGameSessionData().CompletedGames;
			}
			set
			{
				GetGameSessionData().CompletedGames = value;
			}
		}

		int IGameStatistics.SessionsStarted
		{
			get
			{
				return GetGameSessionData().StartedGames;
			}
			set
			{
				GetGameSessionData().StartedGames = value;
			}
		}

		DateTime IGameStatistics.LastPlayed
		{
			get
			{
				return GetGameSessionData().LastPlayed;
			}
			set
			{
				GetGameSessionData().LastPlayed = value;
			}
		}

		public GameSession ActiveSession
		{
			get
			{
				if (mSessions != null && mSessions.Count > 0)
				{
					return mSessions[mSessions.Count - 1];
				}
				return null;
			}
		}

		public bool IsSessionsPaused
		{
			get
			{
				bool result = false;
				if (mSessions != null && ActiveSession != null && mSessions.Count > 0)
				{
					result = ActiveSession.State == GameSessionState.PAUSED;
				}
				return result;
			}
		}

		public int NumberOfSessions
		{
			get
			{
				return (mSessions != null) ? mSessions.Count : 0;
			}
		}

		public bool HasSession
		{
			get
			{
				return mSessions != null && mSessions.Count > 0;
			}
		}

		public bool IsToastPanelActive { get; set; }

		protected List<GameSessionData> mSessionData
		{
			get
			{
				if (_sessionData == null)
				{
					KeyValDocumentCollectionApi keyValDocumentCollectionApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
					string text = keyValDocumentCollectionApi.LoadDeviceValue("GameSessionData");
					if (text != null)
					{
						_sessionData = JsonMapper.ToObject<List<GameSessionData>>(text);
					}
					else
					{
						_sessionData = new List<GameSessionData>();
					}
				}
				return _sessionData;
			}
		}

		public event EventHandler<EventArgs> OnUnfriended;

		public event EventHandler<GameStartedEventArgs> OnGameStarted;

		public event EventHandler<GameClosedEventArgs> OnGameClosed;

		public event EventHandler<GameStateUpdatedEventArgs> OnGameStateUpdated;

		void IBundleObject.OnBundleAssetObject(UnityEngine.Object aGameObject, object aUserData)
		{
			Hashtable hashtable = aUserData as Hashtable;
			string aUrl = hashtable["BundleUrl"].ToString();
			int referenceCount = MonoSingleton<AssetManager>.Instance.GetReferenceCount(aUrl);
			if (referenceCount > -1 && hashtable != null)
			{
				if (hashtable.ContainsKey("SessionAsset"))
				{
					OnGameSessionAssetLoaded(aUserData);
				}
				else
				{
					OnGameLoaded(aUserData);
				}
			}
		}

		void IGameSession.HandleSessionState(GameSession aSession, GameSessionState aSessionState)
		{
			if (!(aSession is ChatThreadGameSession))
			{
				SetSessionState(aSessionState, aSession);
			}
		}

		void IGameSession.LoadGame(ChatThreadGameSession chatSession, IGameTray gameTray)
		{
			IEntitlementGameData entitlement = chatSession.Entitlement;
			IChatThread aThread = chatSession.ThreadParameters.Thread as IChatThread;
			IGameMessageData messageData = chatSession.MessageData;
			LoadGame(entitlement, aThread, gameTray, messageData);
		}

		void IGameSession.PostGameStateData(GameSession aSession, object stateData)
		{
			PostGameStateData_003Ec__AnonStorey249 CS_0024_003C_003E8__locals6 = new PostGameStateData_003Ec__AnonStorey249();
			CS_0024_003C_003E8__locals6.aSession = aSession;
			if (!(stateData is MixGameData))
			{
				return;
			}
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_3_MediaMessage");
			CS_0024_003C_003E8__locals6.mixGameData = stateData as MixGameData;
			string value = JsonMapper.ToJson(stateData);
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary.Add("GameData", value);
			mListener.SendGameStateMessage(CS_0024_003C_003E8__locals6.mixGameData.GameProcessor, dictionary, delegate(object sendResult)
			{
				ISendGameStateMessageResult sendGameStateMessageResult = sendResult as ISendGameStateMessageResult;
				if (sendGameStateMessageResult.Success)
				{
					Analytics.LogGamePost(CS_0024_003C_003E8__locals6.aSession.Entitlement.GetName(), CS_0024_003C_003E8__locals6.mixGameData, CS_0024_003C_003E8__locals6.aSession.Thread as IChatThread);
				}
			});
		}

		void IGameSession.PostGameEventData(GameSession aSession, MixGameData stateData, MixGameResponse eventData)
		{
			PostGameEventData_003Ec__AnonStorey248 CS_0024_003C_003E8__locals20 = new PostGameEventData_003Ec__AnonStorey248();
			CS_0024_003C_003E8__locals20.aSession = aSession;
			CS_0024_003C_003E8__locals20._003C_003Ef__this = this;
			if (!(eventData is MixGameResponse) || mListener == null || CS_0024_003C_003E8__locals20.aSession == null)
			{
				return;
			}
			string value = JsonMapper.ToJson(eventData);
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary.Add("GameData", value);
			CS_0024_003C_003E8__locals20.gameName = string.Empty;
			if (CS_0024_003C_003E8__locals20.aSession.MessageData is IGameEventMessageData)
			{
				CS_0024_003C_003E8__locals20.gameName = (CS_0024_003C_003E8__locals20.aSession.MessageData as IGameEventMessageData).GameName;
			}
			else
			{
				CS_0024_003C_003E8__locals20.gameName = (CS_0024_003C_003E8__locals20.aSession.MessageData as IGameStateMessageData).GameName;
			}
			mListener.UpdateGameStateMessage(CS_0024_003C_003E8__locals20.aSession.MessageData, dictionary, delegate(object result)
			{
				IUpdateGameStateMessageResult updateGameStateMessageResult = result as IUpdateGameStateMessageResult;
				if (updateGameStateMessageResult.Success && CS_0024_003C_003E8__locals20.aSession != null)
				{
					string aGameAction = "respond_" + CS_0024_003C_003E8__locals20.gameName;
					CS_0024_003C_003E8__locals20.aSession.LogAction(aGameAction);
					if (CS_0024_003C_003E8__locals20.aSession.MessageData != null)
					{
						string empty = string.Empty;
						GameStateUpdatedEventArgs e = new GameStateUpdatedEventArgs(aGameDataJson: (!(CS_0024_003C_003E8__locals20.aSession.MessageData is IGameEventMessageData)) ? (((IGameStateMessageData)CS_0024_003C_003E8__locals20.aSession.MessageData).State["GameData"] as string) : (((IGameEventMessageData)CS_0024_003C_003E8__locals20.aSession.MessageData).State["GameData"] as string), aGameStateId: CS_0024_003C_003E8__locals20.aSession.MessageData.Id);
						CS_0024_003C_003E8__locals20._003C_003Ef__this.OnGameStateUpdated(CS_0024_003C_003E8__locals20._003C_003Ef__this, e);
					}
				}
			});
		}

		void IGameSession.PostChat(IEntitlementGameData aEntitlement)
		{
			mListener.SendEntitlement(aEntitlement);
		}

		void IGameAssetManager.CancelBundles()
		{
			if (MonoSingleton<AssetManager>.Instance != null)
			{
				MonoSingleton<AssetManager>.Instance.CancelBundles(this);
			}
		}

		void IGameAssetManager.CancelBundles(string aUrl)
		{
			if (MonoSingleton<AssetManager>.Instance != null)
			{
				MonoSingleton<AssetManager>.Instance.CancelBundles(aUrl);
			}
		}

		void IGameAssetManager.DestroyBundleInstance(string aPath, UnityEngine.Object aObject)
		{
			if (aObject != null && MonoSingleton<AssetManager>.Instance != null)
			{
				MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(aPath, aObject);
			}
		}

		UnityEngine.Object IGameAssetManager.GetBundleInstance(string aPath)
		{
			return MonoSingleton<AssetManager>.Instance.GetBundleInstance(aPath);
		}

		void IGameAssetManager.LoadAsset(IGameAsset aSessionAsset, string aPath, object aParam)
		{
			Hashtable hashtable = new Hashtable();
			hashtable["SessionAsset"] = aSessionAsset;
			if (aParam != null)
			{
				hashtable["Parameter"] = aParam;
			}
			hashtable["BundleUrl"] = aPath;
			if (!(this == null) && hashtable != null && hashtable["BundleUrl"] != null)
			{
				MonoSingleton<AssetManager>.Instance.LoadABundle(this, hashtable["BundleUrl"].ToString(), hashtable, string.Empty, false, false, true);
			}
		}

		void IGameAssetManager.LoadData(GameSession aSession, string aPath, string aFileName, Func<string, object> aMethod)
		{
			Hashtable hashtable = new Hashtable();
			hashtable["Session"] = aSession;
			LoadParams aLoadParams = new LoadParams(AssetManager.GetShaString(aPath), aPath, CachePolicy.CacheThenBundleThenDownload);
			MonoSingleton<AssetManager>.Instance.LoadJsonFromZip(aMethod, aPath, aFileName, this, aLoadParams, hashtable);
		}

		bool IGameAssetManager.WillBundleLoadFromWeb(string aPath)
		{
			return MonoSingleton<AssetManager>.Instance.WillBundleLoadFromWeb(aPath);
		}

		string IGameLocalization.GetLocalizedContent(string aToken)
		{
			return Singleton<Localizer>.Instance.getString(aToken);
		}

		string IGamePlayerInfo.GetFriendDisplayName(string id)
		{
			GetFriendDisplayName_003Ec__AnonStorey247 CS_0024_003C_003E8__locals4 = new GetFriendDisplayName_003Ec__AnonStorey247();
			CS_0024_003C_003E8__locals4.id = id;
			string result = string.Empty;
			if (!MixSession.IsValidSession || this.IsNullOrDisposed())
			{
				return result;
			}
			IFriend friend = MixChat.FindFriend(CS_0024_003C_003E8__locals4.id);
			if (friend == null && ActiveSession != null && ActiveSession.ThreadParameters != null && ActiveSession.ThreadParameters.Members != null)
			{
				IEnumerable<string> members = ActiveSession.ThreadParameters.Members;
				IFriend friend2 = MixChat.FindFriend(members.FirstOrDefault((string memberId) => memberId == CS_0024_003C_003E8__locals4.id));
				if (friend2 != null)
				{
					result = friend2.DisplayName.Text;
				}
				else if (ActiveSession.ThreadParameters.FormerMembers != null)
				{
					IEnumerable<string> formerMembers = ActiveSession.ThreadParameters.FormerMembers;
					IFriend friend3 = MixChat.FindFriend(formerMembers.FirstOrDefault((string memberId) => memberId == CS_0024_003C_003E8__locals4.id));
					if (friend3 != null)
					{
						result = friend3.DisplayName.Text;
					}
				}
			}
			else
			{
				result = friend.DisplayName.Text;
			}
			return result;
		}

		string IGamePlayerInfo.GetFriendDisplayName(IGameThreadParameters threadParams)
		{
			string result = string.Empty;
			if (threadParams.Thread is IOneOnOneChatThread)
			{
				IRemoteChatMember otherUser = (threadParams.Thread as IOneOnOneChatThread).GetOtherUser();
				if (otherUser != null)
				{
					result = otherUser.DisplayName.Text;
				}
			}
			return result;
		}

		int IGameSessionSettings.GetScreenHeight()
		{
			return Singleton<SettingsManager>.Instance.GetScreenHeight();
		}

		int IGameSessionSettings.GetScreenWidth()
		{
			return Singleton<SettingsManager>.Instance.GetScreenWidth();
		}

		int IGameSessionSettings.GetStatusBarHeight()
		{
            return 0;
        }

        float IGameSessionSettings.GetHeightScale()
		{
			return Singleton<SettingsManager>.Instance.GetHeightScale();
		}

		float IGameSessionSettings.GetWidthScale()
		{
			return Singleton<SettingsManager>.Instance.GetWidthScale();
		}

		void IGameSounds.PauseSoundEvent(string eventName, GameObject gameObject)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.PauseSoundEvent(eventName, gameObject);
			}
		}

		void IGameSounds.PlayMusic(string eventName, GameObject gameObject)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.PlayMusic(eventName, gameObject);
			}
		}

		void IGameSounds.PlaySoundEvent(string eventName)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.PlaySoundEvent(eventName);
			}
		}

		void IGameSounds.PlaySoundEvent(string eventName, GameObject gameObject)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.PlaySoundEvent(eventName, gameObject);
			}
		}

		void IGameSounds.PlaySoundEvent(string eventName, object parameter)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.PlaySoundEvent(eventName, parameter);
			}
		}

		void IGameSounds.StopSoundEvent(string eventName)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.StopSoundEvent(eventName);
			}
		}

		void IGameSounds.StopSoundEvent(string eventName, GameObject gameObject)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.StopSoundEvent(eventName, gameObject);
			}
		}

		void IGameSounds.UnpauseSoundEvent(string eventName, GameObject gameObject)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.UnpauseSoundEvent(eventName, gameObject);
			}
		}

		void IGameSounds.SetVolumeEvent(string eventName, float volume)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.SetVolumeEvent(eventName, volume);
			}
		}

		void IGameSounds.SetSwitchEvent(string eventName, string switchEventName, GameObject gameObject)
		{
			if (Singleton<SoundManager>.Instance != null)
			{
				Singleton<SoundManager>.Instance.SetSwitchEvent(eventName, switchEventName, gameObject);
			}
		}

		void IGameAvatar.PreloadAvatar(string id, IGameThreadParameters threadParams)
		{
			IAvatar avatarFromSwid = GetAvatarFromSwid(id, threadParams);
			if (AvatarApi.ValidateAvatar(avatarFromSwid))
			{
				MonoSingleton<AvatarManager>.Instance.PreloadAvatar(avatarFromSwid, (AvatarFlags)0);
			}
		}

		void IGameAvatar.LoadFriend(GameObject aMesh, string id, IGameThreadParameters threadParams, bool aHideCostumes, bool aHideGeoAccessories)
		{
			IAvatar avatarFromSwid = GetAvatarFromSwid(id, threadParams);
			LoadAvatar(avatarFromSwid, aMesh, aHideCostumes, aHideGeoAccessories);
		}

		void IGameAvatar.LoadSnapshot(string id, IGameThreadParameters threadParams, int spriteSize, Action<bool, Sprite> completionCallback)
		{
			LoadSnapshot_003Ec__AnonStorey246 CS_0024_003C_003E8__locals2 = new LoadSnapshot_003Ec__AnonStorey246();
			CS_0024_003C_003E8__locals2.completionCallback = completionCallback;
			IAvatar avatarFromSwid = GetAvatarFromSwid(id, threadParams);
			if (AvatarApi.ValidateAvatar(avatarFromSwid))
			{
				MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(avatarFromSwid, (AvatarFlags)0, spriteSize, delegate(bool success, Sprite sprite)
				{
					CS_0024_003C_003E8__locals2.completionCallback(success, sprite);
				});
			}
		}

		void IGameAvatar.LoadRandomFriend(GameObject aMesh)
		{
			IAvatar avatar = MonoSingleton<AvatarManager>.Instance.GenerateRandomDna();
			LoadAvatar(avatar, aMesh, true, false);
		}

		void IGameInput.HideKeyboard()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
		}

		bool IGameInput.IsToastPanelActive()
		{
			return IsToastPanelActive;
		}

		void IGameAnalytics.LogEvent(GameLogEventType aGameLogEventType, Hashtable aData)
		{
			switch (aGameLogEventType)
			{
			case GameLogEventType.ACTION:
			{
				IChatThread thread = aData["thread"] as IChatThread;
				Analytics.LogGameAction("chat_game", (string)aData["action"], (!thread.TrustLevel.AllMembersTrustEachOther) ? "safe" : "open", (string)aData["message"], thread.GetAnalyticsId());
				break;
			}
			case GameLogEventType.PAGEVIEW:
				Analytics.LogPageView((string)aData["location"]);
				break;
			case GameLogEventType.TIMING:
			{
				IChatThread thread = aData["thread"] as IChatThread;
				Analytics.LogTimingAction("chat_game", thread.GetAnalyticsId(), (string)aData["elapsed_time"], (string)aData["path_name"]);
				break;
			}
			}
		}

		DateTime IGameStatistics.GetLastPlayed(string id)
		{
			return GetGameSessionData(id).LastPlayed;
		}

		void IGameModeration.ModerateTextMessage(object aThread, string aTextToModerate, IGameModerationResult aGame, IEntitlementGameData aEntitlement, object aUserData)
		{
			ModerateTextMessage_003Ec__AnonStorey245 CS_0024_003C_003E8__locals12 = new ModerateTextMessage_003Ec__AnonStorey245();
			CS_0024_003C_003E8__locals12.aGame = aGame;
			CS_0024_003C_003E8__locals12.aEntitlement = aEntitlement;
			CS_0024_003C_003E8__locals12.aUserData = aUserData;
			CS_0024_003C_003E8__locals12._003C_003Ef__this = this;
			IChatThread chatThread = aThread as IChatThread;
			chatThread.ModerateTextMessage(aTextToModerate, actionGenerator.CreateAction(delegate(ITextModerationResult result)
			{
				if (CS_0024_003C_003E8__locals12.aGame != null)
				{
					if (result.Success)
					{
						if (result.IsModerated && CS_0024_003C_003E8__locals12._003C_003Ef__this.ActiveSession != null)
						{
							CS_0024_003C_003E8__locals12._003C_003Ef__this.ActiveSession.LogAction(CS_0024_003C_003E8__locals12.aEntitlement.GetName() + "_text_moderated");
						}
						CS_0024_003C_003E8__locals12.aGame.OnModerationResult(result.IsModerated, result.ModeratedText, CS_0024_003C_003E8__locals12.aUserData);
					}
					else
					{
						CS_0024_003C_003E8__locals12.aGame.OnModerationError(CS_0024_003C_003E8__locals12.aUserData);
					}
				}
			}));
		}

		void IZipJsonAssetObject.OnZipJsonAssetObject(object aObject, object aUserData)
		{
			Hashtable hashtable = aUserData as Hashtable;
			GameSession gameSession = hashtable["Session"] as GameSession;
			gameSession.OnDataLoaded(aObject, aUserData);
		}

		public int GetGameHeight(GameSession aSession)
		{
			return (int)((float)Singleton<SettingsManager>.Instance.GetScreenHeight() * aSession.GameCamera.rect.height * Singleton<SettingsManager>.Instance.GetHeightScale());
		}

		public void SetGameExitHeight(float aTrayHeight)
		{
		}

		protected void Awake()
		{
			MonoSingleton<LifecycleEventDispatcher>.Instance.OnApplicationPauseEvents += OnApplicationPause;
			MonoSingleton<ConnectionManager>.Instance.ConnectedEvent += OnConnected;
			MixSession.OnConnectionChanged += HandleConnectionChanged;
			EnvironmentManager.FocusChangedEvent += OnFocusChanged;
			MonoSingleton<NativeUtilitiesManager>.Instance.Native.OnNativeUtilitiesHeadPhoneJackChanged += OnHeadPhoneJackChanged;
		}

		public void Init()
		{
			mSessions = new List<GameSession>();
			ChatController.OnChatThreadEnter = (EventHandler<ChatThreadEnterEventArgs>)Delegate.Combine(ChatController.OnChatThreadEnter, new EventHandler<ChatThreadEnterEventArgs>(OnChatThreadEnter));
			ChatController.OnChatThreadExit = (EventHandler<ChatThreadExitEventArgs>)Delegate.Combine(ChatController.OnChatThreadExit, new EventHandler<ChatThreadExitEventArgs>(OnChatThreadExit));
		}

		private void HandleConnectionChanged(MixSession.ConnectionState newState, MixSession.ConnectionState oldState)
		{
			if (newState == MixSession.ConnectionState.ONLINE)
			{
				MixSession.User.OnUnfriended += eventGenerator.AddEventHandler<AbstractUnfriendedEventArgs>(this, OnUnfriending);
			}
		}

		private void OnUnfriending(object sender, AbstractUnfriendedEventArgs args)
		{
			if (this != null && ActiveSession != null && ActiveSession.ThreadParameters != null && !ActiveSession.ThreadParameters.IsGroupSession && ActiveSession.ThreadParameters.Thread is IOneOnOneChatThread)
			{
				IOneOnOneChatThread thread = ActiveSession.ThreadParameters.Thread as IOneOnOneChatThread;
				if (!thread.IsOtherUserFriend())
				{
					this.OnUnfriended(this, new EventArgs());
				}
			}
		}

		private void createGameSession(Hashtable data)
		{
			IEntitlementGameData entitlementGameData = data["Entitlement"] as IEntitlementGameData;
			IChatThread chatThread = data["ChatThread"] as IChatThread;
			bool flag = data.ContainsKey("GameMessage");
			if (chatThread != null && entitlementGameData != null)
			{
				GameSessionThreadParameters aThreadParameters = new GameSessionThreadParameters(chatThread);
				GameSession gameSession = new GameSession(this, aThreadParameters, entitlementGameData, flag);
				if (flag)
				{
					IGameMessageData messageData = data["GameMessage"] as IGameMessageData;
					gameSession.MessageData = messageData;
				}
				mSessions.Add(gameSession);
				SetSessionState(GameSessionState.STARTING);
			}
		}

		public void LoadGame(IEntitlementGameData aGame, IChatThread aThread, IGameTray aListener, IGameMessageData gameMessageData = null)
		{
			Hashtable hashtable = new Hashtable();
			hashtable["ChatThread"] = aThread;
			hashtable["Entitlement"] = aGame;
			if (gameMessageData != null)
			{
				hashtable["GameMessage"] = gameMessageData;
			}
			if (ActiveSession == null)
			{
				createGameSession(hashtable);
			}
			else
			{
				if (ActiveSession.State == GameSessionState.LOADING)
				{
					return;
				}
				if (string.Equals(ActiveSession.Entitlement.GetUid(), aGame.GetUid()))
				{
					string text = null;
					string text2 = null;
					if (ActiveSession.MessageData != null)
					{
						if (ActiveSession.MessageData is IGameStateMessageData)
						{
							if (ActiveSession.MessageData.Id != null)
							{
								text = ActiveSession.MessageData.Id;
							}
						}
						else if ((ActiveSession.MessageData as IGameEventMessageData).StateMessageId != null)
						{
							text = (ActiveSession.MessageData as IGameEventMessageData).StateMessageId;
						}
					}
					if (gameMessageData != null)
					{
						if (gameMessageData is IGameStateMessageData)
						{
							if (gameMessageData.Id != null)
							{
								text2 = gameMessageData.Id;
							}
						}
						else if ((gameMessageData as IGameEventMessageData).StateMessageId != null)
						{
							text2 = (gameMessageData as IGameEventMessageData).StateMessageId;
						}
					}
					if (text2 == null || text == null)
					{
						if (ActiveSession.State == GameSessionState.PAUSED)
						{
							if (!CheckPanelOpen())
							{
								ResumeGameSession();
							}
						}
						else if (ActiveSession.State != GameSessionState.ERROR)
						{
						}
						return;
					}
					if (string.Equals(text2, text))
					{
						if (ActiveSession.State == GameSessionState.PAUSED)
						{
							ResumeGameSession();
						}
						else if (ActiveSession.State != GameSessionState.ERROR)
						{
						}
						return;
					}
					SetSessionState(GameSessionState.INCOMPLETE);
					createGameSession(hashtable);
				}
				else
				{
					SetSessionState(GameSessionState.INCOMPLETE);
					createGameSession(hashtable);
				}
			}
			mListener.OnGamePause(null);
			mListener.SetGamePanel(GameTrayState.GAME);
			mListener.UpdateGameLoader(aGame.GetLogo());
			ActiveSession.State = GameSessionState.LOADING;
			hashtable["BundleUrl"] = aGame.GetHd();
			Debug.Log("Loading bundle: " + aGame.GetHd());
			MonoSingleton<AssetManager>.Instance.LoadABundle(this, hashtable["BundleUrl"].ToString(), hashtable, string.Empty);
			Analytics.LogEnterGame(aGame.GetName(), aThread);
			if (ActiveSession != null && ActiveSession.MessageData != null)
			{
				GameStartedEventArgs e = new GameStartedEventArgs(ActiveSession.MessageData.Id);
				this.OnGameStarted(this, e);
			}
		}

		public GameSessionState GetSessionState()
		{
			GameSessionState result = GameSessionState.NOT_PLAYING;
			if (ActiveSession != null)
			{
				result = ActiveSession.State;
			}
			return result;
		}

		public void CloseGameSession(GameSession aSession = null)
		{
			IsToastPanelActive = false;
			showStatusbar(true);
			Singleton<ThreadFramerateThrottler>.Instance.AllowThrottling();
			if (aSession != null || HasSession)
			{
				aSession = ((aSession != null) ? aSession : ActiveSession);
				aSession.PostSession();
				aSession.CleanupSession();
				mSessions.Remove(aSession);
			}
		}

		public void QuitGameSession()
		{
			if (!MonoSingleton<AssetManager>.Instance.IsNullOrDisposed())
			{
				MonoSingleton<AssetManager>.Instance.AreBlockingCallsSuppressedForGameplay = false;
				MonoSingleton<AssetManager>.Instance.CancelBundles(this);
			}
			IsToastPanelActive = false;
			if (Singleton<ThreadFramerateThrottler>.Instance != null)
			{
				Singleton<ThreadFramerateThrottler>.Instance.AllowThrottling();
			}
			if (ActiveSession != null && ActiveSession.MessageData != null)
			{
				GameClosedEventArgs e = new GameClosedEventArgs(ActiveSession.MessageData.Id);
				this.OnGameClosed(this, e);
			}
			showStatusbar(true);
			if (!HasSession)
			{
				return;
			}
			if (mSessions != null)
			{
				foreach (GameSession mSession in mSessions)
				{
					if (mSession != null)
					{
						mSession.CleanupSession();
						mSession.QuitSessionFromApp();
					}
				}
				mSessions.Clear();
			}
			if (mListener != null)
			{
				mListener.OnGamePause(null);
				mListener.SetGamePanel(GameTrayState.NONE);
			}
		}

		public bool PauseGameSession()
		{
			MonoSingleton<AssetManager>.Instance.AreBlockingCallsSuppressedForGameplay = false;
			bool result = false;
			if (ActiveSession != null && (ActiveSession.State == GameSessionState.PLAYING || ActiveSession.State == GameSessionState.STARTING || ActiveSession.State == GameSessionState.LOADING))
			{
				Singleton<ThreadFramerateThrottler>.Instance.AllowThrottling();
				ActiveSession.State = GameSessionState.PAUSED;
				SetSessionState(GameSessionState.PAUSED);
				result = true;
			}
			return result;
		}

		public void ResumeGameSession()
		{
			MonoSingleton<AssetManager>.Instance.AreBlockingCallsSuppressedForGameplay = true;
			if (HasSession && !CheckPanelOpen())
			{
				showStatusbar(false);
				((IGameInput)this).HideKeyboard();
				mListener.OnGamePause(null);
				mListener.SetGamePanel(GameTrayState.GAME);
				Singleton<ThreadFramerateThrottler>.Instance.PreventThrottling();
				if (ActiveSession.IsGameLoaded)
				{
					ActiveSession.State = GameSessionState.PLAYING;
					ActiveSession.ResumeSession();
					mListener.UpdateGameHeight(GetGameHeight(ActiveSession));
				}
				else
				{
					ActiveSession.State = GameSessionState.LOADING;
					mListener.UpdateGameLoader(ActiveSession.Entitlement.GetLogo());
				}
			}
		}

		private void OnConnected()
		{
			foreach (GameSession mSession in mSessions)
			{
				if (mSession.State == GameSessionState.ERROR)
				{
					mSession.ResumeNetworkError();
					break;
				}
			}
		}

		public bool SetSessionState(GameSessionState aState, GameSession aSession = null)
		{
			if (!HasSession)
			{
				return false;
			}
			switch (aState)
			{
			case GameSessionState.STARTING:
				Singleton<TechAnalytics>.Instance.TrackTimeToLoadGameEnd();
				showStatusbar(false);
				break;
			case GameSessionState.INCOMPLETE:
				MonoSingleton<AssetManager>.Instance.AreBlockingCallsSuppressedForGameplay = false;
				if (ActiveSession != null && ActiveSession.MessageData != null)
				{
					string empty = string.Empty;
					empty = ((!(ActiveSession.MessageData is IGameEventMessageData)) ? ActiveSession.MessageData.Id : (ActiveSession.MessageData as IGameEventMessageData).StateMessageId);
					GameClosedEventArgs e = new GameClosedEventArgs(empty);
					this.OnGameClosed(this, e);
				}
				mListener.SetGamePanel(GameTrayState.NONE);
				CloseGameSession();
				break;
			case GameSessionState.COMPLETE:
				mListener.SetGamePanel(GameTrayState.NONE);
				CloseGameSession();
				break;
			case GameSessionState.PAUSED:
			{
				showStatusbar(true);
				List<GameSession> list = new List<GameSession>();
				foreach (GameSession mSession in mSessions)
				{
					if (mSession.ShouldQuitGameOnPause && mSession.State != GameSessionState.ERROR)
					{
						mListener.OnGamePause(null);
						mListener.SetGamePanel(GameTrayState.NONE);
						list.Add(mSession);
					}
					else
					{
						mSession.PauseSession();
						string pauseImage2 = mSessions[mSessions.Count - 1].Entitlement.GetPauseImage();
						mListener.OnGamePause(pauseImage2);
						mListener.SetGamePanel(GameTrayState.PAUSE);
					}
				}
				foreach (GameSession item in list)
				{
					item.CleanupSession();
					item.QuitSessionFromApp();
					mSessions.Remove(item);
				}
				break;
			}
			case GameSessionState.ERROR:
				showStatusbar(true);
				foreach (GameSession mSession2 in mSessions)
				{
					mSession2.NetworkErrorPauseSession();
					mSession2.UpdateGameSessionState(aState);
					string pauseImage = mSessions[mSessions.Count - 1].Entitlement.GetPauseImage();
					mListener.OnGameError(pauseImage);
					mListener.SetGamePanel(GameTrayState.ERROR);
				}
				break;
			}
			return true;
		}

		private void showStatusbar(bool shouldShowStatusBar)
		{
			EnvironmentManager.ShowStatusBar(shouldShowStatusBar);
		}

		protected IAvatar GetAvatarFromSwid(string aPlayerId, IGameThreadParameters threadParams)
		{
			IEnumerable<string> members = threadParams.Members;
			IEnumerable<string> formerMembers = threadParams.FormerMembers;
			if (string.Equals(MixSession.User.Id, aPlayerId))
			{
				return MixSession.User.Avatar;
			}
			IFriend friend = MixChat.FindFriend(aPlayerId);
			if (friend == null)
			{
				IFriend friend2 = MixChat.FindFriend(members.FirstOrDefault((string memberId) => memberId == aPlayerId));
				if (friend2 == null)
				{
					friend2 = MixChat.FindFriend(formerMembers.FirstOrDefault((string memberId) => memberId == aPlayerId));
					if (friend2 == null && ActiveSession.Thread is IGroupChatThread)
					{
						IGroupChatThread thread = ActiveSession.Thread as IGroupChatThread;
						IRemoteChatMember remoteMemberById = thread.GetRemoteMemberById(aPlayerId);
						IAvatar result;
						if (remoteMemberById == null)
						{
							IAvatar avatar = null;
							result = avatar;
						}
						else
						{
							result = remoteMemberById.Avatar;
						}
						return result;
					}
				}
				IAvatar result2;
				if (friend2 == null)
				{
					IAvatar avatar = null;
					result2 = avatar;
				}
				else
				{
					result2 = friend2.Avatar;
				}
				return result2;
			}
			return friend.Avatar;
		}

		protected GameSessionData GetGameSessionData(string gameId = null)
		{
			string threadId = string.Empty;
			if (gameId == null)
			{
				if (ActiveSession != null && ActiveSession.Entitlement != null)
				{
					gameId = ActiveSession.Entitlement.GetUid();
				}
				threadId = ActiveSession.ThreadId;
			}
			else if (mListener != null)
			{
				threadId = mListener.GetCurrentThreadId();
			}
			string playerId = MixSession.User.Id;
			if (mSessionData != null)
			{
				GameSessionData gameSessionData = mSessionData.FirstOrDefault((GameSessionData game) => game.IsMine(gameId, threadId, playerId));
				if (gameSessionData == null)
				{
					gameSessionData = new GameSessionData(gameId, threadId, playerId);
					mSessionData.Add(gameSessionData);
				}
				return gameSessionData;
			}
			return null;
		}

		public void OnApplicationPause(bool aPause)
		{
			QuitGameSession();
			KeyValDocumentCollectionApi keyValDocumentCollectionApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
			string value = JsonMapper.ToJson(mSessionData);
			keyValDocumentCollectionApi.SaveDeviceValue("GameSessionData", value);
		}

		public void OnFocusChanged(bool aFocusState)
		{
			if (!aFocusState)
			{
				PauseGameSession();
			}
		}

		protected bool CheckPanelOpen()
		{
			bool flag = false;
			bool flag2 = false;
			BasePanel basePanel = Singleton<PanelManager>.Instance.FindPanel(typeof(ReportAPlayerPanel));
			BasePanel basePanel2 = Singleton<PanelManager>.Instance.FindPanel(typeof(GenericPanel));
			if (basePanel != null)
			{
				flag = basePanel.gameObject.activeSelf;
			}
			if (basePanel2 != null)
			{
				flag2 = basePanel2.gameObject.activeSelf;
			}
			return flag || flag2;
		}

		protected void OnGameLoaded(object aUserData)
		{
			Hashtable hashtable = aUserData as Hashtable;
			IChatThread chatThread = hashtable["ChatThread"] as IChatThread;
			IEntitlementGameData entitlementGameData = hashtable["Entitlement"] as IEntitlementGameData;
			if (chatThread == null || entitlementGameData == null || ActiveSession == null)
			{
				return;
			}
			GameObject gameObject = (GameObject)((IGameAssetManager)this).GetBundleInstance(ActiveSession.Entitlement.GetHd());
			if (gameObject != null)
			{
				ActiveSession.IsGameLoaded = true;
				ActiveSession.Initialize(gameObject);
				if (CheckPanelOpen())
				{
					ActiveSession.State = GameSessionState.PAUSED;
					SetSessionState(GameSessionState.PAUSED);
				}
				if (ActiveSession.State != GameSessionState.PAUSED)
				{
					GameSession activeSession = ActiveSession;
					mListener.UpdateGameHeight(GetGameHeight(activeSession));
					mListener.UpdateGameLoader(null);
					Singleton<ThreadFramerateThrottler>.Instance.PreventThrottling();
					MonoSingleton<AssetManager>.Instance.AreBlockingCallsSuppressedForGameplay = true;
					activeSession.StartSession();
				}
				else
				{
					ActiveSession.PauseSession();
				}
			}
		}

		protected void OnGameSessionAssetLoaded(object aUserData)
		{
			Hashtable hashtable = aUserData as Hashtable;
			IGameAsset gameAsset = hashtable["SessionAsset"] as IGameAsset;
			gameAsset.OnGameSessionAssetLoaded(aUserData);
		}

		private void OnHeadPhoneJackChanged(object sender, NativeUtilitiesHeadPhoneJackChangedEventArgs e)
		{
			if (string.Equals(e.HeadPhoneJackAction, "headPhoneJackRemoved"))
			{
				PauseGameSession();
			}
		}

		protected void OnChatThreadEnter(object sender, ChatThreadEnterEventArgs e)
		{
			mListener = e.GameTray;
		}

		protected void OnChatThreadExit(object sender, ChatThreadExitEventArgs e)
		{
			mListener = null;
		}

		private void LoadAvatar(IAvatar avatar, GameObject aMesh, bool aHideCostume, bool aHideGeoAccessories)
		{
			if (AvatarApi.ValidateAvatar(avatar))
			{
				AvatarFlags avatarFlags = (AvatarFlags)0;
				if (aHideCostume)
				{
					avatarFlags |= AvatarFlags.WithoutCostume;
				}
				if (aHideGeoAccessories)
				{
					avatarFlags |= AvatarFlags.WithoutGeo;
				}
				MonoSingleton<AvatarManager>.Instance.SkinAvatar(aMesh, avatar, avatarFlags, null);
			}
		}

		public void Log(object message)
		{
		}

		public void Log(object message, UnityEngine.Object context)
		{
		}

		public void LogError(object message)
		{
		}

		public void LogError(object message, UnityEngine.Object context)
		{
		}

		public void LogWarning(object message)
		{
		}

		public void LogWarning(object message, UnityEngine.Object context)
		{
		}
	}
}
