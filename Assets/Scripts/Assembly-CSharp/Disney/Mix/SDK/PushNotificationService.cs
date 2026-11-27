namespace Disney.Mix.SDK
{
	public enum PushNotificationService
	{
		GoogleCloudMessaging = 0,
		ApplePushNotificationService = 1,
		// Pushed by the Glacier server itself (Android, GlacierPushClient).
		Glacier = 2
	}
}
