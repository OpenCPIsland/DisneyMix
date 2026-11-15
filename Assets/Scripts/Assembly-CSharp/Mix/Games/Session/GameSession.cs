using System;
using System.Collections;
using Mix.Games.Data;
using Mix.Games.Message;
using Mix.Ui;
using UnityEngine;

namespace Mix.Games.Session
{
	public class GameSession
	{
		public const string SESSION = "Session";

		public const string SESSION_ASSET = "SessionAsset";

		public const string PARAMETER = "Parameter";

		public const string BUNDLE_URL = "BundleUrl";

		protected const string SOUND_MEDIA_MESSAGE = "MainApp/UI/SFX_3_MediaMessage";

		public bool IsGameLoaded;

		protected BaseGameController mGameController;

		protected IGameSession mGameSessionHandler;

		protected IGameAssetManager mGameSessionAssetManager;

		protected IGameLocalization mGameSessionLocalization;

		protected IGamePlayerInfo mGameSessionPlayer;

		protected IGameSessionSettings mGameSessionSettings;

		protected IGameAvatar mFriendsGameSessionHandler;

		protected IGameSounds mGameSessionSounds;

		protected IGameInput mGameInput;

		protected IGameAnalytics mGameAnalytics;

		protected IGameStatistics mGameStatistics;

		protected IGameModeration mGameModeration;

		protected IGameLogging mLogging;

		protected IEntitlementGameData mEntitlement;

		protected IGameMessageData mMessageData;

		protected DateTime mStartTime;

		protected GameObject mGame;

		protected MixGameData mGameData;

		protected string mGameDataJson;

		protected GameSessionState mState;

		protected string mGameMessageId;

		protected object mGameDataToPost;

		protected IMixGameDataRequest mGameDataRequester;

		protected IGameThreadParameters mThreadParameters;

		protected bool mIsResponseSession;

		protected bool mIsSessionStarted;

		protected bool mIsFakeThread;

		protected object mThreadObject;

		public GameObject GameInstance
		{
			get
			{
				return mGame;
			}
		}

		public GameSessionState State
		{
			get
			{
				return mState;
			}
			set
			{
				mState = value;
			}
		}

		public IEntitlementGameData Entitlement
		{
			get
			{
				return mEntitlement;
			}
			set
			{
				mEntitlement = value;
			}
		}

		public IGameMessageData MessageData
		{
			get
			{
				return mMessageData;
			}
			set
			{
				mMessageData = value;
			}
		}

		public object Thread
		{
			get
			{
				if (mThreadParameters != null)
				{
					return mThreadParameters.Thread;
				}
				return null;
			}
		}

		public string ThreadId
		{
			get
			{
				if (mThreadParameters != null)
				{
					return mThreadParameters.ThreadId;
				}
				return null;
			}
		}

		public bool IsGroupSession
		{
			get
			{
				if (mThreadParameters == null)
				{
					return false;
				}
				return mThreadParameters.IsGroupSession;
			}
		}

		public Camera GameCamera
		{
			get
			{
				return mGameController.MixGameCamera;
			}
		}

		public bool ShouldQuitGameOnPause { get; set; }

		public string PlayerId
		{
			get
			{
				return (mGameSessionPlayer == null) ? string.Empty : mGameSessionPlayer.Id;
			}
		}

		public virtual float HeightScale
		{
			get
			{
				return mGameSessionSettings.GetHeightScale();
			}
		}

		public float WidthScale
		{
			get
			{
				return mGameSessionSettings.GetWidthScale();
			}
		}

		public virtual int ScreenHeight
		{
			get
			{
				return mGameSessionSettings.GetScreenHeight();
			}
		}

		public int ScreenWidth
		{
			get
			{
				return mGameSessionSettings.GetScreenWidth();
			}
		}

		public virtual int StatusBarHeight
		{
			get
			{
				return mGameSessionSettings.GetStatusBarHeight();
			}
		}

		public IGameThreadParameters ThreadParameters
		{
			get
			{
				return mThreadParameters;
			}
			set
			{
				mThreadParameters = value;
			}
		}

		public IGameSounds SessionSounds
		{
			get
			{
				return mGameSessionSounds;
			}
		}

		public IGameSession GameSessionHandler
		{
			get
			{
				return mGameSessionHandler;
			}
		}

		public IGameStatistics GameStatistics
		{
			get
			{
				return mGameStatistics;
			}
		}

		public bool IsToastPanelActive
		{
			get
			{
				return mGameInput.IsToastPanelActive();
			}
		}

		public bool IsFakeThread
		{
			get
			{
				return mThreadParameters.IsFakeThread;
			}
		}

		public IGameLogging Logger
		{
			get
			{
				return mLogging;
			}
		}

		protected GameSession()
		{
		}

