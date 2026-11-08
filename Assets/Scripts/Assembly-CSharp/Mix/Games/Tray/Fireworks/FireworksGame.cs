using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DG.Tweening;
using Mix.Assets;
using Mix.Games.Data;
using Mix.Games.Session;
using Mix.Games.Tray.Common;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Fireworks
{
	public class FireworksGame : MonoBehaviour, IMixGame, IMixGameTimer, IGameAsset, IAssetReferencer
	{
		public enum FireworksStage
		{
			SceneSelection = 0,
			Loading = 1,
			Creation = 2,
			EnterMessage = 3,
			ShowTime = 4,
			End = 5,
			Idle = 6
		}

		private const float mCOUNTER_MAX_TIME = 4f;

		private const int MAX_FIREWORKS = 256;

		public FireworksController GameController;

		public float SpawnZ = 35f;

		public int NumOfParticularFireworks = 5;

		public FireworkF MessageFirework;

		public float FireworkDistance = 15f;

		public GameObject CountdownTextObject;

		public GameObject CountdownInputBlocker;

		public GameObject SongCountdownBar;

		public Image PlaybackProgressBar;

		public GameObject FireworkButtonContainer;

		public GameObject FireworkButtonPrefab;

		public GameObject MessageTextInput;

		public PersonalMessageScreenManager FireworksTextInput;

		public GameObject SaveMessageMixButton;

		public GameObject SaveMessageFireworksButton;

		public GameObject Background;

		public GameObject CreationPanel;

		public GameObject MessageInputPanel;

		public Vector3 MessageStartPoint;

		public float TapAllowanceTime;

		public float GestureRange;

		public Vector2 SpawnScaler;

		public float SpawnYOffsetPercentage;

		public float RColorLerpSpeed;

		public ScrollRectHorizontalSnap ScrollRect;

		public Light PointLight;

		public float ModifyLightDecayVal = 1f;

		public GameObject SceneSelectionPanel;

		public PlaybackLoader PlaybackLoaderScreen;

		public string GestureSoundEvent;

		public string ReactionSoundAmazedEvent;

		public List<string> ReactionSoundEvents;

		public float ReactionRate = 0.2f;

		public List<float> TimeChanges = new List<float>();

		public List<float> RateChanges = new List<float>();

		public GameObject AnimatedHand;

		public float GLerpSpeed = 0.75f;

		public GameObject SceneParent;

		public bool SkipTutorialInDebugScene;

		protected bool mIsPaused;

		private FireworksData mData;

		private float mCounterTime;

		private int mCounterTracker;

		private float mCurrentSongTime;

		private float mMaxSongTime;

		private Queue<KeyValuePair<float, KeyValuePair<int, Vector3>>> mLaunchedFireworks = new Queue<KeyValuePair<float, KeyValuePair<int, Vector3>>>();

		private List<KeyValuePair<float, KeyValuePair<int, Vector3>>> mSavedFireworks = new List<KeyValuePair<float, KeyValuePair<int, Vector3>>>();

		private Scene mChosenScene;

		private string mChosenSong = string.Empty;

		private int mGestureCounter;

		private string mMessage = string.Empty;

		private BoxCollider mFireworksSpawnBox;

		private bool mIsTrackingGesture;

		private float mGestureStartTime;

		private Vector3 mGestureStartPosition = default(Vector3);

		private bool mIsInputinGestureRange;

		private bool mIsSongCountdownStarted;

		private float mTimerStartTime;

		private GameObject mPhysicalMessageFirework;

		private bool mIsGestureInitialized;

		private float mTapTimerMax;

		private float mTapTimer;

		private bool mIsForcingGesture;

		private bool mIsFinishedLaunching;

		private bool mIsSongStarted;

		private float mRColorLerpTime;

		private float mScreenXWidth;

		private bool mIsFinaleComing;

		private float mSongFinaleTime;

		private Color mCountdownBarInitColor;

		private Queue<float> mCrowdReactionTimes = new Queue<float>();

		[HideInInspector]
		public Material mCastleMaterial;

		private float mNumFireworksLaunchedThisFrame;

		private int mReactionIndex;

		private string mSceneBundleUrl;

		public Gesture mCurrentGesture { get; set; }

		public FireworksStage mGameStage { get; set; }

		private bool mIsGesturePlaying { get; set; }

		public float CurrentTimestamp
		{
			get
			{
				return Mathf.Clamp(mMaxSongTime - mCurrentSongTime, 0f, mMaxSongTime);
			}
		}

		void IMixGame.Initialize(MixGameData aData)
		{
			Toolbox.Instance = new Toolbox();
			Toolbox.Instance.mFireworkGame = this;
			ScrollRect.Initialize();
			Toolbox.Instance.mFireworkManager.mController = GameController;
			Toolbox.Instance.mFireworkManager.SetMaxNumberofParticularFireworks(NumOfParticularFireworks);
			Toolbox.Instance.mPlaybackAssembler.mZValue = SpawnZ;
			Toolbox.Instance.Initialize();
			mCurrentSongTime = 0f;
			mMaxSongTime = 0f;
			mFireworksSpawnBox = GetComponent<BoxCollider>();
			mScreenXWidth = GameController.MixGameCamera.ScreenToWorldPoint(new Vector3(GameController.MixGameCamera.pixelWidth, GameController.MixGameCamera.pixelHeight)).x;
			mFireworksSpawnBox.size = new Vector3(mFireworksSpawnBox.size.x, mFireworksSpawnBox.size.y, mFireworksSpawnBox.size.z);
			mFireworksSpawnBox.center = new Vector3(mFireworksSpawnBox.bounds.center.x, mFireworksSpawnBox.bounds.center.y, mFireworksSpawnBox.bounds.center.z);
			if (DebugSceneIndicator.IsDebugScene)
			{
				TestEntitlementGameData entitlement = new TestEntitlementGameData("gogs/fireworks/game_assets", "fireworks", false);
				BaseGameController.Instance.Session.Entitlement = entitlement;
			}
			if (aData != null)
			{
				mData = GameController.GetGameData<FireworksData>();
				ReadyPlayback(mData);
			}
			else
			{
				ChooseFirstScene();
			}
		}

		void IMixGame.Play()
		{
		}

		void IMixGame.Quit()
		{
			BaseGameController.Instance.Session.CancelBundles();
			if (mGameStage == FireworksStage.SceneSelection)
			{
				BaseGameController.Instance.Session.SessionSounds.StopSoundEvent(Toolbox.Instance.mSceneManager.mCurrentScene.PreviewSong, base.gameObject);
			}
			else if (mChosenScene != null)
			{
				BaseGameController.Instance.Session.SessionSounds.StopSoundEvent(mChosenScene.MainSong, base.gameObject);
			}
			Toolbox.Instance = null;
		}

		void IMixGame.Pause()
		{
			mIsPaused = true;
			PlaybackLoaderScreen.Pause();
			if (mGameStage == FireworksStage.SceneSelection)
			{
				BaseGameController.Instance.Session.SessionSounds.PauseSoundEvent(Toolbox.Instance.mSceneManager.mCurrentScene.PreviewSong, base.gameObject);
			}
			else if (mChosenScene != null)
			{
				BaseGameController.Instance.Session.SessionSounds.PauseSoundEvent(mChosenScene.MainSong, base.gameObject);
			}
		}

		void IMixGame.Resume()
		{
			mIsPaused = false;
			PlaybackLoaderScreen.Resume();
			if (mGameStage == FireworksStage.SceneSelection)
			{
				DOVirtual.DelayedCall(0.1f, delegate
				{
					BaseGameController.Instance.Session.SessionSounds.UnpauseSoundEvent(Toolbox.Instance.mSceneManager.mCurrentScene.PreviewSong, base.gameObject);
				});
				ScrollRect.JumpToScene(Toolbox.Instance.mSceneManager.mCurrentScene);
			}
			else if (mChosenScene != null)
			{
				DOVirtual.DelayedCall(0.1f, delegate
				{
					BaseGameController.Instance.Session.SessionSounds.UnpauseSoundEvent(mChosenScene.MainSong, base.gameObject);
				});
			}
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

		void IGameAsset.OnGameSessionAssetLoaded(object aUserData)
		{
			Hashtable hashtable = aUserData as Hashtable;
			string aPath = (string)hashtable["Parameter"];
			GameObject gameObject = (GameObject)BaseGameController.Instance.GetBundleInstance(aPath);
			if (gameObject == null)
			{
				BaseGameController.Instance.PauseOnNetworkError();
				return;
			}
			mSceneBundleUrl = aPath;
			gameObject.transform.SetParent(base.gameObject.transform, false);
			Background.transform.GetComponent<SpriteRenderer>().sprite = mChosenScene.Background;
			mChosenScene.TapPlane = gameObject.GetComponentInChildren<FireworkTapPlane>();
			mChosenScene.GenerationArea = gameObject.GetComponentInChildren<FireworkGenerationArea>();
			Toolbox.Instance.mFireworkManager.Load(mChosenScene, mGameStage, this);
			if (mChosenScene.Foreground3D != null)
			{
				mChosenScene.Foreground3D.gameObject.SetActive(true);
				mCastleMaterial = mChosenScene.Foreground3D.gameObject.GetComponent<MeshRenderer>().materials[0];
			}
			mChosenScene.SceneButton.GetComponent<SceneButtonLoader>().TransitionToCreationMode();
			if (mGameStage == FireworksStage.Loading)
			{
				PlaybackLoaderScreen.EndLoading();
			}
		}

		void IAssetReferencer.CleanupReferences()
		{
		}

		public void LaunchDisplayFirework([Optional] Vector3 location, int fireworkIndex = 5)
		{
			if (location == default(Vector3))
			{
				location = base.transform.InverseTransformPoint(Toolbox.Instance.mFireworkManager.GetRandomFireworkLocation());
			}
			Toolbox.Instance.mFireworkManager.LaunchFirework(fireworkIndex, location);
		}

		public void EndGesture()
		{
			Toolbox.Instance.mFireworkManager.mGestureFirework.Stop();
		}

		public void RestartGame()
		{
			if (mChosenScene.Foreground3D != null)
			{
				mChosenScene.Foreground3D.gameObject.SetActive(false);
			}
			for (int i = 0; i < FireworkButtonContainer.transform.childCount; i++)
			{
				Object.Destroy(FireworkButtonContainer.transform.GetChild(i).gameObject);
			}
			ScrollRect.GetComponent<ScrollRect>().horizontal = true;
			mIsSongCountdownStarted = false;
			SongCountdownBar.transform.GetComponent<Image>().fillAmount = 0f;
			SongCountdownBar.transform.GetComponent<Image>().color = mCountdownBarInitColor;
			mIsFinaleComing = false;
			mIsFinishedLaunching = false;
			mGameStage = FireworksStage.SceneSelection;
			mCounterTime = 0f;
			mCounterTracker = 0;
			mCurrentSongTime = 0f;
			mLaunchedFireworks.Clear();
			mSavedFireworks.Clear();
			mIsSongStarted = false;
			mSongFinaleTime = 0f;
			Toolbox.Instance.mFireworkManager.Purge();
			Toolbox.Instance.mGestureTracker.Purge();
			Toolbox.Instance.mSceneManager.Purge();
			SceneSelectionPanel.SetActive(true);
			AnimatedHand.SetActive(true);
		}

		public void ReadyPlayback(FireworksData configData)
		{
			mGameStage = FireworksStage.Loading;
			ShowData showData = Toolbox.Instance.mPlaybackAssembler.Parse(configData);
			int num = 0;
			float num2 = 0f;
			if (TimeChanges.Count > 0)
			{
				num2 = TimeChanges[num];
			}
			float num3 = 0f;
			List<float> list = new List<float>();
			foreach (KeyValuePair<float, KeyValuePair<int, Vector3>> mLaunchedFirework in showData.mLaunchedFireworks)
			{
				mLaunchedFireworks.Enqueue(new KeyValuePair<float, KeyValuePair<int, Vector3>>(mLaunchedFirework.Key, mLaunchedFirework.Value));
				if (mCrowdReactionTimes.Count == 0 && num3 != 0f)
				{
					float num4 = num3 + mLaunchedFirework.Key;
					num4 /= 2f;
					mCrowdReactionTimes.Enqueue(num4);
				}
				if (mLaunchedFirework.Key <= num2)
				{
					ReactionRate = RateChanges[num];
					if (num + 1 < TimeChanges.Count)
					{
						num++;
						num2 = TimeChanges[num];
					}
				}
				if (Mathf.Abs(mLaunchedFirework.Key - num3) >= ReactionRate)
				{
					list.Clear();
				}
				num3 = mLaunchedFirework.Key;
				list.Add(num3);
				int num5 = 5;
				if (num < TimeChanges.Count - 1)
				{
					num5 *= 2;
				}
				if (list.Count < num5)
				{
					continue;
				}
				float num6 = 0f;
				foreach (float item in list)
				{
					float num7 = item;
					num6 += num7;
				}
				num6 /= (float)list.Count;
				mCrowdReactionTimes.Enqueue(num6);
				list.Clear();
			}
			mChosenScene = showData.mSelectedScene;
			mChosenSong = showData.mSelectedSong;
			mMessage = showData.mMessage;
			PlaybackLoaderScreen.BeginLoading(mChosenScene);
			if (PlaybackProgressBar != null)
			{
				PlaybackProgressBar.transform.parent.gameObject.SetActive(true);
			}
			foreach (Gesture mGesture in showData.mGestures)
			{
				Toolbox.Instance.mGestureTracker.AddNewGesture(mGesture);
			}
			GameObject gameObject = (GameObject)Object.Instantiate(MessageFirework.gameObject, Vector3.zero, Quaternion.identity);
			gameObject.GetComponent<FireworkF>().FontFirework.mMessage = mMessage;
			gameObject.GetComponent<FireworkF>().FontFirework.BuildFirework();
			gameObject.GetComponent<FireworkF>().FireworksGame = this;
			gameObject.transform.SetParent(base.transform);
			gameObject.transform.localPosition = MessageStartPoint;
			mPhysicalMessageFirework = gameObject;
			Toolbox.Instance.mFireworkManager.Load(mChosenScene, mGameStage, this);
			if (mChosenScene.Foreground3D != null)
			{
				mChosenScene.Foreground3D.gameObject.SetActive(true);
				Background.transform.GetComponent<SpriteRenderer>().sprite = mChosenScene.Background;
				mCastleMaterial = mChosenScene.Foreground3D.gameObject.GetComponent<MeshRenderer>().materials[0];
			}
			StartGameTimer(showData.mSelectedScene.SongTime);
			if (!DebugSceneIndicator.IsDebugScene)
			{
				LoadSelectedScene();
			}
		}

		public void SaveFirework(int firework, [Optional] Vector3 location)
		{
			if (mGameStage == FireworksStage.Creation && mCurrentSongTime > 0f)
			{
				if (location == default(Vector3))
				{
					location = base.transform.InverseTransformPoint(Toolbox.Instance.mFireworkManager.GetRandomFireworkLocation());
				}
				Vector3 value = location;
				float num = mCurrentSongTime;
				if (firework < mChosenScene.ButtonFireworks.Length)
				{
					num += mChosenScene.ButtonFireworks[firework].GetComponent<Firework>().LaunchTime;
				}
				if (mSavedFireworks.Count < 256)
				{
					mSavedFireworks.Add(new KeyValuePair<float, KeyValuePair<int, Vector3>>(num - mNumFireworksLaunchedThisFrame, new KeyValuePair<int, Vector3>(firework, value)));
					mNumFireworksLaunchedThisFrame += 0.001f;
				}
				Toolbox.Instance.mFireworkManager.LaunchFirework(firework, location);
			}
		}

		public void SaveMessage(Text aText)
		{
			if (aText != null)
			{
				SaveMessage(aText.text);
			}
		}

		public void SaveMessage(string aMessage)
		{
			if (Toolbox.Instance == null || GameController == null || mSavedFireworks == null)
			{
				return;
			}
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("Fireworks/SFX/UIButton");
			ShowData data = new ShowData
			{
				mLaunchedFireworks = new Dictionary<float, KeyValuePair<int, Vector3>>(),
				mGestures = new List<Gesture>()
			};
			foreach (KeyValuePair<float, KeyValuePair<int, Vector3>> mSavedFirework in mSavedFireworks)
			{
				if (!data.mLaunchedFireworks.ContainsKey(mSavedFirework.Key))
				{
					data.mLaunchedFireworks.Add(mSavedFirework.Key, mSavedFirework.Value);
				}
			}
			int numberofGestures = Toolbox.Instance.mGestureTracker.GetNumberofGestures();
			for (int i = 0; i < numberofGestures; i++)
			{
				data.mGestures.Add(Toolbox.Instance.mGestureTracker.GetGestureAtIndex(i));
			}
			data.mSelectedScene = mChosenScene;
			data.mSelectedSong = mChosenSong;
			data.mMessage = aMessage;
			FireworksData gameData = GameController.GetGameData<FireworksData>();
			Toolbox.Instance.mPlaybackAssembler.Assemble(data, gameData);
			if (DebugSceneIndicator.IsDebugScene)
			{
				mLaunchedFireworks.Clear();
				mGestureCounter = 0;
				MessageInputPanel.SetActive(false);
				ReadyPlayback(gameData);
				return;
			}
			Toolbox.PurgeToolbox();
			GameController.GameOver(gameData);
			string text = "text_present";
			if (string.IsNullOrEmpty(data.mMessage))
			{
				text = "text_absent";
			}
			GameController.LogEvent(GameLogEventType.ACTION, "fireworks_choose", mChosenScene.name + "|" + text);
		}

		public void StartGameTimer(float startTime)
		{
			mTimerStartTime = startTime;
			mCurrentSongTime = startTime;
			mMaxSongTime = startTime;
			mIsSongCountdownStarted = true;
			mRColorLerpTime = 0f;
			mIsSongStarted = false;
		}

		public void SetCurrentTime()
		{
			mCurrentSongTime -= Time.deltaTime;
		}

		public void SaveSelectedSceneandSong()
		{
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("Fireworks/SFX/UIButton");
			ScrollRect.GetComponent<ScrollRect>().horizontal = false;
			mChosenScene = Toolbox.Instance.mSceneManager.mCurrentScene;
			mChosenSong = mChosenScene.PreviewSong;
			mSongFinaleTime = mChosenScene.FinaleTime;
			mCurrentSongTime = mChosenScene.SongTime;
			mGameStage = FireworksStage.Creation;
			LoadSelectedScene();
			BaseGameController.Instance.Session.SessionSounds.StopSoundEvent(mChosenScene.PreviewSong);
		}

		private void LoadSelectedScene()
		{
			string text = GameController.GetGameSession().Entitlement.GetBaseUrl();
			if (DebugSceneIndicator.IsDebugScene)
			{
				text += '/';
			}
			if (mChosenScene.SceneBundle != null && text != null)
			{
				text += mChosenScene.SceneBundle;
				BaseGameController.Instance.LoadAsset(this, text, text);
			}
		}

		public void ChangeScene(Scene scene)
		{
			Toolbox.Instance.mSceneManager.ChangeScene(scene);
		}

		public void ChooseFirstScene()
		{
			Toolbox.Instance.mSceneManager.ChooseFirstScene();
		}

		public void SetInputInGestureRange(bool inRange)
		{
			mIsInputinGestureRange = inRange;
		}

		private void ColorLerpUpdate()
		{
			mRColorLerpTime += Time.deltaTime * RColorLerpSpeed;
		}

		public float GetColorLerp(int numColors, out int whichColor)
		{
			float result = mRColorLerpTime - mRColorLerpTime;
			whichColor = (int)mRColorLerpTime % numColors;
			return result;
		}

		private void LightUpdate()
		{
			if (Toolbox.Instance != null)
			{
				PointLight.intensity -= ModifyLightDecayVal * Time.fixedDeltaTime;
				if (Toolbox.Instance.mFireworkManager.mGestureFirework != null)
				{
					Toolbox.Instance.mFireworkManager.mGestureFirework.GetComponent<Light>().intensity -= ModifyLightDecayVal * Time.fixedDeltaTime * 3f;
				}
				mCastleMaterial.color = Color.Lerp(mCastleMaterial.color, Color.white, ModifyLightDecayVal * Time.fixedDeltaTime / 2f);
			}
		}

		public void LightUpScene(int colorIndex)
		{
			PointLight.intensity = 7f;
			mCastleMaterial.color = mChosenScene.ForegroundTints[colorIndex];
		}

		public void LightUpSceneG()
		{
			if (Toolbox.Instance.mFireworkManager.mGestureFirework != null)
			{
				Toolbox.Instance.mFireworkManager.mGestureFirework.GetComponent<Light>().intensity = 8f;
			}
			if (PointLight.intensity < 4f)
			{
				PointLight.intensity = 4f;
			}
		}

		public float GetXScreenWidth()
		{
			return mScreenXWidth;
		}

		private void OnEnable()
		{
			TrayGamePhysics.ResetAllGameLayers();
		}

		private void Update()
		{
            ReplaceMobileAdditiveShaderWithLegacy(this.gameObject);
            if (mGameStage == FireworksStage.Creation)
			{
				mNumFireworksLaunchedThisFrame = 0f;
				if (mIsSongCountdownStarted)
				{
					SetCurrentTime();
					ColorLerpUpdate();
					float num = mCurrentSongTime / mTimerStartTime;
					num = 1f - num;
					Image component = SongCountdownBar.transform.GetComponent<Image>();
					component.fillAmount = num;
					if (mCurrentSongTime <= mSongFinaleTime && !mIsFinaleComing)
					{
						mIsFinaleComing = true;
						mCountdownBarInitColor = component.color;
					}
					if (mIsFinaleComing)
					{
						float t = Mathf.PingPong(Time.time, 1f);
						component.color = Color.Lerp(mCountdownBarInitColor, Color.white, t);
					}
					if ((mIsInputinGestureRange && Input.GetMouseButton(0)) || mIsTrackingGesture)
					{
						LightUpSceneG();
						FireworkGesture mGestureFirework = Toolbox.Instance.mFireworkManager.mGestureFirework;
						if (!mIsTrackingGesture)
						{
							mIsTrackingGesture = true;
							mGestureStartTime = mCurrentSongTime;
							mTapTimerMax = mGestureStartTime + TapAllowanceTime;
							mTapTimer = mGestureStartTime;
							mCurrentGesture = new Gesture();
							mGestureStartPosition = mCurrentGesture.AddNewPoint(CurrentTimestamp, base.transform.InverseTransformPoint(Toolbox.Instance.mFireworkManager.GetTouchOnTapPlane()));
							mGestureFirework.transform.localPosition = mGestureStartPosition;
						}
						else if (mIsTrackingGesture && Input.GetMouseButton(0) && mIsInputinGestureRange)
						{
							if (mTapTimerMax != 0f)
							{
								mTapTimer += Time.deltaTime;
							}
							Vector3 b = mCurrentGesture.AddNewPoint(CurrentTimestamp, base.transform.InverseTransformPoint(Toolbox.Instance.mFireworkManager.GetTouchOnTapPlane()));
							if (mIsForcingGesture || Mathf.Abs(mGestureStartPosition.x - b.x) > GestureRange || Mathf.Abs(mGestureStartPosition.y - b.y) > GestureRange)
							{
								Toolbox.Instance.mFireworkManager.mGestureFirework.transform.localPosition = Vector3.Lerp(Toolbox.Instance.mFireworkManager.mGestureFirework.transform.localPosition, b, GLerpSpeed);
								if (!mIsGestureInitialized)
								{
									mGestureFirework.gameObject.SetActive(true);
									mGestureFirework.Play();
									mIsGestureInitialized = true;
									mIsForcingGesture = true;
								}
							}
							else if (mTapTimer >= mTapTimerMax)
							{
								mGestureFirework.gameObject.SetActive(true);
								mGestureFirework.Play();
								mIsGestureInitialized = true;
								mIsForcingGesture = true;
								mTapTimer = 0f;
								mTapTimerMax = 0f;
							}
						}
						else if ((mIsTrackingGesture && Input.GetMouseButtonUp(0) && mCurrentGesture != null) || (mIsTrackingGesture && !mIsInputinGestureRange && mCurrentGesture != null))
						{
							mIsTrackingGesture = false;
							mIsGestureInitialized = false;
							mCurrentGesture.EndGesture();
							if (!mIsForcingGesture)
							{
								SaveFirework(5, mGestureStartPosition);
							}
							else
							{
								int num2 = 0;
								using (List<KeyValuePair<float, KeyValuePair<int, Vector3>>>.Enumerator enumerator = mSavedFireworks.GetEnumerator())
								{
									while (enumerator.MoveNext() && !(enumerator.Current.Key < mGestureStartTime))
									{
										num2++;
									}
								}
								mSavedFireworks.Insert(num2, new KeyValuePair<float, KeyValuePair<int, Vector3>>(mGestureStartTime, new KeyValuePair<int, Vector3>(6, mGestureStartPosition)));
								mGestureFirework.GetComponent<FireworkGesture>().Stop();
							}
							mCurrentGesture.ClearPoints();
							mCurrentGesture = null;
							mGestureStartPosition = default(Vector3);
							mGestureStartTime = 0f;
							mTapTimer = 0f;
							mTapTimerMax = 0f;
							mIsForcingGesture = false;
						}
					}
					if (!(mCurrentSongTime <= 0f))
					{
						return;
					}
					if (mIsTrackingGesture)
					{
						mIsTrackingGesture = false;
						int num3 = mCurrentGesture.EndGesture();
						if (num3 < 6)
						{
							GameObject gameObject = Toolbox.Instance.mFireworkManager.mGestureFirework.gameObject;
							gameObject.GetComponent<FireworkGesture>().Stop();
							SaveFirework(5, mGestureStartPosition);
						}
						else
						{
							int num4 = 0;
							using (List<KeyValuePair<float, KeyValuePair<int, Vector3>>>.Enumerator enumerator2 = mSavedFireworks.GetEnumerator())
							{
								while (enumerator2.MoveNext() && !(enumerator2.Current.Key < mGestureStartTime))
								{
									num4++;
								}
							}
							mSavedFireworks.Insert(num4, new KeyValuePair<float, KeyValuePair<int, Vector3>>(mGestureStartTime, new KeyValuePair<int, Vector3>(6, mGestureStartPosition)));
							GameObject gameObject2 = Toolbox.Instance.mFireworkManager.mGestureFirework.gameObject;
							gameObject2.GetComponent<FireworkGesture>().Stop();
						}
						mCurrentGesture.ClearPoints();
						mCurrentGesture = null;
						mGestureStartPosition = default(Vector3);
						mGestureStartTime = 0f;
					}
					mIsFinishedLaunching = true;
					if (mIsFinishedLaunching)
					{
						mIsSongCountdownStarted = false;
						mGameStage = FireworksStage.EnterMessage;
						Toolbox.Instance.mFireworkManager.Purge();
						CreationPanel.GetComponent<PlayMenuTransitionAnimations>().PlayExitAnimation();
					}
				}
				else if (CountdownTextObject.activeInHierarchy)
				{
					mCounterTime += Time.deltaTime;
					float num5 = 4f;
					if (mCounterTime >= 1f)
					{
						mCounterTracker++;
						num5 -= (float)mCounterTracker;
						CountdownTextObject.transform.GetComponent<Text>().text = num5.ToString();
						mCounterTime = 0f;
						BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("Fireworks/SFX/Common/CountdownTick");
					}
					if (num5 <= 0f)
					{
						CountdownTextObject.transform.GetComponent<Text>().text = string.Empty;
						CountdownInputBlocker.SetActive(false);
						CountdownTextObject.transform.parent.GetComponent<PlayMenuTransitionAnimations>().PlayExitAnimation();
						StartGameTimer(mChosenScene.SongTime);
						BaseGameController.Instance.Session.SessionSounds.StopSoundEvent(mChosenScene.PreviewSong);
						BaseGameController.Instance.Session.SessionSounds.PlayMusic(mChosenScene.MainSong);
						mIsSongStarted = true;
					}
				}
			}
			else if (mGameStage == FireworksStage.EnterMessage)
			{
				string value = FireworksTextInput.nativeTextView.text;
				if (value != null && (mMessage == null || !value.Equals(mMessage)))
				{
					FireworksTextInput.OnMessageTextChanged(value);
					mMessage = value;
				}
			}
			else if (mGameStage == FireworksStage.End && !mPhysicalMessageFirework.GetComponent<FireworkF>().mIsPlaying)
			{
				mGameStage = FireworksStage.Idle;
				Toolbox.PurgeToolbox();
				FireworksData gameData = GameController.GetGameData<FireworksData>();
				FireworksResponse myResponse = gameData.GetMyResponse(gameData.Responses, GameController.PlayerId);
				GameController.GameOver(myResponse);
			}
		}

		private void FixedUpdate()
		{
			if (mIsPaused)
			{
				return;
			}
			LightUpdate();
			if (mGameStage != FireworksStage.ShowTime)
			{
				return;
			}
			SetCurrentTime();
			ColorLerpUpdate();
			float num = mCurrentSongTime / mTimerStartTime;
			num = 1f - num;
			if (PlaybackProgressBar != null)
			{
				PlaybackProgressBar.fillAmount = num;
			}
			if (mCurrentSongTime <= 30f && !mIsSongStarted)
			{
				BaseGameController.Instance.Session.SessionSounds.PlayMusic(mChosenScene.MainSong);
				mIsSongStarted = true;
			}
			if (mIsGesturePlaying && mCurrentGesture != null)
			{
				LightUpSceneG();
				if (CurrentTimestamp <= mCurrentGesture.GetEndTime())
				{
					Toolbox.Instance.mFireworkManager.mGestureFirework.transform.localPosition = mCurrentGesture.GetPointAtTime(CurrentTimestamp);
				}
				else
				{
					mIsGesturePlaying = false;
					Toolbox.Instance.mFireworkManager.mGestureFirework.Stop();
				}
			}
			if (mLaunchedFireworks.Count > 0)
			{
				int num2 = 0;
				bool flag = false;
				foreach (KeyValuePair<float, KeyValuePair<int, Vector3>> mLaunchedFirework in mLaunchedFireworks)
				{
					if (mCurrentSongTime <= mLaunchedFirework.Key)
					{
						flag = Toolbox.Instance.mFireworkManager.LaunchFirework(mLaunchedFirework.Value.Key, mLaunchedFirework.Value.Value);
						num2++;
						continue;
					}
					break;
				}
				for (int i = 0; i < num2; i++)
				{
					mLaunchedFireworks.Dequeue();
				}
				if (flag && mGestureCounter < Toolbox.Instance.mGestureTracker.GetNumberofGestures())
				{
					mIsGesturePlaying = true;
					mCurrentGesture = Toolbox.Instance.mGestureTracker.GetGestureAtIndex(mGestureCounter);
					Toolbox.Instance.mFireworkManager.mGestureFirework.gameObject.SetActive(true);
					Toolbox.Instance.mFireworkManager.mGestureFirework.Play();
					mGestureCounter++;
				}
			}
			if (mCrowdReactionTimes.Count > 0)
			{
				int num3 = 0;
				foreach (float mCrowdReactionTime in mCrowdReactionTimes)
				{
					float num4 = mCrowdReactionTime;
					if (!(mCurrentSongTime < num4))
					{
						continue;
					}
					if (ReactionSoundEvents.Count == 0 && ReactionSoundAmazedEvent != null)
					{
						BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent(ReactionSoundAmazedEvent);
					}
					else
					{
						BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent(ReactionSoundEvents[mReactionIndex]);
						mReactionIndex++;
						if (mReactionIndex >= ReactionSoundEvents.Count)
						{
							mReactionIndex = 0;
						}
					}
					num3++;
				}
				for (int j = 0; j < num3; j++)
				{
					mCrowdReactionTimes.Dequeue();
				}
			}
			if (mCurrentSongTime <= 0f)
			{
				Toolbox.Instance.mFireworkManager.mGestureFirework.GetComponent<ParticleSystem>().Stop();
				mPhysicalMessageFirework.GetComponent<FireworkF>().Launch(mPhysicalMessageFirework.transform.localPosition);
				mGameStage = FireworksStage.End;
			}
		}

		private void OnDestroy()
		{
			if (!string.IsNullOrEmpty(mSceneBundleUrl))
			{
				BaseGameController.Instance.DestroyBundleInstance(mSceneBundleUrl);
			}
		}

		public void StartTheShow()
		{
			mGameStage = FireworksStage.ShowTime;

        }

        private void Start()
		{
			Debug.Log("FireworksGame is attached to GameObject: " + gameObject.name);

			// Find "Snow" GameObject and apply legacy shader after 1 second
			var allGameObjects = FindObjectsOfType<GameObject>();
			foreach (var go in allGameObjects)
			{
				if (go.name == "Snow")
				{
					Debug.Log("Found Snow GameObject: " + go.name);
					StartCoroutine(SetLegacyParticleShaderAfterDelay(go, 1f));
				}
			}
		}

		private IEnumerator SetLegacyParticleShaderAfterDelay(GameObject go, float delay)
		{
			yield return new WaitForSeconds(delay);
			var renderers = go.GetComponentsInChildren<Renderer>(true);
			Shader legacyShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
			foreach (var renderer in renderers)
			{
				foreach (var mat in renderer.sharedMaterials)
				{
					if (mat != null && legacyShader != null)
					{
						mat.shader = legacyShader;
						Debug.Log($"Assigned Legacy Shaders/Particles/Alpha Blended to {renderer.gameObject.name}");
					}
				}
			}
		}

		private void ReplaceMobileAdditiveShaderWithLegacy(GameObject root)
		{
			var renderers = root.GetComponentsInChildren<Renderer>(true);
			foreach (var renderer in renderers)
			{
				foreach (var mat in renderer.sharedMaterials)
				{
					if (mat != null && mat.shader != null && mat.shader.name == "Mobile/Particles/Additive")
					{
						Shader legacyShader = Shader.Find("Legacy Shaders/Particles/Additive");
						if (legacyShader != null)
						{
							Debug.Log($"[FireworksGame] Replacing shader on {renderer.gameObject.name} from 'Mobile/Particles/Additive' to 'Legacy Shaders/Particles/Additive'");
							mat.shader = legacyShader;
						}
						else
						{
							Debug.LogWarning("[FireworksGame] Legacy Shaders/Particles/Additive not found.");
						}
					}
				}
			}
		}
	}
}
