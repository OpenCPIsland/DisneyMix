using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Friendzy.FriendzyQuiz
{
	public class TransitionAnimator : MonoBehaviour
	{
		private const float DIST_FROM_CAM = 1.5f;

		private const float ENTER_DURATION = 0.5f;

		private const float FADE_IN_DELAY = 0.15f;

		public const float EXIT_DURATION = 0.25f;

		private const float CASCADE_RATE = 0.035f;

		private const float GROW_DURATION = 0.4f;

		private const float SCALE_STRENGTH = 0.1f;

		private const float SHRINK_STRENGTH = 0.875f;

		private const float ROTATION_STRENGTH = 2f;

		private const float HIGHLIGHT_FADE_DURATION = 0.15f;

		private const float SHRINK_DURATION = 0.45f;

		private const float ENTER_BAR_DURATION = 0.8f;

		private const int FONT_MAX_SIZE = 80;

		[SerializeField]
		private UnityEngine.Camera mEventCamera;

		[SerializeField]
		[Header("Particle Effects")]
		private ParticleSystem mParticleSystem;

		[SerializeField]
		private SelectionParticle mSelectedParticle;

		[SerializeField]
		private ParticleSystem mLeftAmbientParticle;

		[SerializeField]
		private ParticleSystem mRightAmbientParticle;

		public QuestionOption QuestionPanel;

		private QuestionOption[] mOptions;

		private Button[] mButtons;

		public float FadeDuration;

		public float TransitionTime;

		private CanvasGroup mCanvasGroup;

		public Color HighlightColor;

		public Image Highlight;

		private ColorBlock selectedColorBlock;

		private ColorBlock notSelectedColorBlock;

		public Color FadeColor;

		private int mSmallestFont;

		private string[] AudienceReactions = new string[4] { "React/Ooh", "React/Gasp", "React/Laugh", "LightApplause" };

		private void Awake()
		{
			mCanvasGroup = GetComponent<CanvasGroup>();
			selectedColorBlock = ColorBlock.defaultColorBlock;
			selectedColorBlock.disabledColor = HighlightColor;
			selectedColorBlock.fadeDuration = FadeDuration;
			notSelectedColorBlock = ColorBlock.defaultColorBlock;
			notSelectedColorBlock.disabledColor = FadeColor;
			notSelectedColorBlock.fadeDuration = FadeDuration;
		}

		public void SetButtons(QuestionOption[] aOptions)
		{
			mOptions = aOptions;
			mButtons = new Button[aOptions.Length];
			for (int i = 0; i < aOptions.Length; i++)
			{
				mButtons[i] = aOptions[i].Button;
			}
		}

		private void SetButtonDefaults()
		{
			mSmallestFont = 80;
			QuestionPanel.gameObject.SetActive(true);
			Highlight.gameObject.SetActive(false);
			QuestionPanel.SetToAppearPos();
			for (int i = 0; i < mButtons.Length; i++)
			{
				QuestionOption questionOption = mOptions[i];
				questionOption.SetToAppearPos();
				Button button = mButtons[i];
				button.interactable = false;
			}
		}

		private void ResetButtons()
		{
			mSmallestFont = 80;
			QuestionPanel.gameObject.SetActive(false);
			mSelectedParticle.transform.SetParent(base.transform);
			mSelectedParticle.EnableSelectionParticle(false);
			for (int i = 0; i < mButtons.Length; i++)
			{
				mOptions[i].gameObject.SetActive(true);
				mOptions[i].transform.localScale = Vector3.one;
				mOptions[i].TextAnswer.resizeTextForBestFit = true;
				Button button = mButtons[i];
				button.interactable = true;
				button.colors = ColorBlock.defaultColorBlock;
			}
		}

		public void OptionsEnter()
		{
			SetButtonDefaults();
			QuestionPanel.GetComponent<CanvasGroup>().alpha = 0f;
			mCanvasGroup.alpha = 0f;
			float num = 0.15f / (float)mOptions.Length;
			Sequence s = DOTween.Sequence();
			for (int i = 0; i < mOptions.Length; i++)
			{
				QuestionOption questionOption = mOptions[i];
				Text currentText = questionOption.TextAnswer;
				Tween t = questionOption.AnimateIn(0.5f);
				s.Insert((float)(mOptions.Length - i) * 0.035f, t);
				s.InsertCallback((float)i * num, delegate
				{
					DetermineSmallestSize(currentText);
				});
			}
			Tween t2 = mCanvasGroup.DOFade(1f, 0.5f);
			s.Insert(0.15f, t2);
			Tween t3 = QuestionPanel.GetComponent<CanvasGroup>().DOFade(1f, 0.5f);
			s.Insert(0.15f, t3);
			Tween t4 = QuestionPanel.AnimateIn(0.5f);
			s.Insert((float)(mOptions.Length + 1) * 0.035f, t4).OnComplete(EnableButtons);
			s.InsertCallback(0.15f, AdjustFontSizes);
		}

		public void EnableButtons()
		{
			ParticleSystem.EmissionModule emission = mLeftAmbientParticle.emission;
			emission.enabled = true;
			ParticleSystem.EmissionModule emission2 = mRightAmbientParticle.emission;
			emission2.enabled = true;
			for (int i = 0; i < mOptions.Length; i++)
			{
				mButtons[i].interactable = true;
			}
		}

		private void DetermineSmallestSize(Text aCurrentText)
		{
			FriendzyGame.PlaySound("QuestionFadeIn", FriendzyGame.SOUND_PREFIX);
			if (mSmallestFont > aCurrentText.cachedTextGenerator.fontSizeUsedForBestFit)
			{
				mSmallestFont = aCurrentText.cachedTextGenerator.fontSizeUsedForBestFit;
			}
		}

		private void AdjustFontSizes()
		{
			for (int i = 0; i < mOptions.Length; i++)
			{
				QuestionOption questionOption = mOptions[i];
				questionOption.TextAnswer.resizeTextForBestFit = false;
				questionOption.TextAnswer.fontSize = mSmallestFont;
			}
		}

		public Sequence SelectionSequence(int aSelectedButtonIndex, ProgressBar aProgressBar)
		{
			FriendzyGame.PlaySound("SelectAnswer", FriendzyGame.SOUND_PREFIX);
			int num = Random.Range(0, 10);
			if (num < 7)
			{
				num = Random.Range(0, AudienceReactions.Length);
				FriendzyGame.PlaySound(AudienceReactions[num], FriendzyGame.SOUND_PREFIX);
			}
			SelectButton(aSelectedButtonIndex);
			Sequence sequence = DOTween.Sequence();
			sequence.Append(EnlargeChosenResponse(aSelectedButtonIndex));
			sequence.Append(SelectedEnterProgressBar(aSelectedButtonIndex, aProgressBar));
			sequence.Append(RemoveButtons());
			sequence.AppendCallback(ResetButtons);
			return sequence;
		}

		private Sequence EnlargeChosenResponse(int aButtonIndex)
		{
			ParticleSystem.EmissionModule emission = mLeftAmbientParticle.emission;
			emission.enabled = false;
			ParticleSystem.EmissionModule emission2 = mRightAmbientParticle.emission;
			emission2.enabled = false;
			Transform transform = mButtons[aButtonIndex].transform;
			Sequence sequence = DOTween.Sequence();
			for (int i = 0; i < mButtons.Length; i++)
			{
				if (i != aButtonIndex)
				{
					Transform transform2 = mButtons[i].transform;
					sequence.Join(transform2.DOScale(transform2.localScale * 0.875f, 0.4f));
				}
			}
			sequence.Join(transform.DOPunchScale(transform.localScale * 0.1f, 0.4f, 1));
			sequence.Join(Highlight.transform.DOPunchScale(Highlight.transform.localScale * 0.1f, 0.4f, 1));
			sequence.Join(transform.DOPunchRotation(transform.forward * 2f, 0.4f));
			sequence.Join(Highlight.transform.DOPunchRotation(Highlight.transform.forward * 2f, 0.4f));
			return sequence;
		}

		private Sequence SelectedEnterProgressBar(int aOptionIndex, ProgressBar aProgressBar)
		{
			float num = 0.03f;
			float num2 = 0.12f;
			float num3 = -0.02f;
			Transform optionTransform = mOptions[aOptionIndex].transform;
			Highlight.DOFade(0f, 0.15f);
			Sequence sequence = DOTween.Sequence();
			sequence.Append(optionTransform.DOScale(0f, 0.45f));
			sequence.InsertCallback(0f, delegate
			{
				FriendzyGame.PlaySound("PixelTail", FriendzyGame.SOUND_PREFIX);
				mSelectedParticle.EnableSelectionParticle(true);
				mSelectedParticle.transform.SetParent(optionTransform);
				mSelectedParticle.transform.position = optionTransform.position;
			});
			Vector3 progressBarPos = GetProgressBarPos(aProgressBar);
			if (optionTransform.position.x < progressBarPos.x)
			{
				num *= -1f;
				num2 *= -1f;
				num3 *= -1f;
			}
			Vector3 vector = progressBarPos - optionTransform.position;
			Vector3 vector2 = optionTransform.position + vector * -0.1f;
			Vector3 vector3 = optionTransform.position + vector * 0.3f;
			Vector3 vector4 = optionTransform.position + vector * 0.65f;
			vector2 += Vector3.right * num;
			vector3 += Vector3.right * num2;
			vector4 += Vector3.right * num3;
			Vector3[] path = new Vector3[5] { optionTransform.position, vector2, vector3, vector4, progressBarPos };
			sequence.Join(optionTransform.DOPath(path, 0.8f, PathType.CatmullRom));
			sequence.AppendCallback(delegate
			{
				mSelectedParticle.transform.SetParent(aProgressBar.transform);
				optionTransform.gameObject.SetActive(false);
				aProgressBar.FlashColor(Color.white);
				mSelectedParticle.SelectionExplosion();
				aProgressBar.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f, 0, 0.5f);
				aProgressBar.transform.DOPunchRotation(Vector3.right * 0.1f, 0.25f, 0, 0.5f);
			});
			return sequence;
		}

		private Vector3 GetProgressBarPos(ProgressBar aProgressBar)
		{
			int currentDividerPosIndex = aProgressBar.CurrentDividerPosIndex;
			if (currentDividerPosIndex == -1)
			{
				return aProgressBar.StartOfBar.position;
			}
			return aProgressBar.Dividers[currentDividerPosIndex].transform.position;
		}

		private Vector3 GetProgressBarPosMid(ProgressBar aProgressBar)
		{
			Vector3 progressBarPos = GetProgressBarPos(aProgressBar);
			progressBarPos += Vector3.right * aProgressBar.SegmentSize * 0.5f;
			Debug.Log(progressBarPos.x);
			return progressBarPos;
		}

		private void SelectButton(int selectedButtonIndex)
		{
			for (int i = 0; i < mButtons.Length; i++)
			{
				mButtons[i].colors = notSelectedColorBlock;
				mButtons[i].interactable = false;
			}
			mButtons[selectedButtonIndex].colors = selectedColorBlock;
			Highlight.transform.position = mButtons[selectedButtonIndex].transform.position;
			Highlight.gameObject.SetActive(true);
			Highlight.DOFade(1f, 0.1f);
			mParticleSystem.Emit(1);
			Vector2 mousePosition = UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero;
			mParticleSystem.transform.position = mEventCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 1.5f));
		}

		public Sequence RemoveButtons()
		{
			mCanvasGroup.alpha = 1f;
			QuestionPanel.GetComponent<CanvasGroup>().alpha = 1f;
			Sequence sequence = DOTween.Sequence();
			for (int i = 0; i < mOptions.Length; i++)
			{
				QuestionOption currentOption = mOptions[i];
				sequence.InsertCallback(0f, delegate
				{
					FriendzyGame.PlaySound("QuestionFadeOut", FriendzyGame.SOUND_PREFIX);
					currentOption.SetToRestPos();
				});
				Tween t = currentOption.AnimateOut(0.25f);
				sequence.Insert((float)(i + 1) * 0.035f, t);
			}
			sequence.InsertCallback(0f, ResetQuestionPanelAndHighlight);
			Tween t2 = mCanvasGroup.DOFade(0f, 0.25f).OnComplete(delegate
			{
				mCanvasGroup.alpha = 0f;
			});
			sequence.Insert(0f, t2);
			Tween t3 = QuestionPanel.GetComponent<CanvasGroup>().DOFade(0f, 0.25f).OnComplete(delegate
			{
				mCanvasGroup.alpha = 0f;
			});
			sequence.Insert(0f, t3);
			Tween t4 = QuestionPanel.AnimateOut(0.25f);
			sequence.Insert(0f, t4);
			return sequence;
		}

		private void ResetQuestionPanelAndHighlight()
		{
			QuestionPanel.SetToRestPos();
		}
	}
}
