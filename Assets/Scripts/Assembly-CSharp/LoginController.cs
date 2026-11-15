using System;
using System.Collections.Generic;
using DG.Tweening;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Disney.Native;
using Mix;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.Entitlements;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using Mix.Tracking;
using Mix.Ui;
using Mix.User;
using Mix.Video;
using UnityEngine;
using UnityEngine.UI;

public class LoginController : BaseController, IKeyboardSubmitHandler
{
	public bool HandlesKeyboardSubmit(Selectable input)
	{
		return input == usernameField || input == passwordField;
	}

	public void SubmitFromKeyboard(Selectable input)
	{
		onLogin();
	}

	private const string registrationMusicLoop = "UI/Registration/MusicLoop";

	private const string startRegistrationSoundEvent = "UI/Registration/StartRegistration";

	private const string loginSoundEvent = "UI/Registration/StartRegistration";

	private const string generalButtonEvent = "UI/Registration/GeneralButton";

	public const string INTRO_VIDEO_PLAYED = "INTRO_VIDEO_PLAYED";

	public GameObject UsernameGameObject;

	public GameObject PasswordGameObject;

	public CanvasGroup ForgotCredentialsPanel;

	public Button LoginButton;

	public Button ShowLoginButton;

	public Button NewToSparkButton;

	public Button ReplayVideo;

	public Button ShowMoreDisneyButton;

	public GameObject QuestionMarkButton;

	public Animator FGAnimator;

	private InputField usernameField;

	private InputField passwordField;

	private bool loginSubmitted;

	private string incomingToken;

	public GameObject errorMessageGameObject;

	private void Start()
	{
		Singleton<TechAnalytics>.Instance.TrackTimeToLogin();
		usernameField = UsernameGameObject.GetComponent<InputField>();
		passwordField = PasswordGameObject.GetComponent<InputField>();
		MonoSingleton<ConnectionManager>.Instance.ConnectedEvent += OnConnected;
		MonoSingleton<ConnectionManager>.Instance.DisconnectedEvent += OnDisconnected;
		MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
		DOVirtual.DelayedCall(0.1f, StartLoginMusic);
	}

