using System;
using System.Collections;
using System.Collections.Generic;
using Avatar.DataTypes;
using DG.Tweening;
using Mix.Games.Data;
using Mix.Games.Session;
using Mix.Games.Tray.Common;
using UnityEngine;

namespace Mix.Games.Tray.Runner
{
	public class MainRunnerGame : MonoBehaviour, IMixGame, IMixGameTimer
	{
		private enum TransitionScreenState
		{
			IN = 0,
			OUT = 1
		}

		private const float CHUNK_DRAG_SPEED = 30f;

		private const string MUSIC_CUE_NAME = "Runner/MUS/Main";

		public const int MIN_LOSS_VALUE = 10;

		public CameraController cameraController;

		public RunnerGameController GameController;

		public RunnerData gameData;

		public GameObject gamePrefab;

		public GameObject playerPrefab;

		public GameObject ghostPrefab;

		private GameObject spawnedPlayer;

		private RunnerController spawnedPlayerController;

		public Vector3 playerStartPos;

		public Vector3 chunk1Pos;

		public Vector3 chunk2Pos;

		public Vector3 chunkSpawner;

		public float gizmoSize = 1f;

		public List<ChunkController> chunks;

		public ChunkController startChunk;

		public ChunkController endChunk;

		private GameObject lastChunk;

		private ChunkController lastChunkController;

		private List<GameObject> cleanupObjects = new List<GameObject>();

		public GameObject collectibleParticle;

		public int CHUNK_PIECES = 5;

		public string SOUND_PREFIX = "Runner/SFX/";

		public string COLLECTED_MESSAGE = "You collected: ";

		public int maxLives = 3;

		[HideInInspector]
		public int lives;

		public GameMode mode = GameMode.PLAY;

		public float magnetMax;

		private float magnetTimer;

		private bool isMagnetOn;

		private float damageTimer;

		public int MAX_CHUNK_ATTEMPTS = 3;

		private int currentChunkAttempts;

		public float TOTAL_DISTANCE = 240f;

		[HideInInspector]
		public int checkpointsPassed;

		private List<PolygonCollider2D> avatarColliders;

		public List<ChunkController> levelData;

		public bool isFirstPlayer = true;

		private int chunkIndex;

		public float endGameWait = 3f;

		private float endGameTimer;

		private bool hasEnded;

		public int avgDifficulty;

		public LevelBuilder levelBuilder;

		public PlayerGameplay playerGameplay;

		public List<GameObject> ghostList = new List<GameObject>();

		private List<RunnerRecorder> otherPlayerGhosts = new List<RunnerRecorder>();

		private float GHOST_OFFSET = 0.01f;

		public TransitionPanel transitionPanel;

		public GameObject environment;

		public Transform builderHolder;

		public BuildBackground buildBackground;

		public Transform flyingGemHolder;

		public Camera flyingGemCamera;

		public RunnerUI uiController;

		[Space(10f)]
		public AnimationCurve maxChunkDifficultyForSessionNumber;

		private GameMode prePausedMode;

		private Dictionary<int, List<ChunkController>> mChunksByDifficulty;

		private Vector3 mMinScreenPoint;

		private Vector3 mMaxScreenPoint;

		public int coins { get; private set; }

		public bool HasThePlayerJumpedAtLeastOnce { get; set; }

		public bool gameHasEnded
		{
			get
			{
				return hasEnded;
			}
		}

		public int MaxBuildCount
		{
			get
			{
				return CHUNK_PIECES;
			}
		}

		public int CurrentBuildCount
		{
			get
			{
				return levelBuilder.CurrentLevelChunkIndex;
			}
		}

		void IMixGame.Initialize(MixGameData aData)
		{
			if (aData != null)
			{
				gameData = GameController.GetGameData<RunnerData>();
				isFirstPlayer = false;
				avgDifficulty = gameData.Difficulty;
				if (gameData.Responses.Count > 0)
				{
					foreach (RunnerResponse response in gameData.Responses)
					{
						RunnerRecorder runnerRecorder = new RunnerRecorder();
						runnerRecorder.IngestFormattedData(response.Ghost);
						runnerRecorder.playerId = response.PlayerSwid;
						runnerRecorder.score = response.Checkpoints;
						otherPlayerGhosts.Add(runnerRecorder);
						GameController.PreloadAvatar(response.PlayerSwid);
					}
				}
				List<string> list = gameData.LevelData;
				if (list != null)
				{
					levelData = new List<ChunkController>();
					for (int i = 0; i < list.Count; i++)
					{
						levelData.Add(GetChunkByName(list[i]));
					}
				}
				else
				{
					Debug.LogWarning("Could not load previous level data");
				}
			}
			else
			{
				isFirstPlayer = true;
			}
		}

