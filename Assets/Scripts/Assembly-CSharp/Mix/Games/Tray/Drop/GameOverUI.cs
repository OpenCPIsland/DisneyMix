using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mix.Games.Tray.Drop
{
	public class GameOverUI : DropUIScreen
	{
		private const float MIN_TIME_PER_TICK = 0.05f;

		public SimpleBanner Banner;

		public RectTransform ButtonParent;

		public Button RetryButton;

		public Button SaveAndQuitButton;

		public Transform HiScoreMarker;

		public float ScoreAnimTime = 1.5f;

		public float HiScoreAnimTime = 1f;

		public float CounterMaxAnimTime = 2f;

		public float DelayBeforeShowingButtons = 0.5f;

		private bool mShowHighScoreBadge;

		private int mScore;

		private Vector2 mButtonStartPos;

		private OffscreenScoreBanners offscreenScoreBanners;

		public void Init(bool aAllowRetry, UnityAction aRetryAction, UnityAction aSaveAction)
		{
			RetryButton.gameObject.SetActive(aAllowRetry);
			RetryButton.onClick.AddListener(aRetryAction);
			SaveAndQuitButton.onClick.AddListener(aSaveAction);
		}

		public void DisableButtons()
		{
			RetryButton.interactable = false;
			SaveAndQuitButton.interactable = false;
		}

		private void Start()
		{
			DropGame instance = DropGame.Instance;
			mScore = instance.Score;
			mShowHighScoreBadge = !instance.IsFirstRound && instance.Score > instance.BestScore;
			offscreenScoreBanners = Object.FindFirstObjectByType<OffscreenScoreBanners>();
			offscreenScoreBanners.ScoreCounter.JumpToValue(0);
			HiScoreMarker.gameObject.SetActive(false);
			ButtonParent.gameObject.SetActive(false);
			RetryButton.transform.localScale = Vector3.zero;
			SaveAndQuitButton.transform.localScale = Vector3.zero;
			ShowBaseScore();
		}

		private void OnDestroy()
		{
			RetryButton.onClick.RemoveAllListeners();
			SaveAndQuitButton.onClick.RemoveAllListeners();
		}

		private void ShowBaseScore()
		{
			float counterTime = Mathf.Min((float)mScore * 0.05f, CounterMaxAnimTime);
			Banner.Show();
			offscreenScoreBanners.ScoreCounter.OnCountFinished += FinishScore;
			DOVirtual.DelayedCall(1.5f, delegate
			{
				DropAudio.PlaySound("SFX/UI/FinalScoreCounterTick");
				offscreenScoreBanners.ScoreCounter.CountToTargetValue(mScore, counterTime);
			});
		}

		private void FinishScore(int finalValue)
		{
			offscreenScoreBanners.ScoreCounter.OnCountFinished -= FinishScore;
			ShowButtons();
			if (mShowHighScoreBadge)
			{
				HiScoreMarker.gameObject.SetActive(true);
				HiScoreMarker.DOScale(Vector3.zero, HiScoreAnimTime).From().SetEase(Ease.OutBack);
				DropAudio.PlaySound("SFX/Gameplay/Player/NewHighScore");
			}
			DropAudio.StopSound("SFX/UI/FinalScoreCounterTick");
			DropAudio.PlaySound("SFX/UI/FinalScoreCounterPunch");
		}

		private void ShowButtons()
		{
			ButtonParent.gameObject.SetActive(true);
			RetryButton.transform.DOScale(1f, 0.4f).SetEase(Ease.OutQuad).SetDelay(DelayBeforeShowingButtons);
			SaveAndQuitButton.transform.DOScale(1f, 0.4f).SetEase(Ease.OutQuad).SetDelay(DelayBeforeShowingButtons + 0.1f);
		}
	}
}
