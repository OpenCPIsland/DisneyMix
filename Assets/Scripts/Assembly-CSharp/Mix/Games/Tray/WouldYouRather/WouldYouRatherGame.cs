using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using LitJson;
using Mix.Assets;
using Mix.Games.Data;
using Mix.Games.Session;
using Mix.Games.Tray.Common;
using UnityEngine;

namespace Mix.Games.Tray.WouldYouRather
{
	public class WouldYouRatherGame : MonoBehaviour, IMixGame, IMixGameDataRequest, IAssetReferencer
	{
		public enum CardStackLocation
		{
			ALL_CARDS = 0,
			SELECTED_CARDS = 1
		}

		private const int NUM_CHOICES = 10;

		private const int NUM_CARDS_TO_SELECT = 2;

		private const string MUSIC_CUE_NAME = "WouldYouRather/MUS/Music";

		private const float CONST_DELAY_UNTIL_SHAKE_HINT = 30f;

		[Header("External References")]
		public GameObject cardPrefab;

		public ConfirmationOptions confirmationOptions;

		public InputHandler inputHandler;

		public Hints hints;

		public CameraMover cameraMover;

		[Header("Table Layout")]
		public Vector3 cardInitialLocalEulerAngles;

		[Space(10f)]
		public Vector3 normalCardStackPosition = new Vector3(-5f, 0f, -1f);

		public Vector3 selectedCardStackPosition = new Vector3(-0.6f, 0f, 1.5f);

		public Vector3 sourceCardStackPosition = new Vector3(-2.44f, 0f, 1f);

		[Space(10f)]
		public Vector3 normalCardStackOffset = new Vector3(1.1f, 0f, 0f);

		public Vector3 selectedCardStackOffset = new Vector3(1.2f, 0f, 0f);

		public Vector3 sourceCardStackOffset = new Vector3(0f, 0.02f, 0f);

		[Space(10f)]
		public float alternatingZOffset;

		[Header("Intro Sequence")]
		public float delayBeforeDealingCards = 1f;

		public float delayBetweenDealingCards = 0.1f;

		public float delayBeforeInitialCameraMove = 1f;

		[Header("Outro Sequence")]
		public List<Transform> cardFinalPositions;

		[Header("ML Scene Shader")]
		[Tooltip("Assign Custom/MLSceneShader here so it is included in builds.")]
		public Shader mlSceneShader;

		private const string ML_SCENE_PATH = "Table Art/CardsofDoom_ML_Scene/MLScene";

		private Material mMLSceneMaterial;

		[HideInInspector]
		public bool shuffleEnabled;

		private int numShuffles;

		private Tween shuffleHintTween;

		private bool mIsMakingFinalDecision;

		private bool mAllowCardSelection;

		private Dictionary<CardStackLocation, List<WouldYouRatherCard>> mCardStacks;

		private List<WouldYouRatherChoice> mChoices;

		private int mNumCardsAnimating;

		private List<WouldYouRatherCard> mSelectedCards = new List<WouldYouRatherCard>();

		private WouldYouRatherData mData;

		public WouldYouRatherController GameController;

		private List<Would_You_Rather_Data> mWouldYouRatherCardData;

		private GameObject mStackParent;

		public bool IsFirstPlayer
		{
			get
			{
				return mData.Type == MixGameDataType.INVITE;
			}
		}

		public WouldYouRatherResponse.WouldYouRatherResponseType? selectedResponse { get; set; }

		private List<WouldYouRatherCard> normalCardStack
		{
			get
			{
				if (mCardStacks != null && mCardStacks.ContainsKey(CardStackLocation.ALL_CARDS))
				{
					return mCardStacks[CardStackLocation.ALL_CARDS];
				}
				return null;
			}
		}

		private List<WouldYouRatherCard> selectedCardStack
		{
			get
			{
				if (mCardStacks != null && mCardStacks.ContainsKey(CardStackLocation.SELECTED_CARDS))
				{
					return mCardStacks[CardStackLocation.SELECTED_CARDS];
				}
				return null;
			}
		}