		public GameSession(IGameSession aGameSessionHandler, IGameThreadParameters aThreadParameters, IEntitlementGameData aGameEntitlement, bool aIsResponseSession)
		{
			mGameSessionHandler = aGameSessionHandler;
			mGameSessionAssetManager = aGameSessionHandler as IGameAssetManager;
			mGameSessionLocalization = aGameSessionHandler as IGameLocalization;
			mGameSessionPlayer = aGameSessionHandler as IGamePlayerInfo;
			mGameSessionSettings = aGameSessionHandler as IGameSessionSettings;
			mFriendsGameSessionHandler = aGameSessionHandler as IGameAvatar;
			mGameSessionSounds = aGameSessionHandler as IGameSounds;
			mGameInput = aGameSessionHandler as IGameInput;
			mGameAnalytics = aGameSessionHandler as IGameAnalytics;
			mGameStatistics = aGameSessionHandler as IGameStatistics;
			mGameModeration = aGameSessionHandler as IGameModeration;
			mLogging = aGameSessionHandler as IGameLogging;
			mThreadParameters = aThreadParameters;
			mEntitlement = aGameEntitlement;
			mIsResponseSession = aIsResponseSession;
			ShouldQuitGameOnPause = false;
		}

		public void Initialize(GameObject aGame)
		{
			if (aGame != null && mEntitlement != null && string.Equals(mEntitlement.GetName(), "Spikey", StringComparison.OrdinalIgnoreCase))
			{
				ResponsiveCanvasScaler.Ensure(aGame);
			}

			mGame = aGame;
			mGame.transform.position += new Vector3(1000f, 0f, 0f);
			mGame.SetActive(false);
			mGameController = mGame.GetComponent<BaseGameController>();
		}

		public void CancelBundles()
		{
			if (mGameSessionAssetManager != null)
			{
				mGameSessionAssetManager.CancelBundles();
			}
		}

		public void CancelBundles(string aUrl)
		{
			if (mGameSessionAssetManager != null)
			{
				mGameSessionAssetManager.CancelBundles(aUrl);
			}
		}

		public void DestroyBundleInstance(string aPath, UnityEngine.Object aObject)
		{
			if (mGameSessionAssetManager != null)
			{
				mGameSessionAssetManager.DestroyBundleInstance(aPath, aObject);
			}
		}

		public UnityEngine.Object GetBundleInstance(string aPath)
		{
			return mGameSessionAssetManager.GetBundleInstance(aPath);
		}

		public bool WillBundleLoadFromWeb(string aPath)
		{
			return mGameSessionAssetManager.WillBundleLoadFromWeb(aPath);
		}

		public void ModerateText(string aTextToModerate, IGameModerationResult aGame, object aUserData)
		{
			mGameModeration.ModerateTextMessage(Thread, aTextToModerate, aGame, mEntitlement, aUserData);
		}

		public virtual string GetFriendName()
		{
			return mGameSessionPlayer.GetFriendDisplayName(mThreadParameters);
		}

		public string GetLocalizedString(string aToken)
		{
			return mGameSessionLocalization.GetLocalizedContent(aToken);
		}

		public string GetPlayerName(string aPlayerId)
		{
			string empty = string.Empty;
			if (string.Equals(aPlayerId, PlayerId))
			{
				return mGameSessionPlayer.DisplayName;
			}
			return (mGameSessionPlayer == null) ? string.Empty : mGameSessionPlayer.GetFriendDisplayName(aPlayerId);
		}

		public void LoadAsset(IGameAsset aSessionAsset, string aPath, object aParam)
		{
			if (mGameSessionAssetManager != null)
			{
				mGameSessionAssetManager.LoadAsset(aSessionAsset, aPath, aParam);
			}
		}

		public void LoadData(IMixGameDataRequest aGameDataRequester, string aPath, string aFileName, Func<string, object> aMethod)
		{
			mGameDataRequester = aGameDataRequester;
			mGameSessionAssetManager.LoadData(this, aPath, aFileName, aMethod);
		}

		public void LoadFriend(GameObject aMesh, string aPlayerId, bool aHideCostumes, bool aHideGeoAccessories)
		{
			mFriendsGameSessionHandler.LoadFriend(aMesh, aPlayerId, mThreadParameters, aHideCostumes, aHideGeoAccessories);
		}

		public void PreloadAvatar(string aPlayerId)
		{
			mFriendsGameSessionHandler.PreloadAvatar(aPlayerId, mThreadParameters);
		}

		public void LoadSnapshot(string aPlayerId, int size, Action<bool, Sprite> callback)
		{
			mFriendsGameSessionHandler.LoadSnapshot(aPlayerId, mThreadParameters, size, callback);
		}

		public void LoadRandomFriend(GameObject aMesh)
		{
			mFriendsGameSessionHandler.LoadRandomFriend(aMesh);
		}

