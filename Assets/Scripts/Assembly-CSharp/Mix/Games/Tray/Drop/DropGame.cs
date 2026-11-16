using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Disney.LaunchPad.Packages.EventSystem;
using Disney.LaunchPad.Packages.FiniteStateMachine;
using LitJson;
using Mix.Games.Data;
using Mix.Games.Session;
using Mix.Games.Tray.Common;
using Mix.Games.Tray.Drop.GameLogic;
using UnityEngine;

namespace Mix.Games.Tray.Drop
{
	public class DropGame : MonoBehaviour, IMixGame, IMixGameDataRequest
	{
		public Action<float> OnGameTimeUpdated = delegate
		{
		};

		public Action<int> OnGhostScoreUpdated;

		public Action<int> OnScoreUpdated;

		public Action<int> OnBestScoreUpdated;

		public Action OnEarnBonus;

		public Action OnGameStop;

		public Action OnGameStart;

		public Action OnGamePause;

		public Action OnGameResume;

		[Header("Internal References")]
		public Canvas UICanvas;

		public DropUIManager UIManager;

		public LevelGenerator LevelGenerator;

		[Space(10f)]
		public DropInputHandler InputHandler;

		[Space(10f)]
		public FollowCamera FollowCamera;

		public CameraShake CameraShaker;

		public CameraZoom CameraZoomer;

		[Space(10f)]
		public CommonUI CommonUIElements;

		public ColumnGenerator ColumnGenerator;

		[Space(10f)]
		public Camera UICamera;

		[Space(10f)]
		public GameObject Environment;

		[Header("Prefab Rererences")]
		public Platform PlatformPrefab;

		public Pattern PatternPrefab;

		public DropColumn ColumnPrefab;

		[Space(5f)]
		public DropPlayer PlayerPrefab;

		public GhostDropPlayer GhostPrefab;

		[Space(5f)]
		public ScoreboardScene ScoreboardScenePrefab;

		public IntroScene IntroScenePrefab;

		[Space(10f)]
		public DropUIScreen PlayingUI;

		public DropUIScreen PauseUI;

		public DropUIScreen GameOverUI;

		[Header("Level")]
		public Vector2 GridSpacing;

		[Header("Tutorial")]
		public float TutorialTimePerJump = 0.1f;

		[Space(5f)]
		public float InitialZoomAmount = 8f;

		public float InitialZoomDuration = 2f;

		[Header("Bonus")]
		public float QuickJumpBonusTime = 0.5f;

		public int QuickJumpBonusAmount = 1;

		[Header("Player Die")]
		public float DeathZoomAmount;

		public float DeathZoomHold;

		public float DeathZoomDuration;

		[Space(5f)]
		public float DeathShakeAmount;

		public float DeathShakeDuration;

		[Header("Tuning")]
		[Tooltip("The amount of time a player can arrive early to a platform before it has appeared and still be safe.")]
		public float EarlyJumpAllowance;

		[Tooltip("The grace period the player has after a platform has sunk before the player dies.")]
		public float LateJumpAllowance;

		[Space(10f)]
		public float PauseResumeTime = 3f;

		[Space(10f)]
		public float DelayBeforeStarting = 1f;

		[Space(10f)]
		public float DelayBeforeScoreboard = 0.5f;

		public float DelayBeforeClosingMask = 2f;

		public float DelayBeforeEndingGame = 3f;

		[Header("Ghost Data")]
		public bool UseFakeGhost;

		public int FakeGhostScore = 1;

		public int[] FakeGhostBonusJumps = new int[0];

		private DropGameController gameController;

		private float gameTime;

		private int oldScore;

		private int oldGhostScore;

		private int highestPatternReachedByPlayer = -1;

		private FiniteStateMachine stateMachine;

		private ScoreboardScene mScoreboardScene;

		private int bestScore;

		private Tweener cameraZoomTweener;

		private static DropGame instance;

		public DropGameController GameController
		{
			get
			{
				if (gameController == null)
				{
					gameController = base.gameObject.GetComponentInParent<DropGameController>();
				}
				return gameController;
			}
		}

		public float GameTime
		{
			get
			{
				return gameTime;
			}
			set
			{
				gameTime = value;
				OnGameTimeUpdated(gameTime);
			}
		}

		public bool IsPlaying { get; private set; }