		public int selectedCards
		{
			get
			{
				return mSelectedCards.Count;
			}
		}

		public bool AllowCardSelection
		{
			get
			{
				return mAllowCardSelection;
			}
		}

		void IMixGame.Initialize(MixGameData aData)
		{
			mData = GameController.GetGameData<WouldYouRatherData>();
		}

		void IMixGame.Play()
		{
			DOVirtual.DelayedCall(0.1f, delegate
			{
				BaseGameController.Instance.Session.SessionSounds.PlayMusic("WouldYouRather/MUS/Music", base.gameObject);
			});
		}

		void IMixGame.Quit()
		{
			BaseGameController.Instance.Session.SessionSounds.StopSoundEvent("WouldYouRather/MUS/Music", base.gameObject);
			DOTween.KillAll();
		}

		void IMixGame.Pause()
		{
			BaseGameController.Instance.Session.SessionSounds.PauseSoundEvent("WouldYouRather/MUS/Music", base.gameObject);
			DOTween.PauseAll();
		}

		void IMixGame.Resume()
		{
			if (mWouldYouRatherCardData == null)
			{
				GameController.LoadData(this, "data/CardsOfDoom/CardsOfDoom.gz", "CardsOfDoom.txt", ParseJsonData);
			}
			DOVirtual.DelayedCall(0.1f, delegate
			{
				BaseGameController.Instance.Session.SessionSounds.UnpauseSoundEvent("WouldYouRather/MUS/Music", base.gameObject);
			});
			DOTween.PlayAll();
		}

		void IAssetReferencer.CleanupReferences()
		{
			AssetUtils.DestroyGameObject(cardPrefab);
		}

		void IMixGameDataRequest.OnDataLoaded(object aObject, object aUserData)
		{
			ContentObj contentObj = aObject as ContentObj;
			try
			{
				if (contentObj.content.objects.cod.Count == 0)
				{
					GameController.PauseOnNetworkError();
				}
				else
				{
					OnCardDataLoaded(contentObj);
				}
			}
			catch (Exception)
			{
				GameController.PauseOnNetworkError();
			}
		}

		private void OnEnable()
		{
			CameraMover.OnTransitionToGameplayComplete = (Action)Delegate.Combine(CameraMover.OnTransitionToGameplayComplete, new Action(HandleCameraEnterGameplayState));
			CameraMover.OnTransitionToConfirmationComplete = (Action)Delegate.Combine(CameraMover.OnTransitionToConfirmationComplete, new Action(HandleCameraEnterConfirmationState));
			TrayGamePhysics.ResetAllGameLayers();
		}

		private void OnDisable()
		{
			CameraMover.OnTransitionToGameplayComplete = (Action)Delegate.Remove(CameraMover.OnTransitionToGameplayComplete, new Action(HandleCameraEnterGameplayState));
			CameraMover.OnTransitionToConfirmationComplete = (Action)Delegate.Remove(CameraMover.OnTransitionToConfirmationComplete, new Action(HandleCameraEnterConfirmationState));
		}

		private void Awake()
		{
			selectedResponse = null;
		}

		private void Start()
		{
			//ApplyMLSceneShader();
			hints.storyHint.text = GetStoryHint();
			hints.selectCardsHint.text = BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.two_card_hint");
			GameController.LoadData(this, "data/CardsOfDoom/CardsOfDoom.gz", "CardsOfDoom.txt", ParseJsonData);
			ShakeDetector.StartShakeDetection(base.gameObject, ShuffleCards, 0.25f);
			StartShakeHint();
		}

