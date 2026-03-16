using System;
using System.Collections;
using System.Collections.Generic;
using Mix.Games.Data;
using Mix.Games.Message;
using Mix.Games.Session;
using Mix.Games.Tray;
using UnityEngine;
using UnityEngine.UI; // <-- Added for Graphic UI components

namespace Mix.Games
{
	public abstract class BaseGameController : MonoBehaviour
	{
		public GameObject MixGameObject;

		public Camera MixGameCamera;

		public IMixGame MixGame;

		public IMixGameTimer MixGameTimer;

		public GameAudio AudioManager;

		protected MixGameData mGameData;

		protected GameSession mSession;

		protected int mGameClock;

		protected float mTimeRemaining = 30f;

		protected IEnumerator mCountdown;

		protected static BaseGameController mInstance;

		private float mTimeInterval;

		public static BaseGameController Instance
		{
			get
			{
				return mInstance;
			}
		}

		public string FriendName
		{
			get
			{
				return mSession.GetFriendName();
			}
		}

		public string PlayerId
		{
			get
			{
				return mSession.PlayerId;
			}
		}

		public string PlayerName
		{
			get
			{
				return mSession.GetPlayerName(mSession.PlayerId);
			}
		}

		public string OwnerId
		{
			get
			{
				if (mSession.MessageData != null)
				{
					return mSession.MessageData.SenderId;
				}
				Debug.LogWarning("No message data for this session");
				return string.Empty;
			}
		}

		public int NumberOfGamesCompleted
		{
			get
			{
				return mSession.GetNumberOfCompletedSessions();
			}
		}

		public bool IsGroupSession
		{
			get
			{
				return mSession.IsGroupSession;
			}
		}

		public GameSession Session
		{
			get
			{
				return mSession;
			}
		}

		private void Awake()
		{
			Debug.Log("[BaseGameController] Awake called");
		}

		public void CancelBundles()
		{
			Debug.Log("[BaseGameController] CancelBundles called");
			mSession.CancelBundles();
		}

		public void CancelBundles(string aUrl)
		{
			Debug.Log($"[BaseGameController] CancelBundles called for URL: {aUrl}");
			mSession.CancelBundles(aUrl);
		}

		public virtual void CleanUpGame()
		{
			Debug.Log("[BaseGameController] CleanUpGame called");
			if (MixGame != null)
			{
				MixGame.Quit();
			}
		}

		public virtual void CloseGame()
		{
			Debug.Log("[BaseGameController] CloseGame called");
			if (mSession != null)
			{
				mSession.QuitSession();
			}
		}

		public void DestroyBundleInstance(string aPath, UnityEngine.Object aObject = null)
		{
			Debug.Log($"[BaseGameController] DestroyBundleInstance called for path: {aPath}");
			mSession.DestroyBundleInstance(aPath, aObject);
		}

		public virtual void PauseOnNetworkError(bool retryGame = false)
		{
			Debug.Log($"[BaseGameController] PauseOnNetworkError called with retryGame: {retryGame}");
			mSession.PauseOnNetworkError();
			if (retryGame)
			{
				float time = 3f;
				Invoke("RetryGame", time);
			}
		}

		private void RetryGame()
		{
			Debug.Log("[BaseGameController] RetryGame called");
			mSession.ResumeNetworkError();
		}

		public UnityEngine.Object GetBundleInstance(string aPath)
		{
			Debug.Log($"[BaseGameController] GetBundleInstance called for path: {aPath}");
			return mSession.GetBundleInstance(aPath);
		}

		public void LoadAsset(IGameAsset aSessionAsset, string aPath, object aParam = null)
		{
			Debug.Log($"[BaseGameController] LoadAsset called for path: {aPath}");
			mSession.LoadAsset(aSessionAsset, aPath, aParam);
		}

		public void LoadData(IMixGameDataRequest aGameDataRequester, string aPath, string aFileName, Func<string, object> aMethod)
		{
			Debug.Log($"[BaseGameController] LoadData called for path: {aPath} | filename: {aFileName}");
			mSession.LoadData(aGameDataRequester, aPath, aFileName, aMethod);
		}

		public void LoadFriend(GameObject aMesh, string aPlayerId, bool aHideCostumes = false, bool aHideGeoAcessories = false)
		{
			Debug.Log($"[BaseGameController] LoadFriend called for Player ID: {aPlayerId}");
			mSession.LoadFriend(aMesh, aPlayerId, aHideCostumes, aHideGeoAcessories);
		}

