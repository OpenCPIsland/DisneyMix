using System;
using System.Collections.Generic;
using Mix.Assets;
using Mix.DeviceDb;

namespace Mix
{
	public static class ExternalizedConstants
	{
		private class ExternalizedConstant
		{
			public string value;

			public ExternalizedConstantsEventArgs.RefreshStatus status;

			public ExternalizedConstant(string value, ExternalizedConstantsEventArgs.RefreshStatus status)
			{
				this.value = value;
				this.status = status;
			}
		}


		private const string GUEST_CONTROLLER_URL = "guest_baseUrl";

		private const string GUEST_CONTROLLER_CLIENT_ID = "guest_clientId";

		private const string TOU_URL_EN = "tou_url_en";

		private const string PRIVACY_POLICY_URL_EN = "privacy_policy_url_en";

		private const string NEED_HELP_URL_EN = "need_help_url_en";

		private const string CA_PRIVACY_URL_EN = "ca_privacy_url_en";

		private const string COPPA_URL_EN = "coppa_url_en";

		private const string FORCE_UPDATE_VERSION = "force_update_version";

		private const string ANDROID_APP_STORE_LINK = "android_app_store_link";

		private const string IOS_APP_STORE_LINK = "ios_app_store_link";

		private const string CELLOPHANE_CLIENT_ID_ASSETS = "cellophane_client_id_assets";

		private const string CPIPE_URL = "cpipe_url";

		private const string MIX_PLATFORM_SERVICES_URL = "mix_platform_services_url";

		private const string MIX_PLATFORM_SERVICES_CELLOPHANE = "mix_platform_services_cellophane";

		private const string GCM_SENDER_ID = "gcm_sender_id";

		private const int ExternalizedConstantsErrorUpgradeWindow = 30;

