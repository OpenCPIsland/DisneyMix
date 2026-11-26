using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Disney.Mix.SDK;
using Disney.PushNotificationUnityPlugin;
using Mix.DeviceDb;
using Mix.Session;
using Mix.Session.Extensions;
using Mix.Ui;
using UnityEngine;

namespace Mix
{
	public class PushNotifications : MonoSingleton<PushNotifications>
	{
		private const int MAX_PUSH_NOTIFICATION_RETRIES = 3;

		private IPushNotificationClient pushNotificationClient;

		private string token = string.Empty;

		private PushNotificationService service;

		private SdkActions actionGenerator = new SdkActions();

		private int notificationsAtStartUp;

		private string matchingThreadId;

		private NavigationRequest pushNotificationNavigationRequest;

		private Queue<AbstractNotificationReceivedEventArgs> pendingPushNotifications = new Queue<AbstractNotificationReceivedEventArgs>();

		private string profileName = "prod";

		private bool checkingForToken;

		private int tokenCheckCount;

		private int pushNotificationSettingRetries;

		public bool HaveShownPushPrePopup
		{
			get
			{
				return Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.LoadDeviceValueAsBool("spark.PushNotifications.MessageShown", false);
			}
			set
			{
				Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.SaveDeviceValueFromBool("spark.PushNotifications.MessageShown", value);
			}
		}

		private void Awake()
		{
		}

		public void Init()
		{
			switch (UnityEngine.Application.platform)
			{
			case RuntimePlatform.IPhonePlayer:
				pushNotificationClient = new ApplePushNotificationServiceClient();
				service = PushNotificationService.ApplePushNotificationService;
				break;
			case RuntimePlatform.Android:
				// GCM is gone (Google shut it down in 2019): the Glacier server
				// pushes to the app itself. See GlacierPushClient.
				pushNotificationClient = new GlacierPushClient(ExternalizedConstants.MixPlatformServicesUrl);
				service = PushNotificationService.Glacier;
				break;
			}
			if (pushNotificationClient != null)
			{
				pushNotificationClient.OnTokenGenerated += HandleToken;
				pushNotificationClient.OnNotificationReceived += HandleNotification;
				pushNotificationClient.OnResume();
				StartCoroutine(Updater());
			}
			MixSession.OnSessionInitialized += OnSessionInitialized;
		}

		public void OnSessionPause(bool isPausing)
		{
			if (pushNotificationClient == null)
			{
				return;
			}
			if (isPausing)
			{
				pushNotificationClient.OnPause();
				if (checkingForToken)
				{
					CancelInvoke("CheckForToken");
				}
				return;
			}
			if (MixSession.Session != null && MixSession.Session.LocalUser != null)
			{
				if (string.IsNullOrEmpty(token) && pushNotificationClient.IsNotificationEnabled())
				{
					ToggleRegister(true, false);
				}
				else if (!string.IsNullOrEmpty(token) && !pushNotificationClient.IsNotificationEnabled())
				{
					ToggleRegister(false, false);
				}
				else if (!string.IsNullOrEmpty(token) && Singleton<SettingsManager>.Instance.GetPushNotificationsSetting())
				{
					// e.g. the player just allowed notifications in the system dialog.
					SetListening(true);
				}
			}
			if (!pushNotificationClient.IsNotificationEnabled())
			{
				Singleton<SettingsManager>.Instance.RemovePushNotificationsSetting();
			}
			pushNotificationClient.OnResume();
			if (MixSession.IsValidSession)
			{
				pushNotificationClient.Update();
				Invoke("RetryNavRequest", 0.25f);
			}
		}

		private void HandleToken(object aSender, AbstractTokenGeneratedEventArgs aArguments)
		{
			token = aArguments.Token;
			if (!string.IsNullOrEmpty(token))
			{
				if (Singleton<SettingsManager>.Instance.GetPushNotificationsSetting())
				{
					ToggleAllPushNotifications(true);
				}
				else
				{
					EnableInvisiblePushNotifications();
				}
			}
			else if (tokenCheckCount++ < 5)
			{
				checkingForToken = true;
				Invoke("CheckForToken", 0.5f);
			}
			else
			{
				ToggleAllPushNotifications(false);
			}
		}

