using System;
using System.Security.Cryptography;
using DeviceDB;
using Disney.MobileNetwork;

namespace Mix.DeviceDb
{
	public class MixDocumentCollections : Singleton<MixDocumentCollections>
	{
		private AssetCacheDocumentCollectionApi _assetCacheDocumentCollectionApi;

		private KeyValDocumentCollectionApi _keyValDocumentCollectionApi;

		private ChatMetaDataDocumentCollectionApi _chatMetaDataDocumentCollectionApi;

		private AvatarSnapshotDocumentCollectionApi _avatarSnapshotDocumentCollectionApi;

		private AvatarTextureDocumentCollectionApi _avatarTextureDocumentCollectionApi;

		private FakeFirstFriendDocumentCollectionApi _fakeFirstFriendDocumentCollectionApi;

		private OfflineQueueDocumentCollectionApi _offlineQueueDocumentCollectionApi;

		private ContentSeenDocumentCollectionApi _contentSeenDocumentCollectionApi;

		private ChatThreadDocumentCollectionApi _chatThreadDocumentCollectionApi;

		public AssetCacheDocumentCollectionApi assetCacheDocumentCollectionApi
		{
			get
			{
				if (_assetCacheDocumentCollectionApi == null)
				{
					_assetCacheDocumentCollectionApi = new AssetCacheDocumentCollectionApi();
				}
				return _assetCacheDocumentCollectionApi;
			}
			private set
			{
				_assetCacheDocumentCollectionApi = value;
			}
		}

		public KeyValDocumentCollectionApi keyValDocumentCollectionApi
		{
			get
			{
				if (_keyValDocumentCollectionApi == null)
				{
					_keyValDocumentCollectionApi = new KeyValDocumentCollectionApi();
				}
				return _keyValDocumentCollectionApi;
			}
			private set
			{
				_keyValDocumentCollectionApi = value;
			}
		}

		public ChatMetaDataDocumentCollectionApi chatMetaDataDocumentCollectionApi
		{
			get
			{
				if (_chatMetaDataDocumentCollectionApi == null)
				{
					_chatMetaDataDocumentCollectionApi = new ChatMetaDataDocumentCollectionApi();
				}
				return _chatMetaDataDocumentCollectionApi;
			}
			private set
			{
				_chatMetaDataDocumentCollectionApi = value;
			}
		}

		public AvatarSnapshotDocumentCollectionApi avatarSnapshotDocumentCollectionApi
		{
			get
			{
				if (_avatarSnapshotDocumentCollectionApi == null)
				{
					_avatarSnapshotDocumentCollectionApi = new AvatarSnapshotDocumentCollectionApi();
				}
				return _avatarSnapshotDocumentCollectionApi;
			}
			private set
			{
				_avatarSnapshotDocumentCollectionApi = value;
			}
		}

		public AvatarTextureDocumentCollectionApi avatarTextureDocumentCollectionApi
		{
			get
			{
				if (_avatarTextureDocumentCollectionApi == null)
				{
					_avatarTextureDocumentCollectionApi = new AvatarTextureDocumentCollectionApi();
				}
				return _avatarTextureDocumentCollectionApi;
			}
			private set
			{
				_avatarTextureDocumentCollectionApi = value;
			}
		}

		public FakeFirstFriendDocumentCollectionApi fakeFirstFriendDocumentCollectionApi
		{
			get
			{
				if (_fakeFirstFriendDocumentCollectionApi == null)
				{
					_fakeFirstFriendDocumentCollectionApi = new FakeFirstFriendDocumentCollectionApi();
				}
				return _fakeFirstFriendDocumentCollectionApi;
			}
			private set
			{
				_fakeFirstFriendDocumentCollectionApi = value;
			}
		}

		public OfflineQueueDocumentCollectionApi offlineQueueDocumentCollectionApi
		{
			get
			{
				if (_offlineQueueDocumentCollectionApi == null)
				{
					_offlineQueueDocumentCollectionApi = new OfflineQueueDocumentCollectionApi();
				}
				return _offlineQueueDocumentCollectionApi;
			}
			private set
			{
				_offlineQueueDocumentCollectionApi = value;
			}
		}

		public ContentSeenDocumentCollectionApi contentSeenDocumentCollectionApi
		{
			get
			{
				if (_contentSeenDocumentCollectionApi == null)
				{
					_contentSeenDocumentCollectionApi = new ContentSeenDocumentCollectionApi();
				}
				return _contentSeenDocumentCollectionApi;
			}
			private set
			{
				_contentSeenDocumentCollectionApi = value;
			}
		}

		public ChatThreadDocumentCollectionApi chatThreadDocumentCollectionApi
		{
			get
			{
				if (_chatThreadDocumentCollectionApi == null)
				{
					_chatThreadDocumentCollectionApi = new ChatThreadDocumentCollectionApi();
				}
				return _chatThreadDocumentCollectionApi;
			}
			private set
			{
				_chatThreadDocumentCollectionApi = value;
			}
		}