		void IMixGame.Play()
		{
			DOVirtual.DelayedCall(0.1f, delegate
			{
				BaseGameController.Instance.Session.SessionSounds.PlayMusic("Runner/MUS/Main", base.gameObject);
			});
		}

		void IMixGame.Quit()
		{
			BaseGameController.Instance.Session.SessionSounds.StopSoundEvent("Runner/MUS/Main", base.gameObject);
			DOTween.KillAll();
		}

		void IMixGame.Pause()
		{
			BaseGameController.Instance.Session.SessionSounds.PauseSoundEvent("Runner/MUS/Main", base.gameObject);
			Pause(GameMode.PAUSE);
		}

		void IMixGame.Resume()
		{
			BaseGameController.Instance.Session.SessionSounds.UnpauseSoundEvent("Runner/MUS/Main", base.gameObject);
			ResumeAfterDelay();
		}

		void IMixGameTimer.GameTimerStart()
		{
		}

		void IMixGameTimer.GameTimerProgress(float aTimeRemaining)
		{
		}

		void IMixGameTimer.GameTimerComplete()
		{
		}

		private void OnEnable()
		{
			TrayGamePhysics.ResetAllGameLayers();
			int num = LayerMask.NameToLayer("Game3D A");
			Physics2D.IgnoreLayerCollision(num, num, true);
		}

		private void Awake()
		{
			SortChunks();
			GroupChunksByDifficulty();
			SetupScreenBounds();
		}

		private void Start()
		{
			buildBackground.gameObject.SetActive(false);
			PlaySound("ScreenOpenUI", SOUND_PREFIX);
			if (DebugSceneIndicator.IsMainScene)
			{
				GameController.PreloadAvatar(GameController.PlayerId);
			}
			if (isFirstPlayer)
			{
				mode = GameMode.FIRST_PLAYER;
				EnterFirstPlayerMode();
			}
			else
			{
				mode = GameMode.START;
				EnterPlayMode();
			}
			lives = maxLives;
		}

		private void GroupChunksByDifficulty()
		{
			mChunksByDifficulty = new Dictionary<int, List<ChunkController>>();
			for (int i = 0; i < chunks.Count; i++)
			{
				if (!mChunksByDifficulty.ContainsKey(chunks[i].difficulty))
				{
					mChunksByDifficulty.Add(chunks[i].difficulty, new List<ChunkController>());
				}
				mChunksByDifficulty[chunks[i].difficulty].Add(chunks[i]);
			}
		}

		public int GetChunkCountForDifficulty(int difficulty)
		{
			if (mChunksByDifficulty.ContainsKey(difficulty))
			{
				return mChunksByDifficulty[difficulty].Count;
			}
			return 0;
		}

		public int GetRandomChunkIndexWithDifficulty(int difficulty, ChunkController chunkToExclude = null)
		{
			List<ChunkController> list = new List<ChunkController>(mChunksByDifficulty[difficulty]);
			if (list.Count > 1 && chunkToExclude != null)
			{
				list.Remove(chunkToExclude);
			}
			return chunks.IndexOf(list[UnityEngine.Random.Range(0, list.Count)]);
		}

		private void SortChunks()
		{
			ChunkDifficultyComparer comparer = new ChunkDifficultyComparer();
			chunks.Sort(comparer);
			for (int i = 0; i < chunks.Count; i++)
			{
				chunks[i].ChunkIndex = i;
			}
		}

		private void Update()
		{
			if (mode == GameMode.PLAY)
			{
				ManagePowerups();
			}
			else if (mode == GameMode.DAMAGE)
			{
				ManageDamagePause();
			}
			else if (mode == GameMode.END)
			{
				ManageEndWait();
			}
			else if (mode != GameMode.BUILD)
			{
			}
		}