		public DropPlayer Player { get; private set; }

		public GhostDropPlayer GhostPlayer { get; private set; }

		public bool CanFinish { get; set; }

		public bool IsInTutorial
		{
			get
			{
				return highestPatternReachedByPlayer == 0;
			}
		}

		public int BestScore
		{
			get
			{
				return bestScore;
			}
			set
			{
				if (value != bestScore)
				{
					bestScore = value;
					if (OnBestScoreUpdated != null)
					{
						OnBestScoreUpdated(bestScore);
					}
				}
			}
		}

		public bool IsFirstRound { get; set; }

		public int Score
		{
			get
			{
				if (Player != null)
				{
					return Player.LandCount + Player.BonusCount;
				}
				return 0;
			}
		}

		public int GhostScore
		{
			get
			{
				if (GhostPlayer != null)
				{
					return GhostPlayer.LandCount + GhostPlayer.BonusCount;
				}
				return 0;
			}
		}

		public List<int> BonusJumps { get; set; }

		public List<DropDirection> JumpsOffPath { get; set; }

		public EventDispatcher GameEventDispatcher { get; private set; }

		public static DropGame Instance
		{
			get
			{
				if (instance == null)
				{
					instance = UnityEngine.Object.FindFirstObjectByType<DropGame>();
				}
				return instance;
			}
		}

		void IMixGame.Initialize(MixGameData aData)
		{
			if (aData == null)
			{
				IsFirstRound = true;
			}
			else
			{
				IsFirstRound = false;
			}
			GameController.LoadData(this, "data/Drop_Data/Drop_Data.gz", "Drop_Data.txt", ParseJsonData);
		}

		void IMixGame.Pause()
		{
			DropAudio.PauseSound("MUS/Music");
			DOTween.PauseAll();
			GameEventDispatcher.DispatchEvent(new GamePauseEvent());
		}

		void IMixGame.Play()
		{
			DOVirtual.DelayedCall(0.1f, delegate
			{
				DropAudio.PlaySound("MUS/Music");
			});
		}

		void IMixGame.Quit()
		{
			DropAudio.StopSound("MUS/Music");
			DOTween.KillAll();
		}

		void IMixGame.Resume()
		{
			DOVirtual.DelayedCall(0.1f, delegate
			{
				DropAudio.ResumeSound("MUS/Music");
			});
			DOTween.PlayAll();
		}

		void IMixGameDataRequest.OnDataLoaded(object aObject, object aUserData)
		{
			ContentObj contentObj = aObject as ContentObj;
			try
			{
				if (contentObj.content.objects.Patterns.Count == 0)
				{
					GameController.PauseOnNetworkError();
				}
				else
				{
					OnDropDataLoaded(contentObj);
				}
			}
			catch (Exception)
			{
				GameController.PauseOnNetworkError();
			}
		}

		private void OnEnable()
		{
			instance = this;
			TrayGamePhysics.ResetAllGameLayers();
			Physics.IgnoreLayerCollision(14, 14, true);
			Physics.IgnoreLayerCollision(14, 15, true);
			Physics.IgnoreLayerCollision(16, 15, true);
			gameController = base.gameObject.GetComponentInParent<DropGameController>();
		}

		private void OnDisable()
		{
			instance = null;
			TrayGamePhysics.ResetAllGameLayers();
		}

		private void Awake()
		{
			instance = this;
			oldScore = -1;
			oldGhostScore = -1;
			GameEventDispatcher = new EventDispatcher();
			BonusJumps = new List<int>();
			JumpsOffPath = new List<DropDirection>();
			CreateStateMachine();
			DropAudio.Game = this;
			if (DebugSceneIndicator.IsDebugScene)
			{
				DOVirtual.DelayedCall(0.1f, delegate
				{
					DropAudio.PlaySound("MUS/Music");
				});
			}
		}