		public DocumentCollectionFactory factory { get; private set; }

		public string DirPath { get; set; }

		public byte[] key { get; private set; }

		public MixDocumentCollections()
		{
			_assetCacheDocumentCollectionApi = null;
			_keyValDocumentCollectionApi = null;
			_chatMetaDataDocumentCollectionApi = null;
			_avatarSnapshotDocumentCollectionApi = null;
			_avatarTextureDocumentCollectionApi = null;
			_fakeFirstFriendDocumentCollectionApi = null;
			_offlineQueueDocumentCollectionApi = null;
			_contentSeenDocumentCollectionApi = null;
			_chatThreadDocumentCollectionApi = null;
			factory = new DocumentCollectionFactory();
			DirPath = Application.PersistentDataPath + "/cache/";
			key = new byte[32];
			string text = KeyChainManager.Instance.GetString("DocumentsKey");
			if (string.IsNullOrEmpty(text))
			{
				new RNGCryptoServiceProvider().GetBytes(key);
				KeyChainManager.Instance.PutString("DocumentsKey", Convert.ToBase64String(key));
			}
			else
			{
				key = Convert.FromBase64String(text);
			}
		}

		public void LogOut()
		{
			keyValDocumentCollectionApi.LogOut();
			chatMetaDataDocumentCollectionApi.LogOut();
			fakeFirstFriendDocumentCollectionApi.LogOut();
			chatThreadDocumentCollectionApi.LogOut();
		}

		public void DeleteAll()
		{
			assetCacheDocumentCollectionApi.Delete();
			keyValDocumentCollectionApi.Delete();
			chatMetaDataDocumentCollectionApi.Delete();
			avatarSnapshotDocumentCollectionApi.Delete();
			avatarTextureDocumentCollectionApi.Delete();
			fakeFirstFriendDocumentCollectionApi.Delete();
			offlineQueueDocumentCollectionApi.Delete();
			contentSeenDocumentCollectionApi.Delete();
			chatThreadDocumentCollectionApi.Delete();
			assetCacheDocumentCollectionApi = null;
			keyValDocumentCollectionApi = null;
			chatMetaDataDocumentCollectionApi = null;
			avatarSnapshotDocumentCollectionApi = null;
			avatarTextureDocumentCollectionApi = null;
			fakeFirstFriendDocumentCollectionApi = null;
			offlineQueueDocumentCollectionApi = null;
			contentSeenDocumentCollectionApi = null;
			chatThreadDocumentCollectionApi = null;
		}

		public void ClearAssetCacheApiAndCollection()
		{
			if (_assetCacheDocumentCollectionApi != null)
			{
				_assetCacheDocumentCollectionApi.collection.Drop();
				_assetCacheDocumentCollectionApi.collection.Dispose();
				_assetCacheDocumentCollectionApi = null;
			}
		}

		public void ClearKeyValApiAndCollection()
		{
			if (_keyValDocumentCollectionApi != null)
			{
				_keyValDocumentCollectionApi.Clear();
				_keyValDocumentCollectionApi = null;
			}
		}

		public void ClearChatMetaDataAndCollection()
		{
			if (_chatMetaDataDocumentCollectionApi != null)
			{
				_chatMetaDataDocumentCollectionApi.Clear();
				_chatMetaDataDocumentCollectionApi = null;
			}
		}

		public void ClearAvatarSnapshotDocumentCollection()
		{
			if (_avatarSnapshotDocumentCollectionApi != null)
			{
				_avatarSnapshotDocumentCollectionApi.Clear();
				_avatarSnapshotDocumentCollectionApi = null;
			}
		}

		public void ClearAvatarTextureDocumentCollection()
		{
			if (_avatarTextureDocumentCollectionApi != null)
			{
				_avatarTextureDocumentCollectionApi.Clear();
				_avatarTextureDocumentCollectionApi = null;
			}
		}

		public void ClearFakeFirstFriendDocumentCollection()
		{
			if (_fakeFirstFriendDocumentCollectionApi != null)
			{
				_fakeFirstFriendDocumentCollectionApi.Clear();
				_fakeFirstFriendDocumentCollectionApi = null;
			}
		}

		public void ClearOfflineQueueDocumentCollection()
		{
			if (_offlineQueueDocumentCollectionApi != null)
			{
				_offlineQueueDocumentCollectionApi.Clear();
				_offlineQueueDocumentCollectionApi = null;
			}
		}

		public void ClearContentSeenDocumentCollection()
		{
			if (_contentSeenDocumentCollectionApi != null)
			{
				_contentSeenDocumentCollectionApi.Clear();
				_contentSeenDocumentCollectionApi = null;
			}
		}

		public void ClearChatThreadDocumentCollection()
		{
			if (_chatThreadDocumentCollectionApi != null)
			{
				_chatThreadDocumentCollectionApi.Clear();
				_chatThreadDocumentCollectionApi = null;
			}
		}
	}
}
