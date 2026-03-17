using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Disney.DMOAnalytics;
using Disney.Mix.SDK;
using Disney.Native;
using Mix.Assets;
using Mix.Connectivity;
using Mix.DeviceDb;
using Mix.Entitlements;
using Mix.Games;
using Mix.Localization;
using Mix.Session;
using Mix.Session.Extensions;
using Mix.Threading;
using Mix.Ui;
using UnityEngine;

namespace Mix.User
{
    public class LoginManager : MonoSingleton<LoginManager>
    {
        public class MixCoroutineManager : ICoroutineManager
        {
            public void Start(IEnumerator enumerator)
            {
                try
                {
                    if (!MonoSingleton<LoginManager>.Instance.IsNullOrDisposed())
                    {
                        MonoSingleton<LoginManager>.Instance.StartCoroutine(threadedMethod(enumerator));
                    }
                }
                catch (Exception exception)
                {
                    Log.Exception("MixSDK threw exception!", exception);
                    HandleSdkError();
                }
            }

            private IEnumerator threadedMethod(IEnumerator input)
            {
                while (true)
                {
                    try
                    {
                        if (!input.MoveNext())
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception e = ex;
                        Log.Exception("MixSDK threw exception!", e);
                        HandleSdkError();
                    }
                    yield return input.Current;
                }
            }

            private void HandleSdkError()
            {
                NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionNone());
                navigationRequest.AddData("fatalError", true);
                navigationRequest.PopLastRequest = true;
                if (MonoSingleton<NavigationManager>.Instance != null && MonoSingleton<LoginManager>.Instance != null)
                {
                    MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
                    MonoSingleton<LoginManager>.Instance.Logout();
                }
            }

            public void Stop(IEnumerator enumerator)
            {
                if (!MonoSingleton<LoginManager>.Instance.IsNullOrDisposed())
                {
                    MonoSingleton<LoginManager>.Instance.StopCoroutine(threadedMethod(enumerator));
                }
            }
        }

        private bool isNewUser = true;

        private SessionStarter sessionStarter;

        private string storageDir;

        public MixCoroutineManager CoroutineManager = new MixCoroutineManager();

        private SdkEvents eventGenerator = new SdkEvents();

        private SdkActions actionGenerator = new SdkActions();

        private IKeychain keychainData;

        public DateTime NextProfileUpdateTime = DateTime.MinValue;

        private TimeSpan profileUpdateInterval = new TimeSpan(0, 1, 0);

        private List<Action> profileUpdateCallbacks = new List<Action>();

        public IKeychain KeychainData
        {
            get
            {
                return keychainData;
            }
        }

        public string StorageDir
        {
            get
            {
                return storageDir;
            }
        }

        private ISession session
        {
            get
            {
                return MixSession.Session;
            }
        }

        public IRegistrationProfile LastProfileInfo
        {
            get
            {
                if (MixSession.IsValidSession)
                {
                    return session.LocalUser.RegistrationProfile;
                }
                return null;
            }
        }

        protected void Awake()
        {
            storageDir = Application.PersistentDataPath + "/cache/";
            keychainData = new KeychainData();
            sessionStarter = new SessionStarter();
        }

        private void Update()
        {
            if (!MixSession.ParentalConsentRequired || !(DateTime.Now > NextProfileUpdateTime))
            {
                return;
            }
            NextProfileUpdateTime = DateTime.Now + profileUpdateInterval;
            MixSession.Session.LocalUser.RefreshProfile(delegate (IRefreshProfileResult result)
            {
                foreach (Action profileUpdateCallback in profileUpdateCallbacks)
                {
                    profileUpdateCallback();
                }
                profileUpdateCallbacks.Clear();
                if (result.Success && !MixSession.ParentalConsentRequired)
                {
                    //ParentalConsent.ShowConsentGrantedDialog();
                }
            });
        }

        public void TriggerRefreshProfile()
        {
            NextProfileUpdateTime = DateTime.MinValue;
        }

        public void TriggerRefreshProfile(Action callback)
        {
            NextProfileUpdateTime = DateTime.MinValue;
            profileUpdateCallbacks.Add(callback);
        }

        private void AddSessionEventHandlers(ISession aSession)
        {
            aSession.OnAuthenticationLost += eventGenerator.AddEventHandler<AbstractAuthenticationLostEventArgs>(session, HandleAuthLost);
            aSession.OnTerminated += eventGenerator.AddEventHandler<AbstractSessionTerminatedEventArgs>(session, HandleSdkTermination);
        }

        private void RemoveSessionEventHandlers(ISession aSession)
        {
            aSession.OnAuthenticationLost -= eventGenerator.GetEventHandler<AbstractAuthenticationLostEventArgs>(session, HandleAuthLost);
            aSession.OnTerminated -= eventGenerator.GetEventHandler<AbstractSessionTerminatedEventArgs>(session, HandleSdkTermination);
        }