		private void CreateStateMachine()
		{
			stateMachine = FiniteStateMachine.CreateFiniteStateMachine(base.transform, "State Machine");
			stateMachine.eventDispatcher = GameEventDispatcher;
			stateMachine.defaultTransition = stateMachine.CreateTransition<ImmediateTransition>(string.Empty);
			State state = stateMachine.CreateState("Loading");
			State state2 = stateMachine.CreateState<InitializeDropGameState>("Initializing");
			State state3 = stateMachine.CreateState<StartDropGameState>("Start Game");
			State state4 = stateMachine.CreateState<PlayDropGameState>("Playing");
			State state5 = stateMachine.CreateState<GameOverDropGameState>("Game Over");
			State state6 = stateMachine.CreateState<ScoreboardDropGameState>("Scoreboard");
			State endState = stateMachine.CreateState<QuitDropGameState>("Save And Quit");
			State state7 = stateMachine.CreateState<PauseDropGameState>("Paused");
			State state8 = stateMachine.CreateState<RetryDropGameState>("Retry");
			stateMachine.initialState = state;
			stateMachine.CreateEventSignal<GameLoadedEvent>(state, state2);
			stateMachine.CreateEventSignal<GameInitializedEvent>(state2, state3);
			stateMachine.CreateEventSignal<GameStartEvent>(state3, state4);
			stateMachine.CreateEventSignal<GamePauseEvent>(state4, state7);
			stateMachine.CreateEventSignal<GameResumeEvent>(state7, state4);
			stateMachine.CreateEventSignal<GameOverEvent>(state4, state5);
			stateMachine.CreateEventSignal<GameScoreboardEvent>(state5, state6);
			stateMachine.CreateEventSignal<GameSendEvent>(state6, endState);
			stateMachine.CreateEventSignal<GameRetryEvent>(state6, state8);
			stateMachine.CreateEventSignal<GameInitializedEvent>(state8, state3);
			stateMachine.SetActive(true);
		}

		public void ResumePlaying()
		{
			if (!IsPlaying)
			{
				IsPlaying = true;
				Player.ResumeAllTweens();
				Player.PlayerLogicAnimator.speed = 1f;
				if (OnGameResume != null)
				{
					OnGameResume();
				}
			}
		}

		public void PausePlaying()
		{
			if (IsPlaying)
			{
				IsPlaying = false;
				Player.PauseAllTweens();
				Player.PlayerLogicAnimator.speed = 0f;
				if (OnGamePause != null)
				{
					OnGamePause();
				}
			}
		}

		public void StopPlaying()
		{
			IsPlaying = false;
			if (OnGameStop != null)
			{
				OnGameStop();
			}
		}

		public void StartPlaying()
		{
			IsPlaying = true;
			Player.PlayerLogicAnimator.speed = 1f;
			if (OnGameStart != null)
			{
				OnGameStart();
			}
		}

		private void CheckForScoreChange(int value)
		{
			if (oldScore != Score)
			{
				oldScore = Score;
				if (OnScoreUpdated != null)
				{
					OnScoreUpdated(Score);
				}
			}
		}

		private void CheckForGhostScoreChange(int value)
		{
			if (oldGhostScore != GhostScore)
			{
				oldGhostScore = GhostScore;
				if (OnGhostScoreUpdated != null)
				{
					OnGhostScoreUpdated(GhostScore);
				}
			}
		}

		public void OnPlayerPlatformChanged(Platform platform)
		{
			highestPatternReachedByPlayer = Mathf.Max(platform.PatternNumber, highestPatternReachedByPlayer);
		}

		public void PlayerDied()
		{
			GameEventDispatcher.DispatchEvent(new GameOverEvent());
			StopPlaying();
			if (cameraZoomTweener != null)
			{
				cameraZoomTweener.Kill();
			}
			cameraZoomTweener = DOVirtual.Float(CameraZoomer.Zoom, DeathZoomAmount, DeathZoomDuration, delegate(float x)
			{
				CameraZoomer.Zoom = x;
			}).SetEase(Ease.InOutQuad).SetDelay(DeathZoomHold);
			if (CameraShaker != null)
			{
				CameraShaker.ShakeCamera(DeathShakeAmount, DeathShakeDuration, 12);
			}
		}

		private void FixedUpdate()
		{
			if (IsPlaying)
			{
				if (highestPatternReachedByPlayer > 0)
				{
					GameTime += Time.fixedDeltaTime;
				}
				else
				{
					GameTime = Mathf.Min(GameTime + Time.fixedDeltaTime, TutorialTimePerJump * (float)Player.JumpCount);
				}
			}
			if (CanFinish)
			{
				GameOver();
			}
		}

		private void EnterPlayState()
		{
		}

