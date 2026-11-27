using System;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Disney.Native;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using Mix.User;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ForgotInfo : MonoBehaviour, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == EmailUsernameInputField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnRetrieve();
		}

		public Text HeaderText;

		public Text EmailUsernameText;

		public Text EmailUsernameInputText;

		public Text RetrieveButtonText;

		public Button RetrieveButton;

		public Button BackButton;

		public GameObject ScrollViewGameObject;

		public GameObject EmailUsernameInputGameObject;

		public GameObject ErrorMessageGameObject;

		public InputField EmailUsernameInputField;

		private IForgotInfo caller;

		private bool trueIfUsernameFalseIfPassword;

		private bool errorEmailUsername;

		private bool submitted;

		private string[] errorDescriptions;

		private string inputText = string.Empty;

		public Color32 errorColor = new Color32(238, 174, 0, byte.MaxValue);

		public Color32 defaultColor = new Color32(32, 181, 156, byte.MaxValue);

		private void Start()
		{
			ScrollView component = ScrollViewGameObject.GetComponent<ScrollView>();
			component.OnPointerDownDelegates = (ScrollView.OnPointerDownDelegate)Delegate.Combine(component.OnPointerDownDelegates, new ScrollView.OnPointerDownDelegate(OnScrollGotPointerDown));
		}

		private void OnDestroy()
		{
			if (MonoSingleton<NativeKeyboardManager>.Instance != null)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
			}
		}

		private void Update()
		{
			if (!submitted)
			{
				RetrieveButton.interactable = !DataChecker.IsNullEmptyOrJustWhiteSpace(EmailUsernameInputField.text);
			}
		}

		public void Show(bool aTrueIfUsernameFalseIfPassword, IForgotInfo aCaller)
		{
			caller = aCaller;
			trueIfUsernameFalseIfPassword = aTrueIfUsernameFalseIfPassword;
			if (trueIfUsernameFalseIfPassword)
			{
				HeaderText.text = Singleton<Localizer>.Instance.getString("customtokens.register.header_recover_username");
				inputText = Singleton<Localizer>.Instance.getString("customtokens.register.text_recover_email");
				RetrieveButtonText.text = Singleton<Localizer>.Instance.getString("customtokens.register.button_recover_username");
				Analytics.LogForgotUsernamePageView();
			}
			else
			{
				HeaderText.text = Singleton<Localizer>.Instance.getString("customtokens.register.header_recover_pasword");
				inputText = Singleton<Localizer>.Instance.getString("customtokens.register.text_recover_email_username");
				RetrieveButtonText.text = Singleton<Localizer>.Instance.getString("customtokens.register.button_recover_pasword");
				Analytics.LogForgotPasswordPageView();
			}
			EmailUsernameText.text = inputText;
			EmailUsernameInputField.text = string.Empty;
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
			base.gameObject.SetActive(true);
			setUiInteractable(true);
		}

		public void ClearInput()
		{
			setUiInteractable(true);
		}

		public void Hide()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
			base.gameObject.SetActive(false);
		}

		public void OnBack()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			caller.OnBack();
		}

		public void OnScrollGotPointerDown(PointerEventData eventData)
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
		}

		public void OnRetrieve()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			ErrorMessageGameObject.SetActive(false);
			setUiInteractable(false);
			submitted = true;
			if (ClientValidateAdultAccountValues())
			{
				ServerValidateAdultAccountValues();
				return;
			}

			Text component = ErrorMessageGameObject.GetComponent<Text>();
			if (trueIfUsernameFalseIfPassword)
			{
				component.text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_invalid");
			}
			else
			{
				component.text = Singleton<Localizer>.Instance.getString("customtokens.register.error_username_email_invalid");
			}
			if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
			{
				AccessibilityManager.Instance.Speak(component.text);
			}
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			ErrorMessageGameObject.SetActive(true);
			setUiInteractable(true);
			submitted = false;
		}

		private bool ClientValidateAdultAccountValues()
		{
			if (trueIfUsernameFalseIfPassword)
			{
				errorEmailUsername = !DataChecker.IsValidEmail(EmailUsernameInputField.text);
			}
			else
			{
				errorEmailUsername = DataChecker.IsNullEmptyOrJustWhiteSpace(EmailUsernameInputField.text);
			}
			toggleErrorFields();
			return !errorEmailUsername;
		}

		private void ServerValidateAdultAccountValues()
		{
			if (trueIfUsernameFalseIfPassword)
			{
				UsernameRecoverySender usernameRecoverySender = new UsernameRecoverySender(MonoSingleton<LoginManager>.Instance.KeychainData, Log.MixLogger, MonoSingleton<LoginManager>.Instance.StorageDir, ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.GuestControllerClientId, MonoSingleton<LoginManager>.Instance.CoroutineManager);
				usernameRecoverySender.Send(EmailUsernameInputField.text, OnUsernameResponse);
			}
			else
			{
				PasswordRecoverySender passwordRecoverySender = new PasswordRecoverySender(MonoSingleton<LoginManager>.Instance.KeychainData, Log.MixLogger, MonoSingleton<LoginManager>.Instance.StorageDir, ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.GuestControllerClientId, MonoSingleton<LoginManager>.Instance.CoroutineManager);
				passwordRecoverySender.Send(EmailUsernameInputField.text, OnPasswordResponse);
			}
		}

		private void OnUsernameResponse(ISendUsernameRecoveryResult aResult)
		{
			OnRecoverResult(aResult.Success);
		}

		private void OnPasswordResponse(ISendPasswordRecoveryResult aResult)
		{
			OnRecoverResult(aResult.Success);
		}

		private void OnRecoverResult(bool aSuccess)
		{
			if (this.IsNullOrDisposed() || base.gameObject == null)
			{
				return;
			}
			if (aSuccess)
			{
				if (trueIfUsernameFalseIfPassword)
				{
					caller.OnEmailSent(DisneyIdEmailType.RECOVER_USERNAME);
					Analytics.LogSendReminderUserName();
				}
				else
				{
					caller.OnEmailSent(DisneyIdEmailType.RECOVER_PASSWORD);
					Analytics.LogResetPassword();
				}
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				return;
			}
			ErrorMessageGameObject.SetActive(true);
			Text component = ErrorMessageGameObject.GetComponent<Text>();
			component.text = Singleton<Localizer>.Instance.getString("customtokens.register.forgot_send_error");
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			setUiInteractable(true);
			submitted = false;
			if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
			{
				AccessibilityManager.Instance.Speak(component.text);
			}
		}

		private void toggleErrorFields()
		{
			EmailUsernameInputGameObject.transform.parent.GetComponent<Image>().color = ((!errorEmailUsername) ? defaultColor : errorColor);
		}

		private void setUiInteractable(bool aInteractable)
		{
			EmailUsernameInputField.interactable = aInteractable;
			RetrieveButton.interactable = aInteractable;
			BackButton.interactable = aInteractable;
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			OnRetrieve();
		}
	}
}
