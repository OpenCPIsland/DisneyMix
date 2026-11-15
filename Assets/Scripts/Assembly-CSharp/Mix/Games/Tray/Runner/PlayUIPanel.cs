using System;
using DG.Tweening;
using Mix.Games.Tray.Hints;
using Mix.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Runner
{
	public class PlayUIPanel : RunnerUIPanel
	{
		public GameObject heartCounterPrefab;

		[Header("UI Element Holders")]
		public Transform hintHolder;

		public Transform coinCountHolder;

		public Transform heartCountHolder;

		[Space(10f)]
		public Button hiddenPlayButton;

		[Header("Hint")]
		public string tapToPlayHint;

		public string tapToJumpHint;

		public float delayBeforeJumpHint = 1f;

		[Header("Coin Count")]
		public bool showCoinCount;

		public string coinCountFormat = "{0}";

		public Sprite coinIcon;

		public float coinCountBounceTime = 0.2f;

		public float coinCountBounceAmount = 0.2f;

		public Text resumeCountdownText;

		public float resumeCountdownBounceTime = 0.2f;

		public float resumeCountdownBounceAmount = 0.2f;

		private GenericGameTooltip mHint;

		private GenericGameTooltip mCoinCount;

		private HeartCounter mHeartCount;

		private RectTransform mCoinCountHolderRectTransform;

		private Vector2 mCoinCountHolderAnchorMin;

		private Vector2 mCoinCountHolderAnchorMax;

		private Vector2 mCoinCountHolderPivot;

		private Vector2 mCoinCountHolderAnchoredPosition;

		private bool mLastCoinCountLandscapeLayout;

		private bool mIsWaitingForAJump;

		public Vector3 gemIconLocation
		{
			get
			{
				return mCoinCount.leftIcon.transform.position;
			}
		}

		private void Awake()
		{
			mHint = InstantiateTooltip(hintHolder);
			mHint.SetAnchor(AnchorUIElement.AnchorStyle.FILL);
			mHint.text = BaseGameController.Instance.Session.GetLocalizedString(tapToPlayHint);
			if (showCoinCount)
			{
				mCoinCount = InstantiateTooltip(coinCountHolder);
				mCoinCount.SetAnchor(AnchorUIElement.AnchorStyle.TOP_LEFT);
				mCoinCountHolderRectTransform = coinCountHolder as RectTransform;
				mCoinCountHolderAnchorMin = mCoinCountHolderRectTransform.anchorMin;
				mCoinCountHolderAnchorMax = mCoinCountHolderRectTransform.anchorMax;
				mCoinCountHolderPivot = mCoinCountHolderRectTransform.pivot;
				mCoinCountHolderAnchoredPosition = mCoinCountHolderRectTransform.anchoredPosition;
				mCoinCount.leftIcon.sprite = coinIcon;
				mCoinCount.leftIcon.gameObject.SetActive(true);
				mCoinCount.hintUpdateBounceAmount = coinCountBounceAmount;
				mCoinCount.hintUpdateBounceTime = coinCountBounceTime;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(heartCounterPrefab);
			gameObject.transform.SetParent(heartCountHolder, false);
			mHeartCount = gameObject.GetComponentInChildren<HeartCounter>();
			mHeartCount.SetAnchor(AnchorUIElement.AnchorStyle.TOP_RIGHT);
			HeartCounter heartCounter = mHeartCount;
			heartCounter.OnHeartIconDeactivated = (Action)Delegate.Combine(heartCounter.OnHeartIconDeactivated, new Action(OnHeartIconDeactivated));
			resumeCountdownText.gameObject.SetActive(false);
		}

		private void OnHeartIconDeactivated()
		{
			MainRunnerGame.PlaySound("LoseLife", runnerGame.SOUND_PREFIX);
		}

		private void Start()
		{
			ApplyCoinCountLandscapeLayout();
			mHeartCount.Initialize(runnerGame.maxLives);
			mHeartCount.Hide(true);
		}

		private void Update()
		{
			UpdateCoinCountLandscapeLayout();
			if (mCoinCount != null && !runnerGame.gameHasEnded)
			{
				mCoinCount.SetTextWithBounce(string.Format(coinCountFormat, runnerGame.checkpointsPassed));
				if (!mCoinCount.IsShowing && runnerGame.checkpointsPassed > 0)
				{
					mCoinCount.Show();
				}
			}
			mHeartCount.SetHeartCount(runnerGame.lives);
			if (mIsWaitingForAJump && (runnerGame.HasThePlayerJumpedAtLeastOnce || runnerGame.checkpointsPassed > 0))
			{
				mHint.Hide();
			}
		}

		private void UpdateCoinCountLandscapeLayout()
		{
			if (mCoinCountHolderRectTransform == null)
			{
				return;
			}

			bool isLandscape = IsLandscapeLayout();
			if (isLandscape != mLastCoinCountLandscapeLayout)
			{
				ApplyCoinCountLandscapeLayout();
			}
		}

		private void ApplyCoinCountLandscapeLayout()
		{
			bool isLandscape = IsLandscapeLayout();
			Vector2 anchorMin = mCoinCountHolderAnchorMin;
			Vector2 anchorMax = mCoinCountHolderAnchorMax;
			Vector2 pivot = mCoinCountHolderPivot;
			Vector2 anchoredPosition = mCoinCountHolderAnchoredPosition;
			if (isLandscape)
			{
				RectTransform heartHolderRectTransform = heartCountHolder as RectTransform;
				anchorMin.x = 1f;
				anchorMax.x = 1f;
				pivot.x = 1f;
				anchoredPosition.x = -heartHolderRectTransform.rect.width - 10f;
			}
			else
			{
				anchorMin.x = 0f;
				anchorMax.x = 0f;
				pivot.x = 0f;
				anchoredPosition.x = 0.002105713f;
			}

			mCoinCountHolderRectTransform.anchorMin = anchorMin;
			mCoinCountHolderRectTransform.anchorMax = anchorMax;
			mCoinCountHolderRectTransform.pivot = pivot;
			mCoinCountHolderRectTransform.anchoredPosition = anchoredPosition;
			mLastCoinCountLandscapeLayout = isLandscape;
		}

		private bool IsLandscapeLayout()
		{
			return Screen.width > Screen.height;
		}

		public void OnHiddenPlayButtonPressed()
		{
			hiddenPlayButton.gameObject.SetActive(false);
			mHint.Hide();
			DOVirtual.DelayedCall(delayBeforeJumpHint, delegate
			{
				if (!runnerGame.HasThePlayerJumpedAtLeastOnce)
				{
					mHint.text = BaseGameController.Instance.Session.GetLocalizedString(tapToJumpHint);
					mHint.Show();
					mIsWaitingForAJump = true;
				}
			});
			runnerGame.OnPlayButton();
		}

		public override void Show()
		{
			OnShow();
			mHint.Show();
			mHeartCount.Show();
			GenericGameTooltip genericGameTooltip = mHint;
			genericGameTooltip.OnShowComplete = (Action)Delegate.Combine(genericGameTooltip.OnShowComplete, OnShowComplete);
		}

		public void ShowResumeCountdown(int count)
		{
			resumeCountdownText.text = count.ToString();
			resumeCountdownText.gameObject.SetActive(true);
			resumeCountdownText.transform.localScale = Vector3.one;
			resumeCountdownText.transform.DOPunchScale(resumeCountdownBounceAmount * Vector3.one, resumeCountdownBounceTime);
		}

		public void HideResumeCountdown()
		{
			resumeCountdownText.transform.DOScale(Vector3.zero, resumeCountdownBounceTime).SetEase(Ease.InBack).OnComplete(delegate
			{
				resumeCountdownText.gameObject.SetActive(false);
			});
		}

		public override void Hide()
		{
			OnHide();
			float delay = Mathf.Max(mHint.hideAnimationTime, mHeartCount.hideAnimationDuration);
			mHint.Hide();
			mHeartCount.Hide();
			if (mCoinCount != null)
			{
				mCoinCount.Hide();
			}
			DOVirtual.DelayedCall(delay, delegate
			{
				OnHideComplete();
			});
		}
	}
}