        private void SetAnalyticsNetworkReady(bool canUse)
        {
            try { DMOAnalytics.SharedAnalytics.CanUseNetwork = canUse; } catch (Exception ex) { Debug.LogWarning("DMOAnalytics missing: " + ex.Message); }
        }

        private void FlushAnalyticsQueue()
        {
            try { DMOAnalytics.SharedAnalytics.FlushAnalyticsQueue(); } catch (Exception ex) { Debug.LogWarning("DMOAnalytics missing: " + ex.Message); }
        }

        public void Login(string aUsername, string aPassword, Action<ILoginResult> callback)
        {
            try
            {
                Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
                SetAnalyticsNetworkReady(false);
                sessionStarter.Login(ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.MixPlatformServicesUrl, ExternalizedConstants.GuestControllerClientId, ExternalizedConstants.MixPlatformServicesCellophane, MonoSingleton<AssetManager>.Instance.GetBuildNumber(), CoroutineManager, keychainData, Log.MixLogger, storageDir, aUsername, aPassword, Localizer.GetLocale(), delegate (ILoginResult result)
                {
                    if (result is ILoginCorruptionDetectedResult)
                    {
                        Singleton<MixDocumentCollections>.Instance.DeleteAll();
                    }
                    if (!result.Success)
                    {
                        SetAnalyticsNetworkReady(true);
                        FlushAnalyticsQueue();
                        Analytics.LogFailedLogin(result.GetType().ToString());
                        callback(result);
                    }
                    else
                    {
                        MixSession.InitializeSessionData(result.Session, MixSession.ConnectionState.ONLINE);
                        AddSessionEventHandlers(session);
                        startUserServices(delegate
                        {
                            callback(result);
                        });
                        if (MonoSingleton<PushNotifications>.Instance.HaveShownPushPrePopup)
                        {
                            MonoSingleton<PushNotifications>.Instance.ToggleRegister(true, false);
                        }
                        Analytics.LogUserInfoAction(session.LocalUser.Id);
                        Analytics.LogSuccessfulLogin(session.LocalUser.AgeBandType, 1);
                        Analytics.LogSuccessfulLoginLanguage();
                        Singleton<SettingsManager>.Instance.SetUserSettings();
                    }
                    Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
                });
            }
            catch (Exception exception)
            {
                Log.Exception("Platform sdk threw an exception from login call!", exception);
                Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
                callback(null);
            }
        }

        public void CreateChildAccount(int aYear, int aMonth, int aDay, string aFirstName, string aUsername, string aParentEmail, string aPassword, string aLanguage, IEnumerable<KeyValuePair<IMarketingItem, bool>> aMarketing, IEnumerable<ILegalDocument> aLegal, Action<IRegisterResult> aCallback)
        {
            Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
            SetAnalyticsNetworkReady(false);
            sessionStarter.RegisterChildAccount(ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.MixPlatformServicesUrl, ExternalizedConstants.GuestControllerClientId, ExternalizedConstants.MixPlatformServicesCellophane, MonoSingleton<AssetManager>.Instance.GetBuildNumber(), CoroutineManager, keychainData, Log.MixLogger, storageDir, false, aUsername, aPassword, aFirstName, aParentEmail, aLanguage, new DateTime(aYear, aMonth, aDay), aMarketing, aLegal, delegate (IRegisterResult aResult)
            {
                OnRegisterAccount(aResult, aCallback);
            });
        }

        public void CreateAdultAccount(int aYear, int aMonth, int aDay, string aFirstName, string aLastName, string aEmail, string aPassword, string aLanguage, string assertedCountry, IEnumerable<KeyValuePair<IMarketingItem, bool>> aMarketing, IEnumerable<ILegalDocument> aLegal, Action<IRegisterResult> aCallback)
        {
            Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
            SetAnalyticsNetworkReady(false);
            sessionStarter.RegisterAdultAccount(ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.MixPlatformServicesUrl, ExternalizedConstants.GuestControllerClientId, ExternalizedConstants.MixPlatformServicesCellophane, MonoSingleton<AssetManager>.Instance.GetBuildNumber(), CoroutineManager, keychainData, Log.MixLogger, storageDir, false, aPassword, aFirstName, aLastName, aEmail, aLanguage, assertedCountry, new DateTime(aYear, aMonth, aDay), aMarketing, aLegal, delegate (IRegisterResult aResult)
            {
                OnRegisterAccount(aResult, aCallback);
            });
        }

