using System;
using System.Collections.Generic;
using Disney.Mix.SDK.Internal.MixDomain;

namespace Disney.Mix.SDK.Internal
{
	public class MixWebCallFactory : IDisposable, IMixWebCallFactory
	{
		private const long DefaultLatencyWwwCallTimeout = 10000L;

		private const long DefaultMaxWwwCallTimeout = 30000L;

		private readonly AbstractLogger logger;

		private readonly string host;

		private readonly IWwwCallFactory wwwCallFactory;

		private readonly string swid;

		private IWebCallEncryptor webCallEncryptor;

		private string guestControllerAccessToken;

		private readonly string mixClientToken;

		private bool isSessionRefreshing;

		private readonly IMixWebCallQueue webCallQueue;

		private readonly ISessionRefresher sessionRefresher;

		private readonly IEpochTime epochTime;

		private readonly IDatabase database;

		private readonly IList<IDisposable> webCalls;

		public string GuestControllerAccessToken
		{
			get
			{
				return guestControllerAccessToken;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentException("Guest Controller access token can't be null or empty");
				}
				guestControllerAccessToken = value;
			}
		}

		public event EventHandler<AbstractAuthenticationLostEventArgs> OnAuthenticationLost = delegate
		{
		};

		public MixWebCallFactory(AbstractLogger logger, string host, IWwwCallFactory wwwCallFactory, IWebCallEncryptor webCallEncryptor, string swid, string guestControllerAccessToken, string mixClientToken, IMixWebCallQueue webCallQueue, ISessionRefresher sessionRefresher, IEpochTime epochTime, IDatabase database)
		{
			this.logger = logger;
			this.host = host;
			this.wwwCallFactory = wwwCallFactory;
			this.webCallEncryptor = webCallEncryptor;
			this.swid = swid;
			this.guestControllerAccessToken = guestControllerAccessToken;
			this.mixClientToken = mixClientToken;
			this.webCallQueue = webCallQueue;
			this.sessionRefresher = sessionRefresher;
			this.epochTime = epochTime;
			this.database = database;
			webCalls = new List<IDisposable>();
		}

		public IWebCall<GetUsersByDisplayNameRequest, GetUsersResponse> UsersByDisplayNamePost(GetUsersByDisplayNameRequest request)
		{
			return CreateWebCall<GetUsersByDisplayNameRequest, GetUsersResponse>(HttpMethod.POST, new Uri(host + "/users/byDisplayName"), request);
		}

		public IWebCall<GetUsersByUserIdRequest, GetUsersResponse> UsersByUserIdPost(GetUsersByUserIdRequest request)
		{
			return CreateWebCall<GetUsersByUserIdRequest, GetUsersResponse>(HttpMethod.POST, new Uri(host + "/users/byUserId"), request);
		}

		public IWebCall<ClearAlertsRequest, ClearAlertsResponse> AlertsClearPut(ClearAlertsRequest request)
		{
			return CreateWebCall<ClearAlertsRequest, ClearAlertsResponse>(HttpMethod.PUT, new Uri(host + "/alert/clear"), request);
		}

		public IWebCall<SetAvatarRequest, SetAvatarResponse> AvatarPut(SetAvatarRequest request)
		{
			return CreateWebCall<SetAvatarRequest, SetAvatarResponse>(HttpMethod.PUT, new Uri(host + "/avatar"), request);
		}

		public IWebCall<AddChatThreadRequest, AddChatThreadResponse> ChatThreadPut(AddChatThreadRequest request)
		{
			return CreateWebCall<AddChatThreadRequest, AddChatThreadResponse>(HttpMethod.PUT, new Uri(host + "/chatThread"), request);
		}

		public IWebCall<GetChatThreadMessagesRequest, GetChatThreadMessagesResponse> ChatThreadPost(GetChatThreadMessagesRequest request)
		{
			return CreateWebCall<GetChatThreadMessagesRequest, GetChatThreadMessagesResponse>(HttpMethod.POST, new Uri(host + "/chatThread"), request);
		}

