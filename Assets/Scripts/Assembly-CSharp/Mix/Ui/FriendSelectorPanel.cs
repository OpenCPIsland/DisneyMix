using System;
using System.Collections.Generic;
using System.Linq;
using Disney.Mix.SDK;
using Mix.FakeFriend.Datatypes;
using Mix.Localization;
using Mix.Native;
using Mix.Session;
using Mix.Session.Extensions;
using Mix.Session.Local.Thread;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class FriendSelectorPanel : BasePanel
	{
		private const int MAX_GROUP_CHAT = 20;

		public GameObject FriendListItem;

		public ScrollView ScrollView;

		public Button StartChatButton;

		public Button AddToGroupButton;

		public InputField InputField;

		public GameObject Highlighted;

		private int maxCharacters = int.MaxValue;

		private Action<bool> onFriendsAddedToGroupEvent;

		private List<FriendListItem> items = new List<FriendListItem>();

		private List<IFriend> itemFriends = new List<IFriend>();

		private List<IFriend> selectedFriends = new List<IFriend>();

		private SdkActions actionGenerator = new SdkActions();

		private IChatThread thread;

		private bool IsClosed;

		public void Init(IChatThread aThread = null, Action<bool> aOnFriendsAddedToGroupEvent = null)
		{
			thread = aThread;
			onFriendsAddedToGroupEvent = aOnFriendsAddedToGroupEvent;
			InputField.onValueChanged.AddListener(OnInputValueChanged);
			InputField.onEndEdit.AddListener(OnInputEndEdit);
			IOrderedEnumerable<IFriend> orderedEnumerable = MixSession.User.Friends.OrderBy((IFriend f) => f.NickFirstOrDisplayName());
			foreach (IFriend item in orderedEnumerable)
			{
				if (!(item is FakeFriendData))
				{
					bool flag = thread != null && thread.IsThreadMember(item.Id);
					if (flag)
					{
						selectedFriends.Add(item);
					}
					FriendListItem friendListItem = new FriendListItem(FriendListItem, item, null, OnToggleChanged, flag, "Overlay", flag);
					ScrollView.Add(friendListItem, false);
					items.Add(friendListItem);
					itemFriends.Add(item);
				}
			}
			if (thread != null)
			{
				ToggleUnselected(thread.MemberCount() >= 20);
				AddToGroupButton.gameObject.SetActive(true);
				AddToGroupButton.interactable = false;
				base.PanelOpenedEvent += WaitTilLoad;
				StartChatButton.gameObject.SetActive(false);
			}
			else
			{
				StartChatButton.interactable = false;
				StartChatButton.gameObject.GetComponentInChildren<Text>().text = Singleton<Localizer>.Instance.getString("customtokens.friends.select_more");
			}
		}

		private void WaitTilLoad(BasePanel aBasePanel)
		{
			AddToGroupButton.interactable = true;
			base.PanelOpenedEvent -= WaitTilLoad;
		}

		private void OnDestroy()
		{
			InputField.onValueChanged.RemoveListener(OnInputValueChanged);
			InputField.onEndEdit.RemoveListener(OnInputEndEdit);
		}

		public void OnAddToGroupButtonClicked()
		{
			if (IsClosed)
			{
				return;
			}
			IsClosed = true;
			bool obj = false;
			List<IFriend> friendsToAdd = new List<IFriend>();
			foreach (IFriend selectedFriend in selectedFriends)
			{
				if (!thread.IsThreadMember(selectedFriend.Id))
				{
					friendsToAdd.Add(selectedFriend);
					obj = true;
				}
			}
			if (friendsToAdd.Count > 0)
			{
				MixSession.User.AddChatThreadMembers((IGroupChatThread)thread, friendsToAdd, actionGenerator.CreateAction(delegate(IAddChatThreadMemberResult e)
				{
					if (e.Success)
					{
						foreach (IFriend item in friendsToAdd)
						{
							if (item != null)
							{
								Analytics.LogAddFriendToGroupChat(thread, item);
							}
						}
						return;
					}
					GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
					if (e is IAddChatThreadMemberMaxMemberCountExceededResult)
					{
						Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
						{
							{ genericPanel.TitleText, null },
							{ genericPanel.MessageText, "customtokens.chat.chat_membership_full" }
						});
					}
					else
					{
						Singleton<PanelManager>.Instance.SetupTokens(new Dictionary<Text, string>
						{
							{ genericPanel.TitleText, null },
							{ genericPanel.MessageText, "customtokens.global.generic_error" }
						});
					}
				}));
			}
			ScrollView.Hide();
			if (onFriendsAddedToGroupEvent != null)
			{
				onFriendsAddedToGroupEvent(obj);
			}
			ClosePanel();
		}

		public override void ClosePanel()
		{
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			base.ClosePanel();
		}

		public void OnStartChatButtonClicked()
		{
			if (IsClosed)
			{
				return;
			}
			IsClosed = true;
			if (selectedFriends.Count() != 0)
			{
				IChatThread chatThread = MixChat.FindThreadWithSameMembers(selectedFriends);
				if (chatThread != null)
				{
					ChatHelper.NavigateToChatScreen(chatThread, new TransitionSlideLeft());
					return;
				}
				LocalThread aThread = ((selectedFriends.Count() <= 1) ? ((LocalThread)new OneOnOneLocalThread(MixSession.User, selectedFriends)) : ((LocalThread)new GroupLocalThread(MixSession.User, selectedFriends)));
				ChatHelper.NavigateToChatScreen(aThread, new TransitionSlideLeft());
			}
		}

		public void ExitSearch()
		{
			InputField.text = string.Empty;
			Filter(InputField.text);
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
		}

		private int GetTotalMemberCount()
		{
			int result = selectedFriends.Count() + 1;
			if (thread != null)
			{
				List<IFriend> list = new List<IFriend>();
				foreach (IFriend selectedFriend in selectedFriends)
				{
					if (!thread.IsThreadMember(selectedFriend.Id))
					{
						list.Add(selectedFriend);
					}
				}
				result = list.Count + thread.MemberCount();
			}
			return result;
		}

		private void Filter(string aValue)
		{
			List<IFriend> list = new List<IFriend>();
			List<IFriend> list2 = new List<IFriend>();
			foreach (IFriend friend in MixSession.User.Friends)
			{
				if (friend is FakeFriendData)
				{
					continue;
				}
				if (!string.IsNullOrEmpty(aValue))
				{
					if (!string.IsNullOrEmpty(friend.FirstName) && friend.FirstName.ToLower().Contains(aValue.ToLower()))
					{
						list.Add(friend);
						continue;
					}
					if (friend.DisplayName != null && !string.IsNullOrEmpty(friend.DisplayName.Text) && friend.DisplayName.Text.ToLower().Contains(aValue.ToLower()))
					{
						list.Add(friend);
						continue;
					}
				}
				list2.Add(friend);
			}
			FriendListItem[] array = items.ToArray();
			FriendListItem[] array2 = array;
			foreach (FriendListItem friendListItem in array2)
			{
				if (list2.Contains(friendListItem.friend))
				{
					ScrollView.Remove(friendListItem);
					items.Remove(friendListItem);
					itemFriends.Remove(friendListItem.friend);
				}
			}
			int totalMemberCount = GetTotalMemberCount();
			foreach (IFriend item in list)
			{
				if (!itemFriends.Contains(item))
				{
					bool flag = selectedFriends.Contains(item);
					bool aDisabled = (!flag && totalMemberCount >= 20) || (thread != null && thread.IsThreadMember(item.Id));
					FriendListItem friendListItem2 = new FriendListItem(FriendListItem, item, null, OnToggleChanged, aDisabled, "Overlay", flag);
					ScrollView.Add(friendListItem2, true);
					items.Add(friendListItem2);
					itemFriends.Add(item);
				}
			}
		}

		private void ToggleUnselected(bool aState)
		{
			foreach (FriendListItem item in items)
			{
				if (!item.selected)
				{
					item.Disabled = aState;
				}
			}
		}

		private void OnToggleChanged(FriendListItem aItem)
		{
			List<string> list = new List<string>();
			if (aItem.Selected() && !selectedFriends.Contains(aItem.friend))
			{
				selectedFriends.Add(aItem.friend);
			}
			else if (!aItem.Selected() && selectedFriends.Contains(aItem.friend))
			{
				selectedFriends.Remove(aItem.friend);
			}
			int totalMemberCount = GetTotalMemberCount();
			ToggleUnselected(totalMemberCount >= 20);
			foreach (IFriend selectedFriend in selectedFriends)
			{
				list.Add(selectedFriend.NickFirstOrDisplayName());
			}
			Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_6_AvatarUISelect");
			StartChatButton.interactable = list.Count > 0;
			AddToGroupButton.interactable = list.Count > 0;
			Text component = StartChatButton.transform.Find("StartGroupChatText").GetComponent<Text>();
			switch (list.Count)
			{
			case 0:
				component.text = Singleton<Localizer>.Instance.getString("customtokens.friends.select_more");
				return;
			case 1:
				component.text = Singleton<Localizer>.Instance.getString("customtokens.friends.start_chat_with").Replace("#name#", list[0]);
				return;
			}
			string newValue = string.Join(", ", list.ToArray());
			component.text = Singleton<Localizer>.Instance.getString("customtokens.friends.start_group_chat").Replace("#names#", newValue);
			bool flag = false;
			if (component.text.Length > maxCharacters)
			{
				component.text = component.text.Substring(0, maxCharacters);
				flag = true;
			}
			Canvas.ForceUpdateCanvases();
			int lineCount = component.cachedTextGenerator.lineCount;
			while (lineCount > 2)
			{
				component.text = component.text.Substring(0, component.text.Length - 1);
				Canvas.ForceUpdateCanvases();
				flag = true;
				lineCount = component.cachedTextGenerator.lineCount;
				maxCharacters = component.text.Length;
			}
			if (flag)
			{
				component.text = component.text.Substring(0, component.text.Length - 4) + "...";
			}
		}

		private void OnInputValueChanged(string aValue)
		{
			Filter(aValue);
		}

		private void OnInputSelected(string aValue)
		{
			Highlighted.SetActive(true);
		}

		private void OnInputEndEdit(string aValue)
		{
			Highlighted.SetActive(false);
		}

		private void Update()
		{
			Highlighted.SetActive(InputField != null && InputField.isFocused);
		}
	}
}
