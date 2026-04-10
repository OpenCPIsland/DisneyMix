using Disney.Mix.SDK.Internal.MixDomain;

namespace Disney.Mix.SDK.Internal
{
	public static class AvatarBuilder
	{
		public static IInternalAvatar Build(Disney.Mix.SDK.Internal.MixDomain.Avatar avatar)
		{
			UnityEngine.Debug.Log("[AvatarBuilder] Build(MixDomain.Avatar) called");
			Avatar result;
			if (avatar == null)
			{
				UnityEngine.Debug.LogWarning("[AvatarBuilder] Input avatar is null");
				result = new Avatar();
			}
			else
			{
				UnityEngine.Debug.Log($"[AvatarBuilder] Accessory: {avatar.Accessory.SelectionKey}, Hat: {avatar.Hat.SelectionKey}");
				AvatarProperty accessory = new AvatarProperty(avatar.Accessory.SelectionKey, avatar.Accessory.TintIndex.Value, avatar.Accessory.XOffset.Value, avatar.Accessory.YOffset.Value);
				AvatarProperty brow = new AvatarProperty(avatar.Brow.SelectionKey, avatar.Brow.TintIndex.Value, avatar.Brow.XOffset.Value, avatar.Brow.YOffset.Value);
				AvatarProperty costume = new AvatarProperty(avatar.Costume.SelectionKey, avatar.Costume.TintIndex.Value, avatar.Costume.XOffset.Value, avatar.Costume.YOffset.Value);
				AvatarProperty eyes = new AvatarProperty(avatar.Eyes.SelectionKey, avatar.Eyes.TintIndex.Value, avatar.Eyes.XOffset.Value, avatar.Eyes.YOffset.Value);
				AvatarProperty nose = new AvatarProperty(avatar.Nose.SelectionKey, avatar.Nose.TintIndex.Value, avatar.Nose.XOffset.Value, avatar.Nose.YOffset.Value);
				AvatarProperty mouth = new AvatarProperty(avatar.Mouth.SelectionKey, avatar.Mouth.TintIndex.Value, avatar.Mouth.XOffset.Value, avatar.Mouth.YOffset.Value);
				AvatarProperty skin = new AvatarProperty(avatar.Skin.SelectionKey, avatar.Skin.TintIndex.Value, avatar.Skin.XOffset.Value, avatar.Skin.YOffset.Value);
				AvatarProperty hair = new AvatarProperty(avatar.Hair.SelectionKey, avatar.Hair.TintIndex.Value, avatar.Hair.XOffset.Value, avatar.Hair.YOffset.Value);
				AvatarProperty hat = null;
				long value = avatar.AvatarId.Value;
				int? slotId = avatar.SlotId;
				result = new Avatar(accessory, brow, costume, eyes, nose, mouth, skin, hair, hat, value, slotId.HasValue ? slotId.Value : 0);
			}
			return result;
		}

		public static IInternalAvatar Build(AvatarDocument avatarDocument)
		{
			UnityEngine.Debug.Log("[AvatarBuilder] Build(AvatarDocument) called");
			return (avatarDocument != null) ? new Avatar(new AvatarProperty(avatarDocument.AccessoryPropertySelectionKey, avatarDocument.AccessoryPropertyTintIndex, avatarDocument.AccessoryPropertyXOffset, avatarDocument.AccessoryPropertyYOffset), new AvatarProperty(avatarDocument.BrowPropertySelectionKey, avatarDocument.BrowPropertyTintIndex, avatarDocument.BrowPropertyXOffset, avatarDocument.BrowPropertyYOffset), new AvatarProperty(avatarDocument.CostumePropertySelectionKey, avatarDocument.CostumePropertyTintIndex, avatarDocument.CostumePropertyXOffset, avatarDocument.CostumePropertyYOffset), new AvatarProperty(avatarDocument.EyesPropertySelectionKey, avatarDocument.EyesPropertyTintIndex, avatarDocument.EyesPropertyXOffset, avatarDocument.EyesPropertyYOffset), new AvatarProperty(avatarDocument.NosePropertySelectionKey, avatarDocument.NosePropertyTintIndex, avatarDocument.NosePropertyXOffset, avatarDocument.NosePropertyYOffset), new AvatarProperty(avatarDocument.MouthPropertySelectionKey, avatarDocument.MouthPropertyTintIndex, avatarDocument.MouthPropertyXOffset, avatarDocument.MouthPropertyYOffset), new AvatarProperty(avatarDocument.SkinPropertySelectionKey, avatarDocument.SkinPropertyTintIndex, avatarDocument.SkinPropertyXOffset, avatarDocument.SkinPropertyYOffset), new AvatarProperty(avatarDocument.HairPropertySelectionKey, avatarDocument.HairPropertyTintIndex, avatarDocument.HairPropertyXOffset, avatarDocument.HairPropertyYOffset), new AvatarProperty(avatarDocument.HatPropertySelectionKey, avatarDocument.HatPropertyTintIndex, avatarDocument.HatPropertyXOffset, avatarDocument.HatPropertyYOffset), avatarDocument.AvatarId, avatarDocument.SlotId) : new Avatar();
		}
	}
}
