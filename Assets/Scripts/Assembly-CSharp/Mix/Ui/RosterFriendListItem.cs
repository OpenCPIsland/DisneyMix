using System;
using System.Linq;
using Avatar;
using Disney.Mix.SDK;
using Mix.Avatar;
using Mix.Native;
using Mix.Session;
using Mix.Session.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class RosterFriendListItem : IScrollItem
	{
		private enum AnimationType
		{
			Idle = 0,
			Show = 1,
			Hide = 2
		}

		private delegate void OnFriendListItemSlideOpen(RosterFriendListItem aItem);

		private sealed class GenerateGameObject_003Ec__AnonStorey2A5
		{
			internal GameObject inst;

			internal RosterFriendListItem _003C_003Ef__this;
		}

		private sealed class GenerateGameObject_003Ec__AnonStorey2A4
		{
			internal Transform imageTarget;

			internal int imageTargetSize;

			internal SnapshotCallback cb;

			internal EventHandler<AbstractAvatarChangedEventArgs> avatarUpdateListener;

			internal GenerateGameObject_003Ec__AnonStorey2A5 _003C_003Ef__ref_0024677;

			internal RosterFriendListItem _003C_003Ef__this;

			internal void _003C_003Em__59D(bool success, Sprite sprite)
			{
				_003C_003Ef__this.avatarCleanup = null;
				if (sprite != null && imageTarget != null && imageTarget.GetComponent<Image>() != null)
				{
					imageTarget.GetComponent<Image>().sprite = sprite;
					imageTarget.gameObject.SetActive(true);
				}
			}

			internal void _003C_003Em__59E(object sender, AbstractAvatarChangedEventArgs args)
			{
				Transform transform = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? _003C_003Ef__ref_0024677.inst.transform.Find("FriendInfo/AvatarImage/ImageTarget") : _003C_003Ef__ref_0024677.inst.transform.Find("FriendInfo/AvatarImage/Mask/ImageTarget_Geo"));
				if (!transform.Equals(imageTarget))
				{
					imageTarget.gameObject.SetActive(false);
				}
				imageTarget = transform;
				imageTargetSize = (int)imageTarget.GetComponent<RectTransform>().rect.height;
				if (_003C_003Ef__this.avatarCleanup != null)
				{
					_003C_003Ef__this.avatarCleanup();
				}
				if (imageTarget != null && MonoSingleton<AvatarManager>.Instance != null)
				{
					_003C_003Ef__this.avatarCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, imageTargetSize, cb);
				}
			}

			internal void _003C_003Em__59F()
			{
				_003C_003Ef__this.user.OnAvatarChanged -= _003C_003Ef__this.eventGenerator.GetEventHandler(_003C_003Ef__this.user, avatarUpdateListener);
			}

			internal void _003C_003Em__5A0(GameObject aGameObject, Vector2 aMoved)
			{
				Rect rectInScreenSpace = Util.GetRectInScreenSpace(_003C_003Ef__this.friendInfoRect);
				if (_003C_003Ef__this.friendInfoRect.anchoredPosition.x < 0f && !_003C_003Ef__this.touched && rectInScreenSpace.Contains(UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero))
				{
					_003C_003Ef__this.animationType = AnimationType.Hide;
					return;
				}
				if (!_003C_003Ef__this.touched && RosterFriendListItem.OnFriendsListItemSlidedOpenCallback != null)
				{
					RosterFriendListItem.OnFriendsListItemSlidedOpenCallback(_003C_003Ef__this);
				}
				_003C_003Ef__this.touched = true;
				_003C_003Ef__this.slideDelta += aMoved;
				if (!_003C_003Ef__this.posioned && Mathf.Abs(_003C_003Ef__this.slideDelta.x) > 15f && Mathf.Abs(_003C_003Ef__this.slideDelta.y) < 10f)
				{
					_003C_003Ef__this.sliding = true;
					_003C_003Ef__this.listener.OnToggleScroll(false);
				}
				else if (Mathf.Abs(_003C_003Ef__this.slideDelta.y) >= 10f)
				{
					_003C_003Ef__this.posioned = true;
				}
				if (_003C_003Ef__this.sliding)
				{
					_003C_003Ef__this.friendInfoRect.anchoredPosition = new Vector2((!(_003C_003Ef__this.slideDelta.x > 0f)) ? _003C_003Ef__this.slideDelta.x : 0f, 0f);
				}
			}

			internal void _003C_003Em__5A1(GameObject aGameObject)
			{
				_003C_003Ef__this.sliding = false;
				_003C_003Ef__this.touched = false;
				_003C_003Ef__this.posioned = false;
				_003C_003Ef__this.slideDelta = Vector2.zero;
				_003C_003Ef__this.listener.OnToggleScroll(true);
				if (_003C_003Ef__this.friendInfoRect.anchoredPosition.x < (0f - _003C_003Ef__this.reportButton.sizeDelta.x) / 2f)
				{
					_003C_003Ef__this.animationType = AnimationType.Show;
				}
				else
				{
					_003C_003Ef__this.animationType = AnimationType.Hide;
				}
				if (aGameObject.name == "open")
				{
					_003C_003Ef__this.animationType = AnimationType.Show;
				}
				if (aGameObject.name == "close")
				{
					_003C_003Ef__this.animationType = AnimationType.Hide;
				}
			}
		}

		private GameObject prefab;

		public IRemoteChatMember user;

		private RectTransform friendInfoRect;

		private RectTransform reportButton;

		private readonly IRosterFriendListItem listener;

		private bool interactable = true;

		private bool sliding;

		private bool posioned;

		private bool touched;

		private Vector2 slideDelta = Vector2.zero;

		private AnimationType animationType;

		private Action avatarCleanup;

		private Action avatarUpdateCleanup;

		private SdkEvents eventGenerator = new SdkEvents();

		private static event OnFriendListItemSlideOpen OnFriendsListItemSlidedOpenCallback;

		public RosterFriendListItem(GameObject aPrefab, IRemoteChatMember aUser, IRosterFriendListItem aListener)
		{
			prefab = aPrefab;
			user = aUser;
			listener = aListener;
		}

		GameObject IScrollItem.GenerateGameObject(bool aGenerateForHeightOnly)
		{
			GenerateGameObject_003Ec__AnonStorey2A5 generateGameObject_003Ec__AnonStorey2A = new GenerateGameObject_003Ec__AnonStorey2A5();
			generateGameObject_003Ec__AnonStorey2A._003C_003Ef__this = this;
			generateGameObject_003Ec__AnonStorey2A.inst = UnityEngine.Object.Instantiate(prefab);
			UnityEngine.Object.Destroy(generateGameObject_003Ec__AnonStorey2A.inst.GetComponent<Button>());
			RosterFriendListItem.OnFriendsListItemSlidedOpenCallback = (OnFriendListItemSlideOpen)Delegate.Combine(RosterFriendListItem.OnFriendsListItemSlidedOpenCallback, new OnFriendListItemSlideOpen(OnListItemSlidedOpen));
			generateGameObject_003Ec__AnonStorey2A.inst.transform.Find("FriendInfo/FriendNameText").GetComponent<Text>().text = user.NickFirstOrDisplayName();
			generateGameObject_003Ec__AnonStorey2A.inst.transform.Find("FriendInfo/DisplayNameText").GetComponent<Text>().text = MixChat.FormatDisplayName(user.DisplayName.Text);
			friendInfoRect = generateGameObject_003Ec__AnonStorey2A.inst.transform.Find("FriendInfo").GetComponent<RectTransform>();
			reportButton = generateGameObject_003Ec__AnonStorey2A.inst.transform.Find("ReportFriendBtn").GetComponent<RectTransform>();
			reportButton.GetComponent<Button>().onClick.AddListener(OnReportButtonClicked);
			if (!user.IsFriend())
			{
				Button component = generateGameObject_003Ec__AnonStorey2A.inst.transform.Find("FriendInfo/AddFriendBtn").GetComponent<Button>();
				component.gameObject.SetActive(true);
				component.onClick.AddListener(OnAddFriendButtonClicked);
			}
			if (!aGenerateForHeightOnly)
			{
				GenerateGameObject_003Ec__AnonStorey2A4 CS_0024_003C_003E8__locals62 = new GenerateGameObject_003Ec__AnonStorey2A4();
				CS_0024_003C_003E8__locals62._003C_003Ef__ref_0024677 = generateGameObject_003Ec__AnonStorey2A;
				CS_0024_003C_003E8__locals62._003C_003Ef__this = this;
				CS_0024_003C_003E8__locals62.imageTarget = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(user.Avatar)) ? generateGameObject_003Ec__AnonStorey2A.inst.transform.Find("FriendInfo/AvatarImage/ImageTarget") : generateGameObject_003Ec__AnonStorey2A.inst.transform.Find("FriendInfo/AvatarImage/Mask/ImageTarget_Geo"));
				CS_0024_003C_003E8__locals62.imageTargetSize = (int)CS_0024_003C_003E8__locals62.imageTarget.GetComponent<RectTransform>().rect.height;
				CS_0024_003C_003E8__locals62.cb = delegate(bool success, Sprite sprite)
				{
					CS_0024_003C_003E8__locals62._003C_003Ef__this.avatarCleanup = null;
					if (sprite != null && CS_0024_003C_003E8__locals62.imageTarget != null && CS_0024_003C_003E8__locals62.imageTarget.GetComponent<Image>() != null)
					{
						CS_0024_003C_003E8__locals62.imageTarget.GetComponent<Image>().sprite = sprite;
						CS_0024_003C_003E8__locals62.imageTarget.gameObject.SetActive(true);
					}
				};
				avatarCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(user.Avatar, (AvatarFlags)0, CS_0024_003C_003E8__locals62.imageTargetSize, CS_0024_003C_003E8__locals62.cb);
				CS_0024_003C_003E8__locals62.avatarUpdateListener = delegate(object sender, AbstractAvatarChangedEventArgs args)
				{
					Transform transform = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? CS_0024_003C_003E8__locals62._003C_003Ef__ref_0024677.inst.transform.Find("FriendInfo/AvatarImage/ImageTarget") : CS_0024_003C_003E8__locals62._003C_003Ef__ref_0024677.inst.transform.Find("FriendInfo/AvatarImage/Mask/ImageTarget_Geo"));
					if (!transform.Equals(CS_0024_003C_003E8__locals62.imageTarget))
					{
						CS_0024_003C_003E8__locals62.imageTarget.gameObject.SetActive(false);
					}
					CS_0024_003C_003E8__locals62.imageTarget = transform;
					CS_0024_003C_003E8__locals62.imageTargetSize = (int)CS_0024_003C_003E8__locals62.imageTarget.GetComponent<RectTransform>().rect.height;
					if (CS_0024_003C_003E8__locals62._003C_003Ef__this.avatarCleanup != null)
					{
						CS_0024_003C_003E8__locals62._003C_003Ef__this.avatarCleanup();
					}
					if (CS_0024_003C_003E8__locals62.imageTarget != null && MonoSingleton<AvatarManager>.Instance != null)
					{
						CS_0024_003C_003E8__locals62._003C_003Ef__this.avatarCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, CS_0024_003C_003E8__locals62.imageTargetSize, CS_0024_003C_003E8__locals62.cb);
					}
				};
				user.OnAvatarChanged += eventGenerator.AddEventHandler(user, CS_0024_003C_003E8__locals62.avatarUpdateListener);
				avatarUpdateCleanup = delegate
				{
					CS_0024_003C_003E8__locals62._003C_003Ef__this.user.OnAvatarChanged -= CS_0024_003C_003E8__locals62._003C_003Ef__this.eventGenerator.GetEventHandler(CS_0024_003C_003E8__locals62._003C_003Ef__this.user, CS_0024_003C_003E8__locals62.avatarUpdateListener);
				};
				SlideRecognizer slideRecognizer = generateGameObject_003Ec__AnonStorey2A.inst.AddComponent<SlideRecognizer>();
				slideRecognizer.onSlide = (SlideRecognizer.OnSlide)Delegate.Combine(slideRecognizer.onSlide, (SlideRecognizer.OnSlide)delegate(GameObject aGameObject, Vector2 aMoved)
				{
					Rect rectInScreenSpace = Util.GetRectInScreenSpace(CS_0024_003C_003E8__locals62._003C_003Ef__this.friendInfoRect);
					if (CS_0024_003C_003E8__locals62._003C_003Ef__this.friendInfoRect.anchoredPosition.x < 0f && !CS_0024_003C_003E8__locals62._003C_003Ef__this.touched && rectInScreenSpace.Contains(UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero))
					{
						CS_0024_003C_003E8__locals62._003C_003Ef__this.animationType = AnimationType.Hide;
					}
					else
					{
						if (!CS_0024_003C_003E8__locals62._003C_003Ef__this.touched && RosterFriendListItem.OnFriendsListItemSlidedOpenCallback != null)
						{
							RosterFriendListItem.OnFriendsListItemSlidedOpenCallback(CS_0024_003C_003E8__locals62._003C_003Ef__this);
						}
						CS_0024_003C_003E8__locals62._003C_003Ef__this.touched = true;
						CS_0024_003C_003E8__locals62._003C_003Ef__this.slideDelta += aMoved;
						if (!CS_0024_003C_003E8__locals62._003C_003Ef__this.posioned && Mathf.Abs(CS_0024_003C_003E8__locals62._003C_003Ef__this.slideDelta.x) > 15f && Mathf.Abs(CS_0024_003C_003E8__locals62._003C_003Ef__this.slideDelta.y) < 10f)
						{
							CS_0024_003C_003E8__locals62._003C_003Ef__this.sliding = true;
							CS_0024_003C_003E8__locals62._003C_003Ef__this.listener.OnToggleScroll(false);
						}
						else if (Mathf.Abs(CS_0024_003C_003E8__locals62._003C_003Ef__this.slideDelta.y) >= 10f)
						{
							CS_0024_003C_003E8__locals62._003C_003Ef__this.posioned = true;
						}
						if (CS_0024_003C_003E8__locals62._003C_003Ef__this.sliding)
						{
							CS_0024_003C_003E8__locals62._003C_003Ef__this.friendInfoRect.anchoredPosition = new Vector2((!(CS_0024_003C_003E8__locals62._003C_003Ef__this.slideDelta.x > 0f)) ? CS_0024_003C_003E8__locals62._003C_003Ef__this.slideDelta.x : 0f, 0f);
						}
					}
				});
				slideRecognizer.onSlideComplete = (SlideRecognizer.OnSlideComplete)Delegate.Combine(slideRecognizer.onSlideComplete, (SlideRecognizer.OnSlideComplete)delegate(GameObject aGameObject)
				{
					CS_0024_003C_003E8__locals62._003C_003Ef__this.sliding = false;
					CS_0024_003C_003E8__locals62._003C_003Ef__this.touched = false;
					CS_0024_003C_003E8__locals62._003C_003Ef__this.posioned = false;
					CS_0024_003C_003E8__locals62._003C_003Ef__this.slideDelta = Vector2.zero;
					CS_0024_003C_003E8__locals62._003C_003Ef__this.listener.OnToggleScroll(true);
					if (CS_0024_003C_003E8__locals62._003C_003Ef__this.friendInfoRect.anchoredPosition.x < (0f - CS_0024_003C_003E8__locals62._003C_003Ef__this.reportButton.sizeDelta.x) / 2f)
					{
						CS_0024_003C_003E8__locals62._003C_003Ef__this.animationType = AnimationType.Show;
					}
					else
					{
						CS_0024_003C_003E8__locals62._003C_003Ef__this.animationType = AnimationType.Hide;
					}
					if (aGameObject.name == "open")
					{
						CS_0024_003C_003E8__locals62._003C_003Ef__this.animationType = AnimationType.Show;
					}
					if (aGameObject.name == "close")
					{
						CS_0024_003C_003E8__locals62._003C_003Ef__this.animationType = AnimationType.Hide;
					}
				});
			}
			return generateGameObject_003Ec__AnonStorey2A.inst;
		}

		void IScrollItem.Destroy()
		{
			RosterFriendListItem.OnFriendsListItemSlidedOpenCallback = (OnFriendListItemSlideOpen)Delegate.Remove(RosterFriendListItem.OnFriendsListItemSlidedOpenCallback, new OnFriendListItemSlideOpen(OnListItemSlidedOpen));
			if (avatarCleanup != null && MonoSingleton<AvatarManager>.Instance != null)
			{
				avatarCleanup();
			}
			if (avatarUpdateCleanup != null)
			{
				avatarUpdateCleanup();
			}
		}

		public void FixedUpdate()
		{
			if (friendInfoRect == null || reportButton == null)
			{
				return;
			}
			if (animationType == AnimationType.Show)
			{
				friendInfoRect.anchoredPosition = Util.HalfDistance(friendInfoRect.anchoredPosition, new Vector2(0f - reportButton.sizeDelta.x, 0f));
				if (Vector2.Distance(friendInfoRect.anchoredPosition, new Vector2(0f - reportButton.sizeDelta.x, 0f)) < 1f)
				{
					friendInfoRect.anchoredPosition = new Vector2(0f - reportButton.sizeDelta.x, 0f);
					animationType = AnimationType.Idle;
				}
			}
			else if (animationType == AnimationType.Hide)
			{
				friendInfoRect.anchoredPosition = Util.HalfDistance(friendInfoRect.anchoredPosition, Vector2.zero);
				if (Vector2.Distance(friendInfoRect.anchoredPosition, Vector2.zero) < 1f)
				{
					friendInfoRect.anchoredPosition = Vector2.zero;
					animationType = AnimationType.Idle;
				}
			}
		}

		private void OnListItemSlidedOpen(RosterFriendListItem aItem)
		{
			if (aItem != this)
			{
				animationType = AnimationType.Hide;
			}
		}

		public void SetInteractable(bool aInteractable)
		{
			interactable = aInteractable;
		}

		private void OnReportButtonClicked()
		{
			if (!interactable)
			{
				return;
			}
			listener.OnPanelToggled(true);
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			ReportAPlayerPanel reportAPlayerPanel = (ReportAPlayerPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.REPORT);
			OnPanelClosed closeEvent = null;
			closeEvent = delegate(BasePanel invoker)
			{
				if (invoker != null && listener != null)
				{
					invoker.PanelClosedEvent -= closeEvent;
					listener.OnPanelToggled(false);
				}
			};
			reportAPlayerPanel.PanelClosedEvent += closeEvent;
			reportAPlayerPanel.Init(user, "group_chat", null);
		}

		private void OnAddFriendButtonClicked()
		{
			if (!interactable)
			{
				return;
			}
			listener.OnPanelToggled(true);
			MonoSingleton<NativeKeyboardManager>.Instance.Keyboard.Hide();
			FriendResultPanel friendResultPanel = (FriendResultPanel)Singleton<PanelManager>.Instance.ShowPanel(Panels.FRIEND_RESULT);
			Analytics.LogFoundFriendPageView(user.DisplayName.Text, "group_chat");
			OnPanelClosed closeEvent = null;
			closeEvent = delegate(BasePanel invoker)
			{
				if (invoker != null && listener != null)
				{
					invoker.PanelClosedEvent -= closeEvent;
					listener.OnPanelToggled(false);
				}
			};
			friendResultPanel.PanelClosedEvent += closeEvent;
			bool flag = MixSession.User.OutgoingFriendInvitations.Any((IOutgoingFriendInvitation inv) => inv.Invitee.DisplayName.Text == user.DisplayName.Text);
			friendResultPanel.Init(!flag, new InvitableUser(user), FriendResultPanel.Location.GroupRosterFriend, listener.OnFriendInvited);
		}
	}
}
