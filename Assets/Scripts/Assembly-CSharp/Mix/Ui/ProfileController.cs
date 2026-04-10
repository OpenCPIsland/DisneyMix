using System;
using System.Collections.Generic;
using System.Linq;
using Avatar;
using Disney.Mix.SDK;
using Mix.Avatar;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.Localization;
using Mix.Session;
using Mix.User;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ProfileController : BaseController, ISpinnerListener, IDisplayNamePanelCallback
	{
		public enum PROFILE_MODES
		{
			PROFILE = 0,
			INFO_UPDATE = 1,
			FIRST_TIME = 2
		}

		private const string LOADING_AVATAR_TOKEN = "customtokens.profile.loading_avatar";

		private const string EDIT_AVATAR_TOKEN = "profilescreen.editavatarbtn.edittext";

		private const string AVATAR_DEFAULT_STATE = "Default";

		private PROFILE_MODES mode;

		public GameObject DisplayNameGameObject;

		public Text FirstName;

		public Text DisplayName;

		public GameObject AwaitingApproval;

		public Text AwaitingApprovalText;

		public GameObject chatNotificationBadge;

		public GameObject friendsNotificationBadge;

		public Animator friendsButtonAnimator;

		public Button AvatarEditorButton;

		public Animator AvatarEditorAnimator;

		public Text AvatarEditorText;

		public GameObject TouchTooltip;

		public AvatarObjectSpawner AvatarSpawner;

		public AvatarClosetPanel costumeList;

		public AvatarIconizer iconizer;

		public ColorToggleItem[] toggles;

		private AvatarSpinner avatarSpinner;

		private bool updateBadgeCount;

		private IKeyValDatabaseApi databaseApi;

		private AvatarFlags flags;

		private bool avatarEditingEnabled = true;

		private IAvatar originalDNA;

		private bool avatarOutfitChanged;

		public override void OnDataReceived(string aToken, object aData)
		{
			if (aToken == "mode")
			{
				mode = (PROFILE_MODES)(int)aData;
			}
		}

		public override void OnAndroidBackButtonClicked()
		{
			if (!Singleton<PanelManager>.Instance.OnAndroidBackButton())
			{
				GoToChat();
			}
		}

		public void OnSettingsClicked()
		{
			NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Settings/SettingsScreen", new TransitionAnimations());
			MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest);
		}

		public void OnLogoutClicked()
		{
			MonoSingleton<LoginManager>.Instance.Logout();
			Analytics.LogNavigationAction("main_nav", "logout");
			NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionInLeft());
			MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest);
		}

		public void OnColorThemeClicked(ColorToggleItem aColorToggleItem)
		{
			if (databaseApi == null || aColorToggleItem == null)
				return;

			string text = databaseApi.LoadUserValue("default_primary_color");
			if (text == null || !text.Equals(aColorToggleItem.PrimaryColor))
			{
				Analytics.LogAppColorChangeAction(aColorToggleItem.PrimaryColor);
				if (avatarSpinner != null && !avatarSpinner.IsNullOrDisposed())
				{
					avatarSpinner.PlayAnimationTrigger("ChangedColorPreference");
				}
			}
			databaseApi.SaveUserValue("default_primary_color", aColorToggleItem.PrimaryColor);
			databaseApi.SaveUserValue("default_secondary_color", aColorToggleItem.SecondaryColor);

			// Use the new API for finding objects and add null checks
			var tintImages = UnityEngine.Object.FindObjectsByType<TintImage>(FindObjectsSortMode.None);
			if (tintImages != null)
			{
				foreach (TintImage tintImage in tintImages)
				{
					if (tintImage != null)
						tintImage.OnColorThemeChanged();
				}
			}

			var tintTexts = UnityEngine.Object.FindObjectsByType<TintText>(FindObjectsSortMode.None);
			if (tintTexts != null)
			{
				foreach (TintText tintText in tintTexts)
				{
					if (tintText != null)
						tintText.OnColorThemeChanged();
				}
			}
		}

		public void OnEditClicked()
		{
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/AvatarEditor/AvatarEditorScreen", new TransitionAnimations());
			navigationRequest.AddData("mode", AvatarEditorController.EDITOR_MODES.EDITOR);
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void OnChatButtonClicked()
		{
			if (base.transform.Find("UI_BG_Holder/UI_BG_ProfileScreen/BottomNav/ChatsBtn").GetComponent<PressureSensitiveButton>().HardPress)
			{
				NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Conversations/ConversationsScreen", new TransitionNone());
				navigationRequest.PopLastRequest = true;
				navigationRequest.AddData("startConversation", true);
				MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
			}
			else
			{
				GoToChat();
			}
		}

		public void OnFriendsButtonClicked()
		{
			if (MixSession.ParentalConsentRequired)
			{
				ParentalConsent.ShowGateDialog();
				return;
			}
			Analytics.LogNavigationAction("history", "friend_list");
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Friends/FriendsScreen", new TransitionNone());
			if (base.transform.Find("UI_BG_Holder/UI_BG_ProfileScreen/BottomNav/FriendsBtn").GetComponent<PressureSensitiveButton>().HardPress)
			{
				navigationRequest.AddData("qrCode", true);
			}
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void OnStopSpinning()
		{
		}

		public void AtRest()
		{
		}

		public void OnApplicationPause(bool goingToBackground)
		{
			if (!goingToBackground && iconizer != null)
			{
				iconizer.CleanupDragging(null);
			}
		}

		public void AtMaxSpin()
		{
		}

		public void AvatarLoadComplete(bool success, string dnaSha)
		{
			if (avatarSpinner != null && avatarSpinner.Avatar != null)
			{
				avatarSpinner.Avatar.SetActive(true);
			}
			iconizer.SetCurrentDna(MonoSingleton<AvatarManager>.Instance.CurrentDna);
		}

		public void OnDisplayNameUpdated(DisplayNamePanel aPanel)
		{
			if (mode == PROFILE_MODES.INFO_UPDATE)
			{
				GoToChat();
				return;
			}
			if (mode == PROFILE_MODES.FIRST_TIME)
			{
				GoToChat(true);
				return;
			}
			DisplayNameProposedStatus displayNameProposedStatus = MixSession.User.RegistrationProfile.DisplayNameProposedStatus;
			if (displayNameProposedStatus != DisplayNameProposedStatus.Accepted)
			{
				DisplayName.text = MixChat.FormatDisplayName(aPanel.DisplayNameInputField.text);
				DisplayNameGameObject.SetActive(true);
			}
		}

		public override void OnUILoaded(NavigationRequest aNavigationRequest = null)
		{
			MixFriends.OnBadgesChanged += HandleOnRefreshInvitations;
			MixSession.OnConnectionChanged += HandleConnectionChanged;
			UpdateBadges();
		}

		public override void OnUIUnLoaded(NavigationRequest aNavigationRequest = null)
		{
			if (avatarOutfitChanged)
			{
				SaveAvatar();
				avatarOutfitChanged = false;
			}
		}

		private void SaveAvatar()
		{
			Analytics.LogCreateAvatarSuccess(AvatarEditorController.EDITOR_MODES.EDITOR, MonoSingleton<AvatarManager>.Instance.CurrentDna);
			MonoSingleton<AvatarManager>.Instance.setCurrentUsersDna(MonoSingleton<AvatarManager>.Instance.CurrentDna);
			MonoSingleton<AvatarManager>.Instance.saveCurrentUsersDna();
		}

		private void HandleConnectionChanged(MixSession.ConnectionState newState, MixSession.ConnectionState oldState)
		{
			if (newState == MixSession.ConnectionState.ONLINE)
			{
				updateBadgeCount = true;
				enableAvatarEditor();
			}
		}

		private void HandleOnRefreshInvitations()
		{
			updateBadgeCount = true;
		}

		public void OnDestroy()
		{
			if (avatarOutfitChanged)
			{
				SaveAvatar();
				avatarOutfitChanged = false;
			}
			MixFriends.OnBadgesChanged -= HandleOnRefreshInvitations;
			MixSession.OnConnectionChanged -= HandleConnectionChanged;
		}

		public static void ShowDisplayNameAccepted(string newName, Action aCallback)
		{
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
			{
				{ genericPanel.TitleText, "customtokens.panels.display_name_approved" },
				{ genericPanel.MessageText, newName },
				{ genericPanel.ButtonOneText, null },
				{ genericPanel.ButtonTwoText, "customtokens.panels.button_ok" }
			});
			Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action> { { genericPanel.ButtonTwo, aCallback } });
			Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.SaveUserValueFromInt("displayname.approved.seen", 1);
		}

		public static void ShowDisplayNamePanels(IDisplayNamePanelCallback caller)
		{
			string text = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.LoadUserValue("displayname.approved.seen");
			IRegistrationProfile registrationProfile = MixSession.User.RegistrationProfile;
			if (registrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.None)
			{
				DisplayNamePanel displayNamePanel = (DisplayNamePanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.DISPLAY_NAME_SUBMITTED);
				displayNamePanel.Init(caller, registrationProfile);
			}
			else
			{
				if (registrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.Pending)
				{
					return;
				}
				if (registrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.Accepted)
				{
					if (!string.IsNullOrEmpty(text) && text == "0")
					{
						ShowDisplayNameAccepted(registrationProfile.DisplayName, delegate
						{
						});
					}
				}
				else if (registrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.Rejected)
				{
					DisplayNamePanel displayNamePanel2 = (DisplayNamePanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.DISPLAY_NAME_REJECTED);
					displayNamePanel2.Init(caller, registrationProfile);
				}
			}
		}

		public override void OnUITransitionEnd()
		{
			IRegistrationProfile registrationProfile = MixSession.User.RegistrationProfile;
			if (registrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.Pending)
			{
				DisplayName.text = MixChat.FormatDisplayName(registrationProfile.DisplayName);
				DisplayNameGameObject.SetActive(true);
				LocalizedText component = AwaitingApproval.GetComponent<LocalizedText>();
				component.doNotLocalize = true;
				AwaitingApprovalText.text = Singleton<Localizer>.Instance.getString(component.token).Replace("#PendingName#", registrationProfile.ProposedDisplayName);
				AwaitingApproval.SetActive(true);
				Analytics.LogProfilePageView();
			}
			else if (registrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.Accepted)
			{
				DisplayName.text = MixChat.FormatDisplayName(registrationProfile.DisplayName);
				DisplayNameGameObject.SetActive(true);
				AwaitingApproval.SetActive(false);
				Analytics.LogProfilePageView();
			}
			if (string.IsNullOrEmpty(registrationProfile.FirstName))
			{
				FirstName.text = registrationProfile.DisplayName;
				DisplayName.gameObject.SetActive(false);
			}
			else
			{
				FirstName.text = registrationProfile.FirstName;
			}
			if (Input.touchPressureSupported && MixSession.User.Friends.Count() > 1)
			{
				IKeyValDatabaseApi keyValDocumentCollectionApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
				if (!keyValDocumentCollectionApi.LoadUserValueAsBool("3dtouch.tooltip.seen.profile"))
				{
					TouchTooltip.SetActive(true);
					keyValDocumentCollectionApi.SaveUserValueFromBool("3dtouch.tooltip.seen.profile", true);
				}
			}
			ShowDisplayNamePanels(this);
		}

		private void Start()
		{
			if (Singleton<MixDocumentCollections>.Instance == null || Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi == null || toggles == null)
			{
				return;
			}
			databaseApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
			string text = databaseApi.LoadUserValue("default_primary_color");
			string text2 = text ?? "1C97D4";
			for (int i = 0; i < toggles.Length; i++)
			{
				if (text2.Equals(toggles[i].PrimaryColor))
				{
					toggles[i].GetComponent<Toggle>().isOn = true;
					break;
				}
			}
			GameObject avatarObject = AvatarSpawner.Init();
			avatarSpinner = GetComponent<AvatarSpinner>();
			if (avatarSpinner == null)
			{
			}
			avatarSpinner.InitSpinner(avatarObject);
			avatarSpinner.SetListener(this);
			if (avatarSpinner.Avatar != null)
			{
				avatarSpinner.Avatar.SetActive(false);
			}
			avatarSpinner.LoadDnaOnAvatar(MonoSingleton<AvatarManager>.Instance.CurrentDna, flags, AvatarLoadComplete);
			DisplayNameGameObject.SetActive(false);
			iconizer.SetCurrentDna(MonoSingleton<AvatarManager>.Instance.CurrentDna);
			originalDNA = MonoSingleton<AvatarManager>.Instance.CurrentDna;
			costumeList.gameObject.SetActive(true);
			costumeList.Show(MonoSingleton<AvatarManager>.Instance.CurrentDna, delegate(IAvatar dna)
			{
				Analytics.LogAvatarOutfitChosenAction();
				avatarOutfitChanged = !AvatarApi.AreAvatarsEqual(originalDNA, dna);
				MonoSingleton<AvatarManager>.Instance.setCurrentUsersDna(dna);
				avatarSpinner.LoadDnaOnAvatar(dna, flags, delegate(bool success, string sha)
				{
					avatarSpinner.PlayAnimationTrigger("LoadedFromCloset");
					AvatarLoadComplete(success, sha);
				});
				iconizer.SetCurrentDna(MonoSingleton<AvatarManager>.Instance.CurrentDna);
				costumeList.currentDna = MonoSingleton<AvatarManager>.Instance.CurrentDna;
			});
			if (MonoSingleton<ConnectionManager>.Instance.IsConnected && MixSession.connection == MixSession.ConnectionState.RESUMING)
			{
				disableAvatarEditor();
			}
			if (MixSession.connection == MixSession.ConnectionState.ONLINE)
			{
				enableAvatarEditor();
			}
		}

		private void disableAvatarEditor()
		{
			avatarEditingEnabled = false;
			AvatarEditorButton.interactable = false;
			AvatarEditorAnimator.enabled = true;
			AvatarEditorText.text = Singleton<Localizer>.Instance.getString("customtokens.profile.loading_avatar");
		}

		private void enableAvatarEditor()
		{
			AvatarEditorText.text = Singleton<Localizer>.Instance.getString("profilescreen.editavatarbtn.edittext");
			if (!avatarEditingEnabled)
			{
				avatarEditingEnabled = true;
				if (!AvatarEditorButton.IsNullOrDisposed() && !AvatarEditorAnimator.IsNullOrDisposed() && !AvatarEditorText.IsNullOrDisposed())
				{
					AvatarEditorButton.interactable = true;
					AvatarEditorAnimator.Play("Default");
				}
			}
		}

		private void Update()
		{
			MonoSingleton<FakeFriendManager>.Instance.HighlightAnimator(FakeFriendManager.TYPE_FRIEND_BUTTON, friendsButtonAnimator);
			if (updateBadgeCount)
			{
				UpdateBadges();
				updateBadgeCount = false;
			}
		}

		private void UpdateBadges()
		{
			uint totalDisplayableUnreadMessageCount = MixChat.GetTotalDisplayableUnreadMessageCount();
			if (totalDisplayableUnreadMessageCount != 0)
			{
				chatNotificationBadge.SetActive(true);
				chatNotificationBadge.transform.Find("NotificationCounterText").GetComponent<Text>().text = totalDisplayableUnreadMessageCount.ToString();
			}
			else
			{
				chatNotificationBadge.SetActive(false);
			}
			int maxDisplayableFriendInvites = MixFriends.GetMaxDisplayableFriendInvites();
			if (maxDisplayableFriendInvites > 0)
			{
				friendsNotificationBadge.SetActive(true);
				friendsNotificationBadge.transform.Find("NotificationCounterText").GetComponent<Text>().text = maxDisplayableFriendInvites.ToString();
			}
			else
			{
				friendsNotificationBadge.SetActive(false);
			}
		}

		private void GoToChat(bool aFirstTime = false)
		{
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Conversations/ConversationsScreen", new TransitionNone());
			navigationRequest.PopLastRequest = true;
			if (aFirstTime)
			{
				navigationRequest.AddData("regDone", true);
			}
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}
	}
}
