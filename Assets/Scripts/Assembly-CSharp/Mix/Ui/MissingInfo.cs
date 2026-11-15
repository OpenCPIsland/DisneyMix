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
	public class MissingInfo : MonoBehaviour, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == firstNameInput || input == lastNameInput || input == emailInput || input == parentsEmailInput || input == usernameInput;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnUpdateInfo();
		}

		public Toggle Marketing;

		public NativeTextView firstNameInput;

		public NativeTextView lastNameInput;

		public NativeTextView emailInput;

		public NativeTextView parentsEmailInput;

		public NativeTextView usernameInput;

		public Button UpdateAccountButton;

		public RectTransform ScrollContent;

		public GameObject ScrollViewGameObject;

		public GameObject FirstNameInputGameObject;

		public GameObject LastNameInputGameObject;

		public GameObject EmailInputGameObject;

		public GameObject ParentsEmailInputGameObject;

		public GameObject UsernameInputGameObject;

		public GameObject MarketingHolder;

		public GameObject FirstNameErrorMessageGameObject;

		public GameObject LastNameErrorMessageGameObject;

		public GameObject EmailErrorMessageGameObject;

		public GameObject LastNameTextGameObject;

		public GameObject ParentEmailTextGameObject;

		public GameObject UsernameTextGameObject;

		public GameObject EmailTextGameObject;

		public GameObject DisclosureBlockGameObject;

		private ILocalUser localUser;

		private IMissingInfo caller;

		private bool errorFirstName;

		private bool errorLastName;

		private bool errorEmail;

		private bool errorParentEmail;

		private ILoginResult loginResult;

		private bool updateLinks;

		private bool submitted;

		private IAgeBand ageBand;

		private SdkActions actionGenerator = new SdkActions();

		private Dictionary<string, GameObject> marketingDisclosures = new Dictionary<string, GameObject>();

		private Dictionary<string, GameObject> legalDisclosures = new Dictionary<string, GameObject>();

		private bool needUpdate;

		public Color32 errorColor = new Color32(238, 174, 0, byte.MaxValue);

		public Color32 defaultColor = new Color32(32, 181, 156, byte.MaxValue);

		private void Start()
		{
			ScrollView component = ScrollViewGameObject.GetComponent<ScrollView>();
			component.OnPointerDownDelegates = (ScrollView.OnPointerDownDelegate)Delegate.Combine(component.OnPointerDownDelegates, new ScrollView.OnPointerDownDelegate(OnScrollGotPointerDown));
			firstNameInput.KeyboardFocusChanged += InputFocusChanged;
			lastNameInput.KeyboardFocusChanged += InputFocusChanged;
			emailInput.KeyboardFocusChanged += InputFocusChanged;
			parentsEmailInput.KeyboardFocusChanged += InputFocusChanged;
			usernameInput.KeyboardFocusChanged += InputFocusChanged;
			emailInput.KeyboardValueChanged += OnEmailValueChanged;
			base.transform.Find("ScrollView/Content/FirstNameText").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/MarketingHolder").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/LegalHolder").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/ReadPrivacyPracticesText").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13TermsBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13PrivacyPracticesBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13PrivacyPolicyBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/Over13ChildrenOnlinePrivacyPolicyBtn").gameObject.SetActive(false);
			base.transform.Find("ScrollView/Content/ToViewText").gameObject.SetActive(false);
		}

		private void OnApplicationQuit()
		{
			if (needUpdate)
			{
				MonoSingleton<LoginManager>.Instance.Logout();
			}
		}

		private void InputFocusChanged(NativeTextView aField, bool aFocus)
		{
			if (!aFocus && !ClientValidateAccountValues(aField))
			{
				toggleErrorFields(aField);
			}
		}

		private void OnEmailValueChanged(NativeTextView aField, string aValue)
		{
			EmailErrorMessageGameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			firstNameInput.KeyboardFocusChanged -= InputFocusChanged;
			lastNameInput.KeyboardFocusChanged -= InputFocusChanged;
			emailInput.KeyboardFocusChanged -= InputFocusChanged;
			parentsEmailInput.KeyboardFocusChanged -= InputFocusChanged;
			usernameInput.KeyboardFocusChanged -= InputFocusChanged;
		}

		private void Update()
		{
			if (!submitted && ageBand != null)
			{
				if (localUser.AgeBandType == AgeBandType.Child)
				{
					if ((ageBand.Permissions.FirstName is IRegistrationPermissionRequired && string.IsNullOrEmpty(localUser.RegistrationProfile.FirstName) && DataChecker.IsNullEmptyOrJustWhiteSpace(firstNameInput.Value)) || (ageBand.Permissions.ParentEmail is IRegistrationPermissionRequired && string.IsNullOrEmpty(localUser.RegistrationProfile.ParentEmail) && DataChecker.IsNullEmptyOrJustWhiteSpace(parentsEmailInput.Value)) || !TouAccepted())
					{
						UpdateAccountButton.interactable = false;
					}
					else
					{
						UpdateAccountButton.interactable = true;
					}
				}
				else if ((ageBand.Permissions.FirstName is IRegistrationPermissionRequired && string.IsNullOrEmpty(localUser.RegistrationProfile.FirstName) && DataChecker.IsNullEmptyOrJustWhiteSpace(firstNameInput.Value)) || (ageBand.Permissions.LastName is IRegistrationPermissionRequired && string.IsNullOrEmpty(localUser.RegistrationProfile.LastName) && DataChecker.IsNullEmptyOrJustWhiteSpace(lastNameInput.Value)) || (ageBand.Permissions.Email is IRegistrationPermissionRequired && string.IsNullOrEmpty(localUser.RegistrationProfile.Email) && DataChecker.IsNullEmptyOrJustWhiteSpace(emailInput.Value)) || !TouAccepted())
				{
					UpdateAccountButton.interactable = false;
				}
				else
				{
					UpdateAccountButton.interactable = true;
				}
				if (updateLinks)
				{
					foreach (GameObject value in marketingDisclosures.Values)
					{
						TextWithEvents component = value.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
						InitHyperlinks(component);
					}
					foreach (GameObject value2 in legalDisclosures.Values)
					{
						TextWithEvents component2 = value2.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
						InitHyperlinks(component2);
					}
					updateLinks = false;
				}
			}
			if (firstNameInput.Selected || lastNameInput.Selected || usernameInput.Selected || emailInput.Selected || parentsEmailInput.Selected || emailInput.Selected)
			{
				ScrollViewGameObject.GetComponent<ScrollRect>().vertical = false;
			}
			else
			{
				ScrollViewGameObject.GetComponent<ScrollRect>().vertical = true;
			}
		}

		public void Show(ILocalUser aLocalUser, IAgeBand aAgeBand, IMissingInfo aCaller, ILoginResult aLoginResult = null)
		{
			localUser = aLocalUser;
			ageBand = aAgeBand;
			loginResult = aLoginResult;
			caller = aCaller;
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged += OnKeyboardResize;
			ParentsEmailInputGameObject.SetActive(false);
			EmailInputGameObject.SetActive(false);
			UsernameInputGameObject.SetActive(false);
			LastNameInputGameObject.SetActive(false);
			FirstNameInputGameObject.SetActive(false);
			MarketingHolder.SetActive(false);
			needUpdate = true;
			if (ageBand == null)
			{
				RegistrationConfigurationGetter registrationConfigurationGetter = new RegistrationConfigurationGetter(MonoSingleton<LoginManager>.Instance.KeychainData, Log.MixLogger, MonoSingleton<LoginManager>.Instance.StorageDir, ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.GuestControllerClientId, MonoSingleton<LoginManager>.Instance.CoroutineManager, ExternalizedConstants.MixPlatformServicesUrl, ExternalizedConstants.MixPlatformServicesCellophane);
				registrationConfigurationGetter.Get(loginResult.Session.LocalUser.RegistrationProfile.CountryCode, delegate(IGetRegistrationConfigurationResult x)
				{
					OnSiteConfigResponse(true, x);
				});
			}
			else
			{
				InitLayout();
			}
		}

		private void OnSiteConfigResponse(bool aFormSubmit, IGetRegistrationConfigurationResult aResult)
		{
			if (!aResult.Success)
			{
				caller.ShowErrorOverlay("customtokens.register.error_birthday_check", string.Empty);
				return;
			}
			DateTime? dateOfBirth = MonoSingleton<LoginManager>.Instance.LastProfileInfo.DateOfBirth;
			aResult.Configuration.GetUpdateAgeBand(ComputeAge(dateOfBirth.Value.Year, dateOfBirth.Value.Month, dateOfBirth.Value.Day), "en-US", delegate(IGetAgeBandResult ageBandResult)
			{
				if (ageBandResult.Success)
				{
					ageBand = ageBandResult.AgeBand;
					InitLayout();
				}
				else
				{
					caller.ShowErrorOverlay("customtokens.register.error_birthday_check", string.Empty);
				}
			});
		}

		public void InitLayout()
		{
			if (this.IsNullOrDisposed())
			{
				return;
			}
			if (localUser.AgeBandType == AgeBandType.Child)
			{
				UsernameInputGameObject.SetActive(true);
				usernameInput.Value = localUser.RegistrationProfile.Username;
				usernameInput.interactable = false;
				usernameInput.TextComponent.GetComponent<LocalizedText>().doNotLocalize = true;
				ParentsEmailInputGameObject.SetActive(true);
				if (!string.IsNullOrEmpty(localUser.RegistrationProfile.ParentEmail))
				{
					parentsEmailInput.Value = localUser.RegistrationProfile.ParentEmail;
					parentsEmailInput.interactable = false;
					parentsEmailInput.TextComponent.GetComponent<LocalizedText>().doNotLocalize = true;
				}
				LastNameTextGameObject.SetActive(false);
				EmailTextGameObject.SetActive(false);
				Analytics.LogPageView("missing_info_child");
			}
			else
			{
				if (!(ageBand.Permissions.LastName is IRegistrationPermissionNotAllowed))
				{
					LastNameInputGameObject.SetActive(true);
					if (!string.IsNullOrEmpty(localUser.RegistrationProfile.LastName))
					{
						lastNameInput.Value = localUser.RegistrationProfile.LastName;
						lastNameInput.interactable = false;
						lastNameInput.TextComponent.GetComponent<LocalizedText>().doNotLocalize = true;
					}
				}
				EmailInputGameObject.SetActive(true);
				if (!string.IsNullOrEmpty(localUser.RegistrationProfile.Email))
				{
					emailInput.Value = localUser.RegistrationProfile.Email;
					emailInput.interactable = false;
					emailInput.TextComponent.GetComponent<LocalizedText>().doNotLocalize = true;
				}
				UsernameTextGameObject.SetActive(false);
				ParentEmailTextGameObject.SetActive(false);
				Analytics.LogPageView("missing_info_adult");
			}
			SetRegistrationStrings();
			base.gameObject.SetActive(true);
			setUiInteractable(true);
			submitted = false;
		}

		public void SetRegistrationStrings()
		{
			marketingDisclosures.Clear();
			legalDisclosures.Clear();
			HashSet<string> hashSet = new HashSet<string>();
			HashSet<string> hashSet2 = new HashSet<string>();
			if (loginResult is ILoginRequiresLegalMarketingUpdateResult)
			{
				foreach (IMarketingItem item in ((ILoginRequiresLegalMarketingUpdateResult)loginResult).Marketing)
				{
					hashSet.Add(item.Id);
				}
				foreach (ILegalDocument legalDocument in ((ILoginRequiresLegalMarketingUpdateResult)loginResult).LegalDocuments)
				{
					hashSet2.Add(legalDocument.Id);
				}
			}
			Transform transform = base.transform.Find("ScrollView/Content");
			foreach (Transform item2 in transform)
			{
				if (item2.gameObject.name.Contains("DisclosureBlock(Clone)"))
				{
					UnityEngine.Object.Destroy(item2.gameObject);
				}
			}
			int num = 1;
			int num2 = base.transform.Find("ScrollView/Content/MarketingHolder").GetSiblingIndex();
			DisclosureBlockGameObject.SetActive(false);
			if (localUser.AgeBandType != AgeBandType.Child)
			{
				foreach (IMarketingItem item3 in ageBand.Marketing)
				{
					if (hashSet.Contains(item3.Id))
					{
						GameObject gameObject = UnityEngine.Object.Instantiate(DisclosureBlockGameObject);
						marketingDisclosures.Add(item3.Id, gameObject);
						gameObject.name = "DisclosureBlock(Clone)" + num;
						gameObject.SetActive(true);
						gameObject.transform.SetParent(transform, false);
						gameObject.transform.SetSiblingIndex(++num2);
						TextWithEvents component = gameObject.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
						component.text = item3.Text;
						gameObject.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().interactable = true;
						gameObject.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().isOn = item3.Checked;
						num++;
					}
				}
			}
			foreach (ILegalDocument legalDocument2 in ageBand.LegalDocuments)
			{
				if (hashSet2.Contains(legalDocument2.Id))
				{
					GameObject gameObject2 = UnityEngine.Object.Instantiate(DisclosureBlockGameObject);
					legalDisclosures.Add(legalDocument2.Id, gameObject2);
					gameObject2.name = "DisclosureBlock(Clone)" + num;
					gameObject2.SetActive(true);
					gameObject2.transform.SetParent(transform, false);
					gameObject2.transform.SetSiblingIndex(++num2);
					TextWithEvents component2 = gameObject2.transform.Find("AcceptTerms/CheckboxText/DisclosureText").GetComponent<TextWithEvents>();
					component2.text = legalDocument2.Text;
					if (legalDocument2.DisplayCheckbox)
					{
						gameObject2.transform.Find("AcceptTerms/CheckboxText/Checkbox").gameObject.SetActive(true);
						gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.SetActive(true);
						gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.GetComponent<Toggle>().interactable = true;
					}
					else
					{
						gameObject2.transform.Find("AcceptTerms/CheckboxText/Checkbox").gameObject.SetActive(false);
						gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.SetActive(false);
						gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").gameObject.GetComponent<Toggle>().interactable = false;
					}
					gameObject2.transform.Find("AcceptTerms/CheckboxText/CheckboxHit").GetComponent<Toggle>().isOn = false;
					num++;
				}
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
			else
			{
				Application.OpenUrl(linkInfo[0]);
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
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
			ScrollViewGameObject.GetComponent<ScrollRect>().vertical = true;
		}

		public void OnSparkRulesClicked()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			caller.ShowRulesOverlay();
		}

		public void OnPrivacyPracticesClicked()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			caller.ShowPrivacyPracticesOverlay();
		}

		public void OnUpdateInfo()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			setUiInteractable(false);
			submitted = true;
			if (ClientValidateAccountValues())
			{
				ServerValidateAccountValues();
				return;
			}

			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			toggleErrorFields();
			setUiInteractable(true);
			submitted = false;
		}

		private void OnKeyboardResize(object sender, NativeKeyboardHeightChangedEventArgs args)
		{
			if (args.Height <= 0)
			{
				ScrollViewGameObject.GetComponent<ScrollRect>().vertical = true;
				return;
			}
			ScrollViewGameObject.GetComponent<ScrollRect>().vertical = false;
			if (firstNameInput.Selected)
			{
				firstNameInput.ScrollToInput(ScrollContent, args.Height);
			}
			else if (lastNameInput.Selected)
			{
				lastNameInput.ScrollToInput(ScrollContent, args.Height);
			}
			else if (usernameInput.Selected)
			{
				usernameInput.ScrollToInput(ScrollContent, args.Height);
			}
			else if (emailInput.Selected)
			{
				emailInput.ScrollToInput(ScrollContent, args.Height);
			}
			else if (parentsEmailInput.Selected)
			{
				parentsEmailInput.ScrollToInput(ScrollContent, args.Height);
			}
			else if (emailInput.Selected)
			{
				emailInput.ScrollToInput(ScrollContent, args.Height);
			}
		}

		private bool ClientValidateAccountValues(NativeTextView aField = null)
		{
			errorFirstName = string.IsNullOrEmpty(localUser.RegistrationProfile.FirstName) && DataChecker.IsNullEmptyOrJustWhiteSpace(firstNameInput.Value);
			if (errorFirstName && (aField == null || aField == firstNameInput))
			{
				FirstNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_firstname_invalid");
				if (localUser.AgeBandType == AgeBandType.Child)
				{
					Analytics.LogChildAccountCreationFailure("invalid_first_name");
				}
				else
				{
					Analytics.LogAdultAccountCreationFailure("invalid_first_name");
				}
				if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
				{
					AccessibilityManager.Instance.Speak(FirstNameErrorMessageGameObject.GetComponent<Text>().text);
				}
			}
			if (localUser.AgeBandType == AgeBandType.Child)
			{
				errorParentEmail = string.IsNullOrEmpty(localUser.RegistrationProfile.ParentEmail) && !DataChecker.IsValidEmail(parentsEmailInput.Value);
				if (errorParentEmail && (aField == null || aField == parentsEmailInput))
				{
					EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_parent_email_invalid");
					Analytics.LogChildAccountCreationFailure("invalid_parent_email");
					if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
					{
						AccessibilityManager.Instance.Speak(EmailErrorMessageGameObject.GetComponent<Text>().text);
					}
				}
			}
			else
			{
				errorLastName = false;
				if (errorLastName && (aField == null || aField == lastNameInput))
				{
					LastNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_lastname_invalid");
					Analytics.LogAdultAccountCreationFailure("invalid_last_name");
					if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
					{
						AccessibilityManager.Instance.Speak(LastNameErrorMessageGameObject.GetComponent<Text>().text);
					}
				}
				errorEmail = string.IsNullOrEmpty(localUser.RegistrationProfile.Email) && !DataChecker.IsValidEmail(emailInput.Value);
				if (errorEmail && (aField == null || aField == emailInput))
				{
					EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_invalid");
					Analytics.LogAdultAccountCreationFailure("invalid_email");
					if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
					{
						AccessibilityManager.Instance.Speak(EmailErrorMessageGameObject.GetComponent<Text>().text);
					}
				}
			}
			return !errorFirstName && !errorLastName && !errorParentEmail && !errorEmail;
		}

		private void ServerValidateAccountValues()
		{
			if (localUser.AgeBandType != AgeBandType.Child && emailInput.interactable)
			{
				NewAccountValidator newAccountValidator = new NewAccountValidator(MonoSingleton<LoginManager>.Instance.KeychainData, Log.MixLogger, MonoSingleton<LoginManager>.Instance.StorageDir, ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.GuestControllerClientId, MonoSingleton<LoginManager>.Instance.CoroutineManager);
				newAccountValidator.ValidateAdultAccount(emailInput.Value, string.Empty, OnValidateResponse);
			}
			else
			{
				UpdateProfile();
			}
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
						if (error is IValidateNewAccountAccountFoundError || error is IValidateNewAccountAccountInUseError)
						{
							Analytics.LogUsernameTaken(AgeBandType.Adult);
							errorEmail = true;
							EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_taken");
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
			}
			else
			{
				UpdateProfile();
			}
		}

		private void UpdateProfile()
		{
			if (localUser.AgeBandType == AgeBandType.Child)
			{
				localUser.UpdateProfile((!string.IsNullOrEmpty(localUser.RegistrationProfile.FirstName)) ? null : firstNameInput.Value, null, null, null, (!string.IsNullOrEmpty(localUser.RegistrationProfile.ParentEmail)) ? null : parentsEmailInput.Value, null, null, ageBand.LegalDocuments, actionGenerator.CreateAction(delegate(IUpdateProfileResult x)
				{
					OnUpdateProfile(x, true);
				}));
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
			localUser.UpdateProfile((!string.IsNullOrEmpty(localUser.RegistrationProfile.FirstName)) ? null : firstNameInput.Value, (!string.IsNullOrEmpty(localUser.RegistrationProfile.LastName)) ? null : lastNameInput.Value, null, (!string.IsNullOrEmpty(localUser.RegistrationProfile.Email)) ? null : emailInput.Value, null, null, dictionary, ageBand.LegalDocuments, actionGenerator.CreateAction(delegate(IUpdateProfileResult x)
			{
				OnUpdateProfile(x, false);
			}));
		}

		private void toggleErrorFields(NativeTextView aField = null)
		{
			FirstNameInputGameObject.transform.parent.parent.gameObject.GetComponent<Image>().color = ((!errorFirstName || (!(aField == null) && !(aField == firstNameInput))) ? defaultColor : errorColor);
			FirstNameErrorMessageGameObject.SetActive(errorFirstName && (aField == null || aField == firstNameInput));
			LastNameInputGameObject.transform.parent.parent.gameObject.GetComponent<Image>().color = ((!errorLastName || (!(aField == null) && !(aField == lastNameInput))) ? defaultColor : errorColor);
			LastNameErrorMessageGameObject.SetActive(errorLastName && (aField == null || aField == lastNameInput));
			EmailInputGameObject.transform.parent.parent.gameObject.GetComponent<Image>().color = ((!errorEmail || (!(aField == null) && !(aField == emailInput))) ? defaultColor : errorColor);
			ParentsEmailInputGameObject.transform.parent.parent.gameObject.GetComponent<Image>().color = ((!errorParentEmail || (!(aField == null) && !(aField == parentsEmailInput))) ? defaultColor : errorColor);
			EmailErrorMessageGameObject.SetActive((errorEmail && (aField == null || aField == emailInput)) || (errorParentEmail && (aField == null || aField == parentsEmailInput)));
		}

		private void setUiInteractable(bool aInteractable)
		{
			UpdateAccountButton.interactable = aInteractable;
		}

		private void OnUpdateProfile(IUpdateProfileResult aResult, bool aIsChild)
		{
			if (this.IsNullOrDisposed() || base.gameObject == null || aResult == null)
			{
				return;
			}
			if (aResult.Success)
			{
				needUpdate = false;
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				MonoSingleton<LoginManager>.Instance.StartUserServices(delegate
				{
					caller.OnInfoUpdated();
				});
				return;
			}
			bool flag = false;
			foreach (IInvalidProfileItemError error in aResult.Errors)
			{
				flag = true;
				if (error is IInvalidFirstNameError)
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
						break;
					}
				}
				else if (aIsChild && error is IInvalidParentEmailError)
				{
					if (!errorEmail)
					{
						EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_parent_email_invalid");
						errorEmail = (errorParentEmail = true);
						Analytics.LogChildAccountCreationFailure("invalid_parent_email");
						if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
						{
							AccessibilityManager.Instance.Speak(EmailErrorMessageGameObject.GetComponent<Text>().text);
						}
					}
				}
				else if (error is IInvalidEmailError)
				{
					if (!errorEmail)
					{
						EmailErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_email_invalid");
						errorEmail = (errorParentEmail = true);
						Analytics.LogAdultAccountCreationFailure("invalid_email");
						if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
						{
							AccessibilityManager.Instance.Speak(EmailErrorMessageGameObject.GetComponent<Text>().text);
						}
					}
				}
				else if (!aIsChild && error is IInvalidLastNameError && !errorLastName)
				{
					LastNameErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_lastname_invalid");
					errorLastName = true;
					Analytics.LogAdultAccountCreationFailure("invalid_last_name");
					if (MonoSingleton<NativeAccessiblityManager>.Instance.IsEnabled)
					{
						AccessibilityManager.Instance.Speak(LastNameErrorMessageGameObject.GetComponent<Text>().text);
					}
				}
			}
			if (!flag)
			{
				caller.ShowErrorOverlay("customtokens.register.error_create_account", string.Empty);
			}
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
			toggleErrorFields();
			setUiInteractable(true);
			submitted = false;
		}

		private int ComputeAge(int aYear, int aMonth, int aDay)
		{
			int num = DateTime.Now.Year - aYear;
			if (aMonth > DateTime.Now.Month)
			{
				num--;
			}
			else if (aMonth == DateTime.Now.Month && aDay > DateTime.Now.Day)
			{
				num--;
			}
			return num;
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

		private void InitHyperlinks(TextWithEvents disclosure)
		{
			if (disclosure.links.Count <= 0)
			{
				return;
			}
			disclosure.AddButtons();
			foreach (string link in disclosure.links.Keys)
			{
				int num = 1;
				Transform transform;
				while ((transform = disclosure.transform.Find(link + num)) != null)
				{
					Button component = transform.GetComponent<Button>();
					component.onClick.AddListener(delegate
					{
						onLinkClicked(disclosure.links[link]);
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