		public void NewGame()
		{
			CleanupObjects();
			SpawnPlayer();
			SpawnStartChunks();
			mode = GameMode.PAUSE;
			spawnedPlayerController.SetTargetSpeed(playerGameplay.maxSpeed);
			spawnedPlayerController.SetCurrentSpeed(0f);
			coins = 0;
			endGameTimer = endGameWait;
			hasEnded = false;
			SpawnGhosts();
		}

		public void SetLevelData(List<ChunkController> chunksList)
		{
			levelData = new List<ChunkController>();
			for (int i = 0; i < chunksList.Count; i++)
			{
				levelData.Add(GetChunkByIndex(chunksList[i].ChunkIndex));
			}
		}

		private void AddToLevelData(ChunkController chunk)
		{
			if (isFirstPlayer)
			{
				levelData.Add(chunk);
			}
		}

		private void AddToLevelDataByIndex(int index)
		{
			levelData.Add(GetChunkByIndex(index));
		}

		private void SpawnPlayer()
		{
			spawnedPlayer = (GameObject)UnityEngine.Object.Instantiate(playerPrefab, playerStartPos + gamePrefab.transform.position, Quaternion.identity);
			cleanupObjects.Add(spawnedPlayer);
			spawnedPlayer.transform.parent = gamePrefab.transform;
			spawnedPlayerController = spawnedPlayer.GetComponent<RunnerController>();
			cameraController.SetPlayer(spawnedPlayer.transform);
			spawnedPlayerController.SetSwid(GameController.PlayerId);
		}

		private void SpawnGhosts()
		{
			ghostList.Clear();
			float num = GHOST_OFFSET + 1f;
			otherPlayerGhosts.Sort(new RunnerRecorderReverseComparer());
			int num2 = 0;
			for (int i = 0; i < otherPlayerGhosts.Count; i++)
			{
				RunnerRecorder runnerRecorder = otherPlayerGhosts[i];
				if (runnerRecorder.playerId != GameController.PlayerId && runnerRecorder.ghostData != null && runnerRecorder.ghostData.Count > 0)
				{
					SpawnGhost(runnerRecorder, num, runnerRecorder.playerId);
					num += GHOST_OFFSET;
					num2++;
					if (num2 >= 2)
					{
						break;
					}
				}
			}
			SetGhostsIgnoreCollisions();
		}

		private void SpawnGhost(RunnerRecorder aData, float aOffset, string aSwid)
		{
			GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(ghostPrefab, playerStartPos + gamePrefab.transform.position, Quaternion.identity);
			cleanupObjects.Add(gameObject);
			gameObject.transform.parent = gamePrefab.transform;
			GhostController component = gameObject.GetComponent<GhostController>();
			component.ghostData = aData;
			component.PlaceGhostOnZ(aOffset);
			component.SetSwid(aSwid);
			ghostList.Add(gameObject);
		}

		private void SetGhostsIgnoreCollisions()
		{
			if (avatarColliders == null)
			{
				avatarColliders = new List<PolygonCollider2D>();
				avatarColliders.Add(spawnedPlayer.GetComponent<PolygonCollider2D>());
				foreach (GameObject ghost in ghostList)
				{
					avatarColliders.Add(ghost.GetComponent<PolygonCollider2D>());
				}
			}
			for (int i = 0; i < avatarColliders.Count - 1; i++)
			{
				for (int j = i + 1; j < avatarColliders.Count; j++)
				{
					Physics2D.IgnoreCollision(avatarColliders[i], avatarColliders[j]);
					Physics2D.IgnoreCollision(avatarColliders[j], avatarColliders[i]);
				}
			}
		}

		private void SpawnChunk(ChunkController aChunk, bool isFirst = false)
		{
			GameObject gameObject = (GameObject)UnityEngine.Object.Instantiate(aChunk.gameObject, chunkSpawner + gamePrefab.transform.position, Quaternion.identity);
			gameObject.name = aChunk.name;
			cleanupObjects.Add(gameObject);
			gameObject.transform.parent = gamePrefab.transform;
			ChunkController component = gameObject.GetComponent<ChunkController>();
			if (lastChunkController != null)
			{
				lastChunkController.nextChunk = gameObject;
			}
			if (isFirst)
			{
				gameObject.transform.localPosition = chunk1Pos;
			}
			else
			{
				gameObject.transform.localPosition = lastChunk.transform.localPosition + lastChunkController.endLink;
			}
			lastChunk = gameObject;
			lastChunkController = component;
			chunkIndex++;
		}