		public Vector3 GetJumpTarget(Coordinate2D coords)
		{
			return LevelGenerator.transform.TransformPoint(new Vector3((float)coords.x * GridSpacing.x, 0.75f, (float)coords.y * GridSpacing.y));
		}

		public void CreatePlayer()
		{
			Player = UnityEngine.Object.Instantiate(PlayerPrefab);
			Player.name = "Player";
			Player.transform.SetParent(base.transform, false);
			DropPlayer player = Player;
			player.OnPlatformUpdated = (Action<Platform>)Delegate.Combine(player.OnPlatformUpdated, new Action<Platform>(OnPlayerPlatformChanged));
			DropPlayer player2 = Player;
			player2.OnDie = (Action)Delegate.Combine(player2.OnDie, new Action(PlayerDied));
			DropPlayer player3 = Player;
			player3.OnLandCountUpdated = (Action<int>)Delegate.Combine(player3.OnLandCountUpdated, new Action<int>(CheckForScoreChange));
			DropPlayer player4 = Player;
			player4.OnLandCountUpdated = (Action<int>)Delegate.Combine(player4.OnLandCountUpdated, new Action<int>(UpdateInitialZoom));
			DropPlayer player5 = Player;
			player5.OnBonusCountUpdated = (Action<int>)Delegate.Combine(player5.OnBonusCountUpdated, new Action<int>(CheckForScoreChange));
			DropPlayer player6 = Player;
			player6.OnBonusCountUpdated = (Action<int>)Delegate.Combine(player6.OnBonusCountUpdated, new Action<int>(PlayPlayerBonusSound));
			FollowCamera.SetFollowTarget(Player.transform, true);
			GameController.LoadFriend(Player.PlayerSkin.gameObject, GameController.PlayerId, true);
		}

		public void CreateGhost()
		{
			DropData dropData = GameController.GetGameData<DropData>();
			if (UseFakeGhost)
			{
				dropData = new DropData();
				DropResponse dropResponse = new DropResponse();
				dropResponse.PlayerSwid = "-1";
				dropResponse.Score = FakeGhostScore;
				dropResponse.BonusJumps = FakeGhostBonusJumps;
				dropData.Responses.Add(dropResponse);
			}
			if (dropData == null || dropData.Responses.Count <= 0)
			{
				return;
			}
			string playerSwid = "1";
			if (DebugSceneIndicator.IsMainScene)
			{
				playerSwid = GameController.PlayerId;
			}
			IEnumerable<DropResponse> source = dropData.Responses.Where((DropResponse resp) => resp.PlayerSwid != playerSwid);
			if (source.Any())
			{
				DropResponse dropResponse2 = source.OrderByDescending((DropResponse resp) => resp.Score).First();
				GhostPlayer = UnityEngine.Object.Instantiate(GhostPrefab);
				GhostPlayer.name = "Ghost";
				GhostPlayer.transform.SetParent(base.transform, false);
				GhostPlayer.GhostResponse = dropResponse2;
				GhostDropPlayer ghostPlayer = GhostPlayer;
				ghostPlayer.OnLandCountUpdated = (Action<int>)Delegate.Combine(ghostPlayer.OnLandCountUpdated, new Action<int>(CheckForGhostScoreChange));
				GhostDropPlayer ghostPlayer2 = GhostPlayer;
				ghostPlayer2.OnBonusCountUpdated = (Action<int>)Delegate.Combine(ghostPlayer2.OnBonusCountUpdated, new Action<int>(CheckForGhostScoreChange));
				GhostDropPlayer ghostPlayer3 = GhostPlayer;
				ghostPlayer3.OnBonusCountUpdated = (Action<int>)Delegate.Combine(ghostPlayer3.OnBonusCountUpdated, new Action<int>(PlayGhostBonusSound));
				GameController.LoadFriend(GhostPlayer.PlayerSkin.gameObject, dropResponse2.PlayerSwid, true);
				GhostPlayer.NameMarker = CommonUIElements.CreateNameMarkerForGhost(GhostPlayer);
				OnGhostScoreUpdated = (Action<int>)Delegate.Combine(OnGhostScoreUpdated, new Action<int>(GhostPlayer.UpdateGhostScore));
			}
		}

