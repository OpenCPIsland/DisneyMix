using System;
using System.Collections.Generic;
using Disney.PushNotificationUnityPlugin.Internal;
using UnityEngine;

namespace Disney.PushNotificationUnityPlugin
{
	// Android push straight from the Glacier server, replacing the dead GCM
	// plugin. The Java side (Plugins/Android/MixPush.androidlib) keeps a
	// connection to {Mix API host}/push/stream open in a background service
	// and shows the notifications; this class hands Unity its token and the
	// payloads of tapped notifications.
	public class GlacierPushClient : IPushNotificationClient
	{
		private enum State
		{
			Idle = 0,
			WaitingForToken = 1,
			WaitingForNotifications = 2
		}

		private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";

		private static readonly AndroidJavaClass MixPushJavaClass = new AndroidJavaClass("com.opencpi.mixpush.MixPush");

		private readonly string mixApiHostUrl;

		private State state;

		// The server has this device for visible pushes; start the service as
		// soon as notifications are allowed.
		private bool listenRequested;

		// Set from Android's permission callback (not the Unity thread),
		// acted on in Update.
		private volatile bool permissionGranted;

		public event EventHandler<AbstractNotificationReceivedEventArgs> OnNotificationReceived = delegate
		{
		};

		public event EventHandler<AbstractTokenGeneratedEventArgs> OnTokenGenerated = delegate
		{
		};

		public GlacierPushClient(string mixApiHostUrl)
		{
			this.mixApiHostUrl = mixApiHostUrl;
			state = State.WaitingForNotifications;
		}

		private static AndroidJavaObject Activity
		{
			get
			{
				using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
				{
					return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				}
			}
		}

		private static int SdkInt
		{
			get
			{
				using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
				{
					return version.GetStatic<int>("SDK_INT");
				}
			}
		}

		public void Register()
		{
			// Android 13+ asks the player before an app may show notifications.
			// The server's reply to the registration usually arrives while this
			// dialog is still open, and Unity gets no pause/resume for it, so
			// the grant itself is what starts the service.
			if (SdkInt >= 33 && !UnityEngine.Android.Permission.HasUserAuthorizedPermission(PostNotificationsPermission))
			{
				UnityEngine.Android.PermissionCallbacks callbacks = new UnityEngine.Android.PermissionCallbacks();
				callbacks.PermissionGranted += delegate
				{
					permissionGranted = true;
				};
				UnityEngine.Android.Permission.RequestUserPermission(PostNotificationsPermission, callbacks);
			}
		}

		public void CheckForToken()
		{
			state = State.WaitingForToken;
		}

		// Logout: stop listening and forget the token, so the next account
		// registers a new one and this one's pushes never reach it.
		public void Unregister()
		{
			StopListening();
			MixPushJavaClass.CallStatic("resetToken", Activity);
			state = State.WaitingForNotifications;
		}

		// Called once the server has the token with visible pushes on.
		public void StartListening()
		{
			listenRequested = true;
			if (string.IsNullOrEmpty(mixApiHostUrl) || !IsNotificationEnabled())
			{
				return;
			}
			MixPushJavaClass.CallStatic("start", Activity, mixApiHostUrl);
		}

		public void StopListening()
		{
			listenRequested = false;
			MixPushJavaClass.CallStatic("stop", Activity);
		}

		public void Update()
		{
			if (permissionGranted)
			{
				permissionGranted = false;
				if (listenRequested)
				{
					StartListening();
				}
			}
			if (state == State.WaitingForToken)
			{
				string token = MixPushJavaClass.CallStatic<string>("getToken", Activity);
				state = State.WaitingForNotifications;
				OnTokenGenerated(this, new TokenGeneratedEventArgs(token));
			}
			AndroidJavaObject activity = Activity;
			while (true)
			{
				string json = MixPushJavaClass.CallStatic<string>("getMessage", activity);
				if (json == null)
				{
					break;
				}
				Dictionary<string, object> userData = Json.Deserialize(json) as Dictionary<string, object>;
				if (userData != null)
				{
					OnNotificationReceived(this, new NotificationReceivedEventArgs(userData));
				}
			}
		}

		public void OnPause()
		{
			MixPushJavaClass.CallStatic("setAppVisible", Activity, false);
		}

		public void OnResume()
		{
			MixPushJavaClass.CallStatic("setAppVisible", Activity, true);
		}

		public bool IsNotificationEnabled()
		{
			return MixPushJavaClass.CallStatic<bool>("areNotificationsEnabled", Activity);
		}
	}
}