		private static Dictionary<string, object> externalizedConstants = new Dictionary<string, object>
		{
			{
				"guest_baseUrl",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.FatalError)
			},
			{
				"guest_clientId",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.FatalError)
			},
			{
				"tou_url_en",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"privacy_policy_url_en",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"need_help_url_en",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"ca_privacy_url_en",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"coppa_url_en",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"force_update_version",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"android_app_store_link",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"ios_app_store_link",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			},
			{
				"cellophane_client_id_assets",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.FatalError)
			},
			{
				"cpipe_url",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.FatalError)
			},
			{
				"mix_platform_services_url",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.FatalError)
			},
			{
				"mix_platform_services_cellophane",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.FatalError)
			},
			{
				"gcm_sender_id",
				new ExternalizedConstant(null, ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError)
			}
		};

		private static int numTimesLoaded = 0;

		private static DateTime lastExternalizedConstantsError = new DateTime(0L);

		public static string GuestControllerUrl
		{
			get
			{
#if UNITY_ANDROID || !UNITY_EDITOR
				return "http://10.0.0.77:80";
#else
				return "http://localhost:80";
#endif
			}
		}

		public static string GuestControllerClientId
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["guest_clientId"]).value;
			}
		}

		public static string TouUrl
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["tou_url_en"]).value;
			}
		}

		public static string PrivacyPolicyUrl
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["privacy_policy_url_en"]).value;
			}
		}

		public static string NeedHelpUrl
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["need_help_url_en"]).value;
			}
		}

		public static string CaPrivacyUrl
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["ca_privacy_url_en"]).value;
			}
		}

		public static string CoppaUrl
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["coppa_url_en"]).value;
			}
		}

		public static string ForceUpdateVersion
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["force_update_version"]).value;
			}
		}

		public static string AndroidAppStoreLink
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["android_app_store_link"]).value;
			}
		}

		public static string IosAppStoreLink
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["ios_app_store_link"]).value;
			}
		}

		public static string CellophaneClientIdAssets
		{
			get
			{
#if UNITY_ANDROID || !UNITY_EDITOR
				return "http://10.0.0.77:80";
#else
                return "http://localhost:80";
#endif
            }
        }


        public static string CpipeUrl
		{
			get
			{
#if UNITY_ANDROID || !UNITY_EDITOR
				return "http://10.0.0.77:80";
#else
                return "http://localhost:80";
#endif
            }
        }

		public static string MixPlatformServicesUrl
		{
			get
			{
#if UNITY_ANDROID || !UNITY_EDITOR
				return "http://10.0.0.77:80";
#else
                return "http://localhost:80";
#endif
            }
        }

		public static string MixPlatformServicesCellophane
		{
			get
			{
#if UNITY_ANDROID || !UNITY_EDITOR
				return "http://10.0.0.77:80";
#else
                return "http://localhost:80";
#endif
            }
        }

		public static string GcmSenderId
		{
			get
			{
				return ((ExternalizedConstant)externalizedConstants["gcm_sender_id"]).value;
			}
		}

		public static event EventHandler<ExternalizedConstantsEventArgs> OnExternalizedConstantsRefreshed;

		static ExternalizedConstants()
		{
			ExternalizedConstants.OnExternalizedConstantsRefreshed = delegate
			{
			};
		}

		public static void SetupEventHandlers()
		{
			Singleton<ConfigurationManager>.Instance.OnConfigurationRefreshed += HandleOnConfigurationRefreshed;
		}

		public static void HandleOnConfigurationRefreshed(object sender, ConfigurationRefreshEventArgs args)
		{
			ExternalizedConstantsEventArgs.RefreshStatus refreshStatus = ExternalizedConstantsEventArgs.RefreshStatus.Success;
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			numTimesLoaded++;
			if (args != null && args.Success && args.Configuration != null)
			{
				foreach (KeyValuePair<string, object> externalizedConstant2 in externalizedConstants)
				{
					ExternalizedConstant externalizedConstant = (ExternalizedConstant)externalizedConstants[externalizedConstant2.Key];
					if (args.Configuration.ContainsKey(externalizedConstant2.Key))
					{
						externalizedConstant.value = args.Configuration[externalizedConstant2.Key];
						continue;
					}
					if (externalizedConstant.status == ExternalizedConstantsEventArgs.RefreshStatus.FatalError || (externalizedConstant.status == ExternalizedConstantsEventArgs.RefreshStatus.NonFatalError && refreshStatus != ExternalizedConstantsEventArgs.RefreshStatus.FatalError))
					{
						refreshStatus = externalizedConstant.status;
					}
					if (externalizedConstant.value == null || externalizedConstant.value == string.Empty)
					{
						list.Add(externalizedConstant2.Key);
					}
					else
					{
						list2.Add(externalizedConstant2.Key);
					}
				}
				if (list.Count != 0 || list2.Count != 0)
				{
					if (DateTime.Now.CompareTo(lastExternalizedConstantsError.AddSeconds(30.0)) < 0)
					{
						Log.Exception("Error parsing ExternalizedConstants on " + Util.AddOrdinal(numTimesLoaded) + " time loaded; missing: " + string.Join(", ", list.ToArray()) + " stale: " + string.Join(", ", list2.ToArray()));
					}
					lastExternalizedConstantsError = DateTime.Now;
					if (list.Count != 0 && refreshStatus == ExternalizedConstantsEventArgs.RefreshStatus.FatalError)
					{
						Singleton<MixDocumentCollections>.Instance.assetCacheDocumentCollectionApi.DeleteRecordBySha(AssetManager.GetShaString(ConfigurationManager.RelativePathToConfigFile));
						Singleton<ConfigurationManager>.Instance.Refresh(true, "ExternalizedConstants");
						return;
					}
				}
			}
			else
			{
				refreshStatus = ExternalizedConstantsEventArgs.RefreshStatus.FatalError;
			}
			ExternalizedConstants.OnExternalizedConstantsRefreshed(null, new ExternalizedConstantsEventArgs(refreshStatus));
		}

		public static void ResetExternalizedConstants()
		{
			numTimesLoaded = 0;
			foreach (KeyValuePair<string, object> externalizedConstant2 in externalizedConstants)
			{
				ExternalizedConstant externalizedConstant = (ExternalizedConstant)externalizedConstants[externalizedConstant2.Key];
				externalizedConstant.value = string.Empty;
			}
		}
	}
}