		private string GetStringFromKey(IDictionary aData, string aKey)
		{
			string text = string.Empty;
			if (aData is Dictionary<string, object> && (aData as Dictionary<string, object>).ContainsKey(aKey))
			{
				text = (aData as Dictionary<string, object>)[aKey].ToString();
			}
			else if (aData is Hashtable && (aData as Hashtable).ContainsKey(aKey))
			{
				text = (aData as Hashtable)[aKey].ToString();
			}
			return text.ToLower();
		}

		private void HandleNotification(object aSender, AbstractNotificationReceivedEventArgs aArguments)
		{
			if (aArguments == null)
			{
				return;
			}
			if (!MixSession.IsValidSession)
			{
				pendingPushNotifications.Enqueue(aArguments);
				return;
			}
			IPushNotification pushNotification = null;
			try
			{
				pushNotification = MixSession.Session.LocalUser.ReceivePushNotification(aArguments.UserData);
			}
			catch (Exception exception)
			{
				Log.Exception("EXCEPTION parsing PUSH NOTE " + aArguments, exception);
				return;
			}
			if (!GetStringFromKey(aArguments.UserData, "inBackground").Equals("true") || !GetStringFromKey(aArguments.UserData, "Clicked").Equals("true"))
			{
				return;
			}
			notificationsAtStartUp--;
			if (pushNotification is IChatMessageAddedPushNotification)
			{
				if (notificationsAtStartUp <= 0)
				{
					Analytics.LogPushNotificationAdAction("message_received");
				}
				IChatMessageAddedPushNotification chatMessageAddedPushNotification = (IChatMessageAddedPushNotification)pushNotification;
				if (chatMessageAddedPushNotification != null && !string.IsNullOrEmpty(chatMessageAddedPushNotification.ChatThreadId))
				{
					IChatThread chatThread = FindThreadForId(chatMessageAddedPushNotification.ChatThreadId);
					if (chatThread != null)
					{
						pushNotificationNavigationRequest = ChatHelper.NavigateToChatScreen(chatThread, new TransitionNone(), true, false);
					}
					else if (string.IsNullOrEmpty(matchingThreadId))
					{
						matchingThreadId = chatMessageAddedPushNotification.ChatThreadId;
					}
				}
			}
			else if (pushNotification is IFriendshipInvitationReceivedPushNotification)
			{
				if (notificationsAtStartUp <= 0)
				{
					Analytics.LogPushNotificationAdAction("friend_request");
				}
				IFriendshipInvitationReceivedPushNotification friendshipInvitationReceivedPushNotification = (IFriendshipInvitationReceivedPushNotification)pushNotification;
				if (friendshipInvitationReceivedPushNotification != null && !string.IsNullOrEmpty(friendshipInvitationReceivedPushNotification.FriendshipInvitationId))
				{
					NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Friends/FriendsScreen", new TransitionNone());
					pushNotificationNavigationRequest = navigationRequest;
				}
			}
			else if (pushNotification is IFriendshipAddedPushNotification)
			{
				if (notificationsAtStartUp <= 0)
				{
					Analytics.LogPushNotificationAdAction("friend_request_accepted");
				}
				IFriendshipAddedPushNotification friendshipAddedPushNotification = (IFriendshipAddedPushNotification)pushNotification;
				if (friendshipAddedPushNotification != null && !string.IsNullOrEmpty(friendshipAddedPushNotification.FriendId))
				{
					NavigationRequest navigationRequest2 = new NavigationRequest("Prefabs/Screens/Friends/FriendsScreen", new TransitionNone());
					pushNotificationNavigationRequest = navigationRequest2;
				}
			}
		}