		public void LoadRandomFriend(GameObject aMesh)
		{
			Debug.Log("[BaseGameController] LoadRandomFriend called");
			mSession.LoadRandomFriend(aMesh);
		}

		public void LoadSnapshot(string aPlayerId, int size, Action<bool, Sprite> callback)
		{
			Debug.Log($"[BaseGameController] LoadSnapshot called for Player ID: {aPlayerId} | size: {size}");
			mSession.LoadSnapshot(aPlayerId, size, callback);
		}

		public void LogEvent(GameLogEventType aEventType, object aGameParameter, object aGameMessage = null)
		{
			Debug.Log($"[BaseGameController] LogEvent called - EventType: {aEventType}");
			if (mSession != null)
			{
				switch (aEventType)
				{
					case GameLogEventType.ACTION:
						mSession.LogAction((string)aGameParameter, aGameMessage);
						break;
					case GameLogEventType.PAGEVIEW:
						mSession.LogPageView((string)aGameParameter);
						break;
					case GameLogEventType.TIMING:
						mSession.LogTiming((double)aGameParameter);
						break;
				}
			}
		}

		public string GetFriendName(string aPlayerId)
		{
			Debug.Log($"[BaseGameController] GetFriendName called for Player ID: {aPlayerId}");
			return mSession.GetPlayerName(aPlayerId);
		}

		public T GetGameData<T>() where T : MixGameData
		{
			Debug.Log("[BaseGameController] GetGameData<T> called");
			return (T)mGameData;
		}

		public virtual void GameOver(object gameData)
		{
			Debug.Log("[BaseGameController] GameOver called");
			mSession.GameStatistics.LastPlayed = DateTime.Now;
			mSession.GameStatistics.SessionsCompleted++;
			if (gameData is MixGameData)
			{
				mSession.EndSession(gameData);
			}
			else if (gameData is MixGameResponse)
			{
				mSession.UpdateSession(mGameData, gameData as MixGameResponse);
			}
		}

		public string GetLocalizedString(string aToken)
		{
			Debug.Log($"[BaseGameController] GetLocalizedString called for token: {aToken}");
			return mSession.GetLocalizedString(aToken);
		}

		public abstract void SetupGameData();

		public abstract void SetupGameData(MixGameData aData);

		public abstract void SetupGameData(string aGameDataJson);

		public virtual void Initialize(GameSession aSession)
		{
			Debug.Log("[BaseGameController] Initialize(GameSession) called");
			mInstance = this;
			if (aSession.MessageData is IGameEventMessageData)
			{
				Dictionary<string, object> state = (aSession.MessageData as IGameEventMessageData).State;
				string aGameDataJson = state["GameData"] as string;
				SetupGameData(aGameDataJson);
			}
			else
			{
				SetupGameData(aSession.MessageData.GetJson());
			}
			mSession = aSession;
			MixGame = (IMixGame)MixGameObject.GetComponent(typeof(IMixGame));
			MixGame.Initialize(mGameData);
			mSession.GameStatistics.SessionsStarted++;

			// Start delay for shaders via GameManager since this GameObject might be inactive
			if (MonoSingleton<GameManager>.Instance != null)
			{
				MonoSingleton<GameManager>.Instance.StartCoroutine(ApplyShaderAfterDelay(MixGameObject != null ? MixGameObject : gameObject, 0.2f));
			}
		}

		public virtual void Initialize(GameSession aSession, string aEntitlementId)
		{
			Debug.Log($"[BaseGameController] Initialize(GameSession, string) called | Entitlement: {aEntitlementId}");
			mInstance = this;
			SetupGameData();
			mGameData.Entitlement = aEntitlementId;
			mSession = aSession;
			MixGame = (IMixGame)MixGameObject.GetComponent(typeof(IMixGame));
			MixGame.Initialize();
			mSession.GameStatistics.SessionsStarted++;

			// Start delay for shaders via GameManager since this GameObject might be inactive
			if (MonoSingleton<GameManager>.Instance != null)
			{
				MonoSingleton<GameManager>.Instance.StartCoroutine(ApplyShaderAfterDelay(MixGameObject != null ? MixGameObject : gameObject, 0.2f));
			}
		}

