using UnityEngine;

namespace Mix.AvatarInternal
{
	public class AvatarColorTints
	{
		public enum HairColor
		{
			PLATINUM_BLOND = 0,
			DARK_BLOND = 1,
			AUBURN = 2,
			BROWN = 3,
			RED = 4,
			RUSSET = 5,
			DARK_RED = 6,
			MAHOGANY = 7,
			BLACK = 8,
			PINK = 9,
			GOLD = 10,
			VIOLET = 11,
			LIGHT_PINK = 12,
			KOHL = 13,
			STEEL = 14,
			BRONZE = 15,
			SLATE = 16,
			SAPLING = 17,
			BEAUJOLAIS = 18,
			TURQUOISE = 19
		}

		public enum EyeColor
		{
			TEAL = 0,
			OLIVE = 1,
			TAUPE = 2,
			SCARLET = 3,
			KOHL = 4,
			SLATE = 5,
			EMERALD = 6,
			AMBER = 7,
			SUNSET = 8,
			FOREST = 9,
			DUTCH_BLUE = 10,
			PURPLE = 11,
			CHESTNUT = 12,
			CHARTREUSE = 13,
			PINK = 14,
			AMETHYST = 15
		}

		public enum SkinColor
		{
			_001 = 0,
			_002 = 1,
			_003 = 2,
			_004 = 3,
			_005 = 4,
			_006 = 5,
			_007 = 6,
			_008 = 7,
			_009 = 8,
			_010 = 9,
			_011 = 10,
			_012 = 11,
			_013 = 12,
			_014 = 13,
			_015 = 14,
			_016 = 15,
			_017 = 16,
			_018 = 17,
			_019 = 18,
			GAMMORAH_GREEN = 19,
			PORCELAIN_WHITE = 20,
			MYSTIQUE_BLUE = 21,
			PNK_PINK = 22,
			BARSOOM_RED = 23,
			TIGGER_ORANGE = 24,
			STEEL = 25,
			URSULA_PURPLE = 26
		}

		public enum AccessoryColor
		{
			RED = 0,
			NAVY = 1,
			GREEN = 2,
			CYAN = 3,
			PINK = 4,
			ROSE = 5,
			PURPLE = 6,
			LIME_GREEN = 7,
			BLUE = 8,
			UMBER_DARK = 9,
			UMBER_MED = 10,
			UMBER_LIGHT = 11,
			BROWN_DARK = 12,
			BROWN_MED = 13,
			BROWN_LIGHT = 14,
			SADDLE_BROWN = 15,
			DARK_ORANGE = 16,
			MUSTARD = 17,
			DARK_OLIVE = 18,
			OLIVE_MED = 19,
			LIGHT_OLIVE = 20,
			SLATE_DARK = 21,
			SLATE_MED = 22,
			SLATE_LIGHT = 23
		}

		public static Color[] HairColors = new Color[20]
		{
			new Color(0.99607843f, 0.9843137f, 0.8784314f),
			new Color(0.9254902f, 64f / 85f, 0.4862745f),
			new Color(41f / 51f, 44f / 85f, 19f / 51f),
			new Color(25f / 51f, 27f / 85f, 0.20392157f),
			new Color(0.8980392f, 19f / 85f, 0.22745098f),
			new Color(0.7058824f, 0.29411766f, 0.050980393f),
			new Color(57f / 85f, 0.14901961f, 0.043137256f),
			new Color(0.17254902f, 1f / 17f, 1f / 85f),
			new Color(11f / 51f, 0.23137255f, 4f / 15f),
			new Color(1f, 0.47058824f, 0.94509804f),
			new Color(0.99215686f, 57f / 85f, 8f / 51f),
			new Color(49f / 85f, 0.2901961f, 83f / 85f),
			new Color(1f, 0.5921569f, 0.94509804f),
			new Color(0.101960786f, 0.10980392f, 0.1254902f),
			new Color(29f / 51f, 29f / 51f, 29f / 51f),
			new Color(33f / 85f, 0.34901962f, 0.29411766f),
			new Color(21f / 85f, 0.22745098f, 0.20784314f),
			new Color(42f / 85f, 64f / 85f, 0.09019608f),
			new Color(0.80784315f, 0.39607844f, 0.8980392f),
			new Color(0.14509805f, 0.7019608f, 0.6313726f)
		};

