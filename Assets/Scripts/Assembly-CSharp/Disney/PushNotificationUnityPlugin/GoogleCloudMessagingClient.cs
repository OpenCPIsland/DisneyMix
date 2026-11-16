using System;
using System.Collections.Generic;
using Disney.PushNotificationUnityPlugin.Internal;
using UnityEngine;

namespace Disney.PushNotificationUnityPlugin
{
	public class GoogleCloudMessagingClient : IPushNotificationClient
	{
		private enum State
		{
			Idle = 0,
			WaitingForToken = 1,
			WaitingForNotifications = 2
		}

		private State state;

		private readonly string senderId;

		private static readonly AndroidJavaClass UnityApiJavaClass = new AndroidJavaClass("com.disney.gcmunityplugin.UnityApi");

		public event EventHandler<AbstractNotificationReceivedEventArgs> OnNotificationReceived = delegate
		{
		};

		public event EventHandler<AbstractTokenGeneratedEventArgs> OnTokenGenerated = delegate
		{
		};

		public GoogleCloudMessagingClient(string senderId)
		{
			this.senderId = senderId;
			state = State.WaitingForNotifications;
		}

		public void Register()
		{
			UnityApiJavaClass.CallStatic("register", senderId.ToString());
		}

		public void CheckForToken()
		{
			state = State.WaitingForToken;
		}

		public void Unregister()
		{
			state = State.Idle;
			UnityApiJavaClass.CallStatic("unregister");
		}

		public void Update()
		{
			switch (state)
			{
			default:
				return;
			case State.WaitingForToken:
			{
				string text = UnityApiJavaClass.CallStatic<string>("getToken", new object[0]);
				if (text != null)
				{
					state = State.WaitingForNotifications;
					this.OnTokenGenerated(this, new TokenGeneratedEventArgs(text));
				}
				return;
			}
			case State.WaitingForNotifications:
				break;
			}
			while (true)
			{
				string text2 = UnityApiJavaClass.CallStatic<string>("getMessage", new object[0]);
				if (text2 == null)
				{
					break;
				}
				Dictionary<string, object> userData = Json.Deserialize(text2) as Dictionary<string, object>;
				NotificationReceivedEventArgs e = new NotificationReceivedEventArgs(userData);
				this.OnNotificationReceived(this, e);
			}
		}

		public void OnPause()
		{
			UnityApiJavaClass.CallStatic("onPause");
		}

		public void OnResume()
		{
			UnityApiJavaClass.CallStatic("onResume");
		}

		public bool IsNotificationEnabled()
		{
			return UnityApiJavaClass.CallStatic<bool>("isNotificationEnabled", new object[0]);
		}
	}
}
