using Disney.Mix.SDK;

namespace Avatar.DataTypes
{
	public class ClientAvatar : IAvatar
	{
		public IAvatarProperty Accessory { get; set; }

		public IAvatarProperty Brow { get; set; }

		public IAvatarProperty Costume { get; set; }

		public IAvatarProperty Eyes { get; set; }

		public IAvatarProperty Hair { get; set; }

		public IAvatarProperty Nose { get; set; }

		public IAvatarProperty Mouth { get; set; }

		public IAvatarProperty Skin { get; set; }

		public IAvatarProperty Hat { get; set; }

		public ClientAvatar()
		{
		}

		public ClientAvatar(IAvatar avatar)
		{
			Accessory = avatar.Accessory;
			Brow = avatar.Brow;
			Costume = avatar.Costume;
			Eyes = avatar.Eyes;
			Nose = avatar.Nose;
			Mouth = avatar.Mouth;
			Skin = avatar.Skin;
			Hair = avatar.Hair;
			if (avatar.Hat != null)
			{
				Hat = avatar.Hat;
			}
			else
			{
				Hat = new ClientAvatarProperty("0", 0, 0.0, 0.0);
			}
		}
	}
}
