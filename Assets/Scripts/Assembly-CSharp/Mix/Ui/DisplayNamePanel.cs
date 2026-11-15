using System;
using System.Collections.Generic;
using System.Linq;
using Disney.Mix.SDK;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class DisplayNamePanel : BasePanel, IKeyboardSubmitHandler
	{
		public bool HandlesKeyboardSubmit(Selectable input)
		{
			return input == DisplayNameInputField;
		}

		public void SubmitFromKeyboard(Selectable input)
		{
			OnSubmitDisplayName();
		}

		public InputField DisplayNameInputField;

		public Text TempDisplayName;

		public Button SubmitDisplayNameButton;

		public GameObject DisplayNameGameObject;

		public GameObject ErrorMessageGameObject;

		private bool errorDisplayName;

		private bool submitted;

		public IDisplayNamePanelCallback caller;

		public override bool OnAndroidBackButton()
		{
			return true;
		}

		public void Init(IDisplayNamePanelCallback aCaller, IRegistrationProfile aUserInfo)
		{
			Analytics.LogCreateDisplayNamePageView();
			caller = aCaller;
			string text = string.Empty;
			if (aUserInfo != null)
			{
				text = aUserInfo.DisplayName;
			}
			if (TempDisplayName != null)
			{
				TempDisplayName.text = text;
			}
			base.gameObject.SetActive(true);
			SubmitDisplayNameButton.interactable = true;
			submitted = false;

		}

		private void Start()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnKeyboardReturnKey;
		}

		private void Update()
		{
			if (!submitted)
			{
				SubmitDisplayNameButton.interactable = !DataChecker.IsNullEmptyOrJustWhiteSpace(DisplayNameInputField.text);
			}
		}

		private void OnDestroy()
		{
			if (MonoSingleton<NativeKeyboardManager>.Instance != null && MonoSingleton<NativeKeyboardManager>.Instance.Keyboard != null)
			{
				MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnKeyboardReturnKey;
			}
		}

		public bool IsAlphaNumeric(string aString)
		{
			if (aString.All(char.IsLetterOrDigit))
			{
				return true;
			}
			return false;
		}

		public void OnSubmitDisplayName()
		{
			errorDisplayName = false;
			toggleErrorFields();
			SubmitDisplayNameButton.interactable = false;
			submitted = true;
			if (ClientValidateValues() && IsAlphaNumeric(DisplayNameInputField.text))
			{
				MixSession.User.UpdateDisplayName(DisplayNameInputField.text, delegate(IUpdateDisplayNameResult result)
				{
					if (!this.IsNullOrDisposed() && !(base.gameObject == null))
					{
						if (result.Success)
						{
							Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/NextPage/AfterNamingAvatar");
							MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
							Analytics.LogDisplayNameCreationSuccess();
							ShowDisplayNameAccepted(DisplayNameInputField.text, delegate
							{
							});
						}

						else
						{
							Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
							errorDisplayName = true;
							toggleErrorFields();
							submitted = false;
							SubmitDisplayNameButton.interactable = true;
							if (result is IUpdateDisplayNameFailedModerationResult)
							{
								ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_display_name_invalid");
							}
							else if (result is IUpdateDisplayNameExistsResult)
							{
								ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_display_name_taken");
								Analytics.LogDisplaynameTaken(MixSession.Session.LocalUser.AgeBandType);
							}
							else if (!MonoSingleton<ConnectionManager>.Instance.IsConnected)
							{
								ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_no_internet");
							}
							else
							{
								ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.global.generic_error");
							}
						}
					}
				});
			}
			else
			{
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_1_Error");
				errorDisplayName = true;
				ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_display_name_invalid");
				toggleErrorFields();
				submitted = false;
				SubmitDisplayNameButton.interactable = true;
			}
		}

		private void ShowDisplayNameAccepted(string newName, Action aCallback)
		{
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			OnPanelClosed value = delegate
			{
				if (caller != null)
				{
					caller.OnDisplayNameUpdated(this);
				}
				ClosePanel();
			};
			genericPanel.PanelClosedEvent += value;
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

		private bool ClientValidateValues()
		{
			errorDisplayName = DataChecker.IsNullEmptyOrJustWhiteSpace(DisplayNameInputField.text);
			if (errorDisplayName)
			{
				ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_display_name_invalid");
			}
			else
			{
				errorDisplayName = DisplayNameInputField.text.Length < 4 || DisplayNameInputField.text.Length > 15;
				if (errorDisplayName)
				{
					ErrorMessageGameObject.GetComponent<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.register.error_displayname_size");
				}
			}
			return !errorDisplayName;
		}

		private void toggleErrorFields()
		{
			DisplayNameGameObject.GetComponent<Outline>().enabled = errorDisplayName;
			ErrorMessageGameObject.SetActive(errorDisplayName);
		}

		private void OnKeyboardReturnKey(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
		{
			OnSubmitDisplayName();
		}

		private void InputFocusChanged(NativeTextView aField, bool aFocus)
		{
			if (aFocus)
			{
				Singleton<SoundManager>.Instance.PlaySoundEvent("UI/Registration/NextPage/BeginTyping");
			}
		}
	}
}