		private void ApplyMLSceneShader()
		{
			// would_you_rather(Clone) is the parent of this component's GameObject.
			Transform gameRoot = (base.transform.parent != null) ? base.transform.parent : base.transform.root;
			Transform mlScene = gameRoot.Find(ML_SCENE_PATH);
			if (mlScene == null)
			{
				foreach (Transform t in gameRoot.GetComponentsInChildren<Transform>(true))
				{
					if (t.name == "MLScene")
					{
						mlScene = t;
						break;
					}
				}
			}
			if (mlScene == null)
			{
				Debug.LogWarning("WouldYouRatherGame: MLScene not found.");
				return;
			}
			Shader shader = (mlSceneShader != null) ? mlSceneShader : Shader.Find("Custom/MLSceneShader");
			if (shader == null)
			{
				Debug.LogWarning("WouldYouRatherGame: Custom/MLSceneShader not found. Assign it on the component.");
				return;
			}

			Renderer rend = mlScene.GetComponent<Renderer>();
			UnityEngine.UI.Graphic graphic = mlScene.GetComponent<UnityEngine.UI.Graphic>();
			Material oldMat = (rend != null) ? rend.sharedMaterial : ((graphic != null) ? graphic.material : null);

			// Clone the existing material and only swap its shader, so the texture/tiling/offset carry over.
			mMLSceneMaterial = (oldMat != null) ? new Material(oldMat) : new Material(shader);
			mMLSceneMaterial.shader = shader;
			if (oldMat != null && mMLSceneMaterial.GetTexture("_MainTex") == null)
			{
				Texture tex = null;
				Vector2 offset = Vector2.zero;
				Vector2 scale = Vector2.one;
				Shader oldShader = oldMat.shader;
				for (int i = 0; i < oldShader.GetPropertyCount(); i++)
				{
					if (oldShader.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Texture)
					{
						continue;
					}
					string propName = oldShader.GetPropertyName(i);
					Texture candidate = oldMat.GetTexture(propName);
					if (candidate != null)
					{
						tex = candidate;
						offset = oldMat.GetTextureOffset(propName);
						scale = oldMat.GetTextureScale(propName);
						break;
					}
				}
				if (tex == null && graphic != null)
				{
					tex = graphic.mainTexture;
				}
				if (tex != null)
				{
					mMLSceneMaterial.SetTexture("_MainTex", tex);
					mMLSceneMaterial.SetTextureOffset("_MainTex", offset);
					mMLSceneMaterial.SetTextureScale("_MainTex", scale);
				}
				else
				{
					Debug.LogWarning("WouldYouRatherGame: no texture found on MLScene's original material.");
				}
			}

			if (rend != null)
			{
				rend.sharedMaterial = mMLSceneMaterial;
			}
			else if (graphic != null)
			{
				graphic.material = mMLSceneMaterial;
			}
			else
			{
				Debug.LogWarning("WouldYouRatherGame: MLScene has no Renderer or UI Graphic.");
			}
		}

		private void OnDestroy()
		{
			if (mMLSceneMaterial != null)
			{
				UnityEngine.Object.Destroy(mMLSceneMaterial);
			}
		}

