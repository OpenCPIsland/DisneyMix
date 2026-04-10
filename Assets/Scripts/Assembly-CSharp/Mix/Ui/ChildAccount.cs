using System;
using System.Collections.Generic;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Disney.Native;
using Mix.Connectivity;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using Mix.User;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ChildAccount : MonoBehaviour
	{
		private const string defaultNextPageEvent = "UI/Registration/GeneralButton";

		public Toggle ShowPassword;

		public Toggle Tou;

		public Button CreateChildAccountButton;

		public Button DisneyIdLoginButton;

		public Button BackButton;

		public GameObject QuestionButton;

		public RectTransform ScrollContent;

		public GameObject ScrollViewGameObject;

		public GameObject TouGameObject;

		public GameObject PasswordErrorMessageGameObject;

		public GameObject FirstNameErrorMessageGameObject;

		public GameObject UsernameErrorMessageGameObject;

		public GameObject ParentEmailErrorMessageGameObject;

		public GameObject TouErrorMessageGameObject;

		public InputField firstNameInput;

		public InputField usernameInput;

		public InputField parentEmailInput;

		public InputField passwordInput;

		public Text CountryText;

		public GameObject DisclosureBlockGameObject;

		private int birthYear;

		private int birthMonth;

		private int birthDay;

		private IAgeBand ageBand;

		private string countryCode;

		private string countryName;

		private IChildAccount caller;

		private bool errorFirstName;

		private bool errorUsername;

		private bool errorParentEmail;

		private bool errorPassword;

		private bool errorTou;

		private bool submitted;

		private bool updateLinks;

		public Color32 errorColor = new Color32(238, 174, 0, byte.MaxValue);

		public Color32 defaultColor = new Color32(32, 181, 156, byte.MaxValue);

		private bool mEnterSoundPlayed;

		private Dictionary<string, GameObject> marketingDisclosures = new Dictionary<string, GameObject>();

		private Dictionary<string, GameObject> legalDisclosures = new Dictionary<string, GameObject>();

		private void Start()
		{
			firstNameInput.onEndEdit.AddListener(delegate { InputFocusChanged(firstNameInput, false); });
			usernameInput.onEndEdit.AddListener(delegate { InputFocusChanged(usernameInput, false); });
			parentEmailInput.onEndEdit.AddListener(delegate { InputFocusChanged(parentEmailInput, false); });
			passwordInput.onEndEdit.AddListener(delegate { InputFocusChanged(passwordInput, false); });
			ScrollView component = ScrollViewGameObject.GetComponent<ScrollView>();
			component.OnPointerDownDelegates = (ScrollView.OnPointerDownDelegate)Delegate.Combine(component.OnPointerDownDelegates, new ScrollView.OnPointerDownDelegate(OnScrollGotPointerDown));
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
			mEnterSoundPlayed = false;
			base.transform.Find("ScrollView/Content/AgreeCheck").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/LegalHolder").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/GLAC").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/ReadPrivacyPracticesText").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/TermsBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Under13PrivacyPracticesBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Under13PrivacyPolicyBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Under13ChildrenOnlinePrivacyPolicyBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/ToViewText").gameObject.SetActive(false);
		}

		private void InputFocusChanged(InputField aField, bool aFocus)
		{
			if (!aFocus)
			{
				ClientValidateChildAccountValues(aField);
				toggleErrorFields(aField);
			}
			else if (!mEnterSoundPlayed)
			{
				mEnterSoundPlayed = true;
				Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/NextPage/BeginTyping");
			}
		}

		private void OnDestroy()
		{
			firstNameInput.onEndEdit.RemoveAllListeners();
			usernameInput.onEndEdit.RemoveAllListeners();
			parentEmailInput.onEndEdit.RemoveAllListeners();
			passwordInput.onEndEdit.RemoveAllListeners();
			if (MonoSingleton<NativeKeyboardManager>.Instance != null)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
			}
		}

		private void Update()
		{
			if (!submitted)
			{
				if (!MonoSingleton<ConnectionManager>.Instance.IsConnected || string.IsNullOrEmpty(usernameInput.text) || string.IsNullOrEmpty(parentEmailInput.text) || string.IsNullOrEmpty(passwordInput.text) || !TouAccepted() || string.Empty.Equals(usernameInput.text.Trim()) || string.Empty.Equals(parentEmailInput.text.Trim()) || string.Empty.Equals(passwordInput.text.Trim()) || (ageBand.Permissions.FirstName is IRegistrationPermissionRequired && (string.IsNullOrEmpty(firstNameInput.text) || string.Empty.Equals(firstNameInput.text.Trim()))))
				{
					CreateChildAccountButton.interactable = false;
				}
				else
				{
					CreateChildAccountButton.interactable = true;
				}
			}
			QuestionButton.SetActive(string.IsNullOrEmpty(passwordInput.text));
			if (firstNameInput.isFocused || usernameInput.isFocused || parentEmailInput.isFocused || passwordInput.isFocused)
			{
				ScrollViewGameObject.GetComponent<ScrollRect>().enabled = false;
			}
			else
			{
				ScrollViewGameObject.GetComponent<ScrollRect>().enabled = true;
			}
			if (!updateLinks)
			{
				return;
			}
			updateLinks = false;
			foreach (GameObject value in marketingDisclosures.Values)
			{
				TextWithEvents component = value.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
				if (!component.linksDefined)
				{
					updateLinks = true;
				}
			}
			foreach (GameObject value2 in legalDisclosures.Values)
			{
				TextWithEvents component2 = value2.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
				if (!component2.linksDefined)
				{
					updateLinks = true;
				}
			}
			if (updateLinks)
			{
				return;
			}
			foreach (GameObject value3 in marketingDisclosures.Values)
			{
				TextWithEvents component3 = value3.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
				InitHyperlinks(component3);
			}
			foreach (GameObject value4 in legalDisclosures.Values)
			{
				TextWithEvents component4 = value4.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
				InitHyperlinks(component4);
			}
		}

		public void Show(int aBirthYear, int aBirthMonth, int aBirthDay, IAgeBand aAgeBand, string aCountryCode, string aCountryName, IChildAccount aCaller)
		{
			birthYear = aBirthYear;
			birthMonth = aBirthMonth;
			birthDay = aBirthDay;
			ageBand = aAgeBand;
			countryCode = aCountryCode;
			countryName = aCountryName;
			caller = aCaller;
			if (countryCode == null || countryName != null)
			{
			}
			updateLinks = false;
			SetRegistrationStrings();
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged += OnKeyboardResize;
			base.gameObject.SetActive(true);
			setUiInteractable(true);
			Analytics.LogCreateChildPageView();
		}

		public void UpdateRegistrationStrings(string aCountryCode, string aCountryName, IAgeBand aAgeBand)
		{
			countryCode = aCountryCode;
			countryName = aCountryName;
			ageBand = aAgeBand;
			SetRegistrationStrings();
		}

		public void SetRegistrationStrings()
		{
			Transform transform = base.transform.Find("ScrollView/Content");
			if (CountryText == null || DisclosureBlockGameObject == null || transform == null)
			{
				return;
			}
			if (marketingDisclosures == null)
			{
				marketingDisclosures = new Dictionary<string, GameObject>();
			}
			if (legalDisclosures == null)
			{
				legalDisclosures = new Dictionary<string, GameObject>();
			}
			foreach (Transform item in transform)
			{
				if (item.gameObject.name.Contains("DisclosureBlock(Clone)"))
				{
					UnityEngine.Object.Destroy(item.gameObject);
				}
			}
			CountryText.text = Singleton<Localizer>.Instance.getString("createaccountscreen.countrybtn.countrytext").Replace("#countryName#", countryName);
			int num = 1;
			int num2 = base.transform.Find("ScrollView/Content/GLAC").GetSiblingIndex();
			DisclosureBlockGameObject.SetActive(false);
			foreach (IMarketingItem item2 in ageBand.Marketing)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(DisclosureBlockGameObject);
				marketingDisclosures.Add(item2.Id, gameObject);
				gameObject.name = "DisclosureBlock(Clone)" + num;
				gameObject.SetActive(true);
				gameObject.transform.SetParent(transform, false);
				gameObject.transform.SetSiblingIndex(++num2);
				TextWithEvents component = gameObject.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
				component.text = item2.Text;
				gameObject.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().interactable = true;
				gameObject.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().isOn = item2.Checked;
				num++;
			}
			foreach (ILegalDocument legalDocument in ageBand.LegalDocuments)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(DisclosureBlockGameObject);
				legalDisclosures.Add(legalDocument.Id, gameObject2);
				gameObject2.name = "DisclosureBlock(Clone)" + num;
				gameObject2.SetActive(true);
				gameObject2.transform.SetParent(transform, false);
				gameObject2.transform.SetSiblingIndex(++num2);
				TextWithEvents component2 = gameObject2.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
				component2.text = legalDocument.Text;
				if (legalDocument.DisplayCheckbox)
				{
					gameObject2.transform.Find("AcceptTerms/CheckboxText/Checkbox").gameObject.SetActive(true);
					gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.SetActive(true);
					gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().isOn = false;
					gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.GetComponent<Toggle>().interactable = true;
				}
				else
				{
					gameObject2.transform.Find("AcceptTerms/CheckboxText/Checkbox").gameObject.SetActive(false);
					gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.SetActive(false);
					gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().isOn = false;
					gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.GetComponent<Toggle>().interactable = false;
				}
				num++;
			}
			updateLinks = true;
		}

		private void onLinkClicked(string[] linkInfo)
		{
			if (!MonoSingleton<ConnectionManager>.Instance.IsConnected)
			{
				GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
				Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
				{
					{ genericPanel.TitleText, null },
					{ genericPanel.MessageText, "customtokens.global.no_connection" },
					{ genericPanel.ButtonOneText, null },
					{ genericPanel.ButtonTwoText, "createaccountscreen.okbtn.oktext" }
				});
			}
			else if (linkInfo[0].ToLower().Contains("mailto:"))
			{
				Application.OpenUrl(linkInfo[0]);
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
				Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
			}
			else
			{
				Application.OpenUrl(linkInfo[0]);
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
				Analytics.LogViewPrivacyPolicy();
				Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
			}
		}

		public void Hide()
		{
			HideKeyboard();
			base.gameObject.SetActive(false);
		}

		public void OnScrollGotPointerDown(PointerEventData eventData)
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
		}

		public void HideKeyboard()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged -= OnKeyboardResize;
			ScrollViewGameObject.GetComponent<ScrollRect>().enabled = true;
		}

		public void OnCreateChild()
		{
			ClearErrors();
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			setUiInteractable(false);
			submitted = true;
			if (ClientValidateChildAccountValues())
			{
				ServerValidateChildAccountValues();
				return;
			}
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			toggleErrorFields();
			setUiInteractable(true);
			submitted = false;
		}

		public void OnBackClicked()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			caller.OnBackClicked();
		}

		public void GoToDisneyIdLogin()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			caller.GoToDisneyIdLogin();
		}

		public void OnPrivacyPolicyClicked()
		{
			Application.OpenUrl(ExternalizedConstants.PrivacyPolicyUrl);
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			Analytics.LogViewPrivacyPolicy();
			Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
		}

		public void OnCountryClicked()
		{
			caller.ShowCountrySelectOverlay();
		}

		public void OnCOPPClicked()
		{
			Application.OpenUrl(ExternalizedConstants.CoppaUrl);
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			Analytics.LogViewCOPP();
			Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
		}

		public void OnTermsOfUseClicked()
		{
			Application.OpenUrl(ExternalizedConstants.TouUrl);
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			Analytics.LogViewTermsOfUse();
			Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
		}

		public void OnSparkRulesClicked()
		{
			if (Singleton<PanelManager>.Instance.ShowPanel(Panels.MIX_RULES, false) != null)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
				Analytics.LogViewAppRules();
				Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/GeneralButton");
			}
		}

		public void TogglePassword()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			passwordInput.contentType = ShowPassword.isOn ? InputField.ContentType.Standard : InputField.ContentType.Password;
			passwordInput.ForceLabelUpdate();
		}

		public void OnPasswordRulesClicked()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			Analytics.LogViewPasswordRules();
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
			{
				{ genericPanel.TitleText, "createaccountscreen.panelbase.passwordrulestext" },
				{ genericPanel.MessageText, "createaccountscreen.panelbase.passwordrulesdetailtext" },
				{ genericPanel.ButtonOneText, null },
				{ genericPanel.ButtonTwoText, "createaccountscreen.okbtn.oktext" }
			});
		}

		private void OnKeyboardResize(object sender, NativeKeyboardHeightChangedEventArgs args)
		{
			if (args == null)
			{
				return;
			}
			if (ScrollViewGameObject != null)
			{
				ScrollRect component = ScrollViewGameObject.GetComponent<ScrollRect>();
				if (component != null)
				{
					if (args.Height <= 0)
					{
						component.enabled = true;
						return;
					}
					component.enabled = false;
				}
			}
			if (firstNameInput != null && firstNameInput.isFocused)
			{
				ScrollInputIntoView(firstNameInput, args.Height);
			}
			else if (usernameInput != null && usernameInput.isFocused)
			{
				ScrollInputIntoView(usernameInput, args.Height);
			}
			else if (parentEmailInput != null && parentEmailInput.isFocused)
			{
				ScrollInputIntoView(parentEmailInput, args.Height);
			}
			else if (passwordInput != null && passwordInput.isFocused)
			{
				ScrollInputIntoView(passwordInput, args.Height);
			}
		}

		private void ScrollInputIntoView(InputField inputField, int keyboardHeight)
		{
			if (ScrollContent == null || inputField == null)
				return;

			// Calculate the position of the input field relative to the scroll content
			Vector3[] inputCorners = new Vector3[4];
			inputField.GetComponent<RectTransform>().GetWorldCorners(inputCorners);

			Vector3[] contentCorners = new Vector3[4];
			ScrollContent.GetWorldCorners(contentCorners);

			float inputTop = inputCorners[1].y;
			float contentTop = contentCorners[1].y;
			float offset = contentTop - inputTop;

			// Adjust the scroll position to bring the input into view
			Vector2 anchoredPosition = ScrollContent.anchoredPosition;
			anchoredPosition.y -= offset;
			ScrollContent.anchoredPosition = anchoredPosition;
		}

		private bool ClientValidateChildAccountValues(InputField aField = null)
		{
			errorFirstName = (ageBand.Permissions.FirstName is IRegistrationPermissionRequired && DataChecker.IsNullEmptyOrJustWhiteSpace(firstNameInput.text)) || (ageBand.Permissions.FirstName is IRegistrationPermissionOptional && firstNameInput.text.Length != 0 && firstNameInput.text.Trim().Length == 0);
			errorUsername = !DataChecker.IsValidUsernameNotEmail(usernameInput.text);
			errorParentEmail = !DataChecker.IsValidEmail(parentEmailInput.text);
			errorPassword = DataChecker.IsNullEmptyOrJustWhiteSpace(passwordInput.text);
			if (errorUsername && (aField == null || aField == usernameInput))
			{
				UsernameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_username_invalid");
				Analytics.LogChildAccountCreationFailure("invalid_username");
				if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(UsernameErrorMessageGameObject.GetComponent<Text>().text);
				}
			}
			else
			{
				errorUsername = usernameInput.text.Length < 6;
				if (errorUsername && (aField == null || aField == usernameInput))
				{
					UsernameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_username_size");
					Analytics.LogChildAccountCreationFailure("invalid_username");
					if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
					{
						AccessibilityManager.Instance.Speak(UsernameErrorMessageGameObject.GetComponent<Text>().text);
					}
				}
			}
			ColorBlock colors = Tou.colors;
			Tou.colors = colors;
			if (errorPassword && (aField == null || aField == passwordInput))
			{
				PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_invalid");
				Analytics.LogChildAccountCreationFailure("invalid_password_format");
				if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
				}
			}
			else
			{
				errorPassword = passwordInput.text.Length < 6;
				if (errorPassword && (aField == null || aField == passwordInput))
				{
					Analytics.LogChildAccountCreationFailure("invalid_password_size");
					PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_size");
					if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
					{
						AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
					}
				}
			}
			if (errorParentEmail && (aField == null || aField == parentEmailInput))
			{
				ParentEmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_parent_email_invalid");
				Analytics.LogChildAccountCreationFailure("invalid_parent_email");
				if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(ParentEmailErrorMessageGameObject.GetComponent<Text>().text);
				}
			}
			if (errorFirstName && (aField == null || aField == firstNameInput))
			{
				FirstNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_firstname_invalid");
				Analytics.LogChildAccountCreationFailure("invalid_first_name");
				if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(FirstNameErrorMessageGameObject.GetComponent<Text>().text);
				}
			}
			return !errorFirstName && !errorUsername && !errorParentEmail && !errorPassword;
		}

		private void ClearErrors()
		{
			errorUsername = false;
			errorParentEmail = false;
			errorPassword = false;
			errorFirstName = false;
			toggleErrorFields();
		}

		private void toggleErrorFields(InputField aField = null)
		{
			if (aField == null || aField == usernameInput)
			{
				UsernameErrorMessageGameObject.SetActive(errorUsername);
				usernameInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorUsername) ? defaultColor : errorColor);
			}
			if (aField == null || aField == parentEmailInput)
			{
				ParentEmailErrorMessageGameObject.SetActive(errorParentEmail);
				parentEmailInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorParentEmail) ? defaultColor : errorColor);
			}
			if (aField == null || aField == passwordInput)
			{
				PasswordErrorMessageGameObject.SetActive(errorPassword && (aField == null || aField == passwordInput));
				passwordInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorPassword) ? defaultColor : errorColor);
			}
			if (aField == null || aField == firstNameInput)
			{
				FirstNameErrorMessageGameObject.SetActive(errorFirstName);
				firstNameInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorFirstName) ? defaultColor : errorColor);
			}
			if (aField == null)
			{
				scrollToTop();
			}
		}

		private void ServerValidateChildAccountValues()
		{
			NewAccountValidator newAccountValidator = new NewAccountValidator(MonoSingleton<LoginManager>.Instance.KeychainData, Log.MixLogger, MonoSingleton<LoginManager>.Instance.StorageDir, ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.GuestControllerClientId, MonoSingleton<LoginManager>.Instance.CoroutineManager);
			newAccountValidator.ValidateChildAccount(usernameInput.text, passwordInput.text, OnValidateResponse);
		}

		private void OnValidateResponse(IValidateNewAccountResult aResult)
		{
			if (this.IsNullOrDisposed() || base.gameObject == null)
			{
				return;
			}
			if (!aResult.Success)
			{
				if (aResult.Errors != null)
				{
					foreach (IValidateNewAccountError error in aResult.Errors)
					{
						if (error is IValidateNewAccountPassswordMissingCharactersError)
						{
							Analytics.LogChildAccountCreationFailure("invalid_password_format");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_format");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
							}
							continue;
						}
						if (error is IValidateNewAccountPasswordSizeError)
						{
							Analytics.LogChildAccountCreationFailure("invalid_password_size");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_size");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
							}
							continue;
						}
						if (error is IValidateNewAccountPasswordCommonError)
						{
							Analytics.LogChildAccountCreationFailure("password_too_common");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_too_common");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
							}
							continue;
						}
						if (error is IValidateNewAccountPasswordPhoneError)
						{
							Analytics.LogChildAccountCreationFailure("password_matches_profile");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_profile_info");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
							}
							continue;
						}
						if (error is IValidateNewAccountAccountFoundError || error is IValidateNewAccountAccountInUseError)
						{
							Analytics.LogUsernameTaken(AgeBandType.Child);
							UsernameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_username_taken");
							errorUsername = true;
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(UsernameErrorMessageGameObject.GetComponent<Text>().text);
							}
							continue;
						}
						if (error is IValidateNewAccountUsernameError)
						{
							UsernameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_username_invalid");
							errorUsername = true;
							Analytics.LogChildAccountCreationFailure("invalid_username");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(UsernameErrorMessageGameObject.GetComponent<Text>().text);
							}
							continue;
						}
						caller.ShowErrorOverlay("customtokens.register.error_create_account", string.Empty);
						break;
					}
				}
				else
				{
					caller.ShowErrorOverlay("customtokens.register.error_create_account", string.Empty);
				}
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
				toggleErrorFields();
				setUiInteractable(true);
				submitted = false;
			}
			else
			{
				Dictionary<IMarketingItem, bool> aMarketing = new Dictionary<IMarketingItem, bool>();
				MonoSingleton<LoginManager>.Instance.CreateChildAccount(birthYear, birthMonth, birthDay, firstNameInput.text, usernameInput.text, parentEmailInput.text, passwordInput.text, Localizer.GetLocale(), aMarketing, ageBand.LegalDocuments, OnCreateChildAccount);
			}
		}

		public void OnCreateChildAccount(IRegisterResult aResult)
		{
			if (this.IsNullOrDisposed() || base.gameObject == null)
			{
				return;
			}
			if (aResult.Success)
			{
				PoisonFlowVariable.SetBirthdate(0, 0, 0, ageBand);
				caller.OnAccountCreated();
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				Analytics.LogSuccessfulChildAccountCreation();
				return;
			}
			if (aResult.Errors != null)
			{
				bool flag = false;
				foreach (IInvalidProfileItemError error in aResult.Errors)
				{
					flag = true;
					if (error is IInvalidEmailError || error is IInvalidParentEmailError)
					{
						if (!errorParentEmail)
						{
							ParentEmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_parent_email_invalid");
							errorParentEmail = true;
							Analytics.LogChildAccountCreationFailure("invalid_parent_email");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(ParentEmailErrorMessageGameObject.GetComponent<Text>().text);
							}
						}
					}
					else if (error is IPasswordMatchesOtherProfileInfoError)
					{
						if (!errorPassword)
						{
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_profile_info");
							Analytics.LogChildAccountCreationFailure("password_matches_profile");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
							}
						}
					}
					else if (error is IInvalidFirstNameError)
					{
						if (!errorFirstName)
						{
							FirstNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_firstname_invalid");
							errorFirstName = true;
							Analytics.LogChildAccountCreationFailure("invalid_first_name");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(FirstNameErrorMessageGameObject.GetComponent<Text>().text);
							}
						}
					}
					else if (error is IInvalidUsernameError)
					{
						if (!errorUsername)
						{
							UsernameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_username_invalid");
							errorUsername = true;
							Analytics.LogChildAccountCreationFailure("invalid_username");
							if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
							{
								AccessibilityManager.Instance.Speak(UsernameErrorMessageGameObject.GetComponent<Text>().text);
							}
						}
					}
					else if (error is IInvalidPasswordError && !errorPassword)
					{
						errorPassword = true;
						PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_invalid");
						Analytics.LogChildAccountCreationFailure("invalid_password_format");
						if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
						{
							AccessibilityManager.Instance.Speak(PasswordErrorMessageGameObject.GetComponent<Text>().text);
						}
					}
				}
				if (!flag)
				{
					caller.ShowErrorOverlay("customtokens.register.error_create_account", string.Empty);
				}
			}
			else
			{
				caller.ShowErrorOverlay("customtokens.register.error_create_account", string.Empty);
			}
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			toggleErrorFields();
			setUiInteractable(true);
			submitted = false;
		}

		private void scrollToTop()
		{
			ScrollViewGameObject.GetComponent<ScrollRect>().content.anchoredPosition = new Vector2(0f, 0f);
		}

		private void setUiInteractable(bool aInteractable)
		{
			CreateChildAccountButton.interactable = aInteractable;
			DisneyIdLoginButton.interactable = aInteractable;
			BackButton.interactable = aInteractable;
			usernameInput.interactable = aInteractable;
			passwordInput.interactable = aInteractable;
			ShowPassword.interactable = aInteractable;
			firstNameInput.interactable = aInteractable;
			parentEmailInput.interactable = aInteractable;
		}

		private bool ShowWebView(string aUrl, string aLocToken, string aTitle = "")
		{
			WebOverlayPanel webOverlayPanel = (WebOverlayPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.WEB_VIEW, false);
			if (webOverlayPanel != null)
			{
				webOverlayPanel.Init(aUrl, aLocToken, aTitle);
				return true;
			}
			return false;
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			if (args.ReturnKeyType == NativeKeyboardReturnKey.Next)
			{
				if (usernameInput.isFocused)
				{
					passwordInput.Select();
				}
				else if (passwordInput.isFocused)
				{
					firstNameInput.Select();
				}
				else if (firstNameInput.isFocused)
				{
					parentEmailInput.Select();
				}
			}
		}

		private void InitHyperlinks(TextWithEvents disclosure)
		{
			if (disclosure.links.Count <= 0)
			{
				return;
			}
			disclosure.AddButtons();
			foreach (string key in disclosure.links.Keys)
			{
				int num = 1;
				Transform transform;
				while ((transform = disclosure.transform.Find(key + num)) != null)
				{
					string[] linkInfo = disclosure.links[key];
					Button component = transform.GetComponent<Button>();
					component.onClick.AddListener(delegate
					{
						onLinkClicked(linkInfo);
					});
					num++;
				}
			}
		}

		private bool TouAccepted()
		{
			foreach (GameObject value in legalDisclosures.Values)
			{
				if (value.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.activeSelf && !value.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().isOn)
				{
					return false;
				}
			}
			return true;
		}
	}
}