		public static string IDictionaryToString(IDictionary payload)
		{
			StringBuilder stringBuilder = new StringBuilder(":\n");
			foreach (DictionaryEntry item in payload)
			{
				stringBuilder.Append(item.Key);
				stringBuilder.Append(" = ");
				stringBuilder.Append(item.Value);
				stringBuilder.Append('\n');
			}
			return stringBuilder.ToString();
		}

		private void OnSessionInitialized()
		{
			while (pendingPushNotifications.Count > 0)
			{
				HandleNotification(pushNotificationClient, pendingPushNotifications.Dequeue());
			}
		}

		private IChatThread FindThreadForId(string threadId)
		{
			IChatThread chatThread = null;
			if (MixSession.IsValidSession && MixSession.Session.LocalUser.ChatThreadsFromUsers() != null)
			{
				chatThread = MixSession.Session.LocalUser.ChatThreadsFromUsers().FirstOrDefault((IChatThread thread) => thread.Id.Equals(threadId));
				if (chatThread != null && chatThread is IOfficialAccountChatThread && ((IOfficialAccountChatThread)chatThread).OfficialAccount.AccountId.Equals(FakeFriendManager.OAID))
				{
					chatThread = MonoSingleton<FakeFriendManager>.Instance.fakeThread;
				}
			}
			return chatThread;
		}

		private IEnumerator Updater()
		{
			while (true)
			{
				pushNotificationClient.Update();
				RetryNavRequest();
				yield return new WaitForSeconds(1f);
			}
		}

		public void ToggleRegister(bool aEnable, bool aFirstRunOnDevice)
		{
			if (pushNotificationClient != null)
			{
				if (aEnable)
				{
					pushNotificationClient.Register();
					checkingForToken = true;
					Invoke("CheckForToken", 1f);
				}
				else
				{
					EnableInvisiblePushNotifications();
				}
			}
		}

		public bool IsDeviceNotificationEnabled()
		{
			return pushNotificationClient != null && pushNotificationClient.IsNotificationEnabled();
		}

		private void CheckForToken()
		{
			checkingForToken = false;
			if (pushNotificationClient != null)
			{
				pushNotificationClient.CheckForToken();
			}
		}

		public void ToggleAllPushNotifications(bool aEnable)
		{
			if (pushNotificationClient == null || !MixSession.IsValidSession)
			{
				return;
			}
			if (aEnable && !string.IsNullOrEmpty(token))
			{
				Singleton<SettingsManager>.Instance.SetPushNotificationsSetting(true);
				MixSession.Session.LocalUser.EnableAllPushNotifications(token, service, profileName, actionGenerator.CreateAction(delegate(IEnableAllPushNotificationsResult aResult)
				{
					if (!aResult.Success)
					{
						if (pushNotificationSettingRetries < 3)
						{
							pushNotificationSettingRetries++;
							ToggleAllPushNotifications(true);
						}
						else
						{
							pushNotificationSettingRetries = 0;
						}
					}
					else
					{
						pushNotificationSettingRetries = 0;
						SetListening(true);
					}
				}));
				return;
			}
			Singleton<SettingsManager>.Instance.SetPushNotificationsSetting(false);
			MixSession.Session.LocalUser.DisableAllPushNotifications(actionGenerator.CreateAction(delegate(IDisableAllPushNotificationsResult aResult)
			{
				if (!aResult.Success)
				{
					if (pushNotificationSettingRetries < 3)
					{
						pushNotificationSettingRetries++;
						ToggleAllPushNotifications(false);
					}
					else
					{
						pushNotificationSettingRetries = 0;
					}
				}
				else
				{
					pushNotificationSettingRetries = 0;
					SetListening(false);
				}
			}));
		}

