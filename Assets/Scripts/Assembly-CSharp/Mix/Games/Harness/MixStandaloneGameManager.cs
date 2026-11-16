using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avatar;
using Disney.Mix.SDK;
using LitJson;
using Mix.Avatar;
using Mix.DeviceDb;
using Mix.Games.Avatar;
using Mix.Games.Data;
using Mix.Games.Localization;
using Mix.Games.Message;
using Mix.Games.Session;
using Mix.Session;
using Mix.Ui;
using Mix.Ui.Events;
using UnityEngine;

namespace Mix.Games.Harness
{
	public class MixStandaloneGameManager : MonoSingleton<MixStandaloneGameManager>, IGameAssetManager, IGameAnalytics, IGameAvatar, IGameInput, IGameLocalization, IGamePlayerInfo, IGameSounds, IGameStatistics, IGameModeration, IGameSession, IGameSessionSettings
	{
		private sealed class ModerateTextMessage_003Ec__AnonStorey24C
		{
			internal IGameModerationResult aGame;

			internal IEntitlementGameData aEntitlement;

			internal object aUserData;

			internal MixStandaloneGameManager _003C_003Ef__this;

			internal void _003C_003Em__4B2(ITextModerationResult result)
			{
				if (aGame == null)
				{
					return;
				}
				if (result.Success)
				{
					if (result.IsModerated)
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

		private sealed class LoadSnapshot_003Ec__AnonStorey24D
		{
			internal Action<bool, Sprite> completionCallback;

			internal void _003C_003Em__4B3(bool success, Sprite sprite)
			{
				completionCallback(success, sprite);
			}
		}

		private IGameTray mListener;

		private List<GameSession> mSessions;

		private SdkActions actionGenerator = new SdkActions();

		private List<GameSessionData> _sessionData;

		string IGamePlayerInfo.Id
		{
			get
			{
				return "1234";
			}
		}

		string IGamePlayerInfo.DisplayName
		{
			get
			{
				return "TestUser";
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

		public bool HasSession
		{
			get
			{
				return (mSessions != null && mSessions.Count > 0) ? true : false;
			}
		}

		protected List<GameSession> sessions
		{
			get
			{
				if (mSessions == null)
				{
					mSessions = new List<GameSession>();
				}
				return mSessions;
			}
		}

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

		public event EventHandler<EventArgs> OnUnfriended
		{
			add { }
			remove { }
		}

		public event EventHandler<GameStartedEventArgs> OnGameStarted
		{
			add { }
			remove { }
		}

		public event EventHandler<GameClosedEventArgs> OnGameClosed
		{
			add { }
			remove { }
		}

		public event EventHandler<GameStateUpdatedEventArgs> OnGameStateUpdated
		{
			add { }
			remove { }
		}

		void IGameSession.HandleSessionState(GameSession aSession, GameSessionState aSessionState)
		{
			switch (aSessionState)
			{
			case GameSessionState.STARTING:
				break;
			case GameSessionState.LOADING:
				break;
			case GameSessionState.INCOMPLETE:
				if (ActiveSession == null || ActiveSession.MessageData != null)
				{
				}
				mListener.SetGamePanel(GameTrayState.NONE);
				CloseGameSession(ActiveSession);
				break;
			case GameSessionState.COMPLETE:
				mListener.SetGamePanel(GameTrayState.NONE);
				CloseGameSession(ActiveSession);
				break;
			case GameSessionState.PAUSED:
			{
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
						mListener.SetGamePanel(GameTrayState.PAUSE);
					}
				}
				{
					foreach (GameSession item in list)
					{
						item.CleanupSession();
						item.QuitSessionFromApp();
						mSessions.Remove(item);
					}
					break;
				}
			}
			case GameSessionState.ERROR:
			{
				foreach (GameSession mSession2 in mSessions)
				{
					mSession2.NetworkErrorPauseSession();
					mSession2.UpdateGameSessionState(aSessionState);
					mListener.SetGamePanel(GameTrayState.ERROR);
				}
				break;
			}
			case GameSessionState.CLOSED:
			case GameSessionState.PLAYING:
				break;
			}
		}

		void IGameSession.LoadGame(ChatThreadGameSession threadSession, IGameTray gameTray)
		{
			IEntitlementGameData entitlement = threadSession.Entitlement;
			IGameMessageData messageData = threadSession.MessageData;
			LoadGame(entitlement, gameTray, messageData);
		}

		void IGameSession.PostGameStateData(GameSession aSession, object aGameStateData)
		{
			if (aGameStateData is MixGameData)
			{
				MixGameData mixGameData = aGameStateData as MixGameData;
				string value = JsonMapper.ToJson(aGameStateData);
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("GameData", value);
				mListener.SendGameStateMessage(mixGameData.Entitlement, dictionary, delegate
				{
				});
			}
		}

		void IGameSession.PostGameEventData(GameSession session, MixGameData stateData, MixGameResponse eventData)
		{
			if (eventData is MixGameResponse)
			{
				string value = JsonMapper.ToJson(eventData);
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary.Add("GameData", value);
				IGameStateMessageData message = session.MessageData as IGameStateMessageData;
				mListener.UpdateGameStateMessage(message, dictionary, delegate
				{
				});
			}
		}

		void IGameSession.PostChat(IEntitlementGameData aEntitlement)
		{
		}

		void IGameAssetManager.CancelBundles()
		{
			if (MonoSingleton<MixStandaloneAssetManager>.Instance != null)
			{
				MonoSingleton<MixStandaloneAssetManager>.Instance.CancelBundles();
			}
		}

		void IGameAssetManager.CancelBundles(string aUrl)
		{
			if (MonoSingleton<MixStandaloneAssetManager>.Instance != null)
			{
				MonoSingleton<MixStandaloneAssetManager>.Instance.CancelBundles(aUrl);
			}
		}

		void IGameAssetManager.DestroyBundleInstance(string aPath, UnityEngine.Object aObject)
		{
			if (aObject != null && MonoSingleton<MixStandaloneAssetManager>.Instance != null)
			{
				MonoSingleton<MixStandaloneAssetManager>.Instance.DestroyBundleInstance(aPath, aObject);
			}
		}

		UnityEngine.Object IGameAssetManager.GetBundleInstance(string aPath)
		{
			return MonoSingleton<MixStandaloneAssetManager>.Instance.GetBundleInstance(aPath);
		}

		void IGameAssetManager.LoadAsset(IGameAsset aSessionAsset, string aPath, object aParam)
		{
			MonoSingleton<MixStandaloneAssetManager>.Instance.LoadAsset(aSessionAsset, aPath, aParam);
		}

		void IGameAssetManager.LoadData(GameSession aSession, string aPath, string aFileName, Func<string, object> aMethod)
		{
			MonoSingleton<MixStandaloneAssetManager>.Instance.LoadData(aSession, aPath, aFileName, aMethod);
		}

		bool IGameAssetManager.WillBundleLoadFromWeb(string aPath)
		{
			return MonoSingleton<MixStandaloneAssetManager>.Instance.WillBundleLoadFromWeb(aPath);
		}

		string IGameLocalization.GetLocalizedContent(string aToken)
		{
			return MonoSingleton<MixStandaloneLocalizer>.Instance.GetLocalizedContent(aToken);
		}

		string IGamePlayerInfo.GetFriendDisplayName(string id)
		{
			return "TestFriend";
		}

		string IGamePlayerInfo.GetFriendDisplayName(IGameThreadParameters threadParams)
		{
			return "TestFriend";
		}

		int IGameSessionSettings.GetScreenHeight()
		{
			return 0;
		}

		int IGameSessionSettings.GetScreenWidth()
		{
			return 0;
		}

		int IGameSessionSettings.GetStatusBarHeight()
		{
			return 0;
		}

		float IGameSessionSettings.GetHeightScale()
		{
			return 0f;
		}

		float IGameSessionSettings.GetWidthScale()
		{
			return 0f;
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
			LoadSnapshot_003Ec__AnonStorey24D CS_0024_003C_003E8__locals2 = new LoadSnapshot_003Ec__AnonStorey24D();
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
			FakeAvatarManager.Instance.LoadRandomFriend(aMesh);
		}

		void IGameSounds.PauseSoundEvent(string eventName, GameObject gameObject)
		{
		}

		void IGameSounds.PlayMusic(string eventName, GameObject gameObject)
		{
		}

		void IGameSounds.PlaySoundEvent(string eventName)
		{
		}

		void IGameSounds.PlaySoundEvent(string eventName, GameObject gameObject)
		{
		}

		void IGameSounds.PlaySoundEvent(string eventName, object parameter)
		{
		}

		void IGameSounds.StopSoundEvent(string eventName)
		{
		}

		void IGameSounds.StopSoundEvent(string eventName, GameObject gameObject)
		{
		}

		void IGameSounds.UnpauseSoundEvent(string eventName, GameObject gameObject)
		{
		}

		void IGameSounds.SetVolumeEvent(string eventName, float volume)
		{
		}

		void IGameSounds.SetSwitchEvent(string eventName, string switchEventName, GameObject gameObject)
		{
		}

		void IGameInput.HideKeyboard()
		{
		}

		bool IGameInput.IsToastPanelActive()
		{
			return false;
		}

		void IGameAnalytics.LogEvent(GameLogEventType aGameLogEventType, Hashtable aData)
		{
		}

		DateTime IGameStatistics.GetLastPlayed(string id)
		{
			return GetGameSessionData(id).LastPlayed;
		}

		void IGameModeration.ModerateTextMessage(object aThread, string aTextToModerate, IGameModerationResult aGame, IEntitlementGameData aEntitlement, object aUserData)
		{
			ModerateTextMessage_003Ec__AnonStorey24C CS_0024_003C_003E8__locals11 = new ModerateTextMessage_003Ec__AnonStorey24C();
			CS_0024_003C_003E8__locals11.aGame = aGame;
			CS_0024_003C_003E8__locals11.aEntitlement = aEntitlement;
			CS_0024_003C_003E8__locals11.aUserData = aUserData;
			CS_0024_003C_003E8__locals11._003C_003Ef__this = this;
			IChatThread chatThread = aThread as IChatThread;
			chatThread.ModerateTextMessage(aTextToModerate, actionGenerator.CreateAction(delegate(ITextModerationResult result)
			{
				if (CS_0024_003C_003E8__locals11.aGame != null)
				{
					if (result.Success)
					{
						if (result.IsModerated)
						{
							CS_0024_003C_003E8__locals11._003C_003Ef__this.ActiveSession.LogAction(CS_0024_003C_003E8__locals11.aEntitlement.GetName() + "_text_moderated");
						}
						CS_0024_003C_003E8__locals11.aGame.OnModerationResult(result.IsModerated, result.ModeratedText, CS_0024_003C_003E8__locals11.aUserData);
					}
					else
					{
						CS_0024_003C_003E8__locals11.aGame.OnModerationError(CS_0024_003C_003E8__locals11.aUserData);
					}
				}
			}));
		}

		public void Initialize()
		{
			ChatController.OnChatThreadEnter = (EventHandler<ChatThreadEnterEventArgs>)Delegate.Combine(ChatController.OnChatThreadEnter, new EventHandler<ChatThreadEnterEventArgs>(OnChatThreadEnter));
			ChatController.OnChatThreadExit = (EventHandler<ChatThreadExitEventArgs>)Delegate.Combine(ChatController.OnChatThreadExit, new EventHandler<ChatThreadExitEventArgs>(OnChatThreadExit));
		}

		public void LoadGame(IEntitlementGameData aGame, IGameTray aListener, IGameMessageData messageData)
		{
			if (HasSession)
			{
				QuitGameSession();
			}
			GameSession gameSession = new GameSession(this, null, aGame, messageData != null);
			sessions.Add(gameSession);
			if (messageData != null)
			{
				gameSession.MessageData = messageData;
			}
			mListener = aListener;
			mListener.UpdateGameLoader(aGame.GetLogo());
			mListener.UpdateGameHeight(aGame.GetLogoHeight());
			mListener.SetGamePanel(GameTrayState.GAME);
			GameObject aGame2 = UnityEngine.Object.Instantiate(MonoSingleton<MixStandaloneAssetManager>.Instance.GetBundleInstance(aGame.GetHd())) as GameObject;
			gameSession.Initialize(aGame2);
			gameSession.StartSession();
		}

		public void CloseGameSession(GameSession aSession = null)
		{
			if (aSession != null)
			{
				aSession.CleanupSession();
				aSession.PostSession();
			}
			if (aSession == ActiveSession)
			{
				QuitGameSession();
			}
		}

		public void QuitGameSession()
		{
			mListener.UpdateGameExit(null);
			mListener.SetGamePanel(GameTrayState.NONE);
			UnityEngine.Object.Destroy(ActiveSession.GameInstance);
			sessions.Remove(ActiveSession);
		}

		public bool PauseGameSession()
		{
			if (HasSession)
			{
				mListener.OnGamePause(ActiveSession.Entitlement.GetPauseImage());
				mListener.SetGamePanel(GameTrayState.PAUSE);
				ActiveSession.GameInstance.gameObject.SetActive(false);
				return true;
			}
			return false;
		}

		public void ResumeGameSession()
		{
			if (HasSession)
			{
				mListener.SetGamePanel(GameTrayState.GAME);
				ActiveSession.GameInstance.gameObject.SetActive(true);
			}
		}

		protected GameSessionData GetGameSessionData(string gameId = null)
		{
			string id = ((!string.IsNullOrEmpty(gameId)) ? gameId : ActiveSession.Entitlement.GetUid());
			string threadId = ((ActiveSession != null) ? ActiveSession.ThreadId : mListener.GetCurrentThreadId());
			string playerId = MixSession.User.Id;
			GameSessionData gameSessionData = mSessionData.FirstOrDefault((GameSessionData game) => game.IsMine(id, threadId, playerId));
			if (gameSessionData == null)
			{
				gameSessionData = new GameSessionData(id, threadId, playerId);
				mSessionData.Add(gameSessionData);
			}
			return gameSessionData;
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

		protected IAvatar GetAvatarFromSwid(string aPlayerId, IGameThreadParameters threadParams)
		{
			if (string.Equals(MixSession.User.Id, aPlayerId))
			{
				return MixSession.User.Avatar;
			}
			IFriend friend = MixChat.FindFriend(aPlayerId);
			if (friend == null)
			{
				IEnumerable<string> members = threadParams.Members;
				IFriend friend2 = MixChat.FindFriend(members.FirstOrDefault((string memberId) => memberId == aPlayerId));
				if (friend2 == null)
				{
					IEnumerable<string> formerMembers = threadParams.FormerMembers;
					friend2 = MixChat.FindFriend(formerMembers.FirstOrDefault((string memberId) => memberId == aPlayerId));
				}
				IAvatar result;
				if (friend2 == null)
				{
					IAvatar avatar = null;
					result = avatar;
				}
				else
				{
					result = friend2.Avatar;
				}
				return result;
			}
			return friend.Avatar;
		}
	}
}
