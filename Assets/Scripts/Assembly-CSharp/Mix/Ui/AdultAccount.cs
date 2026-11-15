using System;
using System.Collections.Generic;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Disney.Native;
using Mix.Connectivity;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using Mix.Threading;
using Mix.User;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class AdultAccount : MonoBehaviour, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == firstNameInput || input == lastNameInput || input == emailInput || input == passwordInput;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnCreateAdult();
		}

		private const string defaultNextPageEvent = "UI/Registration/GeneralButton";

		public Toggle ShowPassword;

		public Toggle Tou;

		public Toggle Marketing;

		public Button CreateAdultAccountButton;

		public Button DisneyIdLoginButton;

		public Button BackButton;

		public GameObject QuestionButton;

		public RectTransform ScrollContent;

		public GameObject ScrollViewGameObject;

		public GameObject TouGameObject;

		public GameObject PasswordErrorMessageGameObject;

		public GameObject FirstNameErrorMessageGameObject;

		public GameObject LastNameErrorMessageGameObject;

		public GameObject EmailErrorMessageGameObject;

		public GameObject TouErrorMessageGameObject;

		public InputField firstNameInput;

		public InputField lastNameInput;

		public InputField emailInput;

		public InputField passwordInput;

		public Text CountryText;

		public GameObject DisclosureBlockGameObject;

		private int birthYear;

		private int birthMonth;

		private int birthDay;

		private IAgeBand ageBand;

		private string countryCode;

		private string countryName;

		private IAdultAccount caller;

		private bool errorFirstName;

		private bool errorLastName;

		private bool errorEmail;

		private bool errorPassword;

		private bool submitted;

		private bool updateLinks;

		public Color32 errorColor = new Color32(238, 174, 0, byte.MaxValue);

		public Color32 defaultColor = new Color32(32, 181, 156, byte.MaxValue);

		private bool mEnterSoundPlayed;

		private bool fromParentalControls;

		private Dictionary<string, GameObject> marketingDisclosures = new Dictionary<string, GameObject>();

		private Dictionary<string, GameObject> legalDisclosures = new Dictionary<string, GameObject>();

		// Track focus states for standard InputField
		private bool _firstNameFocused;
		private bool _lastNameFocused;
		private bool _emailFocused;
		private bool _passwordFocused;

		private void Start()
		{
			emailInput.onValueChanged.AddListener(OnEmailValueChanged);
			
			ScrollView component = ScrollViewGameObject.GetComponent<ScrollView>();
			component.OnPointerDownDelegates = (ScrollView.OnPointerDownDelegate)Delegate.Combine(component.OnPointerDownDelegates, new ScrollView.OnPointerDownDelegate(OnScrollGotPointerDown));
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
			mEnterSoundPlayed = false;
			
			base.transform.Find("ScrollView/Content/AgreeCheck").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/MarketingHolder").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/LegalHolder").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/GLAC").gameObject.SetActive(true);
			base.transform.Find("ScrollView/Content/ReadPrivacyPracticesText").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13TermsBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13PrivacyPracticesBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13PrivacyPolicyBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13ChildrenOnlinePrivacyPolicyBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/ToViewText").gameObject.SetActive(false);
		}

		private void FirstNameInputFocusChanged(bool aFocus)
		{
			if (aFocus)
			{
				firstNameInput.transform.parent.gameObject.GetComponent<Image>().color = defaultColor;
				FirstNameErrorMessageGameObject.SetActive(false);
				if (!mEnterSoundPlayed)
				{
					mEnterSoundPlayed = true;
					Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/NextPage/BeginTyping");
				}
			}
			else
			{
				errorFirstName = (ageBand.Permissions.FirstName is IRegistrationPermissionRequired && DataChecker.IsNullEmptyOrJustWhiteSpace(firstNameInput.text)) || (ageBand.Permissions.FirstName is IRegistrationPermissionOptional && firstNameInput.text.Length != 0 && firstNameInput.text.Trim().Length == 0);
				if (errorFirstName)
				{
					firstNameInput.transform.parent.gameObject.GetComponent<Image>().color = errorColor;
					FirstNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_firstname_invalid");
					FirstNameErrorMessageGameObject.SetActive(true);
				}
			}
		}

		private void LastNameInputFocusChanged(bool aFocus)
		{
			if (aFocus)
			{
				lastNameInput.transform.parent.gameObject.GetComponent<Image>().color = defaultColor;
				LastNameErrorMessageGameObject.SetActive(false);
				if (!mEnterSoundPlayed)
				{
					mEnterSoundPlayed = true;
					Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/NextPage/BeginTyping");
				}
			}
			else
			{
				errorLastName = (ageBand.Permissions.LastName is IRegistrationPermissionRequired && DataChecker.IsNullEmptyOrJustWhiteSpace(lastNameInput.text)) || (ageBand.Permissions.LastName is IRegistrationPermissionOptional && lastNameInput.text.Length != 0 && lastNameInput.text.Trim().Length == 0);
				if (errorLastName)
				{
					lastNameInput.transform.parent.gameObject.GetComponent<Image>().color = errorColor;
					LastNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_lastname_invalid");
					LastNameErrorMessageGameObject.SetActive(true);
				}
			}
		}

		private void EmailInputFocusChanged(bool aFocus)
		{
			if (aFocus)
			{
				emailInput.transform.parent.gameObject.GetComponent<Image>().color = defaultColor;
				EmailErrorMessageGameObject.SetActive(false);
				if (!mEnterSoundPlayed)
				{
					mEnterSoundPlayed = true;
					Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/NextPage/BeginTyping");
				}
			}
			else
			{
				errorEmail = !DataChecker.IsValidEmail(emailInput.text);
				if (errorEmail)
				{
					emailInput.transform.parent.gameObject.GetComponent<Image>().color = errorColor;
					EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_invalid");
					EmailErrorMessageGameObject.SetActive(true);
				}
			}
		}

		private void PasswordInputFocusChanged(bool aFocus)
		{
			if (aFocus)
			{
				passwordInput.transform.parent.gameObject.GetComponent<Image>().color = defaultColor;
				PasswordErrorMessageGameObject.SetActive(false);
				if (!mEnterSoundPlayed)
				{
					mEnterSoundPlayed = true;
					Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/NextPage/BeginTyping");
				}
			}
			else
			{
				errorPassword = DataChecker.IsNullEmptyOrJustWhiteSpace(passwordInput.text);
				if (errorPassword)
				{
					passwordInput.transform.parent.gameObject.GetComponent<Image>().color = errorColor;
					PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_size");
					PasswordErrorMessageGameObject.SetActive(true);
				}
			}
		}

		private void OnEmailValueChanged(string aValue)
		{
			EmailErrorMessageGameObject.SetActive(false);
			emailInput.transform.parent.gameObject.GetComponent<Image>().color = defaultColor;
		}

		private void OnDestroy()
		{
			if (emailInput != null)
			{
				emailInput.onValueChanged.RemoveListener(OnEmailValueChanged);
			}
			if (MonoSingleton<NativeKeyboardManager>.Instance != null)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged -= OnKeyboardResize;
			}
		}

		private void Update()
		{
			if (firstNameInput.isFocused != _firstNameFocused) 
			{
				_firstNameFocused = firstNameInput.isFocused;
				FirstNameInputFocusChanged(_firstNameFocused);
			}
			if (lastNameInput.isFocused != _lastNameFocused) 
			{
				_lastNameFocused = lastNameInput.isFocused;
				LastNameInputFocusChanged(_lastNameFocused);
			}
			if (emailInput.isFocused != _emailFocused) 
			{
				_emailFocused = emailInput.isFocused;
				EmailInputFocusChanged(_emailFocused);
			}
			if (passwordInput.isFocused != _passwordFocused) 
			{
				_passwordFocused = passwordInput.isFocused;
				PasswordInputFocusChanged(_passwordFocused);
			}

			if (!submitted)
			{
				if (!MonoSingleton<ConnectionManager>.Instance.IsConnected || string.IsNullOrEmpty(emailInput.text) || string.IsNullOrEmpty(passwordInput.text) || !TouAccepted() || string.Empty.Equals(emailInput.text.Trim()) || string.Empty.Equals(passwordInput.text.Trim()) || (ageBand.Permissions.FirstName is IRegistrationPermissionRequired && (string.IsNullOrEmpty(firstNameInput.text) || string.Empty.Equals(firstNameInput.text.Trim()))) || (ageBand.Permissions.LastName is IRegistrationPermissionRequired && (string.IsNullOrEmpty(lastNameInput.text) || string.Empty.Equals(lastNameInput.text.Trim()))))
				{
					CreateAdultAccountButton.interactable = false;
				}
				else
				{
					CreateAdultAccountButton.interactable = true;
				}
			}
			QuestionButton.SetActive(string.IsNullOrEmpty(passwordInput.text));
			if (firstNameInput.isFocused || lastNameInput.isFocused || emailInput.isFocused || passwordInput.isFocused)
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

		public void Show(int aBirthYear, int aBirthMonth, int aBirthDay, IAgeBand aAgeBand, string aCountryCode, string aCountryName, bool aFromParentalControls, IAdultAccount aCaller)
		{
			birthYear = aBirthYear;
			birthMonth = aBirthMonth;
			birthDay = aBirthDay;
			ageBand = aAgeBand;
			countryCode = aCountryCode;
			countryName = aCountryName;
			fromParentalControls = aFromParentalControls;
			caller = aCaller;
			
			updateLinks = false;
			SetRegistrationStrings();
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged += OnKeyboardResize;
			Marketing.isOn = true;
			base.gameObject.SetActive(true);
			setUiInteractable(true);
			Analytics.LogCreateAdultPageView();
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
			marketingDisclosures.Clear();
			legalDisclosures.Clear();
			Transform transform = base.transform.Find("ScrollView/Content");
			foreach (Transform item in transform)
			{
				if (item.gameObject.name.Contains("DisclosureBlock(Clone)"))
				{
					UnityEngine.Object.Destroy(item.gameObject);
				}
			}
			int num = 1;
			CountryText.text = Singleton<Localizer>.Instance.getString("createaccountscreen.countrybtn.countrytext").Replace("#countryName#", countryName);
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

		public void OnCreateAdult()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			submitted = true;
			setUiInteractable(false);
			if (ClientValidateAdultAccountValues())
			{
				ServerValidateAdultAccountValues();
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
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
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

		// Replaced IsPassword to toggle standard inputType.
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
			if (ScrollViewGameObject == null || ScrollViewGameObject.GetComponent<ScrollRect>() == null)
			{
				return;
			}
			if (args.Height <= 0)
			{
				ScrollViewGameObject.GetComponent<ScrollRect>().enabled = true;
				return;
			}
			ScrollViewGameObject.GetComponent<ScrollRect>().enabled = false;
			if (firstNameInput.isFocused)
			{
				ScrollToInput(firstNameInput, ScrollContent, args.Height);
			}
			else if (lastNameInput.isFocused)
			{
				ScrollToInput(lastNameInput, ScrollContent, args.Height);
			}
			else if (emailInput.isFocused)
			{
				ScrollToInput(emailInput, ScrollContent, args.Height);
			}
			else if (passwordInput.isFocused)
			{
				ScrollToInput(passwordInput, ScrollContent, args.Height);
			}
		}

		// Replaced the custom NativeTextView.ScrollToInput function with a local equivalent 
		// so it can target regular InputFields safely.
		private void ScrollToInput(InputField input, RectTransform aRect, int aHeight)
		{
			aHeight = (int)((float)aHeight * Singleton<SettingsManager>.Instance.GetHeightScale());
			RectTransform component = aRect.transform.parent.GetComponent<RectTransform>();
			RectTransform component2 = input.transform.parent.GetComponent<RectTransform>();
			float num = component2.rect.height * component2.pivot.y;
			float num2 = num + (0f - component2.anchoredPosition.y);
			float num3 = aRect.rect.height - (component.anchoredPosition.y + (float)aHeight);
			float num4 = MixConstants.CANVAS_HEIGHT - (component.anchoredPosition.y + (float)aHeight);
			if (num2 < num3)
			{
				if (num2 < num4 / 2f)
				{
					aRect.anchoredPosition = new Vector2(aRect.anchoredPosition.x, 0f);
				}
				else
				{
					aRect.anchoredPosition = new Vector2(aRect.anchoredPosition.x, Mathf.Abs(num4 / 2f - num2));
				}
			}
		}

		private bool ClientValidateAdultAccountValues(InputField aField = null)
		{
			errorFirstName = (ageBand.Permissions.FirstName is IRegistrationPermissionRequired && DataChecker.IsNullEmptyOrJustWhiteSpace(firstNameInput.text)) || (ageBand.Permissions.FirstName is IRegistrationPermissionOptional && firstNameInput.text.Length != 0 && firstNameInput.text.Trim().Length == 0);
			errorLastName = (ageBand.Permissions.LastName is IRegistrationPermissionRequired && DataChecker.IsNullEmptyOrJustWhiteSpace(lastNameInput.text)) || (ageBand.Permissions.LastName is IRegistrationPermissionOptional && lastNameInput.text.Length != 0 && lastNameInput.text.Trim().Length == 0);
			errorEmail = !DataChecker.IsValidEmail(emailInput.text);
			errorPassword = DataChecker.IsNullEmptyOrJustWhiteSpace(passwordInput.text);
			ColorBlock colors = Tou.colors;
			Tou.colors = colors;
			string text = string.Empty;
			if (errorPassword && (aField == null || aField == passwordInput))
			{
				PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_invalid");
				Analytics.LogAdultAccountCreationFailure("invalid_password_format");
				text += PasswordErrorMessageGameObject.GetComponent<Text>().text;
			}
			else
			{
				errorPassword = passwordInput.text.Length < 6;
				if (errorPassword && (aField == null || aField == passwordInput))
				{
					Analytics.LogAdultAccountCreationFailure("invalid_password_size");
					PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_size");
					text = text + " " + PasswordErrorMessageGameObject.GetComponent<Text>().text;
				}
			}
			if (errorFirstName && (aField == null || aField == firstNameInput))
			{
				FirstNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_firstname_invalid");
				Analytics.LogAdultAccountCreationFailure("invalid_first_name");
				text = text + " " + FirstNameErrorMessageGameObject.GetComponent<Text>().text;
			}
			if (errorLastName && (aField == null || aField == lastNameInput))
			{
				LastNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_lastname_invalid");
				Analytics.LogAdultAccountCreationFailure("invalid_last_name");
				text = text + " " + LastNameErrorMessageGameObject.GetComponent<Text>().text;
			}
			if (errorEmail && (aField == null || aField == emailInput))
			{
				EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_invalid");
				Analytics.LogAdultAccountCreationFailure("invalid_email");
				text = text + " " + EmailErrorMessageGameObject.GetComponent<Text>().text;
			}
			if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled && !string.IsNullOrEmpty(text))
			{
				AccessibilityManager.Instance.Speak(text);
			}
			return !errorFirstName && !errorLastName && !errorEmail && !errorPassword;
		}

		private void toggleErrorFields(InputField aField = null)
		{
			EmailErrorMessageGameObject.SetActive(errorEmail && (aField == null || aField == emailInput));
			PasswordErrorMessageGameObject.SetActive(errorPassword && (aField == null || aField == passwordInput));
			FirstNameErrorMessageGameObject.SetActive(errorFirstName && (aField == null || aField == firstNameInput));
			LastNameErrorMessageGameObject.SetActive(errorLastName && (aField == null || aField == lastNameInput));
			firstNameInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorFirstName || (!(aField == null) && !(aField == firstNameInput))) ? defaultColor : errorColor);
			lastNameInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorLastName || (!(aField == null) && !(aField == lastNameInput))) ? defaultColor : errorColor);
			emailInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorEmail || (!(aField == null) && !(aField == emailInput))) ? defaultColor : errorColor);
			passwordInput.transform.parent.gameObject.GetComponent<Image>().color = ((!errorPassword || (!(aField == null) && !(aField == passwordInput))) ? defaultColor : errorColor);
			if (aField == null)
			{
				scrollToTop();
			}
		}

		private void ServerValidateAdultAccountValues()
		{
			NewAccountValidator newAccountValidator = new NewAccountValidator(MonoSingleton<LoginManager>.Instance.KeychainData, Log.MixLogger, MonoSingleton<LoginManager>.Instance.StorageDir, ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.GuestControllerClientId, MonoSingleton<LoginManager>.Instance.CoroutineManager);
			newAccountValidator.ValidateAdultAccount(emailInput.text, passwordInput.text, OnValidateResponse);
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
					string aTextToSpeak = string.Empty;
					foreach (IValidateNewAccountError error in aResult.Errors)
					{
						if (error is IValidateNewAccountPassswordMissingCharactersError)
						{
							Analytics.LogAdultAccountCreationFailure("invalid_password_format");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_format");
							aTextToSpeak = PasswordErrorMessageGameObject.GetComponent<Text>().text;
							continue;
						}
						if (error is IValidateNewAccountPasswordSizeError)
						{
							Analytics.LogAdultAccountCreationFailure("invalid_password_size");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_size");
							aTextToSpeak = PasswordErrorMessageGameObject.GetComponent<Text>().text;
							continue;
						}
						if (error is IValidateNewAccountPasswordCommonError)
						{
							Analytics.LogAdultAccountCreationFailure("password_too_common");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_too_common");
							aTextToSpeak = PasswordErrorMessageGameObject.GetComponent<Text>().text;
							continue;
						}
						if (error is IValidateNewAccountPasswordPhoneError)
						{
							Analytics.LogAdultAccountCreationFailure("password_matches_profile");
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_profile_info");
							aTextToSpeak = PasswordErrorMessageGameObject.GetComponent<Text>().text;
							continue;
						}
						if (error is IValidateNewAccountAccountFoundError || error is IValidateNewAccountAccountInUseError)
						{
							Analytics.LogUsernameTaken(AgeBandType.Adult);
							errorEmail = true;
							EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_taken");
							aTextToSpeak = EmailErrorMessageGameObject.GetComponent<Text>().text;
							continue;
						}
						if (error is IValidateNewAccountNotRegisteredTransactorError)
						{
							Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
							caller.HandleNrtAccount(emailInput.text);
							return;
						}
						if (error is IValidateNewAccountMultipleAccountsError)
						{
							errorEmail = true;
							Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
							caller.HandleMaseAccount(emailInput.text);
							return;
						}
						if (error is IValidateNewAccountUsernameError)
						{
							errorEmail = true;
							EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_invalid");
							Analytics.LogAdultAccountCreationFailure("invalid_email");
							aTextToSpeak = EmailErrorMessageGameObject.GetComponent<Text>().text;
							continue;
						}
						caller.ShowErrorOverlay("customtokens.register.error_create_account", string.Empty);
						break;
					}
					if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
					{
						AccessibilityManager.Instance.Speak(aTextToSpeak);
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
				return;
			}
			Dictionary<IMarketingItem, bool> dictionary = new Dictionary<IMarketingItem, bool>();
			foreach (IMarketingItem item in ageBand.Marketing)
			{
				if (marketingDisclosures.ContainsKey(item.Id))
				{
					dictionary.Add(item, marketingDisclosures[item.Id].transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().isOn);
				}
			}
			if (fromParentalControls)
			{
				MonoSingleton<LoginManager>.Instance.CreateGuardianAccount(birthYear, birthMonth, birthDay, firstNameInput.text, lastNameInput.text, emailInput.text, passwordInput.text, Localizer.GetLocale(), countryCode, dictionary, ageBand.LegalDocuments, OnCreateGuardianAccount);
			}
			else
			{
				MonoSingleton<LoginManager>.Instance.CreateAdultAccount(birthYear, birthMonth, birthDay, firstNameInput.text, lastNameInput.text, emailInput.text, passwordInput.text, Localizer.GetLocale(), countryCode, dictionary, ageBand.LegalDocuments, OnCreateAdultAccount);
			}
		}

		public void OnCreateGuardianAccount(IRegisterResult aResult)
		{
			Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
			OnCreateAdultAccount(aResult);
		}

		public void OnCreateAdultAccount(IRegisterResult aResult)
		{
			if (this.IsNullOrDisposed() || base.gameObject == null)
			{
				return;
			}
			if (aResult.Success)
			{
				caller.OnAccountCreated(aResult.Session.LocalUser);
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				Analytics.LogSuccessfulAdultAccountCreation();
				return;
			}
			if (aResult.Errors != null)
			{
				bool flag = false;
				string text = string.Empty;
				foreach (IInvalidProfileItemError error in aResult.Errors)
				{
					flag = true;
					if (error is IInvalidEmailError)
					{
						caller.ShowErrorOverlay("customtokens.register.error_email_taken", string.Empty);
						break;
					}
					if (error is IInvalidParentEmailError || error is IInvalidUsernameError)
					{
						if (!errorEmail)
						{
							EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_invalid");
							errorEmail = true;
							Analytics.LogAdultAccountCreationFailure("invalid_email");
							text += EmailErrorMessageGameObject.GetComponent<Text>().text;
						}
					}
					else if (error is IPasswordMatchesOtherProfileInfoError)
					{
						if (!errorPassword)
						{
							errorPassword = true;
							PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_profile_info");
							Analytics.LogAdultAccountCreationFailure("password_matches_profile");
							text = text + " " + PasswordErrorMessageGameObject.GetComponent<Text>().text;
						}
					}
					else if (error is IInvalidFirstNameError)
					{
						if (!errorFirstName)
						{
							FirstNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_firstname_invalid");
							errorFirstName = true;
							Analytics.LogAdultAccountCreationFailure("invalid_first_name");
							text = text + " " + FirstNameErrorMessageGameObject.GetComponent<Text>().text;
						}
					}
					else if (error is IInvalidLastNameError)
					{
						if (!errorLastName)
						{
							LastNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_lastname_invalid");
							errorLastName = true;
							Analytics.LogAdultAccountCreationFailure("invalid_last_name");
							text = text + " " + LastNameErrorMessageGameObject.GetComponent<Text>().text;
						}
					}
					else if (error is IInvalidPasswordError && !errorPassword)
					{
						errorPassword = true;
						PasswordErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_password_invalid");
						Analytics.LogAdultAccountCreationFailure("invalid_password_format");
						text = text + " " + PasswordErrorMessageGameObject.GetComponent<Text>().text;
					}
				}
				if (!flag)
				{
					caller.ShowErrorOverlay("customtokens.register.error_create_account", string.Empty);
				}
				else if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(text);
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
			CreateAdultAccountButton.interactable = aInteractable;
			DisneyIdLoginButton.interactable = aInteractable;
			BackButton.interactable = aInteractable;
			emailInput.interactable = aInteractable;
			passwordInput.interactable = aInteractable;
			ShowPassword.interactable = aInteractable;
			firstNameInput.interactable = aInteractable;
			lastNameInput.interactable = aInteractable;
			Marketing.interactable = aInteractable;
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
				if (emailInput.isFocused)
				{
					passwordInput.Select();
				}
				else if (passwordInput.isFocused)
				{
					firstNameInput.Select();
				}
				else if (firstNameInput.isFocused)
				{
					lastNameInput.Select();
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