		public static Color[] EyeColors = new Color[16]
		{
			new Color(0f, 0.8392157f, 1f),
			new Color(1f, 49f / 51f, 0f),
			new Color(0.28627452f, 0.14901961f, 1f / 51f),
			new Color(66f / 85f, 0f, 0.07450981f),
			new Color(0f, 0f, 0f),
			new Color(43f / 85f, 0.4392157f, 53f / 85f),
			new Color(0.11764706f, 0.85490197f, 0.12156863f),
			new Color(0.8862745f, 0.49803922f, 0f),
			new Color(0.2627451f, 0.5882353f, 0.34509805f),
			new Color(0.4627451f, 0.4862745f, 1f),
			new Color(0.38431373f, 0.34509805f, 1f),
			new Color(0.41960785f, 0.16862746f, 0f),
			new Color(0.70980394f, 0.9843137f, 0.050980393f),
			new Color(1f, 0.6039216f, 0.77254903f),
			new Color(0.6039216f, 0.003921569f, 0.44313726f),
			new Color32(0xFF, 0x66, 0x66, 0xFF)
		};

		public static Color[] SkinColors = new Color[27]
		{
			new Color(0.99607843f, 0.8901961f, 0.77254903f),
			new Color(1f, 0.85490197f, 38f / 51f),
			new Color(1f, 0.80784315f, 0.7058824f),
			new Color(0.99215686f, 0.7647059f, 57f / 85f),
			new Color(0.99607843f, 66f / 85f, 0.654902f),
			new Color(49f / 51f, 0.7372549f, 52f / 85f),
			new Color(79f / 85f, 0.6901961f, 29f / 51f),
			new Color(76f / 85f, 0.6509804f, 44f / 85f),
			new Color(0.85490197f, 52f / 85f, 41f / 85f),
			new Color(69f / 85f, 49f / 85f, 0.42745098f),
			new Color(66f / 85f, 0.52156866f, 0.40784314f),
			new Color(32f / 51f, 33f / 85f, 24f / 85f),
			new Color(0.5568628f, 1f / 3f, 0.2627451f),
			new Color(43f / 85f, 4f / 15f, 16f / 85f),
			new Color(0.4745098f, 0.28627452f, 19f / 85f),
			new Color(0.32156864f, 0.18039216f, 0.14509805f),
			new Color(0.2509804f, 12f / 85f, 9f / 85f),
			new Color(0.20392157f, 0.12156863f, 0.09019608f),
			new Color(48f / 85f, 0.7647059f, 24f / 85f),
			new Color(50f / 51f, 0.96862745f, 0.94509804f),
			new Color(0.2784314f, 0.6784314f, 0.8862745f),
			new Color32(0x8A, 0x8C, 0xFF, 0xFF),
			new Color(1f, 0.8156863f, 0.9372549f),
			new Color(1f, 0.6431373f, 0.18431373f),
			new Color(0.8392157f, 0.7607843f, 11f / 15f),
			new Color(56f / 85f, 24f / 85f, 62f / 85f),
			new Color32(0xFF, 0x66, 0x66, 0xFF)
		};

		public static Color[] AccessoryColors = new Color[24]
		{
			new Color(0.8392157f, 0f, 0f),
			new Color(0f, 0.1764706f, 47f / 85f),
			new Color(0f, 0.08627451f, 0.2627451f),
			new Color(0f, 1f, 72f / 85f),
			new Color(1f, 0.6313726f, 72f / 85f),
			new Color(1f, 46f / 85f, 0.7019608f),
			new Color(46f / 85f, 0.5568628f, 1f),
			new Color(0f, 1f, 0.02745098f),
			new Color(0f, 49f / 85f, 1f),
			new Color(0.20784314f, 0.16078432f, 0.12156863f),
			new Color(0.4117647f, 0.35686275f, 26f / 85f),
			new Color(0.59607846f, 0.56078434f, 0.4627451f),
			new Color(24f / 85f, 13f / 85f, 4f / 51f),
			new Color(36f / 85f, 0.29411766f, 0.101960786f),
			new Color(0.7137255f, 0.6039216f, 0.44313726f),
			new Color(36f / 85f, 0.19215687f, 0.03137255f),
			new Color(0.77254903f, 38f / 85f, 0f),
			new Color(14f / 15f, 73f / 85f, 0.45490196f),
			new Color(0.2f, 0.23529412f, 0.101960786f),
			new Color(0.4f, 8f / 15f, 21f / 85f),
			new Color(35f / 51f, 0.7921569f, 48f / 85f),
			new Color(23f / 85f, 0.32156864f, 31f / 85f),
			new Color(0.47843137f, 44f / 85f, 29f / 51f),
			new Color(0.6784314f, 0.7137255f, 11f / 15f)
		};

		public static Color[] GetColorsByCategoryName(string categoryName)
		{
			switch (categoryName)
			{
			case "Hair":
				return HairColors;
			case "Eyes":
				return EyeColors;
			case "Accessory":
				return AccessoryColors;
			case "Skin":
				return SkinColors;
			default:
				return null;
			}
		}
	}
}
