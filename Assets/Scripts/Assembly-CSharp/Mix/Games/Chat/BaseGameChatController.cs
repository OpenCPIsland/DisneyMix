using System;
using System.Collections.Generic;
using Mix.Games.Data;
using Mix.Games.Message;
using Mix.Games.Session;
using Mix.Games.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Games.Chat
{
	public abstract class BaseGameChatController : MonoBehaviour
	{
		private const int UNLIMITED_ATTEMPTS = 0;

		public const string NA = "C9G";

		protected MixGameData mGameData;

		protected List<MixGameResponse> mResponses;

		protected MixGameResponse mGameResponseData;

		protected IEntitlementGameData mEntitlement;

		protected ChatThreadGameSession mSession;

		protected GameChatItem mHolder;

		protected bool mHideFooter;

		protected bool mHideTail;

		public Text GameOwnerText;

		public Text GameTimeText;

		public GameObject FooterPadding;

		public Color FooterColor;

		public Color TailColor;

		public bool HasAttempts
		{
			get
			{
				if (mSession != null && mSession.ThreadParameters != null && mSession.IsFakeThread)
				{
					return false;
				}
				if (mResponses == null)
				{
					return true;
				}
				return AttemptsAvailable(mResponses);
			}
		}

		public GameObject Loader
		{
			get
			{
				return mHolder.ContextualLoader;
			}
		}

		protected GameObject Footer
		{
			get
			{
				return mHolder.Footer;
			}
		}

		protected GameObject Tail
		{
			get
			{
				return mHolder.Tail;
			}
		}

		public abstract void SetupGameData(string aGameDataJson);

		protected bool AttemptsAvailable(List<MixGameResponse> aResponses)
		{
			int num = 0;
			foreach (MixGameResponse aResponse in aResponses)
			{
				if (string.Equals(aResponse.PlayerSwid, mSession.PlayerId))
				{
					num = aResponse.Attempts;
					break;
				}
			}
			int num2 = int.Parse(mEntitlement.GetAttempts());
			if (num2 < 0)
			{
				mSession.Logger.Log("Invalid value for max attempts: " + num2);
				num2 = 0;
			}
			bool flag = num < num2 || (num2 == 0 && !mSession.IsFakeThread);
			if (mSession.ThreadParameters.IsOneOnOneSession && !mSession.ThreadParameters.IsOneOnOneFriend)
			{
				flag = false;
			}
			if (num2 != 0)
			{
				string empty = string.Empty;
				bool flag2 = string.Equals(b: (!(mSession.MessageData is IGameEventMessageData)) ? mSession.MessageData.SenderId : (mSession.MessageData as IGameEventMessageData).StateMessageSenderId, a: mSession.PlayerId);
				flag = flag && !flag2;
			}
			return flag;
		}

		public void CancelBundles()
		{
			mSession.CancelBundles();
		}

		public void DestroyBundleInstance(string aPath, UnityEngine.Object aObject = null)
		{
			mSession.DestroyBundleInstance(aPath, aObject);
		}

		protected string GetGameData()
		{
			string result = null;
			if (mSession.MessageData is IGameStateMessageData)
			{
				result = (mSession.MessageData as IGameStateMessageData).GetJson();
			}
			else if (mSession.MessageData is IGameEventMessageData)
			{
				result = (mSession.MessageData as IGameEventMessageData).GetJson();
			}
			return result;
		}

		public string GetLocalizedString(string aToken)
		{
			return mSession.GetLocalizedString(aToken);
		}

		public virtual void Initialize(ChatThreadGameSession aSession, IEntitlementGameData aEntitlement, GameChatItem aHolder)
		{
			if (aSession == null)
			{
				mSession.Logger.Log("BaseGameChatController was passed a null session.");
				return;
			}
			mSession = aSession;
			mHolder = aHolder;
			mEntitlement = aEntitlement;
			mSession.GameSessionHandler.OnUnfriended += HandleUnfriending;
			mSession.GameSessionHandler.OnGameStarted += HandleGameStarted;
			mSession.GameSessionHandler.OnGameClosed += HandleGameClosed;
			mSession.GameSessionHandler.OnGameStateUpdated += HandleGameStateUpdated;
			string gameData = GetGameData();
			SetupGameData(gameData);
			SetupView();
		}

		public void HandleUnfriending(object sender, EventArgs args)
		{
			shouldDisplayPlayButton(false);
		}

		private void HandleGameStarted(object sender, GameStartedEventArgs args)
		{
			if (args.gameStateId == mSession.MessageData.Id)
			{
				shouldDisplayPlayButton(false);
			}
		}

		private void HandleGameClosed(object sender, GameClosedEventArgs args)
		{
			if (string.Equals(args.gameStateId, mSession.MessageData.Id))
			{
				shouldDisplayPlayButton(HasAttempts);
			}
		}

		private void HandleGameStateUpdated(object sender, GameStateUpdatedEventArgs args)
		{
			if (string.Equals(args.gameStateId, mSession.MessageData.Id))
			{
				SetupGameData(args.gameDataJson);
				shouldDisplayPlayButton(HasAttempts);
			}
		}

		protected virtual void OnDestroy()
		{
			if (mSession != null)
			{
				mSession.GameSessionHandler.OnUnfriended -= HandleUnfriending;
				mSession.GameSessionHandler.OnGameStarted -= HandleGameStarted;
				mSession.GameSessionHandler.OnGameClosed -= HandleGameClosed;
				mSession.GameSessionHandler.OnGameStateUpdated -= HandleGameStateUpdated;
			}
		}

		public void LoadAsset(IGameAsset aSessionAsset, string aPath, object aParam = null)
		{
			if (mSession != null)
			{
				mSession.LoadAsset(aSessionAsset, aPath, aParam);
			}
			else
			{
				mSession.Logger.LogWarning("Warning: mSession is null and we're trying to access it!");
			}
		}

		public virtual void Play()
		{
			SetupGameData(mSession.MessageData.GetJson());
			if (HasAttempts)
			{
				mSession.LoadGame(mEntitlement);
			}
		}

		protected virtual void SetupView()
		{
			if (mSession.MessageData != null && GameTimeText != null)
			{
				TimeSpan timeSpan = DateTime.UtcNow.Subtract(mSession.MessageData.TimeSent);
				string newValue = string.Empty;
				int num = Convert.ToInt32(timeSpan.TotalDays);
				int num2 = Convert.ToInt32(timeSpan.TotalHours);
				int num3 = Convert.ToInt32(timeSpan.TotalMinutes);
				if (num == 0)
				{
					if (num3 < 15)
					{
						newValue = mSession.GetLocalizedString("customtokens.game.gameitem_datetime_justnow");
					}
					else
					{
						switch (num2)
						{
						case 0:
							newValue = mSession.GetLocalizedString("customtokens.game.gameitem_datetime_minutes").Replace("#num#", num3.ToString());
							break;
						case 1:
							newValue = mSession.GetLocalizedString("customtokens.game.gameitem_datetime_hour");
							break;
						default:
							newValue = mSession.GetLocalizedString("customtokens.game.gameitem_datetime_hours").Replace("#num#", num2.ToString());
							break;
						}
					}
				}
				else if (num == 1)
				{
					newValue = mSession.GetLocalizedString("customtokens.game.gameitem_datetime_yesterday");
				}
				else if (num > 1)
				{
					newValue = mSession.GetLocalizedString("customtokens.game.gameitem_datetime_days").Replace("#num#", num.ToString());
				}
				string localizedString = mSession.GetLocalizedString("customtokens.game.gameitem_started");
				GameTimeText.text = localizedString.Replace("#time#", newValue);
			}
			bool flag = !string.IsNullOrEmpty(mSession.MessageData.Id);
			shouldDisplayPlayButton(HasAttempts && flag);
		}

		public void UpdateHeight(int aHeight = -1)
		{
		}

		public T GetGameData<T>() where T : MixGameData
		{
			return (T)mGameData;
		}

		private void setDisplayFooterAndTail(bool shouldDisplay)
		{
			if (!mHideFooter && mHolder.FooterImage != null && FooterPadding != null)
			{
				mHolder.FooterImage.color = FooterColor;
				Footer.SetActive(shouldDisplay);
				FooterPadding.SetActive(Footer.activeSelf);
				if (!mHideTail && mHolder.TailImage != null)
				{
					mHolder.TailImage.color = TailColor;
					Tail.SetActive(!Footer.activeSelf);
				}
			}
		}

		private void shouldDisplayPlayButton(bool shouldDisplay)
		{
			setDisplayFooterAndTail(shouldDisplay);
			if (mHolder != null)
			{
				Button componentInChildren = mHolder.GetComponentInChildren<Button>();
				if (componentInChildren != null)
				{
					componentInChildren.interactable = shouldDisplay;
				}
				else
				{
					mSession.Logger.LogWarning("No play button in chat object!");
				}
			}
		}
	}
}
