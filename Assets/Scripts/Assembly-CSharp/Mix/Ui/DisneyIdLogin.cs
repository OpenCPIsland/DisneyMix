using System;
using Avatar;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Disney.Native;
using Mix.DeviceDb;
using Mix.Localization;
using Mix.Native;
using Mix.User;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class DisneyIdLogin : MonoBehaviour, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == emailInput || input == passwordInput;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnLogin();
		}

		public Button LoginButton;

		public Button CancelButton;

		public Button ForgotUsernameButton;

		public Button ForgotPasswordButton;

		public Button BackButton;

		public GameObject ScrollViewGameObject;

		public GameObject EmailInputGameObject;

		public GameObject PasswordInputGameObject;

		public GameObject ErrorMessageGameObject;

		private NativeTextView emailInput;

		private NativeTextView passwordInput;

		private IDisneyIdLogin caller;

		private bool errorEmail;

		private bool errorPassword;

		private int birthYear;

		private int birthMonth;

		private int birthDay;

		private bool submitted;

		private ILocalUser loginUser;

		private string[] errorDescriptions;

		private SdkActions actionGenerator = new SdkActions();

		private void Start()
		{
			emailInput = EmailInputGameObject.GetComponent<NativeTextView>();
			passwordInput = PasswordInputGameObject.GetComponent<NativeTextView>();
			ScrollView component = ScrollViewGameObject.GetComponent<ScrollView>();
			component.OnPointerDownDelegates = (ScrollView.OnPointerDownDelegate)Delegate.Combine(component.OnPointerDownDelegates, new ScrollView.OnPointerDownDelegate(OnScrollGotPointerDown));
		}

		private void OnDestroy()
		{
			if (!MonoSingleton<NativeKeyboardManager>.Instance.IsNullOrDisposed() && !MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.IsNullOrDisposed())
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
			}
		}

		private void Update()
		{
			if (!submitted)
			{
				if (string.IsNullOrEmpty(emailInput.Value) || string.IsNullOrEmpty(passwordInput.Value) || string.Empty.Equals(emailInput.Value.Trim()) || string.Empty.Equals(passwordInput.Value.Trim()))
				{
					LoginButton.interactable = false;
				}
				else
				{
					LoginButton.interactable = true;
				}
			}
		}

		public void Show(int aBirthYear, int aBirthMonth, int aBirthDay, IDisneyIdLogin aCaller)
		{
			loginUser = null;
			birthYear = aBirthYear;
			birthMonth = aBirthMonth;
			birthDay = aBirthDay;
			caller = aCaller;
			base.gameObject.SetActive(true);
			setUiInteractable(true);
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
		}

		public void Hide()
		{
			HideKeyboard();
			base.gameObject.SetActive(false);
		}

		public void HideKeyboard()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
		}

		public void OnScrollGotPointerDown(PointerEventData eventData)
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
		}

		public void OnLogin()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			ErrorMessageGameObject.SetActive(false);
			setUiInteractable(false);
			submitted = true;
			if (ClientValidateValues())
			{
				Text component = ErrorMessageGameObject.GetComponent<Text>();
				component.text = string.Empty;
				MonoSingleton<LoginManager>.Instance.Login(emailInput.Value, passwordInput.Value, handleLoginResponse);
				return;
			}

			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			Text component2 = ErrorMessageGameObject.GetComponent<Text>();
			component2.text = Singleton<Localizer>.Instance.getString("customtokens.login.error_invalid_login");
			toggleErrorFields(true);
			ErrorMessageGameObject.SetActive(true);
			setUiInteractable(true);
			submitted = false;
			if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
			{
				AccessibilityManager.Instance.Speak(component2.text);
			}
		}

		public void OnUsernameRecovery()
		{
			caller.OnRecoveryClicked(true);
		}

		public void OnPasswordRecovery()
		{
			caller.OnRecoveryClicked(false);
		}

		public void OnBackClicked()
		{
			caller.OnBackClicked();
		}

		public void OnStartAnimationComplete()
		{
			emailInput.SelectInput();
		}

		private bool ClientValidateValues()
		{
			return !string.IsNullOrEmpty(emailInput.Value) && !string.IsNullOrEmpty(passwordInput.Value) && !string.Empty.Equals(emailInput.Value.Trim()) && !string.Empty.Equals(passwordInput.Value.Trim());
		}

		public void handleLoginResponse(ILoginResult authResp)
		{
			if (this.IsNullOrDisposed() || base.gameObject == null)
			{
				return;
			}
			if (authResp == null)
			{
				ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.login.error_invalid_login");
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
				ErrorMessageGameObject.SetActive(true);
				toggleErrorFields(true);
				setUiInteractable(true);
				submitted = false;
				if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(ErrorMessageGameObject.GetComponent<Text>().text);
				}
				return;
			}
			if (authResp.Success)
			{
				loginUser = authResp.Session.LocalUser;
				if (authResp is ILoginMissingInfoResult)
				{
					if (!authResp.Session.LocalUser.RegistrationProfile.DateOfBirth.HasValue)
					{
						authResp.Session.LocalUser.UpdateProfile(null, null, null, null, null, new DateTime(birthYear, birthMonth, birthDay), null, null, actionGenerator.CreateAction<IUpdateProfileResult>(OnUpdateProfile));
						return;
					}
					caller.OnInfoMissing(authResp.Session.LocalUser);
					Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				}
				else
				{
					if (AvatarApi.ValidateAvatar(authResp.Session.LocalUser.Avatar))
					{
						handleDisplayNameStatus();
					}
					else
					{
						caller.OnValidAccount("Prefabs/Screens/AvatarEditor/AvatarEditorScreen", false);
					}
					Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				}
				return;
			}
			if (authResp is ILoginFailedMultipleAccountsResult)
			{
				caller.HandleMaseAccount(emailInput.Value);
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
			}
			else
			{
				if (authResp is ILoginFailedProfileDisabledResult || authResp is ILoginFailedTemporaryBanResult)
				{
					ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.login.error_banned");
				}
				else if (authResp is ILoginFailedAccountLockedOutResult)
				{
					ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.login.error_locked_out");
				}
				else
				{
					ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.login.error_invalid_login");
				}
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
				if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(ErrorMessageGameObject.GetComponent<Text>().text);
				}
			}
			ErrorMessageGameObject.SetActive(true);
			toggleErrorFields(true);
			setUiInteractable(true);
			submitted = false;
		}

		private void OnUpdateProfile(IUpdateProfileResult aResult)
		{
			if (aResult.Success)
			{
				caller.OnInfoMissing(loginUser);
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				return;
			}
			caller.ShowErrorOverlay("customtokens.register.error_update_birthday", string.Empty);
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			setUiInteractable(true);
			submitted = false;
		}

		private void handleDisplayNameStatus()
		{
			string text = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.LoadUserValue("displayname.approved.seen");
			if (loginUser.RegistrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.Rejected || loginUser.RegistrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.None)
			{
				caller.OnValidAccount("Prefabs/Screens/Profile/ProfileScreen", false);
			}
			else if (loginUser.RegistrationProfile.DisplayNameProposedStatus == DisplayNameProposedStatus.Accepted && !string.IsNullOrEmpty(text) && text == "0")
			{
				caller.OnValidAccount("Prefabs/Screens/Profile/ProfileScreen", true);
			}
			else if (!MonoSingleton<PushNotifications>.Instance.HaveShownPushPrePopup)
			{
				caller.OnValidAccount("Prefabs/Screens/ChatMix/ChatMixScreen", true);
			}
			else
			{
				caller.OnValidAccount("Prefabs/Screens/ChatMix/ChatMixScreen", false);
			}
		}

		private void setUiInteractable(bool aInteractable)
		{
			LoginButton.interactable = aInteractable;
			CancelButton.interactable = aInteractable;
			ForgotUsernameButton.interactable = aInteractable;
			ForgotPasswordButton.interactable = aInteractable;
			BackButton.interactable = aInteractable;
		}

		private void toggleErrorFields(bool aEnable)
		{
			EmailInputGameObject.GetComponent<Outline>().enabled = aEnable;
			PasswordInputGameObject.GetComponent<Outline>().enabled = aEnable;
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			if (args.ReturnKeyType == NativeKeyboardReturnKey.Next)
			{
				passwordInput.SelectInput();
			}
			else
			{
				OnLogin();
			}
		}
	}
}