		private string GetStoryHint()
		{
			if (GameController.IsGroupSession)
			{
				return BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.fate_hint_group");
			}
			string friendName = GameController.FriendName;
			return string.Format(BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.fate_hint"), friendName);
		}

		private void OnCardDataLoaded(ContentObj contentData)
		{
			if (mWouldYouRatherCardData == null)
			{
				List<MixGameContentData> aVisibleList = new List<MixGameContentData>();
				MixGameContentDataHelper.SetVisibleAndOwnedContent(contentData.content.objects.cod.Cast<MixGameContentData>().ToList(), ref aVisibleList);
				mWouldYouRatherCardData = aVisibleList.Cast<Would_You_Rather_Data>().ToList();
				CreateCards();
			}
		}

		private void OnDrawGizmos()
		{
			Vector3 size = new Vector3(1f, 0f, 1.5f);
			Gizmos.color = Color.black;
			for (int i = 0; i < 10; i++)
			{
				Gizmos.DrawWireCube(GetPositionForCardInStack(CardStackLocation.ALL_CARDS, i), size);
			}
			for (int j = 0; j < 2; j++)
			{
				Gizmos.DrawWireCube(GetPositionForCardInStack(CardStackLocation.SELECTED_CARDS, j), size);
			}
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(GetPositionForCardInOriginalStack(0), size);
		}

		public void CreateCards()
		{
			mAllowCardSelection = false;
			if (mStackParent == null)
			{
				mStackParent = new GameObject("Cards");
				mStackParent.transform.SetParent(base.transform, false);
			}
			mCardStacks = new Dictionary<CardStackLocation, List<WouldYouRatherCard>>();
			mCardStacks.Add(CardStackLocation.ALL_CARDS, new List<WouldYouRatherCard>());
			mCardStacks.Add(CardStackLocation.SELECTED_CARDS, new List<WouldYouRatherCard>());
			if (mData.Type == MixGameDataType.INVITE)
			{
				BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/CardsFlyOnScreen");
				Sequence s = DOTween.Sequence();
				for (int i = 0; i < 10; i++)
				{
					WouldYouRatherCard wouldYouRatherCard = SpawnCard(i);
					normalCardStack.Add(wouldYouRatherCard);
					wouldYouRatherCard.transform.localEulerAngles = cardInitialLocalEulerAngles;
					wouldYouRatherCard.positionInStack = i;
					Vector3 positionForCardInOriginalStack = GetPositionForCardInOriginalStack(i * 2);
					wouldYouRatherCard.transform.position = positionForCardInOriginalStack + 10f * Vector3.up;
					Tweener t = wouldYouRatherCard.transform.DOMove(positionForCardInOriginalStack, 0.5f).SetEase(Ease.OutCubic);
					s.Insert((float)i * 0.075f, t);
				}
				s.AppendCallback(GetNewChoices);
			}
			else
			{
				WouldYouRatherCard wouldYouRatherCard2 = SpawnCard(0);
				normalCardStack.Add(wouldYouRatherCard2);
				wouldYouRatherCard2.transform.rotation = Quaternion.AngleAxis(180f, cardFinalPositions[0].TransformDirection(Vector3.back)) * cardFinalPositions[0].rotation;
				wouldYouRatherCard2.transform.position = cardFinalPositions[0].position;
				wouldYouRatherCard2.responseChoice = WouldYouRatherResponse.WouldYouRatherResponseType.ChoiceA;
				WouldYouRatherChoice choice = new WouldYouRatherChoice(mData.Option1.Caption, mData.Option1.ImageUrl, string.Empty);
				wouldYouRatherCard2.UpdateChoice(choice);
				WouldYouRatherCard wouldYouRatherCard3 = SpawnCard(0);
				normalCardStack.Add(wouldYouRatherCard3);
				wouldYouRatherCard3.transform.rotation = Quaternion.AngleAxis(180f, cardFinalPositions[1].TransformDirection(Vector3.back)) * cardFinalPositions[1].rotation;
				wouldYouRatherCard3.transform.position = cardFinalPositions[1].position;
				wouldYouRatherCard3.responseChoice = WouldYouRatherResponse.WouldYouRatherResponseType.ChoiceB;
				WouldYouRatherChoice choice2 = new WouldYouRatherChoice(mData.Option2.Caption, mData.Option2.ImageUrl, string.Empty);
				wouldYouRatherCard3.UpdateChoice(choice2);
				mSelectedCards.Add(wouldYouRatherCard2);
				mSelectedCards.Add(wouldYouRatherCard3);
				SelectingCardsIntroSequence();
			}
		}

		public Vector3 GetPositionForCardInStack(CardStackLocation location, int index)
		{
			if (location == CardStackLocation.ALL_CARDS)
			{
				Vector3 vector = new Vector3(0f, 0f, alternatingZOffset);
				if (index % 2 == 0)
				{
					vector.z = 0f - vector.z;
				}
				return base.transform.position + normalCardStackPosition + normalCardStackOffset * index + vector;
			}
			Vector3 vector2 = new Vector3(0f, 0f, alternatingZOffset);
			if (index % 2 == 0)
			{
				vector2.z = 0f - vector2.z;
			}
			return base.transform.position + selectedCardStackPosition + selectedCardStackOffset * index + vector2;
		}

		public Vector3 GetPositionForCardInOriginalStack(int index)
		{
			return base.transform.position + sourceCardStackPosition + sourceCardStackOffset * index;
		}

		private WouldYouRatherCard SpawnCard(int choiceIndex)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(cardPrefab);
			gameObject.name = "Card " + choiceIndex;
			gameObject.transform.SetParent(mStackParent.transform, true);
			WouldYouRatherCard component = gameObject.GetComponent<WouldYouRatherCard>();
			component.CardLocation = CardStackLocation.ALL_CARDS;
			component.WYRGame = this;
			component.choiceIndex = choiceIndex;
			component.positionInStack = choiceIndex;
			return component;
		}