		public IWebCall<AddChatThreadGagMessageRequest, AddChatThreadGagMessageResponse> ChatThreadGagMessagePut(AddChatThreadGagMessageRequest request)
		{
			return CreateWebCall<AddChatThreadGagMessageRequest, AddChatThreadGagMessageResponse>(HttpMethod.PUT, new Uri(host + "/chatThread/gagMessage"), request);
		}

		public IWebCall<AddChatThreadGameStateMessageRequest, AddChatThreadGameStateMessageResponse> ChatThreadGameStateMessagePut(AddChatThreadGameStateMessageRequest request)
		{
			return CreateWebCall<AddChatThreadGameStateMessageRequest, AddChatThreadGameStateMessageResponse>(HttpMethod.PUT, new Uri(host + "/chatThread/gameStateMessage"), request);
		}

		public IWebCall<UpdateChatThreadGameStateMessageRequest, UpdateChatThreadGameStateMessageResponse> ChatThreadGameStateMessagePost(UpdateChatThreadGameStateMessageRequest request)
		{
			return CreateWebCall<UpdateChatThreadGameStateMessageRequest, UpdateChatThreadGameStateMessageResponse>(HttpMethod.POST, new Uri(host + "/chatThread/gameStateMessage"), request);
		}

		public IWebCall<GetChatThreadMessagesBySequenceRangesRequest, GetChatThreadMessagesResponse> ChatThreadMessagesBySequenceRangePost(GetChatThreadMessagesBySequenceRangesRequest request)
		{
			return CreateWebCall<GetChatThreadMessagesBySequenceRangesRequest, GetChatThreadMessagesResponse>(HttpMethod.POST, new Uri(host + "/chatThread/messages/bySequenceRange"), request);
		}

		public IWebCall<GetChatThreadsRecentMessagesRequest, GetChatThreadMessagesResponse> ChatThreadMessagesRecentPost(GetChatThreadsRecentMessagesRequest request)
		{
			return CreateWebCall<GetChatThreadsRecentMessagesRequest, GetChatThreadMessagesResponse>(HttpMethod.POST, new Uri(host + "/chatThread/messages/recent"), request);
		}

		public IWebCall<GetChatThreadSequenceNumberRequest, GetChatThreadSequenceNumberResponse> ChatThreadMessagesSequenceNumberPost(GetChatThreadSequenceNumberRequest request)
		{
			return CreateWebCall<GetChatThreadSequenceNumberRequest, GetChatThreadSequenceNumberResponse>(HttpMethod.POST, new Uri(host + "/chatThread/messages/sequenceNumber"), request);
		}

		public IWebCall<AddChatThreadNicknameRequest, AddChatThreadNicknameResponse> ChatThreadNicknamePut(AddChatThreadNicknameRequest request)
		{
			return CreateWebCall<AddChatThreadNicknameRequest, AddChatThreadNicknameResponse>(HttpMethod.PUT, new Uri(host + "/chatThread/nickname"), request);
		}

		public IWebCall<RemoveChatThreadNicknameRequest, RemoveChatThreadNicknameResponse> ChatThreadNicknameDeletePost(RemoveChatThreadNicknameRequest request)
		{
			return CreateWebCall<RemoveChatThreadNicknameRequest, RemoveChatThreadNicknameResponse>(HttpMethod.POST, new Uri(host + "/chatThread/nickname/delete"), request);
		}

		public IWebCall<AddChatThreadPhotoMessageRequest, AddChatThreadPhotoMessageResponse> ChatThreadPhotoMessagePut(AddChatThreadPhotoMessageRequest request)
		{
			return CreateWebCall<AddChatThreadPhotoMessageRequest, AddChatThreadPhotoMessageResponse>(HttpMethod.PUT, new Uri(host + "/chatThread/photoMessage"), request);
		}

