using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Disney.Mix.SDK.Internal.GuestControllerDomain;
using Disney.Mix.SDK.Internal.MixDomain;

namespace Disney.Mix.SDK.Internal
{
    public class LocalUser : IInternalLocalUser, ILocalUser
    {
        private const string TrustPermissionCode = "MIX_TRUSTEDFRIENDSCOMMUNICATIONS";

        private readonly IChatThreadCreator chatThreadCreator;

        private readonly IList<IInternalIncomingFriendInvitation> incomingFriendInvitations;

        private readonly IList<IInternalOutgoingFriendInvitation> outgoingFriendInvitations;

        private readonly IList<long> oldInvitationIds;

        private readonly IList<IInternalFriend> friends;

        private readonly IList<IInternalOneOnOneChatThread> oneOnOneChatThreads;

        private readonly IList<IInternalGroupChatThread> groupChatThreads;

        private readonly IList<IInternalOfficialAccountChatThread> officialAccountChatThreads;

        private readonly IList<IInternalUnidentifiedUser> unidentifiedUsers;

        private readonly AgeBandType ageBandType;

        private readonly IChatMessageFactory chatMessageFactory;

        private readonly IDatabase database;

        private readonly IUserDatabase userDatabase;

        private readonly IChatMessageParser chatMessageParser;

        private readonly IList<IOfficialAccount> followships;

        private readonly AbstractLogger logger;

        private readonly IInternalRegistrationProfile registrationProfile;

        private readonly IMixWebCallFactory mixWebCallFactory;

        private readonly IPhotoFlavorFactory photoFlavorFactory;

        private readonly IVideoFlavorFactory videoFlavorFactory;

        private readonly IGuestControllerClient guestControllerClient;

        private readonly INotificationQueue notificationQueue;

        private readonly IEncryptor encryptor;

        private readonly IEpochTime epochTime;

        public string Swid { get; private set; }

        public string Id { get; private set; }

        public string HashedId
        {
            get
            {
                return userDatabase.GetUserBySwid(Swid).HashedSwid;
            }
        }

        public IDisplayName DisplayName { get; private set; }

        public string FirstName { get; private set; }

        public IAvatar Avatar
        {
            get
            {
                return InternalAvatar;
            }
        }

        public IInternalAvatar InternalAvatar
        {
            get
            {
                UserDocument userBySwid = userDatabase.GetUserBySwid(Swid);
                AvatarDocument avatar = userDatabase.GetAvatar(userBySwid.AvatarId);
                return AvatarBuilder.Build(avatar);
            }
        }

        public IEnumerable<IAlert> Alerts
        {
            get
            {
                return userDatabase.GetAlerts().Cast<IAlert>().ToArray();
            }
        }

        public IEnumerable<IFriend> Friends
        {
            get
            {
                return friends.ToArray();
            }
        }

        public IEnumerable<IIncomingFriendInvitation> IncomingFriendInvitations
        {
            get
            {
                return incomingFriendInvitations.ToArray();
            }
        }

        public IEnumerable<IOutgoingFriendInvitation> OutgoingFriendInvitations
        {
            get
            {
                return outgoingFriendInvitations.ToArray();
            }
        }

        public IEnumerable<IOfficialAccount> Followships
        {
            get
            {
                return followships.ToArray();
            }
        }

        public IEnumerable<IOfficialAccount> AllOfficialAccounts
        {
            get
            {
                return from doc in userDatabase.GetAllOfficialAccounts()
                       select OfficialAccountFactory.CreateOfficialAccount(doc.AccountId, doc.DisplayName, doc.IsAvailable, doc.CanUnfollow);
            }
        }

        public IEnumerable<IOneOnOneChatThread> OneOnOneChatThreads
        {
            get
            {
                return oneOnOneChatThreads.ToArray();
            }
        }

        public IEnumerable<IGroupChatThread> GroupChatThreads
        {
            get
            {
                return groupChatThreads.ToArray();
            }
        }

        public IEnumerable<IOfficialAccountChatThread> OfficialAccountChatThreads
        {
            get
            {
                return officialAccountChatThreads.Cast<IOfficialAccountChatThread>();
            }
        }

        public AgeBandType AgeBandType
        {
            get
            {
                return ageBandType;
            }
        }

        public IRegistrationProfile RegistrationProfile
        {
            get
            {
                return registrationProfile;
            }
        }

        public IEnumerable<IInternalFriend> InternalFriends
        {
            get
            {
                return friends.ToArray();
            }
        }

        public IEnumerable<IInternalIncomingFriendInvitation> InternalIncomingFriendInvitations
        {
            get
            {
                return incomingFriendInvitations.ToArray();
            }
        }

        public IEnumerable<IInternalOutgoingFriendInvitation> InternalOutgoingFriendInvitations
        {
            get
            {
                return outgoingFriendInvitations.ToArray();
            }
        }

        public IEnumerable<IInternalChatThread> InternalChatThreads
        {
            get
            {
                return GetAllChatThreads().ToArray();
            }
        }

        public IEnumerable<IInternalOneOnOneChatThread> InternalOneOnOneChatThreads
        {
            get
            {
                return oneOnOneChatThreads.ToArray();
            }
        }

        public IEnumerable<IInternalGroupChatThread> InternalGroupChatThreads
        {
            get
            {
                return groupChatThreads.ToArray();
            }
        }

        public IEnumerable<IInternalOfficialAccountChatThread> InternalOfficialAccountChatThreads
        {
            get
            {
                return officialAccountChatThreads;
            }
        }

        public IInternalRegistrationProfile InternalRegistrationProfile
        {
            get
            {
                return registrationProfile;
            }
        }

        public event EventHandler<AbstractAddedToOneOnOneChatThreadEventArgs> OnAddedToOneOnOneChatThread = delegate
        {
        };

        public event EventHandler<AbstractAddedToGroupChatThreadEventArgs> OnAddedToGroupChatThread = delegate
        {
        };

        public event EventHandler<AbstractAddedToOfficialAccountThreadEventArgs> OnAddedToOfficialAccountChatThread = delegate
        {
        };

        public event EventHandler<AbstractReceivedOutgoingFriendInvitationEventArgs> OnReceivedOutgoingFriendInvitation = delegate
        {
        };

        public event EventHandler<AbstractReceivedIncomingFriendInvitationEventArgs> OnReceivedIncomingFriendInvitation = delegate
        {
        };

        public event EventHandler<AbstractUnfriendedEventArgs> OnUnfriended = delegate
        {
        };

        public event EventHandler<AbstractUntrustedEventArgs> OnUntrusted = delegate
        {
        };

        public event EventHandler<AbstractOfficialAccountFollowedEventArgs> OnOfficialAccountFollowed = delegate
        {
        };

        public event EventHandler<AbstractOfficialAccountUnfollowedEventArgs> OnOfficialAccountUnfollowed = delegate
        {
        };

        public event EventHandler<AbstractAvatarChangedEventArgs> OnAvatarChanged = delegate
        {
        };

        public event Action<bool> OnPushNotificationsToggled = delegate
        {
        };

        public event Action<bool> OnPushNotificationReceived = delegate
        {
        };

        public event Action<string> OnDisplayNameUpdated = delegate
        {
        };

        public event EventHandler<AbstractAlertsAddedEventArgs> OnAlertsAdded = delegate
        {
        };

        public event EventHandler<AbstractAlertsClearedEventArgs> OnAlertsCleared = delegate
        {
        };

        public event EventHandler<AbstractLegalMarketingUpdateRequiredEventArgs> OnLegalMarketingUpdateRequired = delegate
        {
        };

        public LocalUser(AbstractLogger logger, IDisplayName displayName, string swid, IChatThreadCreator chatThreadCreator, IList<IInternalFriend> friends, IList<IInternalOneOnOneChatThread> oneOnOneChatThreads, IList<IInternalGroupChatThread> groupChatThreads, IList<IInternalOfficialAccountChatThread> officialAccountChatThreads, IList<IOfficialAccount> followships, AgeBandType ageBandType, IChatMessageFactory chatMessageFactory, IDatabase database, IUserDatabase userDatabase, IChatMessageParser chatMessageParser, IInternalRegistrationProfile registrationProfile, IMixWebCallFactory mixWebCallFactory, IPhotoFlavorFactory photoFlavorFactory, IVideoFlavorFactory videoFlavorFactory, IGuestControllerClient guestControllerClient, INotificationQueue notificationQueue, IEncryptor encryptor, IEpochTime epochTime)
        {
            DisplayName = displayName;
            FirstName = registrationProfile.FirstName;
            Swid = swid;
            Id = swid;
            this.logger = logger;
            this.registrationProfile = registrationProfile;
            this.mixWebCallFactory = mixWebCallFactory;
            this.chatThreadCreator = chatThreadCreator;
            this.friends = friends;
            this.oneOnOneChatThreads = oneOnOneChatThreads;
            this.groupChatThreads = groupChatThreads;
            this.followships = followships;
            this.officialAccountChatThreads = officialAccountChatThreads;
            this.ageBandType = ageBandType;
            this.chatMessageFactory = chatMessageFactory;
            this.database = database;
            this.userDatabase = userDatabase;
            this.chatMessageParser = chatMessageParser;
            incomingFriendInvitations = new List<IInternalIncomingFriendInvitation>();
            outgoingFriendInvitations = new List<IInternalOutgoingFriendInvitation>();
            oldInvitationIds = new List<long>();
            unidentifiedUsers = new List<IInternalUnidentifiedUser>();
            this.photoFlavorFactory = photoFlavorFactory;
            this.videoFlavorFactory = videoFlavorFactory;
            this.guestControllerClient = guestControllerClient;
            this.notificationQueue = notificationQueue;
            this.encryptor = encryptor;
            this.epochTime = epochTime;
            guestControllerClient.OnLegalMarketingUpdateRequired += delegate (object sender, AbstractLegalMarketingUpdateRequiredEventArgs e)
            {
                this.OnLegalMarketingUpdateRequired(this, e);
            };
        }

        private IEnumerable<IInternalChatThread> GetAllChatThreads()
        {
            return groupChatThreads.Cast<IInternalChatThread>().Concat(oneOnOneChatThreads.Cast<IInternalChatThread>()).Concat(officialAccountChatThreads.Cast<IInternalChatThread>());
        }

        public void DispatchOnAvatarChanged()
        {
            this.OnAvatarChanged(this, new AvatarChangedEventArgs(Avatar));
        }

        public void FindUser(string displayNameText, Action<IFindUserResult> callback)
        {
            DisplayNameSearcher.Search(logger, mixWebCallFactory, displayNameText, userDatabase, delegate (IInternalUnidentifiedUser user)
            {
                unidentifiedUsers.Add(user);
                callback(new FindUserResult(true, user));
            }, delegate
            {
                callback(new FindUserResult(false, null));
            });
        }

