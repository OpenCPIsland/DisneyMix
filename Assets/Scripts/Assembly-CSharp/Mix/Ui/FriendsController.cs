using System;
using System.Linq;
using Disney.Mix.SDK;
using Mix.DeviceDb;
using Mix.Localization;
using Mix.Session;
using Mix.Tracking;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class FriendsController : BaseController
	{
		public Text TitleText;

		public FriendsActionBar ActionBar;

		public NotificationBar NotificationBar;

		public ScrollView ScrollView;

		public GameObject TouchTooltip;

		public GameObject CategoryPrefab;

		public GameObject InvitePrefab;

		public GameObject FriendItemPrefab;

		private SdkEvents eventGenerator = new SdkEvents();

		private FriendsInviteCategoryManager inviteManager;

		private FriendsUsersCategoryManager usersManager;

		private FriendsRecommendedCategoryManager recommendedManager;

		private void Start()
		{
			inviteManager = new FriendsInviteCategoryManager(Singleton<Localizer>.Instance.getString("customtokens.friends.invites_list_header"), CategoryPrefab, InvitePrefab, ScrollView);
			usersManager = new FriendsUsersCategoryManager(Singleton<Localizer>.Instance.getString("customtokens.friends.friends_list_header"), CategoryPrefab, FriendItemPrefab, ScrollView);
			recommendedManager = new FriendsRecommendedCategoryManager(Singleton<Localizer>.Instance.getString("customtokens.friends.friends_reccomendations_list_header"), CategoryPrefab, FriendItemPrefab, ScrollView);
		}

		private void Update()
		{
			Singleton<TechAnalytics>.Instance.TrackFPSOnFriends();
		}

		private void FixedUpdate()
		{
			if (usersManager != null)
			{
				usersManager.FixedUpdate();
			}
		}

		private void OnDestroy()
		{
			ActionBar.OnPanelOpened -= OnPanelOpened;
			ActionBar.OnPanelClosed -= OnPanelClosed;
			ActionBar.OnFriendInvited -= OnFriendInvited;
			recommendedManager.OnFriendInvited -= OnFriendInvited;
			inviteManager.OnInviteAccepted -= OnInviteAccepted;
			usersManager.OnTrustInviteSent -= OnTrustInviteSent;
			usersManager.OnHeaderUpdated -= OnFriendsCountUpdated;
			if (MixSession.User == null)
			{
				return;
			}
			MixSession.User.OnReceivedIncomingFriendInvitation -= eventGenerator.GetEventHandler<AbstractReceivedIncomingFriendInvitationEventArgs>(this, OnReceivedIncomingFriendInvitation);
			MixSession.User.OnReceivedOutgoingFriendInvitation -= eventGenerator.GetEventHandler<AbstractReceivedOutgoingFriendInvitationEventArgs>(this, OnReceivedOutgoingFriendInvitation);
			MixSession.User.OnUnfriended -= eventGenerator.GetEventHandler<AbstractUnfriendedEventArgs>(null, OnUnfriended);
			MixSession.OnConnectionChanged -= HandleConnectionChange;
			foreach (IOutgoingFriendInvitation outgoingFriendInvitation in MixSession.User.OutgoingFriendInvitations)
			{
				outgoingFriendInvitation.OnAccepted -= eventGenerator.GetEventHandler<AbstractFriendInvitationAcceptedEventArgs>(outgoingFriendInvitation, OnFriendInvitationAccepted);
			}
		}

		public override void OnAndroidBackButtonClicked()
		{
			if (!Singleton<PanelManager>.Instance.OnAndroidBackButton())
			{
				NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Conversations/ConversationsScreen", new TransitionNone());
				MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest);
			}
		}

		public override void OnDataReceived(string aToken, object aData)
		{
			if (aToken == "qrCode")
			{
				ActionBar.PreOpenQRCodePanel();
			}
		}

		public override void OnUITransitionEnd()
		{
			ActionBar.Init(usersManager, inviteManager);
			ActionBar.OnPanelOpened += OnPanelOpened;
			ActionBar.OnPanelClosed += OnPanelClosed;
			ActionBar.OnFriendInvited += OnFriendInvited;
			recommendedManager.OnFriendInvited += OnFriendInvited;
			inviteManager.OnInviteAccepted += OnInviteAccepted;
			usersManager.OnTrustInviteSent += OnTrustInviteSent;
			usersManager.OnHeaderUpdated += OnFriendsCountUpdated;
			bool touchPressureSupported = false;
			foreach (UnityEngine.InputSystem.EnhancedTouch.Touch touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
			{
				if (touch.pressure > 0f)
				{
					touchPressureSupported = true;
					break;
				}
			}
			if (touchPressureSupported && MixSession.User.Friends.Count() > 1)
			{
				IKeyValDatabaseApi keyValDocumentCollectionApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
				if (!keyValDocumentCollectionApi.LoadUserValueAsBool("3dtouch.tooltip.seen.convo"))
				{
					TouchTooltip.SetActive(true);
					keyValDocumentCollectionApi.SaveUserValueFromBool("3dtouch.tooltip.seen.convo", true);
				}
			}
			MixSession.User.OnReceivedIncomingFriendInvitation += eventGenerator.AddEventHandler<AbstractReceivedIncomingFriendInvitationEventArgs>(this, OnReceivedIncomingFriendInvitation);
			MixSession.User.OnReceivedOutgoingFriendInvitation += eventGenerator.AddEventHandler<AbstractReceivedOutgoingFriendInvitationEventArgs>(this, OnReceivedOutgoingFriendInvitation);
			MixSession.User.OnUnfriended += eventGenerator.AddEventHandler<AbstractUnfriendedEventArgs>(null, OnUnfriended);
			MixSession.OnConnectionChanged += HandleConnectionChange;
			foreach (IOutgoingFriendInvitation outgoingFriendInvitation in MixSession.User.OutgoingFriendInvitations)
			{
				outgoingFriendInvitation.OnAccepted += eventGenerator.AddEventHandler<AbstractFriendInvitationAcceptedEventArgs>(outgoingFriendInvitation, OnFriendInvitationAccepted);
			}
			SetupScrollView();
			Analytics.LogFriendsListPageView();
		}

		private void OnPanelOpened(object sender, EventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				ScrollView.Hide();
			}
		}

		private void OnPanelClosed(object sender, EventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				ScrollView.Show();
				usersManager.Refresh();
			}
		}

		private void OnTrustInviteSent(object sender, EventArgs arugments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				((IOutgoingFriendInvitation)sender).OnAccepted += eventGenerator.AddEventHandler<AbstractFriendInvitationAcceptedEventArgs>(sender, OnFriendInvitationAccepted);
			}
		}

		private void OnFriendsCountUpdated(object sender, EventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				TitleText.text = Singleton<Localizer>.Instance.getString("customtokens.friends.header_title") + " (" + usersManager.GetCount() + ")";
			}
		}

		public void OnSettingsButtonClicked()
		{
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Settings/SettingsScreen", new TransitionAnimations());
			navigationRequest.AddData("fromScene", "Prefabs/Screens/Friends/FriendsScreen");
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void HandleConnectionChange(MixSession.ConnectionState newState, MixSession.ConnectionState oldState)
		{
			if (newState == MixSession.ConnectionState.ONLINE && !this.IsNullOrDisposed())
			{
				SetupScrollView();
			}
		}

		private void OnReceivedIncomingFriendInvitation(object sender, AbstractReceivedIncomingFriendInvitationEventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed() && !(MixSession.User.Id != arguments.Invitation.Invitee.Id))
			{
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
				inviteManager.CreateItem(arguments.Invitation);
				recommendedManager.RemoveItem(arguments.Invitation.Inviter.DisplayName.Text);
			}
		}

		private void OnReceivedOutgoingFriendInvitation(object sender, AbstractReceivedOutgoingFriendInvitationEventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed() && !(MixSession.User.Id != arguments.Invitation.Inviter.Id))
			{
				arguments.Invitation.OnAccepted += eventGenerator.AddEventHandler<AbstractFriendInvitationAcceptedEventArgs>(arguments.Invitation, OnFriendInvitationAccepted);
				recommendedManager.RemoveItem(arguments.Invitation.Invitee.DisplayName.Text);
			}
		}

		private void OnUnfriended(object sender, AbstractUnfriendedEventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				usersManager.RemoveItem(arguments.ExFriend);
			}
		}

		private void OnFriendInvitationAccepted(object sender, AbstractFriendInvitationAcceptedEventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				usersManager.CreateItem(arguments.Friend);
				recommendedManager.RemoveItem(arguments.Friend.DisplayName.Text, true);
			}
		}

		private void OnFriendInvited(object sender, FriendInvitedEventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				NotificationBar.ShowFromString(Singleton<Localizer>.Instance.getString("customtokens.friends.friends_added").Replace("#name#", arguments.OutgoingInvite.Invitee.DisplayName.Text));
				arguments.OutgoingInvite.OnAccepted += eventGenerator.AddEventHandler<AbstractFriendInvitationAcceptedEventArgs>(arguments.OutgoingInvite, OnFriendInvitationAccepted);
				recommendedManager.RemoveItem(arguments.OutgoingInvite.Invitee.DisplayName.Text);
			}
		}

		private void OnInviteAccepted(object sender, InviteAcceptedEventArgs arguments)
		{
			if (MixSession.IsValidSession && !this.IsNullOrDisposed())
			{
				usersManager.CreateItem(arguments.Friend);
				recommendedManager.RemoveItem(arguments.Friend.DisplayName.Text, true);
			}
		}

		private void SetupScrollView()
		{
			inviteManager.Refresh();
			usersManager.Refresh();
			recommendedManager.Refresh();
		}
	}
}