		public void EnterBuildMode()
		{
			PlaySound("ButtonUI", SOUND_PREFIX);
			RunnerUIPanel currentUIPanel = uiController.CurrentUIPanel;
			currentUIPanel.OnHideComplete = (Action)Delegate.Combine(currentUIPanel.OnHideComplete, (Action)delegate
			{
				mode = GameMode.BUILD;
				levelBuilder.gameObject.SetActive(true);
				transitionPanel.Open();
			});
			uiController.LoadBuildUI();
			GameController.LogEvent(GameLogEventType.ACTION, "runner_choose_create");
		}

		public void EnterFirstPlayerMode()
		{
			environment.SetActive(false);
			uiController.LoadFirstPlayerUI();
		}

		public void EnterPlayMode()
		{
			buildBackground.gameObject.SetActive(false);
			environment.SetActive(true);
			if (uiController.CurrentUIPanel != null)
			{
				RunnerUIPanel currentUIPanel = uiController.CurrentUIPanel;
				currentUIPanel.OnHideComplete = (Action)Delegate.Combine(currentUIPanel.OnHideComplete, (Action)delegate
				{
					NewGame();
					transitionPanel.Open();
					PlaySound("ScreenOpenUI", SOUND_PREFIX);
				});
			}
			else
			{
				NewGame();
				transitionPanel.Open();
			}
			uiController.LoadPlayUI();
		}

		private void SpawnStartChunks()
		{
			if (chunks.Count == 0)
			{
				Debug.LogWarning("Not enough chunks to spawn");
				return;
			}
			SpawnChunk(startChunk, true);
			spawnedPlayerController.SetCheckpoint(startChunk.gameObject);
			foreach (ChunkController levelDatum in levelData)
			{
				SpawnChunk(levelDatum);
			}
			SpawnChunk(endChunk);
		}

		public void SaveRandomChunks(int amt)
		{
			int maxDifficulty = Mathf.RoundToInt(maxChunkDifficultyForSessionNumber.Evaluate(GameController.NumberOfGamesCompleted));
			for (int i = 0; i < amt; i++)
			{
				ChunkController randomChunk = GetRandomChunk(1, maxDifficulty);
				AddToLevelData(randomChunk);
			}
		}

		public ChunkController GetChunkByIndex(int index)
		{
			return chunks[index];
		}

		public ChunkController GetChunkByName(string name)
		{
			for (int i = 0; i < chunks.Count; i++)
			{
				if (chunks[i].name.Equals(name))
				{
					return chunks[i];
				}
			}
			return null;
		}

		private ChunkController GetRandomChunk()
		{
			return chunks[UnityEngine.Random.Range(0, chunks.Count)];
		}

		private ChunkController GetRandomChunk(int minDifficulty, int maxDifficulty)
		{
			List<ChunkController> list = new List<ChunkController>();
			for (int i = minDifficulty; i <= maxDifficulty; i++)
			{
				if (mChunksByDifficulty.ContainsKey(i))
				{
					list.AddRange(mChunksByDifficulty[i]);
				}
			}
			return list[UnityEngine.Random.Range(0, list.Count)];
		}

		private ChunkController GetNextChunk()
		{
			return levelData[chunkIndex];
		}

		public void DnaCallback(bool aIsSuccess, string cacheId, AvatarTextureData textureData)
		{
			if (aIsSuccess)
			{
			}
		}

		private void ManagePowerups()
		{
			if (isMagnetOn)
			{
				magnetTimer -= Time.deltaTime;
				if (magnetTimer <= 0f)
				{
					isMagnetOn = false;
					spawnedPlayerController.SetMagnetCollider(false);
				}
			}
		}

		private void ManageDamagePause()
		{
			damageTimer -= Time.deltaTime;
			if (damageTimer <= 0f && (bool)spawnedPlayerController && (bool)cameraController)
			{
				Pause(GameMode.PLAY);
				spawnedPlayerController.Respawn();
				cameraController.SetFollowPlayer(true);
				spawnedPlayerController.SetTargetSpeed(0f);
				spawnedPlayerController.SetRunParticle(true);
				PlaySound("PlayerRespawn", SOUND_PREFIX);
			}
		}

		private void ManageEndWait()
		{
			if (endGameTimer < 0f && !hasEnded)
			{
				uiController.LoadEndUI();
				hasEnded = true;
			}
			else
			{
				endGameTimer -= Time.deltaTime;
			}
		}

