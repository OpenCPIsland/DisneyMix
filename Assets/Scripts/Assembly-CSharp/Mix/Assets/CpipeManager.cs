using System;
using LitJson;
using UnityEngine;

namespace Mix.Assets
{
	public class CpipeManager
	{
		public delegate void CpipeManagerLoadedEvent(CpipeEvent cpipeEvent);

		public const string AUTH_TOKEN = "google_oauth_token";

		public const string AUTH_RENEW_TOKEN = "google_oauth_renew";

		private const string MANIFEST_KEY = "manifest";

		private const string PATHS_KEY = "paths";

		public Cpipe cpipe;

		private Cpipe TempCpipe;

		public bool CpipeIsReady;

		public bool IsFirstRun = true;


		private long AuthTimeoutStartSeconds;

		private long ExpireSeconds;

		private bool IsRefreshingToken;

		private bool IsRefreshingCpipe;

		private string authToken = string.Empty;

		private MonoBehaviour MonoBehaviourEngine;

		private JsonData LocalHashes;

		private string OldManifestVersion;


		private float TimeSinceLastCpipeRefresh;

		public static int num;

		public event CpipeManagerLoadedEvent OnCpipeLoaded;

		public CpipeManager(MonoBehaviour aMonoBehaviour)
		{
			MonoBehaviourEngine = aMonoBehaviour;
		}

		public bool IsUseAssetAuth()
		{
			return false;
		}

		public string GetAuthToken()
		{
			if (IsUseAssetAuth() && PlayerPrefs.HasKey("google_oauth_token"))
			{
				if (string.IsNullOrEmpty(authToken))
				{
					authToken = PlayerPrefs.GetString("google_oauth_token");
				}
				return authToken;
			}
			return string.Empty;
		}

		public void init()
		{
			try
			{
				JsonData firstRunCpipeData = PreloadData.FirstRunCpipeData;
				LocalHashes = firstRunCpipeData["manifest"]["paths"];
			}
			catch (Exception exception)
			{
				Log.Exception(exception);
			}
			if (IsUseAssetAuth())
			{
				refreshToken();
			}
			else
			{
				LoadCpipe(CachePolicy.DefaultCacheControlProtocol);
			}
		}

		public void refreshToken()
		{
			if (!IsRefreshingToken)
			{
				IsRefreshingToken = true;
				GoogleOAuthHelper googleOAuthHelper = new GoogleOAuthHelper();
				MonoBehaviourEngine.StartCoroutine(googleOAuthHelper.GetOAuthRequest(OAuthAccessTokenCallback));
			}
		}

		private void OAuthAccessTokenCallback(AuthResponse aAuthResponse)
		{
			IsRefreshingToken = false;
			if (aAuthResponse != null)
			{
				AuthTimeoutStartSeconds = 0L;
				authToken = aAuthResponse.access_token;
				PlayerPrefs.SetString("google_oauth_token", authToken);
				AuthTimeoutStartSeconds = GoogleOAuthHelper.GetUnixEpochTimeSecs();
				ExpireSeconds = AuthTimeoutStartSeconds + aAuthResponse.expires_in - 300;
				if (ExpireSeconds < 0)
				{
					ExpireSeconds = GoogleOAuthHelper.GetUnixEpochTimeSecs() + aAuthResponse.expires_in;
				}
				PlayerPrefs.SetString("google_oauth_renew", ExpireSeconds.ToString());
				this.OnCpipeLoaded(CpipeEvent.AuthToken);
				if (IsFirstRun)
				{
					LoadCpipe(CachePolicy.DefaultCacheControlProtocol);
				}
			}
			else
			{
				if (IsFirstRun)
				{
					LoadCpipe(CachePolicy.DefaultCacheControlProtocol);
				}
				AuthTimeoutStartSeconds = 0L;
				ExpireSeconds = 60L;
				this.OnCpipeLoaded(CpipeEvent.AuthTokenFailed);
			}
		}

		public void Update()
		{
			// CPIPE backend retired: disable token/cpipe refresh loop.
			return;
		}

		public void LoadCpipe(CachePolicy aCachePolicy)
		{
			if (!IsRefreshingCpipe)
			{
				IsRefreshingCpipe = true;
				TempCpipe = new Cpipe();
				if (cpipe != null)
				{
					OldManifestVersion = cpipe.GetManifestVersion();
				}
				TempCpipe.Init(OnCpipeReady, OnCpipeFail, IsFirstRun, aCachePolicy, OldManifestVersion);
				IsFirstRun = false;
			}
		}

		public void OnCpipeReady(bool newManifest)
		{
			IsRefreshingCpipe = false;
			CpipeIsReady = true;
			if (newManifest)
			{
				cpipe = TempCpipe;
				cpipe.SetLocalHashes(LocalHashes);
				this.OnCpipeLoaded(CpipeEvent.CpipeData);
			}
			else
			{
				this.OnCpipeLoaded(CpipeEvent.CpipeUnchanged);
			}
		}

		public void OnCpipeFail(string error)
		{
			IsRefreshingCpipe = false;
			this.OnCpipeLoaded(CpipeEvent.CpipeDataFailed);
		}
	}
}