        public IOutgoingFriendInvitation SendFriendInvitation(IUnidentifiedUser user, bool requestTrust, Action<ISendFriendInvitationResult> callback)
        {
            IInternalUnidentifiedUser internalUnidentifiedUser = unidentifiedUsers.FirstOrDefault((IInternalUnidentifiedUser u) => u.DisplayName.Text == user.DisplayName.Text);
            if (internalUnidentifiedUser == null)
            {
                logger.Critical("User to send friend invitation to not found");
                callback(new SendFriendInvitationResult(false, null));
                return null;
            }
            ISendFriendInvitationResult invitationValidationResult = GetInvitationValidationResult(user.DisplayName.Text, requestTrust);
            if (invitationValidationResult != null)
            {
                callback(invitationValidationResult);
                return null;
            }
            return InternalSendFriendInvitation(internalUnidentifiedUser, requestTrust, callback);
        }

        public IOutgoingFriendInvitation SendFriendInvitation(IRemoteChatMember user, bool requestTrust, Action<ISendFriendInvitationResult> callback)
        {
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            IInternalRemoteChatMember internalRemoteChatMember = allChatThreads.SelectMany((IInternalChatThread t) => t.InternalMembers).FirstOrDefault((IInternalRemoteChatMember m) => m.Id == user.Id);
            if (internalRemoteChatMember == null)
            {
                logger.Critical("User to send friend invitation to not found");
                callback(new SendFriendInvitationResult(false, null));
                return null;
            }
            IInternalUnidentifiedUser internalUser = RemoteUserFactory.CreateUnidentifiedUser(internalRemoteChatMember.DisplayName.Text, internalRemoteChatMember.FirstName, userDatabase);
            return InternalSendFriendInvitation(internalUser, requestTrust, callback);
        }