        public void CreateGuardianAccount(int aYear, int aMonth, int aDay, string aFirstName, string aLastName, string aEmail, string aPassword, string aLanguage, string assertedCountry, IEnumerable<KeyValuePair<IMarketingItem, bool>> aMarketing, IEnumerable<ILegalDocument> aLegal, Action<IRegisterResult> aCallback)
        {
            string localStorageDirPath = Application.PersistentDataPath + "/cacheForGuardian/";
            Singleton<ThreadFramerateThrottler>.Instance.EnterThrottlingSection();
            SetAnalyticsNetworkReady(false);
            sessionStarter.RegisterAdultAccount(ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.MixPlatformServicesUrl, ExternalizedConstants.GuestControllerClientId, ExternalizedConstants.MixPlatformServicesCellophane, MonoSingleton<AssetManager>.Instance.GetBuildNumber(), CoroutineManager, keychainData, Log.MixLogger, localStorageDirPath, false, aPassword, aFirstName, aLastName, aEmail, aLanguage, assertedCountry, new DateTime(aYear, aMonth, aDay), aMarketing, aLegal, aCallback);
        }

        public bool IsProfileMissingInfo()
        {
            if (!MixSession.IsValidSession || session.LocalUser.RegistrationProfile == null)
            {
                return false;
            }
            if (!LastProfileInfo.DateOfBirth.HasValue || LastProfileInfo.DateOfBirth.Equals(DateTime.MinValue))
            {
                return true;
            }
            if (string.IsNullOrEmpty(LastProfileInfo.FirstName))
            {
                return true;
            }
            if (session.LocalUser.AgeBandType == AgeBandType.Child)
            {
                return string.IsNullOrEmpty(LastProfileInfo.ParentEmail);
            }
            return string.IsNullOrEmpty(LastProfileInfo.Email) || string.IsNullOrEmpty(LastProfileInfo.LastName);
        }

        private void OnRegisterAccount(IRegisterResult aResult, Action<IRegisterResult> aCallback)
        {
            Singleton<ThreadFramerateThrottler>.Instance.ExitThrottlingSection();
            if (aResult is IRegisterCorruptionDetectedResult)
            {
                Singleton<MixDocumentCollections>.Instance.DeleteAll();
            }
            if (aResult.Success)
            {
                MixSession.InitializeSessionData(aResult.Session, MixSession.ConnectionState.ONLINE);
                AddSessionEventHandlers(session);
                startUserServices(delegate
                {
                    Analytics.LogSuccessfulLogin(session.LocalUser.AgeBandType, 1);
                    aCallback(aResult);
                }, true);
                if (MonoSingleton<PushNotifications>.Instance.HaveShownPushPrePopup)
                {
                    MonoSingleton<PushNotifications>.Instance.ToggleRegister(true, false);
                }
                if (!MixSession.ParentalConsentRequired)
                {
                    return;
                }
                MixSession.User.SendParentalApprovalEmail(delegate (ISendParentalApprovalEmailResult result)
                {
                    if (result.Success)
                    {
                        ParentalConsent.ShowParentEmailSentDialog();
                    }
                    else
                    {
                        Log.Exception("SendParentalApprovalEmail failed to send");
                    }
                });
            }
            else
            {
                aCallback(aResult);
                SetAnalyticsNetworkReady(true);
                FlushAnalyticsQueue();
            }
        }