		public void CardSelected(WouldYouRatherCard card)
		{
			if (mData.Type != MixGameDataType.INVITE)
			{
				return;
			}
			mSelectedCards.Add(card);
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/SelectCard");
			if (mSelectedCards.Count >= 2)
			{
				shuffleEnabled = false;
				BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/TwoCardsSelected");
				if (mSelectedCards[0].choiceIndex > mSelectedCards[1].choiceIndex)
				{
					WouldYouRatherCard value = mSelectedCards[0];
					mSelectedCards[0] = mSelectedCards[1];
					mSelectedCards[1] = value;
				}
				mSelectedCards[0].responseChoice = WouldYouRatherResponse.WouldYouRatherResponseType.ChoiceA;
				mSelectedCards[1].responseChoice = WouldYouRatherResponse.WouldYouRatherResponseType.ChoiceB;
				for (int i = 0; i < mSelectedCards.Count; i++)
				{
					Transform transform = cardFinalPositions[i];
					mSelectedCards[i].FlyToLocation(transform.position);
					mSelectedCards[i].transform.DORotate(transform.rotation.eulerAngles, 0.25f).SetEase(Ease.InOutCubic).SetDelay(0.25f);
				}
				cameraMover.GoToConfirmationState();
				hints.selectCardsHint.Hide();
				hints.storyHint.Hide();
				inputHandler.isCardTappingEnabled = false;
				CancelShakeHint();
			}
			else if (mSelectedCards.Count == 1)
			{
				hints.selectCardsHint.SetTextWithBounce(BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.one_card_hint"));
			}
		}

		public void CardDeselected(WouldYouRatherCard card)
		{
			mSelectedCards.Remove(card);
			if (mSelectedCards.Count == 1)
			{
				hints.selectCardsHint.SetTextWithBounce(BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.one_card_hint"));
			}
			else if (mSelectedCards.Count == 0)
			{
				hints.selectCardsHint.SetTextWithBounce(BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.two_card_hint"));
			}
			shuffleEnabled = true;
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/DeselectCard");
		}

		public void GameOver()
		{
			if (DebugSceneIndicator.IsDebugScene)
			{
				return;
			}
			mNumCardsAnimating--;
			if (mNumCardsAnimating != 0)
			{
				return;
			}
			if (mData.Type == MixGameDataType.INVITE)
			{
				for (int i = 0; i < mSelectedCards.Count; i++)
				{
					if (i == 0)
					{
						mData.Option1.Caption = mSelectedCards[i].CurrentChoice.Text;
						mData.Option1.ImageUrl = mSelectedCards[i].CurrentChoice.ImageURL;
					}
					else
					{
						mData.Option2.Caption = mSelectedCards[i].CurrentChoice.Text;
						mData.Option2.ImageUrl = mSelectedCards[i].CurrentChoice.ImageURL;
					}
				}
				string aGameMessage = string.Format("{0}|{1}", mData.Option1.Caption, mData.Option2.Caption);
				GameController.LogEvent(GameLogEventType.ACTION, "would_you_rather_choose", aGameMessage);
				GameController.LogEvent(GameLogEventType.ACTION, "would_you_rather_shuffles", numShuffles.ToString());
				if (selectedResponse.HasValue)
				{
					WouldYouRatherResponse myResponse = mData.GetMyResponse(mData.Responses, GameController.PlayerId);
					myResponse.Choice = selectedResponse.Value;
					myResponse.Attempts++;
				}
				GameController.GameOver(mData);
			}
			else
			{
				WouldYouRatherResponse myResponse2 = mData.GetMyResponse(mData.Responses, GameController.PlayerId);
				myResponse2.Choice = selectedResponse.Value;
				myResponse2.Attempts++;
				GameController.GameOver(myResponse2);
			}
		}

		public void GetNewChoices()
		{
			mChoices = CreateListOfChoices();
			foreach (WouldYouRatherCard item in selectedCardStack)
			{
				item.ReceiveChoices(mChoices);
			}
			foreach (WouldYouRatherCard item2 in normalCardStack)
			{
				item2.ReceiveChoices(mChoices);
			}
			StartGameIntroSequence();
		}

		public List<WouldYouRatherChoice> CreateListOfChoices()
		{
			List<int> list = new List<int>();
			List<WouldYouRatherChoice> list2 = new List<WouldYouRatherChoice>();
			int num = 10;
			if (num > mWouldYouRatherCardData.Count)
			{
				num = mWouldYouRatherCardData.Count;
			}
			for (int i = 0; i < num; i++)
			{
				int j;
				for (j = UnityEngine.Random.Range(0, mWouldYouRatherCardData.Count); list.Contains(j); j++)
				{
				}
				list.Add(j);
			}
			foreach (int item in list)
			{
				list2.Add(new WouldYouRatherChoice(mWouldYouRatherCardData[item].text, mWouldYouRatherCardData[item].imageUrl));
			}
			return list2;
		}

		public void ShuffleCards()
		{
			if (shuffleEnabled)
			{
				CancelShakeHint();
				shuffleEnabled = false;
				numShuffles++;
				for (int num = mSelectedCards.Count - 1; num >= 0; num--)
				{
					mSelectedCards[num].FlyToLocation(GetPositionForCardInStack(CardStackLocation.ALL_CARDS, mSelectedCards[num].positionInStack));
					mSelectedCards[num].isSelected = false;
				}
				mSelectedCards.Clear();
				hints.selectCardsHint.Hide(true);
				hints.storyHint.Hide(true);
				BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/ShakeToShuffle");
				Sequence s = DOTween.Sequence();
				for (int i = 0; i < normalCardStack.Count; i++)
				{
					normalCardStack[i].DisableInput();
					Tweener t = normalCardStack[i].StartShuffleAnimation();
					s.Insert((float)i * 0.1f, t);
				}
				s.AppendCallback(ShuffleAnimComplete);
				cameraMover.GoToShuffleState();
				inputHandler.ResetGameplayCamera();
			}
		}

		public void ShuffleAnimComplete()
		{
			hints.selectCardsHint.text = BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.two_card_hint");
			CreateCards();
		}

		private void StartGameIntroSequence()
		{
			Sequence s = DOTween.Sequence();
			s.AppendInterval(delayBeforeDealingCards);
			s.AppendCallback(delegate
			{
				BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/InitialCardShuffle");
			});
			for (int num = 0; num < normalCardStack.Count; num++)
			{
				Sequence t = normalCardStack[num].BeginStartGameAnimation();
				s.Insert((float)num * delayBetweenDealingCards, t);
			}
			s.InsertCallback(delayBeforeInitialCameraMove, StartGame);
		}

		private void SelectingCardsIntroSequence()
		{
			Sequence s = DOTween.Sequence();
			s.AppendInterval(delayBeforeInitialCameraMove);
			s.AppendCallback(cameraMover.GoToConfirmationState);
			s.Insert(1.7f, mSelectedCards[0].transform.DORotate(cardFinalPositions[0].transform.localEulerAngles, 0.5f).SetEase(Ease.InOutBack));
			s.Insert(1.8f, mSelectedCards[1].transform.DORotate(cardFinalPositions[1].transform.localEulerAngles, 0.5f).SetEase(Ease.InOutBack));
			s.AppendCallback(delegate
			{
				mSelectedCards[0].AppearToBeSelected();
			});
			s.AppendCallback(delegate
			{
				mSelectedCards[1].AppearToBeSelected();
			});
		}

		private void StartGame()
		{
			cameraMover.GoToGameplayState();
		}

		public bool ShouldGameplayCameraFocusOnCard()
		{
			return mSelectedCards.Count < 2;
		}

		public bool IsMakingFinalDecision()
		{
			return mIsMakingFinalDecision;
		}

		private void StartShakeHint()
		{
			shuffleHintTween = DOVirtual.DelayedCall(30f, ShowShakeHint);
		}

		private void ShowShakeHint()
		{
			hints.selectCardsHint.text = BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.shuffle_hint");
		}

		private void CancelShakeHint()
		{
			if (!hints.storyHint.IsFullyHidden)
			{
				hints.storyHint.Hide();
			}
			if (shuffleHintTween != null)
			{
				shuffleHintTween.Kill();
				shuffleHintTween = null;
			}
		}

		public void FinalDecisionMade()
		{
			hints.storyHint.Hide();
			mIsMakingFinalDecision = false;
			inputHandler.isCardTappingEnabled = false;
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/AcceptTwoCards");
			mNumCardsAnimating = 2;
			for (int i = 0; i < mSelectedCards.Count; i++)
			{
				mSelectedCards[i].BeginEndGameAnimation(mSelectedCards[i].responseChoice == selectedResponse);
			}
			StartCoroutine(PlayCardExplodeSound(mSelectedCards[0].cardEndAnimationExplodeDelay));
			cameraMover.GoToOutroState();
			confirmationOptions.HideAllButtons();
		}

		public void CancelButtonPressed()
		{
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/RejectTwoCards");
			for (int num = mSelectedCards.Count - 1; num >= 0; num--)
			{
				mSelectedCards[num].FlyToLocation(GetPositionForCardInStack(CardStackLocation.ALL_CARDS, mSelectedCards[num].positionInStack));
				mSelectedCards[num].transform.DORotate(Vector3.zero, mSelectedCards[num].cardSelectAnimationFlyTime).SetEase(Ease.InOutCubic);
				mSelectedCards[num].isSelected = false;
			}
			mSelectedCards.Clear();
			hints.storyHint.Hide();
			inputHandler.isCardTappingEnabled = false;
			cameraMover.GoToGameplayState();
			confirmationOptions.HideAllButtons();
		}

		private IEnumerator PlayCardExplodeSound(float delay)
		{
			yield return new WaitForSeconds(delay);
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("WouldYouRather/SFX/TwoCardsExplode");
		}

		private void HandleCameraEnterGameplayState()
		{
			hints.selectCardsHint.Show();
			hints.storyHint.text = GetStoryHint();
			hints.storyHint.Show();
			inputHandler.enabled = true;
			inputHandler.isCardTappingEnabled = true;
			shuffleEnabled = true;
			mAllowCardSelection = true;
			mIsMakingFinalDecision = false;
		}

		private void HandleCameraEnterConfirmationState()
		{
			if (mData.Type == MixGameDataType.INVITE)
			{
				inputHandler.isCardTappingEnabled = true;
				confirmationOptions.ShowConfirmButton();
				confirmationOptions.ShowCancelButton();
				mIsMakingFinalDecision = true;
			}
			else
			{
				hints.storyHint.text = BaseGameController.Instance.Session.GetLocalizedString("customtokens.wouldyourather.final_choice_response");
				hints.storyHint.Show();
				inputHandler.isCardTappingEnabled = true;
				mIsMakingFinalDecision = true;
			}
		}

		public object ParseJsonData(string json)
		{
			return JsonMapper.ToObject<ContentObj>(json);
		}
	}
}