		public virtual void Initialize(GameSession aSession, MixGameData aData)
		{
			Debug.Log("[BaseGameController] Initialize(GameSession, MixGameData) called");
			mInstance = this;
			SetupGameData(aData);
			mSession = aSession;
			MixGame = (IMixGame)MixGameObject.GetComponent(typeof(IMixGame));
			MixGame.Initialize(aData);
			mSession.GameStatistics.SessionsStarted++;

			// Start delay for shaders via GameManager since this GameObject might be inactive
			if (MonoSingleton<GameManager>.Instance != null)
			{
				MonoSingleton<GameManager>.Instance.StartCoroutine(ApplyShaderAfterDelay(MixGameObject != null ? MixGameObject : gameObject, 0.2f));
			}
		}

		private IEnumerator ApplyShaderAfterDelay(GameObject targetInstance, float delaySeconds)
		{
			yield return new WaitForSeconds(delaySeconds);

			if (targetInstance != null && !targetInstance.IsNullOrDisposed())
			{
				Debug.Log($"[BaseGameController] {delaySeconds} seconds have passed. Searching for UI root to replace shaders near: {targetInstance.name}");

				// Get root
				Transform rootTransform = targetInstance.transform.root;

				// We define a local helper function so we can re-evaluate the hierarchy whenever we want.
				Action scanAndReplace = () =>
				{
					if (rootTransform == null || rootTransform.gameObject == null) return;

					GameObject uiRoot = null;
					Transform[] allTransforms = rootTransform.GetComponentsInChildren<Transform>(true);

					foreach (Transform t in allTransforms)
					{
						if (t.name.Equals("ui", StringComparison.OrdinalIgnoreCase))
						{
							uiRoot = t.gameObject;
							break;
						}
					}

					if (uiRoot == null)
					{
						uiRoot = targetInstance;
					}

					Canvas[] canvases = uiRoot.GetComponentsInChildren<Canvas>(true);

					List<Graphic> allGraphicsToPatch = new List<Graphic>();
					List<Renderer> allRenderersToPatch = new List<Renderer>();

					if (canvases.Length > 0)
					{
						foreach (Canvas c in canvases)
						{
							allGraphicsToPatch.AddRange(c.GetComponentsInChildren<Graphic>(true));
							allRenderersToPatch.AddRange(c.GetComponentsInChildren<Renderer>(true));
						}
					}
					else
					{
						allGraphicsToPatch.AddRange(uiRoot.GetComponentsInChildren<Graphic>(true));
						allRenderersToPatch.AddRange(uiRoot.GetComponentsInChildren<Renderer>(true));
					}

					Shader optimizedUIShader = Shader.Find("Custom/UI/OptimizedUIShader");
					if (optimizedUIShader != null)
					{
						int graphicCount = 0;
						foreach (Graphic graphic in allGraphicsToPatch)
						{
							if (graphic != null && graphic.material != null && graphic.material.shader != optimizedUIShader)
							{
								Material newMat = new Material(graphic.material);
								newMat.shader = optimizedUIShader;
								graphic.material = newMat;
								graphicCount++;
							}
						}

						int rendererCount = 0;
						foreach (Renderer rend in allRenderersToPatch)
						{
							if (rend != null && rend.sharedMaterials != null)
							{
								bool changed = false;
								Material[] newMaterials = new Material[rend.sharedMaterials.Length];
								for (int i = 0; i < rend.sharedMaterials.Length; i++)
								{
									if (rend.sharedMaterials[i] != null && rend.sharedMaterials[i].shader != optimizedUIShader)
									{
										if (!rend.sharedMaterials[i].name.Contains("Standard"))
										{
											Material repMat = new Material(rend.sharedMaterials[i]);
											repMat.shader = optimizedUIShader;
											newMaterials[i] = repMat;
											changed = true;
										}
										else
										{
											newMaterials[i] = rend.sharedMaterials[i];
										}
									}
									else
									{
										newMaterials[i] = rend.sharedMaterials[i];
									}
								}

								if (changed)
								{
									rend.sharedMaterials = newMaterials;
									rendererCount++;
								}
							}
						}

						if (graphicCount > 0 || rendererCount > 0)
						{
							Debug.Log($"[BaseGameController] Shader update completed. Patched {graphicCount} UI Graphics, {rendererCount} Renderers.");
						}
					}
				};

				// Initial run
				scanAndReplace();

				// Store the current number of children at the root.
				int lastChildCount = -1;

				// Continuously poll while the instance exists to see if the structure dynamically changed
				while (targetInstance != null && !targetInstance.IsNullOrDisposed() && rootTransform != null)
				{
					int currentChildCount = rootTransform.GetComponentsInChildren<Transform>(true).Length;

					// If the hierarchy beneath the root grew or shrank (meaning prefabs were dynamically 
					// instantiated or deleted), re-run the shader patching logic.
					if (currentChildCount != lastChildCount)
					{
						if (lastChildCount != -1)
						{
							Debug.Log($"[BaseGameController] Hierarchy change detected (Children: {lastChildCount} -> {currentChildCount}). Rerunning Shader Patch.");
						}

						lastChildCount = currentChildCount;
						scanAndReplace();
					}

					yield return new WaitForSeconds(1.0f); // check every 1 sec
				}
			}
		}

