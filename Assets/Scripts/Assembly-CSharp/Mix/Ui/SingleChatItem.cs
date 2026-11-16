using System;
using System.Linq;
using Avatar;
using Disney.Mix.SDK;
using Disney.MobileNetwork;
using Mix.AssetBundles;
using Mix.Assets;
using Mix.Avatar;
using Mix.Data;
using Mix.Entitlements;
using Mix.FakeFriend.Datatypes;
using Mix.Localization;
using Mix.Session;
using Mix.Session.Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Mix.Ui
{
	public class SingleChatItem : IBundleObject, IScrollItem
	{
		private enum AnimationType
		{
			Idle = 0,
			Show = 1,
			Hide = 2
		}

		private sealed class GenerateGameObject_003Ec__AnonStorey2B8
		{
			internal string imageTarget;

			internal EventHandler<AbstractAvatarChangedEventArgs> listener;

			internal SingleChatItem _003C_003Ef__this;

			internal void _003C_003Em__5C4(object sender, AbstractAvatarChangedEventArgs args)
			{
				if (MonoSingleton<AvatarManager>.Instance.IsNullOrDisposed())
				{
					return;
				}
				if (_003C_003Ef__this.avatarFGCleanup != null)
				{
					_003C_003Ef__this.avatarFGCleanup();
				}
				string text = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? "ChatContent/Avatar/ImageTarget" : "ChatContent/Avatar/ImageTarget_Geo");
				if (!_003C_003Ef__this.inst.IsNullOrDisposed())
				{
					Transform transform = _003C_003Ef__this.inst.transform.Find(text);
					if (!text.Equals(imageTarget) && !_003C_003Ef__this.imageTargetFG.IsNullOrDisposed() && !_003C_003Ef__this.imageTargetFG.gameObject.IsNullOrDisposed())
					{
						_003C_003Ef__this.imageTargetFG.gameObject.SetActive(false);
					}
					imageTarget = text;
					_003C_003Ef__this.imageTargetFG = transform;
					_003C_003Ef__this.imageTargetSizeFG = (int)transform.GetComponent<RectTransform>().rect.height;
					if (_003C_003Ef__this.imageTargetFG != null)
					{
						_003C_003Ef__this.avatarFGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, _003C_003Ef__this.imageTargetSizeFG, _003C_003Ef__this.SnapshotCallbackFG);
					}
				}
			}

			internal void _003C_003Em__5C5()
			{
				_003C_003Ef__this.foregroundUser.OnAvatarChanged -= _003C_003Ef__this.eventGenerator.GetEventHandler(_003C_003Ef__this.foregroundUser, listener);
			}
		}

		private sealed class GenerateGameObject_003Ec__AnonStorey2B9
		{
			internal string imageTargetFGTransform;

			internal SingleChatItem _003C_003Ef__this;
		}

		private sealed class GenerateGameObject_003Ec__AnonStorey2BA
		{
			internal EventHandler<AbstractAvatarChangedEventArgs> listener;

			internal GenerateGameObject_003Ec__AnonStorey2B9 _003C_003Ef__ref_0024697;

			internal SingleChatItem _003C_003Ef__this;

			internal void _003C_003Em__5C6(object sender, AbstractAvatarChangedEventArgs args)
			{
				string text = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? "ChatContent/AvatarFG/ImageTarget" : "ChatContent/AvatarFG/ImageTarget_Geo");
				Transform transform = _003C_003Ef__this.inst.transform.Find(text);
				if (!text.Equals(_003C_003Ef__ref_0024697.imageTargetFGTransform))
				{
					_003C_003Ef__this.imageTargetFG.gameObject.SetActive(false);
				}
				_003C_003Ef__ref_0024697.imageTargetFGTransform = text;
				_003C_003Ef__this.imageTargetFG = transform;
				_003C_003Ef__this.imageTargetSizeFG = (int)transform.GetComponent<RectTransform>().rect.height;
				if (_003C_003Ef__this.avatarFGCleanup != null)
				{
					_003C_003Ef__this.avatarFGCleanup();
				}
				if (_003C_003Ef__this.imageTargetFG != null && MonoSingleton<AvatarManager>.Instance != null)
				{
					_003C_003Ef__this.avatarFGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, _003C_003Ef__this.imageTargetSizeFG, _003C_003Ef__this.SnapshotCallbackFG);
				}
			}

			internal void _003C_003Em__5C7()
			{
				_003C_003Ef__this.foregroundUser.OnAvatarChanged -= _003C_003Ef__this.eventGenerator.GetEventHandler(_003C_003Ef__this.foregroundUser, listener);
			}
		}

		private sealed class GenerateGameObject_003Ec__AnonStorey2BB
		{
			internal string imageTarget;

			internal EventHandler<AbstractAvatarChangedEventArgs> listener;

			internal SingleChatItem _003C_003Ef__this;

			internal void _003C_003Em__5C8(object sender, AbstractAvatarChangedEventArgs args)
			{
				string text = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? "ChatContent/AvatarBG/ImageTarget" : "ChatContent/AvatarBG/ImageTarget_Geo");
				Transform transform = _003C_003Ef__this.inst.transform.Find(text);
				if (!text.Equals(imageTarget))
				{
					_003C_003Ef__this.imageTargetBG.gameObject.SetActive(false);
				}
				imageTarget = text;
				_003C_003Ef__this.imageTargetBG = transform;
				_003C_003Ef__this.imageTargetSizeBG = (int)transform.GetComponent<RectTransform>().rect.height;
				if (_003C_003Ef__this.avatarBGCleanup != null)
				{
					_003C_003Ef__this.avatarBGCleanup();
				}
				if (_003C_003Ef__this.imageTargetBG != null && MonoSingleton<AvatarManager>.Instance != null)
				{
					_003C_003Ef__this.avatarBGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, _003C_003Ef__this.imageTargetSizeBG, _003C_003Ef__this.SnapshotCallbackBG);
				}
			}

			internal void _003C_003Em__5C9()
			{
				_003C_003Ef__this.backgroundUser.OnAvatarChanged -= _003C_003Ef__this.eventGenerator.GetEventHandler(_003C_003Ef__this.backgroundUser, listener);
			}
		}

		public const byte ER = 32;

		private GameObject prefab;

		public IChatMessage message;

		public IChatThread thread;

		private GameObject inst;

		private RectTransform deleteBtn;

		private GameObject itemBackground;

		private ConversationController conversationController;

		private Transform imageTargetFG;

		private int imageTargetSizeFG;

		private IAvatarHolder foregroundUser;

		private Transform imageTargetBG;

		private int imageTargetSizeBG;

		private IAvatarHolder backgroundUser;

		private Action avatarFGCleanup;

		private Action avatarBGCleanup;

		private Action avatarFGUpdateCleanup;

		private Action avatarBGUpdateCleanup;

		private bool isFriend;

		private GameObject counterHolder;

		private Text counter;

		private SdkEvents eventGenerator = new SdkEvents();

		private SdkActions actionGenerator = new SdkActions();

		private Official_Account OAEntitlement;

		private Texture2D OATexture;

		private RectTransform chatItem;

		private bool sliding;

		private bool posioned;

		private bool touched;

		private Vector2 slideDelta = Vector2.zero;

		private AnimationType animationType;

		public SingleChatItem(GameObject aPrefab, IChatThread aChatThread, IChatMessage aMessage, ConversationController aConversationController)
		{
			prefab = aPrefab;
			message = aMessage;
			conversationController = aConversationController;
			thread = aChatThread;
			if (MonoSingleton<FakeFriendManager>.Instance.IsFake(thread))
			{
				isFriend = true;
			}
			else if (thread is IOneOnOneChatThread)
			{
				IOneOnOneChatThread oneOnOneChatThread = thread as IOneOnOneChatThread;
				isFriend = oneOnOneChatThread.IsOtherUserFriend();
			}
		}

		void IBundleObject.OnBundleAssetObject(UnityEngine.Object aGameObject, object aUserData)
		{
			if (this == null || inst.IsNullOrDisposed())
			{
				return;
			}
			OATexture = MonoSingleton<AssetManager>.Instance.GetBundleInstance(OAEntitlement.GetThumb()) as Texture2D;
			if (OATexture != null)
			{
				imageTargetFG = inst.transform.Find("ChatContent/Avatar/ImageTarget");
				imageTargetFG.gameObject.SetActive(true);
				Sprite sprite = Sprite.Create(OATexture, new Rect(0f, 0f, OATexture.width, OATexture.height), new Vector2(0f, 0f));
				if (sprite != null && imageTargetFG != null)
				{
					Image component = imageTargetFG.GetComponent<Image>();
					component.sprite = sprite;
				}
				if (thread is IOfficialAccountChatThread && !MixSession.User.IsFollowingOfficialAccount(((IOfficialAccountChatThread)thread).OfficialAccount.AccountId))
				{
					MonoSingleton<AvatarManager>.Instance.TintSnapshot(imageTargetFG.GetComponent<Image>(), 0.5f);
				}
			}
		}

		GameObject IScrollItem.GenerateGameObject(bool aGenerateForHeightOnly)
		{
			inst = UnityEngine.Object.Instantiate(prefab);
			if (!aGenerateForHeightOnly)
			{
				chatItem = inst.transform.Find("ChatContent").GetComponent<RectTransform>();
				Text aText = inst.transform.Find("ChatContent/AvatarNameText").GetComponent<Text>();
				Text component = inst.transform.Find("ChatContent/TimeStampText").GetComponent<Text>();
				Text component2 = inst.transform.Find("ChatContent/ChatText").GetComponent<Text>();
				counterHolder = inst.transform.Find("ChatContent/MessageCounter").gameObject;
				counter = inst.transform.Find("ChatContent/MessageCounter/MessageCounterText").GetComponent<Text>();
				string name = ((!(thread is IGroupChatThread)) ? "DeleteConvoBtn" : "LeaveGroupBtn");
				deleteBtn = inst.transform.Find(name).GetComponent<RectTransform>();
				itemBackground = inst.transform.Find("ItemBackground").gameObject;
				HideDeleteBtn();
				int num = component2.fontSize;
				component2.fontSize = ((num >= 48) ? 48 : num);
				if (thread is IOfficialAccountChatThread)
				{
					IOfficialAccountChatThread officialAccountChatThread = (IOfficialAccountChatThread)thread;
					OAEntitlement = Singleton<EntitlementsManager>.Instance.GetOfficialAccount(officialAccountChatThread.OfficialAccount.AccountId);
					if (OAEntitlement != null)
					{
						MonoSingleton<AssetManager>.Instance.LoadABundle(this, OAEntitlement.GetThumb(), null, string.Empty, true, false, true);
					}
				}
				else if (thread is IOneOnOneChatThread)
				{
					GenerateGameObject_003Ec__AnonStorey2B8 CS_0024_003C_003E8__locals35 = new GenerateGameObject_003Ec__AnonStorey2B8();
					CS_0024_003C_003E8__locals35._003C_003Ef__this = this;
					IOneOnOneChatThread oneOnOneChatThread = thread as IOneOnOneChatThread;
					foregroundUser = oneOnOneChatThread.GetOtherAvatarHolder();
					CS_0024_003C_003E8__locals35.imageTarget = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(foregroundUser.Avatar)) ? "ChatContent/Avatar/ImageTarget" : "ChatContent/Avatar/ImageTarget_Geo");
					imageTargetFG = inst.transform.Find(CS_0024_003C_003E8__locals35.imageTarget);
					imageTargetSizeFG = (int)imageTargetFG.GetComponent<RectTransform>().rect.height;
					avatarFGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(foregroundUser.Avatar, (AvatarFlags)0, imageTargetSizeFG, SnapshotCallbackFG);
					CS_0024_003C_003E8__locals35.listener = delegate(object sender, AbstractAvatarChangedEventArgs args)
					{
						if (!MonoSingleton<AvatarManager>.Instance.IsNullOrDisposed())
						{
							if (CS_0024_003C_003E8__locals35._003C_003Ef__this.avatarFGCleanup != null)
							{
								CS_0024_003C_003E8__locals35._003C_003Ef__this.avatarFGCleanup();
							}
							string text3 = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? "ChatContent/Avatar/ImageTarget" : "ChatContent/Avatar/ImageTarget_Geo");
							if (!CS_0024_003C_003E8__locals35._003C_003Ef__this.inst.IsNullOrDisposed())
							{
								Transform transform2 = CS_0024_003C_003E8__locals35._003C_003Ef__this.inst.transform.Find(text3);
								if (!text3.Equals(CS_0024_003C_003E8__locals35.imageTarget) && !CS_0024_003C_003E8__locals35._003C_003Ef__this.imageTargetFG.IsNullOrDisposed() && !CS_0024_003C_003E8__locals35._003C_003Ef__this.imageTargetFG.gameObject.IsNullOrDisposed())
								{
									CS_0024_003C_003E8__locals35._003C_003Ef__this.imageTargetFG.gameObject.SetActive(false);
								}
								CS_0024_003C_003E8__locals35.imageTarget = text3;
								CS_0024_003C_003E8__locals35._003C_003Ef__this.imageTargetFG = transform2;
								CS_0024_003C_003E8__locals35._003C_003Ef__this.imageTargetSizeFG = (int)transform2.GetComponent<RectTransform>().rect.height;
								if (CS_0024_003C_003E8__locals35._003C_003Ef__this.imageTargetFG != null)
								{
									CS_0024_003C_003E8__locals35._003C_003Ef__this.avatarFGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, CS_0024_003C_003E8__locals35._003C_003Ef__this.imageTargetSizeFG, CS_0024_003C_003E8__locals35._003C_003Ef__this.SnapshotCallbackFG);
								}
							}
						}
					};
					foregroundUser.OnAvatarChanged += eventGenerator.AddEventHandler(foregroundUser, CS_0024_003C_003E8__locals35.listener);
					avatarFGUpdateCleanup = delegate
					{
						CS_0024_003C_003E8__locals35._003C_003Ef__this.foregroundUser.OnAvatarChanged -= CS_0024_003C_003E8__locals35._003C_003Ef__this.eventGenerator.GetEventHandler(CS_0024_003C_003E8__locals35._003C_003Ef__this.foregroundUser, CS_0024_003C_003E8__locals35.listener);
					};
				}
				else
				{
					GenerateGameObject_003Ec__AnonStorey2B9 generateGameObject_003Ec__AnonStorey2B = new GenerateGameObject_003Ec__AnonStorey2B9();
					generateGameObject_003Ec__AnonStorey2B._003C_003Ef__this = this;
					ILocalUser user = MixSession.User;
					string text = null;
					string text2 = FindLastMemberWithMessage(thread, true, string.Empty);
					if (text2 == null)
					{
						text2 = FindLastMemberWithMessage(thread, false, string.Empty);
						if (text2 == null && thread.RemoteMembers.Any())
						{
							text2 = thread.RemoteMembers.First().Id;
						}
						if (text2 == null && thread.FormerRemoteMembers.Any())
						{
							text2 = thread.FormerRemoteMembers.First().Id;
						}
					}
					if (text2 == null)
					{
						foregroundUser = new AvatarHolder(user);
					}
					else
					{
						foregroundUser = new AvatarHolder(thread.GetRemoteMemberById(text2));
					}
					if (thread.RemoteMembers.Count() > 1)
					{
						text = FindLastMemberWithMessage(thread, true, text2);
					}
					if (text == null)
					{
						text = FindLastMemberWithMessage(thread, false, text2);
						if (text == null)
						{
							foreach (IRemoteChatMember remoteMember in thread.RemoteMembers)
							{
								if (remoteMember.Id != text2)
								{
									text = remoteMember.Id;
									break;
								}
							}
							if (text == null)
							{
								foreach (IRemoteChatMember formerRemoteMember in thread.FormerRemoteMembers)
								{
									if (formerRemoteMember.Id != text2)
									{
										text = formerRemoteMember.Id;
										break;
									}
								}
							}
						}
					}
					if (text == null)
					{
						backgroundUser = new AvatarHolder(user);
					}
					else
					{
						backgroundUser = new AvatarHolder(thread.GetRemoteMemberById(text));
					}
					generateGameObject_003Ec__AnonStorey2B.imageTargetFGTransform = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(foregroundUser.Avatar)) ? "ChatContent/AvatarFG/ImageTarget" : "ChatContent/AvatarFG/ImageTarget_Geo");
					imageTargetFG = inst.transform.Find(generateGameObject_003Ec__AnonStorey2B.imageTargetFGTransform);
					imageTargetSizeFG = (int)imageTargetFG.GetComponent<RectTransform>().rect.height;
					avatarFGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(foregroundUser.Avatar, (AvatarFlags)0, imageTargetSizeFG, SnapshotCallbackFG);
					if (foregroundUser != null)
					{
						GenerateGameObject_003Ec__AnonStorey2BA CS_0024_003C_003E8__locals51 = new GenerateGameObject_003Ec__AnonStorey2BA();
						CS_0024_003C_003E8__locals51._003C_003Ef__ref_0024697 = generateGameObject_003Ec__AnonStorey2B;
						CS_0024_003C_003E8__locals51._003C_003Ef__this = this;
						CS_0024_003C_003E8__locals51.listener = delegate(object sender, AbstractAvatarChangedEventArgs args)
						{
							string text3 = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? "ChatContent/AvatarFG/ImageTarget" : "ChatContent/AvatarFG/ImageTarget_Geo");
							Transform transform2 = CS_0024_003C_003E8__locals51._003C_003Ef__this.inst.transform.Find(text3);
							if (!text3.Equals(CS_0024_003C_003E8__locals51._003C_003Ef__ref_0024697.imageTargetFGTransform))
							{
								CS_0024_003C_003E8__locals51._003C_003Ef__this.imageTargetFG.gameObject.SetActive(false);
							}
							CS_0024_003C_003E8__locals51._003C_003Ef__ref_0024697.imageTargetFGTransform = text3;
							CS_0024_003C_003E8__locals51._003C_003Ef__this.imageTargetFG = transform2;
							CS_0024_003C_003E8__locals51._003C_003Ef__this.imageTargetSizeFG = (int)transform2.GetComponent<RectTransform>().rect.height;
							if (CS_0024_003C_003E8__locals51._003C_003Ef__this.avatarFGCleanup != null)
							{
								CS_0024_003C_003E8__locals51._003C_003Ef__this.avatarFGCleanup();
							}
							if (CS_0024_003C_003E8__locals51._003C_003Ef__this.imageTargetFG != null && MonoSingleton<AvatarManager>.Instance != null)
							{
								CS_0024_003C_003E8__locals51._003C_003Ef__this.avatarFGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, CS_0024_003C_003E8__locals51._003C_003Ef__this.imageTargetSizeFG, CS_0024_003C_003E8__locals51._003C_003Ef__this.SnapshotCallbackFG);
							}
						};
						foregroundUser.OnAvatarChanged += eventGenerator.AddEventHandler(foregroundUser, CS_0024_003C_003E8__locals51.listener);
						avatarFGUpdateCleanup = delegate
						{
							CS_0024_003C_003E8__locals51._003C_003Ef__this.foregroundUser.OnAvatarChanged -= CS_0024_003C_003E8__locals51._003C_003Ef__this.eventGenerator.GetEventHandler(CS_0024_003C_003E8__locals51._003C_003Ef__this.foregroundUser, CS_0024_003C_003E8__locals51.listener);
						};
					}
					if (backgroundUser != null)
					{
						GenerateGameObject_003Ec__AnonStorey2BB CS_0024_003C_003E8__locals67 = new GenerateGameObject_003Ec__AnonStorey2BB();
						CS_0024_003C_003E8__locals67._003C_003Ef__this = this;
						CS_0024_003C_003E8__locals67.imageTarget = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(backgroundUser.Avatar)) ? "ChatContent/AvatarBG/ImageTarget" : "ChatContent/AvatarBG/ImageTarget_Geo");
						imageTargetBG = inst.transform.Find(CS_0024_003C_003E8__locals67.imageTarget);
						imageTargetSizeBG = (int)imageTargetBG.GetComponent<RectTransform>().rect.height;
						avatarBGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(backgroundUser.Avatar, (AvatarFlags)0, imageTargetSizeBG, SnapshotCallbackBG);
						CS_0024_003C_003E8__locals67.listener = delegate(object sender, AbstractAvatarChangedEventArgs args)
						{
							string text3 = ((!MonoSingleton<AvatarManager>.Instance.AvatarHasGeo(args.Avatar)) ? "ChatContent/AvatarBG/ImageTarget" : "ChatContent/AvatarBG/ImageTarget_Geo");
							Transform transform2 = CS_0024_003C_003E8__locals67._003C_003Ef__this.inst.transform.Find(text3);
							if (!text3.Equals(CS_0024_003C_003E8__locals67.imageTarget))
							{
								CS_0024_003C_003E8__locals67._003C_003Ef__this.imageTargetBG.gameObject.SetActive(false);
							}
							CS_0024_003C_003E8__locals67.imageTarget = text3;
							CS_0024_003C_003E8__locals67._003C_003Ef__this.imageTargetBG = transform2;
							CS_0024_003C_003E8__locals67._003C_003Ef__this.imageTargetSizeBG = (int)transform2.GetComponent<RectTransform>().rect.height;
							if (CS_0024_003C_003E8__locals67._003C_003Ef__this.avatarBGCleanup != null)
							{
								CS_0024_003C_003E8__locals67._003C_003Ef__this.avatarBGCleanup();
							}
							if (CS_0024_003C_003E8__locals67._003C_003Ef__this.imageTargetBG != null && MonoSingleton<AvatarManager>.Instance != null)
							{
								CS_0024_003C_003E8__locals67._003C_003Ef__this.avatarBGCleanup = MonoSingleton<AvatarManager>.Instance.GetSnapshotFromDna(args.Avatar, (AvatarFlags)0, CS_0024_003C_003E8__locals67._003C_003Ef__this.imageTargetSizeBG, CS_0024_003C_003E8__locals67._003C_003Ef__this.SnapshotCallbackBG);
							}
						};
						backgroundUser.OnAvatarChanged += eventGenerator.AddEventHandler(backgroundUser, CS_0024_003C_003E8__locals67.listener);
						avatarBGUpdateCleanup = delegate
						{
							CS_0024_003C_003E8__locals67._003C_003Ef__this.backgroundUser.OnAvatarChanged -= CS_0024_003C_003E8__locals67._003C_003Ef__this.eventGenerator.GetEventHandler(CS_0024_003C_003E8__locals67._003C_003Ef__this.backgroundUser, CS_0024_003C_003E8__locals67.listener);
						};
					}
					else
					{
						inst.transform.Find("ChatContent/AvatarBG").gameObject.SetActive(false);
					}
				}
				aText.text = thread.GetThreadTitle();
				Util.EllipsizeText(ref aText);
				if (message != null)
				{
					bool flag = message.TimeSent >= DateTime.UtcNow.AddDays(-1.0);
					bool flag2 = message.TimeSent >= DateTime.UtcNow.AddDays(-2.0);
					bool flag3 = message.TimeSent >= DateTime.UtcNow.AddDays(-7.0);
					if (flag)
					{
						component.text = Singleton<Localizer>.Instance.getString("customtokens.conversations.today");
					}
					else if (flag3)
					{
						if (flag2)
						{
							component.text = Singleton<Localizer>.Instance.getString("customtokens.conversations.yesterday") + " " + message.TimeSent.ToLocalTime().ToString("h:mm tt");
						}
						else
						{
							component.text = message.TimeSent.ToLocalTime().ToString("dddd h:mm tt");
						}
					}
					else
					{
						component.text = message.TimeSent.ToLocalTime().ToString("ddd, MMM d, h:mm tt");
					}
				}
				else
				{
					component.text = string.Empty;
				}
				if (thread is IGroupChatThread && thread.MemberCount() <= 1 && thread.IsNicknamed())
				{
					component2.text = Singleton<Localizer>.Instance.getString("customtokens.chat.empty_thread_title");
				}
				else
				{
					component2.text = MessageUtils.GetDisplayTextForMessage(thread, message);
				}
				UpdateUnreadCount();
				UnityAction call = OnChatClicked;
				Transform transform = inst.transform.Find("ChatContent");
				transform.GetComponent<Button>().onClick.AddListener(call);
				UnityAction call2 = OnDeleteConvoClicked;
				deleteBtn.GetComponent<Button>().onClick.AddListener(call2);
				if (!(thread is FakeThread))
				{
					SlideRecognizer component3 = inst.GetComponent<SlideRecognizer>();
					component3.onSlide = (SlideRecognizer.OnSlide)Delegate.Combine(component3.onSlide, (SlideRecognizer.OnSlide)delegate(GameObject aGameObject, Vector2 aMoved)
					{
						if (chatItem != null)
						{
							Rect rectInScreenSpace = Util.GetRectInScreenSpace(chatItem);
							if (chatItem.anchoredPosition.x < 0f && !touched && rectInScreenSpace.Contains(UnityEngine.InputSystem.Mouse.current != null ? UnityEngine.InputSystem.Mouse.current.position.ReadValue() : Vector2.zero))
							{
								animationType = AnimationType.Hide;
							}
							else
							{
								touched = true;
								slideDelta += aMoved;
								if (!posioned && Mathf.Abs(slideDelta.x) > 15f && Mathf.Abs(slideDelta.y) < 10f)
								{
									sliding = true;
									ShowDeleteBtn();
									DisableScroll();
								}
								else if (Mathf.Abs(slideDelta.y) >= 10f)
								{
									posioned = true;
								}
								if (sliding)
								{
									chatItem.anchoredPosition = new Vector2((!(slideDelta.x > 0f)) ? slideDelta.x : 0f, 0f);
								}
							}
						}
					});
					component3.onSlideComplete = (SlideRecognizer.OnSlideComplete)Delegate.Combine(component3.onSlideComplete, (SlideRecognizer.OnSlideComplete)delegate(GameObject aGameObject)
					{
						sliding = false;
						touched = false;
						posioned = false;
						slideDelta = Vector2.zero;
						EnableScroll();
						if (chatItem != null)
						{
							if (chatItem.anchoredPosition.x < (0f - deleteBtn.sizeDelta.x) / 2f)
							{
								animationType = AnimationType.Show;
							}
							else
							{
								animationType = AnimationType.Hide;
							}
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
				Service.Get<SystemTextManager>().GenerateSystemText(component2, new Color32(230, 233, 239, 0));
				inst.SetActive(false);
				inst.SetActive(true);
			}
			return inst;
		}

		void IScrollItem.Destroy()
		{
			if (MonoSingleton<AvatarManager>.Instance != null)
			{
				if (avatarFGCleanup != null)
				{
					avatarFGCleanup();
				}
				if (avatarBGCleanup != null)
				{
					avatarBGCleanup();
				}
			}
			if (avatarFGUpdateCleanup != null)
			{
				avatarFGUpdateCleanup();
			}
			if (avatarBGUpdateCleanup != null)
			{
				avatarBGUpdateCleanup();
			}
			if (MonoSingleton<AssetManager>.Instance != null && OAEntitlement != null && OATexture != null)
			{
				MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(OAEntitlement.GetThumb());
			}
		}

		public void FixedUpdate()
		{
			if (chatItem.IsNullOrDisposed())
			{
				return;
			}
			if (animationType == AnimationType.Show)
			{
				chatItem.anchoredPosition = Util.HalfDistance(chatItem.anchoredPosition, new Vector2(0f - deleteBtn.sizeDelta.x, 0f));
				if (Vector2.Distance(chatItem.anchoredPosition, new Vector2(0f - deleteBtn.sizeDelta.x, 0f)) < 1f)
				{
					chatItem.anchoredPosition = new Vector2(0f - deleteBtn.sizeDelta.x, 0f);
					animationType = AnimationType.Idle;
				}
			}
			else if (animationType == AnimationType.Hide)
			{
				chatItem.anchoredPosition = Util.HalfDistance(chatItem.anchoredPosition, Vector2.zero);
				if (Vector2.Distance(chatItem.anchoredPosition, Vector2.zero) < 1f)
				{
					chatItem.anchoredPosition = Vector2.zero;
					animationType = AnimationType.Idle;
					HideDeleteBtn();
				}
			}
		}

		public void ResetItemTitle()
		{
			if (inst != null && (bool)inst.transform.Find("ChatContent/AvatarNameText").GetComponent<Text>())
			{
				Text aText = inst.transform.Find("ChatContent/AvatarNameText").GetComponent<Text>();
				aText.text = thread.GetThreadTitle();
				Util.EllipsizeText(ref aText);
			}
		}

		public void setAvatarTint(float aTint)
		{
			MonoSingleton<AvatarManager>.Instance.TintSnapshot(imageTargetFG.GetComponent<Image>(), aTint);
		}

		public void UpdateUnreadCount()
		{
			if (counterHolder == null)
			{
				return;
			}
			uint num = thread.UnreadMessageCount;
			if (num != 0)
			{
				if (num > 99)
				{
					num = 99u;
				}
				Text text = counter;
				string text2 = num.ToString();
				counter.text = text2;
				text.text = text2;
				counter.gameObject.SetActive(true);
				counterHolder.SetActive(true);
			}
			else
			{
				counterHolder.SetActive(false);
			}
		}

		private void DisableScroll()
		{
			ScrollRect component = conversationController.ScrollView.gameObject.GetComponent<ScrollRect>();
			component.vertical = false;
		}

		private void EnableScroll()
		{
			ScrollRect component = conversationController.ScrollView.gameObject.GetComponent<ScrollRect>();
			component.vertical = true;
		}

		private void ShowDeleteBtn()
		{
			if (!deleteBtn.gameObject.activeSelf)
			{
				deleteBtn.gameObject.SetActive(true);
				itemBackground.gameObject.SetActive(true);
			}
		}

		private void HideDeleteBtn()
		{
			if (deleteBtn.gameObject.activeSelf)
			{
				deleteBtn.gameObject.SetActive(false);
				itemBackground.gameObject.SetActive(false);
			}
		}

		private void OnChatClicked()
		{
			if (!(chatItem != null))
			{
				return;
			}
			if (chatItem.anchoredPosition.Equals(Vector2.zero))
			{
				if (thread.ThreadRequiresParentalConsent())
				{
					ParentalConsent.ShowGateDialog();
					return;
				}
				conversationController.OnTransitionOutBegin();
				ChatHelper.NavigateToChatScreen(thread, new TransitionSlideLeft());
				Analytics.LogNavigationAction("history", "chat");
			}
			else
			{
				animationType = AnimationType.Hide;
			}
		}

		private void OnDeleteConvoClicked()
		{
			if (MixSession.connection != MixSession.ConnectionState.ONLINE)
			{
				return;
			}
			if (thread is IOfficialAccountChatThread)
			{
				((IOfficialAccountChatThread)thread).ClearChatHistory(actionGenerator.CreateAction(delegate(IClearChatHistoryResult e)
				{
					if (!e.Success)
					{
					}
					thread.ClearUnreadMessageCount(actionGenerator.CreateAction<IClearUnreadMessageCountResult>(delegate
					{
						conversationController.DeleteScrollViewItem(this);
					}));
				}));
			}
			else if (thread is IGroupChatThread)
			{
				MixSession.User.RemoveChatThreadMember((IGroupChatThread)thread, MixSession.User, actionGenerator.CreateAction<IRemoveChatThreadMemberResult>(delegate
				{
					conversationController.DeleteScrollViewItem(this);
				}));
			}
			else
			{
				((IOneOnOneChatThread)thread).ClearChatHistory(actionGenerator.CreateAction(delegate(IClearChatHistoryResult e)
				{
					if (!e.Success)
					{
					}
					thread.ClearUnreadMessageCount(actionGenerator.CreateAction<IClearUnreadMessageCountResult>(delegate
					{
						conversationController.DeleteScrollViewItem(this);
					}));
				}));
			}
			MonoSingleton<ChatPrimerManager>.Instance.OnThreadDeleted(thread);
			deleteBtn.GetComponent<Button>().enabled = false;
			deleteBtn.Find("ContextualLoader").gameObject.SetActive(true);
			deleteBtn.Find("Icon").gameObject.SetActive(false);
		}

		private string FindLastMemberWithMessage(IChatThread thread, bool currentMembersOnly, string ignoreId = "")
		{
			ILocalUser user = MixSession.User;
			for (int num = thread.ChatMessages.Count() - 1; num >= 0; num--)
			{
				string senderId = thread.ChatMessages.ElementAt(num).SenderId;
				if (senderId != null && user.Id != senderId && ignoreId != senderId && ((!currentMembersOnly) ? thread.IsOrWasThreadMember(senderId) : thread.IsThreadMember(senderId)))
				{
					return senderId;
				}
			}
			return null;
		}

		public void SnapshotCallbackFG(bool success, Sprite sprite)
		{
			avatarFGCleanup = null;
			if (sprite != null && imageTargetFG != null && imageTargetFG.GetComponent<Image>() != null)
			{
				imageTargetFG.GetComponent<Image>().sprite = sprite;
				if (!(thread is IGroupChatThread) && !isFriend)
				{
					MonoSingleton<AvatarManager>.Instance.TintSnapshot(imageTargetFG.GetComponent<Image>(), 0.5f);
				}
				imageTargetFG.gameObject.SetActive(true);
			}
		}

		public void SnapshotCallbackBG(bool success, Sprite sprite)
		{
			avatarBGCleanup = null;
			if (sprite != null && imageTargetBG != null && imageTargetBG.GetComponent<Image>() != null)
			{
				imageTargetBG.GetComponent<Image>().sprite = sprite;
				imageTargetBG.gameObject.SetActive(true);
			}
		}
	}
}