	private void OnDestroy()
	{
		if (MonoSingleton<ConnectionManager>.Instance != null)
		{
			MonoSingleton<ConnectionManager>.Instance.ConnectedEvent -= OnConnected;
			MonoSingleton<ConnectionManager>.Instance.DisconnectedEvent -= OnDisconnected;
		}
		if (MonoSingleton<NativeKeyboardManager>.Instance != null && MonoSingleton<NativeKeyboardManager>.Instance.Keyboard != null)
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
		}
	}

	private void Update()
	{
		QuestionMarkButton.SetActive(string.IsNullOrEmpty(usernameField.text));
		if (!loginSubmitted)
		{
			if (string.IsNullOrEmpty(usernameField.text) || string.IsNullOrEmpty(passwordField.text) || string.Empty.Equals(usernameField.text.Trim()) || string.Empty.Equals(passwordField.text.Trim()))
			{
				LoginButton.interactable = false;
			}
			else
			{
				LoginButton.interactable = true;
			}
		}
	}

	public override void OnDataReceived(string aToken, object aData)
	{
		incomingToken = aToken;
	}

	public override void OnUITransitionEnd()
	{
		if (incomingToken != null)
		{
			if (incomingToken == "banned")
			{
				GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
				genericPanel.ShowSimpleError("customtokens.global.account_is_banned_title", "customtokens.global.account_is_banned_text");
			}
			else if (incomingToken == "authInfo" || incomingToken == "fatalError")
			{
				GameObject gameObject = GameObject.Find("LoginBtn");
				gameObject.GetComponent<Button>().onClick.Invoke();
				GenericPanel genericPanel2 = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
				genericPanel2.ShowSimpleError("customtokens.global.login_again_title", "customtokens.global.login_again_text");
			}
			else if (incomingToken == "parentalConsent")
			{
				GameObject gameObject2 = GameObject.Find("LoginBtn");
				gameObject2.GetComponent<Button>().onClick.Invoke();
				ParentalConsent.ShowAuthLostDialog();
			}
			else if (incomingToken == "parentalConsentGranted")
			{
				GameObject gameObject3 = GameObject.Find("LoginBtn");
				gameObject3.GetComponent<Button>().onClick.Invoke();
			}
			else if (incomingToken == "loginPrompt")
			{
				GameObject gameObject4 = GameObject.Find("LoginBtn");
				gameObject4.GetComponent<Button>().onClick.Invoke();
			}
			else if (incomingToken == "forceUpdate")
			{
				ForceUpdatePanel forceUpdatePanel = Singleton<PanelManager>.Instance.ShowPanel(Panels.FORCE_UPDATE) as ForceUpdatePanel;
				Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action> { 
				{
					forceUpdatePanel.ButtonOne,
					()=>ForceUpdate.GoToAppStore()
				} });
				forceUpdatePanel.ShowSimpleError("customtokens.forceupdate.number_one");
			}
		}
		BIStartScreen(false);
	}

	public override void OnAndroidBackButtonClicked()
	{
		if (!Singleton<PanelManager>.Instance.OnAndroidBackButton())
		{
			if (LoginButton.IsActive())
			{
				if (FGAnimator != null && FGAnimator.isActiveAndEnabled && FGAnimator.runtimeAnimatorController != null)
				{
					FGAnimator.Play("AccountLogin_BacktoSplash", 0, 0f); // explicitly targeting layer 0 (base layer)
				}
				errorMessageGameObject.SetActive(false);
				return;
			}
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
			{
				{ genericPanel.TitleText, "customtokens.panels.leaving_app" },
				{ genericPanel.MessageText, null },
				{ genericPanel.ButtonOneText, "customtokens.login.logout_yes" },
				{ genericPanel.ButtonTwoText, "customtokens.login.logout_no" }
			});
			Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action> { 
			{
				genericPanel.ButtonOne,
				Mix.Application.Quit
			} });
		}
	}

	public void PlayAnimationSparkSound()
	{
	}

	public void onLogin()
	{
		MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
		loginSubmitted = true;
		errorMessageGameObject.SetActive(false);
		setUiInteractable(false);
		if (ValidateInput())
		{
			errorMessageGameObject.SetActive(false);
			ReplayVideo.interactable = false;
			ShowMoreDisneyButton.interactable = false;
			MonoSingleton<LoginManager>.Instance.Login(usernameField.text, passwordField.text, handleLoginResponse);
			Singleton<SoundManager>.Instance.StopSoundEvent("MainApp/UI/SFX_2_SendDisney");
			return;
		}

		Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
		Text component = errorMessageGameObject.GetComponent<Text>();
		component.text = Singleton<Localizer>.Instance.getString("customtokens.login.error_invalid_login");
		errorMessageGameObject.SetActive(true);
		setUiInteractable(true);
		loginSubmitted = false;
		if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
		{
			AccessibilityManager.Instance.Speak(component.text);
		}
	}

	public void onForgotPassword()
	{
		NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionAnimations(false, "NoProgressBar", "ToCreateAccount"));
		navigationRequest.AddData("recover_true_if_username_false_if_password", false);
		MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		setUiInteractable(false);
		Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
	}

	public void onForgotUsername()
	{
		NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionAnimations(false, "NoProgressBar", "ToCreateAccount"));
		navigationRequest.AddData("recover_true_if_username_false_if_password", true);
		MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		setUiInteractable(false);
		Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
	}

	public void onCreate()
	{
		setUiInteractable(false);
		if (!PlayerPrefs.HasKey("INTRO_VIDEO_PLAYED"))
		{
			PlayerPrefs.SetString("INTRO_VIDEO_PLAYED", "INTRO_VIDEO_PLAYED");
			FullScreenVideo fullScreenVideo = new FullScreenVideo("IntroVideo_10s_169.mp4", "IntroVideo_10s_43.mp4", "intro.mp4", 12f);
			fullScreenVideo.OnVideoComplete += DoOnCreate;
			Analytics.LogFirstPlayVideo();
			fullScreenVideo.PlayVideo(this);
		}
		else
		{
			DoOnCreate(null);
		}
	}

	public void ReplayIntroVideoComplete(FullScreenVideo aFullScreenVideo)
	{
		setUiInteractable(true);
		if (aFullScreenVideo != null)
		{
			aFullScreenVideo.OnVideoComplete -= ReplayIntroVideoComplete;
		}
		ShowMoreDisneyButton.interactable = true;
	}

	public void DoOnCreate(FullScreenVideo aFullScreenVideo)
	{
		if (aFullScreenVideo != null)
		{
			aFullScreenVideo.OnVideoComplete -= DoOnCreate;
		}
		Singleton<EntitlementsManager>.Instance.LoadMyEntitlements();
		NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionAnimations(false, "Intro", "ToCreateAccount"));
		MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest);
		Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/StartRegistration");
	}

	public void BIStartScreen(bool aFromLogin = true)
	{
		if (usernameField.isFocused || passwordField.isFocused)
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			return;
		}
		if (aFromLogin)
		{
			Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/PreviousPage");
			usernameField.text = string.Empty;
			passwordField.text = string.Empty;
			if (loginSubmitted)
			{
				return;
			}
			if (FGAnimator != null && FGAnimator.isActiveAndEnabled && FGAnimator.runtimeAnimatorController != null)
			{
				FGAnimator.Play("AccountLogin_BacktoSplash", 0, 0f); // explicitly targeting layer 0 (base layer)
			}
		}
		errorMessageGameObject.SetActive(false);
		if (incomingToken != "loginPrompt")
		{
			Analytics.LogStartPageView();
		}
		else
		{
			incomingToken = string.Empty;
		}
	}

	public void ReplayIntroVideo()
	{
		setUiInteractable(false);
		ShowMoreDisneyButton.interactable = false;
		FullScreenVideo fullScreenVideo = new FullScreenVideo("IntroVideo_10s_169.mp4", "IntroVideo_10s_43.mp4", "intro.mp4", 12f);
		Analytics.LogReplayPlayVideo();
		fullScreenVideo.OnVideoComplete += ReplayIntroVideoComplete;
		fullScreenVideo.PlayVideo(this);
	}

	public void BILoginScreen()
	{
		Analytics.LogLoginPageView();
		usernameField.text = string.Empty;
		passwordField.text = string.Empty;
		Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/StartRegistration");
	}

	public void ShowMoreDisney()
	{
		ReplayVideo.interactable = false;
		ReferralStoreManager.StoreHideEvent += ReferralStoreHidden;
		Singleton<ReferralStore>.Instance.ShowReferrals();
		Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
		StopLoginMusic();
	}

	private void ReferralStoreHidden(string aData)
	{
		ReplayVideo.interactable = true;
		ReferralStoreManager.StoreHideEvent -= ReferralStoreHidden;
		StartLoginMusic();
	}

	public void handleLoginResponse(ILoginResult loginResult)
	{
		ReplayVideo.interactable = true;
		ShowMoreDisneyButton.interactable = true;
		if (loginResult == null)
		{
			Text component = errorMessageGameObject.GetComponent<Text>();
			component.text = Singleton<Localizer>.Instance.getString("customtokens.global.login_again_text");
			errorMessageGameObject.SetActive(true);
			setUiInteractable(true);
			loginSubmitted = false;
			return;
		}
		if (loginResult.Success)
		{
			if (loginResult is ILoginMissingInfoResult || hasMissingLegalDocuments(loginResult))
			{
				NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionAnimations(false, "MissingInfo", "ToCreateAccount"));
				navigationRequest.AddData("missingInfo", loginResult);
				MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
			}
			else
			{
				handleDisplayNameStatus();
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
			}
			StopLoginMusic();
			return;
		}
		Text component2 = errorMessageGameObject.GetComponent<Text>();
		if (loginResult is ILoginFailedMultipleAccountsResult)
		{
			NavigationRequest navigationRequest2 = new NavigationRequest("Prefabs/Screens/CreateAccount/CreateAccountScreen", new TransitionAnimations(false, "Intro", "ToCreateAccount"));
			navigationRequest2.AddData("MASE_username", usernameField.text);
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest2);
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
		}
		else if (loginResult is ILoginFailedProfileDisabledResult || loginResult is ILoginFailedTemporaryBanResult)
		{
			component2.text = Singleton<Localizer>.Instance.getString("customtokens.login.error_banned");
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
		}
		else if (loginResult is ILoginFailedAccountLockedOutResult)
		{
			component2.text = Singleton<Localizer>.Instance.getString("customtokens.login.error_locked_out");
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
		}
		else if (loginResult is ILoginFailedAuthenticationResult)
		{
			if (!MonoSingleton<ConnectionManager>.Instance.IsConnected)
			{
				component2.text = Singleton<Localizer>.Instance.getString("customtokens.login.error_no_internet");
			}
			else
			{
				component2.text = Singleton<Localizer>.Instance.getString("customtokens.login.error_invalid_login");
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			}
		}
		else
		{
			component2.text = Singleton<Localizer>.Instance.getString("customtokens.global.login_again_text");
		}
		errorMessageGameObject.SetActive(true);
		setUiInteractable(true);
		loginSubmitted = false;
		if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
		{
			AccessibilityManager.Instance.Speak(component2.text);
		}
	}

	private bool hasMissingLegalDocuments(ILoginResult loginResult)
	{
		if (loginResult is ILoginRequiresLegalMarketingUpdateResult && ((ILoginRequiresLegalMarketingUpdateResult)loginResult).LegalDocuments.GetEnumerator().MoveNext())
		{
			return true;
		}
		return false;
	}

	private void handleDisplayNameStatus()
	{
		if (!MixSession.IsValidSession)
		{
			return;
		}
		DisplayNameProposedStatus displayNameProposedStatus = MixSession.Session.LocalUser.RegistrationProfile.DisplayNameProposedStatus;
		if (displayNameProposedStatus == DisplayNameProposedStatus.None || displayNameProposedStatus == DisplayNameProposedStatus.Rejected || displayNameProposedStatus == DisplayNameProposedStatus.Pending)
		{
			Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.SaveUserValueFromInt("displayname.approved.seen", 0);
		}
		NavigationRequest navigationRequest;
		switch (displayNameProposedStatus)
		{
		case DisplayNameProposedStatus.Rejected:
			navigationRequest = new NavigationRequest("Prefabs/Screens/Profile/ProfileScreen", new TransitionAnimations(false, "FromLogin"));
			navigationRequest.AddData("mode", ProfileController.PROFILE_MODES.INFO_UPDATE);
			break;
		case DisplayNameProposedStatus.None:
			navigationRequest = new NavigationRequest("Prefabs/Screens/AvatarEditor/AvatarEditorScreen", new TransitionAnimations(false, "ToDisplayNameFromHardLogin"));
			navigationRequest.AddData("mode", AvatarEditorController.EDITOR_MODES.CREATOR);
			navigationRequest.AddData("missingDisplayName", true);
			break;
		default:
		{
			string text = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.LoadUserValue("displayname.approved.seen");
			if (displayNameProposedStatus == DisplayNameProposedStatus.Accepted && !string.IsNullOrEmpty(text) && text == "0")
			{
				navigationRequest = new NavigationRequest("Prefabs/Screens/Profile/ProfileScreen", new TransitionAnimations(false, "FromLogin"));
				break;
			}
			navigationRequest = new NavigationRequest("Prefabs/Screens/Conversations/ConversationsScreen", new TransitionAnimations(false, "FromLogin"));
			if (!MonoSingleton<PushNotifications>.Instance.HaveShownPushPrePopup)
			{
				navigationRequest.AddData("regDone", true);
			}
			break;
		}
		}
		MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
	}

	private bool ValidateInput()
	{
		return !string.IsNullOrEmpty(usernameField.text) && !string.IsNullOrEmpty(passwordField.text) && !string.Empty.Equals(usernameField.text.Trim()) && !string.Empty.Equals(passwordField.text.Trim());
	}

	private void setUiInteractable(bool aInteractable)
	{
		ForgotCredentialsPanel.interactable = aInteractable;
		LoginButton.interactable = aInteractable;
		usernameField.interactable = aInteractable;
		passwordField.interactable = aInteractable;
		NewToSparkButton.interactable = aInteractable;
		ReplayVideo.interactable = aInteractable;
	}

	private void OnConnected()
	{
		NewToSparkButton.interactable = true;
		ShowLoginButton.interactable = true;
	}

	private void OnDisconnected()
	{
		NewToSparkButton.interactable = false;
		ShowLoginButton.interactable = false;
	}

	private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
	{
		if (args.ReturnKeyType == NativeKeyboardReturnKey.Next)
		{
			passwordField.Select();
		}
		else
		{
			onLogin();
		}
	}

	public static void StartLoginMusic()
	{
		Singleton<SoundManager>.Instance.PlayMusic("UI/Registration/MusicLoop");
	}

	public static void StopLoginMusic()
	{
		Singleton<SoundManager>.Instance.StopSoundEvent("UI/Registration/MusicLoop");
	}
}