		public IWebCall<GetRecentChatThreadMessagesRequest, GetRecentChatThreadMessagesResponse> ChatThreadRecentMessagesPost(GetRecentChatThreadMessagesRequest request)
		{
			return CreateWebCall<GetRecentChatThreadMessagesRequest, GetRecentChatThreadMessagesResponse>(HttpMethod.POST, new Uri(host + "/chatThread/recentMessages"), request);
		}

		public IWebCall<GetSpecificChatThreadMessagesRequest, GetRecentChatThreadMessagesResponse> ChatThreadSpecificMessagesPost(GetSpecificChatThreadMessagesRequest request)
		{
			return CreateWebCall<GetSpecificChatThreadMessagesRequest, GetRecentChatThreadMessagesResponse>(HttpMethod.POST, new Uri(host + "/chatThread/specificMessages"), request);
		}

		public IWebCall<AddChatThreadStickerMessageRequest, AddChatThreadStickerMessageResponse> ChatThreadStickerMessagePut(AddChatThreadStickerMessageRequest request)
		{
			return CreateWebCall<AddChatThreadStickerMessageRequest, AddChatThreadStickerMessageResponse>(HttpMethod.PUT, new Uri(host + "/chatThread/stickerMessage"), request);
		}

		public IWebCall<AddChatThreadTextMessageRequest, AddChatThreadTextMessageResponse> ChatThreadTextMessagePut(AddChatThreadTextMessageRequest request)
		{
			return CreateWebCall<AddChatThreadTextMessageRequest, AddChatThreadTextMessageResponse>(HttpMethod.PUT, new Uri(host + "/chatThread/textMessage"), request);
		}

		public IWebCall<AddChatThreadVideoMessageRequest, AddChatThreadVideoMessageResponse> ChatThreadVideoMessagePut(AddChatThreadVideoMessageRequest request)
		{
			return CreateWebCall<AddChatThreadVideoMessageRequest, AddChatThreadVideoMessageResponse>(HttpMethod.PUT, new Uri(host + "/chatThread/videoMessage"), request);
		}

		public IWebCall<ClearMemberChatHistoryRequest, ClearMemberChatHistoryResponse> ChatThreadMembershipHistoryDeletePost(ClearMemberChatHistoryRequest request)
		{
			return CreateWebCall<ClearMemberChatHistoryRequest, ClearMemberChatHistoryResponse>(HttpMethod.POST, new Uri(host + "/chatThreadMembership/history/delete"), request);
		}

		public IWebCall<ClearUnreadMessageCountRequest, ClearUnreadMessageCountResponse> ChatThreadMembershipUnreadMessageCountDeletePost(ClearUnreadMessageCountRequest request)
		{
			return CreateWebCall<ClearUnreadMessageCountRequest, ClearUnreadMessageCountResponse>(HttpMethod.POST, new Uri(host + "/chatThreadMembership/unreadMessageCount/delete"), request);
		}

		public IWebCall<AddChatThreadMembershipRequest, AddChatThreadMembershipResponse> ChatThreadMembershipPut(AddChatThreadMembershipRequest request)
		{
			return CreateWebCall<AddChatThreadMembershipRequest, AddChatThreadMembershipResponse>(HttpMethod.PUT, new Uri(host + "/chatThreadMembership"), request);
		}

		public IWebCall<RemoveChatThreadMembershipRequest, RemoveChatThreadMembershipResponse> ChatThreadMembershipDeletePost(RemoveChatThreadMembershipRequest request)
		{
			return CreateWebCall<RemoveChatThreadMembershipRequest, RemoveChatThreadMembershipResponse>(HttpMethod.POST, new Uri(host + "/chatThreadMembership/delete"), request);
		}

		public IWebCall<SetDisplayNameRequest, SetDisplayNameResponse> DisplaynamePut(SetDisplayNameRequest request)
		{
			return CreateWebCall<SetDisplayNameRequest, SetDisplayNameResponse>(HttpMethod.PUT, new Uri(host + "/displayname"), request);
		}

