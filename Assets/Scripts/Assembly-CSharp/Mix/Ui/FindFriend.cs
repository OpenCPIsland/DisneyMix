using System;
using System.Collections.Generic;
using System.Linq;
using Disney.Mix.SDK;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using Mix.Session.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
    public class FindFriend : MonoBehaviour, IFriendListItem
    {
        public InputField Input;

        public ScrollView ScrollView;

        public Button SearchButton;

        public GameObject FriendListItemPrefab;

        public GameObject SearchOptions;

        public GameObject ErrorBar;

        public GameObject FriendSearchIndicator;

        public Text FriendSearchIndicatorText;

        public Animator SearchOptionsAnimator;

        public NotificationBar NotificationBar;

        private bool IsBusy;

        private string currentValue = string.Empty;

        private SdkActions actionGenerator = new SdkActions();

        private List<FriendListItem> currentListItems = new List<FriendListItem>();

        private List<IFriend> currentListItemFriends = new List<IFriend>();

        private Action<IOutgoingFriendInvitation> friendInvitedCallback;

        private IFriendListItem listItemListener;

        private float headerHeight;

        private float footerHeight;

        private float searchBarHeight;

        private float searchButtonHeight;

        private Text searchText;

        private string searchForText = string.Empty;

        private bool inited;

        public event EventHandler OnFindFriendShown = delegate
        {
        };

        public event EventHandler OnFindFriendHidden = delegate
        {
        };

        void IFriendListItem.OnScrollToListItem(RectTransform aTransform, int aHeight)
        {
        }

        private void Start()
        {
            headerHeight = GameObject.Find("header").GetComponent<RectTransform>().rect.height;
            footerHeight = GameObject.Find("BottomNav").GetComponent<RectTransform>().rect.height;
            searchBarHeight = SearchOptions.GetComponent<RectTransform>().rect.height;
            searchButtonHeight = SearchButton.GetComponent<RectTransform>().rect.height;
            searchText = SearchButton.GetComponentInChildren<Text>();
            searchText.text = Singleton<Localizer>.Instance.getString("customtokens.friends.keep_typing_to_find_friends");
            searchForText = Singleton<Localizer>.Instance.getString("customtokens.friends.find_friend");
            ResizeScrollView(0);
            inited = true;
        }

        private void Update()
        {
            if (SearchButton != null)
            {
                SearchButton.interactable = !IsBusy && currentValue.Length > 0;
                SearchButton.gameObject.SetActive(Input.isFocused);
                FriendSearchIndicator.SetActive(IsBusy);
            }
        }

        private void FixedUpdate()
        {
            if (currentListItems != null && currentListItems.Count > 0)
            {
                currentListItems.ForEach(delegate(FriendListItem item)
                {
                    item.FixedUpdate();
                });
            }
        }

        public void Init(IFriendListItem aListItemListener, Action<IOutgoingFriendInvitation> aFriendInvitedCallback)
        {
            listItemListener = aListItemListener;
            friendInvitedCallback = aFriendInvitedCallback;
            MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed += OnReturnKeyPressed;
            MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged += OnKeyboardHeightChanged;
            Input.onValueChanged.AddListener(OnSearchValueChanged);
            SearchButton.onClick.AddListener(OnSearch);
            // Ensure input field also triggers search on return/submit
            Input.onEndEdit.AddListener(OnInputEndEdit);
        }

        private void OnDestroy()
        {
            if (MonoSingleton<NativeKeyboardManager>.Instance != null)
            {
                MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardReturnKeyPressed -= OnReturnKeyPressed;
                MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.OnNativeKeyboardHeightChanged -= OnKeyboardHeightChanged;
            }
            if (Input != null)
            {
                Input.onValueChanged.RemoveListener(OnSearchValueChanged);
                Input.onEndEdit.RemoveListener(OnInputEndEdit);
            }
            if (SearchButton != null)
            {
                SearchButton.onClick.RemoveListener(OnSearch);
            }
        }

        public void Show()
        {
            MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
            SearchOptions.SetActive(false);
            base.gameObject.SetActive(true);
            this.OnFindFriendShown(this, new EventArgs());
            Input.Select();
        }

        public void Hide()
        {
            HideErrorMessage();
            MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
            if (inited)
            {
                Input.text = string.Empty;
                currentValue = string.Empty;
                searchText.text = searchForText.Replace("#DisplayName#", "...");
                ScrollView.Clear();
                currentListItems.Clear();
                currentListItemFriends.Clear();
                SearchOptions.SetActive(true);
                SearchOptionsAnimator.Play("SearchIcon_SlideIn");
                base.gameObject.SetActive(false);
            }
            this.OnFindFriendHidden(this, new EventArgs());
        }

        public void OnSearch()
        {
            SearchForFriendByDisplayName(Input.text);
            FriendSearchIndicatorText.text = searchText.text;
        }

        private void OnKeyboardHeightChanged(object sender, NativeKeyboardHeightChangedEventArgs args)
        {
            if (Input != null && (Input.isFocused || args.Height <= 0))
            {
                ResizeScrollView(args.Height);
            }
        }

        private void OnSearchValueChanged(string aValue)
        {
            SetValue(aValue);
        }

        private void OnInputEndEdit(string aValue)
        {
            // Only trigger search if user submitted (return key), not just lost focus
            if (Input.wasCanceled)
            {
                return;
            }
            OnSearch();
        }

        private void OnReturnKeyPressed(object sender, NativeKeyboardReturnKeyPressedEventArgs args)
        {
            if (!this.IsNullOrDisposed() && !(base.gameObject == null) && base.gameObject.activeSelf)
            {
                if (SearchButton.IsActive())
                {
                    SearchButton.onClick.Invoke();
                }
                else
                {
                    Hide();
                }
            }
        }

        public void ResizeScrollView(int aHeight)
        {
            if (!(ScrollView == null) && !(base.gameObject == null))
            {
                RectTransform component = ScrollView.GetComponent<RectTransform>();
                float y = MixConstants.CANVAS_HEIGHT - headerHeight - searchBarHeight - searchButtonHeight - (float)aHeight;
                if (aHeight <= 0)
                {
                    y = MixConstants.CANVAS_HEIGHT - headerHeight - searchBarHeight - footerHeight;
                    SearchButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(SearchButton.GetComponent<RectTransform>().anchoredPosition.x, footerHeight);
                }
                else
                {
                    SearchButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(SearchButton.GetComponent<RectTransform>().anchoredPosition.x, aHeight);
                }
                component.sizeDelta = new Vector2(component.sizeDelta.x, y);
                ScrollView.Reposition();
                ScrollView.container.gameObject.SetActive(false);
                ScrollView.container.gameObject.SetActive(true);
                Canvas.ForceUpdateCanvases();
            }
        }

        private void ShowErrorMessage(string aText)
        {
            if ((this.IsNullOrDisposed() || !(ErrorBar != null) || ErrorBar.activeSelf) && ErrorBar.activeInHierarchy)
            {
                return;
            }
            if (!base.gameObject.activeSelf)
            {
                NotificationBar.ShowFromString(aText);
                return;
            }
            ErrorBar.SetActive(true);
            Text componentInChildren = ErrorBar.GetComponentInChildren<Text>();
            if (componentInChildren != null)
            {
                componentInChildren.text = aText;
            }
        }

        private void HideErrorMessage()
        {
            if (ErrorBar != null)
            {
                ErrorBar.SetActive(false);
            }
        }

        public void SetValue(string aValue)
        {
            currentValue = aValue;
            if (inited)
            {
                searchText.text = searchForText.Replace("#DisplayName#", (aValue.Length <= 0) ? "..." : aValue);
            }
            Filter();
        }

        private int CompareListItems(ScrollItem item1, ScrollItem item2)
        {
            return (item1.Generator as BaseFriendsListItem).SortName.CompareTo((item2.Generator as BaseFriendsListItem).SortName);
        }

        private void Filter()
        {
            if (Equals(null) || !MixSession.IsValidSession || MixSession.User == null || MixSession.User.Friends == null)
            {
                return;
            }
            List<IFriend> list = new List<IFriend>();
            List<IFriend> list2 = new List<IFriend>();
            foreach (IFriend friend in MixSession.User.Friends)
            {
                if (friend != null && friend.DisplayName != null)
                {
                    if (!currentValue.Equals(string.Empty) && friend.DisplayName.Text != null && friend.DisplayName.Text.ToLower().Contains(currentValue.ToLower()))
                    {
                        list.Add(friend);
                    }
                    else
                    {
                        list2.Add(friend);
                    }
                }
            }
            FriendListItem[] array = currentListItems.ToArray();
            FriendListItem[] array2 = array;
            foreach (FriendListItem friendListItem in array2)
            {
                if (list2.Contains(friendListItem.friend))
                {
                    ScrollView.Remove(friendListItem);
                    currentListItems.Remove(friendListItem);
                    currentListItemFriends.Remove(friendListItem.friend);
                }
            }
            foreach (IFriend item in list)
            {
                if (!currentListItemFriends.Contains(item))
                {
                    FriendListItem friendListItem2 = new FriendListItem(FriendListItemPrefab, item, this, null, false, "UI Foreground");
                    friendListItem2.RemoveEditNickname(true);
                    ScrollView.Add(friendListItem2, true, false);
                    currentListItems.Add(friendListItem2);
                    currentListItemFriends.Add(item);
                }
            }
            ScrollView.Sort(CompareListItems);
        }

        public void OnFriendListItemClicked(IFriend friend)
        {
            if (listItemListener != null)
            {
                listItemListener.OnFriendListItemClicked(friend);
            }
        }

        public void OnFriendListSendTrustInviteClicked(IFriend friend)
        {
            if (listItemListener != null)
            {
                listItemListener.OnFriendListSendTrustInviteClicked(friend);
            }
        }

        public void OnFriendListRemoveClicked(IFriend friend)
        {
            FriendListItem[] array = currentListItems.ToArray();
            FriendListItem[] array2 = array;
            foreach (FriendListItem friendListItem in array2)
            {
                if (friendListItem.friend.IsSameUser(friend))
                {
                    ScrollView.Remove(friendListItem);
                    currentListItems.Remove(friendListItem);
                    currentListItemFriends.Remove(friendListItem.friend);
                    break;
                }
            }
            if (listItemListener != null)
            {
                listItemListener.OnFriendListRemoveClicked(friend);
            }
        }

        public void OnToggleScroll(bool aState)
        {
            ScrollView.GetComponent<ScrollRect>().vertical = aState;
        }

        public void OnScrollToListItem(RectTransform aTransform, int aHeight)
        {
        }

        public void SearchForFriendByDisplayName(string aDisplayName)
        {
            if (string.IsNullOrEmpty(aDisplayName))
            {
                return;
            }

            if (MixSession.Session == null || MixSession.Session.LocalUser == null)
            {
                ShowErrorMessage("Session not available");
                return;
            }

            if (aDisplayName == MixSession.Session.LocalUser.DisplayName.Text)
            {
                ShowErrorMessage(Singleton<Localizer>.Instance.getString("customtokens.friends.error_that_is_your_displayname"));
                return;
            }

            if (IsBusy)
            {
                return;
            }

            try
            {
                IsBusy = true;
                MixSession.Session.LocalUser.FindUser(aDisplayName, actionGenerator.CreateAction<IFindUserResult>(OnFriendFound));
            }
            catch (Exception exception)
            {
                IsBusy = false;
                Log.Exception(exception);
            }
        }

        public void OnFriendFound(IFindUserResult results)
        {
            if (this.IsNullOrDisposed() || base.gameObject == null)
            {
                return;
            }

            IsBusy = false;

            if (!results.Success)
            {
                Analytics.LogFindFriendFailed();
                ShowErrorMessage(Singleton<Localizer>.Instance.getString("customtokens.friends.error_displayname_not_valid"));
                return;
            }

            if (MixSession.Session == null || MixSession.Session.LocalUser == null || results.User == null)
            {
                ShowErrorMessage(Singleton<Localizer>.Instance.getString("customtokens.global.generic_error"));
                return;
            }

            if (results.User.DisplayName.Text == MixSession.Session.LocalUser.DisplayName.Text)
            {
                ShowErrorMessage(Singleton<Localizer>.Instance.getString("customtokens.friends.error_that_is_your_displayname"));
                return;
            }

            if (MixChat.IsFriendByDisplayName(results.User))
            {
                ShowErrorMessage(Singleton<Localizer>.Instance.getString("customtokens.friends.user_already_friend"));
                return;
            }

            if (MixSession.Session.LocalUser.OutgoingFriendInvitations.Any((IOutgoingFriendInvitation inv) => inv.Invitee.DisplayName.Text == results.User.DisplayName.Text))
            {
                Hide();
                FixupDisplayName(results.User);
                Analytics.LogPageView("already_sent_friend_spark");
                return;
            }

            Hide();
            FixupDisplayName(results.User, true);
        }

        private void FixupDisplayName(IUnidentifiedUser userInfo, bool showInvitePanel = false)
        {
            if (userInfo != null && !string.IsNullOrEmpty(userInfo.DisplayName.Text))
            {
                if (!(Singleton<PanelManager>.Instance.FindPanel(typeof(FriendResultPanel)) != null))
                {
                    this.OnFindFriendShown(this, new EventArgs());
                    FriendResultPanel friendResultPanel = (FriendResultPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.FRIEND_RESULT);
                    friendResultPanel.PanelClosingEvent += delegate
                    {
                        this.OnFindFriendHidden(this, new EventArgs());
                    };
                    friendResultPanel.Init(showInvitePanel, new InvitableUser(userInfo), FriendResultPanel.Location.FindFriend, friendInvitedCallback);
                    Analytics.LogFoundFriendPageView(userInfo.DisplayName.Text, "invite_spark");
                    Input.text = string.Empty;
                }
            }
            else
            {
                GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
                genericPanel.ShowSimpleError("customtokens.global.generic_error");
            }
        }
    }
}
