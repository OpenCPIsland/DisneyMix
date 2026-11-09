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
	public class GroupOptionsPanel : MonoBehaviour, ICategoryListItem, IRosterFriendListItem
	{
		private sealed class OnPanelToggled_003Ec__AnonStorey2A2
		{
			internal bool isShown;

			internal void _003C_003Em__593(KeyValuePair<long, RosterFriendListItem> aItem)
			{
				aItem.Value.SetInteractable(!isShown);
			}

			internal void _003C_003Em__594(KeyValuePair<long, RosterFriendListItem> aItem)
			{
				aItem.Value.SetInteractable(!isShown);
			}
		}

		public GameObject AddToGroupPrefab;

		public GameObject FriendItemPrefab;

		public GameObject CategoryPrefab;

		public GameObject HighlightedNickname;

		public Text Title;

		public InputField NativeInput;

		public ScrollView Scroller;

		private IChatThread thread;

		private IGroupOptionsPanel listener;

		private AddToGroupListItem addToGroupItem;

		private CategoryListItem categoryListItem;

		private long categoryId;

		private bool inited;

		private bool wasFocused;

		private SdkEvents eventGenerator = new SdkEvents();

		private SdkActions actionGenerator = new SdkActions();

		private Dictionary<long, RosterFriendListItem> nonFriendItems = new Dictionary<long, RosterFriendListItem>();

		private Dictionary<long, RosterFriendListItem> friendItems = new Dictionary<long, RosterFriendListItem>();

		void ICategoryListItem.OnCategoryToggled(bool aState)
		{
			Dictionary<long, RosterFriendListItem> dictionary = new Dictionary<long, RosterFriendListItem>(nonFriendItems);
			foreach (KeyValuePair<long, RosterFriendListItem> item in dictionary)
			{
				if (aState)
				{
					nonFriendItems.Remove(item.Key);
					nonFriendItems.Add(Scroller.AddAfter(categoryId, item.Value), item.Value);
				}
				else
				{
					Scroller.Remove(item.Key);
				}
			}
		}

		void IRosterFriendListItem.OnToggleScroll(bool aState)
		{
			Scroller.GetComponent<ScrollRect>().vertical = aState;
		}

		void IRosterFriendListItem.OnPanelToggled(bool isShown)
		{
			OnPanelToggled_003Ec__AnonStorey2A2 CS_0024_003C_003E8__locals3 = new OnPanelToggled_003Ec__AnonStorey2A2();
			CS_0024_003C_003E8__locals3.isShown = isShown;
			nonFriendItems.ToList().ForEach(delegate(KeyValuePair<long, RosterFriendListItem> aItem)
			{
				aItem.Value.SetInteractable(!CS_0024_003C_003E8__locals3.isShown);
			});
			friendItems.ToList().ForEach(delegate(KeyValuePair<long, RosterFriendListItem> aItem)
			{
				aItem.Value.SetInteractable(!CS_0024_003C_003E8__locals3.isShown);
			});
		}

		void IRosterFriendListItem.OnFriendInvited(IOutgoingFriendInvitation aInvite)
		{
			aInvite.OnAccepted += eventGenerator.AddEventHandler<AbstractFriendInvitationAcceptedEventArgs>(aInvite, RecievedInviteAcceptanceFromApi);
		}

		private void Start()
		{
			NativeInput.onEndEdit.AddListener(OnEndEdit);
			NativeInput.text = GetEditedThreadTitle();
			MixSession.User.OnUnfriended += eventGenerator.AddEventHandler<AbstractUnfriendedEventArgs>(this, RemovedFriendFromApi);
			foreach (IOutgoingFriendInvitation outgoingFriendInvitation in MixSession.User.OutgoingFriendInvitations)
			{
				outgoingFriendInvitation.OnAccepted += eventGenerator.AddEventHandler<AbstractFriendInvitationAcceptedEventArgs>(outgoingFriendInvitation, RecievedInviteAcceptanceFromApi);
			}
			thread.OnMemberAdded += eventGenerator.AddEventHandler<AbstractChatThreadMemberAddedEventArgs>(thread, OnNewMemberAdded);
			thread.OnMemberRemoved += eventGenerator.AddEventHandler<AbstractChatThreadMemberRemovedEventArgs>(thread, OnMemberRemoved);
		}

		private void OnDestroy()
		{
			NativeInput.onEndEdit.RemoveListener(OnEndEdit);
			if (MixSession.User != null)
			{
				MixSession.User.OnUnfriended -= eventGenerator.GetEventHandler<AbstractUnfriendedEventArgs>(this, RemovedFriendFromApi);
				foreach (IOutgoingFriendInvitation outgoingFriendInvitation in MixSession.User.OutgoingFriendInvitations)
				{
					outgoingFriendInvitation.OnAccepted -= eventGenerator.GetEventHandler<AbstractFriendInvitationAcceptedEventArgs>(outgoingFriendInvitation, RecievedInviteAcceptanceFromApi);
				}
			}
			if (thread != null)
			{
				thread.OnMemberAdded -= eventGenerator.GetEventHandler<AbstractChatThreadMemberAddedEventArgs>(thread, OnNewMemberAdded);
				thread.OnMemberRemoved -= eventGenerator.GetEventHandler<AbstractChatThreadMemberRemovedEventArgs>(thread, OnMemberRemoved);
			}
		}

		private void Update()
		{
			if (NativeInput.isFocused != wasFocused)
			{
				wasFocused = NativeInput.isFocused;
				HighlightedNickname.SetActive(wasFocused);
			}
		}

		private void FixedUpdate()
		{
			nonFriendItems.ToList().ForEach(delegate(KeyValuePair<long, RosterFriendListItem> aItem)
			{
				aItem.Value.FixedUpdate();
			});
			friendItems.ToList().ForEach(delegate(KeyValuePair<long, RosterFriendListItem> aItem)
			{
				aItem.Value.FixedUpdate();
			});
		}

		private string GetEditedThreadTitle()
		{
			string threadTitle = thread.GetThreadTitle();
			int max = NativeInput.characterLimit;
			return (string.IsNullOrEmpty(threadTitle) || max <= 0 || threadTitle.Length <= max) ? threadTitle : threadTitle.Substring(0, max);
		}

		public void Toggle(IChatThread aThread, IGroupOptionsPanel aListener)
		{
			thread = aThread;
			listener = aListener;
			base.gameObject.SetActive(!base.gameObject.activeSelf);
			if (base.gameObject.activeSelf)
			{
				Scroller.Show();
			}
			if (inited)
			{
				return;
			}
			addToGroupItem = new AddToGroupListItem(AddToGroupPrefab, thread, delegate(bool isDirty)
			{
				base.gameObject.SetActive(false);
				Scroller.Hide();
				if (isDirty)
				{
					listener.OnShowNotification(Singleton<Localizer>.Instance.getString("customtokens.chat.friends_added"));
				}
			});
			addToGroupItem.ItemClickedEvent += OnAddToGroupItemClicked;
			BuildMemberList();
			inited = true;
		}

		private void RecievedInviteAcceptanceFromApi(object sender, AbstractFriendInvitationAcceptedEventArgs args)
		{
			ClearMemberList();
			BuildMemberList();
		}

		private void RemovedFriendFromApi(object sender, AbstractUnfriendedEventArgs args)
		{
			ClearMemberList();
			BuildMemberList();
		}

		private void ClearMemberList()
		{
			friendItems.Clear();
			nonFriendItems.Clear();
			Scroller.Clear();
		}

		private void BuildMemberList()
		{
			categoryListItem = new CategoryListItem(CategoryPrefab, this);
			categoryId = Scroller.Add(categoryListItem, false);
			IOrderedEnumerable<IRemoteChatMember> orderedEnumerable = thread.RemoteMembers.OrderBy((IRemoteChatMember m) => m.NickFirstOrDisplayName());
			foreach (IRemoteChatMember item in orderedEnumerable)
			{
				RosterFriendListItem rosterFriendListItem = new RosterFriendListItem(FriendItemPrefab, item, this);
				if (!item.IsFriend())
				{
					nonFriendItems.Add(Scroller.Add(rosterFriendListItem, false), rosterFriendListItem);
				}
				else
				{
					friendItems.Add(Scroller.AddBefore(categoryId, rosterFriendListItem), rosterFriendListItem);
				}
			}
			if (nonFriendItems.Count <= 0)
			{
				Scroller.Remove(categoryId);
			}
		}

		public void ClosePanel()
		{
			listener.OnClosing();
			NativeInput.DeactivateInputField();
		}

		public void OnNewMemberAdded(object sender, AbstractChatThreadMemberAddedEventArgs args)
		{
			IRemoteChatMember remoteMemberById = thread.GetRemoteMemberById(args.MemberId);
			if (!this.IsNullOrDisposed() && remoteMemberById != null)
			{
				ClearMemberList();
				BuildMemberList();
				NativeInput.text = GetEditedThreadTitle();
			}
		}

		public void OnMemberRemoved(object sender, AbstractChatThreadMemberRemovedEventArgs args)
		{
			IRemoteChatMember remoteMemberById = thread.GetRemoteMemberById(args.MemberId);
			if (remoteMemberById == null)
			{
				return;
			}
			RemoveUserFromDict(remoteMemberById, remoteMemberById.IsFriend() ? friendItems : nonFriendItems);
			NativeInput.text = GetEditedThreadTitle();
		}

		private void RemoveUserFromDict(IRemoteChatMember userToRemove, Dictionary<long, RosterFriendListItem> items)
		{
			bool flag = false;
			long num = -1L;
			foreach (KeyValuePair<long, RosterFriendListItem> item in items)
			{
				if (item.Value.user.IsSameUser(userToRemove))
				{
					flag = true;
					num = item.Key;
					break;
				}
			}
			if (flag)
			{
				Scroller.Remove(num);
				items.Remove(num);
			}
		}

		private void OnEndEdit(string value)
		{
			HighlightedNickname.SetActive(false);
			string trimmedInput = (value ?? string.Empty).Trim();
			// onEndEdit fires on both blur and submit; skip the network call if nothing changed
			if (trimmedInput == GetEditedThreadTitle())
			{
				return;
			}
			if (trimmedInput != string.Empty)
			{
				NativeInput.text = trimmedInput;
				if (thread is IOneOnOneChatThread)
				{
					((IOneOnOneChatThread)thread).SetNickname(trimmedInput, actionGenerator.CreateAction(delegate(ISetChatThreadNicknameResult res)
					{
						if (res.Success)
						{
							listener.OnNicknameUpdated(trimmedInput);
						}
						else
						{
							GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
							genericPanel.ShowSimpleError("customtokens.global.generic_error");
							listener.OnNicknameUpdated(((IOneOnOneChatThread)thread).Nickname.Nickname);
							NativeInput.text = GetEditedThreadTitle();
						}
					}));
				}
				if (thread is IGroupChatThread)
				{
					((IGroupChatThread)thread).SetNickname(trimmedInput, actionGenerator.CreateAction(delegate(ISetChatThreadNicknameResult res)
					{
						if (res != null && res.Success)
						{
							listener.OnNicknameUpdated(trimmedInput);
						}
						else
						{
							GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
							genericPanel.ShowSimpleError("customtokens.global.generic_error");
							listener.OnNicknameUpdated(((IGroupChatThread)thread).Nickname.Nickname);
							NativeInput.text = GetEditedThreadTitle();
						}
					}));
				}
			}
			else
			{
				if (thread is IOneOnOneChatThread)
				{
					((IOneOnOneChatThread)thread).RemoveNickname(actionGenerator.CreateAction(delegate(IRemoveChatThreadNicknameResult res)
					{
						if (res.Success)
						{
							listener.OnNicknameUpdated(thread.GetThreadTitle());
							NativeInput.text = GetEditedThreadTitle();
						}
						else
						{
							GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
							genericPanel.ShowSimpleError("customtokens.global.generic_error");
							listener.OnNicknameUpdated(((IOneOnOneChatThread)thread).Nickname.Nickname);
							NativeInput.text = GetEditedThreadTitle();
						}
					}));
				}
				if (thread is IGroupChatThread)
				{
					((IGroupChatThread)thread).RemoveNickname(actionGenerator.CreateAction(delegate(IRemoveChatThreadNicknameResult res)
					{
						if (res != null && res.Success)
						{
							listener.OnNicknameUpdated(thread.GetThreadTitle());
							NativeInput.text = GetEditedThreadTitle();
						}
						else
						{
							GenericPanel genericPanel = (GenericPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.GENERIC);
							genericPanel.ShowSimpleError("customtokens.global.generic_error");
							listener.OnNicknameUpdated(((IGroupChatThread)thread).Nickname.Nickname);
							NativeInput.text = GetEditedThreadTitle();
						}
					}));
				}
			}
			Analytics.LogNicknameGroupAction(thread);
		}

		public void OnAddToGroupItemClicked(BasePanel panel)
		{
			if (panel != null)
			{
				panel.PanelOpenedEvent += OnFriendSelectorPanelOpened;
			}
		}

		public void OnFriendSelectorPanelOpened(BasePanel panel)
		{
			if (base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(false);
			}
			if (panel != null)
			{
				panel.PanelClosingEvent += OnFriendSelectorPanelClosing;
			}
		}

		public void OnFriendSelectorPanelClosing(BasePanel panel)
		{
			if (!base.gameObject.activeSelf)
			{
				base.gameObject.SetActive(true);
				Scroller.Show();
			}
			if (panel != null)
			{
				panel.PanelClosingEvent -= OnFriendSelectorPanelClosing;
				panel.PanelOpenedEvent -= OnFriendSelectorPanelOpened;
			}
		}
	}
}