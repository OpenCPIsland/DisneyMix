using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Disney.Mix.SDK;
using LitJson;
using Mix.DeviceDb;
using Mix.FakeFriend.Datatypes;
using Mix.FakeFriend.Results;
using Mix.Session.Local.Messages;
using Mix.Session.Local.Thread;

namespace Mix.Session.Local
{
    public class LocalUserWrapper : ILocalUser
    {
        public static int fakeObjectIds;

        protected ILocalUser realUser;

        public IEnumerable<IFriend> fakeFriends;

        public IEnumerable<IOneOnOneChatThread> fakeThreads;

        private FakeFriendData mixbot;

        private FakeThread mixbotThread;

        public IEnumerable<IAlert> Alerts
        {
            get
            {
                return realUser.Alerts;
            }
        }

        public IEnumerable<IFriend> Friends
        {
            get
            {
                return fakeFriends.Concat(realUser.Friends);
            }
        }

        public IEnumerable<IIncomingFriendInvitation> IncomingFriendInvitations
        {
            get
            {
                return realUser.IncomingFriendInvitations.Where((IIncomingFriendInvitation inv) => !PendingFriendInvitations.Contains(inv));
            }
        }

        public IEnumerable<IOutgoingFriendInvitation> OutgoingFriendInvitations
        {
            get
            {
                return realUser.OutgoingFriendInvitations;
            }
        }

        public List<IIncomingFriendInvitation> PendingFriendInvitations { get; set; }

        public IEnumerable<IOfficialAccount> Followships
        {
            get
            {
                return realUser.Followships;
            }
        }

        public IEnumerable<IOfficialAccount> AllOfficialAccounts
        {
            get
            {
                return realUser.AllOfficialAccounts;
            }
        }

        public IEnumerable<IOneOnOneChatThread> OneOnOneChatThreads
        {
            get
            {
                return fakeThreads.Concat(realUser.OneOnOneChatThreads);
            }
        }

        public IEnumerable<IGroupChatThread> GroupChatThreads
        {
            get
            {
                return realUser.GroupChatThreads;
            }
        }

        public IEnumerable<IOfficialAccountChatThread> OfficialAccountChatThreads
        {
            get
            {
                IEnumerable<IOfficialAccountChatThread> result;
                if (realUser.OfficialAccountChatThreads == null)
                {
                    IEnumerable<IOfficialAccountChatThread> enumerable = new List<IOfficialAccountChatThread>();
                    result = enumerable;
                }
                else
                {
                    result = realUser.OfficialAccountChatThreads;
                }
                return result;
            }
        }

        public AgeBandType AgeBandType
        {
            get
            {
                return realUser.AgeBandType;
            }
        }

        public string HashedId
        {
            get
            {
                return realUser.HashedId;
            }
        }

        public IRegistrationProfile RegistrationProfile
        {
            get
            {
                return realUser.RegistrationProfile;
            }
        }

        public IDisplayName DisplayName
        {
            get
            {
                return realUser.DisplayName;
            }
        }

        public string FirstName
        {
            get
            {
                return realUser.FirstName;
            }
        }

        public IAvatar Avatar
        {
            get
            {
                return realUser.Avatar;
            }
        }

