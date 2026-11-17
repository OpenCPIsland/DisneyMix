using System;
using System.Collections.Generic;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Mix.Assets;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.Localization;
using Mix.Session;
using Mix.Session.Local;
using Mix.User;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class SettingsController : BaseController, AddChildPanel.IParentControlAddChildListener, ContactCustomerSupportPanel.IContactCustomerSupportPanelListener, ParentLogInLinkedAdultPanel.IParentLogInLinkedAdultPanelListener, ParentLogInNoLinkedAdultPanel.IParentLogInNoLinkedAdultPanelListener, ParentLoginPanel.IParentLoginPanelListener, ParentalControlSetupPanel.IParentalControlSetupPanelListener, ParentalControlVerifyAdultPanel.IParentalControlVerifyAdultListener, ParentalControlsPanel.IParentalControlsPanelListener, ResendEmailVerificationPanel.IResendEmailVerificationPanelListener, IAgeGateController
	{
		public GameObject soundToggle;

		public Button logoutButton;

		public Button needHelpButton;

		public Button privacyPolicyButton;

		public Button termsOfUseButton;

		public Button coppButton;

		public Toggle voiceOverToggle;

		public Toggle batteryLifeToggle;

		public Toggle fakeFriendsToggle;

		public Slider voiceOverSlider;

		public GameObject PushGameObject;

		private Toggle pushToggle;

		private GameObject AgeGateGameObject;

		private AgeGateController AgeGateScreen;

		private float delay;

		private IKeyValDatabaseApi databaseApi;

		private bool pushNotificationsSettingToggled;

		private string fromScene = "Prefabs/Screens/Profile/ProfileScreen";

		private UnityAction<bool> pushListener;

		private ResendEmailVerificationPanel resendEmailVerificationPanel;

		private ParentLoginPanel parentLoginPanel;

		private ParentalControlSetupPanel parentalControlSetupPanel;

		private AddChildPanel addChildPanel;

		private ParentalControlVerifyAdultPanel verifyAdultPanel;

		private ParentalControlsPanel parentalControlsPanel;

		private ContactCustomerSupportPanel contactCustomerSupportPanel;

		private ParentLogInLinkedAdultPanel parentLogInLinkedAdultPanel;

		private ParentLogInNoLinkedAdultPanel parentLogInNoLinkedAdultPanel;

		private ILocalUser createdParent;

		private bool fromAccountCreation;

		private bool toSetupPC;

		void IAgeGateController.OnSuccess(string destinationUrl, string destinationTitleToken)
		{
			ShowWebView(destinationUrl, destinationTitleToken);
		}

		private void Start()
		{
			databaseApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
			Toggle component = soundToggle.GetComponent<Toggle>();
			component.isOn = Singleton<SettingsManager>.Instance.GetSoundEnabledSetting();
			voiceOverToggle.isOn = Singleton<SettingsManager>.Instance.GetChatVoiceOverSetting();
			voiceOverSlider.value = Singleton<SettingsManager>.Instance.GetChatVoiceOverRateSetting();
			batteryLifeToggle.isOn = Singleton<SettingsManager>.Instance.GetBatteryLifeSetting();
			fakeFriendsToggle.SetIsOnWithoutNotify(Singleton<SettingsManager>.Instance.GetFakeFriendsEnabled());
			fakeFriendsToggle.onValueChanged.AddListener(OnFakeFriendsSettingChanged);
			needHelpButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			logoutButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			privacyPolicyButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			termsOfUseButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			coppButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			MixSession.OnConnectionChanged += OnConnectionChanged;
			if (MixSession.IsValidSession)
			{
				Analytics.LogSettingsPageView(MixSession.Session.LocalUser.AgeBandType);
			}
			pushToggle = PushGameObject.GetComponentInChildren<Toggle>();
			pushToggle.isOn = ShouldPushBeEnabled();
			pushListener = delegate
			{
				OnPushSettingToggled(pushToggle);
			};
			pushToggle.onValueChanged.AddListener(pushListener);
			Singleton<SettingsManager>.Instance.OnPushSettingUpdate += HandleOnPushSettingUpdate;
			if (!LoadingController.HideManagePC)
			{
				base.transform.Find("Overlay_Holder/Overlay_SettingsScreen/Settings/SettingsScroller/SettingsList/PCCategory").gameObject.SetActive(true);
				Transform transform = base.transform.Find("Overlay_Holder/Overlay_SettingsScreen/Settings/SettingsScroller/SettingsList/ManageParentalControls");
				transform.gameObject.SetActive(true);
				Button component2 = transform.GetComponent<Button>();
				component2.onClick.AddListener(delegate
				{
					OnParentalControlsButtonClicked();
				});
			}
		}

		private void HandleOnPushSettingUpdate(object sender, EventArgs args)
		{
			if (pushToggle != null)
			{
				pushToggle.onValueChanged.RemoveListener(pushListener);
				pushToggle.isOn = ShouldPushBeEnabled();
				pushToggle.onValueChanged.AddListener(pushListener);
			}
		}

		private void OnFakeFriendsSettingChanged(bool enabled)
		{
			Singleton<SettingsManager>.Instance.SetFakeFriendsEnabled(enabled);
			if (enabled)
			{
				LocalUserWrapper user = MixSession.User as LocalUserWrapper;
				if (user != null)
				{
					user.LoadFakeFriendData();
				}
				MonoSingleton<FakeFriendManager>.Instance.Init();
			}
		}

		public void OnApplicationPause(bool aState)
		{
			if (aState)
			{
				UpdatePushNotificationsSettingOnServer();
			}
			else
			{
				pushToggle.isOn = ShouldPushBeEnabled();
			}
		}

		public override void OnUIUnLoaded(NavigationRequest aNavigationRequest = null)
		{
			UpdatePushNotificationsSettingOnServer();
		}

		private void OnConnectionChanged(MixSession.ConnectionState newState, MixSession.ConnectionState oldState)
		{
			if (!this.IsNullOrDisposed())
			{
				needHelpButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
				logoutButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
				privacyPolicyButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
				termsOfUseButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
				coppButton.interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			}
		}

		public override void OnDataReceived(string aToken, object aData)
		{
			if (aToken.Equals("fromScene"))
			{
				fromScene = (string)aData;
			}
			else if (aToken.Equals("createAdultAccount"))
			{
				createdParent = (ILocalUser)aData;
				fromAccountCreation = true;
			}
			else if (aToken.Equals("setupPC"))
			{
				toSetupPC = true;
			}
		}

		public override void OnUITransitionEnd()
		{
			if (fromAccountCreation)
			{
				fromAccountCreation = false;
				resendEmailVerificationPanel = (ResendEmailVerificationPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_RESEND_EMAIL, false);
				resendEmailVerificationPanel.Init(createdParent, false, this);
			}
			else if (toSetupPC)
			{
				toSetupPC = false;
				OnParentalControlsButtonClicked();
			}
		}

		public override void OnAndroidBackButtonClicked()
		{
			if (!Singleton<PanelManager>.Instance.OnAndroidBackButton())
			{
				OnCloseButtonClicked();
			}
		}

		private void Update()
		{
			delay += Time.deltaTime;
		}

		private void OnDestroy()
		{
			MixSession.OnConnectionChanged -= OnConnectionChanged;
			if (fakeFriendsToggle != null)
			{
				fakeFriendsToggle.onValueChanged.RemoveListener(OnFakeFriendsSettingChanged);
			}
			if (Singleton<SettingsManager>.Instance != null)
			{
				Singleton<SettingsManager>.Instance.OnPushSettingUpdate -= HandleOnPushSettingUpdate;
			}
		}

		public void OnColorThemeClicked(ColorToggleItem aColorToggleItem)
		{
			if (databaseApi == null || aColorToggleItem == null)
				return;

			Toggle colorToggle = aColorToggleItem.GetComponent<Toggle>();
			if (colorToggle == null || !colorToggle.isOn)
				return;

			string text = databaseApi.LoadUserValue("default_primary_color");
			if (text == null || !text.Equals(aColorToggleItem.PrimaryColor))
			{
				Analytics.LogAppColorChangeAction(aColorToggleItem.PrimaryColor);
			}
			databaseApi.SaveUserValue("default_primary_color", aColorToggleItem.PrimaryColor);
			databaseApi.SaveUserValue("default_secondary_color", aColorToggleItem.SecondaryColor);
			TintImage[] array = (TintImage[])UnityEngine.Object.FindObjectsByType(typeof(TintImage), FindObjectsSortMode.InstanceID);
			TintImage[] array2 = array;
			foreach (TintImage tintImage in array2)
			{
				tintImage.OnColorThemeChanged();
			}
			TintText[] array3 = (TintText[])UnityEngine.Object.FindObjectsByType(typeof(TintText), FindObjectsSortMode.InstanceID);
			TintText[] array4 = array3;
			foreach (TintText tintText in array4)
			{
				tintText.OnColorThemeChanged();
			}
		}

		public void OnNeedHelpClicked()
		{
			if (ShowWebView(ExternalizedConstants.NeedHelpUrl, "customtokens.panels.about_help_title"))
			{
				Analytics.LogNeedHelpPageView();
			}
		}

		public void OnCAPrivacyRightsClicked()
		{
			ShowWebView(ExternalizedConstants.CaPrivacyUrl, "settingsscreen.caprivacybtn.caprivacytext");
		}

		public void OnCOPPClicked()
		{
			if (ShowWebView(ExternalizedConstants.CoppaUrl, "settingsscreen.childrenonlineprivacypolicy.copptext"))
			{
				Analytics.LogChildPrivacyPageView();
			}
		}

		public void OnPrivacyPolicyClicked()
		{
			if (ShowWebView(ExternalizedConstants.PrivacyPolicyUrl, "customtokens.webview.privacy_policy"))
			{
				Analytics.LogPrivacyPolicyPageView();
			}
		}

		public void OnTermsOfUseClicked()
		{
			Application.OpenUrl(ExternalizedConstants.TouUrl);
			Analytics.LogTermsOfUsePageView();
		}

		public void OnLogoutButtonClicked()
		{
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC, false);
			if (genericPanel != null)
			{
				Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
				{
					{ genericPanel.TitleText, "customtokens.login.logout_confirm" },
					{ genericPanel.MessageText, null },
					{ genericPanel.ButtonOneText, "customtokens.login.logout_yes" },
					{ genericPanel.ButtonTwoText, "customtokens.login.logout_no" }
				});
				Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action> { { genericPanel.ButtonOne, OnLogoutClicked } });
			}
		}

		public void OnParentalControlsButtonClicked()
		{
			bool permissions = false;
			ILocalUser me = MixSession.User;
			me.GetAdultVerificationRequirements(delegate(IGetAdultVerificationRequirementsResult getAdultVerificationRequirements)
			{
				if (getAdultVerificationRequirements.Success)
				{
					if (!getAdultVerificationRequirements.IsRequired)
					{
						if (me.AgeBandType == AgeBandType.Child)
						{
							if (permissions)
							{
								Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_CONTACT_CUSTOMER_SUPPORT, false);
							}
							else
							{
								Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_RESEND_EMAIL, false);
							}
						}
						else
						{
							me.GetLinkedChildren(delegate(IGetLinkedUsersResult getLinkedUsersResult)
							{
								if (getLinkedUsersResult.Success)
								{
									IEnumerable<ILinkedUser> linkedUsers = getLinkedUsersResult.LinkedUsers;
									if (linkedUsers != null)
									{
										IEnumerator<ILinkedUser> enumerator = linkedUsers.GetEnumerator();
										if (enumerator != null)
										{
											if (enumerator.MoveNext())
											{
												contactCustomerSupportPanel = (ContactCustomerSupportPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_CONTACT_CUSTOMER_SUPPORT, false);
												contactCustomerSupportPanel.Init(this);
											}
											else
											{
												NoLinkedChildrenPanel noLinkedChildrenPanel = (NoLinkedChildrenPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_NO_LINKED_CHILDREN, false);
												noLinkedChildrenPanel.Init();
											}
										}
									}
								}
							});
						}
					}
					else if (getAdultVerificationRequirements.IsAvailable)
					{
						if (me.AgeBandType == AgeBandType.Child)
						{
							me.GetLinkedGuardians(delegate(IGetLinkedUsersResult getLinkedUsersResult)
							{
								if (getLinkedUsersResult.Success)
								{
									IEnumerable<ILinkedUser> linkedUsers = getLinkedUsersResult.LinkedUsers;
									if (linkedUsers != null)
									{
										IEnumerator<ILinkedUser> enumerator = linkedUsers.GetEnumerator();
										if (enumerator != null)
										{
											if (enumerator.MoveNext())
											{
												parentLogInLinkedAdultPanel = (ParentLogInLinkedAdultPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_PARENT_LOGIN_LINKED_ADULT, false);
												parentLogInLinkedAdultPanel.Init(linkedUsers, this);
											}
											else
											{
												parentLogInNoLinkedAdultPanel = (ParentLogInNoLinkedAdultPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_PARENT_LOGIN_NO_LINKED_ADULT, false);
												parentLogInNoLinkedAdultPanel.Init(this);
											}
										}
									}
								}
							});
						}
						else if (me.RegistrationProfile.EmailVerified)
						{
							parentLoginPanel = (ParentLoginPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_PARENT_LOGIN);
							parentLoginPanel.Init(this);
						}
						else
						{
							resendEmailVerificationPanel = (ResendEmailVerificationPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_RESEND_EMAIL, false);
							resendEmailVerificationPanel.Init(me, true, this);
						}
					}
					else
					{
						Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_PC_NOT_SUPPORTED, false);
					}
				}
			});
		}

		public void ContactCustomerSupport()
		{
			contactCustomerSupportPanel.ClosePanel();
			WebOverlayPanel webOverlayPanel = (WebOverlayPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.WEB_VIEW);
			if (webOverlayPanel != null)
			{
				webOverlayPanel.Init("https://help.disney.com/en_US/Games/Contact?productCode=Disney-Mix", string.Empty, "Contact Customer Service");
			}
		}

		public void ParentLoggedIn(ILocalUser aParent, bool aFromNoLinkedLogin)
		{
			if (aFromNoLinkedLogin)
			{
				parentLogInNoLinkedAdultPanel.ClosePanel();
			}
			else
			{
				parentLogInLinkedAdultPanel.ClosePanel();
			}
			if (aParent.RegistrationProfile.EmailVerified)
			{
				if (aParent.RegistrationProfile.IsAdultVerified)
				{
					parentalControlsPanel = (ParentalControlsPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROLS);
					parentalControlsPanel.Init(aParent, this);
				}
				else
				{
					ShowLinkChildPanel(aParent);
				}
			}
			else
			{
				resendEmailVerificationPanel = (ResendEmailVerificationPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_RESEND_EMAIL);
				resendEmailVerificationPanel.Init(aParent, true, this);
			}
		}

		public void CreateAccount()
		{
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionNone());
			navigationRequest.AddData("createAdultAccount", true);
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void ParentRelogin(string aPassword)
		{
			string email = MixSession.User.RegistrationProfile.Email;
			ISession session = MixSession.Session;
			session.LogOut(delegate(ISessionLogOutResult sessionLogOutResult)
			{
				if (sessionLogOutResult.Success)
				{
					MonoSingleton<LoginManager>.Instance.Login(email, aPassword, delegate(ILoginResult loginResult)
					{
						if (loginResult.Success)
						{
							parentLoginPanel.ClosePanel();
							if (MixSession.User.RegistrationProfile.IsAdultVerified)
							{
								parentalControlsPanel = (ParentalControlsPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROLS);
								parentalControlsPanel.Init(MixSession.User, this);
							}
							else
							{
								ShowLinkChildPanel(MixSession.User);
							}
						}
					});
				}
			});
		}

		public void LoginSecondAccount(string aUsername, string aPassword, string aCachePath, Action<ILoginResult> callback)
		{
			LoginManager.MixCoroutineManager coroutineManager = new LoginManager.MixCoroutineManager();
			IKeychain keychain = new KeychainData();
			string localStorageDirPath = Application.PersistentDataPath + aCachePath;
			SessionStarter sessionStarter = new SessionStarter();
			sessionStarter.Login(ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.MixPlatformServicesUrl, ExternalizedConstants.GuestControllerClientId, ExternalizedConstants.MixPlatformServicesCellophane, MonoSingleton<AssetManager>.Instance.GetBuildNumber(), coroutineManager, keychain, Log.MixLogger, localStorageDirPath, aUsername, aPassword, Localizer.GetLocale(), callback);
		}

		public void AdultEmailVerified(ILocalUser aLocalUser)
		{
			resendEmailVerificationPanel.ClosePanel();
			if (aLocalUser.AgeBandType == AgeBandType.Child)
			{
				parentLoginPanel = (ParentLoginPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_PARENT_LOGIN);
				parentLoginPanel.Init(this);
			}
			else
			{
				ShowLinkChildPanel(aLocalUser);
			}
		}

		public void ForgotPassword()
		{
			parentLoginPanel.ClosePanel();
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionNone());
			navigationRequest.AddData("recover_password", true);
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void OnVerifyAdult(ILocalUser aParent)
		{
			parentalControlSetupPanel.ClosePanel();
			if (aParent.RegistrationProfile.IsAdultVerified)
			{
				parentalControlsPanel = (ParentalControlsPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROLS);
				parentalControlsPanel.Init(aParent, this);
			}
			else
			{
				verifyAdultPanel = (ParentalControlVerifyAdultPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_VERIFY_ADULT);
				verifyAdultPanel.Init(aParent, this);
			}
		}

		public void OnAdultVerified(ILocalUser aParent)
		{
			verifyAdultPanel.ClosePanel();
			parentalControlsPanel = (ParentalControlsPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROLS);
			parentalControlsPanel.Init(aParent, this);
		}

		public void OnAddChild(ILocalUser aParent, BasePanel fromPanel)
		{
			parentalControlsPanel.ClosePanel();
			ShowLinkChildPanel(aParent);
		}

		private void ShowLinkChildPanel(ILocalUser aParent, bool fromManualAdd = false)
		{
			IEnumerable<ILinkedUser> claimableUsers = null;
			IEnumerable<ILinkedUser> linkedUsers = null;
			aParent.GetClaimableChildren(delegate(IGetLinkedUsersResult getClaimableUsersResult)
			{
				if (getClaimableUsersResult.Success)
				{
					claimableUsers = getClaimableUsersResult.LinkedUsers;
					aParent.GetLinkedChildren(delegate(IGetLinkedUsersResult getLinkedUsersResult)
					{
						if (getLinkedUsersResult.Success)
						{
							linkedUsers = getLinkedUsersResult.LinkedUsers;
							bool flag = false;
							if (linkedUsers != null && linkedUsers.GetEnumerator().MoveNext())
							{
								flag = true;
							}
							if (claimableUsers != null && claimableUsers.GetEnumerator().MoveNext())
							{
								flag = true;
							}
							if (flag)
							{
								if (fromManualAdd)
								{
									addChildPanel.ClosePanel();
								}
								parentalControlSetupPanel = (ParentalControlSetupPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_SETUP);
								parentalControlSetupPanel.Init(aParent, claimableUsers, linkedUsers, this);
							}
							else if (!fromManualAdd)
							{
								addChildPanel = (AddChildPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_ADD_CHILD);
								addChildPanel.Init(aParent, this);
							}
						}
					});
				}
			});
		}

		public void OnAddChildManually(ILocalUser aParent)
		{
			parentalControlSetupPanel.ClosePanel();
			addChildPanel = (AddChildPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PARENTAL_CONTROL_ADD_CHILD);
			addChildPanel.Init(aParent, this);
		}

		public void OnChildAdded(ILocalUser aParent)
		{
			ShowLinkChildPanel(aParent, true);
		}

		public void OnPushSettingToggled(Toggle aToggle)
		{
			if (!MonoSingleton<PushNotifications>.Instance.IsDeviceNotificationEnabled())
			{
				PreMessagePanel preMessagePanel = (PreMessagePanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PUSH_SETTING_MESSAGE);
				preMessagePanel.Setup();
				pushToggle.onValueChanged.RemoveListener(pushListener);
				aToggle.isOn = false;
				pushToggle.onValueChanged.AddListener(pushListener);
			}
			else
			{
				pushNotificationsSettingToggled = true;
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_6_AvatarUISelect");
				Analytics.LogChatNotificationsToggle(aToggle.isOn);
			}
		}

		private bool ShouldPushBeEnabled()
		{
			if (!MonoSingleton<PushNotifications>.Instance.IsDeviceNotificationEnabled())
			{
				PushGameObject.transform.Find("StatusText").gameObject.SetActive(true);
				return false;
			}
			PushGameObject.transform.Find("StatusText").gameObject.SetActive(false);
			return Singleton<SettingsManager>.Instance.GetPushNotificationsSetting();
		}

		private void UpdatePushNotificationsSettingOnServer()
		{
			if (pushNotificationsSettingToggled)
			{
				if (MonoSingleton<ConnectionManager>.Instance.IsConnected)
				{
					MonoSingleton<PushNotifications>.Instance.ToggleVisiblePushNotifications(pushToggle.isOn);
				}
				else
				{
					MixSession.AddOfflineItem(new PushQueueItem(pushToggle.isOn.ToString()));
				}
			}
		}

		public void OnLicenseCreditsClicked()
		{
			if (Singleton<PanelManager>.Instance.ShowPanel(Panels.ABOUT, false) != null)
			{
				Analytics.LogLicenseCreditsPageView();
			}
		}

		public void OnSparkRulesClicked()
		{
			Singleton<PanelManager>.Instance.ShowPanel(Panels.MIX_RULES, false);
		}

		public void OnCloseButtonClicked()
		{
			MonoSingleton<NavigationManager>.Instance.AddRequest(new NavigationRequest(fromScene, new TransitionAnimations()));
		}

		public void OnVoiceOverSettingsChanged(Toggle aToggle)
		{
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_6_AvatarUISelect");
			Singleton<SettingsManager>.Instance.SetChatVoiceOverSetting(aToggle.isOn);
			voiceOverSlider.transform.parent.gameObject.SetActive(aToggle.isOn);
			Analytics.LogChatVoiceOverToggle(aToggle.isOn);
		}

		public void OnBatteryLifeChanged(Toggle aToggle)
		{
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_6_AvatarUISelect");
			Singleton<SettingsManager>.Instance.SetBatteryLifeSetting(aToggle.isOn);
			Analytics.LogBatteryLifeToggle(aToggle.isOn);
		}

		public void OnVoiceOverSliderChanged(Slider aSlider)
		{
			Singleton<SettingsManager>.Instance.SetChatVoiceOverRateSetting(aSlider.value);
		}

		public void OnSoundsSettingsChanged(GameObject inputObject)
		{
			Toggle component = inputObject.GetComponent<Toggle>();
			bool isOn = component.isOn;
			Singleton<SettingsManager>.Instance.SetSoundEnabledSetting(isOn);
			if (isOn && !EnvironmentManager.IsMusicPlaying)
			{
				Singleton<SoundManager>.Instance.EnableSound();
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_6_AvatarUISelect");
			}
			else
			{
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_6_AvatarUISelect");
				Singleton<SoundManager>.Instance.DisableSound();
			}
			Analytics.LogSoundSettingsToggle(isOn);
		}

		private bool ShowWebView(string aUrl, string aLocToken)
		{
			WebOverlayPanel webOverlayPanel = (WebOverlayPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.WEB_VIEW, false);
			if (webOverlayPanel != null)
			{
				webOverlayPanel.Init(aUrl, aLocToken, string.Empty);
				return true;
			}
			return false;
		}

		public void OnLogoutClicked()
		{
			MonoSingleton<LoginManager>.Instance.Logout();
			Analytics.LogNavigationAction("main_nav", "logout");
			NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionAnimations(false, "Intro", "ToLogin"));
			MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest);
		}
	}
}
