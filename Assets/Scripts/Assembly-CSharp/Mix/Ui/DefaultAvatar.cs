using Avatar.DataTypes;

namespace Mix.Ui
{
	public static class DefaultAvatar
	{
		public static ClientAvatar GetDefaultAvatar()
		{
			ClientAvatar clientAvatar = new ClientAvatar();
			clientAvatar.Accessory = new ClientAvatarProperty("3", 17, 0.0, 0.0);
			clientAvatar.Costume = new ClientAvatarProperty("15", 0, 0.0, 0.0);
			clientAvatar.Brow = new ClientAvatarProperty("9", 0, 0.0, 0.00417972784489393);
			clientAvatar.Eyes = new ClientAvatarProperty("54", 11, 0.0, 0.0121355056762695);
			clientAvatar.Hair = new ClientAvatarProperty("69", 7, 0.0, 0.0);
			clientAvatar.Nose = new ClientAvatarProperty("106", 0, 0.0, 0.0116169452667236);
			clientAvatar.Mouth = new ClientAvatarProperty("194", 0, -0.00126063823699951, 0.0);
			clientAvatar.Skin = new ClientAvatarProperty("123", 9, 0.0, 0.0);
			clientAvatar.Hat = new ClientAvatarProperty("0", 0, 0.0, 0.0);
			return clientAvatar;
		}
	}
}