        public void Logout(bool aNavToLogin = false)
        {
            if (this.session == null)
            {
                return;
            }
            if (MonoSingleton<NativeVideoPlaybackManager>.Instance != null && MonoSingleton<NativeVideoPlaybackManager>.Instance.Native.IsVideoPlaying())
            {
                MonoSingleton<NativeVideoPlaybackManager>.Instance.Native.Unload();
            }
            ISession session = this.session;
            string userSwid = session.LocalUser.Id;
            Disconnect(true);
            session.LogOut(actionGenerator.CreateAction(delegate (ISessionLogOutResult result)
            {
                Analytics.LogLogoutSuccess(userSwid, result.Success);
                if (aNavToLogin)
                {
                    NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionAnimations(false, "Intro", "ToLogin"));
                    MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest, true);
                }
            }));
        }

        public void Disconnect(bool doingLogout = false)
        {
            if (session != null)
            {
                RemoveSessionEventHandlers(session);
                MixSession.DiscardSessionData();
            }
            isNewUser = true;
        }

        public void DiscardAndReturnToLogin(string navToken)
        {
            ISession session = this.session;
            Disconnect();
            if (session != null)
            {
                session.Dispose();
            }
            MonoSingleton<GameManager>.Instance.QuitGameSession();
            NavigationRequest navigationRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionAnimations(false));
            navigationRequest.AddData(navToken, true);
            navigationRequest.PopLastRequest = true;
            MonoSingleton<NavigationManager>.Instance.AddRequest(navigationRequest);
        }

        private void HandleSdkTermination(object s, AbstractSessionTerminatedEventArgs args)
        {
            Log.Exception("SDK gave us a SDK Termination event!");
            DiscardAndReturnToLogin("fatalError");
        }

        private void HandleAuthLost(object s, AbstractAuthenticationLostEventArgs args)
        {
            if (args is AbstractAccountBannedEventArgs)
            {
                Analytics.LogAuthFailed(session.LocalUser.AgeBandType, "banned");
                DiscardAndReturnToLogin("banned");
            }
            else if (args is AbstractAuthenticationRequiresParentalConsentArgs)
            {
                Analytics.LogAuthFailed(session.LocalUser.AgeBandType, "hallpass_expired");
                DiscardAndReturnToLogin("parentalConsent");
            }
            else if (args is AbstractAuthenticationRevokedEventArgs)
            {
                Analytics.LogAuthFailed(session.LocalUser.AgeBandType, "revoked");
                DiscardAndReturnToLogin("fatalError");
            }
            else if (args is AbstractAuthenticationUnavailableEventArgs)
            {
                Analytics.LogAuthFailed(session.LocalUser.AgeBandType, "unavailable");
                DiscardAndReturnToLogin("fatalError");
            }
        }

        public void StartOfflineAndResume(Action<bool> onSessionOfflined)
        {
            try
            {
                DateTime start = DateTime.Now;
                sessionStarter.OfflineLastSession(ExternalizedConstants.GuestControllerUrl, MixSession.GuestControllerSpoofedIP, ExternalizedConstants.MixPlatformServicesUrl, ExternalizedConstants.GuestControllerClientId, ExternalizedConstants.MixPlatformServicesCellophane, MonoSingleton<AssetManager>.Instance.GetBuildNumber(), CoroutineManager, keychainData, Log.MixLogger, storageDir, Localizer.GetLocale(), delegate (IOfflineLastSessionResult result)
                {
                    if (result is IOfflineLastSessionCorruptionDetectedResult)
                    {
                        Singleton<MixDocumentCollections>.Instance.DeleteAll();
                    }
                    if (result.Success)
                    {
                        MixSession.InitializeSessionData(result.Session, MixSession.ConnectionState.RESUMING);
                        AddSessionEventHandlers(session);
                        startUserServices(delegate
                        {
                            GetThreadsFirstMessage(MixSession.User.ChatThreadsFromUsers(), 0, delegate
                            {
                                onSessionOfflined(true);
                            });
                        });
                        Analytics.LogUserInfoAction(session.LocalUser.Id);
                        Analytics.LogSuccessfulLogin(session.LocalUser.AgeBandType, 0);
                        Analytics.LogSuccessfulLoginLanguage();
                        Singleton<SettingsManager>.Instance.SetUserSettings();
                        if (MonoSingleton<ConnectionManager>.Instance.IsConnected)
                        {
                            MixSession.UnPauseSession(delegate
                            {
                            });
                        }
                        else
                        {
                            MonoSingleton<ConnectionManager>.Instance.ShowBanners(ConnectionManager.BannerState.Disconnected);
                        }
                    }
                    else
                    {
                        onSessionOfflined(false);
                        SetAnalyticsNetworkReady(true);
                        FlushAnalyticsQueue();
                    }
                });
            }
            catch (Exception exception)
            {
                Log.Exception("Platform sdk threw an exception from start offline call!", exception);
                onSessionOfflined(false);
            }
        }

        public void GetThreadsFirstMessage(IEnumerable<IChatThread> chatThreads, int index, Action completedCallback)
        {
            int numRemaining = chatThreads.Count();
            foreach (IChatThread chatThread in chatThreads)
            {
                IChatMessageRetriever chatMessageRetriever = chatThread.CreateChatMessageRetriever(1);
                chatMessageRetriever.RetrieveMessages(delegate
                {
                    if (--numRemaining == 0)
                    {
                        completedCallback();
                    }
                });
            }
        }

        public void StartUserServices(Action callback)
        {
            startUserServices(callback);
        }

        private void startUserServices(Action callback, bool isAccountCreate = false)
        {
            if (!MixSession.IsValidSession)
            {
                NavigationRequest aRequest = new NavigationRequest("Prefabs/Screens/Login/LoginScreen", new TransitionAnimations(false, "Intro", "ToLogin"));
                MonoSingleton<NavigationManager>.Instance.AddRequest(aRequest);
                callback();
                return;
            }
            Singleton<EntitlementsManager>.Instance.LoadMyEntitlements();
            if (isNewUser)
            {
                MonoSingleton<FakeFriendManager>.Instance.Init();
            }
            callback();
            isNewUser = false;
        }
    }
}