		public virtual void PlayGame()
		{
			Debug.Log("[BaseGameController] PlayGame called");
			mSession.UpdateGameSessionState(GameSessionState.PLAYING);
			MixGame.Play();
		}

		public virtual void PauseGame()
		{
			Debug.Log("[BaseGameController] PauseGame called");
			if (mSession != null && MixGame != null)
			{
				mSession.UpdateGameSessionState(GameSessionState.PAUSED);
				MixGame.Pause();
			}
			if (MixGameTimer != null)
			{
				StopCountdown();
			}
		}

		public void PreloadAvatar(string aPlayerId = null)
		{
			Debug.Log($"[BaseGameController] PreloadAvatar called for Player ID: {aPlayerId}");
			mSession.PreloadAvatar(aPlayerId);
		}

		public virtual void QuitGame()
		{
			Debug.Log("[BaseGameController] QuitGame called");
			if (MixGame != null)
			{
				MixGame.Quit();
			}
			UnityEngine.Object.Destroy(base.gameObject);
		}

		public virtual void ResumeGame()
		{
			Debug.Log("[BaseGameController] ResumeGame called");
			mSession.UpdateGameSessionState(GameSessionState.PLAYING);
			MixGame.Resume();
			if (MixGameTimer != null)
			{
				ResumeCountdown();
			}
		}

		public void ResumeCountdown()
		{
			Debug.Log("[BaseGameController] ResumeCountdown called");
			mCountdown = CountdownGame(mTimeRemaining);
			StartCoroutine(mCountdown);
		}

		public void StartCountdown(int aTime = 30, float aTimeInterval = 1f)
		{
			Debug.Log($"[BaseGameController] StartCountdown called with Time: {aTime} | Interval: {aTimeInterval}");
			mTimeInterval = aTimeInterval;
			MixGameTimer = MixGame as IMixGameTimer;
			if (MixGameTimer == null)
			{
				mSession.Logger.LogError("IMixGameTimer not implemented. Unable to start game countdown");
				return;
			}
			mGameClock = aTime;
			mCountdown = CountdownGame(mGameClock);
			StartCoroutine(mCountdown);
		}

		public void StopCountdown()
		{
			Debug.Log("[BaseGameController] StopCountdown called");
			if (mCountdown != null && !mCountdown.Equals(null))
			{
				StopCoroutine(mCountdown);
			}
		}

		public float GetCurrentTime()
		{
			Debug.Log("[BaseGameController] GetCurrentTime called");
			return mTimeRemaining;
		}

		private IEnumerator CountdownGame(float aTimeRemaining)
		{
			Debug.Log($"[BaseGameController] CountdownGame started with remaining time: {aTimeRemaining}");
			for (float i = aTimeRemaining; i >= 0f; i -= mTimeInterval)
			{
				mTimeRemaining = i;
				yield return new WaitForSeconds(mTimeInterval);
				if (i == (float)mGameClock)
				{
					MixGameTimer.GameTimerStart();
				}
				else if (i == 0f)
				{
					MixGameTimer.GameTimerComplete();
				}
				else
				{
					MixGameTimer.GameTimerProgress(i);
				}
			}
			Debug.Log("[BaseGameController] CountdownGame finished");
		}

		public void ModerateText(string aTextToModerate, IGameModerationResult aGame, object aUserData)
		{
			Debug.Log($"[BaseGameController] ModerateText called for text: {aTextToModerate}");
			mSession.ModerateText(aTextToModerate, aGame, aUserData);
		}
	}
}