        public IOutgoingFriendInvitation SendFriendInvitation(IFriend user, bool requestTrust, Action<ISendFriendInvitationResult> callback)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Id == user.Id);
            if (internalFriend == null)
            {
                logger.Critical("User to send friend invitation to not found");
                callback(new SendFriendInvitationResult(false, null));
                return null;
            }
            IInternalUnidentifiedUser internalUser = RemoteUserFactory.CreateUnidentifiedUser(internalFriend.DisplayName.Text, internalFriend.FirstName, userDatabase);
            return InternalSendFriendInvitation(internalUser, requestTrust, callback);
        }

        private IOutgoingFriendInvitation InternalSendFriendInvitation(IInternalUnidentifiedUser internalUser, bool requestTrust, Action<ISendFriendInvitationResult> callback)
        {
            ISendFriendInvitationResult invitationValidationResult = GetInvitationValidationResult(internalUser.DisplayName.Text, requestTrust);
            if (invitationValidationResult != null)
            {
                callback(invitationValidationResult);
                return null;
            }
            OutgoingFriendInvitation invitation = new OutgoingFriendInvitation(this, internalUser, requestTrust);
            AddOutgoingFriendInvitation(invitation);
            FriendInvitationSender.Send(logger, notificationQueue, mixWebCallFactory, internalUser.DisplayName.Text, requestTrust, delegate
            {
                callback(new SendFriendInvitationResult(true, invitation));
            }, delegate (ISendFriendInvitationResult r)
            {
                RemoveOutgoingFriendInvitation(invitation);
                callback(r);
            });
            return invitation;
        }

        private ISendFriendInvitationResult GetInvitationValidationResult(string displayName, bool requestTrust)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.DisplayName.Text == displayName);
            if (internalFriend != null)
            {
                if (internalFriend.IsTrusted)
                {
                    logger.Critical("This user is already a trusted friend");
                    return new SendFriendInvitationAlreadyFriendsResult(false, null);
                }
                if (!requestTrust)
                {
                    logger.Critical("This user is already an untrusted friend");
                    return new SendFriendInvitationAlreadyFriendsResult(false, null);
                }
            }
            IInternalOutgoingFriendInvitation internalOutgoingFriendInvitation = outgoingFriendInvitations.FirstOrDefault((IInternalOutgoingFriendInvitation i) => i.Invitee.DisplayName.Text == displayName);
            if (internalOutgoingFriendInvitation != null)
            {
                if (internalOutgoingFriendInvitation.RequestTrust)
                {
                    logger.Critical("We already have a trusted invitation out for this user");
                    return new SendFriendInvitationAlreadyExistsResult(false, null);
                }
                if (!requestTrust)
                {
                    logger.Critical("We already have an untrusted invitation out for this user");
                    return new SendFriendInvitationAlreadyExistsResult(false, null);
                }
            }
            return null;
        }

        public void AcceptFriendInvitation(IIncomingFriendInvitation invitation, bool acceptTrust, Action<IAcceptFriendInvitationResult> callback)
        {
            IInternalIncomingFriendInvitation internalIncomingFriendInvitation = incomingFriendInvitations.FirstOrDefault((IInternalIncomingFriendInvitation i) => i.Id == invitation.Id);
            if (internalIncomingFriendInvitation == null)
            {
                logger.Critical("Friend invitation to accept not found");
                callback(new AcceptFriendInvitationResult(false, null));
                return;
            }
            FriendInvitationAccepter.Accept(logger, notificationQueue, mixWebCallFactory, internalIncomingFriendInvitation.InvitationId, acceptTrust, delegate (AddFriendshipNotification notification)
            {
                IInternalFriend friend = friends.FirstOrDefault((IInternalFriend f) => f.Swid == notification.Friend.UserId);
                callback(new AcceptFriendInvitationResult(true, friend));
            }, delegate
            {
                callback(new AcceptFriendInvitationResult(false, null));
            });
        }

        public void RejectFriendInvitation(IIncomingFriendInvitation invitation, Action<IRejectFriendInvitationResult> callback)
        {
            IInternalIncomingFriendInvitation internalInvitation = incomingFriendInvitations.FirstOrDefault((IInternalIncomingFriendInvitation i) => i.Id == invitation.Id);
            if (internalInvitation == null)
            {
                logger.Critical("Friend invitation to reject not found");
                callback(new RejectFriendInvitationResult(false));
                return;
            }
            FriendInvitationRejecter.Reject(logger, notificationQueue, mixWebCallFactory, internalInvitation.InvitationId, delegate
            {
                RemoveIncomingFriendInvitation(internalInvitation.InvitationId);
                internalInvitation.Rejected();
                callback(new RejectFriendInvitationResult(true));
            }, delegate
            {
                callback(new RejectFriendInvitationResult(false));
            });
        }

        public void Unfriend(IFriend friend, Action<IUnfriendResult> callback)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Id == friend.Id);
            if (internalFriend == null)
            {
                logger.Critical("Friend to unfriend not found");
                callback(new UnfriendResult(false));
                return;
            }
            Unfriender.Unfriend(logger, notificationQueue, mixWebCallFactory, internalFriend.Swid, delegate
            {
                callback(new UnfriendResult(true));
            }, delegate
            {
                callback(new UnfriendResult(false));
            });
        }

        public void Untrust(IFriend trustedUser, Action<IUntrustResult> callback)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Id == trustedUser.Id);
            if (internalFriend == null)
            {
                logger.Critical("Friend to untrust not found");
                callback(new UntrustResult(false));
                return;
            }
            if (!internalFriend.IsTrusted)
            {
                logger.Critical("Friend is already untrusted");
                callback(new UntrustResult(false));
                return;
            }
            Untruster.Untrust(logger, notificationQueue, mixWebCallFactory, internalFriend.Swid, delegate
            {
                UntrustFriend(internalFriend);
                callback(new UntrustResult(true));
            }, delegate
            {
                callback(new UntrustResult(false));
            });
        }

        public IUserNickname SetNickname(IFriend user, string nicknameText, Action<ISetUserNicknameResult> callback)
        {
            IInternalFriend friend = friends.FirstOrDefault((IInternalFriend f) => f.Id == user.Id);
            if (friend == null)
            {
                logger.Critical("Friend to set nickname on not found");
                callback(new SetUserNicknameResult(false));
                return null;
            }
            UserNickname nickname = new UserNickname(nicknameText);
            UserNicknameSetter.SetNickname(logger, notificationQueue, mixWebCallFactory, friend.Swid, nicknameText, delegate
            {
                nickname.ApplyFinished();
                friend.UpdateNickname(nickname);
                callback(new SetUserNicknameResult(true));
            }, delegate
            {
                callback(new SetUserNicknameResult(false));
            });
            return nickname;
        }

        public void RemoveNickname(IFriend user, Action<IRemoveUserNicknameResult> callback)
        {
            IInternalFriend friend = friends.FirstOrDefault((IInternalFriend f) => f.Id == user.Id);
            if (friend == null)
            {
                logger.Critical("Friend to set nickname on not found");
                callback(new RemoveUserNicknameResult(false));
                return;
            }
            UserNicknameRemover.RemoveNickname(logger, notificationQueue, mixWebCallFactory, friend.Swid, delegate
            {
                friend.UpdateNickname(null);
                callback(new RemoveUserNicknameResult(true));
            }, delegate
            {
                callback(new RemoveUserNicknameResult(false));
            });
        }

        public void GetRecommendedFriends(Action<IGetRecommendedFriendsResult> callback)
        {
            FriendRecommender.Recommend(logger, mixWebCallFactory, userDatabase, delegate (IEnumerable<IInternalUnidentifiedUser> internalUsers)
            {
                IUnidentifiedUser[] users = internalUsers.Select((Func<IInternalUnidentifiedUser, IUnidentifiedUser>)delegate (IInternalUnidentifiedUser user)
                {
                    unidentifiedUsers.Add(user);
                    return user;
                }).ToArray();
                callback(new GetRecommendedFriendResult(true, users));
            }, delegate
            {
                callback(new GetRecommendedFriendResult(false, Enumerable.Empty<IUnidentifiedUser>()));
            });
        }

        public void SetAvatar(IAvatar avatar, Action<ISetAvatarResult> callback)
        {
            IInternalAvatar internalAvatar = InternalAvatar;
            AvatarSetter.SetAvatar(logger, notificationQueue, mixWebCallFactory, avatar, internalAvatar.AvatarId, internalAvatar.SlotId, delegate
            {
                callback(new SetAvatarResult(true));
            }, delegate
            {
                callback(new SetAvatarResult(false));
            });
        }

        public void CreateOneOnOneChatThread(IFriend member, Action<ICreateOneOnOneChatThreadResult> callback)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Id == member.Id);
            if (internalFriend == null)
            {
                callback(new CreateOneOnOneChatThreadResult(false, null));
                return;
            }
            chatThreadCreator.CreateOneOnOneChatThread(internalFriend.Swid, delegate (ChatThread chatThread)
            {
                AddChatThread(chatThread);
                IInternalOneOnOneChatThread chatThread2 = oneOnOneChatThreads.FirstOrDefault((IInternalOneOnOneChatThread c) => c.ChatThreadId == chatThread.ChatThreadId.Value);
                callback(new CreateOneOnOneChatThreadResult(true, chatThread2));
            }, delegate
            {
                callback(new CreateOneOnOneChatThreadResult(false, null));
            });
        }

        public void CreateGroupChatThread(IEnumerable<IFriend> members, Action<ICreateGroupChatThreadResult> callback)
        {
            chatThreadCreator.CreateGroupChatThread(members.Select((IFriend u) => friends.First((IInternalFriend f) => f.Id == u.Id).Swid), delegate (ChatThread chatThread)
            {
                AddChatThread(chatThread);
                IInternalGroupChatThread chatThread2 = groupChatThreads.FirstOrDefault((IInternalGroupChatThread c) => c.ChatThreadId == chatThread.ChatThreadId.Value);
                callback(new CreateGroupChatThreadResult(true, chatThread2));
            }, delegate
            {
                callback(new CreateGroupChatThreadResult(false, null));
            });
        }

        public void AddChatThreadMembers(IGroupChatThread chatThread, IEnumerable<IFriend> members, Action<IAddChatThreadMemberResult> callback)
        {
            IInternalGroupChatThread internalGroupChatThread = groupChatThreads.FirstOrDefault((IInternalGroupChatThread c) => c.Id == chatThread.Id);
            if (internalGroupChatThread == null)
            {
                logger.Critical("Chat thread to add members to not found");
                callback(new AddChatThreadMemberThreadNotFoundResult(false));
                return;
            }
            List<IInternalFriend> list = new List<IInternalFriend>();
            IFriend member;
            foreach (IFriend member2 in members)
            {
                member = member2;
                IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Id == member.Id);
                if (internalFriend == null)
                {
                    logger.Critical("Friend to add to chat thread not found");
                    callback(new AddChatThreadMemberNotFriendsResult(false));
                    return;
                }
                if (internalGroupChatThread.RemoteMembers.Any((IRemoteChatMember c) => c.Id == member.Id))
                {
                    logger.Critical("Friend to add to chat thread is already in the thread");
                    callback(new AddChatThreadMemberAlreadyMemberResult(false));
                    return;
                }
                list.Add(internalFriend);
            }
            internalGroupChatThread.AddMembers(list, callback);
        }

        public void RemoveChatThreadMember(IGroupChatThread chatThread, IRemoteChatMember member, Action<IRemoveChatThreadMemberResult> callback)
        {
            IInternalGroupChatThread internalGroupChatThread = groupChatThreads.FirstOrDefault((IInternalGroupChatThread c) => c.Id == chatThread.Id);
            if (internalGroupChatThread == null)
            {
                logger.Critical("Chat thread to remove member of not found");
                callback(new RemoveChatThreadMemberResult(false));
                return;
            }
            IInternalRemoteChatMember internalRemoteChatMember = internalGroupChatThread.InternalMembers.FirstOrDefault((IInternalRemoteChatMember m) => m.Id == member.Id);
            if (internalRemoteChatMember == null)
            {
                logger.Critical("Chat member to remove isn't a member of the thread");
                callback(new RemoveChatThreadMemberResult(false));
            }
            else
            {
                internalGroupChatThread.RemoveRemoteMember(internalRemoteChatMember, callback);
            }
        }

        public void RemoveChatThreadMember(IGroupChatThread chatThread, IFriend member, Action<IRemoveChatThreadMemberResult> callback)
        {
            IInternalGroupChatThread internalGroupChatThread = groupChatThreads.FirstOrDefault((IInternalGroupChatThread c) => c.Id == chatThread.Id);
            if (internalGroupChatThread == null)
            {
                logger.Critical("Chat thread to remove member from not found");
                callback(new RemoveChatThreadMemberResult(false));
                return;
            }
            IInternalRemoteChatMember internalRemoteChatMember = internalGroupChatThread.InternalMembers.FirstOrDefault((IInternalRemoteChatMember m) => m.Id == member.Id);
            if (internalRemoteChatMember == null)
            {
                logger.Critical("Chat member to remove from thread is not a member");
                callback(new RemoveChatThreadMemberResult(false));
            }
            else
            {
                internalGroupChatThread.RemoveRemoteMember(internalRemoteChatMember, callback);
            }
        }

        public void RemoveChatThreadMember(IGroupChatThread chatThread, ILocalUser member, Action<IRemoveChatThreadMemberResult> callback)
        {
            IInternalGroupChatThread internalGroupChatThread = groupChatThreads.FirstOrDefault((IInternalGroupChatThread c) => c.Id == chatThread.Id);
            if (internalGroupChatThread == null)
            {
                logger.Critical("Chat thread to remove member from not found");
                callback(new RemoveChatThreadMemberResult(false));
            }
            else
            {
                groupChatThreads.Remove(internalGroupChatThread);
                userDatabase.DeleteChatMessages(internalGroupChatThread.ChatThreadId);
                internalGroupChatThread.RemoveLocalUser(this, callback);
            }
        }

        public void EnableAllPushNotifications(string token, PushNotificationService service, string provisionId, Action<IEnableAllPushNotificationsResult> callback)
        {
            PushNotificationSettings.EnableAllPushNotifications(logger, database, mixWebCallFactory, token, service, provisionId, Swid, delegate (IEnableAllPushNotificationsResult result)
            {
                if (result.Success)
                {
                    this.OnPushNotificationsToggled(true);
                }
                callback(result);
            });
        }

        public void EnableInvisiblePushNotifications(string token, PushNotificationService service, string provisionId, Action<IEnableInvisiblePushNotificationsResult> callback)
        {
            PushNotificationSettings.EnableInvisiblePushNotifications(logger, database, mixWebCallFactory, token, service, provisionId, Swid, delegate (IEnableInvisiblePushNotificationsResult result)
            {
                if (result.Success)
                {
                    this.OnPushNotificationsToggled(true);
                }
                callback(result);
            });
        }

        public void DisableAllPushNotifications(Action<IDisableAllPushNotificationsResult> callback)
        {
            PushNotificationSettings.DisableAllPushNotifications(logger, database, mixWebCallFactory, Swid, delegate (IDisableAllPushNotificationsResult result)
            {
                if (result.Success)
                {
                    this.OnPushNotificationsToggled(false);
                }
                callback(result);
            });
        }

        public void DisableVisiblePushNotifications(Action<IDisableVisiblePushNotificationsResult> callback)
        {
            PushNotificationSettings.DisableVisiblePushNotifications(logger, database, mixWebCallFactory, Swid, delegate (IDisableVisiblePushNotificationsResult result)
            {
                if (result.Success)
                {
                    this.OnPushNotificationsToggled(true);
                }
                callback(result);
            });
        }

        public IPushNotification ReceivePushNotification(IDictionary notification)
        {
            IInternalPushNotification internalPushNotification = PushNotificationReceiver.Receive(logger, encryptor, database, Swid, notification);
            this.OnPushNotificationReceived(internalPushNotification.NotificationsAvailable);
            return internalPushNotification;
        }

        public void TemporarilyBanAccount(Action<ITemporarilyBanAccountResult> callback)
        {
            AccountBanner.TemporarilyBan(logger, mixWebCallFactory, delegate (bool success)
            {
                callback(new TemporarilyBanAccountResult(success));
            });
        }

        public void SendMassPushNotification(Action<ISendMassPushNotificationResult> callback)
        {
            MassPushNotificationSender.SendMassPushNotification(logger, mixWebCallFactory, delegate (bool success)
            {
                callback(new SendMassPushNotificationResult(success));
            });
        }

        public void SendAlert(int level, AlertType type, Action<ISendAlertResult> callback)
        {
            try
            {
                IWebCall<TriggerAlertRequest, BaseResponse> webCall = mixWebCallFactory.IntegrationTestSupportUserAlertPost(new TriggerAlertRequest
                {
                    Level = level.ToString(),
                    Text = AlertTypeUtils.ToString(type)
                });
                webCall.OnResponse += delegate
                {
                    callback(new SendAlertResult(true));
                };
                webCall.OnError += delegate
                {
                    callback(new SendAlertResult(false));
                };
                webCall.Execute();
            }
            catch (Exception ex)
            {
                logger.Critical("Unhandled exception: " + ex);
                callback(new SendAlertResult(false));
            }
        }

        public void GetAllOfficialAccounts(Action<IGetAllOfficialAccountsResult> callback)
        {
            OfficialAccountGetter.GetAllOfficialAccounts(logger, userDatabase, mixWebCallFactory, delegate (OfficialAccountsResponse response)
            {
                HandleGetAllOfficialAccounts(response, callback);
            }, delegate
            {
                callback(new GetAllOfficialAccountsResult(false, null));
            });
        }

        private void HandleGetAllOfficialAccounts(OfficialAccountsResponse response, Action<IGetAllOfficialAccountsResult> callback)
        {
            List<IOfficialAccount> list = new List<IOfficialAccount>();
            foreach (GuestOfficialAccount officialAccountId in response.OfficialAccountIds)
            {
                list.Add(OfficialAccountFactory.CreateOfficialAccount(officialAccountId.OaId, officialAccountId.OaName, officialAccountId.IsAvailable.Value, officialAccountId.CanUnfollow.Value));
            }
            callback(new GetAllOfficialAccountsResult(true, list));
        }

        public void FollowOfficialAccount(IOfficialAccount account, Action<IFollowOfficialAccountResult> callback)
        {
            OfficialAccountFollower.Follow(logger, notificationQueue, account.AccountId, mixWebCallFactory, delegate
            {
                callback(new FollowOfficialAccountResult(true));
            }, delegate
            {
                callback(new FollowOfficialAccountResult(false));
            });
        }

        public void UnfollowOfficialAccount(IOfficialAccount account, Action<IUnfollowOfficialAccountResult> callback)
        {
            if (!account.CanUnfollow)
            {
                callback(new UnfollowOfficialAccountResult(false));
            }
            else if (followships.Any((IOfficialAccount officialAccount) => account.AccountId == officialAccount.AccountId))
            {
                OfficialAccountUnfollower.Unfollow(logger, notificationQueue, account.AccountId, mixWebCallFactory, delegate
                {
                    callback(new UnfollowOfficialAccountResult(true));
                }, delegate
                {
                    callback(new UnfollowOfficialAccountResult(false));
                });
            }
            else
            {
                callback(new UnfollowOfficialAccountResult(false));
            }
        }

        public void FollowOfficialAccount(GuestOfficialAccount domainOfficialAccount)
        {
            if (!followships.Any((IOfficialAccount oa) => oa.AccountId == domainOfficialAccount.OaId))
            {
                IOfficialAccount officialAccount = OfficialAccountFactory.CreateOfficialAccount(domainOfficialAccount.OaId, domainOfficialAccount.OaName, domainOfficialAccount.IsAvailable.Value, domainOfficialAccount.CanUnfollow.Value);
                followships.Add(officialAccount);
                OfficialAccountFollowedEventArgs e = new OfficialAccountFollowedEventArgs(officialAccount);
                this.OnOfficialAccountFollowed(this, e);
            }
        }

        public void UnfollowOfficialAccount(string officialAccountId)
        {
            IOfficialAccount officialAccount = followships.FirstOrDefault((IOfficialAccount oa) => oa.AccountId == officialAccountId);
            if (officialAccount != null)
            {
                followships.Remove(officialAccount);
                OfficialAccountUnfollowedEventArgs e = new OfficialAccountUnfollowedEventArgs(officialAccount);
                this.OnOfficialAccountUnfollowed(this, e);
            }
        }

        public void DispatchOnAlertsCleared(IEnumerable<IAlert> alerts)
        {
            this.OnAlertsCleared(this, new AlertsClearedEventArgs(alerts));
        }

        public void DispatchOnAlertsAdded(IAlert alert)
        {
            this.OnAlertsAdded(this, new AlertsAddedEventArgs(new IAlert[1] { alert }));
        }

        public void UpdateProfile(string firstName, string lastName, string displayName, string email, string parentEmail, DateTime? dateOfBirth, IEnumerable<KeyValuePair<IMarketingItem, bool>> marketingAgreements, IEnumerable<ILegalDocument> acceptedLegalDocuments, Action<IUpdateProfileResult> callback)
        {
            ProfileUpdater.UpdateProfile(logger, guestControllerClient, database, Swid, epochTime, registrationProfile, firstName, lastName, displayName, email, parentEmail, dateOfBirth, marketingAgreements, acceptedLegalDocuments, callback);
        }

        public void RefreshProfile(Action<IRefreshProfileResult> callback)
        {
            ProfileGetter.GetProfile(logger, guestControllerClient, delegate (ProfileData profileData)
            {
                HandleRefreshProfile(profileData, callback);
            });
        }

        private void HandleRefreshProfile(ProfileData profileData, Action<IRefreshProfileResult> callback)
        {
            bool flag = profileData != null;
            if (flag)
            {
                flag = ProfileStorer.StoreProfile(logger, database, epochTime, Swid, profileData);
                registrationProfile.Update(profileData.profile, profileData.displayName, profileData.marketing);
            }
            callback(new RefreshProfileResult(flag));
        }

        public void SendParentalApprovalEmail(Action<ISendParentalApprovalEmailResult> callback)
        {
            ParentalApprovalEmailSender.SendParentalApprovalEmail(logger, guestControllerClient, callback);
        }

        public void SendVerificationEmail(Action<ISendVerificationEmailResult> callback)
        {
            if (RegistrationProfile.EmailVerified)
            {
                callback(new SendVerificationEmailResult(true));
            }
            else
            {
                VerificationEmailSender.SendVerificationEmail(logger, guestControllerClient, callback);
            }
        }

        public void ValidateDisplayName(string displayName, Action<IValidateDisplayNameResult> callback)
        {
            DisplayNameValidator.ValidateDisplayName(logger, guestControllerClient, mixWebCallFactory, displayName, callback);
        }

        public void ValidateDisplayNames(IEnumerable<string> displayNames, Action<IValidateDisplayNamesResult> callback)
        {
            string[] array = displayNames.ToArray();
            if (array.Any((string d) => d == null))
            {
                logger.Critical("Can't validate empty name");
                callback(new ValidateDisplayNamesResult(false, Enumerable.Empty<string>()));
            }
            else
            {
                DisplayNameValidator.ValidateDisplayNames(logger, mixWebCallFactory, array, callback);
            }
        }

        public void UpdateDisplayName(string displayName, Action<IUpdateDisplayNameResult> callback)
        {
            DisplayNameUpdater.UpdateDisplayName(logger, mixWebCallFactory, displayName, delegate (IUpdateDisplayNameResult r)
            {
                try
                {
                    if (r.Success)
                    {
                        DisplayName = new DisplayName(displayName);
                        this.OnDisplayNameUpdated(displayName);
                        UserDocument userBySwid = userDatabase.GetUserBySwid(Swid);
                        userBySwid.DisplayName = displayName;
                        userDatabase.UpdateUserDocument(userBySwid);
                        registrationProfile.UpdateDisplayName(displayName);
                    }
                    callback(r);
                }
                catch (Exception ex)
                {
                    logger.Critical("Unhandled exception: " + ex);
                    callback(new UpdateDisplayNameResult(false));
                }
            });
        }

        public void GetRecentChatThreadMessages(Action<IGetRecentChatThreadMessagesResult> callback)
        {
            IInternalChatThread[] array = GetAllChatThreads().ToArray();
            List<IInternalChatThread> list = new List<IInternalChatThread>();
            IInternalChatThread[] array2 = array;
            foreach (IInternalChatThread internalChatThread in array2)
            {
                if (!internalChatThread.AreSequenceNumbersIndexed)
                {
                    list.Add(internalChatThread);
                }
            }
            int count = list.Count;
            if (count > 0)
            {
                try
                {
                    int numCallbacks = 0;
                    foreach (IInternalChatThread chatThread in list)
                    {
                        long chatThreadId = chatThread.ChatThreadId;
                        userDatabase.IndexSequenceNumberField(chatThreadId, delegate
                        {
                            chatThread.AreSequenceNumbersIndexed = true;
                            numCallbacks++;
                            if (numCallbacks == count)
                            {
                                OnChatThreadsIndexed(callback);
                            }
                        });
                    }
                    return;
                }
                catch (Exception ex)
                {
                    logger.Critical("Unhandled exception: " + ex);
                    callback(new GetRecentChatThreadMessagesResult(false));
                    return;
                }
            }
            OnChatThreadsIndexed(callback);
        }

        public void SetLanguagePreference(string languageCode, Action<ISetLangaugePreferenceResult> callback)
        {
            try
            {
                IWebCall<SetLanguageRequest, BaseResponse> webCall = mixWebCallFactory.LanguagePreferencePost(new SetLanguageRequest
                {
                    LanguageCode = languageCode
                });
                webCall.OnResponse += delegate
                {
                    callback(new SetLangaugePreferenceResult(true));
                };
                webCall.OnError += delegate
                {
                    logger.Critical("Error setting language preference");
                    callback(new SetLangaugePreferenceResult(false));
                };
                webCall.Execute();
            }
            catch (Exception ex)
            {
                logger.Critical("Unhandled exception: " + ex);
                callback(new SetLangaugePreferenceResult(false));
            }
        }

        private void OnChatThreadsIndexed(Action<IGetRecentChatThreadMessagesResult> callback)
        {
            long?[] array = GetNextChatMessageSequenceNumbers(oneOnOneChatThreads).Concat(GetNextChatMessageSequenceNumbers(groupChatThreads)).Concat(GetNextChatMessageSequenceNumbers(officialAccountChatThreads)).Cast<long?>()
                .ToArray();
            if (!array.Any())
            {
                callback(new GetRecentChatThreadMessagesResult(true));
                return;
            }
            RecentChatThreadMessageRetriever.RetrieveMessages(logger, mixWebCallFactory, array, delegate (GetChatThreadMessagesResponse response)
            {
                try
                {
                    IInternalChatThread[] array2 = GetAllChatThreads().ToArray();
                    List<IInternalChatThread> list = array2.ToList();
                    List<IInternalChatMessage> messageList = new List<IInternalChatMessage>();
                    bool flag = ParseMessages(logger, chatMessageParser, response.Gag, array2, messageList, list);
                    flag = ParseMessages(logger, chatMessageParser, response.GameState, array2, messageList, list) && flag;
                    flag = ParseMessages(logger, chatMessageParser, response.GameEvent, array2, messageList, list) && flag;
                    flag = ParseMessages(logger, chatMessageParser, response.Photo, array2, messageList, list) && flag;
                    flag = ParseMessages(logger, chatMessageParser, response.Sticker, array2, messageList, list) && flag;
                    flag = ParseMessages(logger, chatMessageParser, response.Text, array2, messageList, list) && flag;
                    flag = ParseMessages(logger, chatMessageParser, response.Video, array2, messageList, list) && flag;
                    flag = ParseMessages(logger, chatMessageParser, response.MemberListChanged, array2, messageList, list) && flag;
                    chatMessageParser.InsertParsedChatMessageDocuments();
                    foreach (IInternalChatThread item in list)
                    {
                        if (!item.InternalChatMessages.Any())
                        {
                            chatMessageParser.RetrieveParsedChatMessages(item, 1);
                        }
                    }
                    callback(new GetRecentChatThreadMessagesResult(flag));
                }
                catch (Exception ex)
                {
                    logger.Critical("Unhandled exception: " + ex);
                    callback(new GetRecentChatThreadMessagesResult(false));
                }
            }, delegate
            {
                callback(new GetRecentChatThreadMessagesResult(false));
            });
        }

        private static IEnumerable<long> GetNextChatMessageSequenceNumbers<T>(IEnumerable<T> chatThreads) where T : IInternalChatThread
        {
            return chatThreads.SelectMany((T x) => new long[2] { x.ChatThreadId, x.NextChatMessageSequenceNumber });
        }

        private static bool ParseMessages<TMessage>(AbstractLogger logger, IChatMessageParser chatMessageParser, IEnumerable<TMessage> messages, IList<IInternalChatThread> allChatThreads, IList<IInternalChatMessage> messageList, ICollection<IInternalChatThread> chatThreadList) where TMessage : BaseChatMessage
        {
            TMessage message;
            foreach (TMessage message2 in messages)
            {
                message = message2;
                IInternalChatThread internalChatThread = allChatThreads.FirstOrDefault((IInternalChatThread ct) => ct.ChatThreadId == message.ChatThreadId);
                if (internalChatThread == null)
                {
                    logger.Critical("Couldn't find thread " + message.ChatThreadId + " for message " + message.ChatMessageId);
                    return false;
                }
                chatMessageParser.ParseMessage(message, internalChatThread, messageList);
                chatThreadList.Remove(internalChatThread);
            }
            return true;
        }

        public void ModerateText(string text, bool isTrusted, Action<ITextModerationResult> callback)
        {
            TextModerator.ModerateText(logger, mixWebCallFactory, text, isTrusted, delegate (ModerateTextResponse response)
            {
                callback(new TextModerationResult(true, response.Moderated.Value, response.Text));
            }, delegate
            {
                callback(new TextModerationResult(false, false, null));
            });
        }

        public void ReportUser(IRemoteChatMember user, ReportUserReason reportUserReason, Action<IReportUserResult> callback)
        {
            string userId = null;
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            IInternalRemoteChatMember internalRemoteChatMember = allChatThreads.SelectMany((IInternalChatThread t) => t.InternalMembers).FirstOrDefault((IInternalRemoteChatMember m) => m.Id == user.Id);
            if (internalRemoteChatMember != null)
            {
                userId = internalRemoteChatMember.Swid;
            }
            ReportUser(userId, reportUserReason, callback);
        }

        public void ReportUser(IFriend user, ReportUserReason reportUserReason, Action<IReportUserResult> callback)
        {
            string userId = null;
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Id == user.Id);
            if (internalFriend != null)
            {
                userId = internalFriend.Swid;
            }
            ReportUser(userId, reportUserReason, callback);
        }

        private void ReportUser(string userId, ReportUserReason reportUserReason, Action<IReportUserResult> callback)
        {
            if (userId == null)
            {
                logger.Critical("Can not report this user as we do not know their ID!");
                callback(new ReportUserResult(false));
                return;
            }
            UserReporter.Report(logger, mixWebCallFactory, userId, reportUserReason, delegate
            {
                callback(new ReportUserResult(true));
            }, delegate
            {
                callback(new ReportUserResult(false));
            });
        }

        public void AddIncomingFriendInvitation(IInternalIncomingFriendInvitation invitation)
        {
            if (!incomingFriendInvitations.Any((IInternalIncomingFriendInvitation i) => i.Id == invitation.Id))
            {
                incomingFriendInvitations.Add(invitation);
                ReceivedIncomingFriendInvitationEventArgs e = new ReceivedIncomingFriendInvitationEventArgs(invitation);
                this.OnReceivedIncomingFriendInvitation(this, e);
            }
        }

        public void AddOutgoingFriendInvitation(IInternalOutgoingFriendInvitation invitation)
        {
            IInternalOutgoingFriendInvitation internalOutgoingFriendInvitation = outgoingFriendInvitations.FirstOrDefault((IInternalOutgoingFriendInvitation i) => i.InternalInvitee.DisplayName.Text == invitation.InternalInvitee.DisplayName.Text);
            IOutgoingFriendInvitation outgoingFriendInvitation = null;
            if (internalOutgoingFriendInvitation == null)
            {
                outgoingFriendInvitations.Add(invitation);
                outgoingFriendInvitation = invitation;
            }
            else if (!internalOutgoingFriendInvitation.Sent && invitation.Sent)
            {
                internalOutgoingFriendInvitation.SendComplete(invitation.InvitationId);
                outgoingFriendInvitation = internalOutgoingFriendInvitation;
            }
            if (invitation.Sent && outgoingFriendInvitation != null)
            {
                ReceivedOutgoingFriendInvitationEventArgs e = new ReceivedOutgoingFriendInvitationEventArgs(outgoingFriendInvitation);
                this.OnReceivedOutgoingFriendInvitation(this, e);
            }
        }

        public void ClearAlerts(IEnumerable<IAlert> alerts, Action<IClearAlertsResult> callback)
        {
            try
            {
                List<IInternalAlert> list = new List<IInternalAlert>();
                foreach (IAlert alert in alerts)
                {
                    IInternalAlert internalAlert = alert as IInternalAlert;
                    if (internalAlert == null)
                    {
                        logger.Critical("Can't clear unknown alert");
                        callback(new ClearAlertsResult(false));
                        return;
                    }
                    list.Add(internalAlert);
                }
                IWebCall<ClearAlertsRequest, ClearAlertsResponse> webCall = mixWebCallFactory.AlertsClearPut(new ClearAlertsRequest
                {
                    AlertIds = list.Select((IInternalAlert a) => a.AlertId).Cast<long?>().ToList()
                });
                webCall.OnResponse += delegate (object sender, WebCallEventArgs<ClearAlertsResponse> e)
                {
                    ClearAlertsResponse response = e.Response;
                    if (NotificationValidator.Validate(response.Notification))
                    {
                        notificationQueue.Dispatch(response.Notification, delegate
                        {
                            callback(new ClearAlertsResult(true));
                        }, delegate
                        {
                            callback(new ClearAlertsResult(false));
                        });
                    }
                    else
                    {
                        logger.Critical("Failed to validate clear alerts response: " + JsonParser.ToJson(response));
                        callback(new ClearAlertsResult(false));
                    }
                };
                webCall.OnError += delegate
                {
                    callback(new ClearAlertsResult(false));
                };
                webCall.Execute();
            }
            catch (Exception ex)
            {
                logger.Critical("Unhandled exception: " + ex);
                callback(new ClearAlertsResult(false));
            }
        }

        private IInternalIncomingFriendInvitation RemoveIncomingFriendInvitation(long invitationId)
        {
            oldInvitationIds.Add(invitationId);
            IInternalIncomingFriendInvitation internalIncomingFriendInvitation = incomingFriendInvitations.FirstOrDefault((IInternalIncomingFriendInvitation i) => i.InvitationId == invitationId);
            if (internalIncomingFriendInvitation != null)
            {
                incomingFriendInvitations.Remove(internalIncomingFriendInvitation);
            }
            return internalIncomingFriendInvitation;
        }

        private IInternalOutgoingFriendInvitation RemoveOutgoingFriendInvitation(long invitationId)
        {
            oldInvitationIds.Add(invitationId);
            IInternalOutgoingFriendInvitation internalOutgoingFriendInvitation = outgoingFriendInvitations.FirstOrDefault((IInternalOutgoingFriendInvitation i) => i.InvitationId == invitationId);
            if (internalOutgoingFriendInvitation != null)
            {
                outgoingFriendInvitations.Remove(internalOutgoingFriendInvitation);
            }
            return internalOutgoingFriendInvitation;
        }

        public void AddFriendshipInvitation(FriendshipInvitation invitation, User friend)
        {
            long invitationId = invitation.FriendshipInvitationId.Value;
            if (oldInvitationIds.Contains(invitationId) || outgoingFriendInvitations.Any((IInternalOutgoingFriendInvitation i) => i.InvitationId == invitationId) || incomingFriendInvitations.Any((IInternalIncomingFriendInvitation i) => i.InvitationId == invitationId))
            {
                return;
            }
            if (invitation.IsInviter.Value)
            {
                IInternalOutgoingFriendInvitation internalOutgoingFriendInvitation = outgoingFriendInvitations.FirstOrDefault((IInternalOutgoingFriendInvitation i) => i.Invitee.DisplayName.Text == invitation.FriendDisplayName && i.RequestTrust != invitation.IsTrusted);
                if (internalOutgoingFriendInvitation != null)
                {
                    RemoveOutgoingFriendInvitation(internalOutgoingFriendInvitation.InvitationId);
                }
                IInternalUnidentifiedUser invitee = RemoteUserFactory.CreateUnidentifiedUser(friend.DisplayName, friend.FirstName, userDatabase);
                OutgoingFriendInvitation outgoingFriendInvitation = new OutgoingFriendInvitation(this, invitee, invitation.IsTrusted.Value);
                outgoingFriendInvitation.SendComplete(invitationId);
                AddOutgoingFriendInvitation(outgoingFriendInvitation);
            }
            else
            {
                IInternalIncomingFriendInvitation internalIncomingFriendInvitation = incomingFriendInvitations.FirstOrDefault((IInternalIncomingFriendInvitation i) => i.Inviter.DisplayName.Text == invitation.FriendDisplayName && i.RequestTrust != invitation.IsTrusted);
                if (internalIncomingFriendInvitation != null)
                {
                    RemoveIncomingFriendInvitation(internalIncomingFriendInvitation.InvitationId);
                }
                IInternalUnidentifiedUser inviter = RemoteUserFactory.CreateUnidentifiedUser(friend.DisplayName, friend.FirstName, userDatabase);
                IncomingFriendInvitation incomingFriendInvitation = new IncomingFriendInvitation(inviter, this, invitation.IsTrusted.Value);
                incomingFriendInvitation.SendComplete(invitationId);
                AddIncomingFriendInvitation(incomingFriendInvitation);
            }
        }

        public void RemoveFriendshipInvitation(long invitationId)
        {
            IInternalIncomingFriendInvitation internalIncomingFriendInvitation = RemoveIncomingFriendInvitation(invitationId);
            if (internalIncomingFriendInvitation != null)
            {
                internalIncomingFriendInvitation.Rejected();
                return;
            }
            IInternalOutgoingFriendInvitation internalOutgoingFriendInvitation = RemoveOutgoingFriendInvitation(invitationId);
            if (internalOutgoingFriendInvitation != null)
            {
                internalOutgoingFriendInvitation.Rejected();
            }
        }

        public void AddFriend(User domainFriend, bool isTrusted, long invitationId)
        {
            IInternalFriend friend = RemoteUserFactory.CreateFriend(domainFriend.UserId, isTrusted, domainFriend.DisplayName, domainFriend.FirstName, userDatabase);
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Id == friend.Id && f.IsTrusted != friend.IsTrusted);
            if (internalFriend != null)
            {
                friends.Remove(internalFriend);
            }
            if (friends.Any((IInternalFriend f) => f.Id == friend.Id))
            {
                return;
            }
            friends.Add(friend);
            IInternalIncomingFriendInvitation internalIncomingFriendInvitation = RemoveIncomingFriendInvitation(invitationId);
            if (internalIncomingFriendInvitation != null)
            {
                internalIncomingFriendInvitation.Accepted(isTrusted, friend);
                return;
            }
            IInternalOutgoingFriendInvitation internalOutgoingFriendInvitation = RemoveOutgoingFriendInvitation(invitationId);
            if (internalOutgoingFriendInvitation != null)
            {
                internalOutgoingFriendInvitation.Accepted(isTrusted, friend);
            }
        }

        public void RemoveFriend(string friendSwid, bool sendEvent)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Swid == friendSwid);
            friends.Remove(internalFriend);
            if (internalFriend != null && sendEvent)
            {
                UnfriendedEventArgs e = new UnfriendedEventArgs(internalFriend);
                this.OnUnfriended(this, e);
            }
        }

        public void UntrustFriend(string friendSwid)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Swid == friendSwid);
            if (internalFriend != null)
            {
                UntrustFriend(internalFriend);
            }
        }

        public void UntrustFriend(IInternalFriend friend)
        {
            friend.ChangeTrust(false);
            UntrustedEventArgs e = new UntrustedEventArgs(friend);
            this.OnUntrusted(this, e);
        }

        public void AddChatThread(ChatThread chatThread)
        {
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            if (!allChatThreads.Any((IInternalChatThread c) => c.ChatThreadId == chatThread.ChatThreadId.Value))
            {
                switch (chatThread.ChatThreadType)
                {
                    case "ONE_ON_ONE":
                        {
                            IInternalOneOnOneChatThread internalOneOnOneChatThread = chatThreadCreator.CreateLocalOneOnOneChatThread(mixWebCallFactory, chatThread);
                            oneOnOneChatThreads.Add(internalOneOnOneChatThread);
                            this.OnAddedToOneOnOneChatThread(this, new AddedToOneOnOneChatThreadEventArgs(internalOneOnOneChatThread));
                            break;
                        }
                    case "GROUP":
                        {
                            IInternalGroupChatThread internalGroupChatThread = chatThreadCreator.CreateLocalGroupChatThread(mixWebCallFactory, chatThread);
                            groupChatThreads.Add(internalGroupChatThread);
                            this.OnAddedToGroupChatThread(this, new AddedToGroupChatThreadEventArgs(internalGroupChatThread));
                            break;
                        }
                    case "OFFICIAL_ACCOUNT":
                        {
                            IInternalOfficialAccountChatThread internalOfficialAccountChatThread = chatThreadCreator.CreateLocalOfficialAccountChatThread(mixWebCallFactory, chatThread.ChatThreadId.Value, chatThread.OfficialAccountId);
                            AddChatMembers(internalOfficialAccountChatThread, chatThread);
                            officialAccountChatThreads.Add(internalOfficialAccountChatThread);
                            this.OnAddedToOfficialAccountChatThread(this, new AddedToOfficialAccountChatThreadEventArgs(internalOfficialAccountChatThread));
                            break;
                        }
                }
            }
        }

        private void AddChatMembers(IInternalChatThread newChatThread, ChatThread chatThread)
        {
            foreach (User member2 in chatThread.Members)
            {
                if (Swid != member2.UserId && !OfficialAccountUtils.IsOfficialAccountId(member2.UserId))
                {
                    IInternalRemoteChatMember member = RemoteUserFactory.CreateRemoteChatMember(member2.UserId, member2.DisplayName, member2.FirstName, userDatabase);
                    newChatThread.AddRemoteMember(member, false);
                }
            }
        }

        public void AddChatThreadMembership(long chatThreadId, IEnumerable<User> members)
        {
            IInternalGroupChatThread internalGroupChatThread = groupChatThreads.FirstOrDefault((IInternalGroupChatThread c) => c.ChatThreadId == chatThreadId);
            if (internalGroupChatThread != null)
            {
                {
                    User member;
                    foreach (User member5 in members)
                    {
                        member = member5;
                        if (!internalGroupChatThread.InternalMembers.Any((IInternalRemoteChatMember m) => m.Swid == member.UserId))
                        {
                            IInternalRemoteChatMember member2 = RemoteUserFactory.CreateRemoteChatMember(member.UserId, member.DisplayName, member.FirstName, userDatabase);
                            internalGroupChatThread.AddRemoteMember(member2, true);
                        }
                    }
                    return;
                }
            }
            IInternalOfficialAccountChatThread internalOfficialAccountChatThread = officialAccountChatThreads.FirstOrDefault((IInternalOfficialAccountChatThread c) => c.ChatThreadId == chatThreadId);
            if (internalOfficialAccountChatThread == null)
            {
                return;
            }
            User member3;
            foreach (User member6 in members)
            {
                member3 = member6;
                if (!internalOfficialAccountChatThread.InternalMembers.Any((IInternalRemoteChatMember m) => m.Swid == member3.UserId))
                {
                    IInternalRemoteChatMember member4 = RemoteUserFactory.CreateRemoteChatMember(member3.UserId, member3.DisplayName, member3.FirstName, userDatabase);
                    internalOfficialAccountChatThread.AddRemoteMember(member4, true);
                }
            }
        }

        public void RemoveChatThreadMembership(long chatThreadId, string memberUserId)
        {
            IInternalGroupChatThread internalGroupChatThread = groupChatThreads.FirstOrDefault((IInternalGroupChatThread c) => c.ChatThreadId == chatThreadId);
            if (internalGroupChatThread != null)
            {
                if (memberUserId == Swid)
                {
                    groupChatThreads.Remove(internalGroupChatThread);
                    internalGroupChatThread.DispatchLocalUserRemovedEvent();
                    return;
                }
                IInternalRemoteChatMember internalRemoteChatMember = internalGroupChatThread.InternalMembers.FirstOrDefault((IInternalRemoteChatMember m) => m.Swid == memberUserId);
                if (internalRemoteChatMember != null)
                {
                    internalGroupChatThread.RemoveRemoteMember(internalRemoteChatMember, true, true);
                }
                return;
            }
            IInternalOfficialAccountChatThread internalOfficialAccountChatThread = officialAccountChatThreads.FirstOrDefault((IInternalOfficialAccountChatThread c) => c.ChatThreadId == chatThreadId);
            if (internalOfficialAccountChatThread != null)
            {
                IInternalRemoteChatMember internalRemoteChatMember2 = internalOfficialAccountChatThread.InternalMembers.FirstOrDefault((IInternalRemoteChatMember m) => m.Swid == memberUserId);
                if (internalRemoteChatMember2 != null)
                {
                    internalOfficialAccountChatThread.RemoveRemoteMember(internalRemoteChatMember2, true, true);
                }
            }
        }

        private void GetChatThreadAndSender<TMessage>(BaseChatMessage message, out IInternalChatThread chatThread, out bool senderExists, out TMessage chatMessage) where TMessage : IInternalChatMessage
        {
            senderExists = false;
            chatMessage = default(TMessage);
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            chatThread = allChatThreads.FirstOrDefault((IInternalChatThread c) => c.ChatThreadId == message.ChatThreadId);
            if (chatThread == null)
            {
                return;
            }
            IInternalChatMessage internalChatMessage = chatThread.InternalChatMessages.FirstOrDefault((IInternalChatMessage c) => c.ChatMessageId == message.ChatMessageId.Value);
            if (internalChatMessage == null)
            {
                long? clientChatMessageId = message.ClientChatMessageId;
                if (clientChatMessageId.HasValue && message.ClientChatMessageId != 0)
                {
                    IInternalChatMessage internalChatMessage2 = chatThread.InternalChatMessages.FirstOrDefault((IInternalChatMessage c) => c.LocalChatMessageId == message.ClientChatMessageId);
                    if (internalChatMessage2 != null)
                    {
                        chatMessage = (TMessage)internalChatMessage2;
                    }
                }
                IOfficialAccountChatThread officialAccountChatThread = chatThread as IOfficialAccountChatThread;
                senderExists = Swid == message.SenderUserId || officialAccountChatThread != null;
                if (!senderExists)
                {
                    senderExists = chatThread.InternalMembers.Any((IInternalRemoteChatMember m) => m.Swid == message.SenderUserId);
                }
            }
            else
            {
                chatThread = null;
            }
        }

        public void AddChatTextMessage(TextChatMessage message)
        {
            AddChatMessage(message, (IInternalChatThread thread) => true, () => chatMessageFactory.CreateTextMessage(true, message.SequenceNumber.Value, message.SenderUserId, message.Text), delegate (IInternalTextMessage cm, IInternalChatThread thread, bool addMessage)
            {
                thread.AddTextMessage(cm, addMessage, true);
            });
        }

        public void AddChatStickerMessage(StickerChatMessage message)
        {
            AddChatMessage(message, (IInternalChatThread thread) => true, () => chatMessageFactory.CreateStickerMessage(true, message.SequenceNumber.Value, message.SenderUserId, message.ContentId), delegate (IInternalStickerMessage cm, IInternalChatThread thread, bool addMessage)
            {
                thread.AddStickerMessage(cm, addMessage, true);
            });
        }

        public void AddChatGagMessage(GagChatMessage message)
        {
            AddChatMessage(message, (IInternalChatThread thread) => Swid == message.TargetUserId || thread.InternalMembers.Any((IInternalRemoteChatMember m) => m.Swid == message.TargetUserId) || thread.InternalFormerMembers.Any((IInternalRemoteChatMember m) => m.Swid == message.TargetUserId), () => chatMessageFactory.CreateGagMessage(true, message.SequenceNumber.Value, message.SenderUserId, message.ContentId, message.TargetUserId), delegate (IInternalGagMessage cm, IInternalChatThread thread, bool addMessage)
            {
                thread.AddGagMessage(cm, addMessage, true);
            });
        }

        private void AddChatMessage<TDomainMessage, TMessage>(TDomainMessage domainMessage, Func<IInternalChatThread, bool> checkTargetExists, Func<TMessage> createChatMessage, Action<TMessage, IInternalChatThread, bool> addChatMessage) where TDomainMessage : BaseChatMessage where TMessage : IInternalChatMessage
        {
            IInternalChatThread chatThread;
            bool senderExists;
            TMessage chatMessage;
            GetChatThreadAndSender<TMessage>(domainMessage, out chatThread, out senderExists, out chatMessage);
            if (senderExists)
            {
                bool flag = chatMessage == null;
                if (flag && checkTargetExists(chatThread))
                {
                    chatMessage = createChatMessage();
                }
                if (chatMessage == null)
                {
                    logger.Critical("Received a gag whose target was not in the thread!");
                    return;
                }
                chatMessage.SendCompleteWithOffsetTime(domainMessage.ChatMessageId.Value, domainMessage.Created.Value, domainMessage.SequenceNumber.Value);
                chatMessage.LocalChatMessageId = domainMessage.ClientChatMessageId.Value;
                addChatMessage(chatMessage, chatThread, flag);
            }
        }

        public void AddChatPhotoMessage(PhotoChatMessage message)
        {
            AddChatMessage(message, (IInternalChatThread thread) => true, delegate
            {
                List<IPhotoFlavor> photoFlavors = message.PhotoFlavors.Select((Disney.Mix.SDK.Internal.MixDomain.PhotoFlavor f) => photoFlavorFactory.Create(message.PhotoId, f.PhotoFlavorId, f.Url, PhotoEncodingTypeConverter.ConvertServerTypeToDatabaseType(f.Encoding), f.Width.Value, f.Height.Value, null)).ToList();
                return chatMessageFactory.CreatePhotoMessage(true, message.SequenceNumber.Value, message.SenderUserId, message.PhotoId, message.Caption, photoFlavors);
            }, delegate (IInternalPhotoMessage cm, IInternalChatThread thread, bool addMessage)
            {
                thread.AddPhotoMessage(cm, addMessage, true);
            });
        }

        public void AddChatVideoMessage(VideoChatMessage message)
        {
            AddChatMessage(message, (IInternalChatThread thread) => true, delegate
            {
                IPhotoFlavor thumbnail = photoFlavorFactory.Create(message.VideoId, message.Thumbnail.PhotoFlavorId, message.Thumbnail.Url, PhotoEncodingTypeConverter.ConvertServerTypeToDatabaseType(message.Thumbnail.Encoding), message.Thumbnail.Width.Value, message.Thumbnail.Height.Value, null);
                List<IVideoFlavor> videoFlavors = message.VideoFlavors.Select((Disney.Mix.SDK.Internal.MixDomain.VideoFlavor f) => videoFlavorFactory.Create(message.VideoId, f.VideoFlavorId, f.Bitrate.Value, VideoFormatTypeConverter.ConvertServerTypeToDatabaseType(f.Format), f.Width.Value, f.Height.Value, mixWebCallFactory)).ToList();
                return chatMessageFactory.CreateVideoMessage(true, message.SequenceNumber.Value, message.SenderUserId, message.VideoId, message.Caption, message.Duration.Value, thumbnail, videoFlavors);
            }, delegate (IInternalVideoMessage cm, IInternalChatThread thread, bool addMessage)
            {
                thread.AddVideoMessage(cm, addMessage, true);
            });
        }

        public void AddChatMemberListChangedMessage(MemberListChangedChatMessage message)
        {
            bool value = message.IsAdded.Value;
            IInternalChatThread chatThread;
            bool senderExists;
            IInternalChatMessage chatMessage;
            GetChatThreadAndSender<IInternalChatMessage>(message, out chatThread, out senderExists, out chatMessage);
            if (chatThread == null)
            {
                return;
            }
            List<string> list = new List<string>();
            string memberId;
            foreach (string memberUserId in message.MemberUserIds)
            {
                memberId = memberUserId;
                if (chatThread.InternalMembers.Any((IInternalRemoteChatMember m) => m.Swid == memberId) || chatThread.InternalFormerMembers.Any((IInternalRemoteChatMember m) => m.Swid == memberId))
                {
                    list.Add(memberId);
                }
            }
            if (list.Count > 0)
            {
                if (value)
                {
                    IInternalChatMemberAddedMessage internalChatMemberAddedMessage = chatMessageFactory.CreateChatMemberAddedMessage(true, message.SequenceNumber.Value, list);
                    internalChatMemberAddedMessage.SendCompleteWithOffsetTime(message.ChatMessageId.Value, message.Created.Value, message.SequenceNumber.Value);
                    chatThread.AddMemberAddedMessage(internalChatMemberAddedMessage, true);
                }
                else
                {
                    IInternalChatMemberRemovedMessage internalChatMemberRemovedMessage = chatMessageFactory.CreateChatMemberRemovedMessage(true, message.SequenceNumber.Value, list.First());
                    internalChatMemberRemovedMessage.SendCompleteWithOffsetTime(message.ChatMessageId.Value, message.Created.Value, message.SequenceNumber.Value);
                    chatThread.AddMemberRemovedMessage(internalChatMemberRemovedMessage, true);
                }
            }
        }

        public void AddChatGameStateMessage(GameStateChatMessage message)
        {
            AddChatMessage(message, (IInternalChatThread thread) => true, () => chatMessageFactory.CreateGameStateMessage(true, message.SequenceNumber.Value, message.SenderUserId, message.GameName, JsonParser.FromJson<Dictionary<string, object>>(message.State)), delegate (IInternalGameStateMessage cm, IInternalChatThread thread, bool addMessage)
            {
                Dictionary<string, object> state = JsonParser.FromJson<Dictionary<string, object>>(message.State);
                cm.UpdateState(null, state);
                thread.AddGameStateMessage(cm, addMessage, true);
            });
        }

        public void UpdateChatGameStateMessage(GameStateChatMessage message, string result)
        {
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            IInternalChatThread internalChatThread = allChatThreads.FirstOrDefault((IInternalChatThread c) => c.ChatThreadId == message.ChatThreadId);
            if (internalChatThread != null)
            {
                IInternalGameStateMessage internalGameStateMessage = internalChatThread.InternalChatMessages.FirstOrDefault((IInternalChatMessage m) => m.ChatMessageId == message.ChatMessageId) as IInternalGameStateMessage;
                if (internalGameStateMessage != null)
                {
                    internalGameStateMessage.UpdateState(result, JsonParser.FromJson<Dictionary<string, object>>(message.State));
                }
            }
        }

        public void AddChatGameEventMessage(GameEventChatMessage message)
        {
            IInternalGameStateMessage gameStateMessage = null;
            AddChatMessage(message, delegate (IInternalChatThread thread)
            {
                gameStateMessage = thread.InternalChatMessages.FirstOrDefault((IInternalChatMessage m) => m.ChatMessageId == message.GameStateMessageId) as IInternalGameStateMessage;
                return gameStateMessage != null;
            }, () => chatMessageFactory.CreateGameEventMessage(true, message.SequenceNumber.Value, message.SenderUserId, gameStateMessage, message.GameName, JsonParser.FromJson<Dictionary<string, object>>(message.Payload)), delegate (IInternalGameEventMessage cm, IInternalChatThread thread, bool addMessage)
            {
                thread.AddGameEventMessage(cm, true);
            });
        }

        public void AddNickname(Disney.Mix.SDK.Internal.MixDomain.UserNickname userNickname)
        {
            UserNickname userNickname2 = new UserNickname(userNickname.Nickname);
            userNickname2.ApplyFinished();
            UpdateNickname(userNickname.NicknamedUserId, userNickname2);
        }

        public void UpdateNickname(string userId, IUserNickname nickname)
        {
            IInternalFriend internalFriend = friends.FirstOrDefault((IInternalFriend f) => f.Swid == userId);
            if (internalFriend != null)
            {
                internalFriend.UpdateNickname(nickname);
            }
        }

        public void UpdateChatTrustLevel(long chatThreadId, bool isTrusted)
        {
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            IInternalChatThread internalChatThread = allChatThreads.FirstOrDefault((IInternalChatThread c) => c.ChatThreadId == chatThreadId);
            if (internalChatThread != null)
            {
                internalChatThread.UpdateChatTrustLevel(isTrusted);
            }
        }

        public void AddChatThreadNickname(Disney.Mix.SDK.Internal.MixDomain.ChatThreadNickname nickname)
        {
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            IInternalChatThread internalChatThread = allChatThreads.FirstOrDefault((IInternalChatThread c) => c.ChatThreadId == nickname.ChatThreadId);
            if (internalChatThread != null)
            {
                internalChatThread.UpdateNickname(nickname.Nickname);
            }
        }

        public void RemoveChatThreadNickname(long chatThreadId)
        {
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            IInternalChatThread internalChatThread = allChatThreads.FirstOrDefault((IInternalChatThread c) => c.ChatThreadId == chatThreadId);
            if (internalChatThread != null)
            {
                internalChatThread.UpdateNickname(null);
            }
        }

        public void ClearChatThreadHistory(IEnumerable<long> chatThreadIds)
        {
            long chatThreadId;
            foreach (long chatThreadId2 in chatThreadIds)
            {
                chatThreadId = chatThreadId2;
                IInternalOneOnOneChatThread internalOneOnOneChatThread = oneOnOneChatThreads.FirstOrDefault((IInternalOneOnOneChatThread c) => c.ChatThreadId == chatThreadId);
                if (internalOneOnOneChatThread != null)
                {
                    internalOneOnOneChatThread.ClearChatHistory();
                }
                IInternalOfficialAccountChatThread internalOfficialAccountChatThread = officialAccountChatThreads.FirstOrDefault((IInternalOfficialAccountChatThread c) => c.ChatThreadId == chatThreadId);
                if (internalOfficialAccountChatThread != null)
                {
                    internalOfficialAccountChatThread.ClearChatHistory();
                }
            }
        }

        public void ClearUnreadMessageCount(IEnumerable<long> chatThreadIds)
        {
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            long chatThreadId;
            foreach (long chatThreadId2 in chatThreadIds)
            {
                chatThreadId = chatThreadId2;
                IInternalChatThread internalChatThread = allChatThreads.FirstOrDefault((IInternalChatThread c) => c.ChatThreadId == chatThreadId);
                if (internalChatThread != null)
                {
                    internalChatThread.UpdateUnreadMessageCount(0u);
                }
            }
        }

        public void UpdateAvatar(Disney.Mix.SDK.Internal.MixDomain.Avatar avatar)
        {
            IInternalAvatar updatedAvatar = AvatarBuilder.Build(avatar);
            CheckForAvatar(InternalAvatar, updatedAvatar, DispatchOnAvatarChanged);
            foreach (IInternalFriend friend in friends)
            {
                CheckForAvatar(friend.InternalAvatar, updatedAvatar, friend.DispatchOnAvatarChanged);
            }
            foreach (IInternalOutgoingFriendInvitation outgoingFriendInvitation in outgoingFriendInvitations)
            {
                IInternalUnidentifiedUser internalInvitee = outgoingFriendInvitation.InternalInvitee;
                CheckForAvatar(internalInvitee.InternalAvatar, updatedAvatar, internalInvitee.DispatchOnAvatarChanged);
            }
            foreach (IInternalIncomingFriendInvitation incomingFriendInvitation in incomingFriendInvitations)
            {
                IInternalUnidentifiedUser internalInviter = incomingFriendInvitation.InternalInviter;
                CheckForAvatar(internalInviter.InternalAvatar, updatedAvatar, internalInviter.DispatchOnAvatarChanged);
            }
            IEnumerable<IInternalChatThread> allChatThreads = GetAllChatThreads();
            foreach (IInternalChatThread item in allChatThreads)
            {
                foreach (IInternalRemoteChatMember internalMember in item.InternalMembers)
                {
                    CheckForAvatar(internalMember.InternalAvatar, updatedAvatar, internalMember.DispatchOnAvatarChanged);
                }
                foreach (IInternalRemoteChatMember internalFormerMember in item.InternalFormerMembers)
                {
                    CheckForAvatar(internalFormerMember.InternalAvatar, updatedAvatar, internalFormerMember.DispatchOnAvatarChanged);
                }
            }
            foreach (IInternalUnidentifiedUser unidentifiedUser in unidentifiedUsers)
            {
                CheckForAvatar(unidentifiedUser.InternalAvatar, updatedAvatar, unidentifiedUser.DispatchOnAvatarChanged);
            }
        }

        private static void CheckForAvatar(IInternalAvatar userAvatar, IInternalAvatar updatedAvatar, Action updateAvatar)
        {
            if (userAvatar != null && userAvatar.AvatarId == updatedAvatar.AvatarId)
            {
                updateAvatar();
            }
        }

        public void AddFriend(IInternalFriend friend)
        {
            friends.Add(friend);
        }

        public void RemoveFriend(IInternalFriend friend)
        {
            friends.Remove(friend);
            this.OnUnfriended(this, new UnfriendedEventArgs(friend));
        }

        public void AddOneOnOneChatThread(IInternalOneOnOneChatThread chatThread)
        {
            oneOnOneChatThreads.Add(chatThread);
            this.OnAddedToOneOnOneChatThread(this, new AddedToOneOnOneChatThreadEventArgs(chatThread));
        }

        public void AddGroupChatThread(IInternalGroupChatThread chatThread)
        {
            groupChatThreads.Add(chatThread);
            this.OnAddedToGroupChatThread(this, new AddedToGroupChatThreadEventArgs(chatThread));
        }

        public void AddOfficialAccountChatThread(IInternalOfficialAccountChatThread chatThread)
        {
            officialAccountChatThreads.Add(chatThread);
            this.OnAddedToOfficialAccountChatThread(this, new AddedToOfficialAccountChatThreadEventArgs(chatThread));
        }

        public void RemoveOneOnOneChatThread(IInternalOneOnOneChatThread chatThread)
        {
            oneOnOneChatThreads.Remove(chatThread);
        }

        public void RemoveGroupChatThread(IInternalGroupChatThread chatThread)
        {
            groupChatThreads.Remove(chatThread);
        }

        public void RemoveIncomingFriendInvitation(IInternalIncomingFriendInvitation invitation)
        {
            oldInvitationIds.Add(invitation.InvitationId);
            incomingFriendInvitations.Remove(invitation);
        }

        public void RemoveOutgoingFriendInvitation(IInternalOutgoingFriendInvitation invitation)
        {
            oldInvitationIds.Add(invitation.InvitationId);
            outgoingFriendInvitations.Remove(invitation);
        }

        public void UpdateFollowships(IEnumerable<IOfficialAccount> officialAccounts)
        {
            followships.Clear();
            foreach (IOfficialAccount officialAccount in officialAccounts)
            {
                followships.Add(officialAccount);
            }
        }

        public void GetAdultVerificationRequirements(Action<IGetAdultVerificationRequirementsResult> callback)
        {
            AdultVerificationRequirementsGetter.GetRequirements(logger, registrationProfile.CountryCode, mixWebCallFactory, delegate (bool required, bool available)
            {
                callback(new GetAdultVerificationRequirementsResult(true, required, available));
            }, delegate
            {
                callback(new GetAdultVerificationRequirementsResult(false, false, false));
            });
        }

        public void GetAdultVerificationStatus(Action<IGetAdultVerificationStatusResult> callback)
        {
            AdultVerificationStatusGetter.GetAdultVerificationStatus(logger, guestControllerClient, callback);
        }

        public void GetVerifyAdultForm(Action<IGetVerifyAdultFormResult> callback)
        {
            VerifyAdultFormUnitedStates form = new VerifyAdultFormUnitedStates();
            callback(new GetVerifyAdultFormResult(form));
        }

        public void VerifyAdult(IVerifyAdultFormUnitedStates form, Action<IVerifyAdultResult> callback)
        {
            if (registrationProfile.IsAdultVerified)
            {
                callback(new VerifyAdultResult(true, false));
                return;
            }
            if (AgeBandType != AgeBandType.Adult)
            {
                callback(new VerifyAdultFailedNotAdultResult());
                return;
            }
            if (string.IsNullOrEmpty(form.FirstName) || string.IsNullOrEmpty(form.LastName) || string.IsNullOrEmpty(form.PostalCode))
            {
                callback(new VerifyAdultFailedMissingInfoResult());
                return;
            }
            AdultVerifierUnitedStates.VerifyAdult(logger, guestControllerClient, form, delegate (IVerifyAdultResult r)
            {
                if (r.Success)
                {
                    registrationProfile.IsAdultVerified = true;
                }
                callback(r);
            });
        }

        public void AnswerVerifyAdultQuiz(IVerifyAdultQuizAnswers answers, Action<IVerifyAdultResult> callback)
        {
            VerifyAdultQuizAnswersSender.AnswerQuiz(logger, guestControllerClient, answers, delegate (IVerifyAdultResult r)
            {
                if (r.Success)
                {
                    registrationProfile.IsAdultVerified = true;
                }
                callback(r);
            });
        }

        public void GetClaimableChildren(Action<IGetLinkedUsersResult> callback)
        {
            if (!registrationProfile.EmailVerified)
            {
                callback(new GetLinkedUsersFailedEmailNotVerifiedResult());
            }
            else if (AgeBandType != AgeBandType.Adult)
            {
                callback(new GetLinkedUsersFailedNotAdultResult());
            }
            else
            {
                ClaimableChildrenGetter.GetChildren(logger, guestControllerClient, mixWebCallFactory, callback);
            }
        }

        public void LinkChildAccount(ISession child, Action<ILinkChildResult> callback)
        {
            if (!(child is IInternalSession))
            {
                callback(new LinkChildResult(false));
                return;
            }
            if (child.LocalUser.AgeBandType != AgeBandType.Child)
            {
                callback(new LinkChildFailedNotChildResult());
                return;
            }
            if (AgeBandType != AgeBandType.Adult)
            {
                callback(new LinkChildFailedNotAdultResult());
                return;
            }
            IInternalSession internalSession = (IInternalSession)child;
            ChildLinker.LinkChild(logger, guestControllerClient, internalSession.InternalLocalUser.Swid, internalSession.GuestControllerAccessToken, callback);
        }

        public void LinkClaimableChildAccounts(IEnumerable<ILinkedUser> children, Action<ILinkChildResult> callback)
        {
            if (AgeBandType != AgeBandType.Adult)
            {
                callback(new LinkChildFailedNotAdultResult());
                return;
            }
            List<string> list = new List<string>();
            foreach (ILinkedUser child in children)
            {
                if (!(child is IInternalLinkedUser))
                {
                    callback(new LinkChildResult(false));
                    return;
                }
                list.Add(((IInternalLinkedUser)child).Swid);
            }
            ChildLinker.LinkClaimableChildren(logger, guestControllerClient, list, callback);
        }

        public void GetLinkedChildren(Action<IGetLinkedUsersResult> callback)
        {
            if (AgeBandType != AgeBandType.Adult)
            {
                callback(new GetLinkedUsersFailedNotAdultResult());
            }
            else
            {
                LinkedChildrenGetter.GetChildren(logger, guestControllerClient, mixWebCallFactory, callback);
            }
        }

        public void GetLinkedGuardians(Action<IGetLinkedUsersResult> callback)
        {
            LinkedGuardiansGetter.GetGuardians(logger, guestControllerClient, mixWebCallFactory, callback);
        }

        public void RequestTrustPermission(Action<IPermissionResult> callback)
        {
            if (AgeBandType == AgeBandType.Adult || AgeBandType == AgeBandType.Teen)
            {
                callback(new PermissionNotRequiredResult());
            }
            else
            {
                PermissionRequester.RequestPermission(logger, guestControllerClient, "MIX_TRUSTEDFRIENDSCOMMUNICATIONS", callback);
            }
        }

        public void RequestTrustPermissionForChild(ILinkedUser child, Action<IPermissionResult> callback)
        {
            if (AgeBandType != AgeBandType.Adult)
            {
                callback(new PermissionFailedNotAdultResult());
                return;
            }
            IInternalLinkedUser internalLinkedUser = child as IInternalLinkedUser;
            if (internalLinkedUser == null)
            {
                callback(new PermissionResult(false, ActivityApprovalStatus.Unknown));
            }
            else
            {
                PermissionRequester.RequestPermissionForChild(logger, guestControllerClient, "MIX_TRUSTEDFRIENDSCOMMUNICATIONS", internalLinkedUser.Swid, callback);
            }
        }

        public void ApproveChildTrustPermission(ISession child, ActivityApprovalStatus status, Action<IPermissionResult> callback)
        {
            ApproveChildTrustPermission(child.LocalUser.Id, child.LocalUser.AgeBandType, status, callback);
        }

        public void ApproveChildTrustPermission(ILinkedUser child, ActivityApprovalStatus status, Action<IPermissionResult> callback)
        {
            IInternalLinkedUser internalLinkedUser = child as IInternalLinkedUser;
            if (internalLinkedUser == null)
            {
                callback(new PermissionFailedInvalidResult());
            }
            else
            {
                ApproveChildTrustPermission(internalLinkedUser.Swid, child.AgeBand, status, callback);
            }
        }

        private void ApproveChildTrustPermission(string childSwid, AgeBandType childAgeBand, ActivityApprovalStatus status, Action<IPermissionResult> callback)
        {
            if (AgeBandType != AgeBandType.Adult)
            {
                callback(new PermissionFailedNotAdultResult());
            }
            else if (childAgeBand == AgeBandType.Adult || childAgeBand == AgeBandType.Teen)
            {
                callback(new PermissionNotRequiredResult());
            }
            else
            {
                PermissionApprover.ApprovePermission(logger, guestControllerClient, "MIX_TRUSTEDFRIENDSCOMMUNICATIONS", childSwid, status, callback);
            }
        }

        public void GetChildTrustPermission(ISession child, Action<IPermissionResult> callback)
        {
            GetChildTrustPermission(child.LocalUser.Id, child.LocalUser.AgeBandType, callback);
        }

        public void GetChildTrustPermission(ILinkedUser child, Action<IPermissionResult> callback)
        {
            IInternalLinkedUser internalLinkedUser = child as IInternalLinkedUser;
            if (internalLinkedUser == null)
            {
                callback(new PermissionFailedInvalidResult());
            }
            else
            {
                GetChildTrustPermission(internalLinkedUser.Swid, internalLinkedUser.AgeBand, callback);
            }
        }

        private void GetChildTrustPermission(string childSwid, AgeBandType childAgeBand, Action<IPermissionResult> callback)
        {
            if (childAgeBand == AgeBandType.Adult || childAgeBand == AgeBandType.Teen)
            {
                callback(new PermissionNotRequiredResult());
            }
            else
            {
                PermissionGetter.GetPermission(logger, guestControllerClient, "MIX_TRUSTEDFRIENDSCOMMUNICATIONS", childSwid, callback);
            }
        }
    }
}