		public void ToggleVisiblePushNotifications(bool aEnable)
		{
			if (pushNotificationClient == null || !MixSession.IsValidSession)
			{
				return;
			}
			if (aEnable && !string.IsNullOrEmpty(token))
			{
				Singleton<SettingsManager>.Instance.SetPushNotificationsSetting(true);
				MixSession.Session.LocalUser.EnableAllPushNotifications(token, service, profileName, actionGenerator.CreateAction(delegate(IEnableAllPushNotificationsResult aResult)
				{
					if (!aResult.Success)
					{
						if (pushNotificationSettingRetries < 3)
						{
							pushNotificationSettingRetries++;
							ToggleVisiblePushNotifications(true);
						}
						else
						{
							pushNotificationSettingRetries = 0;
						}
					}
					else
					{
						pushNotificationSettingRetries = 0;
						SetListening(true);
					}
				}));
				return;
			}
			Singleton<SettingsManager>.Instance.SetPushNotificationsSetting(false);
			MixSession.Session.LocalUser.DisableVisiblePushNotifications(actionGenerator.CreateAction(delegate(IDisableVisiblePushNotificationsResult aResult)
			{
				if (!aResult.Success)
				{
					if (pushNotificationSettingRetries < 3)
					{
						pushNotificationSettingRetries++;
						ToggleVisiblePushNotifications(false);
					}
					else
					{
						pushNotificationSettingRetries = 0;
					}
				}
				else
				{
					pushNotificationSettingRetries = 0;
					SetListening(false);
				}
			}));
		}

		public void EnableInvisiblePushNotifications()
		{
			if (pushNotificationClient == null || !MixSession.IsValidSession || string.IsNullOrEmpty(token))
			{
				return;
			}
			MixSession.Session.LocalUser.EnableInvisiblePushNotifications(token, service, profileName, actionGenerator.CreateAction(delegate(IEnableInvisiblePushNotificationsResult aResult)
			{
				if (!aResult.Success)
				{
					if (pushNotificationSettingRetries < 3)
					{
						pushNotificationSettingRetries++;
						EnableInvisiblePushNotifications();
					}
					else
					{
						pushNotificationSettingRetries = 0;
					}
				}
				else
				{
					pushNotificationSettingRetries = 0;
					SetListening(false);
				}
			}));
		}

		// The Glacier client's background connection runs only while the
		// server has this device registered for visible pushes.
		private void SetListening(bool aListen)
		{
			GlacierPushClient glacierClient = pushNotificationClient as GlacierPushClient;
			if (glacierClient == null)
			{
				return;
			}
			if (aListen)
			{
				glacierClient.StartListening();
			}
			else
			{
				glacierClient.StopListening();
			}
		}

		// Before the session goes away: this device stops getting the
		// account's pushes. The Glacier client also drops its token, so
		// the next account to log in registers a new one.
		public void OnLogout()
		{
			if (pushNotificationClient is GlacierPushClient)
			{
				pushNotificationClient.Unregister();
				token = string.Empty;
				tokenCheckCount = 0;
			}
			ClearNavRequest();
			pendingPushNotifications.Clear();
		}

		public void RetryNavRequest(bool fallbackToConvo = false)
		{
			if (!string.IsNullOrEmpty(matchingThreadId))
			{
				IChatThread chatThread = FindThreadForId(matchingThreadId);
				if (chatThread != null)
				{
					pushNotificationNavigationRequest = ChatHelper.NavigateToChatScreen(chatThread, new TransitionNone(), true, false);
					matchingThreadId = null;
				}
				else if (fallbackToConvo)
				{
					Log.Exception("unable to find thread id " + matchingThreadId + " after retry so nav to convo");
					NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Conversations/ConversationsScreen", new TransitionNone());
					pushNotificationNavigationRequest = navigationRequest;
					matchingThreadId = null;
				}
			}
			if (pushNotificationNavigationRequest != null)
			{
				if (!Singleton<ForceUpdate>.Instance.CheckForForceUpdate() && !MixSession.ParentalConsentRequired)
				{
					MonoSingleton<NavigationManager>.Instance.AddRequest(pushNotificationNavigationRequest);
				}
				pushNotificationNavigationRequest = null;
			}
		}

		public void ClearNavRequest()
		{
			pushNotificationNavigationRequest = null;
			matchingThreadId = null;
		}

		public bool IsPendingNavRequest()
		{
			return pushNotificationNavigationRequest != null || matchingThreadId != null || pendingPushNotifications.Count > 0;
		}
	}
}
