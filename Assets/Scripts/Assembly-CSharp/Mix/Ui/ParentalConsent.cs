using System;
using System.Collections.Generic;
using Disney.Mix.SDK;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.Localization;
using Mix.Session;
using Mix.User;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ParentalConsent
	{
		public static void ShowGateDialog()
		{
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			TimeSpan timeSpan = TimeSpan.MaxValue;
			string text = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.LoadUserValue("TimeParentEmailSent");
			if (text != null)
			{
				DateTime dateTime = new DateTime(long.Parse(text));
				timeSpan = DateTime.UtcNow - dateTime;
			}
			string text2 = Singleton<Localizer>.Instance.getString("customtokens.panels.parent_gate").Replace("#ParentEmail#", MixSession.User.RegistrationProfile.ParentEmail);
			if (timeSpan.TotalHours >= 24.0 && MonoSingleton<ConnectionManager>.Instance.IsConnected)
			{
				text2 = text2 + "\n" + Singleton<Localizer>.Instance.getString("customtokens.panels.parent_gate_email_again");
				Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
				{
					{ genericPanel.TitleText, "customtokens.panels.parent_gate_title" },
					{ genericPanel.MessageText, text2 },
					{ genericPanel.ButtonOneText, "customtokens.login.logout_no" },
					{ genericPanel.ButtonTwoText, "customtokens.login.logout_yes" }
				});
				bool isClicked = false;
				Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action>
				{
					{
						genericPanel.ButtonOne,
						delegate
						{
							MonoSingleton<LoginManager>.Instance.TriggerRefreshProfile();
						}
					},
					{
						genericPanel.ButtonTwo,
						delegate
						{
							if (!isClicked)
							{
								isClicked = true;
								MonoSingleton<LoginManager>.Instance.TriggerRefreshProfile();
							}
						}
					}
				});
			}
			else
			{
				Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
				{
					{ genericPanel.TitleText, "customtokens.panels.parent_gate_title" },
					{ genericPanel.MessageText, text2 },
					{ genericPanel.ButtonOneText, "customtokens.panels.button_ok" }
				});
				Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action>
				{
					{
						genericPanel.ButtonOne,
						delegate
						{
							MonoSingleton<LoginManager>.Instance.TriggerRefreshProfile();
						}
					},
					{ genericPanel.ButtonTwo, null }
				});
			}
		}

		public static void ShowParentEmailSentDialog()
		{
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			genericPanel.ShowSimpleError("customtokens.panels.parent_email_sent_title", Singleton<Localizer>.Instance.getString("customtokens.panels.parent_email_sent").Replace("#ParentEmail#", MixSession.User.RegistrationProfile.ParentEmail));
			Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
			{
				{ genericPanel.TitleText, "customtokens.panels.parent_email_sent_title" },
				{
					genericPanel.MessageText,
					Singleton<Localizer>.Instance.getString("customtokens.panels.parent_email_sent").Replace("#ParentEmail#", MixSession.User.RegistrationProfile.ParentEmail)
				},
				{ genericPanel.ButtonOneText, "customtokens.panels.button_ok" }
			});
			Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action>
			{
				{
					genericPanel.ButtonOne,
					delegate
					{
					}
				},
				{ genericPanel.ButtonTwo, null }
			});
		}

		public static void ShowAuthLostDialog()
		{
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			genericPanel.ShowSimpleError("customtokens.panels.parent_auth_lost_title", "customtokens.panels.parent_auth_lost");
		}

		public static void ShowConsentGrantedDialog()
		{
			GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
			genericPanel.ShowSimpleError("customtokens.panels.parent_granted_title", "customtokens.panels.parent_granted");
			Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action>
			{
				{
					genericPanel.ButtonOne,
					delegate
					{
						MonoSingleton<LoginManager>.Instance.DiscardAndReturnToLogin("parentalConsentGranted");
					}
				},
				{ genericPanel.ButtonTwo, null }
			});
		}
	}
}
