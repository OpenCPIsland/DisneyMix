using Disney.Mix.SDK;
using Mix.DeviceDb;
using Mix.Session;
using Mix.Ui;
using Mix.User;

namespace Mix.SequenceOperations
{
	public class SoftLoginOperation : SequenceOperation
	{
		public SoftLoginOperation(IOperationCompleteHandler aCaller)
			: base(aCaller)
		{
		}

		public override void StartOperation()
		{
			BaseStartOperation();
			doSoftLogin();
		}

		private void doSoftLogin()
		{
			if (MonoSingleton<LoginManager>.Instance.LastProfileInfo != null && MonoSingleton<LoginManager>.Instance.IsProfileMissingInfo())
			{
				MonoSingleton<LoginManager>.Instance.Logout();
				NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionAnimations(false, "Intro", "ToLogin"));
				MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest, true);
				finish(OperationStatus.STATUS_SUCCESSFUL);
			}
			else
			{
				startOfflineLoggedIn();
			}
		}

		private void startOfflineLoggedIn()
		{
			if (MonoSingleton<NavigationManager>.Instance == null || Singleton<MixDocumentCollections>.Instance == null)
			{
				Log.Exception("Softlogin operation with NavigationManager:" + (MonoSingleton<NavigationManager>.Instance != null) + " MixDocumentCollections:" + (Singleton<MixDocumentCollections>.Instance != null));
				return;
			}
			MonoSingleton<LoginManager>.Instance.StartOfflineAndResume(delegate(bool r)
			{
				if (r)
				{
					if (MixSession.Session == null || MixSession.Session.LocalUser == null || MixSession.Session.LocalUser.RegistrationProfile == null)
					{
						Log.Exception("Staring online with missing session or user or registration profile");
						finish(OperationStatus.STATUS_FAILED);
					}
					else
					{
						DisplayNameProposedStatus displayNameProposedStatus = MixSession.Session.LocalUser.RegistrationProfile.DisplayNameProposedStatus;
						if (displayNameProposedStatus == DisplayNameProposedStatus.Accepted)
						{
							NavigationRequest navigationRequest = NavToConvo();
							if (navigationRequest != null)
							{
								MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest, true);
							}
							MixSession.Session.LocalUser.RefreshProfile(delegate(IRefreshProfileResult result)
							{
								NavigationRequest navigationRequest2 = null;
								if (result.Success)
								{
									navigationRequest2 = handleDisplayNameStatus(false);
									if (navigationRequest2 != null)
									{
										MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest2);
									}
								}
							});
							finish(OperationStatus.STATUS_SUCCESSFUL);
						}
						else
						{
							MixSession.Session.LocalUser.RefreshProfile(delegate
							{
								NavigationRequest navigationRequest2 = handleDisplayNameStatus(true);
								if (navigationRequest2 != null)
								{
									MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest2);
								}
								finish(OperationStatus.STATUS_SUCCESSFUL);
							});
						}
					}
				}
				else
				{
					NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionAnimations(false, "Intro", "ToLogin"));
					MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest, true);
					finish(OperationStatus.STATUS_SUCCESSFUL);
				}
			});
		}

		private NavigationRequest handleDisplayNameStatus(bool navToConvo)
		{
			NavigationRequest navigationRequest = null;
			// The profile refresh can answer after the session was discarded
			// (auth lost -> back to login); there's nothing to navigate then.
			if (MixSession.Session == null || MixSession.Session.LocalUser == null || MixSession.Session.LocalUser.RegistrationProfile == null)
			{
				return null;
			}
			DisplayNameProposedStatus displayNameProposedStatus = MixSession.Session.LocalUser.RegistrationProfile.DisplayNameProposedStatus;
			if (displayNameProposedStatus != DisplayNameProposedStatus.Accepted)
			{
				Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.SaveUserValueFromInt("displayname.approved.seen", 0);
			}
			switch (displayNameProposedStatus)
			{
			case DisplayNameProposedStatus.Rejected:
				navigationRequest = new NavigationRequest("Prefabs/Screens/Profile/ProfileScreen", new TransitionAnimations(false, "FromSoftLogin"));
				navigationRequest.AddData("mode", ProfileController.PROFILE_MODES.INFO_UPDATE);
				break;
			case DisplayNameProposedStatus.None:
				navigationRequest = new NavigationRequest("Prefabs/Screens/AvatarEditor/AvatarEditorScreen", new TransitionAnimations(false, "ToDisplayNameFromSoftLogin"));
				navigationRequest.AddData("mode", AvatarEditorController.EDITOR_MODES.CREATOR);
				navigationRequest.AddData("missingDisplayName", true);
				break;
			default:
			{
				string text = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.LoadUserValue("displayname.approved.seen");
				if (displayNameProposedStatus == DisplayNameProposedStatus.Accepted && !string.IsNullOrEmpty(text) && text == "0")
				{
					navigationRequest = new NavigationRequest("Prefabs/Screens/Profile/ProfileScreen", new TransitionAnimations(false, "FromSoftLogin"));
				}
				else if (navToConvo)
				{
					navigationRequest = NavToConvo();
				}
				break;
			}
			}
			return navigationRequest;
		}

		private static NavigationRequest NavToConvo()
		{
			NavigationRequest navigationRequest = null;
			if (!MonoSingleton<PushNotifications>.Instance.IsPendingNavRequest())
			{
				navigationRequest = new NavigationRequest("Prefabs/Screens/Conversations/ConversationsScreen", new TransitionAnimations(false, "FromSoftLogin"));
				if (!MonoSingleton<PushNotifications>.Instance.HaveShownPushPrePopup)
				{
					navigationRequest.AddData("regDone", true);
				}
			}
			return navigationRequest;
		}
	}
}