        public string Id
        {
            get
            {
                return realUser.Id;
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

        public event EventHandler<AbstractAvatarChangedEventArgs> OnAvatarChanged = delegate
        {
        };

        public event EventHandler<AbstractOfficialAccountFollowedEventArgs> OnOfficialAccountFollowed = delegate
        {
        };

        public event EventHandler<AbstractOfficialAccountUnfollowedEventArgs> OnOfficialAccountUnfollowed = delegate
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

        public LocalUserWrapper(ILocalUser aRealUser)
        {
            realUser = aRealUser;
            SetupListeners();
            mixbot = new FakeFriendData();
            fakeFriends = new List<IFriend>();
            (fakeFriends as List<IFriend>).Add(mixbot);
            fakeThreads = new List<IOneOnOneChatThread>();
            mixbotThread = new FakeThread(this, new List<IRemoteChatMember> { mixbot });
            (fakeThreads as List<IOneOnOneChatThread>).Add(mixbotThread);
            PendingFriendInvitations = new List<IIncomingFriendInvitation>();
        }

        public void ReportUser(IRemoteChatMember remote, ReportUserReason reason, Action<IReportUserResult> result)
        {
            realUser.ReportUser(remote, reason, result);
        }

        public void ReportUser(IFriend remote, ReportUserReason reason, Action<IReportUserResult> result)
        {
            realUser.ReportUser(remote, reason, result);
        }

        public void FixLocalUser(ILocalUser aRealUser)
        {
            if (aRealUser.Id.Equals(realUser.Id))
            {
                realUser = aRealUser;
                SetupListeners();
            }
        }

        private void SetupListeners()
        {
            realUser.OnAddedToOneOnOneChatThread += delegate (object s, AbstractAddedToOneOnOneChatThreadEventArgs e)
            {
                this.OnAddedToOneOnOneChatThread(s, e);
            };
            realUser.OnAddedToGroupChatThread += delegate (object s, AbstractAddedToGroupChatThreadEventArgs e)
            {
                this.OnAddedToGroupChatThread(s, e);
            };
            realUser.OnAddedToOfficialAccountChatThread += delegate (object s, AbstractAddedToOfficialAccountThreadEventArgs e)
            {
                this.OnAddedToOfficialAccountChatThread(s, e);
            };
            realUser.OnReceivedIncomingFriendInvitation += delegate (object s, AbstractReceivedIncomingFriendInvitationEventArgs e)
            {
                this.OnReceivedIncomingFriendInvitation(s, e);
            };
            realUser.OnReceivedOutgoingFriendInvitation += delegate (object s, AbstractReceivedOutgoingFriendInvitationEventArgs e)
            {
                this.OnReceivedOutgoingFriendInvitation(s, e);
            };
            realUser.OnUnfriended += delegate (object s, AbstractUnfriendedEventArgs e)
            {
                this.OnUnfriended(s, e);
            };
            realUser.OnUntrusted += delegate (object s, AbstractUntrustedEventArgs e)
            {
                this.OnUntrusted(s, e);
            };
            realUser.OnAvatarChanged += delegate (object s, AbstractAvatarChangedEventArgs e)
            {
                this.OnAvatarChanged(s, e);
            };
            realUser.OnOfficialAccountFollowed += delegate (object s, AbstractOfficialAccountFollowedEventArgs e)
            {
                this.OnOfficialAccountFollowed(s, e);
            };
            realUser.OnOfficialAccountUnfollowed += delegate (object s, AbstractOfficialAccountUnfollowedEventArgs e)
            {
                this.OnOfficialAccountUnfollowed(s, e);
            };
            realUser.OnAlertsAdded += delegate (object s, AbstractAlertsAddedEventArgs e)
            {
                this.OnAlertsAdded(s, e);
            };
            realUser.OnAlertsCleared += delegate (object s, AbstractAlertsClearedEventArgs e)
            {
                this.OnAlertsCleared(s, e);
            };
            realUser.OnLegalMarketingUpdateRequired += delegate (object s, AbstractLegalMarketingUpdateRequiredEventArgs e)
            {
                this.OnLegalMarketingUpdateRequired(s, e);
            };
        }

        public void LoadFakeFriendData()
        {
            List<BaseFakeFirstFriendDocument> allDocuments = Singleton<MixDocumentCollections>.Instance.fakeFirstFriendDocumentCollectionApi.GetAllDocuments();
            foreach (BaseFakeFirstFriendDocument item in allDocuments)
            {
                AddFakeMessage(mixbotThread, item);
            }
            mixbotThread.SortMessagesByTime();
        }

        private void AddFakeMessage(FakeThread thread, BaseFakeFirstFriendDocument message)
        {
            if (message.IsType(55))
            {
                IRemoteChatMember remoteChatMember = thread.RemoteMembers.FirstOrDefault((IRemoteChatMember member) => member is FakeFriendData);
                LocalGagMessage message2 = new LocalGagMessage(message.ContentId_or_GameName_or_Text_or_MessageId, (!message.isFromFriend) ? remoteChatMember.Id : realUser.Id, (!message.isFromFriend) ? realUser.Id : remoteChatMember.Id, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId);
                thread.AddExistingMessage(message2);
            }
            else if (message.IsType(57))
            {
                string senderId = ((!message.isFromFriend) ? realUser.Id : thread.RemoteMembers.FirstOrDefault((IRemoteChatMember member) => member is FakeFriendData).Id);
                LocalTextMessage message3 = new LocalTextMessage(senderId, message.ContentId_or_GameName_or_Text_or_MessageId, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId);
                thread.AddExistingMessage(message3);
            }
            else if (message.IsType(58))
            {
                string senderId2 = ((!message.isFromFriend) ? realUser.Id : thread.RemoteMembers.FirstOrDefault((IRemoteChatMember member) => member is FakeFriendData).Id);
                LocalStickerMessage message4 = new LocalStickerMessage(message.ContentId_or_GameName_or_Text_or_MessageId, senderId2, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId);
                thread.AddExistingMessage(message4);
            }
            else if (message.IsType(59))
            {
                Dictionary<string, object> aPayload = JsonMapper.ToObject<Dictionary<string, object>>(message.GameData_or_FilePath_or_VideoPath);
                string senderId3 = ((!message.isFromFriend) ? realUser.Id : thread.RemoteMembers.FirstOrDefault((IRemoteChatMember member) => member is FakeFriendData).Id);
                LocalGameStateMessage message5 = new LocalGameStateMessage(message.ContentId_or_GameName_or_Text_or_MessageId, aPayload, senderId3, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId);
                thread.AddExistingMessage(message5);
            }
            else if (message.IsType(63))
            {
                string senderId4 = ((!message.isFromFriend) ? realUser.Id : thread.RemoteMembers.FirstOrDefault((IRemoteChatMember member) => member is FakeFriendData).Id);
                LocalPhotoMessage localPhotoMessage = null;
                localPhotoMessage = (string.IsNullOrEmpty(message.ContentId_or_GameName_or_Text_or_MessageId) ? new LocalPhotoMessage(message.GameData_or_FilePath_or_VideoPath, message.Encoding_or_Format, message.Width_or_Bitrate, message.Height_or_Duration, senderId4, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId) : new LocalPhotoMessage(message.ContentId_or_GameName_or_Text_or_MessageId, senderId4, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId));
                thread.AddExistingMessage(localPhotoMessage);
            }
            else if (message.IsType(64))
            {
                string senderId5 = ((!message.isFromFriend) ? realUser.Id : thread.RemoteMembers.FirstOrDefault((IRemoteChatMember member) => member is FakeFriendData).Id);
                LocalVideoMessage localVideoMessage = null;
                localVideoMessage = (string.IsNullOrEmpty(message.ContentId_or_GameName_or_Text_or_MessageId) ? new LocalVideoMessage(message.GameData_or_FilePath_or_VideoPath, message.Encoding_or_Format, message.Width_or_Bitrate, message.Height_or_Duration, message.VideoWidth, message.VideoHeight, message.ThumbnailPath, message.ThumbnailEncoding, message.ThumbnailWidth, message.ThumbnailHeight, senderId5, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId) : new LocalVideoMessage(message.ContentId_or_GameName_or_Text_or_MessageId, senderId5, new DateTime(message.timeSent, DateTimeKind.Utc), message.isSent, message.isRead, message.uniqueId));
                thread.AddExistingMessage(localVideoMessage);
            }
            if (int.Parse(message.uniqueId) >= fakeObjectIds)
            {
                fakeObjectIds = int.Parse(message.uniqueId) + 1;
            }
        }

        public void GetRecommendedFriends(Action<IGetRecommendedFriendsResult> aIGetRecommendedFriendsResult)
        {
            realUser.GetRecommendedFriends(aIGetRecommendedFriendsResult);
        }

        public void FindUser(string displayName, Action<IFindUserResult> callback)
        {
            realUser.FindUser(displayName, callback);
        }

        public IOutgoingFriendInvitation SendFriendInvitation(IUnidentifiedUser user, bool requestTrust, Action<ISendFriendInvitationResult> callback)
        {
            return realUser.SendFriendInvitation(user, requestTrust, callback);
        }

        public IOutgoingFriendInvitation SendFriendInvitation(IRemoteChatMember user, bool requestTrust, Action<ISendFriendInvitationResult> callback)
        {
            return realUser.SendFriendInvitation(user, requestTrust, callback);
        }

        public IOutgoingFriendInvitation SendFriendInvitation(IFriend user, bool requestTrust, Action<ISendFriendInvitationResult> callback)
        {
            return realUser.SendFriendInvitation(user, requestTrust, callback);
        }

        public void AcceptFriendInvitation(IIncomingFriendInvitation invitation, bool acceptTrust, Action<IAcceptFriendInvitationResult> callback)
        {
            PendingFriendInvitations.Add(invitation);
            realUser.AcceptFriendInvitation(invitation, acceptTrust, delegate (IAcceptFriendInvitationResult r)
            {
                PendingFriendInvitations.Remove(invitation);
                callback(r);
            });
        }

        public void RejectFriendInvitation(IIncomingFriendInvitation invitation, Action<IRejectFriendInvitationResult> callback)
        {
            PendingFriendInvitations.Add(invitation);
            realUser.RejectFriendInvitation(invitation, delegate (IRejectFriendInvitationResult r)
            {
                PendingFriendInvitations.Remove(invitation);
                callback(r);
            });
        }

        public void Unfriend(IFriend friend, Action<IUnfriendResult> callback)
        {
            realUser.Unfriend(friend, callback);
        }

        public void Untrust(IFriend trustedUser, Action<IUntrustResult> callback)
        {
            realUser.Untrust(trustedUser, callback);
        }

        public IUserNickname SetNickname(IFriend user, string nickname, Action<ISetUserNicknameResult> callback)
        {
            return realUser.SetNickname(user, nickname, callback);
        }

        public void RemoveNickname(IFriend user, Action<IRemoveUserNicknameResult> callback)
        {
            realUser.RemoveNickname(user, callback);
        }

        public void SetAvatar(IAvatar avatar, Action<ISetAvatarResult> callback)
        {
            realUser.SetAvatar(avatar, callback);
        }

        public void RefreshProfile(Action<IRefreshProfileResult> callback)
        {
            realUser.RefreshProfile(callback);
        }

        public void SendParentalApprovalEmail(Action<ISendParentalApprovalEmailResult> callback)
        {
            realUser.SendParentalApprovalEmail(delegate (ISendParentalApprovalEmailResult result)
            {
                if (result.Success)
                {
                    Singleton<MixDocumentCollections>.Instance.keyValDocumentCollectionApi.SaveUserValue("TimeParentEmailSent", DateTime.UtcNow.Ticks.ToString());
                }
                callback(result);
            });
        }

        public void SendVerificationEmail(Action<ISendVerificationEmailResult> callback)
        {
            realUser.SendVerificationEmail(callback);
        }

        public void UpdateDisplayName(string displayName, Action<IUpdateDisplayNameResult> callback)
        {
            realUser.UpdateDisplayName(displayName, callback);
        }

        public void ValidateDisplayName(string displayName, Action<IValidateDisplayNameResult> callback)
        {
            realUser.ValidateDisplayName(displayName, callback);
        }

        public void GetRecentChatThreadMessages(Action<IGetRecentChatThreadMessagesResult> callback)
        {
            realUser.GetRecentChatThreadMessages(callback);
        }

        public void CreateOneOnOneChatThread(IFriend member, Action<ICreateOneOnOneChatThreadResult> callback)
        {
            realUser.CreateOneOnOneChatThread(member, callback);
        }

        public void CreateGroupChatThread(IEnumerable<IFriend> members, Action<ICreateGroupChatThreadResult> callback)
        {
            realUser.CreateGroupChatThread(members, callback);
        }

        public void UpdateProfile(string firstName, string lastName, string displayName, string email, string parentEmail, DateTime? dateOfBirth, IEnumerable<KeyValuePair<IMarketingItem, bool>> newsletterSubscriptions, IEnumerable<ILegalDocument> acceptedLegalDocuments, Action<IUpdateProfileResult> callback)
        {
            realUser.UpdateProfile(firstName, lastName, displayName, email, parentEmail, dateOfBirth, newsletterSubscriptions, acceptedLegalDocuments, callback);
        }

        public void AddChatThreadMembers(IGroupChatThread chatThread, IEnumerable<IFriend> members, Action<IAddChatThreadMemberResult> callback)
        {
            if (chatThread is LocalThread)
            {
                GroupLocalThread groupLocalThread = (GroupLocalThread)chatThread;
                chatThread = (IGroupChatThread)groupLocalThread.GetRealThread();
                if (chatThread == null)
                {
                    groupLocalThread.AddLocalThreadMembers(members, callback);
                    return;
                }
            }
            realUser.AddChatThreadMembers(chatThread, members, callback);
        }

        public void RemoveChatThreadMember(IGroupChatThread chatThread, IRemoteChatMember member, Action<IRemoveChatThreadMemberResult> callback)
        {
            realUser.RemoveChatThreadMember(chatThread, member, callback);
        }

        public void RemoveChatThreadMember(IGroupChatThread chatThread, IFriend member, Action<IRemoveChatThreadMemberResult> callback)
        {
            realUser.RemoveChatThreadMember(chatThread, member, callback);
        }

        public void RemoveChatThreadMember(IGroupChatThread chatThread, ILocalUser member, Action<IRemoveChatThreadMemberResult> callback)
        {
            ILocalUser localUser = realUser;
            ILocalUser member2;
            if (member == this)
            {
                ILocalUser localUser2 = realUser;
                member2 = localUser2;
            }
            else
            {
                member2 = member;
            }
            localUser.RemoveChatThreadMember(chatThread, member2, callback);
        }

        public void ModerateText(string text, bool isTrusted, Action<ITextModerationResult> callback)
        {
            try
            {
                realUser.ModerateText(text, isTrusted, callback);
            }
            catch (NotImplementedException)
            {
                callback(new FakeTextModerationResult(true, false, text));
            }
        }

        public void EnableAllPushNotifications(string token, PushNotificationService service, string provisionId, Action<IEnableAllPushNotificationsResult> callback)
        {
            realUser.EnableAllPushNotifications(token, service, provisionId, callback);
        }

        public void EnableInvisiblePushNotifications(string token, PushNotificationService service, string provisionId, Action<IEnableInvisiblePushNotificationsResult> callback)
        {
            realUser.EnableInvisiblePushNotifications(token, service, provisionId, callback);
        }

        public void DisableAllPushNotifications(Action<IDisableAllPushNotificationsResult> callback)
        {
            realUser.DisableAllPushNotifications(callback);
        }

        public void DisableVisiblePushNotifications(Action<IDisableVisiblePushNotificationsResult> callback)
        {
            realUser.DisableVisiblePushNotifications(callback);
        }

        public IPushNotification ReceivePushNotification(IDictionary notification)
        {
            return realUser.ReceivePushNotification(notification);
        }

        public void TemporarilyBanAccount(Action<ITemporarilyBanAccountResult> callback)
        {
            realUser.TemporarilyBanAccount(callback);
        }

        public void SendMassPushNotification(Action<ISendMassPushNotificationResult> callback)
        {
            realUser.SendMassPushNotification(callback);
        }

        public void SendAlert(int level, AlertType type, Action<ISendAlertResult> callback)
        {
            realUser.SendAlert(level, type, callback);
        }

        public void ClearAlerts(IEnumerable<IAlert> alerts, Action<IClearAlertsResult> callback)
        {
            realUser.ClearAlerts(alerts, callback);
        }

        public void GetAdultVerificationRequirements(Action<IGetAdultVerificationRequirementsResult> callback)
        {
            realUser.GetAdultVerificationRequirements(callback);
        }

        public void ValidateDisplayNames(IEnumerable<string> displayNames, Action<IValidateDisplayNamesResult> callback)
        {
            realUser.ValidateDisplayNames(displayNames, callback);
        }

        public void RequestTrustPermission(Action<IPermissionResult> callback)
        {
            realUser.RequestTrustPermission(callback);
        }

        public void ApproveChildTrustPermission(ISession child, ActivityApprovalStatus status, Action<IPermissionResult> callback)
        {
            realUser.ApproveChildTrustPermission(child, status, callback);
        }

        public void ApproveChildTrustPermission(ILinkedUser child, ActivityApprovalStatus status, Action<IPermissionResult> callback)
        {
            realUser.ApproveChildTrustPermission(child, status, callback);
        }

        public void GetChildTrustPermission(ISession child, Action<IPermissionResult> callback)
        {
            realUser.GetChildTrustPermission(child, callback);
        }

        public void GetChildTrustPermission(ILinkedUser child, Action<IPermissionResult> callback)
        {
            realUser.GetChildTrustPermission(child, callback);
        }

        public void GetAdultVerificationStatus(Action<IGetAdultVerificationStatusResult> callback)
        {
            realUser.GetAdultVerificationStatus(callback);
        }

        public void GetVerifyAdultForm(Action<IGetVerifyAdultFormResult> callback)
        {
            realUser.GetVerifyAdultForm(callback);
        }

        public void VerifyAdult(IVerifyAdultFormUnitedStates form, Action<IVerifyAdultResult> callback)
        {
            realUser.VerifyAdult(form, callback);
        }

        public void AnswerVerifyAdultQuiz(IVerifyAdultQuizAnswers answers, Action<IVerifyAdultResult> callback)
        {
            realUser.AnswerVerifyAdultQuiz(answers, callback);
        }

        public void GetClaimableChildren(Action<IGetLinkedUsersResult> callback)
        {
            realUser.GetClaimableChildren(callback);
        }

        public void LinkChildAccount(ISession child, Action<ILinkChildResult> callback)
        {
            realUser.LinkChildAccount(child, callback);
        }

        public void GetLinkedChildren(Action<IGetLinkedUsersResult> callback)
        {
            realUser.GetLinkedChildren(callback);
        }

        public void GetLinkedGuardians(Action<IGetLinkedUsersResult> callback)
        {
            realUser.GetLinkedGuardians(callback);
        }

        public void LinkClaimableChildAccounts(IEnumerable<ILinkedUser> children, Action<ILinkChildResult> callback)
        {
            realUser.LinkClaimableChildAccounts(children, callback);
        }

        public void RequestTrustPermissionForChild(ILinkedUser child, Action<IPermissionResult> callback)
        {
            realUser.RequestTrustPermissionForChild(child, callback);
        }

        public void GetAllOfficialAccounts(Action<IGetAllOfficialAccountsResult> callback)
        {
            realUser.GetAllOfficialAccounts(callback);
        }

        public void FollowOfficialAccount(IOfficialAccount account, Action<IFollowOfficialAccountResult> callback)
        {
            realUser.FollowOfficialAccount(account, callback);
        }

        public void UnfollowOfficialAccount(IOfficialAccount account, Action<IUnfollowOfficialAccountResult> callback)
        {
            realUser.UnfollowOfficialAccount(account, callback);
        }

        public void SetLanguagePreference(string languageCode, Action<ISetLangaugePreferenceResult> callback)
        {
            realUser.SetLanguagePreference(languageCode, callback);
        }
    }
}