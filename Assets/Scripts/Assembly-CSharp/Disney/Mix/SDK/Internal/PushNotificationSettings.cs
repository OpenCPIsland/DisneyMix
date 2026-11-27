using System;
using Disney.Mix.SDK.Internal.MixDomain;

namespace Disney.Mix.SDK.Internal
{
	public static class PushNotificationSettings
	{
		public static void EnableAllPushNotifications(AbstractLogger logger, IDatabase database, IMixWebCallFactory mixWebCallFactory, string token, PushNotificationService service, string provisionId, string swid, Action<IEnableAllPushNotificationsResult> callback)
		{
			string tokenType = GetTokenType(service);
			Enable(logger, database, mixWebCallFactory, token, tokenType, provisionId, true, swid, delegate
			{
				callback(new EnableAllPushNotificationsResult(true));
			}, delegate
			{
				callback(new EnableAllPushNotificationsResult(false));
			});
		}

		public static void EnableInvisiblePushNotifications(AbstractLogger logger, IDatabase database, IMixWebCallFactory mixWebCallFactory, string token, PushNotificationService service, string provisionId, string swid, Action<IEnableInvisiblePushNotificationsResult> callback)
		{
			string tokenType = GetTokenType(service);
			Enable(logger, database, mixWebCallFactory, token, tokenType, provisionId, false, swid, delegate
			{
				callback(new EnableInvisiblePushNotificationsResult(true));
			}, delegate
			{
				callback(new EnableInvisiblePushNotificationsResult(false));
			});
		}

		public static void DisableAllPushNotifications(AbstractLogger logger, IDatabase database, IMixWebCallFactory mixWebCallFactory, string swid, Action<IDisableAllPushNotificationsResult> callback)
		{
			try
			{
				SessionDocument sessionDocument = database.GetSessionDocument(swid);
				if (sessionDocument.PushNotificationToken == null)
				{
					callback(new DisableAllPushNotificationsResult(false));
					return;
				}
				BaseUserRequest request = new BaseUserRequest();
				IWebCall<BaseUserRequest, BaseResponse> webCall = mixWebCallFactory.PushNotificationsSettingDeletePost(request);
				webCall.OnResponse += delegate
				{
					try
					{
						database.UpdateSessionDocument(swid, delegate(SessionDocument d)
						{
							d.PushNotificationToken = null;
							d.PushNotificationTokenType = null;
							d.VisiblePushNotificationsEnabled = false;
						});
						callback(new DisableAllPushNotificationsResult(true));
					}
					catch (Exception ex2)
					{
						logger.Critical("Unhandled exception: " + ex2);
						callback(new DisableAllPushNotificationsResult(false));
					}
				};
				webCall.OnError += delegate
				{
					callback(new DisableAllPushNotificationsResult(false));
				};
				webCall.Execute();
			}
			catch (Exception ex)
			{
				logger.Critical("Unhandled exception: " + ex);
				callback(new DisableAllPushNotificationsResult(false));
			}
		}

		public static void DisableVisiblePushNotifications(AbstractLogger logger, IDatabase database, IMixWebCallFactory mixWebCallFactory, string swid, Action<IDisableVisiblePushNotificationsResult> callback)
		{
			SessionDocument sessionDocument = database.GetSessionDocument(swid);
			if (sessionDocument.PushNotificationToken == null)
			{
				callback(new DisableVisiblePushNotificationsResult(false));
				return;
			}
			Enable(logger, database, mixWebCallFactory, sessionDocument.PushNotificationToken, sessionDocument.PushNotificationTokenType, sessionDocument.ProvisionId, false, swid, delegate
			{
				callback(new DisableVisiblePushNotificationsResult(true));
			}, delegate
			{
				callback(new DisableVisiblePushNotificationsResult(false));
			});
		}

		private static void Enable(AbstractLogger logger, IDatabase database, IMixWebCallFactory mixWebCallFactory, string token, string tokenType, string provisionId, bool enableVisible, string swid, Action successCallback, Action failureCallback)
		{
			try
			{
				TogglePushNotificationRequest togglePushNotificationRequest = new TogglePushNotificationRequest();
				togglePushNotificationRequest.PushToken = new PushToken
				{
					Token = token,
					TokenType = tokenType
				};
				togglePushNotificationRequest.State = ((!enableVisible) ? "ONLY_SILENT" : "ALL");
				togglePushNotificationRequest.IosProvisioningId = provisionId;
				TogglePushNotificationRequest request = togglePushNotificationRequest;
				IWebCall<TogglePushNotificationRequest, BaseResponse> webCall = mixWebCallFactory.PushNotificationsSettingPost(request);
				webCall.OnResponse += delegate
				{
					try
					{
						database.UpdateSessionDocument(swid, delegate(SessionDocument sessionDoc)
						{
							sessionDoc.PushNotificationToken = token;
							sessionDoc.PushNotificationTokenType = tokenType;
							sessionDoc.VisiblePushNotificationsEnabled = enableVisible;
							sessionDoc.ProvisionId = provisionId;
						});
						successCallback();
					}
					catch (Exception ex2)
					{
						logger.Critical("Unhandled exception: " + ex2);
						failureCallback();
					}
				};
				webCall.OnError += delegate
				{
					failureCallback();
				};
				webCall.Execute();
			}
			catch (Exception ex)
			{
				logger.Critical("Unhandled exception: " + ex);
				failureCallback();
			}
		}

		private static string GetTokenType(PushNotificationService service)
		{
			switch (service)
			{
			case PushNotificationService.ApplePushNotificationService:
				return "APNS";
			case PushNotificationService.GoogleCloudMessaging:
				return "GCM";
			case PushNotificationService.Glacier:
				return "GLACIER";
			default:
				throw new ArgumentException("Unsupported push notification service: " + service);
			}
		}
	}
}
