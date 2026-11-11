using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Tray.Drop
{
	public class GamePlayingUI : DropUIScreen
	{
		private const float SHOW_NEW_BEST_SCORE_BADGE_TIME = 0.5f;

		private const float HIDE_NEW_BEST_SCORE_BADGE_TIME = 0.2f;

		public Text SeedText;

		public Text CurrentPlatformText;

		[Space(10f)]
		public SimpleBanner ScoreBanner;

		public SimpleBanner BestScoreBanner;

		[Space(10f)]
		public Animator TutorialAnimator;

		[Space(10f)]
		public RectTransform NewBestScoreBadge;

		[Space(10f)]
		public int DelayBeforeShowingScore;

		private DropGame dropGame;

		private bool isCurrentScoreVisible;

		private bool mCurScoreIsBest;

		private void Start()
		{
			SeedText.text = "Seed: " + dropGame.LevelGenerator.RandomSeed;
			isCurrentScoreVisible = false;
			if (dropGame.BestScore > 0)
			{
				mCurScoreIsBest = false;
			}
			else
			{
				mCurScoreIsBest = true;
			}
			NewBestScoreBadge.transform.localScale = Vector3.zero;
		}

		public void DebugEnableBestScoreBanner()
		{
			if (dropGame.Score >= DelayBeforeShowingScore)
			{
				BestScoreBanner.Show();
			}
			mCurScoreIsBest = false;
		}

		private void OnEnable()
		{
			dropGame = DropGame.Instance;
			DropPlayer player = dropGame.Player;
			player.OnLandCountUpdated = (Action<int>)Delegate.Combine(player.OnLandCountUpdated, new Action<int>(UpdateHopCount));
			DropPlayer player2 = dropGame.Player;
			player2.OnDie = (Action)Delegate.Combine(player2.OnDie, new Action(OnPlayerDie));
			DropGame obj = dropGame;
			obj.OnScoreUpdated = (Action<int>)Delegate.Combine(obj.OnScoreUpdated, new Action<int>(UpdateScore));
		}

		private void OnDisable()
		{
			DropPlayer player = dropGame.Player;
			player.OnLandCountUpdated = (Action<int>)Delegate.Remove(player.OnLandCountUpdated, new Action<int>(UpdateHopCount));
			DropPlayer player2 = dropGame.Player;
			player2.OnDie = (Action)Delegate.Remove(player2.OnDie, new Action(OnPlayerDie));
			DropGame obj = dropGame;
			obj.OnScoreUpdated = (Action<int>)Delegate.Remove(obj.OnScoreUpdated, new Action<int>(UpdateScore));
		}

		private void UpdateHopCount(int count)
		{
			TutorialAnimator.SetTrigger("PlayerDead");
			//TutorialAnimator.SetInteger("TutorialStep", count);
			if (dropGame.Player.CurrentPlatform != null)
			{
				CurrentPlatformText.text = dropGame.Player.CurrentPlatform.name;
			}
		}

		private void UpdateScore(int score)
		{
			if (score < DelayBeforeShowingScore)
			{
				return;
			}
			if (!isCurrentScoreVisible)
			{
				ScoreBanner.Show();
				if (dropGame.BestScore > 0)
				{
					DOVirtual.DelayedCall(0.3f, delegate
					{
						BestScoreBanner.Show();
					});
				}
				isCurrentScoreVisible = true;
			}
			else if (score > dropGame.BestScore && !mCurScoreIsBest)
			{
				mCurScoreIsBest = true;
				ShowBestScoreBadge();
				DropAudio.PlaySound("SFX/Gameplay/Player/NewHighScore");
			}
		}

		private void ShowBestScoreBadge()
		{
			NewBestScoreBadge.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
		}

		private void OnPlayerDie()
		{
			TutorialAnimator.SetTrigger("PlayerDead");
		}

		public override void HideAndDestroy()
		{
			Sequence s = DOTween.Sequence();
			NewBestScoreBadge.DOScale(0f, 0.2f).SetEase(Ease.InBack);
			s.InsertCallback(0f, delegate
			{
				BestScoreBanner.Hide();
			});
			s.InsertCallback(0.3f, delegate
			{
				ScoreBanner.Hide();
			});
			s.AppendInterval(2f);
			s.AppendCallback(delegate
			{
				UnityEngine.Object.Destroy(base.gameObject);
			});
		}
	}
}