		public void SetPowerup(Powerup p)
		{
			if (p == Powerup.MAGNET)
			{
				isMagnetOn = true;
				magnetTimer = magnetMax;
				spawnedPlayerController.SetMagnetCollider(true);
			}
		}

		public GameObject GetSpawnedPlayer()
		{
			return spawnedPlayer;
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(playerStartPos + base.transform.position, gizmoSize);
		}

		private void CleanupObjects()
		{
			foreach (GameObject cleanupObject in cleanupObjects)
			{
				UnityEngine.Object.Destroy(cleanupObject);
			}
		}

		public void PassCheckpoint()
		{
			PlaySound("CollectCoin", SOUND_PREFIX);
			checkpointsPassed++;
		}

		public void HitObstacle()
		{
			currentChunkAttempts++;
			if (currentChunkAttempts >= MAX_CHUNK_ATTEMPTS)
			{
				spawnedPlayerController.SwapNextCheckpoint();
				ResetChunkAttempts();
			}
			lives--;
			Vector3 vector = spawnedPlayerController.GetCurrentChunk().transform.InverseTransformPoint(spawnedPlayerController.transform.position);
			string aGameMessage = string.Format("{0}|{1}|{2}", spawnedPlayerController.GetCurrentChunk().name, Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y));
			GameController.LogEvent(GameLogEventType.ACTION, "runner_die", aGameMessage);
			if (lives <= 0)
			{
				EndGame();
			}
			else
			{
				cameraController.Interp(spawnedPlayerController.GetPosLastCheckpoint(), playerGameplay.respawnInterpDuration, playerGameplay.preCameraMoveWait);
			}
			PlaySound("PlayerHitSpike", SOUND_PREFIX);
		}

		public void ResetChunkAttempts()
		{
			currentChunkAttempts = 0;
		}

		public void SetDamagePause()
		{
			Pause(GameMode.DAMAGE);
		}

		public void ResumeAfterDelay()
		{
			if (checkpointsPassed > 0)
			{
				StartCoroutine(ResumeAfterDelayRoutine());
			}
			else
			{
				Pause(GameMode.RESUME);
			}
		}

		public IEnumerator ResumeAfterDelayRoutine()
		{
			if (mode != GameMode.PAUSE)
			{
				yield break;
			}
			PlayUIPanel playPanel = uiController.CurrentUIPanel as PlayUIPanel;
			if (playPanel != null)
			{
				for (int i = 3; i > 0; i--)
				{
					playPanel.ShowResumeCountdown(i);
					yield return new WaitForSeconds(1f);
				}
				playPanel.HideResumeCountdown();
			}
			Pause(GameMode.RESUME);
		}

		public void Pause(GameMode aMode)
		{
			if (aMode == mode)
			{
				return;
			}
			switch (aMode)
			{
			case GameMode.PLAY:
				mode = aMode;
				DOTween.PlayAll();
				break;
			case GameMode.PAUSE:
				prePausedMode = mode;
				mode = aMode;
				DOTween.PauseAll();
				if ((bool)spawnedPlayerController)
				{
					spawnedPlayerController.PausePlayer();
				}
				break;
			case GameMode.DAMAGE:
				mode = aMode;
				if ((bool)spawnedPlayerController)
				{
					damageTimer = playerGameplay.damageWait;
				}
				break;
			case GameMode.RESUME:
				mode = prePausedMode;
				DOTween.PlayAll();
				if ((bool)spawnedPlayerController)
				{
					spawnedPlayerController.UnPausePlayer();
					SetGhostsIgnoreCollisions();
				}
				break;
			}
		}

		public void Collect(GameObject collectible)
		{
			CollectibleController component = collectible.GetComponent<CollectibleController>();
			if (component != null)
			{
				component.CollectionAction();
			}
		}

		public void AddScore(int amt)
		{
			coins += amt;
		}

		public void RemoveScore(int amt)
		{
			if (coins >= amt)
			{
				coins -= amt;
			}
			else
			{
				coins = 0;
			}
		}

		public void OnPlayButton()
		{
			mode = GameMode.PLAY;
			spawnedPlayerController.PlayRun();
			foreach (GameObject ghost in ghostList)
			{
				ghost.GetComponent<AvatarController>().PlayRun();
			}
			HasThePlayerJumpedAtLeastOnce = false;
			spawnedPlayerController.SetRunParticle(true);
		}

