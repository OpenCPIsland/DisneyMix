using System;
using Avatar;
using Avatar.DataTypes;
using Disney.Mix.SDK;
using Mix.Assets;

namespace Mix.FakeFriend.Datatypes
{
	public class FakeFriendData : IFriend, IRemoteChatMember, ITextAssetObject
	{
		private const string mixbotDna = "mixbotdna.txt";

		private const string mixbotDnaPath = "data/Avatar/mixbotdna.txt";

		private ClientAvatar avatar;

		private FakeDisplayName displayName;

		private string characterName;

		private string id;

		public bool IsOfflineCharacter { get; private set; }

		public bool IsTrusted
		{
			get
			{
				return true;
			}
		}

		public IDisplayName DisplayName
		{
			get
			{
				return displayName;
			}
		}

		public IUserNickname Nickname
		{
			get
			{
				return null;
			}
		}

		public IAvatar Avatar
		{
			get
			{
				return avatar;
			}
		}

		public AccountStatus Status
		{
			get
			{
				return AccountStatus.Active;
			}
		}

		public string Id
		{
			get
			{
				return id;
			}
		}

		public string HashedId
		{
			get
			{
				return id;
			}
		}

		public string FirstName
		{
			get
			{
				return characterName;
			}
		}

		public ChatMemberType ChatMemberType
		{
			get
			{
				return ChatMemberType.Bot;
			}
		}

		public event EventHandler<AbstractAvatarChangedEventArgs> OnAvatarChanged = delegate
		{
		};

		public event EventHandler<AbstractNicknameChangedEventArgs> OnNicknameChanged = delegate
		{
		};

		public FakeFriendData()
			: this("Mixbot", "Fake Friend", false)
		{
		}

		public FakeFriendData(string characterName, string id, bool isOfflineCharacter, string costumeId = null)
		{
			this.characterName = characterName;
			this.id = id;
			IsOfflineCharacter = isOfflineCharacter;
			displayName = new FakeDisplayName();
			displayName.Text = characterName;
			avatar = new ClientAvatar();
			avatar.Accessory = new ClientAvatarProperty("3", 13, 0.0, 0.0);
			avatar.Costume = new ClientAvatarProperty("15", 0, 0.0, 0.0);
			avatar.Brow = new ClientAvatarProperty("185", 0, 0.0, 0.0124999998137355);
			avatar.Eyes = new ClientAvatarProperty("48", 8, 0.0, 0.0124999998137355);
			avatar.Hair = new ClientAvatarProperty("186", 3, 0.0, 0.0);
			avatar.Nose = new ClientAvatarProperty("101", 0, 0.0, 0.00203833859413862);
			avatar.Mouth = new ClientAvatarProperty("84", 0, 0.0, 0.0110180018469691);
			avatar.Skin = new ClientAvatarProperty("123", 3, 0.0, 0.0);
			avatar.Hat = new ClientAvatarProperty("257", 0, 0.0, 0.0);
			if (isOfflineCharacter)
			{
				avatar.Costume = new ClientAvatarProperty(string.IsNullOrEmpty(costumeId) ? "0" : costumeId, 0, 0.0, 0.0);
			}
			if (!isOfflineCharacter)
			{
				loadMixbotDna();
				MonoSingleton<AssetManager>.Instance.cpipeManager.OnCpipeLoaded += OnCpipeReady;
			}
		}

		public void Report(IChatThread thread, ReportUserReason reason, Action<IReportUserResult> callback)
		{
		}

		public bool IsSameUser(IFriend user)
		{
			if (user is FakeFriendData)
			{
				return user != null && Id == user.Id;
			}
			return false;
		}

		public bool IsSameUser(IRemoteChatMember user)
		{
			if (user is FakeFriendData)
			{
				return user != null && Id == user.Id;
			}
			return false;
		}

		private void loadMixbotDna()
		{
			LoadParams aLoadParams = new LoadParams(AssetManager.GetShaString("data/Avatar/mixbotdna.txt"), "data/Avatar/mixbotdna.txt");
			MonoSingleton<AssetManager>.Instance.LoadText(this, aLoadParams);
		}

		public void TextAssetObjectComplete(string aText, object aUserData)
		{
			if (aText == null)
			{
				return;
			}
			try
			{
				IAvatar avatar = AvatarApi.DeserializeAvatar(aText);
				if (AvatarApi.ValidateAvatar(avatar))
				{
					this.avatar = new ClientAvatar(avatar);
				}
			}
			catch (Exception exception)
			{
				Log.Exception("Unable to deserialize Mix Bot's DNA", exception);
			}
		}

		public void OnCpipeReady(CpipeEvent cpipeEvent)
		{
			if (MonoSingleton<AssetManager>.Instance.LatestManifestVersionNewerThanCachedVersion("data/Avatar/mixbotdna.txt"))
			{
				loadMixbotDna();
			}
		}
	}
}
