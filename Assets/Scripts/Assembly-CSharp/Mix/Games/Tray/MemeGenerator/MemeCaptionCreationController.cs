using System.Collections;
using Mix.Games.Session;
using Mix.Native;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.MemeGenerator
{
	public class MemeCaptionCreationController : MonoBehaviour, IGameAsset, IGameModerationResult
	{
		private MemeCaptionText activeText;

		private ToastPanelController toastPanel;

		private bool submitSelected;

		private MemeModerationReponse memeModerationResponse;

		protected IMemeGeneration mMemeGeneration;

		protected Captions mCaptions;

		public Image MemeImage;

		public InputField MemeNativeTextField;

		public MemeCaptionText topText;

		public MemeCaptionText bottomText;

		public Button SubmitButton;

		public Button CancelButton;

		[Header("Highlight Text")]
		public Color HighlightedTextColor;

		public Vector2 HighlightedTextOffset;

		public Color TextEnteredColor;

		public Color DefaultTextColor;

		public Color DefaultTextHighlightColor;

		[Header("Screenshot Image")]
		public Camera ScreenshotCamera;

		public Image ScreenshotImage;

		public Text ScreenshotTopText;

		public Text ScreenshotBottomText;

		public Color ScreenColor;

		protected bool SubmitSelected
		{
			get
			{
				return submitSelected;
			}
			set
			{
				submitSelected = value;
				SubmitButton.interactable = !submitSelected;
				CancelButton.interactable = !submitSelected;
			}
		}

		void IGameAsset.OnGameSessionAssetLoaded(object aUserData)
		{
			if (this.IsNullOrDisposed() || mMemeGeneration == null)
			{
				return;
			}
			Hashtable hashtable = aUserData as Hashtable;
			string aPath = (string)hashtable["Parameter"];
			Object bundleInstance = mMemeGeneration.GetMemeGeneratorController().GetBundleInstance(aPath);
			if (bundleInstance != null)
			{
				if (bundleInstance is Texture2D)
				{
					Texture2D texture2D = (Texture2D)bundleInstance;
					Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0f, 0f));
					MemeImage.sprite = sprite;
				}
				else if (bundleInstance is GameObject)
				{
					GameObject gameObject = (GameObject)bundleInstance;
					Sprite sprite2 = gameObject.GetComponent<Image>().sprite;
					MemeImage.sprite = sprite2;
				}
				MemeImage.color = Color.white;
				ScreenshotImage.sprite = MemeImage.sprite;
			}
		}

		void IGameModerationResult.OnModerationResult(bool aIsModerated, string aModeratedText, object aUserData)
		{
			if (this.IsNullOrDisposed() || base.gameObject == null || !BaseGameController.Instance.Session.IsToastPanelActive)
			{
				return;
			}
			MemeCaptionText memeCaptionText = aUserData as MemeCaptionText;
			if (!memeCaptionText.IsNullOrDisposed())
			{
				if (memeCaptionText == topText)
				{
					memeModerationResponse.TopModerationResult = aIsModerated;
					mCaptions.TopCaption = aModeratedText;
				}
				else
				{
					memeModerationResponse.BottomModerationResult = aIsModerated;
					mCaptions.BottomCaption = aModeratedText;
				}
				if (aIsModerated)
				{
					SubmitSelected = false;
					memeCaptionText.Caption = aModeratedText;
				}
				memeCaptionText.PostModeration();
				onModerationResponseReceived();
			}
		}

		void IGameModerationResult.OnModerationError(object aUserData)
		{
			if (!this.IsNullOrDisposed() && !(base.gameObject == null) && BaseGameController.Instance.Session.IsToastPanelActive && DebugSceneIndicator.IsMainScene)
			{
				SubmitSelected = false;
				if (!BaseGameController.Instance.IsNullOrDisposed())
				{
					BaseGameController.Instance.PauseOnNetworkError();
				}
			}
		}

		public void Initialize(IMemeGeneration aMemeGeneration, string aImageUrl)
		{
			mMemeGeneration = aMemeGeneration;
			mMemeGeneration.GetMemeGeneratorController().LoadAsset(this, aImageUrl, aImageUrl);
			GetComponent<Canvas>().worldCamera = GameObject.Find("UI_FG_Camera").GetComponent<Camera>();
			mCaptions = new Captions();
			toastPanel = GetComponent<ToastPanelController>();
			toastPanel.ToastPanelAnimationComplete += AnimationComplete;
			toastPanel.ToastPanelOnKeyboardReturnPressed += OnKeyboardReturnKey;
			toastPanel.ToastPanelHideComplete += HideObject;
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("MemeGenerator/SFX/MemeImagePopsUp");
			memeModerationResponse = new MemeModerationReponse();
			SubmitSelected = false;
		}

		private void HideObject()
		{
			MemeImage.sprite = null;
			mMemeGeneration.HideMemeImage();
		}

		private void AnimationComplete()
		{
			activeText = topText;
			topText.SetActive(true);
		}

		private void OnKeyboardReturnKey(NativeKeyboardReturnKey returnKey)
		{
			if (!(activeText == null) && !(topText == null) && !(bottomText == null))
			{
				if (activeText == topText)
				{
					SetActiveTextObject(bottomText);
				}
				else if (activeText == bottomText)
				{
					OnSubmitSelected();
				}
			}
		}

		private void SetActiveTextObject(MemeCaptionText aNewText)
		{
			if (activeText != aNewText && activeText != null)
			{
				activeText.SetActive(false);
				BaseGameController baseGameController = mMemeGeneration as BaseGameController;
				if (activeText.Caption.Trim() != string.Empty && baseGameController != null)
				{
					baseGameController.ModerateText(activeText.Caption, this, activeText);
				}
				activeText = aNewText;
				activeText.SetActive(true);
			}
		}

		private void Start()
		{
			topText.InitializeHighlightOutline(HighlightedTextColor, HighlightedTextOffset);
			bottomText.InitializeHighlightOutline(HighlightedTextColor, HighlightedTextOffset);
			bottomText.SetActive(false);
		}

		private void OnEnable()
		{
			MemeImage.color = ScreenColor;
		}

		private void OnDisable()
		{
			if (toastPanel != null)
			{
				toastPanel.ToastPanelAnimationComplete -= AnimationComplete;
				toastPanel.ToastPanelOnKeyboardReturnPressed -= OnKeyboardReturnKey;
				toastPanel.ToastPanelHideComplete -= HideObject;
			}
		}

		private void OnCaptionSelected(Text textObject)
		{
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("MemeGenerator/SFX/TapToBeginTextEntry");
			if (topText.MatchesSelected(textObject))
			{
				SetActiveTextObject(topText);
			}
			else if (bottomText.MatchesSelected(textObject))
			{
				SetActiveTextObject(bottomText);
			}
		}

		public void OnQuitSelected()
		{
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("MemeGenerator/SFX/ButtonUI");
			topText.Reset();
			bottomText.Reset();
			toastPanel.HideToastPanel();
		}

		public void QuitMeme()
		{
			mMemeGeneration.QuitMemeGeneration();
		}

		public void OnSubmitSelected()
		{
			BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("MemeGenerator/SFX/ButtonUI");
			BaseGameController baseGameController = mMemeGeneration as BaseGameController;
			if (this.IsNullOrDisposed() || topText == null || bottomText == null || baseGameController == null || mCaptions == null || memeModerationResponse == null || (topText.IsEmpty() && bottomText.IsEmpty()))
			{
				return;
			}
			SubmitSelected = true;
			if (topText != null && topText.IsDirty)
			{
				if (topText.IsEmpty())
				{
					mCaptions.TopCaption = topText.Caption;
					topText.PostModeration();
					memeModerationResponse.TopModerationResult = false;
					onModerationResponseReceived();
				}
				else
				{
					baseGameController.ModerateText(topText.Caption, this, topText);
				}
			}
			else
			{
				memeModerationResponse.TopModerationResult = false;
				onModerationResponseReceived();
			}
			if (bottomText.IsDirty)
			{
				if (bottomText.IsEmpty())
				{
					mCaptions.BottomCaption = bottomText.Caption;
					bottomText.PostModeration();
					memeModerationResponse.BottomModerationResult = false;
					onModerationResponseReceived();
				}
				else
				{
					baseGameController.ModerateText(bottomText.Caption, this, bottomText);
				}
			}
			else
			{
				memeModerationResponse.BottomModerationResult = false;
				onModerationResponseReceived();
			}
		}

		private void onModerationResponseReceived()
		{
			if (memeModerationResponse != null && memeModerationResponse.AllResultsReceived && !topText.IsDirty && !bottomText.IsDirty)
			{
				processMemeModerationResponse();
			}
		}

		private void processMemeModerationResponse()
		{
			if (this.IsNullOrDisposed() || base.gameObject == null || memeModerationResponse == null || mMemeGeneration == null || BaseGameController.Instance.IsNullOrDisposed() || mCaptions == null || MemeImage.IsNullOrDisposed() || MemeImage.sprite.IsNullOrDisposed())
			{
				return;
			}
			if (memeModerationResponse.PassedModeration && SubmitSelected)
			{
				mMemeGeneration.SetMemeCaption(mCaptions);
				BaseGameController.Instance.LogEvent(GameLogEventType.ACTION, "meme_choose", MemeImage.sprite.name);
				return;
			}
			SubmitSelected = false;
			if (!activeText.IsNullOrDisposed())
			{
				activeText.SetActive(true);
			}
		}

		public void RenderMemeSnapshot()
		{
			ScreenshotTopText.text = mCaptions.TopCaption;
			ScreenshotBottomText.text = mCaptions.BottomCaption;
			Camera screenshotCamera = ScreenshotCamera;
			int height = MemeImage.mainTexture.height;
			int width = MemeImage.mainTexture.width;
			screenshotCamera.targetTexture = new RenderTexture(width, height, 24);
			screenshotCamera.targetTexture.anisoLevel = 4;
			RenderTexture.active = screenshotCamera.targetTexture;
			Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
			screenshotCamera.Render();
			texture2D.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
			texture2D.Apply();
			RenderTexture.active = null;
		}
	}
}