		public IWebCall<ValidateDisplayNamesRequest, ValidateDisplayNamesResponse> DisplaynameValidatePost(ValidateDisplayNamesRequest request)
		{
			return CreateWebCall<ValidateDisplayNamesRequest, ValidateDisplayNamesResponse>(HttpMethod.POST, new Uri(host + "/displayname/validate"), request);
		}

		public IWebCall<AddFriendshipRequest, AddFriendshipResponse> FriendshipPut(AddFriendshipRequest request)
		{
			return CreateWebCall<AddFriendshipRequest, AddFriendshipResponse>(HttpMethod.PUT, new Uri(host + "/friendship"), request);
		}

		public IWebCall<RemoveFriendshipRequest, RemoveFriendshipResponse> FriendshipDeletePost(RemoveFriendshipRequest request)
		{
			return CreateWebCall<RemoveFriendshipRequest, RemoveFriendshipResponse>(HttpMethod.POST, new Uri(host + "/friendship/delete"), request);
		}

		public IWebCall<BaseUserRequest, GetFriendshipRecommendationResponse> FriendshipRecommendPost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, GetFriendshipRecommendationResponse>(HttpMethod.POST, new Uri(host + "/friendship/recommend"), request);
		}

		public IWebCall<AddFriendshipInvitationRequest, AddFriendshipInvitationResponse> FriendshipInvitationPut(AddFriendshipInvitationRequest request)
		{
			return CreateWebCall<AddFriendshipInvitationRequest, AddFriendshipInvitationResponse>(HttpMethod.PUT, new Uri(host + "/friendship/invitation"), request);
		}

		public IWebCall<RemoveFriendshipInvitationRequest, RemoveFriendshipInvitationResponse> FriendshipInvitationDeletePost(RemoveFriendshipInvitationRequest request)
		{
			return CreateWebCall<RemoveFriendshipInvitationRequest, RemoveFriendshipInvitationResponse>(HttpMethod.POST, new Uri(host + "/friendship/invitation/delete"), request);
		}

		public IWebCall<BaseUserRequest, GetGeolocationResponse> GeolocationPost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, GetGeolocationResponse>(HttpMethod.POST, new Uri(host + "/geolocation"), request);
		}

		public IWebCall<BaseUserRequest, BaseResponse> IntegrationTestSupportModerationTempBanPut(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, BaseResponse>(HttpMethod.PUT, new Uri(host + "/integrationTestSupport/moderation/tempBan"), request);
		}

		public IWebCall<OfficialAccountPublishPhotoTestRequest, BaseResponse> IntegrationTestSupportOfficialAccountPublishPhotoPost(OfficialAccountPublishPhotoTestRequest request)
		{
			return CreateWebCall<OfficialAccountPublishPhotoTestRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/integrationTestSupport/officialAccount/publishPhoto"), request);
		}

		public IWebCall<OfficialAccountPublishTextTestRequest, BaseResponse> IntegrationTestSupportOfficialAccountPublishTextPost(OfficialAccountPublishTextTestRequest request)
		{
			return CreateWebCall<OfficialAccountPublishTextTestRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/integrationTestSupport/officialAccount/publishText"), request);
		}

		public IWebCall<OfficialAccountPublishVideoTestRequest, BaseResponse> IntegrationTestSupportOfficialAccountPublishVideoPost(OfficialAccountPublishVideoTestRequest request)
		{
			return CreateWebCall<OfficialAccountPublishVideoTestRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/integrationTestSupport/officialAccount/publishVideo"), request);
		}

		public IWebCall<BaseUserRequest, BaseResponse> IntegrationTestSupportPushNotificationMassSendPost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/integrationTestSupport/pushNotification/massSend"), request);
		}

		public IWebCall<TriggerAlertRequest, BaseResponse> IntegrationTestSupportUserAlertPost(TriggerAlertRequest request)
		{
			return CreateWebCall<TriggerAlertRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/integrationTestSupport/user/alert"), request);
		}

		public IWebCall<BaseUserRequest, BaseResponse> IntegrationTestSupportUserSessionExpirePost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/integrationTestSupport/user/session/expire"), request);
		}

		public IWebCall<SetLanguageRequest, BaseResponse> LanguagePreferencePost(SetLanguageRequest request)
		{
			return CreateWebCall<SetLanguageRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/languagePreference"), request);
		}

		public IWebCall<ReportPlayerRequest, BaseResponse> ModerationReportPlayerPut(ReportPlayerRequest request)
		{
			return CreateWebCall<ReportPlayerRequest, BaseResponse>(HttpMethod.PUT, new Uri(host + "/moderation/reportPlayer"), request);
		}

		public IWebCall<ModerateTextRequest, ModerateTextResponse> ModerationTextPut(ModerateTextRequest request)
		{
			return CreateWebCall<ModerateTextRequest, ModerateTextResponse>(HttpMethod.PUT, new Uri(host + "/moderation/text"), request);
		}

		public IWebCall<AddNicknameRequest, AddNicknameResponse> NicknamePut(AddNicknameRequest request)
		{
			return CreateWebCall<AddNicknameRequest, AddNicknameResponse>(HttpMethod.PUT, new Uri(host + "/nickname"), request);
		}

		public IWebCall<RemoveNicknameRequest, RemoveNicknameResponse> NicknameDeletePost(RemoveNicknameRequest request)
		{
			return CreateWebCall<RemoveNicknameRequest, RemoveNicknameResponse>(HttpMethod.POST, new Uri(host + "/nickname/delete"), request);
		}

		public IWebCall<GetNotificationsRequest, GetNotificationsResponse> NotificationsPost(GetNotificationsRequest request)
		{
			return CreateWebCall<GetNotificationsRequest, GetNotificationsResponse>(HttpMethod.POST, new Uri(host + "/notifications"), request);
		}

		public IWebCall<GetNotificationsSinceSequenceRequest, GetNotificationsResponse> NotificationsSinceSequencePost(GetNotificationsSinceSequenceRequest request)
		{
			return CreateWebCall<GetNotificationsSinceSequenceRequest, GetNotificationsResponse>(HttpMethod.POST, new Uri(host + "/notifications/sinceSequence"), request);
		}

		public IWebCall<AddOfficialAccountFollowshipRequest, AddOfficialAccountFollowshipResponse> OfficialAccountAddPut(AddOfficialAccountFollowshipRequest request)
		{
			return CreateWebCall<AddOfficialAccountFollowshipRequest, AddOfficialAccountFollowshipResponse>(HttpMethod.PUT, new Uri(host + "/officialAccount/add"), request);
		}

		public IWebCall<BaseUserRequest, OfficialAccountsResponse> OfficialAccountAllPost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, OfficialAccountsResponse>(HttpMethod.POST, new Uri(host + "/officialAccount/all"), request);
		}

		public IWebCall<RemoveOfficialAccountFollowshipRequest, RemoveOfficialAccountFollowshipResponse> OfficialAccountRemovePost(RemoveOfficialAccountFollowshipRequest request)
		{
			return CreateWebCall<RemoveOfficialAccountFollowshipRequest, RemoveOfficialAccountFollowshipResponse>(HttpMethod.POST, new Uri(host + "/officialAccount/remove"), request);
		}

		public IWebCall<PilCheckRequest, PilCheckResponse> PilCheckPost(PilCheckRequest request)
		{
			return CreateWebCall<PilCheckRequest, PilCheckResponse>(HttpMethod.POST, new Uri(host + "/pil/check"), request);
		}

		public IWebCall<SetPresenceRequest, BaseResponse> PresencePut(SetPresenceRequest request)
		{
			return CreateWebCall<SetPresenceRequest, BaseResponse>(HttpMethod.PUT, new Uri(host + "/presence"), request);
		}

		public IWebCall<BaseUserRequest, PostPresenceResponse> PresencePost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, PostPresenceResponse>(HttpMethod.POST, new Uri(host + "/presence"), request);
		}

		public IWebCall<TogglePushNotificationRequest, BaseResponse> PushNotificationsSettingPost(TogglePushNotificationRequest request)
		{
			return CreateWebCall<TogglePushNotificationRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/pushNotificationsSetting"), request);
		}

		public IWebCall<BaseUserRequest, BaseResponse> PushNotificationsSettingDeletePost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/pushNotificationsSetting/delete"), request);
		}

		public IWebCall<GetRegistrationTextRequest, GetRegistrationTextResponse> RegistrationTextPost(GetRegistrationTextRequest request)
		{
			return CreateWebCall<GetRegistrationTextRequest, GetRegistrationTextResponse>(HttpMethod.POST, new Uri(host + "/registration/text"), request);
		}

		public IWebCall<DisplayNameSearchRequest, DisplayNameSearchResponse> SearchDisplaynamePost(DisplayNameSearchRequest request)
		{
			return CreateWebCall<DisplayNameSearchRequest, DisplayNameSearchResponse>(HttpMethod.POST, new Uri(host + "/search/displayname"), request);
		}

		public IWebCall<StartUserSessionRequest, StartUserSessionResponse> SessionUserPut(StartUserSessionRequest request)
		{
			return CreateWebCall<StartUserSessionRequest, StartUserSessionResponse>(HttpMethod.PUT, new Uri(host + "/session/user"), request);
		}

		public IWebCall<BaseUserRequest, BaseResponse> SessionUserDeletePost(BaseUserRequest request)
		{
			return CreateWebCall<BaseUserRequest, BaseResponse>(HttpMethod.POST, new Uri(host + "/session/user/delete"), request);
		}

		public IWebCall<GetStateRequest, GetStateResponse> StatePost(GetStateRequest request)
		{
			return CreateWebCall<GetStateRequest, GetStateResponse>(HttpMethod.POST, new Uri(host + "/state"), request, 30000L, 40000L);
		}

		public IWebCall<RemoveFriendshipTrustRequest, RemoveFriendshipTrustResponse> FriendshipTrustDeletePost(RemoveFriendshipTrustRequest request)
		{
			return CreateWebCall<RemoveFriendshipTrustRequest, RemoveFriendshipTrustResponse>(HttpMethod.POST, new Uri(host + "/friendship/trust/delete"), request);
		}

		public IWebCall<GetVideoUrlRequest, GetVideoUrlResponse> VideoUrlPost(GetVideoUrlRequest request)
		{
			return CreateWebCall<GetVideoUrlRequest, GetVideoUrlResponse>(HttpMethod.POST, new Uri(host + "/video/url"), request);
		}

		public void Dispose()
		{
			foreach (IDisposable webCall in webCalls)
			{
				webCall.Dispose();
			}
			webCalls.Clear();
		}

		private IWebCall<TRequest, TResponse> CreateWebCall<TRequest, TResponse>(HttpMethod method, Uri uri, TRequest request) where TRequest : BaseUserRequest where TResponse : BaseResponse, new()
		{
			return CreateWebCall<TRequest, TResponse>(method, uri, request, 10000L, 30000L);
		}

		private IWebCall<TRequest, TResponse> CreateWebCall<TRequest, TResponse>(HttpMethod method, Uri uri, TRequest request, long latencyWwwCallTimeout, long maxWwwCallTimeout) where TRequest : BaseUserRequest where TResponse : BaseResponse, new()
		{
			request.UserId = swid;
			request.Timestamp = ((!database.GetServerTimeOffsetMillis().HasValue) ? ((long?)null) : new long?(epochTime.Milliseconds));
			string body = JsonParser.ToJson(request);
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("Content-Type", webCallEncryptor.ContentType);
			dictionary.Add("X-Mix-OneIdToken", guestControllerAccessToken);
			dictionary.Add("X-Mix-ClientToken", mixClientToken);
			Dictionary<string, string> dictionary2 = dictionary;
			if (webCallEncryptor.SessionId != null)
			{
				dictionary2.Add("X-Mix-UserSessionId", webCallEncryptor.SessionId);
			}
			MixWebCall<TRequest, TResponse> webCall = new MixWebCall<TRequest, TResponse>(logger, uri, body, method, dictionary2, wwwCallFactory, webCallEncryptor, latencyWwwCallTimeout, maxWwwCallTimeout, database, swid);
			webCalls.Add(webCall);
			webCallQueue.AddWebCall(webCall);
			if (isSessionRefreshing)
			{
				webCall.RefreshStatus = WebCallRefreshStatus.WaitingForRefreshCallback;
			}
			webCall.OnResponse += delegate
			{
				webCalls.Remove(webCall);
				webCallQueue.RemoveWebCall(webCall);
			};
			webCall.OnError += delegate
			{
				webCalls.Remove(webCall);
				webCallQueue.RemoveWebCall(webCall);
			};
			webCall.OnUnauthorized += delegate(object sender, WebCallUnauthorizedEventArgs e)
			{
				HandleOnUnauthorized(webCall, e);
			};
			return webCall;
		}

		private void HandleOnUnauthorized<TRequest, TResponse>(IWebCall<TRequest, TResponse> webCall, WebCallUnauthorizedEventArgs e) where TRequest : BaseUserRequest where TResponse : BaseResponse, new()
		{
			if (webCallEncryptor.SessionId == null)
			{
				webCall.DispatchError("Unauthorized access to start a new session!");
			}
			else if (!isSessionRefreshing)
			{
				if (webCall.RefreshStatus == WebCallRefreshStatus.RefreshedWhileWaitingForCallback)
				{
					webCall.RefreshStatus = WebCallRefreshStatus.NotRefreshing;
					webCall.Execute();
					return;
				}
				webCall.RefreshStatus = WebCallRefreshStatus.WaitingForRefreshCallback;
				isSessionRefreshing = true;
				switch (e.Status)
				{
				case "UNAUTHORIZED_ONEID_TOKEN":
					sessionRefresher.RefreshGuestControllerToken(swid, delegate
					{
						isSessionRefreshing = false;
					}, delegate
					{
						isSessionRefreshing = false;
					});
					break;
				case "UNAUTHORIZED_MIX_SESSION":
					// The Mix session usually expires together with the Guest
					// Controller access token (both after a long idle), and
					// starting a new session with an expired token fails with a
					// 401 that can't be recovered, which logs the user out.
					// Refresh the token first, then start the session.
					sessionRefresher.RefreshAll(swid, delegate(IWebCallEncryptor encryptor)
					{
						isSessionRefreshing = false;
						webCallEncryptor = encryptor;
					}, delegate
					{
						isSessionRefreshing = false;
						this.OnAuthenticationLost(this, new AuthenticationUnavailableEventArgs());
					});
					break;
				case "UNAUTHORIZED_BANNED":
					webCall.DispatchError("Account is banned!");
					isSessionRefreshing = false;
					this.OnAuthenticationLost(this, new AccountBannedEventArgs());
					break;
				default:
					sessionRefresher.RefreshAll(swid, delegate(IWebCallEncryptor encryptor)
					{
						isSessionRefreshing = false;
						webCallEncryptor = encryptor;
					}, delegate
					{
						isSessionRefreshing = false;
						this.OnAuthenticationLost(this, new AuthenticationUnavailableEventArgs());
					});
					break;
				}
			}
			else
			{
				webCall.RefreshStatus = WebCallRefreshStatus.WaitingForRefreshCallback;
			}
		}
	}
}