		public void LogAction(string aGameAction, object aGameMessage = null)
		{
			Hashtable hashtable = new Hashtable();
			hashtable.Add("action", aGameAction);
			hashtable.Add("message", (aGameMessage != null) ? aGameMessage : string.Empty);
			hashtable.Add("thread", Thread);
			if (mGameAnalytics != null)
			{
				mGameAnalytics.LogEvent(GameLogEventType.ACTION, hashtable);
			}
		}

		public void LogPageView(string aGameAction)
		{
			Hashtable hashtable = new Hashtable();
			hashtable.Add("location", aGameAction + "_" + mEntitlement.GetName().Trim().Replace(" ", "_"));
			if (mGameAnalytics != null)
			{
				mGameAnalytics.LogEvent(GameLogEventType.PAGEVIEW, hashtable);
			}
		}

		public void LogTiming(double aGameTime)
		{
			Hashtable hashtable = new Hashtable();
			hashtable.Add("elapsedTime", aGameTime.ToString());
			hashtable.Add("pathName", mEntitlement.GetName().Trim().Replace(" ", "_"));
			hashtable.Add("thread", Thread);
			if (mGameAnalytics != null)
			{
				mGameAnalytics.LogEvent(GameLogEventType.TIMING, hashtable);
			}
		}

		public void LogPause()
		{
			LogAction("pause_" + mEntitlement.GetName());
		}

		public void LogContinue()
		{
			LogAction("continue_" + mEntitlement.GetName());
		}

		public void LogQuit()
		{
			LogAction("quit_" + mEntitlement.GetName());
		}

		public virtual void EndSession(object aGameData)
		{
			mGameDataToPost = aGameData;
			mGameSessionHandler.HandleSessionState(this, GameSessionState.COMPLETE);
			double totalSeconds = DateTime.Now.Subtract(mStartTime).TotalSeconds;
			LogTiming(totalSeconds);
		}

		public void OnDataLoaded(object aObject, object aUserData)
		{
			if (mGameDataRequester != null)
			{
				mGameDataRequester.OnDataLoaded(aObject, aUserData);
			}
		}

		public virtual void PauseSession()
		{
			LogPause();
			if (State != GameSessionState.LOADING)
			{
				if (mGameController != null)
				{
					mGameController.PauseGame();
				}
				if (mGame != null)
				{
					mGame.SetActive(false);
				}
			}
		}

		public void NetworkErrorPauseSession()
		{
			if (mGame != null)
			{
				mGame.SetActive(false);
			}
		}

		public virtual void PostSession()
		{
			if (mGameDataToPost != null && mGameSessionHandler != null)
			{
				mGameSessionHandler.PostGameStateData(this, mGameDataToPost);
			}
			if (mGameController != null)
			{
				mGameController.CleanUpGame();
			}
		}

		public virtual void QuitSessionFromApp()
		{
			LogQuit();
			if (State != GameSessionState.STARTING && mGameController != null)
			{
				mGameController.QuitGame();
			}
		}

		public virtual void QuitSession()
		{
			if (mGameSessionHandler != null)
			{
				mGameSessionHandler.HandleSessionState(this, GameSessionState.INCOMPLETE);
			}
		}

		public virtual void ResumeSession()
		{
			LogContinue();
			if (mGame != null)
			{
				if (!mIsSessionStarted)
				{
					StartSession();
				}
				mGame.SetActive(true);
				mGameController.ResumeGame();
			}
		}

		public void PauseOnNetworkError()
		{
			mGameSessionHandler.HandleSessionState(this, GameSessionState.ERROR);
		}

		public void ResumeNetworkError()
		{
			mGameSessionHandler.HandleSessionState(this, GameSessionState.PAUSED);
		}

		public virtual void StartSession()
		{
			mStartTime = DateTime.Now;
			if (mIsResponseSession)
			{
				mGameController.Initialize(this);
			}
			else
			{
				mGameController.Initialize(this, mEntitlement.GetUid());
			}
			mGame.SetActive(true);
			mGameController.PlayGame();
			mIsSessionStarted = true;
			mState = GameSessionState.PLAYING;
		}

		public void UpdateSession(MixGameData gameData, MixGameResponse responseData)
		{
			mGameSessionHandler.PostGameEventData(this, gameData, responseData);
			mGameSessionHandler.HandleSessionState(this, GameSessionState.COMPLETE);
		}

		public virtual void UpdateGameSessionState(GameSessionState aState)
		{
			mState = aState;
		}

		public virtual void CleanupSession()
		{
			if (mEntitlement != null)
			{
				DestroyBundleInstance(mEntitlement.GetHd(), mGame);
			}
		}

		public virtual int GetNumberOfCompletedSessions()
		{
			return mGameStatistics.SessionsCompleted;
		}
	}
}
