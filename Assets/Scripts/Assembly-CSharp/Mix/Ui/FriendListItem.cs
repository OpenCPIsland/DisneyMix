using System;
using System.Collections.Generic;
using Avatar;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Mix.Avatar;
using Mix.Native;
using Mix.Session;
using Mix.Session.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class FriendListItem : BaseFriendsListItem, IScrollItem, IScrollItemHelper
	{
		protected enum AnimationType
		{
			Idle = 0,
			Show = 1,
			Hide = 2
		}

		protected delegate void OnFriendListItemSlideOpen(FriendListItem aItem);

		public IFriend friend;

		public bool selected;

		protected Toggle toggle;

		protected GameObject prefab;

		protected IFriendListItem listener;

		protected string layer;

		protected GameObject instance;

		protected RectTransform friendInfoRect;

		protected RectTransform swipeOptions;

		protected GameObject swipeOptionsObject;

		protected GameObject friendItemBackground;

		private Action<FriendListItem> toggleListener;

		private bool disabled;

		private bool removeEditNickname;

		private Action cancelSnapshot;

		private Action avatarUpdateCleanup;

		private int imageTargetSize;

		private SnapshotCallback snapshotDelegate;

		private SdkEvents eventGenerator = new SdkEvents();

		private SdkActions actionGenerator = new SdkActions();

		private bool sliding;

		private bool posioned;

		private bool touched;

		private Vector2 slideDelta = Vector2.zero;

		protected AnimationType animationType;

		public string Name { get; protected set; }

		public bool Disabled
		{
			get
			{
				return disabled;
			}
			set
			{
				disabled = value;
				if (toggle != null)
				{
					toggle.interactable = !value;
				}
			}
		}

		public override string SortName
		{
			get
			{
				return friend.NickFirstOrDisplayName();
			}
		}

		protected static event OnFriendListItemSlideOpen OnFriendsListItemSlidedOpenCallback;

		public FriendListItem(GameObject aPrefab, IFriendListItem aListener, string aLayer = "UI")
		{
			prefab = aPrefab;
			listener = aListener;
			layer = aLayer;
		}

		public FriendListItem(GameObject aPrefab, IFriend friend, IFriendListItem aListener, Action<FriendListItem> aToggleListener = null, bool aDisabled = false, string aLayer = "UI", bool aSelected = false)
		{
			prefab = aPrefab;
			this.friend = friend;
			Name = friend.NickFirstOrDisplayName();
			listener = aListener;
			toggleListener = aToggleListener;
			Disabled = aDisabled;
			selected = aSelected;
			layer = aLayer;
		}

		public void RemoveEditNickname(bool aState)
		{
			removeEditNickname = aState;
		}

		public float GetGameObjectHeight()
		{
			return BaseFriendsListItem.GAMEOBJECT_HEIGHT;
		}

		public override void FixedUpdate()
		{
			if (animationType == AnimationType.Show)
			{
				float num = Mathf.Abs(swipeOptions.sizeDelta.x);
				friendInfoRect.anchoredPosition = Util.HalfDistance(friendInfoRect.anchoredPosition, new Vector2(0f - num, 0f));
				if (Vector2.Distance(friendInfoRect.anchoredPosition, new Vector2(0f - num, 0f)) < 1f)
				{
					friendInfoRect.anchoredPosition = new Vector2(0f - num, 0f);
					animationType = AnimationType.Idle;
				}
			}
			else if (animationType == AnimationType.Hide && friendInfoRect != null)
			{
				friendInfoRect.anchoredPosition = Util.HalfDistance(friendInfoRect.anchoredPosition, Vector2.zero);
				if (Vector2.Distance(friendInfoRect.anchoredPosition, Vector2.zero) < 1f)
				{
					friendInfoRect.anchoredPosition = Vector2.zero;
					swipeOptionsObject.SetActive(false);
					friendItemBackground.SetActive(false);
					animationType = AnimationType.Idle;
				}
			}
		}

		protected void OnListItemSlidedOpen(object aItem)
		{
			if (aItem != this)
			{
				animationType = AnimationType.Hide;
			}
		}

		public virtual GameObject GenerateGameObject(bool aGenerateForHeightOnly)
		{
			instance = UnityEngine.Object.Instantiate(prefab);
			if (BaseFriendsListItem.GAMEOBJECT_HEIGHT < 1f)
			{
				BaseFriendsListItem.GAMEOBJECT_HEIGHT = instance.GetComponent<RectTransform>().sizeDelta.y;
			}
			if (aGenerateForHeightOnly)
			{
				return instance;
			}
			FriendListItem.OnFriendsListItemSlidedOpenCallback = (OnFriendListItemSlideOpen)Delegate.Combine(FriendListItem.OnFriendsListItemSlidedOpenCallback, new OnFriendListItemSlideOpen(OnListItemSlidedOpen));
			RefreshName();
			SetTrust(friend != null && friend.IsTrusted);
			friendInfoRect = instance.transform.Find("FriendInfo").GetComponent<RectTransform>();
			swipeOptions = instance.transform.Find("SwipeOptions").GetComponent<RectTransform>();
			swipeOptionsObject = instance.transform.Find("SwipeOptions").gameObject;
			friendItemBackground = instance.transform.Find("ItemBackground").gameObject;
			swipeOptions.transform.Find("DeleteFriendBtn").GetComponent<Button>().interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			swipeOptions.transform.Find("EditNicknameBtn").GetComponent<Button>().interactable = MixSession.connection == MixSession.ConnectionState.ONLINE;
			MixSession.OnConnectionChanged += HandleConnectionChange;
			Util.SetLayerRecursively(instance, LayerMask.NameToLayer(layer));
			if (toggleListener == null)
			{
				instance.GetComponent<Button>().onClick.AddListener(delegate
				{
					if (friendInfoRect.anchoredPosition.Equals(Vector2.zero))
					{
						listener.OnFriendListItemClicked(friend);
					}
					else
					{
						animationType = AnimationType.Hide;
					}
				});
			}
			else
			{
				toggle = instance.transform.Find("FriendInfo/SelectedToggle").GetComponent<Toggle>();
				instance.transform.Find("FriendInfo/SelectedToggle").gameObject.SetActive(true);
				toggle.isOn = selected;
				if (disabled)
				{
					toggle.interactable = false;
				}
				instance.GetComponent<Button>().onClick.AddListener(delegate
				{
					if (!disabled)
					{
						toggle.isOn = !toggle.isOn;
					}
				});
				toggle.onValueChanged.AddListener(delegate(bool aState)
				{
					selected = aState;
					toggleListener(this);
				});
			}
			instance.transform.Find("OptionBars/DeleteConfirmBar/AcceptBtn").GetComponent<Button>().onClick.AddListener(OnDeleteFriend);
			instance.transform.Find("OptionBars/UpgradeConfirmBar/AcceptBtn").GetComponent<Button>().onClick.AddListener(OnUpgradeTrust);
			if (toggleListener != null)
			{
				instance.transform.Find("FriendInfo/RequestTrustBtn").GetComponent<Button>().enabled = false;
			}
			else
			{
				instance.transform.Find("FriendInfo/RequestTrustBtn").GetComponent<Button>().onClick.AddListener(OnUpgradeTrustRequested);
				instance.transform.Find("OptionBars/AskForOpenChatBar/ProceedBtn").GetComponent<Button>().onClick.AddListener(OnAskForOpenChatClicked);
			}
			instance.transform.Find("FriendInfo/RequestTrustBtn/PendingIcon").gameObject.SetActive(false);
			if (removeEditNickname)
			{
				swipeOptions.Find("EditNicknameBtn").gameObject.SetActive(false);
			}
			else
			{
				swipeOptions.Find("EditNicknameBtn").GetComponent<Button>().onClick.AddListener(OnEditNicknameShown);
				instance.transform.Find("OptionBars/NicknameInputBar/NicknameInput").GetComponent<NativeTextView>().KeyboardHeightChanged += OnKeyboardHeightChanged;
				instance.transform.Find("OptionBars/NicknameInputBar/NicknameInput").GetComponent<NativeTextView>().KeyboardFocusChanged += OnKeyboardFocusChanged;
			}
			Text component = instance.transform.Find("FriendInfo/FriendNameText").GetComponent<Text>();
			int num = component.fontSize;
			component.fontSize = ((num >= 48) ? 48 : num);
			Transform imageTarget = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(friend.Avatar)) ? instance.transform.Find("FriendInfo/AvatarImage/ImageTarget") : instance.transform.Find("FriendInfo/AvatarImage/Mask/ImageTarget_Geo"));
			imageTargetSize = (int)imageTarget.GetComponent<RectTransform>().rect.height;
			snapshotDelegate = delegate(bool success, Sprite sprite)
			{
				imageTarget = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(friend.Avatar)) ? instance.transform.Find("FriendInfo/AvatarImage/ImageTarget") : instance.transform.Find("FriendInfo/AvatarImage/Mask/ImageTarget_Geo"));
				imageTargetSize = (int)imageTarget.GetComponent<RectTransform>().rect.height;
				cancelSnapshot = null;
				if (sprite != null && imageTarget != null && imageTarget.GetComponent<Image>() != null)
				{
					imageTarget.GetComponent<Image>().sprite = sprite;
					imageTarget.gameObject.SetActive(true);
				}
			};
			cancelSnapshot = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(friend.Avatar, (AvatarFlags)0, imageTargetSize, snapshotDelegate);
			EventHandler<AbstractAvatarChangedEventArgs> avatarUpdater = delegate(object sender, AbstractAvatarChangedEventArgs args)
			{
				Transform transform = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(friend.Avatar)) ? instance.transform.Find("FriendInfo/AvatarImage/ImageTarget") : instance.transform.Find("FriendInfo/AvatarImage/Mask/ImageTarget_Geo"));
				if (!transform.Equals(imageTarget))
				{
					imageTarget.gameObject.SetActive(false);
				}
				imageTarget = transform;
				imageTargetSize = (int)imageTarget.GetComponent<RectTransform>().rect.height;
				if (cancelSnapshot != null)
				{
					cancelSnapshot();
				}
				if (imageTarget != null && MonoSingleton<AvatarManager>.Instance != null)
				{
					cancelSnapshot = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, imageTargetSize, snapshotDelegate);
				}
			};
			friend.OnAvatarChanged += eventGenerator.AddEventHandler(friend, avatarUpdater);
			avatarUpdateCleanup = delegate
			{
				friend.OnAvatarChanged -= eventGenerator.GetEventHandler(friend, avatarUpdater);
			};
			if (!MonoSingleton<FakeFriendManager>.Instance.IsFake(friend) && toggleListener == null)
			{
				SetupSlide();
			}
			return instance;
		}

		public virtual void Destroy()
		{
			if (cancelSnapshot != null)
			{
				cancelSnapshot();
			}
			if (avatarUpdateCleanup != null)
			{
				avatarUpdateCleanup();
			}
			FriendListItem.OnFriendsListItemSlidedOpenCallback = (OnFriendListItemSlideOpen)Delegate.Remove(FriendListItem.OnFriendsListItemSlidedOpenCallback, new OnFriendListItemSlideOpen(OnListItemSlidedOpen));
			MixSession.OnConnectionChanged -= HandleConnectionChange;
		}

		protected void SetupSlide()
		{
			SlideRecognizer slideRecognizer = instance.AddComponent<SlideRecognizer>();
			slideRecognizer.onSlide = (SlideRecognizer.OnSlide)Delegate.Combine(slideRecognizer.onSlide, (SlideRecognizer.OnSlide)delegate(GameObject aGameObject, Vector2 aMoved)
			{
				Rect rectInScreenSpace = Util.GetRectInScreenSpace(friendInfoRect);
				if (friendInfoRect.anchoredPosition.x < 0f && !touched && rectInScreenSpace.Contains(UnityEngine.InputSystem.Pointer.current != null ? UnityEngine.InputSystem.Pointer.current.position.ReadValue() : Vector2.zero))
				{
					animationType = AnimationType.Hide;
				}
				else
				{
					if (!touched && FriendListItem.OnFriendsListItemSlidedOpenCallback != null)
					{
						FriendListItem.OnFriendsListItemSlidedOpenCallback(this);
					}
					touched = true;
					swipeOptionsObject.SetActive(true);
					friendItemBackground.SetActive(true);
					slideDelta += aMoved;
					if (!posioned && Mathf.Abs(slideDelta.x) > 15f && Mathf.Abs(slideDelta.y) < 10f)
					{
						sliding = true;
						if (listener != null)
						{
							listener.OnToggleScroll(false);
						}
					}
					else if (Mathf.Abs(slideDelta.y) >= 10f)
					{
						posioned = true;
					}
					if (sliding)
					{
						friendInfoRect.anchoredPosition = new Vector2((!(slideDelta.x > 0f)) ? slideDelta.x : 0f, 0f);
					}
				}
			});
			slideRecognizer.onSlideComplete = (SlideRecognizer.OnSlideComplete)Delegate.Combine(slideRecognizer.onSlideComplete, (SlideRecognizer.OnSlideComplete)delegate(GameObject aGameObject)
			{
				sliding = false;
				touched = false;
				posioned = false;
				slideDelta = Vector2.zero;
				if (listener != null)
				{
					listener.OnToggleScroll(true);
				}
				if (friendInfoRect.anchoredPosition.x < (0f - Mathf.Abs(swipeOptions.sizeDelta.x)) / 2f)
				{
					animationType = AnimationType.Show;
				}
				else
				{
					animationType = AnimationType.Hide;
				}
				if (aGameObject.name == "open")
				{
					animationType = AnimationType.Show;
				}
				if (aGameObject.name == "close")
				{
					animationType = AnimationType.Hide;
				}
			});
		}

		public void HandleConnectionChange(MixSession.ConnectionState newState, MixSession.ConnectionState oldState)
		{
			swipeOptions.transform.Find("DeleteFriendBtn").GetComponent<Button>().interactable = newState == MixSession.ConnectionState.ONLINE;
			swipeOptions.transform.Find("EditNicknameBtn").GetComponent<Button>().interactable = newState == MixSession.ConnectionState.ONLINE;
		}

		public bool Selected()
		{
			if (instance == null)
			{
				return false;
			}
			toggle = instance.transform.Find("FriendInfo/SelectedToggle").GetComponent<Toggle>();
			return toggle.isOn && toggle.interactable;
		}

		public void RefreshName(bool aOnInit = true, string aName = null)
		{
			if (instance != null)
			{
				instance.transform.Find("FriendInfo/FriendNameText").GetComponent<Text>().text = Name;
				Transform transform = instance.transform.Find("FriendInfo/DisplayNameText");
				if (string.IsNullOrEmpty(friend.FirstName))
				{
					transform.gameObject.SetActive(false);
				}
				else
				{
					instance.transform.Find("FriendInfo/DisplayNameText").GetComponent<Text>().text = MixChat.FormatDisplayName(friend.DisplayName.Text);
					transform.gameObject.SetActive(true);
				}
				instance.transform.Find("OptionBars/DeleteConfirmBar/DisplayNameText").GetComponent<Text>().text = Name;
				instance.transform.Find("OptionBars/UpgradeConfirmBar/DisplayNameText").GetComponent<Text>().text = Name;
				if (!aOnInit)
				{
					string text = friend.NickFirstOrDisplayName();
					instance.transform.Find("OptionBars/NicknameInputBar/NicknameInput").GetComponent<NativeTextView>().Value = ((aName == null) ? text : aName);
				}
			}
		}

		public void SetTrust(bool aTrusted)
		{
			if (instance != null)
			{
				if (LoadingController.HideManagePC)
				{
					bool flag = MixSession.IsValidSession && MixSession.User.AgeBandType != AgeBandType.Child;
					instance.transform.Find("FriendInfo/RequestTrustBtn").gameObject.SetActive(!aTrusted && flag);
				}
				else
				{
					instance.transform.Find("FriendInfo/RequestTrustBtn").gameObject.SetActive(!aTrusted && MixSession.IsValidSession);
				}
			}
		}

		public void OnEditNicknameShown()
		{
			if (instance != null)
			{
				RefreshName(false);
				instance.transform.Find("OptionBars/NicknameInputBar/NicknameInput").GetComponent<NativeTextView>().Invoke("SelectInput", 0.5f);
			}
		}

		private void OnDeleteFriend()
		{
			if (listener != null)
			{
				listener.OnFriendListRemoveClicked(friend);
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
			}
		}

		private void OnAskForOpenChatClicked()
		{
			NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Settings/SettingsScreen", new TransitionAnimations());
			navigationRequest.AddData("setupPC", true);
			MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
		}

		private void OnUpgradeTrustRequested()
		{
			ILocalUser me = MixSession.User;
			if (me.AgeBandType == AgeBandType.Child)
			{
				LocalLinkedUser localLinkedUser = new LocalLinkedUser();
				localLinkedUser.Swid = me.Id;
				localLinkedUser.AgeBand = me.AgeBandType;
				MixSession.User.GetChildTrustPermission(localLinkedUser, delegate(IPermissionResult permissionResult)
				{
					if (permissionResult.Success)
					{
						me.GetAdultVerificationRequirements(delegate(IGetAdultVerificationRequirementsResult getAdultVerificationRequirements)
						{
							if (getAdultVerificationRequirements.Success)
							{
								if (!getAdultVerificationRequirements.IsRequired)
								{
									if (permissionResult.Status == ActivityApprovalStatus.Approved)
									{
										instance.transform.Find("OptionBars").gameObject.SetActive(true);
										instance.transform.Find("OptionBars/UpgradeConfirmBar").gameObject.SetActive(true);
									}
									else
									{
										instance.transform.Find("OptionBars").gameObject.SetActive(true);
										instance.transform.Find("OptionBars/AskForOpenChatBar").gameObject.SetActive(true);
									}
								}
								else
								{
									me.GetLinkedGuardians(delegate(IGetLinkedUsersResult getLinkedUsersResult)
									{
										if (getLinkedUsersResult.Success)
										{
											IEnumerable<ILinkedUser> linkedUsers = getLinkedUsersResult.LinkedUsers;
											IEnumerator<ILinkedUser> enumerator = linkedUsers.GetEnumerator();
											if (enumerator.MoveNext())
											{
												if (permissionResult.Status == ActivityApprovalStatus.Approved)
												{
													instance.transform.Find("OptionBars").gameObject.SetActive(true);
													instance.transform.Find("OptionBars/UpgradeConfirmBar").gameObject.SetActive(true);
												}
												else if (permissionResult.Status == ActivityApprovalStatus.Denied)
												{
													instance.transform.Find("OptionBars").gameObject.SetActive(true);
													instance.transform.Find("OptionBars/SafeChatSetBar").gameObject.SetActive(true);
												}
												else
												{
													instance.transform.Find("OptionBars").gameObject.SetActive(true);
													instance.transform.Find("OptionBars/AskForOpenChatBar").gameObject.SetActive(true);
												}
											}
											else
											{
												instance.transform.Find("OptionBars").gameObject.SetActive(true);
												instance.transform.Find("OptionBars/AskForOpenChatBar").gameObject.SetActive(true);
											}
										}
									});
								}
							}
						});
					}
				});
			}
			else
			{
				instance.transform.Find("OptionBars").gameObject.SetActive(true);
				instance.transform.Find("OptionBars/UpgradeConfirmBar").gameObject.SetActive(true);
			}
		}

		private void OnUpgradeTrust()
		{
			if (listener != null)
			{
				listener.OnFriendListSendTrustInviteClicked(friend);
				Singleton<SoundManager>.Instance.PlaySoundEvent("MainApp/UI/SFX_2_SendDisney");
			}
		}

		protected virtual void OnKeyboardFocusChanged(NativeTextView aField, bool aState)
		{
			if (!aState && instance != null)
			{
				UpdateNickname();
				instance.transform.Find("OptionBars/NicknameInputBar").GetComponent<Animator>().Play("Bar_SlideOut");
				animationType = AnimationType.Hide;
			}
		}

		private void UpdateNickname()
		{
			if (instance != null)
			{
				string value = instance.transform.Find("OptionBars/NicknameInputBar/NicknameInput").GetComponent<NativeTextView>().Value;
				if (!string.IsNullOrEmpty(value.Trim()))
				{
					MixSession.User.SetNickname(friend, value, actionGenerator.CreateAction<ISetUserNicknameResult>(delegate
					{
					}));
					Name = value;
					RefreshName(false, Name);
				}
				else
				{
					MixSession.User.RemoveNickname(friend, actionGenerator.CreateAction<IRemoveUserNicknameResult>(delegate
					{
					}));
					Name = friend.FirstName;
					RefreshName(false, string.Empty);
				}
				Analytics.LogAddNicknameAction(friend.Id);
			}
		}

		private void OnKeyboardHeightChanged(NativeTextView aField, int aHeight)
		{
			if (instance != null)
			{
				listener.OnScrollToListItem(instance.GetComponent<RectTransform>(), aHeight);
			}
		}
	}
}