		public void GameOver()
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				if (IsFirstRound)
				{
					GameOverFirst();
				}
				else
				{
					GameOverResponse();
				}
				string aGameMessage = Score.ToString();
				GameController.LogEvent(GameLogEventType.ACTION, "winter_waltz_post_score", aGameMessage);
			}
		}

		public void EarnBonus()
		{
			Player.BonusCount++;
			BonusJumps.Add(Player.JumpCount);
			if (OnEarnBonus != null)
			{
				OnEarnBonus();
			}
		}

		public void InitScoreboard()
		{
			if (mScoreboardScene == null)
			{
				mScoreboardScene = UnityEngine.Object.Instantiate(ScoreboardScenePrefab, new Vector3(0f, -500f, 0f), Quaternion.identity) as ScoreboardScene;
				mScoreboardScene.transform.SetParent(base.transform);
				mScoreboardScene.Init(this);
			}
			mScoreboardScene.gameObject.SetActive(false);
		}

		public void ShowScoreboard()
		{
			if (GhostPlayer != null)
			{
				UnityEngine.Object.Destroy(GhostPlayer.gameObject);
			}
			mScoreboardScene.Show(!IsFirstRound && Score > BestScore);
		}

		public void HideScoreboard()
		{
			mScoreboardScene.Hide();
		}

		public bool AllowRetry()
		{
			return !IsFirstRound && (Score <= BestScore || Score <= GetPersonalBest());
		}

		private int GetPersonalBest()
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				DropData gameData = GameController.GetGameData<DropData>();
				if (gameData != null)
				{
					DropResponse myResponse = gameData.GetMyResponse(gameData.Responses, GameController.PlayerId);
					if (myResponse != null)
					{
						return myResponse.Score;
					}
				}
				return -1;
			}
			return 6;
		}

		public void Reset()
		{
			InputHandler.OnTouchBegin = null;
			OnGameStart = null;
			OnGameStop = null;
			highestPatternReachedByPlayer = -1;
			IsPlaying = false;
			CanFinish = false;
			FollowCamera.enabled = false;
			UnityEngine.Object.Destroy(Player.gameObject);
			CreatePlayer();
			CreateGhost();
			DOVirtual.DelayedCall(0.1f, delegate
			{
				GameTime = 0f;
			});
		}

		public void GameOverFirst()
		{
			DropData gameData = GameController.GetGameData<DropData>();
			gameData.LevelGenerationSeed = LevelGenerator.RandomSeed;
			CreateResponse();
			GameController.GameOver(gameData);
		}

		public void GameOverResponse()
		{
			if (DebugSceneIndicator.IsMainScene)
			{
				DropResponse gameData = CreateResponse();
				GameController.GameOver(gameData);
			}
		}

		private DropResponse CreateResponse()
		{
			DropData gameData = GameController.GetGameData<DropData>();
			DropResponse myResponse = gameData.GetMyResponse(gameData.Responses, GameController.PlayerId);
			myResponse.Score = Score;
			myResponse.BonusJumps = BonusJumps.ToArray();
			myResponse.JumpsOffPath = JumpsOffPath.ToArray();
			myResponse.Attempts++;
			return myResponse;
		}

		private void PlayPlayerBonusSound(int count)
		{
			DropAudio.PlaySound("SFX/Gameplay/Player/BonusEarned");
		}

		private void PlayGhostBonusSound(int count)
		{
			DropAudio.PlaySound("SFX/Gameplay/Player/BonusEarned");
		}

		private void UpdateInitialZoom(int count)
		{
			if (count > 0)
			{
				cameraZoomTweener = DOVirtual.Float(CameraZoomer.Zoom, 0f, InitialZoomDuration, delegate(float x)
				{
					CameraZoomer.Zoom = x;
				}).SetEase(Ease.InOutQuad);
				DropPlayer player = Player;
				player.OnLandCountUpdated = (Action<int>)Delegate.Remove(player.OnLandCountUpdated, new Action<int>(UpdateInitialZoom));
			}
		}

		private void OnGameDataLoaded()
		{
			GameEventDispatcher.DispatchEvent(new GameLoadedEvent());
		}

		private void OnDropDataLoaded(ContentObj data)
		{
			LevelGenerator.CreatePatterns(data.content.objects.Patterns);
			GameEventDispatcher.DispatchEvent(new GameLoadedEvent());
		}

		public object ParseJsonData(string json)
		{
			return JsonMapper.ToObject<ContentObj>(json);
		}
	}
}