		public void OnRandomBuildButton()
		{
			SaveRandomChunks(CHUNK_PIECES);
			AverageDifficulty();
			EnterPlayMode();
			PlaySound("ButtonUI", SOUND_PREFIX);
		}

		private void AverageDifficulty()
		{
			foreach (ChunkController levelDatum in levelData)
			{
				avgDifficulty += levelDatum.difficulty;
			}
			avgDifficulty /= levelData.Count;
		}

		public void OnBuilderFinish()
		{
			AverageDifficulty();
			TransitionPanel obj = transitionPanel;
			obj.OnCloseComplete = (Action)Delegate.Combine(obj.OnCloseComplete, new Action(EndBuilder));
			transitionPanel.Close();
			string text = string.Empty;
			for (int i = 0; i < levelData.Count; i++)
			{
				if (i > 0 && i < levelData.Count - 1)
				{
					text += "|";
				}
				text += levelData[i].name;
			}
			GameController.LogEvent(GameLogEventType.ACTION, "runner_create_level", text);
		}

		private void EndBuilder()
		{
			TransitionPanel obj = transitionPanel;
			obj.OnCloseComplete = (Action)Delegate.Remove(obj.OnCloseComplete, new Action(EndBuilder));
			levelBuilder.gameObject.SetActive(false);
			EnterPlayMode();
			transitionPanel.Open();
			PlaySound("ScreenCloseUI", SOUND_PREFIX);
		}

		private List<string> GetLevelDataAsStrings()
		{
			List<string> list = new List<string>();
			for (int i = 0; i < levelData.Count; i++)
			{
				list.Add(levelData[i].name);
			}
			return list;
		}

		public void OnPostButton()
		{
			gameData = GameController.GetGameData<RunnerData>();
			if (isFirstPlayer)
			{
				gameData.LevelData = GetLevelDataAsStrings();
				gameData.Difficulty = avgDifficulty;
				gameData.GameCreatorGemCount = checkpointsPassed;
				CreateResponse(gameData);
				gameData.Status = MixGameSessionStatus.COMPLETE;
				GameController.GameOver(gameData);
			}
			else
			{
				RunnerResponse runnerResponse = CreateResponse(gameData);
				GameController.GameOver(runnerResponse);
			}
		}

		private RunnerResponse CreateResponse(RunnerData aGameData)
		{
			RunnerResponse runnerResponse = aGameData.CreateNewReponse(aGameData.Responses, GameController.PlayerId);
			runnerResponse.Ghost = spawnedPlayerController.playerRecording.Formatted();
			runnerResponse.Checkpoints = checkpointsPassed;
			runnerResponse.Attempts++;
			return runnerResponse;
		}

		public void CrossFinishLine()
		{
			if (!spawnedPlayerController.isDamaged && mode == GameMode.PLAY)
			{
				EndGame();
			}
			BaseGameController.Instance.Session.SessionSounds.SetSwitchEvent("Runner/MUS/Main", "EndGameStinger", base.gameObject);
		}

		private void EndGame()
		{
			mode = GameMode.END;
			spawnedPlayerController.End();
			spawnedPlayerController.SetRunParticle(false);
			BaseGameController.Instance.Session.SessionSounds.SetSwitchEvent("Runner/MUS/Main", "EndGameStinger", base.gameObject);
			string aGameMessage = string.Format("{0}|{1}|{2}", checkpointsPassed, maxLives - lives, avgDifficulty);
			GameController.LogEvent(GameLogEventType.ACTION, "runner_finish_game", aGameMessage);
		}

		public static void PlaySound(string aSoundName, string aPrefix)
		{
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent(aPrefix + aSoundName);
		}

		private void SetupScreenBounds()
		{
			Camera componentInChildren = cameraController.GetComponentInChildren<Camera>();
			mMinScreenPoint = componentInChildren.ViewportToScreenPoint(Vector3.zero);
			mMaxScreenPoint = componentInChildren.ViewportToScreenPoint(Vector3.one);
		}

		public bool IsInputInViewport()
		{
			Vector3 mousePosition = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;
			return mousePosition.x >= mMinScreenPoint.x && mousePosition.x <= mMaxScreenPoint.x && mousePosition.y >= mMinScreenPoint.y && mousePosition.y <= mMaxScreenPoint.y;
		}
	}
}
