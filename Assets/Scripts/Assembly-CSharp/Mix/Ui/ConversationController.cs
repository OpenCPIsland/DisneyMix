using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avatar;
using Disney.Mix.SDK;
using Mix.Avatar;
using Mix.DeviceDb;
using Mix.Session;
using Mix.Session.Extensions;
using Mix.Tracking;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class ConversationController : BaseController
	{
		public class ListUpdateItem
		{
			public IChatThread thread;

			public IChatMessage message;

			public Coroutine outgoingUpdate;

			public bool updatePending;
		}

		private const float listUpdateDelay = 1f;

		public ScrollView ScrollView;

		public GameObject SingleChatItem;

		public GameObject GroupChatItem;

		public GameObject OfficialAccountItemPrefab;

		public GameObject OfficialAccountsScrollerPrefab;

		public Button StartChatButton;

		public GameObject ChatNotificationBadge;

		public GameObject FriendsNotificationBadge;

		public GameObject NoChatItems;

		public AvatarObjectSpawner AvatarSpawner;

		public Animator FriendsButtonAnimator;

		public GameObject BackgroundScreen;

		public GameObject TouchTooltip;

		private SdkEvents eventGenerator = new SdkEvents();

		private bool updateBadgeCount;

		private float updateBadgeCooldown;

		private bool startConversation;

		private OfficialAccountsScroller officialAccountsScroller;

		private bool officialAccountsLoaded;

		private Dictionary<IChatThread, ListUpdateItem> listUpdater = new Dictionary<IChatThread, ListUpdateItem>();

		private Dictionary<long, SingleChatItem> scrollListItems = new Dictionary<long, SingleChatItem>();

		private void Start()
		{
			Singleton<TechAnalytics>.Instance.TrackTimeToConversations();
			GameObject noChatItemsAvatar = AvatarSpawner.Init();
			noChatItemsAvatar.SetActive(false);
			NoChatItems.SetActive(false);
			MonoSingleton<AvatarManager>.Instance.SkinAvatar(noChatItemsAvatar, MixSession.User.Avatar, (AvatarFlags)0, delegate
			{
				if (!this.IsNullOrDisposed() && noChatItemsAvatar != null)
				{
					noChatItemsAvatar.transform.Rotate(new Vector3(0f, 15f, 0f));
					noChatItemsAvatar.SetActive(true);
				}
			});
			MonoSingleton<FakeFriendManager>.Instance.CheckTimeBasedMessages();
			StartChatButton.gameObject.SetActive(MixSession.User.Friends.Count() > 0);
			officialAccountsScroller = new OfficialAccountsScroller(OfficialAccountsScrollerPrefab, OfficialAccountItemPrefab, ToggleScroll);
			officialAccountsScroller.LoadOfficialAccounts(delegate(bool didLoad)
			{
				officialAccountsLoaded = didLoad;
				if (didLoad && ScrollView.Get(officialAccountsScroller) == null)
				{
					ScrollView.Add(officialAccountsScroller, true);
				}
				else
				{
					officialAccountsScroller = null;
				}
			});
		}

		private void FixedUpdate()
		{
			if (scrollListItems == null || scrollListItems.Count <= 0)
			{
				return;
			}
			foreach (KeyValuePair<long, SingleChatItem> scrollListItem in scrollListItems)
			{
				if (scrollListItem.Value != null)
				{
					scrollListItem.Value.FixedUpdate();
				}
			}
		}

		public override void OnUITransitionEnd()
		{
			MixFriends.OnBadgesChanged += HandleOnRefreshInvitations;
			MixSession.OnConnectionChanged += HandleConnectionChanged;
			MixSession.OnGotNewMessages += OnGotNewMessages;
			BuildFriendListAndEvents();
			UpdateBadges();
			ScrollView scrollView = ScrollView;
			scrollView.OnPointerDownDelegates = (ScrollView.OnPointerDownDelegate)Delegate.Combine(scrollView.OnPointerDownDelegates, new ScrollView.OnPointerDownDelegate(OnScrollGotPointerDown));
			if (!MonoSingleton<PushNotifications>.Instance.HaveShownPushPrePopup)
			{
				MonoSingleton<PushNotifications>.Instance.HaveShownPushPrePopup = true;
				MonoSingleton<PushNotifications>.Instance.ToggleRegister(true, true);
				PreMessagePanel preMessagePanel = (PreMessagePanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.PUSH_SETTING_MESSAGE);
				preMessagePanel.Setup(true);
				Analytics.LogShowPushNotificationPopup();
			}
			if (startConversation)
			{
				OnMessageButtonClicked();
			}
			if (Input.touchPressureSupported && MixSession.User.Friends.Count() > 1)
			{
				IKeyValDatabaseApi keyValDocumentCollectionApi = Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi;
				if (!keyValDocumentCollectionApi.LoadUserValueAsBool("3dtouch.tooltip.seen.convo"))
				{
					TouchTooltip.SetActive(true);
					keyValDocumentCollectionApi.SaveUserValueFromBool("3dtouch.tooltip.seen.convo", true);
				}
			}
		}

		private void BuildFriendListAndEvents()
		{
			if (!MixSession.IsValidSession)
			{
				return;
			}
			MixSession.User.OnAddedToOneOnOneChatThread += eventGenerator.AddEventHandler<AbstractAddedToOneOnOneChatThreadEventArgs>(this, OnAddedToOneOnOneThread);
			MixSession.User.OnAddedToOfficialAccountChatThread += eventGenerator.AddEventHandler<AbstractAddedToOfficialAccountThreadEventArgs>(this, OnAddedToOfficialAccountThread);
			MixSession.User.OnAddedToGroupChatThread += eventGenerator.AddEventHandler<AbstractAddedToGroupChatThreadEventArgs>(this, OnAddedToGroupThread);
			foreach (IChatThread item in MixSession.User.ChatThreadsFromUsers())
			{
				AddThreadListeners(item);
			}
			LoadCurrentMessages();
			OrderChatMessages();
		}

		private void ClearList()
		{
			if (MixSession.Session != null && MixSession.IsValidSession)
			{
				foreach (IChatThread item in MixSession.User.ChatThreadsFromUsers())
				{
					RemoveThreadListeners(item);
				}
				MixSession.User.OnAddedToOneOnOneChatThread -= eventGenerator.GetEventHandler<AbstractAddedToOneOnOneChatThreadEventArgs>(this, OnAddedToOneOnOneThread);
				MixSession.User.OnAddedToGroupChatThread -= eventGenerator.GetEventHandler<AbstractAddedToGroupChatThreadEventArgs>(this, OnAddedToGroupThread);
				MixSession.User.OnAddedToOfficialAccountChatThread -= eventGenerator.GetEventHandler<AbstractAddedToOfficialAccountThreadEventArgs>(this, OnAddedToOfficialAccountThread);
			}
			if (ScrollView != null)
			{
				ScrollView scrollView = ScrollView;
				scrollView.OnPointerDownDelegates = (ScrollView.OnPointerDownDelegate)Delegate.Remove(scrollView.OnPointerDownDelegates, new ScrollView.OnPointerDownDelegate(OnScrollGotPointerDown));
				ScrollView.Clear();
			}
			scrollListItems.Clear();
		}

		private void HandleConnectionChanged(MixSession.ConnectionState newState, MixSession.ConnectionState oldState)
		{
			HandleOnRefreshInvitations();
		}

		private void OnGotNewMessages(bool success, bool newMessages)
		{
			if (!this.IsNullOrDisposed() && !ScrollView.IsNullOrDisposed() && newMessages)
			{
				ScrollView.Clear();
				scrollListItems.Clear();
				LoadCurrentMessages();
				OrderChatMessages();
				updateBadgeCount = true;
			}
		}

		private void OnAddedToOneOnOneThread(object sender, AbstractAddedToOneOnOneChatThreadEventArgs e)
		{
			OnAddedToThread(e.ChatThread);
		}

		private void OnAddedToOfficialAccountThread(object sender, AbstractAddedToOfficialAccountThreadEventArgs e)
		{
			if (!e.ChatThread.OfficialAccount.AccountId.Equals(FakeFriendManager.OAID))
			{
				OnAddedToThread(e.ChatThread);
			}
		}

		private void OnAddedToGroupThread(object sender, AbstractAddedToGroupChatThreadEventArgs e)
		{
			OnAddedToThread(e.ChatThread);
		}

		private void OnAddedToThread(IChatThread chatThread)
		{
			AddThreadListeners(chatThread);
		}

		private void AddThreadListeners(IChatThread aChatThread)
		{
			if (aChatThread is IOneOnOneChatThread)
			{
				((IOneOnOneChatThread)aChatThread).OnGagMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadGagMessageAddedEventArgs>(aChatThread, OnGagMessageReceived);
			}
			if (aChatThread is IGroupChatThread)
			{
				((IGroupChatThread)aChatThread).OnGagMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadGagMessageAddedEventArgs>(aChatThread, OnGagMessageReceived);
			}
			aChatThread.OnMemberAdded += eventGenerator.AddEventHandler<AbstractChatThreadMemberAddedEventArgs>(aChatThread, OnThreadMemberAdded);
			aChatThread.OnMemberRemoved += eventGenerator.AddEventHandler<AbstractChatThreadMemberRemovedEventArgs>(aChatThread, OnThreadMemberRemoved);
			aChatThread.OnTextMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadTextMessageAddedEventArgs>(aChatThread, OnTextMessageReceived);
			aChatThread.OnStickerMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadStickerMessageAddedEventArgs>(aChatThread, OnStickerMessageReceived);
			aChatThread.OnMemberAddedMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadMemberAddedMessageAddedEventArgs>(aChatThread, OnMemberAddedMessageReceived);
			aChatThread.OnMemberRemovedMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadMemberRemovedMessageAddedEventArgs>(aChatThread, OnMemberRemovedMessageReceived);
			aChatThread.OnPhotoMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadPhotoMessageAddedEventArgs>(aChatThread, OnPhotoMessageReceived);
			aChatThread.OnVideoMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadVideoMessageAddedEventArgs>(aChatThread, OnVideoMessageReceived);
			aChatThread.OnUnreadMessageCountChanged += eventGenerator.AddEventHandler<AbstractUnreadMessageCountChangedEventArgs>(aChatThread, UpdateUnreadMessageCount);
			aChatThread.OnGameStateMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadGameStateMessageAddedEventArgs>(aChatThread, OnGameStateMessageReceived);
			aChatThread.OnGameEventMessageAdded += eventGenerator.AddEventHandler<AbstractChatThreadGameEventMessageAddedEventArgs>(aChatThread, OnGameEventMessageReceived);
			if (aChatThread is IOneOnOneChatThread)
			{
				(aChatThread as IOneOnOneChatThread).OnChatHistoryCleared += eventGenerator.AddEventHandler<AbstractChatHistoryClearedEventArgs>(aChatThread, OnChatHistoryCleared);
			}
		}

		private void RemoveThreadListeners(IChatThread aChatThread)
		{
			if (aChatThread is IOneOnOneChatThread)
			{
				((IOneOnOneChatThread)aChatThread).OnGagMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadGagMessageAddedEventArgs>(aChatThread, OnGagMessageReceived);
			}
			if (aChatThread is IGroupChatThread)
			{
				((IGroupChatThread)aChatThread).OnGagMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadGagMessageAddedEventArgs>(aChatThread, OnGagMessageReceived);
			}
			aChatThread.OnMemberAdded -= eventGenerator.GetEventHandler<AbstractChatThreadMemberAddedEventArgs>(aChatThread, OnThreadMemberAdded);
			aChatThread.OnMemberRemoved -= eventGenerator.GetEventHandler<AbstractChatThreadMemberRemovedEventArgs>(aChatThread, OnThreadMemberRemoved);
			aChatThread.OnTextMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadTextMessageAddedEventArgs>(aChatThread, OnTextMessageReceived);
			aChatThread.OnStickerMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadStickerMessageAddedEventArgs>(aChatThread, OnStickerMessageReceived);
			aChatThread.OnMemberAddedMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadMemberAddedMessageAddedEventArgs>(aChatThread, OnMemberAddedMessageReceived);
			aChatThread.OnMemberRemovedMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadMemberRemovedMessageAddedEventArgs>(aChatThread, OnMemberRemovedMessageReceived);
			aChatThread.OnPhotoMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadPhotoMessageAddedEventArgs>(aChatThread, OnPhotoMessageReceived);
			aChatThread.OnVideoMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadVideoMessageAddedEventArgs>(aChatThread, OnVideoMessageReceived);
			aChatThread.OnUnreadMessageCountChanged -= eventGenerator.GetEventHandler<AbstractUnreadMessageCountChangedEventArgs>(aChatThread, UpdateUnreadMessageCount);
			aChatThread.OnGameStateMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadGameStateMessageAddedEventArgs>(aChatThread, OnGameStateMessageReceived);
			aChatThread.OnGameEventMessageAdded -= eventGenerator.GetEventHandler<AbstractChatThreadGameEventMessageAddedEventArgs>(aChatThread, OnGameEventMessageReceived);
			if (aChatThread is IOneOnOneChatThread)
			{
				(aChatThread as IOneOnOneChatThread).OnChatHistoryCleared -= eventGenerator.GetEventHandler<AbstractChatHistoryClearedEventArgs>(aChatThread, OnChatHistoryCleared);
			}
		}

		private void OnTextMessageReceived(object sender, AbstractChatThreadTextMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnStickerMessageReceived(object sender, AbstractChatThreadStickerMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnGagMessageReceived(object sender, AbstractChatThreadGagMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnMemberAddedMessageReceived(object sender, AbstractChatThreadMemberAddedMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnMemberRemovedMessageReceived(object sender, AbstractChatThreadMemberRemovedMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnGameStateMessageReceived(object sender, AbstractChatThreadGameStateMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnGameEventMessageReceived(object sender, AbstractChatThreadGameEventMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnPhotoMessageReceived(object sender, AbstractChatThreadPhotoMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnVideoMessageReceived(object sender, AbstractChatThreadVideoMessageAddedEventArgs e)
		{
			OnMessageReceived((IChatThread)sender, e.Message);
		}

		private void OnChatHistoryCleared(object sender, AbstractChatHistoryClearedEventArgs e)
		{
			SingleChatItem singleChatItem = null;
			foreach (KeyValuePair<long, SingleChatItem> scrollListItem in scrollListItems)
			{
				if (scrollListItem.Value.thread.Equals((IChatThread)sender))
				{
					singleChatItem = scrollListItem.Value;
					break;
				}
			}
			if (singleChatItem != null)
			{
				DeleteScrollViewItem(singleChatItem);
			}
		}

		private void OnMessageReceived(IChatThread aChatThread, IChatMessage aMessage)
		{
			updateBadgeCount = true;
			updateMessageItem(aChatThread, aMessage);
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/RedNumberMessageReceive", this);
		}

		private void UpdateUnreadMessageCount(object sender, AbstractUnreadMessageCountChangedEventArgs args)
		{
			IChatThread obj = (IChatThread)sender;
			foreach (KeyValuePair<long, SingleChatItem> scrollListItem in scrollListItems)
			{
				if (scrollListItem.Value.thread.Equals(obj))
				{
					scrollListItem.Value.UpdateUnreadCount();
					break;
				}
			}
			updateBadgeCount = true;
		}

		private void HandleOnRefreshInvitations()
		{
			updateBadgeCount = true;
		}

		public override void OnUILoaded(NavigationRequest aNavigationRequest = null)
		{
			updateBadgeCount = true;
			Analytics.LogChatHistoryPageView();
		}

		public override void OnDataReceived(string aToken, object aData)
		{
			if (aToken == "startConversation")
			{
				startConversation = true;
			}
		}

		public override void OnAndroidBackButtonClicked()
		{
			if (!Singleton<PanelManager>.Instance.OnAndroidBackButton())
			{
				GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
				Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
				{
					{ genericPanel.TitleText, "customtokens.panels.leaving_app" },
					{ genericPanel.MessageText, null },
					{ genericPanel.ButtonOneText, "customtokens.login.logout_yes" },
					{ genericPanel.ButtonTwoText, "customtokens.login.logout_no" }
				});
				Singleton<PanelManager>.Instance.SetupButtons(new Dictionary<Button, Action> { 
				{
					genericPanel.ButtonOne,
					Application.Quit
				} });
			}
		}

		private void Update()
		{
			Singleton<TechAnalytics>.Instance.TrackFPSOnConvo();
			MonoSingleton<FakeFriendManager>.Instance.HighlightAnimator(FakeFriendManager.TYPE_FRIEND_BUTTON, FriendsButtonAnimator);
			if (MixSession.IsValidSession)
			{
				StartChatButton.gameObject.SetActive(MixSession.User.Friends.Count() > 0);
				SetNoChatItems();
			}
			if (updateBadgeCooldown > 0f)
			{
				updateBadgeCooldown -= Time.deltaTime;
			}
			if (updateBadgeCount && updateBadgeCooldown <= 0f)
			{
				UpdateBadges();
				updateBadgeCount = false;
				updateBadgeCooldown = 1f;
			}
		}

		private void OnDestroy()
		{
			MixFriends.OnBadgesChanged -= HandleOnRefreshInvitations;
			MixSession.OnConnectionChanged -= HandleConnectionChanged;
			MixSession.OnGotNewMessages -= OnGotNewMessages;
			ClearList();
		}

		public void OnTransitionOutBegin()
		{
		}

		public void OnMessageButtonClicked()
		{
			if (MixSession.ParentalConsentRequired)
			{
				ParentalConsent.ShowGateDialog();
				return;
			}
			Analytics.LogStartChatButton();
			Analytics.LogNavigationAction("history", "friend_list");
			FriendSelectorPanel friendSelectorPanel = (FriendSelectorPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.FRIEND_SELECTOR);
			friendSelectorPanel.Init();
			friendSelectorPanel.PanelOpenedEvent += OnFriendSelectorPanelOpened;
		}

		public void OnFriendSelectorPanelOpened(BasePanel panel)
		{
			if (BackgroundScreen != null)
			{
				BackgroundScreen.SetActive(false);
			}
			if (panel != null)
			{
				panel.PanelClosingEvent += OnFriendSelectorPanelClosing;
			}
			if (ScrollView != null)
			{
				ScrollView.Hide();
			}
		}

		public void OnFriendSelectorPanelClosing(BasePanel panel)
		{
			if (BackgroundScreen != null)
			{
				BackgroundScreen.SetActive(true);
			}
			if (panel != null)
			{
				panel.PanelClosingEvent -= OnFriendSelectorPanelClosing;
				panel.PanelOpenedEvent -= OnFriendSelectorPanelOpened;
			}
			if (ScrollView != null)
			{
				ScrollView.Show();
			}
		}

		public void OnFriendsButtonClicked()
		{
			if (MixSession.ParentalConsentRequired)
			{
				ParentalConsent.ShowGateDialog();
				return;
			}
			Analytics.LogNavigationAction("history", "friend_list");
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Friends/FriendsScreen", new TransitionNone());
			if (base.transform.Find("UI_FG_Holder/UI_FG_ConversationsScreen/BottomNav/FriendsBtn").GetComponent<PressureSensitiveButton>().HardPress)
			{
				navigationRequest.AddData("qrCode", true);
			}
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void OnScrollGotPointerDown(PointerEventData eventData)
		{
		}

		public void OnSettingsButtonClicked()
		{
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Settings/SettingsScreen", new TransitionAnimations());
			navigationRequest.AddData("fromScene", "Prefabs/Screens/Conversations/ConversationsScreen");
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void OnProfileButtonClicked()
		{
			NavigationRequest navigationRequest = null;
			if (base.transform.Find("UI_FG_Holder/UI_FG_ConversationsScreen/BottomNav/ProfileBtn").GetComponent<PressureSensitiveButton>().HardPress)
			{
				navigationRequest = new NavigationRequest("Prefabs/Screens/AvatarEditor/AvatarEditorScreen", new TransitionAnimations());
				navigationRequest.AddData("mode", AvatarEditorController.EDITOR_MODES.EDITOR);
			}
			else
			{
				navigationRequest = new NavigationRequest("Prefabs/Screens/Profile/ProfileScreen", new TransitionNone());
			}
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		public void OrderChatMessages()
		{
			List<SingleChatItem> list = new List<SingleChatItem>();
			list.AddRange(scrollListItems.Values);
			list.Sort(delegate(SingleChatItem first, SingleChatItem second)
			{
				if (first.message != null && second.message != null)
				{
					return first.message.TimeSent.CompareTo(second.message.TimeSent);
				}
				return (first.message != null || second.message != null) ? (int.MaxValue * ((first.message != null) ? 1 : (-1))) : 0;
			});
			ScrollView.Clear();
			scrollListItems.Clear();
			if (officialAccountsScroller != null && officialAccountsLoaded && ScrollView.Get(officialAccountsScroller) == null)
			{
				ScrollView.Add(officialAccountsScroller, true);
			}
			if (list.Count > 0)
			{
				ScrollView.SetStaticHeight(list[0]);
			}
			for (int num = list.Count - 1; num >= 0; num--)
			{
				long key = ScrollView.Add(list[num], false);
				scrollListItems.Add(key, list[num]);
			}
		}

		private void LoadCurrentMessages()
		{
			foreach (IChatThread item in MixSession.User.ChatThreadsFromUsers())
			{
				if (!(item is IOfficialAccountChatThread) && item.ChatMessages.Count() > 0)
				{
					AddOrUpdateChatMessage(item, item.ChatMessages.LastOrDefault(), false);
				}
			}
		}

		public void OnThreadMemberAdded(object s, AbstractChatThreadMemberAddedEventArgs args)
		{
			OnChatThreadTitleUpdate((IChatThread)s);
		}

		public void OnThreadMemberRemoved(object s, AbstractChatThreadMemberRemovedEventArgs args)
		{
			OnChatThreadTitleUpdate((IChatThread)s);
		}

		public void OnChatThreadTitleUpdate(IChatThread thread)
		{
			foreach (KeyValuePair<long, SingleChatItem> scrollListItem in scrollListItems)
			{
				if (scrollListItem.Value.thread.Id == thread.Id)
				{
					scrollListItem.Value.ResetItemTitle();
					break;
				}
			}
		}

		public void DeleteScrollViewItem(SingleChatItem item)
		{
			foreach (KeyValuePair<long, SingleChatItem> scrollListItem in scrollListItems)
			{
				if (scrollListItem.Value == item)
				{
					ScrollView.Remove(scrollListItem.Key);
					scrollListItems.Remove(scrollListItem.Key);
					updateBadgeCount = true;
					break;
				}
			}
		}

		private void ToggleScroll(bool aState)
		{
			ScrollView.GetComponent<ScrollRect>().vertical = aState;
		}

		private void UpdateBadges()
		{
			uint totalDisplayableUnreadMessageCount = MixChat.GetTotalDisplayableUnreadMessageCount();
			if (totalDisplayableUnreadMessageCount != 0)
			{
				ChatNotificationBadge.SetActive(true);
				ChatNotificationBadge.transform.Find("NotificationCounterText").GetComponent<Text>().text = totalDisplayableUnreadMessageCount.ToString();
			}
			else
			{
				ChatNotificationBadge.SetActive(false);
			}
			int maxDisplayableFriendInvites = MixFriends.GetMaxDisplayableFriendInvites();
			if (maxDisplayableFriendInvites > 0)
			{
				FriendsNotificationBadge.SetActive(true);
				FriendsNotificationBadge.transform.Find("NotificationCounterText").GetComponent<Text>().text = maxDisplayableFriendInvites.ToString();
			}
			else
			{
				FriendsNotificationBadge.SetActive(false);
			}
		}

		private void updateMessageItem(IChatThread aChatThread, IChatMessage aMessage)
		{
			if (listUpdater.ContainsKey(aChatThread))
			{
				if (listUpdater[aChatThread].updatePending)
				{
					listUpdater[aChatThread].message = aMessage;
					return;
				}
				listUpdater[aChatThread].message = aMessage;
				listUpdater[aChatThread].updatePending = true;
			}
			else
			{
				ListUpdateItem listUpdateItem = new ListUpdateItem();
				listUpdateItem.thread = aChatThread;
				listUpdateItem.message = aMessage;
				listUpdateItem.updatePending = false;
				listUpdateItem.outgoingUpdate = StartCoroutine(IterateOnListQueue(listUpdateItem));
				listUpdater.Add(aChatThread, listUpdateItem);
			}
		}

		private IEnumerator IterateOnListQueue(ListUpdateItem updateItem)
		{
			yield return new WaitForEndOfFrame();
			if (this.IsNullOrDisposed() || updateItem == null)
			{
				yield break;
			}
			AddOrUpdateChatMessage(updateItem.thread, updateItem.message);
			yield return new WaitForSeconds(1f);
			if (!this.IsNullOrDisposed() && updateItem != null)
			{
				if (updateItem.updatePending)
				{
					updateItem.updatePending = false;
					updateItem.outgoingUpdate = StartCoroutine(IterateOnListQueue(updateItem));
				}
				else
				{
					listUpdater.Remove(updateItem.thread);
				}
			}
		}

		private void AddOrUpdateChatMessage(IChatThread aChatThread, IChatMessage aMessage, bool addToScrollView = true)
		{
			if (aChatThread is IOfficialAccountChatThread && ((IOfficialAccountChatThread)aChatThread).OfficialAccount.AccountId.Equals(FakeFriendManager.OAID))
			{
				return;
			}
			if (aChatThread is IOfficialAccountChatThread)
			{
				officialAccountsScroller.RefreshScroller(aChatThread, aMessage);
				return;
			}
			foreach (KeyValuePair<long, SingleChatItem> scrollListItem in scrollListItems)
			{
				if (scrollListItem.Value.thread.Equals(aChatThread))
				{
					ScrollView.Remove(scrollListItem.Key);
					scrollListItems.Remove(scrollListItem.Key);
					break;
				}
			}
			SingleChatItem singleChatItem = new SingleChatItem((!(aChatThread is IGroupChatThread)) ? SingleChatItem : GroupChatItem, aChatThread, aMessage, this);
			long key = scrollListItems.Count;
			if (addToScrollView)
			{
				key = ScrollView.AddAfter(officialAccountsScroller, singleChatItem);
			}
			scrollListItems.Add(key, singleChatItem);
		}

		private void SetNoChatItems()
		{
			int num = 0;
			foreach (IChatThread item in MixSession.User.ChatThreadsFromUsers())
			{
				if (item.ChatMessages.Any() && !(item is IOfficialAccountChatThread))
				{
					num++;
				}
			}
			NoChatItems.SetActive(num <= 1);
		}
	}
}
